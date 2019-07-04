using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Simulador_KIT_SENAI_PIC16_v3_3
{
    public partial class FormConversor : Form
    {
        public FormConversor()
        {
            InitializeComponent();
        }

        private void bConverter_Click(object sender, EventArgs e)
        {
            if (rbDecimal.Checked) ConverteDeDecimal();
            else if (rbBin.Checked) ConverteDeBin();
            else if (rbHexa.Checked) ConverteDeHexa();
            else if (rbASCII.Checked) ConverteDeASCII();
        }

        private void ConverteDeDecimal()
        {
            try
            {
                //Converte para decimal
                int dec = int.Parse(tbValor.Text);

                //Ajuste do limite em 16 bits
                dec = dec & 0xFFFF;
                tbValor.Text = dec.ToString();

                //Converte de decimal para todas as bases
                ConverteDeDecimalParaTodasAsBases(dec);
            }
            catch
            {
                tbValor.Clear();
                tbDecimal.Clear();
                tbBin.Clear();
                tbHexa.Clear();
                tbASCII.Clear();
            }
            finally
            {
                //Força o foco do TextBox principal
                tbValor.Focus();
            }
        }

        private void ConverteDeBin()
        {
            try
            {
                //Ajuste de limite em 16 bits
                if (tbValor.TextLength > 16)
                    tbValor.Text = tbValor.Text.Substring(tbValor.TextLength - 16, 16);

                //Converte para decimal
                int dec = Convert.ToInt32(tbValor.Text, 2);

                //Converte de decimal para todas as bases
                ConverteDeDecimalParaTodasAsBases(dec);
            }
            catch
            {
                tbValor.Clear();
                tbDecimal.Clear();
                tbBin.Clear();
                tbHexa.Clear();
                tbASCII.Clear();
            }
            finally
            {
                //Força o foco do TextBox principal
                tbValor.Focus();
            }
        }

        private void ConverteDeHexa()
        {
            try
            {
                //Ajuste de limite em 16 bits
                if (tbValor.TextLength > 4)
                    tbValor.Text = tbValor.Text.Substring(tbValor.TextLength - 4, 4);

                //Converte para decimal
                int dec = Convert.ToInt32(tbValor.Text, 16);

                //Converte de decimal para todas as bases
                ConverteDeDecimalParaTodasAsBases(dec);
            }
            catch
            {
                tbValor.Clear();
                tbDecimal.Clear();
                tbBin.Clear();
                tbHexa.Clear();
                tbASCII.Clear();
            }
            finally
            {
                //Força o foco do TextBox principal
                tbValor.Focus();
            }
        }

        private void ConverteDeASCII()
        {
            try
            {
                //Ajuste do limite em 1 caractere
                tbValor.Text = tbValor.Text.Substring(tbValor.TextLength - 1, 1);

                //Converte para decimal
                char carac = Convert.ToChar(tbValor.Text);
                int dec = Convert.ToInt32(carac);

                //Converte de decimal para todas as bases
                ConverteDeDecimalParaTodasAsBases(dec);
            }
            catch
            {
                tbValor.Clear();
                tbDecimal.Clear();
                tbBin.Clear();
                tbHexa.Clear();
                tbASCII.Clear();
            }
            finally
            {
                //Força o foco do TextBox principal
                tbValor.Focus();
            }
        }

        private void ConverteDeDecimalParaTodasAsBases(int valor)
        {
            int dec = valor;

            //Realiza as conversões
            string bin = Convert.ToString(dec, 2);
            string hexa = Convert.ToString(dec, 16);
            string ascii = Convert.ToChar(dec).ToString();

            //Ajuste binário (de byte em byte)
            if (bin.Length < 8)
            {
                while (bin.Length < 8)
                {
                    bin = "0" + bin;
                }
            }
            else if ((bin.Length > 8) && (bin.Length < 16))
            {
                while (bin.Length < 16)
                {
                    bin = "0" + bin;
                }
            }

            //Ajuste hexa (maiúsculo de byte em byte)
            if ((hexa.Length == 1) || (hexa.Length == 3))
            {
                hexa = "0" + hexa;
            }
            hexa = hexa.ToUpper();

            //Mostra os resultados
            tbDecimal.Text = dec.ToString();
            tbBin.Text = bin;
            tbHexa.Text = hexa;
            tbASCII.Text = ascii;

            //Força o foco do TextBox principal
            tbValor.Focus();
        }

        private void FormConversor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                //Força o click do botão
                bConverter.PerformClick();
                //Evento manipulado
                e.Handled = true;
            }
        }
    }
}
