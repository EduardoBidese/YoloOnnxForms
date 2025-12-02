"""
process_video_stream.py

Script for:
- Loading a YOLO model (Ultralytics)
- Running detection + ByteTrack tracking frame by frame
- Drawing boxes on each frame
- Sending ONE JSON line per frame through stdout, with:
    - frame_index
    - time_seconds
    - detections: list of objects {track_id, class_id, score, x, y, w, h, is_new}
    - image: JPG base64 string of the annotated frame

IMPORTANT:
- Save this file as UTF-8 (no BOM if possible).
- Avoid non-ASCII characters to prevent encoding issues on Windows.
"""

import argparse
import base64
import json
import os
import sys

import cv2
import numpy as np
from ultralytics import YOLO
import supervision as sv

# ============================================================
# CONFIG SECTION - YOU CAN TUNE THESE PARAMETERS
# ============================================================

# Default confidence threshold if not provided by --conf
DEFAULT_CONF = 0.35

# -------------------------------------------------------------------
# ByteTrack parameters (compatível com o exemplo que funcionou pra você)
# -------------------------------------------------------------------
# track_activation_threshold: min confidence for a detection to START a track
# lost_track_buffer: how many frames we keep a lost track before deleting it
# minimum_matching_threshold: how strict to match detections to existing tracks
# frame_rate: will be set from video FPS
# minimum_consecutive_frames: min number of consecutive frames before track is considered "stable"
BYTE_TRACK_ACTIVATION_THRESHOLD = 0.25
BYTE_LOST_TRACK_BUFFER = 30
BYTE_MIN_MATCHING_THRESHOLD = 0.80
BYTE_MIN_CONSECUTIVE_FRAMES = 3
BYTE_FRAME_RATE_FALLBACK = 30.0  # used if video has no FPS info

# -------------------------------------------------------------------
# Optional: Region Of Interest (ROI)
# -------------------------------------------------------------------
# If you want to restrict detections to a rectangle:
#   USE_ROI = True
#   ROI_RECT = (x_min, y_min, x_max, y_max)
USE_ROI = False
ROI_RECT = (100, 100, 500, 400)  # example values, edit as needed

# If True, only detections whose CENTER is inside ROI_RECT will be kept.
FILTER_BY_ROI_CENTER = True

# -------------------------------------------------------------------
# Optional: Class filter (similar to your 'Cebola' example)
# -------------------------------------------------------------------
USE_CLASS_FILTER = False          # set True to filter by class names
SELECTED_CLASS_NAMES = ["Cebola"]  # will map to IDs based on model.model.names

# ============================================================
# HELPER FUNCTIONS
# ============================================================

def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument("--model", type=str, required=True, help="Path to YOLO .pt model")
    parser.add_argument("--video", type=str, required=True, help="Path to video or image")
    parser.add_argument("--conf", type=float, default=DEFAULT_CONF, help="YOLO confidence threshold")
    return parser.parse_args()


def frame_to_base64_bgr(frame):
    """
    Convert BGR frame (OpenCV) to JPG base64 string.
    """
    ret, buf = cv2.imencode(".jpg", frame)
    if not ret:
        raise RuntimeError("Could not encode frame to JPG")
    return base64.b64encode(buf).decode("ascii")


def apply_roi_filter(detections, roi_rect, filter_by_center=True):
    """
    Filter detections by a rectangular ROI.
    detections: supervision.Detections object
    roi_rect: (x_min, y_min, x_max, y_max)
    If filter_by_center is True, keep detections whose center is inside the ROI.
    """
    if detections is None or len(detections) == 0:
        return detections

    x_min, y_min, x_max, y_max = roi_rect

    # centers of boxes
    centers = detections.get_anchors_coordinates(sv.Position.CENTER)  # shape (N, 2)

    cx = centers[:, 0]
    cy = centers[:, 1]

    if filter_by_center:
        mask = (cx >= x_min) & (cx <= x_max) & (cy >= y_min) & (cy <= y_max)
    else:
        # Example: keep boxes whose top-left corner is inside ROI
        x1 = detections.xyxy[:, 0]
        y1 = detections.xyxy[:, 1]
        mask = (x1 >= x_min) & (x1 <= x_max) & (y1 >= y_min) & (y1 <= y_max)

    return detections[mask]


# ============================================================
# MAIN
# ============================================================

