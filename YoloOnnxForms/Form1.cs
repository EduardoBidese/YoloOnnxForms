using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace YoloOnnxForms
{
    public partial class Form1 : Form
    {
        // ========= CONFIGURAÇÃO DO PYTHON =========

        private const string PythonExePath =
            @"C:\Users\epuhl\anaconda3\envs\env-cuda\python.exe";  // AJUSTAR SE PRECISAR

        private const string PythonScriptPath =
            @"C:\Users\epuhl\source\repos\YoloOnnxForms\YoloOnnxForms\Python\process_video_stream.py";  // AJUSTAR

        private const string YoloPtModelPath =
            @"C:\Users\epuhl\source\repos\YoloOnnxForms\YoloOnnxForms\Models\best.pt";  // AJUSTAR

        // Processo Python em execução (vídeo ou câmera)
        private Process? _pythonProcess;
        private bool _cameraRunning = false;     // se true, botão "Abrir Câmera" passa a "Fechar Câmera"
        private bool _cancelRequested = false;   // flag para cancelar leitura do streaming

        // LOG EM CSV
        private string? _currentLogFilePath;     // caminho do CSV atual
        private readonly object _logLock = new(); // lock para escrita no CSV
        private string _currentSourceLabel = ""; // ex: "video:meu_video.mp4" ou "camera:0"

        // Pasta base onde serão salvos os logs (configurável pelo botão btnPastaArquivoLog)
        private string _logsDirectory;

        // CONTADORES PARA O RESUMO FINAL
        private readonly Dictionary<int, int> _classCounts = new(); // class_id -> quantidade
        private int _totalPieces = 0;

        public Form1()
        {
            InitializeComponent();

            // Pasta padrão: subpasta "Logs" ao lado do executável
            _logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            Directory.CreateDirectory(_logsDirectory);

            PreencherListaCameras();

            textBoxConf.Text = "0.60"; // confiança padrão
            lblStatus.Text = "Pronto. Clique em 'Carregar Arquivo' ou 'Abrir Câmera'.";
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        // =========================================================
        // SOBRESCREVE FECHAMENTO DO FORM → GARANTE QUE FECHA PYTHON/CÂMERA
        // =========================================================
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            FecharProcessoPython();
            _cameraRunning = false;

            base.OnFormClosing(e);
        }

        // =========================================================
        // PREENCHE COMBO DE CÂMERAS (Câmera 0, 1, 2 ...)
        // =========================================================
        private void PreencherListaCameras()
        {
            comboBoxCamera.Items.Clear();

            comboBoxCamera.Items.Add(new CameraItem { Index = 0, Name = "Câmera 0" });
            comboBoxCamera.Items.Add(new CameraItem { Index = 1, Name = "Câmera 1" });
            comboBoxCamera.Items.Add(new CameraItem { Index = 2, Name = "Câmera 2" });

            comboBoxCamera.SelectedIndex = 0;
        }

        // =========================================================
        // BOTÃO: ESCOLHER PASTA DE LOG (btnPastaArquivoLog)
        // =========================================================
        private void btnPastaArquivoLog_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Selecione a pasta onde os arquivos de log CSV serão salvos";
                fbd.SelectedPath = _logsDirectory;

                if (fbd.ShowDialog() == DialogResult.OK &&
                    !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    _logsDirectory = fbd.SelectedPath;
                    try
                    {
                        Directory.CreateDirectory(_logsDirectory);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            "Não foi possível criar/verificar a pasta selecionada:\n" + ex.Message,
                            "Erro na pasta de logs",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }

                    lblStatus.Text = $"Pasta de logs definida: {_logsDirectory}";
                }
            }
        }

        // =========================================================
        // 1) CARREGAR MÍDIA (IMAGEM/VÍDEO) E PROCESSAR EM STREAMING
        // =========================================================
        private async void btnLoadImage_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter =
                    "Imagens e Vídeos|*.jpg;*.jpeg;*.png;*.bmp;*.mp4;*.avi;*.mov;*.mkv|" +
                    "Somente Imagens|*.jpg;*.jpeg;*.png;*.bmp|" +
                    "Somente Vídeos|*.mp4;*.avi;*.mov;*.mkv";

                if (ofd.ShowDialog() != DialogResult.OK)
                    return;

                string mediaPath = ofd.FileName;
                float conf = LerConfiancaDaTextBox();

                FecharProcessoPython();
                _cameraRunning = false;
                btnOpenCamera.Text = "Abrir Câmera";

                _currentSourceLabel = $"video:{Path.GetFileName(mediaPath)}";
                IniciarNovoLogCsv(_currentSourceLabel);

                btnLoadImage.Enabled = false;
                btnOpenCamera.Enabled = false;

                lbResults.Items.Clear();
                pictureBox1.Image = null;
                lblStatus.Text = "Iniciando Python (streaming)...";

                try
                {
                    await Task.Run(() => RodarPythonStreaming(mediaPath, conf));

                    EscreverResumoCsv();

                    lblStatus.Text = "Processamento finalizado.";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erro ao processar mídia via Python (streaming):\n" + ex.Message);
                    lblStatus.Text = "Erro no processamento.";
                }
                finally
                {
                    btnLoadImage.Enabled = true;
                    btnOpenCamera.Enabled = true;
                }
            }
        }

        // =========================================================
        // 2) BOTÃO ABRIR/FECHAR CÂMERA (TOGGLE)
        // =========================================================
        private async void btnOpenCamera_Click(object sender, EventArgs e)
        {
            if (_cameraRunning)
            {
                FecharProcessoPython();
                _cameraRunning = false;

                EscreverResumoCsv();

                btnOpenCamera.Text = "Abrir Câmera";
                lblStatus.Text = "Câmera encerrada.";
                btnLoadImage.Enabled = true;
                return;
            }

            float conf = LerConfiancaDaTextBox();

            int camIndex = 0;
            if (comboBoxCamera.SelectedItem is CameraItem camItem)
            {
                camIndex = camItem.Index;
            }
            else
            {
                int.TryParse(comboBoxCamera.Text, out camIndex);
            }

            FecharProcessoPython();

            _currentSourceLabel = $"camera:{camIndex}";
            IniciarNovoLogCsv(_currentSourceLabel);

            _cameraRunning = true;
            btnOpenCamera.Text = "Fechar Câmera";
            btnLoadImage.Enabled = false;

            lbResults.Items.Clear();
            pictureBox1.Image = null;
            lblStatus.Text = $"Abrindo câmera {camIndex} e iniciando streaming...";

            try
            {
                await Task.Run(() => RodarPythonStreaming(camIndex.ToString(), conf));

                if (_cameraRunning)
                {
                    _cameraRunning = false;
                    btnOpenCamera.Text = "Abrir Câmera";
                    btnLoadImage.Enabled = true;
                    lblStatus.Text = "Captura da câmera finalizada.";

                    EscreverResumoCsv();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro na captura da câmera:\n" + ex.Message);
                lblStatus.Text = "Erro na câmera.";
                _cameraRunning = false;
                btnOpenCamera.Text = "Abrir Câmera";
                btnLoadImage.Enabled = true;

                EscreverResumoCsv();
            }
        }

        // =========================================================
        // 3) CORE: INICIA PYTHON E STREAMA FRAME A FRAME
        // =========================================================
        private void RodarPythonStreaming(string mediaPath, float conf)
        {
            _cancelRequested = false;

            var psi = new ProcessStartInfo
            {
                FileName = PythonExePath,
                Arguments =
                    $"-u \"{PythonScriptPath}\" " +
                    $"--model \"{YoloPtModelPath}\" " +
                    $"--video \"{mediaPath}\" " +
                    $"--conf {conf.ToString(CultureInfo.InvariantCulture)}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            var proc = new Process { StartInfo = psi };
            _pythonProcess = proc;

            try
            {
                proc.Start();
            }
            catch (Exception ex)
            {
                _pythonProcess = null;
                throw new Exception("Não foi possível iniciar o processo Python:\n" + ex.Message);
            }

            try
            {
                string? line;

                while (!_cancelRequested && (line = proc.StandardOutput.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    FrameResult? frame;
                    try
                    {
                        frame = JsonSerializer.Deserialize<FrameResult>(line);
                    }
                    catch
                    {
                        continue;
                    }

                    if (frame == null)
                        continue;

                    Bitmap? bmp = null;
                    try
                    {
                        byte[] bytes = Convert.FromBase64String(frame.image);
                        using (var ms = new MemoryStream(bytes))
                        {
                            bmp = new Bitmap(ms);
                        }
                    }
                    catch
                    {
                    }

                    if (bmp != null)
                    {
                        if (IsDisposed) break;

                        Invoke(new Action(() =>
                        {
                            if (IsDisposed) return;

                            pictureBox1.Image?.Dispose();
                            pictureBox1.Image = (Bitmap)bmp.Clone();
                        }));
                        bmp.Dispose();
                    }

                    if (IsDisposed) break;

                    Invoke(new Action(() =>
                    {
                        if (IsDisposed) return;

                        foreach (var d in frame.detections)
                        {
                            if (!d.is_new)
                                continue;

                            string linha =
                                $"[t={frame.time_seconds:0.0}s F{frame.frame_index}] " +
                                $"Track {d.track_id} | Classe {d.class_id} | Score {d.score:0.00}";

                            lbResults.Items.Insert(0, linha);

                            RegistrarDeteccaoCsv(_currentSourceLabel, frame, d);
                        }

                        if (frame.detections != null && frame.detections.Count > 0)
                        {
                            var partes = new List<string>();
                            foreach (var d in frame.detections)
                            {
                                partes.Add($"T{d.track_id}-C{d.class_id}({d.score:0.00})");
                            }

                            string resumo = string.Join("  ", partes);
                            lblStatus.Text =
                                $"Frame {frame.frame_index} (t={frame.time_seconds:0.0}s) | {resumo}";
                        }
                        else
                        {
                            lblStatus.Text =
                                $"Frame {frame.frame_index} (t={frame.time_seconds:0.0}s) | sem detecções";
                        }
                    }));
                }

                if (_cancelRequested)
                {
                    try
                    {
                        if (!proc.HasExited)
                        {
                            proc.Kill();
                            proc.WaitForExit(2000);
                        }
                    }
                    catch { }
                    return;
                }

                string stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode != 0)
                {
                    throw new Exception(
                        $"Python retornou código {proc.ExitCode}.\nSTDERR:\n{stderr}");
                }
            }
            finally
            {
                proc.Dispose();
                _pythonProcess = null;
            }
        }

        // =========================================================
        // 4) FECHA PROCESSO PYTHON (CÂMERA OU VÍDEO)
        // =========================================================
        private void FecharProcessoPython()
        {
            _cancelRequested = true;

            try
            {
                if (_pythonProcess != null && !_pythonProcess.HasExited)
                {
                    _pythonProcess.Kill();
                    _pythonProcess.WaitForExit(2000);
                }
            }
            catch
            {
            }
            finally
            {
                _pythonProcess?.Dispose();
                _pythonProcess = null;
            }
        }

        // =========================================================
        // 5) BOTÃO DETECT (ONNX) – APENAS MENSAGEM
        // =========================================================
        private void btnDetect_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "A detecção via ONNX em C# foi desativada.\n\n" +
                "Use apenas 'Carregar Arquivo' ou 'Abrir Câmera' (que também fecha) " +
                "para rodar o modelo em Python (streaming).",
                "Info",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // =========================================================
        // 6) LER CONFIANÇA DA TEXTBOX
        // =========================================================
        private float LerConfiancaDaTextBox()
        {
            float confThreshold = 0.25f;

            try
            {
                if (!string.IsNullOrWhiteSpace(textBoxConf.Text))
                {
                    var s = textBoxConf.Text.Trim().Replace(',', '.');
                    if (!float.TryParse(
                            s,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out confThreshold))
                    {
                        confThreshold = 0.25f;
                    }
                }
            }
            catch
            {
                confThreshold = 0.25f;
            }

            return confThreshold;
        }

        // =========================================================
        // 7) LOG EM CSV + RESUMO FINAL
        // =========================================================

        private void IniciarNovoLogCsv(string sourceLabel)
        {
            try
            {
                _classCounts.Clear();
                _totalPieces = 0;

                Directory.CreateDirectory(_logsDirectory);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"log_{timestamp}.csv";
                _currentLogFilePath = Path.Combine(_logsDirectory, fileName);

                string header =
                    "datetime;source;frame_index;time_seconds;track_id;class_id;score;x;y;w;h;is_new" +
                    Environment.NewLine;

                lock (_logLock)
                {
                    File.WriteAllText(_currentLogFilePath, header, Encoding.UTF8);
                }

                Invoke(new Action(() =>
                {
                    lblStatus.Text = $"Log CSV: {_currentLogFilePath}";
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao iniciar log CSV:\n" + ex.Message);
                _currentLogFilePath = null;
            }
        }

        private void RegistrarDeteccaoCsv(string sourceLabel, FrameResult frame, DetectionDto det)
        {
            if (string.IsNullOrEmpty(_currentLogFilePath))
                return;

            try
            {
                _totalPieces++;
                if (_classCounts.ContainsKey(det.class_id))
                    _classCounts[det.class_id]++;
                else
                    _classCounts[det.class_id] = 1;

                string datetime = DateTime.Now.ToString(
                    "yyyy-MM-dd HH:mm:ss.fff",
                    CultureInfo.InvariantCulture);

                string line =
                    $"{datetime};" +
                    $"{sourceLabel};" +
                    $"{frame.frame_index};" +
                    $"{frame.time_seconds.ToString(CultureInfo.InvariantCulture)};" +
                    $"{det.track_id};" +
                    $"{det.class_id};" +
                    $"{det.score.ToString(CultureInfo.InvariantCulture)};" +
                    $"{det.x.ToString(CultureInfo.InvariantCulture)};" +
                    $"{det.y.ToString(CultureInfo.InvariantCulture)};" +
                    $"{det.w.ToString(CultureInfo.InvariantCulture)};" +
                    $"{det.h.ToString(CultureInfo.InvariantCulture)};" +
                    $"{(det.is_new ? 1 : 0)}" +
                    Environment.NewLine;

                lock (_logLock)
                {
                    File.AppendAllText(_currentLogFilePath, line, Encoding.UTF8);
                }
            }
            catch
            {
            }
        }

        private void EscreverResumoCsv()
        {
            if (string.IsNullOrEmpty(_currentLogFilePath))
                return;

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("# ---- RESUMO ----");
                sb.AppendLine($"# source={_currentSourceLabel}");
                sb.AppendLine($"# total_pecas={_totalPieces}");

                foreach (var kvp in _classCounts.OrderBy(k => k.Key))
                {
                    sb.AppendLine($"# class_{kvp.Key}={kvp.Value}");
                }

                sb.AppendLine();

                lock (_logLock)
                {
                    File.AppendAllText(_currentLogFilePath, sb.ToString(), Encoding.UTF8);
                }
            }
            catch
            {
            }
        }

        // =========================================================
        // CLASSES AUXILIARES
        // =========================================================

        private class DetectionDto
        {
            public int track_id { get; set; }
            public int class_id { get; set; }
            public float score { get; set; }
            public float x { get; set; }
            public float y { get; set; }
            public float w { get; set; }
            public float h { get; set; }
            public bool is_new { get; set; }
        }

        private class FrameResult
        {
            public int frame_index { get; set; }
            public double time_seconds { get; set; }
            public string image { get; set; } = "";
            public List<DetectionDto> detections { get; set; } = new List<DetectionDto>();
        }

        private class CameraItem
        {
            public int Index { get; set; }
            public string Name { get; set; } = "";

            public override string ToString()
            {
                return Name;
            }
        }

        private void btnPastaArquivoLog_Click_1(object sender, EventArgs e)
        {

        }
    }
}
