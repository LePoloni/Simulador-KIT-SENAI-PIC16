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
    public partial class FormSobre : Form
    {
        public FormSobre()
        {
            InitializeComponent();
        }

        private void linklMaisInfo_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FormInfo fInfo = new FormInfo();
            fInfo.ShowDialog();
        }
    }
}