def main():
    args = parse_args()

    model_path = args.model
    video_path = args.video
    conf = float(args.conf)

    if not os.path.isfile(model_path):
        print(f"ERROR: model not found: {model_path}", file=sys.stderr)
        sys.exit(1)
    if not os.path.isfile(video_path):
        print(f"ERROR: video/image not found: {video_path}", file=sys.stderr)
        sys.exit(1)

    # Load YOLO model
    model = YOLO(model_path)

    # Optional: prepare class ID filter based on class names
    selected_class_ids = None
    if USE_CLASS_FILTER:
        # model.model.names: dict {class_id: class_name}
        class_names_dict = model.model.names
        # invert dict to {class_name: class_id}
        name_to_id = {v: k for k, v in class_names_dict.items()}
        selected_class_ids = []
        for cname in SELECTED_CLASS_NAMES:
            if cname in name_to_id:
                selected_class_ids.append(name_to_id[cname])
            else:
                print(f"WARNING: class name '{cname}' not found in model.model.names", file=sys.stderr)

    # Open video (or image as a 1-frame video)
    cap = cv2.VideoCapture(video_path)
    if not cap.isOpened():
        print(f"ERROR: could not open media: {video_path}", file=sys.stderr)
        sys.exit(1)

    fps = cap.get(cv2.CAP_PROP_FPS)
    if fps <= 0:
        fps = BYTE_FRAME_RATE_FALLBACK  # fallback if FPS is not available

    # Initialize ByteTrack tracker with the SAME pattern you used in your example
    byte_tracker = sv.ByteTrack(
        track_activation_threshold=BYTE_TRACK_ACTIVATION_THRESHOLD,
        lost_track_buffer=BYTE_LOST_TRACK_BUFFER,
        minimum_matching_threshold=BYTE_MIN_MATCHING_THRESHOLD,
        frame_rate=fps,
        minimum_consecutive_frames=BYTE_MIN_CONSECUTIVE_FRAMES,
    )
    byte_tracker.reset()

    # (Optional) You COULD also create a LineZone like in your example
    # and annotate it, but for agora vamos manter só tracking + boxes
    #
    # Example if you quiser ativar no futuro:
    # LINE_START = sv.Point(0, 800)
    # LINE_END = sv.Point(1080, 750)
    # line_zone = sv.LineZone(start=LINE_START, end=LINE_END)
    # line_zone_annotator = sv.LineZoneAnnotator(...)

    seen_tracks = set()  # to mark "new" objects
    frame_index = 0

    while True:
        ret, frame = cap.read()
        if not ret:
            break

        frame_index += 1
        t_seconds = frame_index / fps

        # YOLO inference
        results = model(frame, conf=conf, verbose=False)[0]

        # Convert YOLO results to supervision.Detections
        detections = sv.Detections.from_ultralytics(results)

        # Optional: filter by selected class IDs (like only 'Cebola')
        if USE_CLASS_FILTER and selected_class_ids:
            detections = detections[np.isin(detections.class_id, selected_class_ids)]

        # Optional: apply ROI filter
        if USE_ROI:
            detections = apply_roi_filter(detections, ROI_RECT, FILTER_BY_ROI_CENTER)

        # Update tracking with ByteTrack
        detections = byte_tracker.update_with_detections(detections)

        det_list = []

        # Draw boxes on frame for visualization
        annotated_frame = frame.copy()

        for i in range(len(detections)):
            x1, y1, x2, y2 = detections.xyxy[i]
            class_id = int(detections.class_id[i])
            score = float(detections.confidence[i])
            track_id = detections.tracker_id[i]

            if track_id is None:
                track_id_int = -1
            else:
                track_id_int = int(track_id)

            w = float(x2 - x1)
            h = float(y2 - y1)

            # Mark "is_new" when we see a new track_id for the first time
            is_new = False
            if track_id_int >= 0 and track_id_int not in seen_tracks:
                seen_tracks.add(track_id_int)
                is_new = True

            # Draw rectangle and label on frame
            cv2.rectangle(
                annotated_frame,
                (int(x1), int(y1)),
                (int(x2), int(y2)),
                (0, 255, 0),
                2,
            )
            label = f"#{track_id_int} C{class_id} {score:.2f}"
            cv2.putText(
                annotated_frame,
                label,
                (int(x1), int(y1) - 5),
                cv2.FONT_HERSHEY_SIMPLEX,
                0.5,
                (0, 255, 0),
                1,
                cv2.LINE_AA,
            )

            # Append detection info to list (will be sent to C#)
            det_list.append(
                {
                    "track_id": track_id_int,
                    "class_id": class_id,
                    "score": score,
                    "x": float(x1),
                    "y": float(y1),
                    "w": w,
                    "h": h,
                    "is_new": bool(is_new),
                }
            )

        # Encode annotated frame as base64
        img_b64 = frame_to_base64_bgr(annotated_frame)

        # Build JSON object for this frame
        out_obj = {
            "frame_index": frame_index,
            "time_seconds": float(t_seconds),
            "detections": det_list,
            "image": img_b64,
        }

        # Print one JSON per line (C# reads line by line)
        print(json.dumps(out_obj), flush=True)

    cap.release()


if __name__ == "__main__":
    main()
