namespace Simulador_KIT_SENAI_PIC16_v3_3
{
    partial class LCDv2
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
            this.components = new System.ComponentModel.Container();
            this.tCursor = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // tCursor
            // 
            this.tCursor.Interval = 500;
            this.tCursor.Tick += new System.EventHandler(this.tCursor_Tick);
            // 
            // LCDv2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Lime;
            this.Name = "LCDv2";
            this.Size = new System.Drawing.Size(248, 64);
            this.Resize += new System.EventHandler(this.LCDv2_Resize);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Timer tCursor;

    }
}
