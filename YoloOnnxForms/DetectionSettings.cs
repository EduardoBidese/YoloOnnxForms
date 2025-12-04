using System.Collections.Generic;

namespace YoloOnnxForms
{
    /// <summary>
    /// Configurações gerais de detecção e contagem.
    /// Será serializada em JSON.
    /// </summary>
    public class DetectionSettings
    {
        public float Confidence { get; set; } = 0.60f;        // confiança mínima do YOLO
        public float ByteTrackActivation { get; set; } = 0.35f;
        public int LostTrackBuffer { get; set; } = 30;
        public float MatchingThreshold { get; set; } = 0.60f;
        public int MinConsecutiveFrames { get; set; } = 5;
        public bool UseClassFilter { get; set; } = false;

        // Lista de classes configuradas
        public List<ClassConfig> Classes { get; set; } = new List<ClassConfig>();
    }

    /// <summary>
    /// Configuração de cada classe: id, nome e se conta como defeito.
    /// </summary>
    public class ClassConfig
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public bool CountAsDefect { get; set; } = false;
    }
}
