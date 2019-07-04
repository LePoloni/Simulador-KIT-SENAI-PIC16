namespace Simulador_KIT_SENAI_PIC16_v3_3
{
    partial class FormInfo
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormInfo));
            this.label1 = new System.Windows.Forms.Label();
            this.linklLattes = new System.Windows.Forms.LinkLabel();
            this.label2 = new System.Windows.Forms.Label();
            this.linklBlog = new System.Windows.Forms.LinkLabel();
            this.label3 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(85, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Currículo Lattes:";
            // 
            // linklLattes
            // 
            this.linklLattes.AutoSize = true;
            this.linklLattes.Location = new System.Drawing.Point(103, 9);
            this.linklLattes.Name = "linklLattes";
            this.linklLattes.Size = new System.Drawing.Size(203, 13);
            this.linklLattes.TabIndex = 1;
            this.linklLattes.TabStop = true;
            this.linklLattes.Text = "http://lattes.cnpq.br/6255986062207024";
            this.linklLattes.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linklLattes_LinkClicked);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 35);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(31, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Blog:";
            // 
            // linklBlog
            // 
            this.linklBlog.AutoSize = true;
            this.linklBlog.Location = new System.Drawing.Point(49, 35);
            this.linklBlog.Name = "linklBlog";
            this.linklBlog.Size = new System.Drawing.Size(201, 13);
            this.linklBlog.TabIndex = 3;
            this.linklBlog.TabStop = true;
            this.linklBlog.Text = "http://oprofessorleandro.wordpress.com/";
            this.linklBlog.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linklBlog_LinkClicked);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 65);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(38, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "E-mail:";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(56, 62);
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.Size = new System.Drawing.Size(194, 20);
            this.textBox1.TabIndex = 5;
            this.textBox1.Text = "leandro.dantas@sp.senai.br";
            this.textBox1.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // FormInfo
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.ClientSize = new System.Drawing.Size(310, 94);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.linklBlog);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.linklLattes);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "FormInfo";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Prof. Leandro";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.LinkLabel linklLattes;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.LinkLabel linklBlog;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBox1;
    }
}