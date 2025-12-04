    using System;
    using System.IO;
    using System.Text.Json;

    public static class ConfigService
    {
        private static readonly string ConfigFileName = "config.json";

        // Caminho completo do arquivo de config (na pasta do executável)
        private static string GetConfigPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, ConfigFileName);
        }

        public static AppConfig Load()
        {
            string path = GetConfigPath();

            if (!File.Exists(path))
            {
                // Se não existir, cria uma config padrão
                var defaultConfig = GetDefaultConfig();
                Save(defaultConfig);
                return defaultConfig;
            }

            string json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<AppConfig>(json);

            // Fallback se der algum problema na desserialização
            return config ?? GetDefaultConfig();
        }

        public static void Save(AppConfig config)
        {
            string path = GetConfigPath();
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(path, json);
        }

        private static AppConfig GetDefaultConfig()
        {
            return new AppConfig
            {
                CaminhoModelo = @"C:\Modelos\meu_modelo.onnx",
                CaminhoPastaLogCsv = @"C:\LogsProducao",
                CaminhoPastaFramesReprovados = @"C:\LogsProducao\Reprovados",
                IndiceCamera = 0,
                LimiarConfianca = 0.5,
                LimiarIoU = 0.45,
                HabilitarContagemPorClasse = true,
                ClassesMonitoradas = new List<string> { "PecaA", "PecaB" }
            };
        }
    }
