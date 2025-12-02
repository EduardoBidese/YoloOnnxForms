using System;
using System.Drawing;

namespace YoloOnnxForms
{
    public class DetectionResult
    {
        public int FrameIndex { get; set; }
        public TimeSpan Timestamp { get; set; }
        public int TrackId { get; set; }
        public int ClassId { get; set; }
        public float Score { get; set; }
        public RectangleF Box { get; set; }
        public bool IsNewObject { get; set; }
    }
}
