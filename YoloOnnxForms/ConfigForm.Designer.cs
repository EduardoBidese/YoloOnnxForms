namespace YoloOnnxForms
{
    partial class ConfigForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            numConfidence = new NumericUpDown();
            numActivation = new NumericUpDown();
            numLostBuffer = new NumericUpDown();
            numMatching = new NumericUpDown();
            numMinFrames = new NumericUpDown();
            chkUseClassFilter = new CheckBox();
            dgvClasses = new DataGridView();
            btnOk = new Button();
            btnCancel = new Button();
            label1 = new Label();
            toolTip1 = new ToolTip(components);
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            ((System.ComponentModel.ISupportInitialize)numConfidence).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numActivation).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numLostBuffer).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMatching).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMinFrames).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvClasses).BeginInit();
            SuspendLayout();
            // 
            // numConfidence
            // 
            numConfidence.DecimalPlaces = 2;
            numConfidence.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
            numConfidence.Location = new Point(289, 126);
            numConfidence.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
            numConfidence.Name = "numConfidence";
            numConfidence.Size = new Size(93, 27);
            numConfidence.TabIndex = 0;
            // 
            // numActivation
            // 
            numActivation.DecimalPlaces = 2;
            numActivation.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
            numActivation.Location = new Point(289, 168);
            numActivation.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
            numActivation.Name = "numActivation";
            numActivation.Size = new Size(93, 27);
            numActivation.TabIndex = 1;
            numActivation.ValueChanged += numActivation_ValueChanged;
            // 
            // numLostBuffer
            // 
            numLostBuffer.Location = new Point(289, 210);
            numLostBuffer.Name = "numLostBuffer";
            numLostBuffer.Size = new Size(93, 27);
            numLostBuffer.TabIndex = 2;
            // 
            // numMatching
            // 
            numMatching.Location = new Point(289, 252);
            numMatching.Name = "numMatching";
            numMatching.Size = new Size(93, 27);
            numMatching.TabIndex = 3;
            numMatching.ValueChanged += numMatching_ValueChanged;
            // 
            // numMinFrames
            // 
            numMinFrames.Location = new Point(289, 294);
            numMinFrames.Name = "numMinFrames";
            numMinFrames.Size = new Size(93, 27);
            numMinFrames.TabIndex = 4;
            // 
            // chkUseClassFilter
            // 
            chkUseClassFilter.AutoSize = true;
            chkUseClassFilter.CheckAlign = ContentAlignment.MiddleRight;
            chkUseClassFilter.ImageAlign = ContentAlignment.BottomLeft;
            chkUseClassFilter.Location = new Point(58, 338);
            chkUseClassFilter.Name = "chkUseClassFilter";
            chkUseClassFilter.Size = new Size(166, 24);
            chkUseClassFilter.TabIndex = 5;
            chkUseClassFilter.Text = "Usar filtro de classes";
            chkUseClassFilter.TextAlign = ContentAlignment.MiddleCenter;
            chkUseClassFilter.UseVisualStyleBackColor = true;
            // 
            // dgvClasses
            // 
            dgvClasses.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvClasses.Location = new Point(478, 105);
            dgvClasses.Name = "dgvClasses";
            dgvClasses.RowHeadersWidth = 51;
            dgvClasses.Size = new Size(300, 188);
            dgvClasses.TabIndex = 6;
            // 
            // btnOk
            // 
            btnOk.Location = new Point(573, 409);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(94, 29);
            btnOk.TabIndex = 7;
            btnOk.Text = "Ok";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += btnOk_Click;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(694, 409);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(94, 29);
            btnCancel.TabIndex = 8;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(58, 128);
            label1.Name = "label1";
            label1.Size = new Size(168, 20);
            label1.TabIndex = 9;
            label1.Text = "Confiança mínima YOLO";
            label1.Click += label1_Click;
            // 
            // toolTip1
            // 
            toolTip1.Popup += toolTip1_Popup;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(58, 170);
            label2.Name = "label2";
            label2.Size = new Size(201, 20);
            label2.TabIndex = 10;
            label2.Text = "Ativação de track (ByteTrack)";
            label2.Click += label2_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(58, 212);
            label3.Name = "label3";
            label3.Size = new Size(172, 20);
            label3.TabIndex = 11;
            label3.Text = "Buffer de perda (frames)";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(58, 254);
            label4.Name = "label4";
            label4.Size = new Size(126, 20);
            label4.TabIndex = 12;
            label4.Text = "Matching mínimo";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(58, 296);
            label5.Name = "label5";
            label5.Size = new Size(227, 20);
            label5.TabIndex = 13;
            label5.Text = "Frames consecutivos para validar";
            // 
            // ConfigForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(btnCancel);
            Controls.Add(btnOk);
            Controls.Add(dgvClasses);
            Controls.Add(chkUseClassFilter);
            Controls.Add(numMinFrames);
            Controls.Add(numMatching);
            Controls.Add(numLostBuffer);
            Controls.Add(numActivation);
            Controls.Add(numConfidence);
            Name = "ConfigForm";
            Text = "Configurações";
            Load += ConfigForm_Load;
            ((System.ComponentModel.ISupportInitialize)numConfidence).EndInit();
            ((System.ComponentModel.ISupportInitialize)numActivation).EndInit();
            ((System.ComponentModel.ISupportInitialize)numLostBuffer).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMatching).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMinFrames).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvClasses).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private NumericUpDown numConfidence;
        private NumericUpDown numActivation;
        private NumericUpDown numLostBuffer;
        private NumericUpDown numMatching;
        private NumericUpDown numMinFrames;
        private CheckBox chkUseClassFilter;
        private DataGridView dgvClasses;
        private Button btnOk;
        private Button btnCancel;
        private Label label1;
        private ToolTip toolTip1;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;
    }
}