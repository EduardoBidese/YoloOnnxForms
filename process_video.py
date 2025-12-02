import argparse
import json

import cv2
from ultralytics import YOLO
import supervision as sv


def process_video(model_path, video_path, conf, output_path):
    model = YOLO(model_path)
    tracker = sv.ByteTrack()

    cap = cv2.VideoCapture(video_path)
    fps = cap.get(cv2.CAP_PROP_FPS)
    if fps <= 0:
        fps = 30.0

    frame_idx = 0

    seen = set()  # (class_id, track_id)
    results = []

    while True:
        ret, frame = cap.read()
        if not ret:
            break

        frame_idx += 1

        yolo_results = model(frame, conf=conf, verbose=False)[0]
        detections = sv.Detections.from_ultralytics(yolo_results)
        tracked = tracker.update_with_detections(detections)

        time_sec = (frame_idx - 1) / fps

        for i in range(len(tracked)):
            class_id = int(tracked.class_id[i])
            score = float(tracked.confidence[i])
            track_id = tracked.tracker_id[i]

            if track_id is None:
                continue
            track_id = int(track_id)

            x1, y1, x2, y2 = tracked.xyxy[i]
            w = float(x2 - x1)
            h = float(y2 - y1)

            key = (class_id, track_id)
            is_new = key not in seen
            if is_new:
                seen.add(key)

            results.append({
                "FrameIndex": frame_idx,
                "TimeSeconds": float(time_sec),
                "TrackId": track_id,
                "ClassId": class_id,
                "Score": score,
                "X": float(x1),
                "Y": float(y1),
                "W": w,
                "H": h,
                "IsNewObject": is_new,
            })

    cap.release()

    with open(output_path, "w", encoding="utf-8") as f:
        json.dump(results, f, ensure_ascii=False, indent=2)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--model", required=True, help="Caminho para best.pt")
    parser.add_argument("--video", required=True, help="Caminho do vídeo")
    parser.add_argument("--conf", type=float, default=0.25)
    parser.add_argument("--output", required=True, help="Caminho do JSON de saída")
    args = parser.parse_args()

    process_video(args.model, args.video, args.conf, args.output)
