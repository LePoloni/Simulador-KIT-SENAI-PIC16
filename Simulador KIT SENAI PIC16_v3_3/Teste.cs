using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Simulador_KIT_SENAI_PIC16_v3_3
{
    public partial class Teste : UserControl
    {
        //---------------------------------------------------------------------
        #region Variáveis e Objetos--------------------------------------------
        int colunas = 16;
        int linhas = 2;
        int borda = 0;
        int tamanhoFonte = 16;
        int larguraCaractere = 1;
        int alturaCaractere = 1;
        Color corFundo = Color.Lime;
        Color corCaractere = Color.Black;
        Label[,] caractere = new Label[2, 16];
        #endregion

        //---------------------------------------------------------------------
        #region Propriedades---------------------------------------------------
        //public int Colunas
        //{
        //    get { return colunas; }
        //    set
        //    {
        //        if (value > 0 && value <= 40)
        //            colunas = value;
        //    }
        //}
        //public int Linhas
        //{
        //    get { return linhas; }
        //    set
        //    {
        //        if (value > 0 && value <= 4)
        //            linhas = value;
        //    }
        //}
        //public int TamanhoFonte
        //{
        //    get { return tamanhoFonte; }
        //    set
        //    {
        //        if (value >= 8 && value <= 72)
        //            tamanhoFonte = value;
        //    }
        //}
        //public Color CorFundo
        //{
        //    get { return CorFundo; }
        //    set
        //    {
        //        corFundo = value;
        //        this.BackColor = value;
        //    }
        //}
        //public Color CorCaractere
        //{
        //    get { return CorCaractere; }
        //    set
        //    {
        //        corCaractere = value;
        //        //Chamada do método de atualização
        //        AtualizaCorCaracteres();
        //    }
        //}
        #endregion

        //---------------------------------------------------------------------
        #region Método Construtor----------------------------------------------
        public Teste()
        {
            InitializeComponent();

            //colunas = 16;
            //linhas = 2;
            //tamanhoFonte = 16;
            //larguraCaractere = 1;
            //alturaCaractere = 1;
            //Color corFundo = Color.Lime;
            //Color corCaractere = Color.Black;

            //CalculaTamanhoCaracteres();
            //CriaCaracteres();
            //Texto(1, "0123456789ABCDEF");
            //Texto(2, "abcdefghijhkmnop");
        }
        #endregion

        //---------------------------------------------------------------------
        #region Métodos Privados Auxiliares------------------------------------
        private void CalculaTamanhoCaracteres()
        {
            larguraCaractere = (this.Width - (2 * borda)) / colunas;
            alturaCaractere = this.Height / linhas;
        }

        private void CriaCaracteres()
        {
            caractere = new Label[linhas, colunas];
            for (int l = 0; l < linhas; l++)
            {
                for (int c = 0; c < colunas; c++)
                {
                    caractere[l, c] = new Label();
                    caractere[l, c].Parent = this;
                    caractere[l, c].Visible = true;
                    caractere[l, c].Font = new Font("Lucida Console", tamanhoFonte, FontStyle.Bold);
                    caractere[l, c].AutoSize = false;
                    caractere[l, c].Size = new Size(larguraCaractere, alturaCaractere);
                    caractere[l, c].ForeColor = corCaractere;
                    caractere[l, c].BackColor = Color.Transparent;
                    caractere[l, c].TextAlign = ContentAlignment.MiddleCenter;
                    caractere[l, c].Top = l * alturaCaractere;
                    caractere[l, c].Left = c * larguraCaractere + borda;
                }
            }
        }

        private void AtualizaCorCaracteres()
        {
            caractere = new Label[linhas, colunas];
            for (int l = 0; l < linhas; l++)
            {
                for (int c = 0; c < colunas; c++)
                {
                    caractere[l, c].ForeColor = corCaractere;
                }
            }
        }

        private void AtualizaTamanhoCaracteres()
        {
            for (int l = 0; l < linhas; l++)
            {
                for (int c = 0; c < colunas; c++)
                {
                    caractere[l, c].Size = new Size(larguraCaractere, alturaCaractere);
                    caractere[l, c].Top = l * alturaCaractere;
                    caractere[l, c].Left = c * larguraCaractere + borda;
                }
            }
        }
        #endregion

        //---------------------------------------------------------------------
        #region Métodos Públicos-----------------------------------------------
        public bool Texto(int linha, string texto)
        {
            if (linha < 1 || linha > linhas || texto.Length > colunas)
            { return false; }

            char[] l = texto.ToCharArray();
            for (int c = 0; c < texto.Length; c++)
            {
                caractere[linha - 1, c].Text = l[c].ToString();     //Poderia ser diferente (Substring)
            }
            return true;
        }
        #endregion

        //---------------------------------------------------------------------
        #region Métodos privados disparados por eventos------------------------
        private void LCDv2_Resize(object sender, EventArgs e)
        {
            //CalculaTamanhoCaracteres();
            //AtualizaTamanhoCaracteres();
        }
        #endregion
    }
}
