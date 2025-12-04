# -*- coding: utf-8 -*-
"""
process_video_stream.py

- Carrega YOLO (Ultralytics)
- Roda detecção + ByteTrack frame a frame
- Desenha boxes
- Envia 1 JSON por frame para o C#:
    frame_index, time_seconds, detections[], image (JPG base64)

Integração com C#:
- Recebe --settings apontando para detection_settings.json
- Usa os parâmetros do JSON para ajuste de confiança, ByteTrack e filtro de classes
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
# VALORES PADRÃO (fallback se não vier do JSON)
# ============================================================

DEFAULT_CONF = 0.40  # confiança mínima padrão do YOLO

BYTE_TRACK_ACTIVATION_THRESHOLD = 0.40   # confiança min para iniciar um track
BYTE_LOST_TRACK_BUFFER = 15              # frames que um track "perdido" é mantido
BYTE_MIN_MATCHING_THRESHOLD = 0.90       # quão exigente é a associação de tracks (0-1)
BYTE_MIN_CONSECUTIVE_FRAMES = 10         # frames min para considerar track estável
BYTE_FRAME_RATE_FALLBACK = 30.0          # FPS padrão se o vídeo/câmera não informar

USE_ROI = False
ROI_RECT = (100, 100, 500, 400)
FILTER_BY_ROI_CENTER = True

# Filtro de classes "hardcoded" (usado só se não vier nada do JSON)
USE_CLASS_FILTER = False
SELECTED_CLASS_NAMES = ["Cebola"]


# ============================================================
# HELPERS
# ============================================================

def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument("--model", type=str, required=True, help="Path to YOLO .pt model")
    parser.add_argument("--video", type=str, required=True, help="Path to video path or camera index (0,1,2...)")
    parser.add_argument("--conf", type=float, default=DEFAULT_CONF, help="YOLO confidence threshold")
    parser.add_argument("--settings", type=str, default=None, help="Path to detection_settings.json")
    return parser.parse_args()


def load_settings(settings_path: str | None):
    """
    Lê o JSON de configuração gerado pelo C#.
    Retorna um dict ou None se não existir / erro.
    """
    if not settings_path:
        return None

    try:
        if not os.path.isfile(settings_path):
            print(f"[WARN] settings file not found: {settings_path}", file=sys.stderr)
            return None

        with open(settings_path, "r", encoding="utf-8") as f:
            data = json.load(f)

        print(f"[DEBUG] Loaded settings from {settings_path}", file=sys.stderr)
        return data
    except Exception as e:
        print(f"[WARN] failed to load settings: {e}", file=sys.stderr)
        return None


def frame_to_base64_bgr(frame):
    """Converte frame BGR (OpenCV) para JPG base64 (string)."""
    ret, buf = cv2.imencode(".jpg", frame)
    if not ret:
        raise RuntimeError("Could not encode frame to JPG")
    return base64.b64encode(buf).decode("ascii")


def apply_roi_filter(detections, roi_rect, filter_by_center=True):
    """Filtra detecções por um retângulo ROI."""
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
    video_arg = args.video      # pode ser caminho ou índice da câmera (texto "0","1"...)
    conf = float(args.conf)     # valor default da linha de comando

    print(f"[DEBUG] model_path = {model_path}", file=sys.stderr, flush=True)
    print(f"[DEBUG] video_arg = {video_arg}", file=sys.stderr, flush=True)

    # --------------------------------------------------------
    # 1) Carrega JSON de configuração (se tiver)
    # --------------------------------------------------------
    settings = load_settings(args.settings)

    # valores que podem ser sobrescritos pelas configs
    bt_activation = BYTE_TRACK_ACTIVATION_THRESHOLD
    bt_lost_buffer = BYTE_LOST_TRACK_BUFFER
    bt_matching = BYTE_MIN_MATCHING_THRESHOLD
    bt_min_frames = BYTE_MIN_CONSECUTIVE_FRAMES

    selected_class_ids_from_settings = None

    if settings is not None:
        # confiança do YOLO
        try:
            conf = float(settings.get("Confidence", conf))
        except Exception:
            pass

        # ByteTrack
        try:
            bt_activation = float(settings.get("ByteTrackActivation", bt_activation))
        except Exception:
            pass

        try:
            bt_lost_buffer = int(settings.get("LostTrackBuffer", bt_lost_buffer))
        except Exception:
            pass

        try:
            bt_matching = float(settings.get("MatchingThreshold", bt_matching))
        except Exception:
            pass

        try:
            bt_min_frames = int(settings.get("MinConsecutiveFrames", bt_min_frames))
        except Exception:
            pass

        # filtro de classes por ID vindo da tela de configuração
        try:
            use_filter = bool(settings.get("UseClassFilter", False))
            if use_filter:
                classes_cfg = settings.get("Classes") or []
                selected_class_ids_from_settings = [
                    int(c["Id"])
                    for c in classes_cfg
                    if "Id" in c and str(c.get("Name", "")).strip() != ""
                ]
                print(f"[DEBUG] Class filter ON (JSON). IDs: {selected_class_ids_from_settings}", file=sys.stderr)
        except Exception as e:
            print(f"[WARN] error reading class filter from settings: {e}", file=sys.stderr)

    print(f"[DEBUG] Using conf={conf}", file=sys.stderr)
    print(
        f"[DEBUG] ByteTrack: act={bt_activation}, lost_buf={bt_lost_buffer}, "
        f"match={bt_matching}, min_frames={bt_min_frames}",
        file=sys.stderr,
    )

    # --------------------------------------------------------
    # 2) Verifica modelo
    # --------------------------------------------------------
    if not os.path.isfile(model_path):
        print(f"ERROR: model not found: {model_path}", file=sys.stderr)
        sys.exit(1)

    # --------------------------------------------------------
    # 3) Decide se é ARQUIVO ou CÂMERA
    # --------------------------------------------------------
    use_camera = False
    cam_index = 0
    video_path = video_arg

    if video_arg.isdigit():
        use_camera = True
        cam_index = int(video_arg)
        print(f"[DEBUG] Using CAMERA index {cam_index}", file=sys.stderr, flush=True)
    else:
        if not os.path.isfile(video_path):
            print(f"ERROR: video/image not found: {video_path}", file=sys.stderr)
            sys.exit(1)
        print(f"[DEBUG] Using VIDEO file {video_path}", file=sys.stderr, flush=True)

    # --------------------------------------------------------
    # 4) Carrega modelo YOLO
    # --------------------------------------------------------
    print("[DEBUG] Loading YOLO model...", file=sys.stderr, flush=True)
    model = YOLO(model_path)
    print("[DEBUG] YOLO model loaded.", file=sys.stderr, flush=True)

    # Filtro de classes baseado em NOME (fallback, se nao tiver nada no JSON)
    selected_class_ids_from_names = None
    if settings is None and USE_CLASS_FILTER:
        class_names_dict = model.model.names
        name_to_id = {v: k for k, v in class_names_dict.items()}
        tmp_ids = []
        for cname in SELECTED_CLASS_NAMES:
            if cname in name_to_id:
                tmp_ids.append(name_to_id[cname])
            else:
                print(f"WARNING: class name '{cname}' not found in model.model.names", file=sys.stderr)
        if tmp_ids:
            selected_class_ids_from_names = tmp_ids
            print(f"[DEBUG] Class filter ON (hardcoded names). IDs: {tmp_ids}", file=sys.stderr)

    # --------------------------------------------------------
    # 5) Abre vídeo ou câmera
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
    # 6) Cria ByteTrack
    # --------------------------------------------------------
    byte_tracker = sv.ByteTrack(
        track_activation_threshold=bt_activation,
        lost_track_buffer=bt_lost_buffer,
        minimum_matching_threshold=bt_matching,
        frame_rate=fps,
        minimum_consecutive_frames=bt_min_frames,
    )
    byte_tracker.reset()
    print("[DEBUG] ByteTrack created.", file=sys.stderr, flush=True)

    seen_tracks = set()
    frame_index = 0

    # --------------------------------------------------------
    # 7) Loop de frames
    # --------------------------------------------------------
    while True:
        ret, frame = cap.read()
        if not ret:
            print("[DEBUG] cap.read() returned False. Ending loop.", file=sys.stderr, flush=True)
            break

        frame_index += 1
        t_seconds = frame_index / fps

        # ---------- YOLO ----------
        results = model(frame, conf=conf, verbose=False)[0]
        detections = sv.Detections.from_ultralytics(results)

        # filtro de classes (prioridade: JSON -> hardcoded -> nenhum)
        if selected_class_ids_from_settings is not None:
            detections = detections[np.isin(detections.class_id, selected_class_ids_from_settings)]
        elif selected_class_ids_from_names is not None:
            detections = detections[np.isin(detections.class_id, selected_class_ids_from_names)]

        # ROI opcional
        if USE_ROI:
            detections = apply_roi_filter(detections, ROI_RECT, FILTER_BY_ROI_CENTER)

        # ---------- Tracking ----------
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

            # novo ID?
            is_new = False
            if track_id_int >= 0 and track_id_int not in seen_tracks:
                seen_tracks.add(track_id_int)
                is_new = True

            # desenha
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

        # ---------- Envia JSON do frame ----------
        img_b64 = frame_to_base64_bgr(annotated_frame)
        out_obj = {
            "frame_index": frame_index,
            "time_seconds": float(t_seconds),
            "detections": det_list,
            "image": img_b64,
        }
        print(json.dumps(out_obj), flush=True)

    cap.release()

    # para vídeo, se não leu nada, algo deu errado
    if not use_camera and frame_index == 0:
        print("ERROR: no frames were read from the video.", file=sys.stderr)
        sys.exit(1)
    else:
        print(f"[DEBUG] Finished. Total frames processed = {frame_index}", file=sys.stderr, flush=True)


if __name__ == "__main__":
    main()
