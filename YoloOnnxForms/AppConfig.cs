
    public class AppConfig
    {
        public string CaminhoModelo { get; set; }
        public string CaminhoPastaLogCsv { get; set; }
        public string CaminhoPastaFramesReprovados { get; set; }
        public int IndiceCamera { get; set; }
        public double LimiarConfianca { get; set; }
        public double LimiarIoU { get; set; }
        public bool HabilitarContagemPorClasse { get; set; }
        public List<string> ClassesMonitoradas { get; set; } = new();
    }


