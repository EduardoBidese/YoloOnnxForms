using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace YoloOnnxForms
{
    public class PythonDetectorService
    {
        private readonly string _pythonExePath;
        private readonly string _scriptPath;
        private readonly string _modelPath;

        public PythonDetectorService(string pythonExePath, string scriptPath, string modelPath)
        {
            _pythonExePath = pythonExePath; // ex: C:\Python311\python.exe
            _scriptPath = scriptPath;       // ex: C:\ICAL\python\process_video.py
            _modelPath = modelPath;         // ex: C:\...\best.pt
        }

        public List<DetectionResult> ProcessVideo(string videoPath, float confThreshold)
        {
            // arquivo temporário para o JSON
            string tempJson = Path.GetTempFileName();

            var psi = new ProcessStartInfo
            {
                FileName = _pythonExePath,
                Arguments = $"\"{_scriptPath}\" " +
                            $"--model \"{_modelPath}\" " +
                            $"--video \"{videoPath}\" " +
                            $"--conf {confThreshold.ToString(CultureInfo.InvariantCulture)} " +
                            $"--output \"{tempJson}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var proc = Process.Start(psi))
            {
                if (proc == null)
                    throw new Exception("Não foi possível iniciar o processo Python.");

                string stdout = proc.StandardOutput.ReadToEnd();
                string stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode != 0)
                {
                    throw new Exception(
                        $"Python retornou código {proc.ExitCode}.\nSTDOUT:\n{stdout}\n\nSTDERR:\n{stderr}");
                }
            }

            // lê o JSON gerado pelo Python
            var json = File.ReadAllText(tempJson);
            var results = JsonSerializer.Deserialize<List<DetectionResultDto>>(json);

            var list = new List<DetectionResult>();
            if (results == null)
                return list;

            foreach (var r in results)
            {
                list.Add(new DetectionResult
                {
                    FrameIndex = r.FrameIndex,
                    Timestamp = TimeSpan.FromSeconds(r.TimeSeconds),
                    TrackId = r.TrackId,
                    ClassId = r.ClassId,
                    Score = r.Score,
                    Box = new System.Drawing.RectangleF(r.X, r.Y, r.W, r.H),
                    IsNewObject = r.IsNewObject
                });
            }

            return list;
        }

        // DTO que mapeia o JSON gerado pelo Python
        private class DetectionResultDto
        {
            public int FrameIndex { get; set; }
            public double TimeSeconds { get; set; }
            public int TrackId { get; set; }
            public int ClassId { get; set; }
            public float Score { get; set; }
            public float X { get; set; }
            public float Y { get; set; }
            public float W { get; set; }
            public float H { get; set; }
            public bool IsNewObject { get; set; }
        }
    }
}
