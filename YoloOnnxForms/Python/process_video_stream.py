# -*- coding: utf-8 -*-
"""
process_video_stream.py
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
# CONFIG
# ============================================================

DEFAULT_CONF = 0.40              # confiança mínima padrão do YOLO (filtro inicial das detecções)

BYTE_TRACK_ACTIVATION_THRESHOLD = 0.4   # confiança mínima para o ByteTrack iniciar um novo ID (track)
BYTE_LOST_TRACK_BUFFER = 15             # nº de frames que um ID “perdido” é mantido antes de ser apagado
BYTE_MIN_MATCHING_THRESHOLD = 0.90       # quão exigente é para associar detecção a um ID existente (0–1)
BYTE_MIN_CONSECUTIVE_FRAMES = 10         # nº mínimo de frames seguidos para considerar o ID estável/válido
BYTE_FRAME_RATE_FALLBACK = 30.0          # FPS assumido quando o vídeo não informa FPS (fallback p/ ByteTrack)

USE_ROI = False
ROI_RECT = (100, 100, 500, 400)
FILTER_BY_ROI_CENTER = True

USE_CLASS_FILTER = False
SELECTED_CLASS_NAMES = ["Cebola"]


# ============================================================
# HELPERS
# ============================================================

def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument("--model", type=str, required=True, help="Path to YOLO .pt model")
    parser.add_argument("--video", type=str, required=True, help="Path to video or image")
    parser.add_argument("--conf", type=float, default=DEFAULT_CONF, help="YOLO confidence threshold")
    return parser.parse_args()


def frame_to_base64_bgr(frame):
    ret, buf = cv2.imencode(".jpg", frame)
    if not ret:
        raise RuntimeError("Could not encode frame to JPG")
    return base64.b64encode(buf).decode("ascii")


def apply_roi_filter(detections, roi_rect, filter_by_center=True):
    if detections is None or len(detections) == 0:
        return detections

    x_min, y_min, x_max, y_max = roi_rect

    centers = detections.get_anchors_coordinates(sv.Position.CENTER)
    cx = centers[:, 0]
    cy = centers[:, 1]

    if filter_by_center:
        mask = (cx >= x_min) & (cx <= x_max) & (cy >= y_min) & (cy <= y_max)
    else:
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
    video_arg = args.video  # pode ser caminho de arquivo ou índice de câmera em texto
    conf = float(args.conf)

    # DEBUG
    print(f"[DEBUG] model_path = {model_path}", file=sys.stderr, flush=True)
    print(f"[DEBUG] video_arg = {video_arg}", file=sys.stderr, flush=True)

    # --------------------------------------------------------
    # 1) Verifica modelo
    # --------------------------------------------------------
    if not os.path.isfile(model_path):
        print(f"ERROR: model not found: {model_path}", file=sys.stderr)
        sys.exit(1)

    # --------------------------------------------------------
    # 2) Decide se é ARQUIVO ou CÂMERA
    #    - se video_arg for '0', '1', '2' etc. -> câmera
    #    - caso contrário, trata como caminho de arquivo
    # --------------------------------------------------------
    use_camera = False
    cam_index = 0
    video_path = video_arg

    if video_arg.isdigit():
        use_camera = True
        cam_index = int(video_arg)
        print(f"[DEBUG] Using CAMERA index {cam_index}", file=sys.stderr, flush=True)
    else:
        # arquivo normal
        if not os.path.isfile(video_path):
            print(f"ERROR: video/image not found: {video_path}", file=sys.stderr)
            sys.exit(1)
        print(f"[DEBUG] Using VIDEO file {video_path}", file=sys.stderr, flush=True)

    # --------------------------------------------------------
    # 3) Carrega modelo YOLO
    # --------------------------------------------------------
    print("[DEBUG] Loading YOLO model...", file=sys.stderr, flush=True)
    model = YOLO(model_path)
    print("[DEBUG] YOLO model loaded.", file=sys.stderr, flush=True)

    # filtro opcional de classes (se você estiver usando USE_CLASS_FILTER etc.)
    selected_class_ids = None
    if USE_CLASS_FILTER:
        class_names_dict = model.model.names
        name_to_id = {v: k for k, v in class_names_dict.items()}
        selected_class_ids = []
        for cname in SELECTED_CLASS_NAMES:
            if cname in name_to_id:
                selected_class_ids.append(name_to_id[cname])
            else:
                print(f"WARNING: class name '{cname}' not found in model.model.names", file=sys.stderr)

    # --------------------------------------------------------
    # 4) Abre vídeo ou câmera
    # --------------------------------------------------------
    if use_camera:
        cap = cv2.VideoCapture(cam_index)
    else:
        cap = cv2.VideoCapture(video_path)

    if not cap.isOpened():
        print("ERROR: could not open media (video/camera).", file=sys.stderr)
        sys.exit(1)

    fps = cap.get(cv2.CAP_PROP_FPS)
    print(f"[DEBUG] CAP opened. FPS reported = {fps}", file=sys.stderr, flush=True)
    if fps <= 0:
        fps = BYTE_FRAME_RATE_FALLBACK
        print(f"[DEBUG] FPS fallback used: {fps}", file=sys.stderr, flush=True)

    # --------------------------------------------------------
    # 5) Cria ByteTrack
    # --------------------------------------------------------
    byte_tracker = sv.ByteTrack(
        track_activation_threshold=BYTE_TRACK_ACTIVATION_THRESHOLD,
        lost_track_buffer=BYTE_LOST_TRACK_BUFFER,
        minimum_matching_threshold=BYTE_MIN_MATCHING_THRESHOLD,
        frame_rate=fps,
        minimum_consecutive_frames=BYTE_MIN_CONSECUTIVE_FRAMES,
    )
    byte_tracker.reset()
    print("[DEBUG] ByteTrack created.", file=sys.stderr, flush=True)

    seen_tracks = set()
    frame_index = 0

    # --------------------------------------------------------
    # 6) Loop de frames (arquivo ou câmera, é igual)
    # --------------------------------------------------------
    while True:
        ret, frame = cap.read()
        if not ret:
            print("[DEBUG] cap.read() returned False. Ending loop.", file=sys.stderr, flush=True)
            break

        frame_index += 1
        t_seconds = frame_index / fps

        # YOLO
        results = model(frame, conf=conf, verbose=False)[0]
        detections = sv.Detections.from_ultralytics(results)

        # filtro de classes, se estiver ligado
        if USE_CLASS_FILTER and selected_class_ids:
            detections = detections[np.isin(detections.class_id, selected_class_ids)]

        # ROI opcional
        if USE_ROI:
            detections = apply_roi_filter(detections, ROI_RECT, FILTER_BY_ROI_CENTER)

        # tracking
        detections = byte_tracker.update_with_detections(detections)

        det_list = []
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

            is_new = False
            if track_id_int >= 0 and track_id_int not in seen_tracks:
                seen_tracks.add(track_id_int)
                is_new = True

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

        # Encode frame + manda JSON para o C#
        img_b64 = frame_to_base64_bgr(annotated_frame)
        out_obj = {
            "frame_index": frame_index,
            "time_seconds": float(t_seconds),
            "detections": det_list,
            "image": img_b64,
        }
        print(json.dumps(out_obj), flush=True)

    cap.release()

    # para câmera, é normal sair com frame_index grande;
    # para vídeo, se frame_index == 0, algo deu errado
    if not use_camera and frame_index == 0:
        print("ERROR: no frames were read from the video.", file=sys.stderr)
        sys.exit(1)
    else:
        print(f"[DEBUG] Finished. Total frames processed = {frame_index}", file=sys.stderr, flush=True)


if __name__ == "__main__":
    main()
