using System;
using System.ComponentModel;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace YoloOnnxForms
{
    public partial class ConfigForm : Form
    {
        public DetectionSettings Settings { get; private set; }

        // pode ser anulável, será inicializada em CarregarSettingsNosControles()
        private BindingList<ClassConfig>? _bindingClasses;

        public ConfigForm(DetectionSettings current)
        {
            InitializeComponent();   // usa o InitializeComponent gerado no Designer

            // Cópia das configurações atuais para edição
            Settings = new DetectionSettings
            {
                Confidence = current.Confidence,
                ByteTrackActivation = current.ByteTrackActivation,
                LostTrackBuffer = current.LostTrackBuffer,
                MatchingThreshold = current.MatchingThreshold,
                MinConsecutiveFrames = current.MinConsecutiveFrames,
                UseClassFilter = current.UseClassFilter,
                Classes = new System.Collections.Generic.List<ClassConfig>()
            };

            foreach (var c in current.Classes)
            {
                Settings.Classes.Add(new ClassConfig
                {
                    Id = c.Id,
                    Name = c.Name,
                    CountAsDefect = c.CountAsDefect
                });
            }

            CarregarSettingsNosControles();
        }

        private void CarregarSettingsNosControles()
        {
            // NumericUpDowns
            numConfidence.Value = (decimal)Settings.Confidence;
            numActivation.Value = (decimal)Settings.ByteTrackActivation;
            numLostBuffer.Value = Settings.LostTrackBuffer;
            numMatching.Value = (decimal)Settings.MatchingThreshold;
            numMinFrames.Value = Settings.MinConsecutiveFrames;

            // CheckBox
            chkUseClassFilter.Checked = Settings.UseClassFilter;

            // DataGridView – cria colunas e faz binding
            dgvClasses.AutoGenerateColumns = false;
            dgvClasses.Columns.Clear();

            var colId = new DataGridViewTextBoxColumn
            {
                HeaderText = "ID",
                DataPropertyName = "Id",
                Width = 60
            };
            var colName = new DataGridViewTextBoxColumn
            {
                HeaderText = "Nome da Classe",
                DataPropertyName = "Name",
                Width = 300
            };
            var colDefect = new DataGridViewCheckBoxColumn
            {
                HeaderText = "Conta como defeito",
                DataPropertyName = "CountAsDefect",
                Width = 150
            };

            dgvClasses.Columns.Add(colId);
            dgvClasses.Columns.Add(colName);
            dgvClasses.Columns.Add(colDefect);

            _bindingClasses = new BindingList<ClassConfig>(Settings.Classes);
            dgvClasses.DataSource = _bindingClasses;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }



        private void btnOk_Click(object sender, EventArgs e)
        {
            // Atualiza o Settings com o que o usuário editou
            Settings.Confidence = (float)numConfidence.Value;
            Settings.ByteTrackActivation = (float)numActivation.Value;
            Settings.LostTrackBuffer = (int)numLostBuffer.Value;
            Settings.MatchingThreshold = (float)numMatching.Value;
            Settings.MinConsecutiveFrames = (int)numMinFrames.Value;
            Settings.UseClassFilter = chkUseClassFilter.Checked;

            if (_bindingClasses != null)
            {
                Settings.Classes =
                    new System.Collections.Generic.List<ClassConfig>(_bindingClasses);
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void ConfigForm_Load(object sender, EventArgs e)
        {

            toolTip1.SetToolTip(numConfidence,
                "Probabilidade mínima para considerar uma detecção válida. (0,4–0,7 recomendado)");

            toolTip1.SetToolTip(numActivation,
                "Confiança mínima para o ByteTrack criar um novo ID de rastreamento.");

            toolTip1.SetToolTip(numLostBuffer,
                "Quantos frames o objeto pode sumir antes de o ID ser descartado.");

            toolTip1.SetToolTip(numMatching,
                "Quão rígido é o matching entre detecções e tracks (0–1). Valores altos = mais estável.");

            toolTip1.SetToolTip(numMinFrames,
                "Frames consecutivos que o objeto precisa aparecer para ser considerado válido.");

            toolTip1.SetToolTip(chkUseClassFilter,
                "Quando marcado, só as classes configuradas ao lado serão consideradas na contagem.");


        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void toolTip1_Popup(object sender, PopupEventArgs e)
        {

        }

        private void numMatching_ValueChanged(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void numActivation_ValueChanged(object sender, EventArgs e)
        {

        }
    }
}
