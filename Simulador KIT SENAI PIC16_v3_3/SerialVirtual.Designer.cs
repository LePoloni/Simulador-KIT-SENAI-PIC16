namespace Simulador_KIT_SENAI_PIC16_v3_3
{
    partial class SerialVirtual
    {
        /// <summary> 
        /// Variável de designer necessária.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Limpar os recursos que estão sendo usados.
        /// </summary>
        /// <param name="disposing">verdade se for necessário descartar os recursos gerenciados; caso contrário, falso.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region código gerado pelo Component Designer

        /// <summary> 
        /// Método necessário para o suporte do Designer - não modifique 
        /// o conteúdo deste método com o editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.bConectar = new System.Windows.Forms.Button();
            this.tbTX = new System.Windows.Forms.TextBox();
            this.tbRX = new System.Windows.Forms.TextBox();
            this.bLimpar = new System.Windows.Forms.Button();
            this.cbBaudRate = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.gbSerialVirtual = new System.Windows.Forms.GroupBox();
            this.cbLimpar = new System.Windows.Forms.CheckBox();
            this.cbEnter = new System.Windows.Forms.CheckBox();
            this.bEnviar = new System.Windows.Forms.Button();
            this.gbSerialVirtual.SuspendLayout();
            this.SuspendLayout();
            // 
            // bConectar
            // 
            this.bConectar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bConectar.Enabled = false;
            this.bConectar.Location = new System.Drawing.Point(124, 33);
            this.bConectar.Name = "bConectar";
            this.bConectar.Size = new System.Drawing.Size(87, 23);
            this.bConectar.TabIndex = 0;
            this.bConectar.Text = "Conectar";
            this.bConectar.UseVisualStyleBackColor = true;
            this.bConectar.Click += new System.EventHandler(this.bConectar_Click);
            // 
            // tbTX
            // 
            this.tbTX.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbTX.Location = new System.Drawing.Point(6, 86);
            this.tbTX.Name = "tbTX";
            this.tbTX.Size = new System.Drawing.Size(112, 20);
            this.tbTX.TabIndex = 1;
            // 
            // tbRX
            // 
            this.tbRX.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbRX.Location = new System.Drawing.Point(6, 154);
            this.tbRX.Multiline = true;
            this.tbRX.Name = "tbRX";
            this.tbRX.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tbRX.Size = new System.Drawing.Size(205, 107);
            this.tbRX.TabIndex = 2;
            // 
            // bLimpar
            // 
            this.bLimpar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.bLimpar.Location = new System.Drawing.Point(6, 267);
            this.bLimpar.Name = "bLimpar";
            this.bLimpar.Size = new System.Drawing.Size(205, 23);
            this.bLimpar.TabIndex = 3;
            this.bLimpar.Text = "Limpar";
            this.bLimpar.UseVisualStyleBackColor = true;
            this.bLimpar.Click += new System.EventHandler(this.bLimpar_Click);
            // 
            // cbBaudRate
            // 
            this.cbBaudRate.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbBaudRate.FormattingEnabled = true;
            this.cbBaudRate.Location = new System.Drawing.Point(6, 35);
            this.cbBaudRate.Name = "cbBaudRate";
            this.cbBaudRate.Size = new System.Drawing.Size(112, 21);
            this.cbBaudRate.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(57, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "Baus Rate";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 69);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(57, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "TX (ASCII)";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 138);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(58, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "RX (ASCII)";
            // 
            // gbSerialVirtual
            // 
            this.gbSerialVirtual.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbSerialVirtual.Controls.Add(this.cbLimpar);
            this.gbSerialVirtual.Controls.Add(this.cbEnter);
            this.gbSerialVirtual.Controls.Add(this.tbRX);
            this.gbSerialVirtual.Controls.Add(this.label3);
            this.gbSerialVirtual.Controls.Add(this.bLimpar);
            this.gbSerialVirtual.Controls.Add(this.label2);
            this.gbSerialVirtual.Controls.Add(this.tbTX);
            this.gbSerialVirtual.Controls.Add(this.label1);
            this.gbSerialVirtual.Controls.Add(this.bEnviar);
            this.gbSerialVirtual.Controls.Add(this.bConectar);
            this.gbSerialVirtual.Controls.Add(this.cbBaudRate);
            this.gbSerialVirtual.Enabled = false;
            this.gbSerialVirtual.Location = new System.Drawing.Point(3, 0);
            this.gbSerialVirtual.Name = "gbSerialVirtual";
            this.gbSerialVirtual.Size = new System.Drawing.Size(217, 298);
            this.gbSerialVirtual.TabIndex = 8;
            this.gbSerialVirtual.TabStop = false;
            this.gbSerialVirtual.Text = "Serial Virtual";
            // 
            // cbLimpar
            // 
            this.cbLimpar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbLimpar.AutoSize = true;
            this.cbLimpar.Location = new System.Drawing.Point(124, 112);
            this.cbLimpar.Name = "cbLimpar";
            this.cbLimpar.Size = new System.Drawing.Size(57, 17);
            this.cbLimpar.TabIndex = 8;
            this.cbLimpar.Text = "Limpar";
            this.cbLimpar.UseVisualStyleBackColor = true;
            // 
            // cbEnter
            // 
            this.cbEnter.AutoSize = true;
            this.cbEnter.Location = new System.Drawing.Point(6, 112);
            this.cbEnter.Name = "cbEnter";
            this.cbEnter.Size = new System.Drawing.Size(112, 17);
            this.cbEnter.TabIndex = 8;
            this.cbEnter.Text = "Enter (0x0D,0x0A)";
            this.cbEnter.UseVisualStyleBackColor = true;
            // 
            // bEnviar
            // 
            this.bEnviar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bEnviar.Enabled = false;
            this.bEnviar.Location = new System.Drawing.Point(124, 84);
            this.bEnviar.Name = "bEnviar";
            this.bEnviar.Size = new System.Drawing.Size(87, 23);
            this.bEnviar.TabIndex = 0;
            this.bEnviar.Text = "Enviar";
            this.bEnviar.UseVisualStyleBackColor = true;
            this.bEnviar.Click += new System.EventHandler(this.bEnviar_Click);
            // 
            // SerialVirtual
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.gbSerialVirtual);
            this.Name = "SerialVirtual";
            this.Size = new System.Drawing.Size(225, 304);
            this.gbSerialVirtual.ResumeLayout(false);
            this.gbSerialVirtual.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button bConectar;
        private System.Windows.Forms.TextBox tbTX;
        private System.Windows.Forms.TextBox tbRX;
        private System.Windows.Forms.Button bLimpar;
        private System.Windows.Forms.ComboBox cbBaudRate;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox gbSerialVirtual;
        private System.Windows.Forms.CheckBox cbLimpar;
        private System.Windows.Forms.CheckBox cbEnter;
        private System.Windows.Forms.Button bEnviar;
    }
}
