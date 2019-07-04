////--------------------------------------------------------------------
#region Cabeçalho
/*
 * -Versão 3.2
 * Corrigi o comando para ligar o cursor piscante, troquei o 0Eh por 0Dh.
 * No momento essa é a única possibilidade para o cursor piscar ou desligar (0Ch)
*/
#endregion

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
    public partial class LCDv2 : UserControl
    {
        //--------------------------------------------------------------------
        #region Modelo Memória e Comandos do LCD
        /*
         * Display 20x4 Endereços das Linhas
         * int const line1 = 0x80; //endereço linha1 x posição1 do LCD = 128(decimal)
         * int const line2 = 0xC0; //endereço linha2 x posição1 do LCD = 192(decimal)
         * int const line3 = 0x94; //endereço linha3 x posição1 do LCD = 148(decimal)
         * int const line4 = 0xD4; //endereço linha4 x posição1 do LCD = 212(decimal)
         * 
         * Comandos
        =================================================================================
        DESCRIÇÃO						MODO						RS	R/W	Código (Hexa)
        =================================================================================
        Display							Liga (sem cursor) 			0	0	0C
								        Desliga						0	0	0A / 08
        ---------------------------------------------------------------------------------
        Limpa display com home cursor								0	0	01
        ---------------------------------------------------------------------------------
        Controle do cursor				Liga						0	0	0E
								        Desliga						0	0	0C
								        Desloca para esquerda		0	0	10
								        Desloca para direita		0	0	14
								        Cursor home 				0	0	02
								        Cursor piscante				0	0	0D
								        Cursor com alternância		0	0	0F
        ---------------------------------------------------------------------------------
        Sentido de deslocamento do 		Para a esquerda				0	0	04
        cursor ao entrar com caracter 	Para a direita 				0	0	06
        ---------------------------------------------------------------------------------
        Deslocamento da mensagem		Para a esquerda				0	0	07
        ao entrar com caracter 			Para a direita				0	0	05
        ---------------------------------------------------------------------------------
        Deslocamento da mensagem 		Para a esquerda				0	0	18
        sem entrada de caracter 		Para a direita				0	0	1C
        ---------------------------------------------------------------------------------
        End. da primeira posição 		Primeira linha				0	0	80
								        Segunda linha				0	0	C0
        ---------------------------------------------------------------------------------
        Tipo de comunicação e caractere	4 bits, 2 ou + linhas,		0	0	28 
								        8x5 pontos	
								        8 bits, 2 ou + linhas, 		0	0	38
								        8x5 pontos	
        =================================================================================         
         */
        #endregion

        //---------------------------------------------------------------------
        #region Enumerações----------------------------------------------------
        public enum modo { _4bits, _8bits };
        #endregion

        //---------------------------------------------------------------------
        #region Variáveis e Objetos--------------------------------------------
        int colunas = 16;
        int linhas = 2;
        int borda = 4;
        int ajusteBorda = 0;
        int tamanhoFonte = 16;
        int larguraCaractere = 1;
        int alturaCaractere = 1;
        Color corFundo = Color.Lime;
        Color corCaractere = Color.Black;
        Label[,] caractere = new Label[2, 16];
        int[] endLinha = new int[] { 0x80, 0xC0, 0x94, 0xD4 };
        int[] caractereSel = new int[] { 0, 0 };    //Coluna,Linha
        modo modoFuncionamento = modo._8bits;
        bool E = false; //Enable do LCD
        bool cursor = true; //Cursor do LCD
        bool nibble1 = false;   //Nibble mais signigficativo do modo 4 bits
        ushort nibbleH;
        #endregion

        //---------------------------------------------------------------------
        #region Propriedades---------------------------------------------------
        public int Colunas
        {
            get { return colunas; }
            set
            {
                if (value > 0 && value <= 20)
                    colunas = value;
            }
        }
        public int Linhas
        {
            get { return linhas; }
            set
            {
                if (value > 0 && value <= 4)
                    linhas = value;
            }
        }
        public int TamanhoFonte
        {
            get { return tamanhoFonte; }
            set
            {
                if (value >= 8 && value <= 72)
                {
                    tamanhoFonte = value;
                    AtualizaFonteCaracteres();
                }
            }
        }
        public Color CorFundo
        {
            get { return corFundo; }
            set
            {
                corFundo = value;
                this.BackColor = value;
            }
        }
        public Color CorCaractere
        {
            get { return corCaractere; }
            set
            {
                corCaractere = value;
                //Chamada do método de atualização
                AtualizaCorCaracteres();
            }
        }
        public modo ModoFuncionamento
        {
            get { return modoFuncionamento; }
            set { modoFuncionamento = value; }
        }
        public bool Cursor
        {
            get { return cursor; }
            set
            {
                cursor = value;
                tCursor.Enabled = true;
            }
        }
        #endregion

        //---------------------------------------------------------------------
        #region Método Construtor----------------------------------------------
        public LCDv2()
        {
            InitializeComponent();

            colunas = 16;
            linhas = 2;
            tamanhoFonte = 16;
            larguraCaractere = 1;
            alturaCaractere = 1;
            Color corFundo = Color.Lime;
            Color corCaractere = Color.Black;
            modoFuncionamento = modo._8bits;

            CalculaTamanhoCaracteres();
            CriaCaracteres();
            EscreveTexto(1, "0123456789ABCDEF");
            EscreveTexto(2, "abcdefghijhkmnop");
        }
        #endregion

        //---------------------------------------------------------------------
        #region Métodos Privados Auxiliares------------------------------------
        private void CalculaTamanhoCaracteres()
        {
            //Calcula a largura das colunas
            larguraCaractere = (this.Width - (2 * borda)) / colunas;
            //Corregi erros de arredondamento e centraliza colunas no User Control 
            ajusteBorda = ((this.Width - (2 * borda)) - (larguraCaractere * colunas)) / 2;
            //Calcula a altura das linhas
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
                    caractere[l, c].Font = new Font("Consolas", tamanhoFonte, FontStyle.Bold);
                    caractere[l, c].AutoSize = false;
                    caractere[l, c].Size = new Size(larguraCaractere, alturaCaractere);
                    caractere[l, c].ForeColor = corCaractere;
                    caractere[l, c].BackColor = Color.Transparent;
                    caractere[l, c].TextAlign = ContentAlignment.MiddleCenter;
                    caractere[l, c].Top = l * alturaCaractere;
                    caractere[l, c].Left = c * larguraCaractere + borda + ajusteBorda;
                }
            }
        }

        private void AtualizaCorCaracteres()
        {
            for (int l = 0; l < linhas; l++)
            {
                for (int c = 0; c < colunas; c++)
                {
                    try
                    {
                        caractere[l, c].ForeColor = corCaractere;
                    }
                    catch { }
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
                    caractere[l, c].Left = c * larguraCaractere + borda + ajusteBorda;
                }
            }
        }

        private void AtualizaFonteCaracteres()
        {
            for (int l = 0; l < linhas; l++)
            {
                for (int c = 0; c < colunas; c++)
                {
                    try
                    {
                        caractere[l, c].Font = new Font("Consolas", tamanhoFonte, FontStyle.Bold);
                    }
                    catch { }
                }
            }
        }

        private bool TrataComando(byte comando)
        {
            if (comando == 0x01) Limpa();               //Limpa display com home cursor
            else if (comando == 0x0C) cursor = false;   //Desliga cursor
            else if (comando == 0x0D) { cursor = true; tCursor.Enabled = true; } //Liga cursor
            else if ((comando >= endLinha[0]) & (comando < (endLinha[3] + colunas)))
                //Seleciona endereço
                SelecionaEndereço(comando);
            else
                return false;
            return true;
        }
        #endregion

        //---------------------------------------------------------------------
        #region Métodos Públicos-----------------------------------------------
        public void Limpa()
        {
            for (int l = 0; l < linhas; l++)
            {
                for (int c = 0; c < colunas; c++)
                {
                    try
                    {
                        caractere[l, c].Text = "";
                    }
                    catch { }
                }
            }

            //Se é uma coluna e uma linha válidas
            if (caractereSel[0] >= 0 && caractereSel[0] < colunas &&
                caractereSel[1] >= 0 && caractereSel[1] < linhas)
            {
                //Remove o efeito do cursor neste caractere
                caractere[caractereSel[1], caractereSel[0]].BackColor = Color.Transparent;
            }

            //Atualiza caractere selecionado (cursor home)
            caractereSel[0] = 0;
            caractereSel[1] = 0;

        }

        public bool EscreveTexto(int linha, string texto)
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

        public bool SelecionaEndereço(int end)
        {
            bool endOK = false;
            int linhaSel = 0, colunaSel = 0;
            //Verifica o endereço
            for (int l = 0; l < linhas; l++)
            {
                //Se é uma faixa de endereço válida
                if ((end >= endLinha[l]) && (end < (endLinha[l] + colunas)))
                {
                    endOK = true;
                    //Salva a linha selecionada de acordo com o matriz de labels
                    linhaSel = l;
                    break;
                }
            }
            //Se o endereço não é válido retorna com false
            if (!endOK) return endOK;
            //Salva a coluna selecionada de acordo com o matriz de labels
            colunaSel = end - endLinha[linhaSel];

            //Se é uma coluna e uma linha válidas
            if (caractereSel[0] >= 0 && caractereSel[0] < colunas &&
                caractereSel[1] >= 0 && caractereSel[1] < linhas)
            {
                //Remove o efeito do cursor neste caractere
                caractere[caractereSel[1], caractereSel[0]].BackColor = Color.Transparent;
            }

            //Atualiza caractere selecionado
            caractereSel[0] = colunaSel;
            caractereSel[1] = linhaSel;

            return endOK;
        }

        public bool EscreveCaractere(char carac)
        {
            //Se é uma coluna e uma linha válidas
            if (caractereSel[0] >= 0 && caractereSel[0] < colunas &&
                caractereSel[1] >= 0 && caractereSel[1] < linhas)
            {
                caractere[caractereSel[1], caractereSel[0]].Text = carac.ToString();
                return true;
            }
            return false;
        }

        public bool EnviaComandoOuDado(ushort valor)
        {
            //Formato do dado:
            //15..11    10  9   8   7   ..  0
            // x..x     RS  RW  E   DB7 ..  DB0
            //RS = 0 --> Comando
            //RS = 1 --> Dado
            //RW = 0 --> Escreve
            //RW = 1 --> Lê
            //E = 0 = 1 = 0 --> Pulso de escrita no LCD

            bool status = false;

            //Se está em modo de 8 bits de dados
            if (modoFuncionamento == modo._8bits)
            {
                //Se ocorreu uma borda de descida no Enable (E)
                if ((E == true) && ((valor & 0x0100) == 0))
                {
                    //Se for escrita (RW=0)
                    if ((valor & 0x0200) == 0)
                    {
                        //Se for comando (RS=0)
                        if ((valor & 0x0400) == 0)
                        {
                            //Trata comando recebido
                            status = TrataComando((byte)(valor & 0x00FF));
                        }
                        //Se for dado (RS=0)
                        else
                        {
                            //Cverte o valor em um caractere
                            char dado = Convert.ToChar(valor & 0x00FF);
                            //Escreve o caractere no LCD
                            status = EscreveCaractere(dado);
                            //Se é uma coluna e uma linha válidas
                            if (caractereSel[0] >= 0 && caractereSel[0] < colunas &&
                                caractereSel[1] >= 0 && caractereSel[1] < linhas)
                            {
                                //Remove o efeito do cursor neste caractere
                                caractere[caractereSel[1], caractereSel[0]].BackColor = Color.Transparent;
                            }
                            //Faz o deslocamento automático
                            caractereSel[0]++;
                        }
                    }
                }
            }
            //Se está em modo de 4 bits de dados (2 nibbles em DB7..DB4 - 1o DB7..DB4 - 2o DB3..DB0)
            else
            {
                //Se ocorreu uma borda de descida no Enable (E)
                if ((E == true) && ((valor & 0x0100) == 0))
                {
                    //Se for escrita (RW=0)
                    if ((valor & 0x0200) == 0)
                    {
                        //Se for comando (RS=0)
                        if ((valor & 0x0400) == 0)
                        {
                            //Se ainda não rebeu o 1o nibble
                            if (!nibble1)
                            {
                                //Salva o nibble mais significativo
                                nibbleH = (byte)(valor & 0x00F0);   //Máscara de AND
                            }
                            //Se esse é o 2o nibble
                            else
                            {
                                //Monta o comando
                                valor = (ushort)(nibbleH + ((valor & 0x00F0) >> 4));
                                //Trata comando recebido
                                status = TrataComando((byte)(valor & 0x00FF));
                            }
                        }
                        //Se for dado (RS=0)
                        else
                        {
                            //Se ainda não rebeu o 1o nibble
                            if (!nibble1)
                            {
                                //Salva o nibble mais significativo
                                nibbleH = (byte)(valor & 0x00F0);   //Máscara de AND
                            }
                            //Se esse é o 2o nibble
                            else
                            {
                                //Monta o comando
                                valor = (ushort)(nibbleH + ((valor & 0x00F0) >> 4));
                                //Converte o valor em um caractere
                                char dado = Convert.ToChar(valor & 0x00FF);
                                //Escreve o caractere no LCD
                                status = EscreveCaractere(dado);
                                //Se é uma coluna e uma linha válidas
                                if (caractereSel[0] >= 0 && caractereSel[0] < colunas &&
                                    caractereSel[1] >= 0 && caractereSel[1] < linhas)
                                {
                                    //Remove o efeito do cursor neste caractere
                                    caractere[caractereSel[1], caractereSel[0]].BackColor = Color.Transparent;
                                }
                                //Faz o deslocamento automático
                                caractereSel[0]++;
                            }
                        }
                    }
                    //Inverte o estado o nibble mais significativo
                    nibble1 = !nibble1;
                }
            }

            //Atualiza Enable
            if ((valor & 0x0100) == 0) E = false;
            else E = true;

            return status;
        }
        #endregion

        //---------------------------------------------------------------------
        #region Métodos privados disparados por eventos------------------------
        private void LCDv2_Resize(object sender, EventArgs e)
        {
            CalculaTamanhoCaracteres();
            AtualizaTamanhoCaracteres();
        }

        private void tCursor_Tick(object sender, EventArgs e)
        {
            //Se o cursor está ligado
            if (cursor)
            {
                //Se é uma coluna e uma linha válidas
                if (caractereSel[0] >= 0 && caractereSel[0] < colunas &&
                    caractereSel[1] >= 0 && caractereSel[1] < linhas)
                {
                    //Pisca-pisca
                    if (caractere[caractereSel[1], caractereSel[0]].BackColor == Color.Transparent)
                        caractere[caractereSel[1], caractereSel[0]].BackColor = CorCaractere;
                    else
                        caractere[caractereSel[1], caractereSel[0]].BackColor = Color.Transparent;
                }
            }
            else
            {
                caractere[caractereSel[1], caractereSel[0]].BackColor = Color.Transparent;
                tCursor.Enabled = false;
            }
        }
        #endregion
    }
}
