namespace YoloOnnxForms
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            pictureBox1 = new PictureBox();
            btnLoadImage = new Button();
            btnDetect = new Button();
            lbResults = new ListBox();
            lblStatus = new Label();
            textBoxConf = new TextBox();
            labelConfianca = new Label();
            btnOpenCamera = new Button();
            comboBoxCamera = new ComboBox();
            btnPastaArquivoLog = new Button();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // pictureBox1
            // 
            pictureBox1.Location = new Point(12, 12);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(608, 478);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // btnLoadImage
            // 
            btnLoadImage.Location = new Point(911, 12);
            btnLoadImage.Name = "btnLoadImage";
            btnLoadImage.Size = new Size(189, 29);
            btnLoadImage.TabIndex = 1;
            btnLoadImage.Text = "Carregar Arquivo";
            btnLoadImage.UseVisualStyleBackColor = true;
            btnLoadImage.Click += btnLoadImage_Click;
            // 
            // btnDetect
            // 
            btnDetect.Location = new Point(912, 47);
            btnDetect.Name = "btnDetect";
            btnDetect.Size = new Size(188, 29);
            btnDetect.TabIndex = 2;
            btnDetect.Text = "Carregar Modelo";
            btnDetect.UseVisualStyleBackColor = true;
            btnDetect.Click += btnDetect_Click;
            // 
            // lbResults
            // 
            lbResults.FormattingEnabled = true;
            lbResults.Location = new Point(638, 186);
            lbResults.Name = "lbResults";
            lbResults.Size = new Size(461, 304);
            lbResults.TabIndex = 4;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(638, 163);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(53, 20);
            lblStatus.TabIndex = 5;
            lblStatus.Text = "Pronto";
            // 
            // textBoxConf
            // 
            textBoxConf.Location = new Point(1004, 89);
            textBoxConf.Name = "textBoxConf";
            textBoxConf.Size = new Size(95, 27);
            textBoxConf.TabIndex = 6;
            textBoxConf.Text = "0.3";
            // 
            // labelConfianca
            // 
            labelConfianca.AutoSize = true;
            labelConfianca.Location = new Point(912, 92);
            labelConfianca.Name = "labelConfianca";
            labelConfianca.Size = new Size(75, 20);
            labelConfianca.TabIndex = 7;
            labelConfianca.Text = "Confiança";
            // 
            // btnOpenCamera
            // 
            btnOpenCamera.Location = new Point(638, 12);
            btnOpenCamera.Name = "btnOpenCamera";
            btnOpenCamera.Size = new Size(205, 29);
            btnOpenCamera.TabIndex = 8;
            btnOpenCamera.Text = "Abrir Camera";
            btnOpenCamera.UseVisualStyleBackColor = true;
            btnOpenCamera.Click += btnOpenCamera_Click;
            // 
            // comboBoxCamera
            // 
            comboBoxCamera.FormattingEnabled = true;
            comboBoxCamera.Location = new Point(638, 48);
            comboBoxCamera.Name = "comboBoxCamera";
            comboBoxCamera.Size = new Size(205, 28);
            comboBoxCamera.TabIndex = 9;
            // 
            // btnPastaArquivoLog
            // 
            btnPastaArquivoLog.Location = new Point(640, 88);
            btnPastaArquivoLog.Name = "btnPastaArquivoLog";
            btnPastaArquivoLog.Size = new Size(203, 29);
            btnPastaArquivoLog.TabIndex = 10;
            btnPastaArquivoLog.Text = "Pasta do Arquivo Log";
            btnPastaArquivoLog.UseVisualStyleBackColor = true;
           
            this.btnPastaArquivoLog.Click += new System.EventHandler(this.btnPastaArquivoLog_Click);

            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1112, 512);
            Controls.Add(btnPastaArquivoLog);
            Controls.Add(comboBoxCamera);
            Controls.Add(btnOpenCamera);
            Controls.Add(labelConfianca);
            Controls.Add(textBoxConf);
            Controls.Add(lblStatus);
            Controls.Add(lbResults);
            Controls.Add(btnDetect);
            Controls.Add(btnLoadImage);
            Controls.Add(pictureBox1);
            Name = "Form1";
            Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private PictureBox pictureBox1;
        private Button btnLoadImage;
        private Button btnDetect;
        private ListBox lbResults;
        private Label lblStatus;
        private TextBox textBoxConf;
        private Label labelConfianca;
        private Button btnOpenCamera;
        private ComboBox comboBoxCamera;
        private Button btnPastaArquivoLog;
    }
}
