namespace Simulador_KIT_SENAI_PIC16_v3_3
{
    partial class FormTicks
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormTicks));
            this.tbTicks = new System.Windows.Forms.TextBox();
            this.tbarTicks = new System.Windows.Forms.TrackBar();
            this.bOK = new System.Windows.Forms.Button();
            this.bCancelar = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.cbTurbo = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.tbarTicks)).BeginInit();
            this.SuspendLayout();
            // 
            // tbTicks
            // 
            this.tbTicks.Location = new System.Drawing.Point(346, 26);
            this.tbTicks.Name = "tbTicks";
            this.tbTicks.Size = new System.Drawing.Size(68, 20);
            this.tbTicks.TabIndex = 0;
            this.tbTicks.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.tbTicks.TextChanged += new System.EventHandler(this.tbTicks_TextChanged);
            // 
            // tbarTicks
            // 
            this.tbarTicks.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.tbarTicks.Location = new System.Drawing.Point(12, 26);
            this.tbarTicks.Maximum = 20;
            this.tbarTicks.Minimum = 1;
            this.tbarTicks.Name = "tbarTicks";
            this.tbarTicks.Size = new System.Drawing.Size(326, 45);
            this.tbarTicks.TabIndex = 1;
            this.tbarTicks.Value = 1;
            this.tbarTicks.Scroll += new System.EventHandler(this.tbarTicks_Scroll);
            // 
            // bOK
            // 
            this.bOK.Location = new System.Drawing.Point(15, 115);
            this.bOK.Name = "bOK";
            this.bOK.Size = new System.Drawing.Size(75, 23);
            this.bOK.TabIndex = 3;
            this.bOK.Text = "OK";
            this.bOK.UseVisualStyleBackColor = true;
            this.bOK.Click += new System.EventHandler(this.bOK_Click);
            // 
            // bCancelar
            // 
            this.bCancelar.Location = new System.Drawing.Point(96, 115);
            this.bCancelar.Name = "bCancelar";
            this.bCancelar.Size = new System.Drawing.Size(75, 23);
            this.bCancelar.TabIndex = 2;
            this.bCancelar.Text = "Cancelar";
            this.bCancelar.UseVisualStyleBackColor = true;
            this.bCancelar.Click += new System.EventHandler(this.bCancelar_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(326, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Tempo mínimo entre instruções no modo sem refresh das memórias:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(420, 29);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(18, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "us";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.label3.Location = new System.Drawing.Point(12, 58);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(25, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "100";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.label4.Location = new System.Drawing.Point(307, 58);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(31, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "2000";
            // 
            // cbTurbo
            // 
            this.cbTurbo.AutoSize = true;
            this.cbTurbo.BackColor = System.Drawing.Color.Salmon;
            this.cbTurbo.Location = new System.Drawing.Point(15, 84);
            this.cbTurbo.Name = "cbTurbo";
            this.cbTurbo.Size = new System.Drawing.Size(428, 17);
            this.cbTurbo.TabIndex = 8;
            this.cbTurbo.Text = "Habilitar modo turbo (máximo desempenho, pode causar instabilidade no computador)" +
    "";
            this.cbTurbo.UseVisualStyleBackColor = false;
            // 
            // FormTicks
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(450, 150);
            this.Controls.Add(this.cbTurbo);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.bCancelar);
            this.Controls.Add(this.bOK);
            this.Controls.Add(this.tbarTicks);
            this.Controls.Add(this.tbTicks);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormTicks";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Tempo Entre Instruções Sem Refresh";
            ((System.ComponentModel.ISupportInitialize)(this.tbarTicks)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tbTicks;
        private System.Windows.Forms.TrackBar tbarTicks;
        private System.Windows.Forms.Button bOK;
        private System.Windows.Forms.Button bCancelar;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox cbTurbo;
    }
}