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
    public partial class FormTicks : Form
    {
        int ticks_us;
        int ticks;
        bool modo_turbo;        //Criado na versão 3.3
        fPrincipal fprincipal;

        public FormTicks(fPrincipal form)
        {
            InitializeComponent();

            //Passa o form principal (pai) como referência para poder acessar a variável
            fprincipal = form;
            ticks = fprincipal.ticksThread;

            try
            {
                ticks_us = ticks / 10;              //ticks_atual é múltiplo de 100ns
                tbarTicks.Value = ticks_us / 100;
                tbTicks.Text = ticks_us.ToString();
            }
            catch
            { }

            modo_turbo = fprincipal.turbo; //Criado na versão 3.3
            if (modo_turbo)
                cbTurbo.Checked = true;
            else
                cbTurbo.Checked = false;
        }

        private void tbarTicks_Scroll(object sender, EventArgs e)
        {
            tbTicks.Text = (tbarTicks.Value * 100).ToString();
        }

        private void tbTicks_TextChanged(object sender, EventArgs e)
        {
            try
            {
                ticks_us = int.Parse(tbTicks.Text);
                tbarTicks.Value = ticks_us / 100;
            }
            catch
            { }
        }

        private void bCancelar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void bOK_Click(object sender, EventArgs e)
        {
            if (ticks_us < 100)
            { ticks_us = 100; tbTicks.Text = "100"; tbarTicks.Value = 1; }
            else if (ticks_us > 2000)
            { ticks_us = 2000; tbTicks.Text = "2000"; tbarTicks.Value = 20; }

            ticks = ticks_us * 10;
            fprincipal.ticksThread = ticks;

            if (cbTurbo.Checked)
                fprincipal.turbo = true;
            else
                fprincipal.turbo = false;

            this.DialogResult = DialogResult.OK;

            this.Close();
        }
    }
}
