namespace Simulador_KIT_SENAI_PIC16_v3_3
{
    partial class FormConversor
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormConversor));
            this.tbValor = new System.Windows.Forms.TextBox();
            this.tbDecimal = new System.Windows.Forms.TextBox();
            this.tbBin = new System.Windows.Forms.TextBox();
            this.tbHexa = new System.Windows.Forms.TextBox();
            this.tbASCII = new System.Windows.Forms.TextBox();
            this.rbDecimal = new System.Windows.Forms.RadioButton();
            this.rbBin = new System.Windows.Forms.RadioButton();
            this.rbHexa = new System.Windows.Forms.RadioButton();
            this.rbASCII = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.bConverter = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tbValor
            // 
            this.tbValor.Location = new System.Drawing.Point(110, 18);
            this.tbValor.MaxLength = 16;
            this.tbValor.Name = "tbValor";
            this.tbValor.Size = new System.Drawing.Size(109, 20);
            this.tbValor.TabIndex = 0;
            this.tbValor.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // tbDecimal
            // 
            this.tbDecimal.Location = new System.Drawing.Point(80, 17);
            this.tbDecimal.MaxLength = 16;
            this.tbDecimal.Name = "tbDecimal";
            this.tbDecimal.ReadOnly = true;
            this.tbDecimal.Size = new System.Drawing.Size(109, 20);
            this.tbDecimal.TabIndex = 0;
            this.tbDecimal.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // tbBin
            // 
            this.tbBin.Location = new System.Drawing.Point(80, 40);
            this.tbBin.MaxLength = 16;
            this.tbBin.Name = "tbBin";
            this.tbBin.ReadOnly = true;
            this.tbBin.Size = new System.Drawing.Size(109, 20);
            this.tbBin.TabIndex = 1;
            this.tbBin.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // tbHexa
            // 
            this.tbHexa.Location = new System.Drawing.Point(80, 63);
            this.tbHexa.MaxLength = 16;
            this.tbHexa.Name = "tbHexa";
            this.tbHexa.ReadOnly = true;
            this.tbHexa.Size = new System.Drawing.Size(109, 20);
            this.tbHexa.TabIndex = 2;
            this.tbHexa.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // tbASCII
            // 
            this.tbASCII.Location = new System.Drawing.Point(80, 86);
            this.tbASCII.MaxLength = 16;
            this.tbASCII.Name = "tbASCII";
            this.tbASCII.ReadOnly = true;
            this.tbASCII.Size = new System.Drawing.Size(109, 20);
            this.tbASCII.TabIndex = 3;
            this.tbASCII.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // rbDecimal
            // 
            this.rbDecimal.AutoSize = true;
            this.rbDecimal.Checked = true;
            this.rbDecimal.Location = new System.Drawing.Point(9, 19);
            this.rbDecimal.Name = "rbDecimal";
            this.rbDecimal.Size = new System.Drawing.Size(63, 17);
            this.rbDecimal.TabIndex = 2;
            this.rbDecimal.TabStop = true;
            this.rbDecimal.Text = "Decimal";
            this.rbDecimal.UseVisualStyleBackColor = true;
            // 
            // rbBin
            // 
            this.rbBin.AutoSize = true;
            this.rbBin.Location = new System.Drawing.Point(9, 42);
            this.rbBin.Name = "rbBin";
            this.rbBin.Size = new System.Drawing.Size(57, 17);
            this.rbBin.TabIndex = 3;
            this.rbBin.Text = "Binário";
            this.rbBin.UseVisualStyleBackColor = true;
            // 
            // rbHexa
            // 
            this.rbHexa.AutoSize = true;
            this.rbHexa.Location = new System.Drawing.Point(9, 65);
            this.rbHexa.Name = "rbHexa";
            this.rbHexa.Size = new System.Drawing.Size(86, 17);
            this.rbHexa.TabIndex = 4;
            this.rbHexa.Text = "Hexadecimal";
            this.rbHexa.UseVisualStyleBackColor = true;
            // 
            // rbASCII
            // 
            this.rbASCII.AutoSize = true;
            this.rbASCII.Location = new System.Drawing.Point(9, 88);
            this.rbASCII.Name = "rbASCII";
            this.rbASCII.Size = new System.Drawing.Size(52, 17);
            this.rbASCII.TabIndex = 5;
            this.rbASCII.Text = "ASCII";
            this.rbASCII.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.groupBox1.Controls.Add(this.bConverter);
            this.groupBox1.Controls.Add(this.tbValor);
            this.groupBox1.Controls.Add(this.rbDecimal);
            this.groupBox1.Controls.Add(this.rbASCII);
            this.groupBox1.Controls.Add(this.rbBin);
            this.groupBox1.Controls.Add(this.rbHexa);
            this.groupBox1.Location = new System.Drawing.Point(12, 5);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(225, 114);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Selecione a base de origem e digite o valor";
            // 
            // bConverter
            // 
            this.bConverter.Location = new System.Drawing.Point(110, 39);
            this.bConverter.Name = "bConverter";
            this.bConverter.Size = new System.Drawing.Size(109, 23);
            this.bConverter.TabIndex = 1;
            this.bConverter.Text = "&Converter";
            this.bConverter.UseVisualStyleBackColor = true;
            this.bConverter.Click += new System.EventHandler(this.bConverter_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.BackColor = System.Drawing.Color.PaleGreen;
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.tbDecimal);
            this.groupBox2.Controls.Add(this.tbBin);
            this.groupBox2.Controls.Add(this.tbASCII);
            this.groupBox2.Controls.Add(this.tbHexa);
            this.groupBox2.Location = new System.Drawing.Point(243, 6);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(196, 113);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Resultado da conversão";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 89);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(34, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "ASCII";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 66);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(68, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Hexadecimal";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 43);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(39, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Binário";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(45, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Decimal";
            // 
            // FormConversor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(450, 128);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.Name = "FormConversor";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Conversor de Bases (limite de 16 bits)";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FormConversor_KeyDown);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox tbValor;
        private System.Windows.Forms.TextBox tbDecimal;
        private System.Windows.Forms.TextBox tbBin;
        private System.Windows.Forms.TextBox tbHexa;
        private System.Windows.Forms.TextBox tbASCII;
        private System.Windows.Forms.RadioButton rbDecimal;
        private System.Windows.Forms.RadioButton rbBin;
        private System.Windows.Forms.RadioButton rbHexa;
        private System.Windows.Forms.RadioButton rbASCII;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button bConverter;
    }
}