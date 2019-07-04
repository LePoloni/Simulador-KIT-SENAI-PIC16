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
    public partial class FormPeriféricos : Form
    {
        public FormPeriféricos()
        {
            InitializeComponent();
        }

        private void FormPeriféricos_Load(object sender, EventArgs e)
        {
            string[,] perif = new string[16, 4] 
            {{"Externo", "LEDs",                        "1", "PORTD pinos de 0 a 7, PORTA pino 5"},
            {"Externo",	"Displays de 7 Segmentos",	    "1", "PORTD pinos de 0 a 7, PORTE pinos 0 a 2"},
            {"Externo",	"LCD 16x2",	                    "1", "PORTD pinos de 0 a 6, PORTA pino 5"},
            {"Externo",	"Botões",	            "1", "PORTB  pinos 0 e 1"},
            {"Externo",	"Teclado Matricial",	"1", "PORTB pinos de 2 a 7"},
            {"Externo",	"Porta Serial USART",	"1", "Somente modo assíncrono, PORTC pinos 6 e 7"},
            {"Externo",	"Entradas Analógiacas",	"1", "PORTA pinos 0 e 1"},
            {"Interno",	"I/O Ports",	        "1", ""},
            {"Interno",	"Timer0",	            "1", "Somente modo temporizador, configure através do registrador OPTION_REG"},
            {"Interno",	"Timer1",	            "0", ""},
            {"Interno",	"Timer2",	            "0", ""},
            {"Interno",	"CCP",	                "0", ""},
            {"Interno",	"SPI",	                "0", ""},
            {"Interno",	"CAD",	                "1", "Somente entradas AN0 e AN1 com VREF+ = VDD e VREF- = VSS"},
            {"Interno",	"Comparador",           "0", ""},
            {"Interno",	"Interrupções",         "1", "Somente TIMER0 e INT (RB0)"}};

            //Define a quantidade de linhas
            dgPeriféricos.RowCount = 16;


            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    if (j != 2)
                    {
                        //Tabela.Linha[?].Coluna[?].Valor = valor desejado
                        dgPeriféricos.Rows[i].Cells[j].Value = perif[i, j];
                    }
                    else
                    {
                        bool imp;
                        if (perif[i, j] == "1") imp = true;
                        else imp = false;
                        dgPeriféricos.Rows[i].Cells[j].Value = imp;
                    }
                }
            }
        }
    }
}
