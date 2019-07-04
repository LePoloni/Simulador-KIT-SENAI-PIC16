//---------------------------------------------------------------------
#region Diretinvas do pré-processador
//#define ModoString
#define ModoBinario
//#define TesteThread
//#define DebugLCD
#endregion

//---------------------------------------------------------------------
#region Cabeçalho e Observações
/*
 * Entradas analógicas funcionam apenas no ModoBinario
 * -Versão 3.2
 *      - Corrigi o comando para ligar o cursor piscante, troquei o 0Eh por 0Dh.
 *        No momento essa é a única possibilidade para o cursor piscar ou desligar (0Ch).
 *      - Graças ao professor Renato, pude verificar falahas no funcionamento do LCD 
 *      bem como a necessidade de implementar o funcionamento do registradores PCL e PCLATH 
 *      usados pelo program counter, além do acesso indireto a memória RAM feito pelos 
 *      registradores FSR e INDF.
 *      - Pequena correção no BANK1 posição 0x10 estáva aparecendo v10.
 *      - Pequena correção no BANK3 Option_Reg estava inicializando com 0x00 ao invés de 0xFF.
 *      - Implementação das interrupções de Timer0 e Int(RB0).
 *      - Correção da cor do taclado e cr de fundo das labels presentes sobre a imagem do KIT.
 * -Versão 3.3
 *      - Corrigi a apresentação dos bits de configuração, estavam 2 bits deslocados para esquerda
 *      resultando em interpretação errada.
 *      - Criei o modo turbo para execusão das instruções com máximo desempenho. Pelas medidas feitas
 *      alguns códigos de exemplo atingiram desempenho até 10x superior ao do microcontrolador.
 *       Em casos onde se utiliza periféricos com tempo de resposta alto como (LCD) o aumento no desempenho 
 *      não chega a se aproximar ao do microcontrolador com clock de 4 MHz.
 *       Para manter a compatibilidade com o dispositivo real, limitei em desempenho há aproximadamente 82% do real.
 */
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
//Incluido manualmente
using System.Collections;
using System.Threading;
using System.Diagnostics;

namespace Simulador_KIT_SENAI_PIC16_v3_3
{
    public partial class fPrincipal : Form
    {
        //---------------------------------------------------------------------
        #region Variáveis e Objetos--------------------------------------------
        string titulo = "Simulador KIT SENAI PIC16 e Interpretador de Arquivos Intel Hex - V3.3";

        string binreg = "";
        string dest = "";
        string constant = "";
        string mn = "";
        string fa = "";
        int timer0 = 0;
        uint ticks = 0;

        Stack picStack;

        int PC = 0, Atual = 0, selBank = 0;
        string sWork = "00000000";
        string sStatus = "00011000";

        //Flags para refresh de registradores
        bool refreshWork = false;
        bool refreshStatus = false;
        bool refreshReg = false;
        bool refreshStack_push = false;
        bool refreshStack_pop = false;
        bool refreshIntcon = false;

        string[,] memoria;
        string[, ,] bank;
        int linhas = 0;

        //Análise de código binária
        int[] memoria_bin;  //Endereços da ROM
        int[,] bank_bin;    //Banco,Endereço da RAM
        int Work_bin = 0;   //Registrador W
        int Status_bin = 0x18;  //Registrados Status - "atenção esse registrador altera o banco selecionado"
        int PORTA_ant_bin = 0;
        int PORTD_ant_bin = 0;
        int PORTE_ant_bin = 0;

        bool TMR0 = false;
        int PS = 256;

        string PORTA_ant = "0";
        string PORTD_ant = "0";
        string PORTE_ant = "0";

        int abertura = 0;

        /// <summary>
        /// Define o valor anterior de RB0 para fins de interrupção por borda.
        /// Como as chaves abertas representam nível lógico 1,  o estado inicial é 1.
        /// </summary>
        int RB0_ant = 1;    //08/09/16

        #endregion

        //---------------------------------------------------------------------
        #region Método Construtor----------------------------------------------
        /// <summary>
        /// Construtor da classe, define consdições iniciais
        /// </summary>
        public fPrincipal()
        {
            InitializeComponent();

            //Carrega a título padrão
            this.Text = titulo;

            //Limpa todos os dados e já carrega os valores iniciais dos bancos
            Limpar();

            //Bloco do esquema elétrico padrão
            cbEsquemas.SelectedIndex = 0;

            //Período entre cada instrução executada
            lPeriodo.Text = tbPeriodo.Value.ToString() + " ms";

            //Desabilita Timer0 e Entradas Analógicas
            gbTimer0.Enabled = false;
            gbEA.Enabled = false;
        }
        #endregion

        //---------------------------------------------------------------------
        #region Métodos Privados Auxiliares------------------------------------
        /// <summary>
        /// Limpa todos os dados e já carrega os valores iniciais dos bancos.
        /// </summary>
        /// <returns></returns>
        private void Limpar()
        {
            //Reseta o microcontrolador e para o processamento de instruções
            Reset();

            //Atualiza estado dos controles
            this.Text = titulo;

            //Limpa os datagrids da aba Intel Hex
            dgvIntelHex.Rows.Clear();
            dgvTradutor.Rows.Clear();
            dgvConfigBits.Rows.Clear();

            //Apaga toda memória de programa (ROM)
            dgvMemoria.Rows.Clear();
            mn = "NOP";         //MNEMÔNICOS COMO NOP NA INICIALIZAÇÃO
            dest = "";          //VAZIO NA INICIALIZAÇÃO
            binreg = "";        //VAZIO NA INICIALIZAÇÃO
            constant = "";      //VAZIO NA INICIALIZAÇÃO
            dgvMemoria.RowsDefaultCellStyle.BackColor = Color.LightGray; //COLORAÇÃO CINZA INDICANDO AUSÊNCIA DE DADOS
            for (int c = 0; c < 8192; c++)
            {
                fa = c.ToString();
                //while (fa.ToString().Length < 5) fa = "0" + fa;
                dgvMemoria.Rows.Add((fa), (mn), (dest), (binreg), (" "), (constant)); //ALOCAÇÃO DAS VARIÁVEIS
            }
            dgvMemoria.CurrentCell = dgvMemoria.Rows[PC].Cells[0];  //Posiona a seleção na linha inicial
        }
        /// <summary>
        /// Reseta o microcontrolador e para o processamento de instruções.
        /// </summary>
        private void Reset()
        {
            //Para a Thread
            if (threadRun != null)
            {
                pararThread = true;
            }
            //Zera variáveis de controle
            ticks = 0;
            PC = 0;
            picStack = new Stack(8);
            picStack.Clear();

            //Atualiza estado dos controles
            clock.Enabled = false;
            cbExecutar.Checked = false;
            cbResetar.Checked = false;
            bBuscar.Enabled = true;
            tbBuscar.Enabled = true;
            bExecPasso.Enabled = true;
            bExecutar.Text = "Executar";
            dgvMemoria.Enabled = true;
            lTicks.Text = "0";

            //Apaga todos os leds
            ucLED0.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;
            ucLED1.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;
            ucLED2.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;
            ucLED3.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;
            ucLED4.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;
            ucLED5.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;
            ucLED6.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;
            ucLED7.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;
            //Apaga os 3 displays
            ucDisp1.ValorBinario(0);
            ucDisp2.ValorBinario(0);
            ucDisp3.ValorBinario(0);
            //Limpa o LCD e desliga o cursor
            ucLCD.Limpa();
            ucLCD.PiscarCursor = false;

            //Limpa os datagrids da aba Memórias e carreaga valores iniciais
            //Limpa o stack (reset)
            dgvStack.Rows.Clear();
            //Limpa os bancos (RAM)
            dgvBank0.Rows.Clear();
            dgvBank1.Rows.Clear();
            dgvBank2.Rows.Clear();
            dgvBank3.Rows.Clear();
            //Limpa a memória de programa (ROM)
            //dgvMemoria.Rows.Clear();  //O reset não altera a ROM
            //Limpa o registrador de status
            dgvStatusReg.Rows.Clear();
            //Limpa o registrador Work
            dgvWork.Rows.Clear();

            //Carrega valor inicial d registrador de status
            dgvStatusReg.Rows.Insert(0, "00011000");
            //Carrega valor inicial d registrador Work
            dgvWork.Rows.Insert(0, "00000000");

            //Carrega valor inicial dos bancos de registradores (RAM)
            Bancos();
            //Posiona a seleção na linha inicial
            dgvBank0.CurrentCell = dgvBank0.Rows[0].Cells[0];
            dgvBank1.CurrentCell = dgvBank1.Rows[0].Cells[0];
            dgvBank2.CurrentCell = dgvBank2.Rows[0].Cells[0];
            dgvBank3.CurrentCell = dgvBank3.Rows[0].Cells[0];

            //Posiona a seleção na linha inicial da memória de programa (ROM)
            if (dgvMemoria.Rows.Count > 0)
                dgvMemoria.CurrentCell = dgvMemoria.Rows[PC].Cells[0];
        }
        /// <summary>
        /// Inicializa os DataGridViews Banks com os valores iniciais de cada registrador.
        /// </summary>
        private void Bancos()
        {
            #region Versão antiga (hexa --> ??h)
            //#region BANK0 //////////////////////////////////////////////////////////////////////////////////////////////////////////
            //dgvBank0.Rows.Insert(0, "00h", "INDIRECT", "00000000");
            //dgvBank0.Rows.Insert(1, "01h", "TMR0", "00000000");
            //dgvBank0.Rows.Insert(2, "02h", "PCL", "00000000");
            //dgvBank0.Rows.Insert(3, "03h", "STATUS", "00011000");
            //dgvBank0.Rows.Insert(4, "04h", "FSR", "00000000");
            //dgvBank0.Rows.Insert(5, "05h", "PORTA", "00000000");
            //dgvBank0.Rows.Insert(6, "06h", "PORTB", "00011111"); //0, 1, 2, 3, E 4 EM 1 POIS OS BOTÕES E O TECLADO MATRICIAL ATIVAM EM 0
            //dgvBank0.Rows.Insert(7, "07h", "PORTC", "00000000");
            //dgvBank0.Rows.Insert(8, "08h", "PORTD", "00000000");
            //dgvBank0.Rows.Insert(9, "09h", "PORTE", "00000000");
            //dgvBank0.Rows.Insert(10, "0Ah", "PCLATH", "00000000");
            //dgvBank0.Rows.Insert(11, "0Bh", "INTCON", "00000000");
            //dgvBank0.Rows.Insert(12, "0Ch", "PIR1", "00000000");
            //dgvBank0.Rows.Insert(13, "0Dh", "PIR2", "00000000");
            //dgvBank0.Rows.Insert(14, "0Eh", "TMR1L", "00000000");
            //dgvBank0.Rows.Insert(15, "0Fh", "TMR2H", "00000000");
            //dgvBank0.Rows.Insert(16, "10h", "T1CON", "00000000");
            //dgvBank0.Rows.Insert(17, "11h", "TMR2", "00000000");
            //dgvBank0.Rows.Insert(18, "12h", "T2CON", "00000000");
            //dgvBank0.Rows.Insert(19, "13h", "SSPBUF", "00000000");
            //dgvBank0.Rows.Insert(20, "14h", "SSPCON", "00000000");
            //dgvBank0.Rows.Insert(21, "15h", "CCPR1L", "00000000");
            //dgvBank0.Rows.Insert(22, "16h", "CCPR1H", "00000000");
            //dgvBank0.Rows.Insert(23, "17h", "CCP1CON", "00000000");
            //dgvBank0.Rows.Insert(24, "18h", "RCSTA", "00000000");
            //dgvBank0.Rows.Insert(25, "19h", "TXREG", "00000000");
            //dgvBank0.Rows.Insert(26, "1Ah", "RCREG", "00000000");
            //dgvBank0.Rows.Insert(27, "1Bh", "CCPR2L", "00000000");
            //dgvBank0.Rows.Insert(28, "1Ch", "CCPR2H", "00000000");
            //dgvBank0.Rows.Insert(29, "1Dh", "CCP2CON", "00000000");
            //dgvBank0.Rows.Insert(30, "1Eh", "ADRESH", "00000000");
            //dgvBank0.Rows.Insert(31, "1Fh", "ADCON0", "00000000");

            //for (int i = 32; i < 128; i++)
            //{
            //    string h = Convert.ToString(i, 16).ToUpper();
            //    dgvBank0.Rows.Insert(i, h + "h", "Register", "00000000");
            //}

            //#endregion

            //#region BANK1 //////////////////////////////////////////////////////////////////////////////////////////////////////////
            //dgvBank1.Rows.Insert(0, "00h", "INDIRECT", "00000000");
            //dgvBank1.Rows.Insert(1, "01h", "OPTION_REG", "11111111");
            //dgvBank1.Rows.Insert(2, "02h", "PCL", "00000000");
            //dgvBank1.Rows.Insert(3, "03h", "STATUS", "00011000");
            //dgvBank1.Rows.Insert(4, "04h", "FSR", "00000000");
            //dgvBank1.Rows.Insert(5, "05h", "TRISA", "00111111");
            //dgvBank1.Rows.Insert(6, "06h", "TRISB", "11111111");
            //dgvBank1.Rows.Insert(7, "07h", "TRISC", "11111111");
            //dgvBank1.Rows.Insert(8, "08h", "TRISD", "11111111");
            //dgvBank1.Rows.Insert(9, "09h", "TRISE", "00000111");
            //dgvBank1.Rows.Insert(10, "0Ah", "PCLATH", "00000000");
            //dgvBank1.Rows.Insert(11, "0Bh", "INTCON", "00000000");
            //dgvBank1.Rows.Insert(12, "0Ch", "PIE1", "00000000");
            //dgvBank1.Rows.Insert(13, "0Dh", "PIE2", "00000000");
            //dgvBank1.Rows.Insert(14, "0Eh", "PCON", "00000000");
            //dgvBank1.Rows.Insert(15, "0Fh", "", "00000000");
            //dgvBank1.Rows.Insert(16, "10h", "", "00000000");
            //dgvBank1.Rows.Insert(17, "11h", "SSPCON2", "00000000");
            //dgvBank1.Rows.Insert(18, "12h", "PR2", "11111111");
            //dgvBank1.Rows.Insert(19, "13h", "SSPADD", "00000000");
            //dgvBank1.Rows.Insert(20, "14h", "SSPSTAT", "00000000");
            //dgvBank1.Rows.Insert(21, "15h", "", "00000000");
            //dgvBank1.Rows.Insert(22, "16h", "", "00000000");
            //dgvBank1.Rows.Insert(23, "17h", "", "00000000");
            //dgvBank1.Rows.Insert(24, "18h", "TXSTA", "00000010");
            //dgvBank1.Rows.Insert(25, "19h", "SPBRG", "00000000");
            //dgvBank1.Rows.Insert(26, "1Ah", "", "00000000");
            //dgvBank1.Rows.Insert(27, "1Bh", "", "00000000");
            //dgvBank1.Rows.Insert(28, "1Ch", "CMCON", "00000111");
            //dgvBank1.Rows.Insert(29, "1Dh", "CVRCON", "00000000");
            //dgvBank1.Rows.Insert(30, "1Eh", "ADRESL", "00000000");
            //dgvBank1.Rows.Insert(31, "1Fh", "ADCON1", "00000000");
            //for (int i = 32; i < 128; i++)
            //{
            //    string h = Convert.ToString(i, 16).ToUpper();
            //    dgvBank1.Rows.Insert(i, h + "h", "Register", "00000000");
            //}
            //#endregion

            //#region BANK2 //////////////////////////////////////////////////////////////////////////////////////////////////////////
            //dgvBank2.Rows.Insert(0, "00h", "INDIRECT", "00000000");
            //dgvBank2.Rows.Insert(1, "01h", "TMR0", "00000000");
            //dgvBank2.Rows.Insert(2, "02h", "PCL", "00000000");
            //dgvBank2.Rows.Insert(3, "03h", "STATUS", "00011000");
            //dgvBank2.Rows.Insert(4, "04h", "FSR", "00000000");
            //dgvBank2.Rows.Insert(5, "05h", "UNIMPLEMENTED", "00000000");
            //dgvBank2.Rows.Insert(6, "06h", "PORTB", "00000000");
            //dgvBank2.Rows.Insert(7, "07h", "UNIMPLEMENTED", "00000000");
            //dgvBank2.Rows.Insert(8, "08h", "UNIMPLEMENTED", "00000000");
            //dgvBank2.Rows.Insert(9, "09h", "UNIMPLEMENTED", "00000000");
            //dgvBank2.Rows.Insert(10, "0A", "PCLATH", "00000000");
            //dgvBank2.Rows.Insert(11, "0Bh", "INTCON", "00000000");
            //dgvBank2.Rows.Insert(12, "0Ch", "EEDATA", "00000000");
            //dgvBank2.Rows.Insert(13, "0Dh", "EEADR", "00000000");
            //dgvBank2.Rows.Insert(14, "0Eh", "EEDATH", "00000000");
            //dgvBank2.Rows.Insert(15, "0Fh", "EEADRH", "00000000");
            //for (int i = 16; i < 128; i++)
            //{
            //    string h = Convert.ToString(i, 16).ToUpper();
            //    dgvBank2.Rows.Insert(i, h + "h", "Register", "00000000");
            //}
            //#endregion

            //#region BANK3 //////////////////////////////////////////////////////////////////////////////////////////////////////////
            //dgvBank3.Rows.Insert(0, "00h", "INDIRECT", "00000000");
            //dgvBank3.Rows.Insert(1, "01h", "OPTION_REG", "00000000");
            //dgvBank3.Rows.Insert(2, "02h", "PCL", "00000000");
            //dgvBank3.Rows.Insert(3, "03h", "STATUS", "00011000");
            //dgvBank3.Rows.Insert(4, "04h", "FSR", "00000000");
            //dgvBank3.Rows.Insert(5, "05h", "UNIMPLEMENTED", "00000000");
            //dgvBank3.Rows.Insert(6, "06h", "TRISB", "11111111");
            //dgvBank3.Rows.Insert(7, "07h", "UNIMPLEMENTED", "00000000");
            //dgvBank3.Rows.Insert(8, "08h", "UNIMPLEMENTED", "00000000");
            //dgvBank3.Rows.Insert(9, "09h", "UNIMPLEMENTED", "00000000");
            //dgvBank3.Rows.Insert(10, "0A", "PCLATH", "00000000");
            //dgvBank3.Rows.Insert(11, "0Bh", "INTCON", "00000000");
            //dgvBank3.Rows.Insert(12, "0Ch", "EECON1", "00000000");
            //dgvBank3.Rows.Insert(13, "0Dh", "EECON2", "00000000");
            //dgvBank3.Rows.Insert(14, "0Eh", "RESERVED", "00000000");
            //dgvBank3.Rows.Insert(15, "0Fh", "RESERVED", "00000000");
            //for (int i = 16; i < 128; i++)
            //{
            //    string h = Convert.ToString(i, 16).ToUpper();
            //    dgvBank3.Rows.Insert(i, h + "h", "Register", "00000000");
            //}
            //#endregion
            #endregion

            #region BANK0 //////////////////////////////////////////////////////////////////////////////////////////////////////////
            dgvBank0.Rows.Insert(0, "0x00", "INDIRECT", "00000000");
            dgvBank0.Rows.Insert(1, "0x01", "TMR0", "00000000");
            dgvBank0.Rows.Insert(2, "0x02", "PCL", "00000000");
            dgvBank0.Rows.Insert(3, "0x03", "STATUS", "00011000");
            dgvBank0.Rows.Insert(4, "0x04", "FSR", "00000000");
            dgvBank0.Rows.Insert(5, "0x05", "PORTA", "00000000");
            dgvBank0.Rows.Insert(6, "0x06", "PORTB", "00011111"); //0, 1, 2, 3, E 4 EM 1 POIS OS BOTÕES E O TECLADO MATRICIAL ATIVAM EM 0
            dgvBank0.Rows.Insert(7, "0x07", "PORTC", "00000000");
            dgvBank0.Rows.Insert(8, "0x08", "PORTD", "00000000");
            dgvBank0.Rows.Insert(9, "0x09", "PORTE", "00000000");
            dgvBank0.Rows.Insert(10, "0x0A", "PCLATH", "00000000");
            dgvBank0.Rows.Insert(11, "0x0B", "INTCON", "00000000");
            dgvBank0.Rows.Insert(12, "0x0C", "PIR1", "00000000");
            dgvBank0.Rows.Insert(13, "0x0D", "PIR2", "00000000");
            dgvBank0.Rows.Insert(14, "0x0E", "TMR1L", "00000000");
            dgvBank0.Rows.Insert(15, "0x0F", "TMR2H", "00000000");
            dgvBank0.Rows.Insert(16, "0x10", "T1CON", "00000000");
            dgvBank0.Rows.Insert(17, "0x11", "TMR2", "00000000");
            dgvBank0.Rows.Insert(18, "0x12", "T2CON", "00000000");
            dgvBank0.Rows.Insert(19, "0x13", "SSPBUF", "00000000");
            dgvBank0.Rows.Insert(20, "0x14", "SSPCON", "00000000");
            dgvBank0.Rows.Insert(21, "0x15", "CCPR1L", "00000000");
            dgvBank0.Rows.Insert(22, "0x16", "CCPR1H", "00000000");
            dgvBank0.Rows.Insert(23, "0x17", "CCP1CON", "00000000");
            dgvBank0.Rows.Insert(24, "0x18", "RCSTA", "00000000");
            dgvBank0.Rows.Insert(25, "0x19", "TXREG", "00000000");
            dgvBank0.Rows.Insert(26, "0x1A", "RCREG", "00000000");
            dgvBank0.Rows.Insert(27, "0x1B", "CCPR2L", "00000000");
            dgvBank0.Rows.Insert(28, "0x1C", "CCPR2H", "00000000");
            dgvBank0.Rows.Insert(29, "0x1D", "CCP2CON", "00000000");
            dgvBank0.Rows.Insert(30, "0x1E", "ADRESH", "00000000");
            dgvBank0.Rows.Insert(31, "0x1F", "ADCON0", "00000000");

            for (int i = 32; i < 128; i++)
            {
                string h = Convert.ToString(i, 16).ToUpper();
                dgvBank0.Rows.Insert(i, "0x" + h, "Register", "00000000");
            }

            #endregion

            #region BANK1 //////////////////////////////////////////////////////////////////////////////////////////////////////////
            dgvBank1.Rows.Insert(0, "0x00", "INDIRECT", "00000000");
            dgvBank1.Rows.Insert(1, "0x01", "OPTION_REG", "11111111");
            dgvBank1.Rows.Insert(2, "0x02", "PCL", "00000000");
            dgvBank1.Rows.Insert(3, "0x03", "STATUS", "00011000");
            dgvBank1.Rows.Insert(4, "0x04", "FSR", "00000000");
            dgvBank1.Rows.Insert(5, "0x05", "TRISA", "00111111");
            dgvBank1.Rows.Insert(6, "0x06", "TRISB", "11111111");
            dgvBank1.Rows.Insert(7, "0x07", "TRISC", "11111111");
            dgvBank1.Rows.Insert(8, "0x08", "TRISD", "11111111");
            dgvBank1.Rows.Insert(9, "0x09", "TRISE", "00000111");
            dgvBank1.Rows.Insert(10, "0x0A", "PCLATH", "00000000");
            dgvBank1.Rows.Insert(11, "0x0B", "INTCON", "00000000");
            dgvBank1.Rows.Insert(12, "0x0C", "PIE1", "00000000");
            dgvBank1.Rows.Insert(13, "0x0D", "PIE2", "00000000");
            dgvBank1.Rows.Insert(14, "0x0E", "PCON", "00000000");
            dgvBank1.Rows.Insert(15, "0x0F", "", "00000000");
            dgvBank1.Rows.Insert(16, "0x10", "", "00000000");
            dgvBank1.Rows.Insert(17, "0x11", "SSPCON2", "00000000");
            dgvBank1.Rows.Insert(18, "0x12", "PR2", "11111111");
            dgvBank1.Rows.Insert(19, "0x13", "SSPADD", "00000000");
            dgvBank1.Rows.Insert(20, "0x14", "SSPSTAT", "00000000");
            dgvBank1.Rows.Insert(21, "0x15", "", "00000000");
            dgvBank1.Rows.Insert(22, "0x16", "", "00000000");
            dgvBank1.Rows.Insert(23, "0x17", "", "00000000");
            dgvBank1.Rows.Insert(24, "0x18", "TXSTA", "00000010");
            dgvBank1.Rows.Insert(25, "0x19", "SPBRG", "00000000");
            dgvBank1.Rows.Insert(26, "0x1A", "", "00000000");
            dgvBank1.Rows.Insert(27, "0x1B", "", "00000000");
            dgvBank1.Rows.Insert(28, "0x1C", "CMCON", "00000111");
            dgvBank1.Rows.Insert(29, "0x1D", "CVRCON", "00000000");
            dgvBank1.Rows.Insert(30, "0x1E", "ADRESL", "00000000");
            dgvBank1.Rows.Insert(31, "0x1F", "ADCON1", "00000000");
            for (int i = 32; i < 128; i++)
            {
                string h = Convert.ToString(i, 16).ToUpper();
                dgvBank1.Rows.Insert(i, "0x" + h, "Register", "00000000");
            }
            #endregion

            #region BANK2 //////////////////////////////////////////////////////////////////////////////////////////////////////////
            dgvBank2.Rows.Insert(0, "0x00", "INDIRECT", "00000000");
            dgvBank2.Rows.Insert(1, "0x01", "TMR0", "00000000");
            dgvBank2.Rows.Insert(2, "0x02", "PCL", "00000000");
            dgvBank2.Rows.Insert(3, "0x03", "STATUS", "00011000");
            dgvBank2.Rows.Insert(4, "0x04", "FSR", "00000000");
            dgvBank2.Rows.Insert(5, "0x05", "UNIMPLEMENTED", "00000000");
            dgvBank2.Rows.Insert(6, "0x06", "PORTB", "00000000");
            dgvBank2.Rows.Insert(7, "0x07", "UNIMPLEMENTED", "00000000");
            dgvBank2.Rows.Insert(8, "0x08", "UNIMPLEMENTED", "00000000");
            dgvBank2.Rows.Insert(9, "0x09", "UNIMPLEMENTED", "00000000");
            dgvBank2.Rows.Insert(10, "0x0A", "PCLATH", "00000000");
            dgvBank2.Rows.Insert(11, "0x0B", "INTCON", "00000000");
            dgvBank2.Rows.Insert(12, "0x0C", "EEDATA", "00000000");
            dgvBank2.Rows.Insert(13, "0x0D", "EEADR", "00000000");
            dgvBank2.Rows.Insert(14, "0x0E", "EEDATH", "00000000");
            dgvBank2.Rows.Insert(15, "0x0F", "EEADRH", "00000000");
            for (int i = 16; i < 128; i++)
            {
                string h = Convert.ToString(i, 16).ToUpper();
                dgvBank2.Rows.Insert(i, "0x" + h, "Register", "00000000");
            }
            #endregion

            #region BANK3 //////////////////////////////////////////////////////////////////////////////////////////////////////////
            dgvBank3.Rows.Insert(0, "0x00", "INDIRECT", "00000000");
            dgvBank3.Rows.Insert(1, "0x01", "OPTION_REG", "11111111");
            dgvBank3.Rows.Insert(2, "0x02", "PCL", "00000000");
            dgvBank3.Rows.Insert(3, "0x03", "STATUS", "00011000");
            dgvBank3.Rows.Insert(4, "0x04", "FSR", "00000000");
            dgvBank3.Rows.Insert(5, "0x05", "UNIMPLEMENTED", "00000000");
            dgvBank3.Rows.Insert(6, "0x06", "TRISB", "11111111");
            dgvBank3.Rows.Insert(7, "0x07", "UNIMPLEMENTED", "00000000");
            dgvBank3.Rows.Insert(8, "0x08", "UNIMPLEMENTED", "00000000");
            dgvBank3.Rows.Insert(9, "0x09", "UNIMPLEMENTED", "00000000");
            dgvBank3.Rows.Insert(10, "0x0A", "PCLATH", "00000000");
            dgvBank3.Rows.Insert(11, "0x0B", "INTCON", "00000000");
            dgvBank3.Rows.Insert(12, "0x0C", "EECON1", "00000000");
            dgvBank3.Rows.Insert(13, "0x0D", "EECON2", "00000000");
            dgvBank3.Rows.Insert(14, "0x0E", "RESERVED", "00000000");
            dgvBank3.Rows.Insert(15, "0x0F", "RESERVED", "00000000");
            for (int i = 16; i < 128; i++)
            {
                string h = Convert.ToString(i, 16).ToUpper();
                dgvBank3.Rows.Insert(i, "0x" + h, "Register", "00000000");
            }
            #endregion
        }
        /// <summary>
        /// Prepara matrizes de memória de programa e bancos de registradores para a execução.
        /// </summary>
        private void PreparaVetoresRAMeROM()
        {
            //Valor inicial do program counter PC
            PC = 0;
            //Lê memória ROM
            linhas = dgvMemoria.RowCount;
            memoria = new string[linhas + 1, 6];
            for (int i = 0; i < linhas; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    memoria[i, j] = dgvMemoria.Rows[i].Cells[j].Value.ToString();
                }
            }
            //Lê memória RAM
            bank = new string[4, 128, 3];   //Banco,Endereço,Campo

            for (int i = 0; i < 128; i++)   //Endereço
            {
                for (int j = 0; j < 3; j++)     //Campo
                {
                    bank[0, i, j] = dgvBank0.Rows[i].Cells[j].Value.ToString();
                    bank[1, i, j] = dgvBank1.Rows[i].Cells[j].Value.ToString();
                    bank[2, i, j] = dgvBank2.Rows[i].Cells[j].Value.ToString();
                    bank[3, i, j] = dgvBank3.Rows[i].Cells[j].Value.ToString();
                }
            }
        }
        /// <summary>
        /// Prepara matrizes de memória de programa e bancos de registradores para a execução no modo binário.
        /// A solução aqui empregado apenas facilita a rápica crição das memórias, mas não é a mais adequada.
        /// </summary>
        private void PreparaVetoresRAMeROM_Binario(ushort[,] rom)
        {
            //Valor inicial do program counter PC
            PC = 0;

            //Prepara a memória ROM
            //Calcula a quantidade de linhas
            linhas = rom.Length / 2 - 1;    //Ultima linha são os bits de configuração

            //Define o tamamho da memória de programa
            memoria_bin = new int[8192];

            //Preenche a memória com NOPs
            for (int i = 0; i < memoria_bin.Length; i++)
            {
                memoria_bin[i] = 0;
            }

            //Substitui os endereços com instruções definidas
            for (int i = 0; i < linhas; i++)
            {
                //          endereço     instrução
                memoria_bin[rom[i, 0]] = rom[i, 1];
            }

            //Prepara a memória RAM
            bank_bin = new int[4, 128];   //Banco,Endereço

            for (int i = 0; i < 128; i++)   //Endereço
            {
                bank_bin[0, i] = Convert.ToInt32(dgvBank0.Rows[i].Cells[2].Value.ToString(), 2);
                bank_bin[1, i] = Convert.ToInt32(dgvBank1.Rows[i].Cells[2].Value.ToString(), 2);
                bank_bin[2, i] = Convert.ToInt32(dgvBank2.Rows[i].Cells[2].Value.ToString(), 2);
                bank_bin[3, i] = Convert.ToInt32(dgvBank3.Rows[i].Cells[2].Value.ToString(), 2);
            }
        }
        /// <summary>
        /// Prepara matrizes de memória de programa e bancos de registradores para a execução no modo binário.
        /// A solução aqui empregado apenas facilita a rápica crição das memórias, mas não é a mais adequada.
        /// </summary>
        private void PreparaVetoresRAMeROM_Binario()
        {
            //Valor inicial do program counter PC
            PC = 0;

            //Limpa a memória ROM
            //Define o tamamho da memória de programa
            memoria_bin = new int[8192];

            //Preenche a memória com NOPs
            for (int i = 0; i < memoria_bin.Length; i++)
            {
                memoria_bin[i] = 0;
            }

            //Lê memória RAM
            bank_bin = new int[4, 128];   //Banco,Endereço

            for (int i = 0; i < 128; i++)   //Endereço
            {
                bank_bin[0, i] = Convert.ToInt32(dgvBank0.Rows[i].Cells[2].Value.ToString(), 2);
                bank_bin[1, i] = Convert.ToInt32(dgvBank1.Rows[i].Cells[2].Value.ToString(), 2);
                bank_bin[2, i] = Convert.ToInt32(dgvBank2.Rows[i].Cells[2].Value.ToString(), 2);
                bank_bin[3, i] = Convert.ToInt32(dgvBank3.Rows[i].Cells[2].Value.ToString(), 2);
            }
        }
        /// <summary>
        /// Prepara matrizes de bancos de registradores para a execução no modo binário.
        /// </summary>
        private void PreparaVetoresRAM_Binario()
        {
            //Valor inicial do program counter PC
            PC = 0;

            //Lê memória RAM
            bank_bin = new int[4, 128];   //Banco,Endereço

            for (int i = 0; i < 128; i++)   //Endereço
            {
                bank_bin[0, i] = Convert.ToInt32(dgvBank0.Rows[i].Cells[2].Value.ToString(), 2);
                bank_bin[1, i] = Convert.ToInt32(dgvBank1.Rows[i].Cells[2].Value.ToString(), 2);
                bank_bin[2, i] = Convert.ToInt32(dgvBank2.Rows[i].Cells[2].Value.ToString(), 2);
                bank_bin[3, i] = Convert.ToInt32(dgvBank3.Rows[i].Cells[2].Value.ToString(), 2);
            }
        }
        /// <summary>
        /// Converte valor inteiro em string binária com 8 bits
        /// </summary>
        /// <param name="valor">Valor inteiro para conversão.</param>
        /// <returns>String binária com 8 bits.</returns>
        private string IntToBinString(int valor)
        {
            string bin = Convert.ToString(valor, 2);
            while (bin.Length < 8)
                bin = "0" + bin;
            return bin;
        }
        #endregion

        //---------------------------------------------------------------------
        #region Métodos Privados Disparados pelo Form--------------------------
        /// <summary>
        /// FORÇA CARREGAMENTO DE TODAS AS TABS DO TABCONTROL NA MEMÓRIA
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fPrincipal_Load(object sender, EventArgs e)
        {
            //Força o carregamento de todas as Tabs do tabControl na memória
            //Sem isso a execusão do programa diretamente pela Tab Simulador não funciona e
            //seria necessário ir até a Tab Memória para efetivar a carga do programa.
            tabControl1.SelectedIndex = 0;
            tabControl1.SelectedIndex = 1;
            tabControl1.SelectedIndex = 2;
            tabControl1.SelectedIndex = 3;
            tabControl1.SelectedIndex = 0;

            timerAbertura.Enabled = true;
        }
        /// <summary>
        /// Interrompe a Thread caso estaja em execução durante o encerramento do programa.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fPrincipal_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (threadRun != null)
            {
                pararThread = true;
            }
        }
        /// <summary>
        /// Mensagem de Abertura
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timerAbertura_Tick(object sender, EventArgs e)
        {
            menuStrip1.Enabled = false;
            switch (abertura)
            {
                case 0: ucLCD.Limpa(); break;
                case 1: ucLCD.SelecionaEndereço(0x83); ucLCD.EscreveCaractere('S'); break;
                case 2: ucLCD.SelecionaEndereço(0x84); ucLCD.EscreveCaractere('I'); break;
                case 3: ucLCD.SelecionaEndereço(0x85); ucLCD.EscreveCaractere('M'); break;
                case 4: ucLCD.SelecionaEndereço(0x86); ucLCD.EscreveCaractere('U'); break;
                case 5: ucLCD.SelecionaEndereço(0x87); ucLCD.EscreveCaractere('L'); break;
                case 6: ucLCD.SelecionaEndereço(0x88); ucLCD.EscreveCaractere('A'); break;
                case 7: ucLCD.SelecionaEndereço(0x89); ucLCD.EscreveCaractere('D'); break;
                case 8: ucLCD.SelecionaEndereço(0x8A); ucLCD.EscreveCaractere('O'); break;
                case 9: ucLCD.SelecionaEndereço(0x8B); ucLCD.EscreveCaractere('R'); break;
                case 10: ucLCD.SelecionaEndereço(0xC0); ucLCD.EscreveCaractere('K'); break;
                case 11: ucLCD.SelecionaEndereço(0xC1); ucLCD.EscreveCaractere('I'); break;
                case 12: ucLCD.SelecionaEndereço(0xC2); ucLCD.EscreveCaractere('T'); break;
                case 13: ucLCD.SelecionaEndereço(0xC3); ucLCD.EscreveCaractere(' '); break;
                case 14: ucLCD.SelecionaEndereço(0xC4); ucLCD.EscreveCaractere('S'); break;
                case 15: ucLCD.SelecionaEndereço(0xC5); ucLCD.EscreveCaractere('E'); break;
                case 16: ucLCD.SelecionaEndereço(0xC6); ucLCD.EscreveCaractere('N'); break;
                case 17: ucLCD.SelecionaEndereço(0xC7); ucLCD.EscreveCaractere('A'); break;
                case 18: ucLCD.SelecionaEndereço(0xC8); ucLCD.EscreveCaractere('I'); break;
                case 19: ucLCD.SelecionaEndereço(0xC9); ucLCD.EscreveCaractere(' '); break;
                case 20: ucLCD.SelecionaEndereço(0xCA); ucLCD.EscreveCaractere('P'); break;
                case 21: ucLCD.SelecionaEndereço(0xCB); ucLCD.EscreveCaractere('I'); break;
                case 22: ucLCD.SelecionaEndereço(0xCC); ucLCD.EscreveCaractere('C'); break;
                case 23: ucLCD.SelecionaEndereço(0xCD); ucLCD.EscreveCaractere('1'); break;
                case 24: ucLCD.SelecionaEndereço(0xCE); ucLCD.EscreveCaractere('6'); break;
                case 25: ucDisp3.ValorDecimalouASCII('P'); break;
                case 26: ucDisp2.ValorDecimalouASCII('I'); break;
                case 27: ucDisp1.ValorDecimalouASCII('C'); break;
                case 28:
                    ucLED0.SetEstado(Library_LPD_UserControl.UserControlLed.Estados.On);
                    ucLED7.SetEstado(Library_LPD_UserControl.UserControlLed.Estados.On);
                    break;
                case 29:
                    ucLED1.SetEstado(Library_LPD_UserControl.UserControlLed.Estados.On);
                    ucLED6.SetEstado(Library_LPD_UserControl.UserControlLed.Estados.On);
                    break;
                case 30:
                    ucLED2.SetEstado(Library_LPD_UserControl.UserControlLed.Estados.On);
                    ucLED5.SetEstado(Library_LPD_UserControl.UserControlLed.Estados.On);
                    break;
                case 31:
                    ucLED3.SetEstado(Library_LPD_UserControl.UserControlLed.Estados.On);
                    ucLED4.SetEstado(Library_LPD_UserControl.UserControlLed.Estados.On);
                    break;
                default: timerAbertura.Enabled = false; menuStrip1.Enabled = true; break;
            }
            abertura++;
        }
        #endregion

        //---------------------------------------------------------------------
        #region Métodos Privados Disparados pelo Menu Arquivo------------------
        /// <summary>
        /// Abre um arquivo de máquina (Intel Hex) e prepara o ambiente.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuAbrir_Click(object sender, EventArgs e)
        {
            try
            {
                openFileDialog1.Filter = "Hexadecimal Files|*.hex";
                openFileDialog1.Title = "Abrir um arquivo...";
                openFileDialog1.FileName = "";
                if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    //Limpa memorias RAM e ROM, stack, apaga os leds, recarrega os banks com os valores padrão e a ROM com NOPs
                    Limpar();

                    //Lê toda as linhas do arquivo e salva em uma matriz de strings
                    var lines = File.ReadAllLines(openFileDialog1.FileName.ToString());

                    //Preenche o DataGrid com o as linhas do código IntelHex
                    foreach (var item in lines)
                    {
                        dgvIntelHex.Rows.Add(item);
                    }

                    //Cria um objeto intrepertador de arquivos IntelHex
                    IntelHex_to_PIC16 disassembly = new IntelHex_to_PIC16();

                    //Converte o arquivo para matriz de Endereços e Instruções
                    ushort[,] ROM = disassembly.IntelHex_To_Rom(openFileDialog1.FileName);

                    //DEBUG
                    //for (int i = 0; i < ROM.Length / 2; i++)
                    //{
                    //    tbDebug.AppendText(ROM[i, 0].ToString() + "->" + ROM[i, 1].ToString() + Environment.NewLine);
                    //}

                    //Converte a matriz de Endereços e Instruções em um matriz com
                    //os campos do Configuration Word separados em strings
                    //0-ADDRESS, 1-CP, 2-DEBUG, 3-WRT1:WRT0, 4-CPD, 5-LVP, 
                    //6-BOREN, 7-PWRTEN, 8-WDTEN, 9-FOSC1:FOSC0//
                    string[,] cbits = disassembly.Rom_To_ConfigurationBits(ROM);
                    dgvConfigBits.Rows.Add(cbits[0, 0], cbits[0, 1], cbits[0, 2], cbits[0, 3], cbits[0, 4], cbits[0, 5], cbits[0, 6], cbits[0, 7], cbits[0, 8], cbits[0, 9]);
                    dgvConfigBits.Rows.Add(cbits[1, 0], cbits[1, 1], cbits[1, 2], cbits[1, 3], cbits[1, 4], cbits[1, 5], cbits[1, 6], cbits[1, 7], cbits[1, 8], cbits[1, 9]);

                    //Converte a matriz de Endereços e Instruções em um matriz com
                    //os campos separados em strings
                    //0-Endereço, 1-Byte OpCode High (hexa), 2-Byte OpCode Low (hexa), 
                    //3-Byte OpCode High (bi), 4-Byte OpCode Low (bi), 
                    //5-Mnemônico, 6-Destino, 7-Registrador, 8-Endereço do Bit, 9-Constante
                    string[,] str = disassembly.Rom_To_String(ROM);

                    //Preenche os DataGrids com o as strings dos campos de cada instruções
                    for (int i = 0; i < ROM.Length / 2; i++)
                    {
                        string Address = str[i, 0];
                        string HHex = str[i, 1];
                        string LHex = str[i, 2];
                        string HBin = str[i, 3];
                        string LBin = str[i, 4];
                        string Mnemonic = str[i, 5];
                        string Destination = str[i, 6];
                        string Register = str[i, 7];
                        string Bit = str[i, 8];
                        string Constant = str[i, 9];

                        //Limita em 8k de memória de programa (1FFFh = 8191, endereço 2007h = Configuration Bits
                        if (int.Parse(Address) > 8191) break;
                        //Preenche os DataGrids
                        dgvTradutor.Rows.Add((Address), (HHex), (LHex), (HBin), (LBin), (Mnemonic), (Destination), (Register), (Bit), (Constant));
                        dgvMemoria.Rows.RemoveAt(int.Parse(Address));
                        dgvMemoria.Rows.Insert(int.Parse(Address), (Address), (Mnemonic), (Destination), (Register), (Bit), (Constant)); //INCLUI NOVO DADO NA MEMORIA          
                    }

                    //Seleciona a primeira linha do programa
                    dgvMemoria.CurrentCell = dgvMemoria.Rows[0].Cells[0];

                    //Prepara matrizes de memória de programa e bancos de registradores para a execução
#if ModoString
                    PreparaVetoresRAMeROM();
#endif
#if ModoBinario
                    PreparaVetoresRAMeROM_Binario(ROM);
#endif

                    //Passa a referência da RAM para Serial Virtual
#if ModoString
                    serialVirtual.ReferenciaRAM(bank);
#endif
#if ModoBinario
                    serialVirtual.ReferenciaRAM_Binario(bank_bin);
#endif

                    //Habilita Timer0 e Entradas Analógicas
                    gbTimer0.Enabled = true;
                    gbEA.Enabled = true;

                    //Se tudo correu bem
                    this.Text = titulo + " (" + openFileDialog1.SafeFileName + ")";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao abrir o arquivo ou interpretá-lo.\n" + ex.Message, "ERRO",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Limpar();
            }
        }
        /// <summary>
        /// Fecha a aplicação
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuSair_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        /// <summary>
        /// Limpa a interface e prepara as memórias.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuLimpar_Click(object sender, EventArgs e)
        {
            //Limpa todos os dados e já carrega os valores iniciais dos bancos
            Limpar();
            //Prepara matrizes de memória de programa e bancos de registradores para a execução
#if ModoString
            PreparaVetoresRAMeROM();
#endif
#if ModoBinario
            PreparaVetoresRAMeROM_Binario();
#endif
            //Passa a referência da RAM para Serial Virtual
#if ModoString
            serialVirtual.ReferenciaRAM(bank);
#endif
#if ModoBinario
            serialVirtual.ReferenciaRAM_Binario(bank_bin);
#endif

        }
        #endregion

        //---------------------------------------------------------------------
        #region Métodos Privados Disparados pelo Menu Ferramentas--------------
        /// <summary>
        /// Converte valores decimais, binários, hexadecimais e ASCIIs
        /// de até 16 bits.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuConversorDeBases_Click(object sender, EventArgs e)
        {
            if (Application.OpenForms.OfType<FormConversor>().Count() == 0)
            {
                FormConversor formConversor = new FormConversor();
                formConversor.Show();
            }
        }
        /// <summary>
        /// Abre o datasheet do PIC16F877A
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuDatasheetPIC16F877A_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(Environment.CurrentDirectory + @"\Documentação\PIC16F87xA.pdf");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERRO", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// Link para o site de Microchip
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuSiteMicrochip_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.microchip.com/");
        }
        /// <summary>
        /// Link para o site do SENAI Anchieta
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuSiteSENAIAnchieta_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://eletronica.sp.senai.br/");
        }
        #endregion

        //---------------------------------------------------------------------
        #region Métodos Privados Disparados pelo Menu Configurações------------
        /// <summary>
        /// Define os leds na cor VERMELHA.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuLedsVermelhos_Click(object sender, EventArgs e)
        {
            ucLED0.Cor = Library_LPD_UserControl.UserControlLed.Cores.Vermelho_On;
            ucLED1.Cor = Library_LPD_UserControl.UserControlLed.Cores.Vermelho_On;
            ucLED2.Cor = Library_LPD_UserControl.UserControlLed.Cores.Vermelho_On;
            ucLED3.Cor = Library_LPD_UserControl.UserControlLed.Cores.Vermelho_On;
            ucLED4.Cor = Library_LPD_UserControl.UserControlLed.Cores.Vermelho_On;
            ucLED5.Cor = Library_LPD_UserControl.UserControlLed.Cores.Vermelho_On;
            ucLED6.Cor = Library_LPD_UserControl.UserControlLed.Cores.Vermelho_On;
            ucLED7.Cor = Library_LPD_UserControl.UserControlLed.Cores.Vermelho_On;
        }
        /// <summary>
        /// Define os leds na cor VERDE.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuLedsVerdes_Click(object sender, EventArgs e)
        {
            ucLED0.Cor = Library_LPD_UserControl.UserControlLed.Cores.Verde_On;
            ucLED1.Cor = Library_LPD_UserControl.UserControlLed.Cores.Verde_On;
            ucLED2.Cor = Library_LPD_UserControl.UserControlLed.Cores.Verde_On;
            ucLED3.Cor = Library_LPD_UserControl.UserControlLed.Cores.Verde_On;
            ucLED4.Cor = Library_LPD_UserControl.UserControlLed.Cores.Verde_On;
            ucLED5.Cor = Library_LPD_UserControl.UserControlLed.Cores.Verde_On;
            ucLED6.Cor = Library_LPD_UserControl.UserControlLed.Cores.Verde_On;
            ucLED7.Cor = Library_LPD_UserControl.UserControlLed.Cores.Verde_On;
        }
        /// <summary>
        /// Define os leds na cor AZUL.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuLedsAzuis_Click(object sender, EventArgs e)
        {
            ucLED0.Cor = Library_LPD_UserControl.UserControlLed.Cores.Azul_On;
            ucLED1.Cor = Library_LPD_UserControl.UserControlLed.Cores.Azul_On;
            ucLED2.Cor = Library_LPD_UserControl.UserControlLed.Cores.Azul_On;
            ucLED3.Cor = Library_LPD_UserControl.UserControlLed.Cores.Azul_On;
            ucLED4.Cor = Library_LPD_UserControl.UserControlLed.Cores.Azul_On;
            ucLED5.Cor = Library_LPD_UserControl.UserControlLed.Cores.Azul_On;
            ucLED6.Cor = Library_LPD_UserControl.UserControlLed.Cores.Azul_On;
            ucLED7.Cor = Library_LPD_UserControl.UserControlLed.Cores.Azul_On;
        }
        /// <summary>
        /// Define os segmentos acesos dos displays na cor ESCOLHIDA.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuCorDosDisplaysAcesos_Click(object sender, EventArgs e)
        {
            ColorDialog CorDisplays = new ColorDialog();
            CorDisplays.Color = ucDisp1.CorDoSegmento;
            if (CorDisplays.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ucDisp1.CorDoSegmento = CorDisplays.Color;
                ucDisp2.CorDoSegmento = CorDisplays.Color;
                ucDisp3.CorDoSegmento = CorDisplays.Color;
            }
        }
        /// <summary>
        /// Define os segmentos apagados dos displays na cor ESCOLHIDA.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuCorDosDisplaysApagados_Click(object sender, EventArgs e)
        {
            ColorDialog CorDisplays = new ColorDialog();
            CorDisplays.Color = ucDisp1.CorDoSegmentoApagado;
            if (CorDisplays.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ucDisp1.CorDoSegmentoApagado = CorDisplays.Color;
                ucDisp2.CorDoSegmentoApagado = CorDisplays.Color;
                ucDisp3.CorDoSegmentoApagado = CorDisplays.Color;
            }
        }
        /// <summary>
        /// Define o fundo do LCD na cor ESCOLHIDA.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuCorDeFundoDoLCD_Click(object sender, EventArgs e)
        {
            ColorDialog CorLCD = new ColorDialog();
            CorLCD.Color = ucLCD.CorFundo;
            if (CorLCD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ucLCD.CorFundo = CorLCD.Color;
            }
        }
        /// <summary>
        /// Define os caracteres do LCD na cor ESCOLHIDA.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuCorDosCaracteresDoLCD_Click(object sender, EventArgs e)
        {
            ColorDialog CorLCD = new ColorDialog();
            CorLCD.Color = ucLCD.CorCaractere;
            if (CorLCD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ucLCD.CorCaractere = CorLCD.Color;
            }
        }
        /// <summary>
        /// Altera o tempo entre instrução no modo sem refresh das memórias (thread).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuTempoEntreInstruções_Click(object sender, EventArgs e)
        {
            FormTicks fTicks = new FormTicks(this);
            if (fTicks.ShowDialog() == DialogResult.OK)
            {
                if (turbo == false)  //Alterado na versão 3.3
                    MessageBox.Show(String.Format("Tempo entre instruções = {0} us", ticksThread / 10), "Atualizado com Sucesso");
                else
                    MessageBox.Show("Atenção modo turbo ativado. " + String.Format("Desative para limitar o tempo entre instruções em {0} us", ticksThread / 10), "Atualizado com Sucesso");
            }
        }
        #endregion

        //---------------------------------------------------------------------
        #region Métodos Privados Disparados pelo Menu Sobre--------------------
        /// <summary>
        /// Informações sobre o programa e seus desenvolvedores.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuSobreDesenvimento_Click(object sender, EventArgs e)
        {
            FormSobre fSobre = new FormSobre();
            fSobre.ShowDialog();
        }
        /// <summary>
        /// Informações sobre os periféricos implementados.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuSobrePeriféricos_Click(object sender, EventArgs e)
        {
            FormPeriféricos fPerif = new FormPeriféricos();
            fPerif.ShowDialog();
        }
        #endregion

        //---------------------------------------------------------------------
        #region Métodos Privados Disparados pelas Entradas Analógicas
        /// <summary>
        /// Scroll do TrackBar AN0
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbAN0_Scroll(object sender, EventArgs e)
        {
            int s;
            float v;
            string tensão;

            tbAN0Tensão.Text = tbarAN0.Value.ToString();

            v = (float)tbarAN0.Value / 1000;
            tensão = v.ToString();
            if (tensão.Length > 4)
                tbAN0Tensão.Text = tensão.Substring(0, 4);
            else
                tbAN0Tensão.Text = tensão;

            s = (1023 * tbarAN0.Value) / 5000;
            tbAN0Step.Text = s.ToString();
        }
        /// <summary>
        /// Mudança do TextBox AN0Tensão
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbAN0Tensão_TextChanged(object sender, EventArgs e)
        {
            int s;
            float v;

            try
            {
                v = float.Parse(tbAN0Tensão.Text);
                if (v > 5000) v = 5000;
                if (v < 0) v = 0;

                tbarAN0.Value = (int)(v * 1000);

                s = (1023 * tbarAN0.Value) / 5000;
                tbAN0Step.Text = s.ToString();
            }
            catch (Exception)
            { }
        }
        /// <summary>
        /// Scroll do TrackBar AN1
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbarAN1_Scroll(object sender, EventArgs e)
        {
            int s;
            float v;
            string tensão;

            tbAN1Tensão.Text = tbarAN1.Value.ToString();

            v = (float)tbarAN1.Value / 1000;
            tensão = v.ToString();
            if (tensão.Length > 4)
                tbAN1Tensão.Text = tensão.Substring(0, 4);
            else
                tbAN1Tensão.Text = tensão;

            s = (1023 * tbarAN1.Value) / 5000;
            tbAN1Step.Text = s.ToString();
        }
        /// <summary>
        /// Mudança do TextBox AN1Tensão
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbAN1Tensão_TextChanged(object sender, EventArgs e)
        {
            int s;
            float v;

            try
            {
                v = float.Parse(tbAN1Tensão.Text);
                if (v > 5000) v = 5000;
                if (v < 0) v = 0;

                tbarAN1.Value = (int)(v * 1000);

                s = (1023 * tbarAN1.Value) / 5000;
                tbAN1Step.Text = s.ToString();
            }
            catch (Exception)
            { }
        }
        #endregion

        //---------------------------------------------------------------------
        #region Métodos Privados Disparados por Controles da Aba Memórias------
        /// <summary>
        /// Busca um endereço na memória de programa (ROM)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bBuscar_Click(object sender, EventArgs e)
        {
            try
            {
                int buscar = int.Parse(tbBuscar.Text);
                dgvMemoria.CurrentCell = dgvMemoria.Rows[buscar].Cells[0];
            }
            catch { }
        }
        /// <summary>
        /// Busca um endereço na memória de programa (ROM)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbBuscar_TextChanged(object sender, EventArgs e)
        {
            bBuscar.PerformClick();
        }
        /// <summary>
        /// Habilita a execução do programa
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bExecutar_Click(object sender, EventArgs e)
        {
            if (clock.Enabled == false)
            {
                dgvMemoria.CurrentCell = dgvMemoria.Rows[PC].Cells[0];
                bBuscar.Enabled = false;
                tbBuscar.Enabled = false;
                dgvMemoria.Enabled = false;
                clock.Enabled = true;
                bExecutar.Text = "Pausar";
                cbExecutar.Checked = true;

                bExecPasso.Enabled = false; //Se estiver executando desabilita a execução passo a passo
            }
            else
            {
                clock.Enabled = false;
                if (threadRun != null)
                {
                    pararThread = true;
                }

                bExecutar.Text = "Executar";
                cbExecutar.Checked = false;

                bExecPasso.Enabled = true;  //Se não estiver executando habilita a execução passo a passo
            }
        }
        /// <summary>
        /// Habilita a execução de uma instrução do programa.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bExecPasso_Click(object sender, EventArgs e)
        {
            //Se não estiver executando executa uma instrução
            if (clock.Enabled == false)
            {
                //Incrementa Ticks
                ticks++;
                //if (cbRefresh.Checked)
                {
                    lTicks.Text = ticks.ToString();
                }
#if ModoString
                Clock_Run();
#endif
#if ModoBinario
                Clock_Run_Binario();
#endif
            }
        }
        /// <summary>
        /// Reseta o microcontrolador e para o processamento de instruções.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bResetar_Click(object sender, EventArgs e)
        {
            Reset();
            //Prepara matrizes de memória de programa e bancos de registradores para a execução
#if ModoString
            PreparaVetoresRAMeROM();
#endif
#if ModoBinario
            PreparaVetoresRAM_Binario();
#endif
        }
        /// <summary>
        /// Define o período entre cada instrução executada.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbPeriodo_Scroll(object sender, EventArgs e)
        {
            lPeriodo.Text = tbPeriodo.Value.ToString() + " ms";
            clock.Interval = tbPeriodo.Value;
#if TesteThread
            periodo_ms = tbPeriodo.Value;
#endif
        }
        /// <summary>
        /// Atualiza a flag de refesh a cada alteração do checkbox e
        /// evita erros em outras flags de atulização de datagrids
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbRefresh_CheckedChanged(object sender, EventArgs e)
        {
            //Primeiro para a Thread
            if (cbRefresh.Checked)
            {
                //Para a Thread caso ela estaja rodando
                if (threadRun != null)
                {
                    //Passa solicitação para Thread
                    pararThread = true;
                    //Aguarda ela parar
                    while (threadRun.IsAlive) ;
                }
            }

            //Depois limpa todos os refreshs
            //Limpando todas as flags evita que uma flag sinalizada que não
            //tenha sido atendida quando o checkbox de refresh foi desmarcado
            //tente atualizar um campo sem referência de endereço (-1)
            refreshWork = false;
            refreshStatus = false;
            refreshReg = false;
            refreshStack_push = false;
            refreshStack_pop = false;
            refreshIntcon = false;
        }
        #endregion

        //---------------------------------------------------------------------
        #region Métodos Privados Disparados por Controles da Aba Simulador-----
        /// <summary>
        /// Habilita a execução do programa na forma gráfica.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbExecutar_CheckedChanged(object sender, EventArgs e)
        {
            if (cbExecutar.Checked == true)
            {
                dgvMemoria.CurrentCell = dgvMemoria.Rows[PC].Cells[0];
                bBuscar.Enabled = false;
                tbBuscar.Enabled = false;
                dgvMemoria.Enabled = false;
                clock.Enabled = true;
                bExecutar.Text = "Pausar";

                bExecPasso.Enabled = false; //Se estiver executando desabilita a execução passo a passo
            }
            else
            {
                clock.Enabled = false;
                if (threadRun != null)
                {
                    pararThread = true;
                }

                bExecutar.Text = "Executar";

                bExecPasso.Enabled = true;  //Se não estiver executando habilita a execução passo a passo
            }
        }
        /// <summary>
        /// Reseta o microcontrolador e para o processamento de instruções na forma gráfica.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbResetar_CheckedChanged(object sender, EventArgs e)
        {
            Reset();
            //Prepara matrizes de memória de programa e bancos de registradores para a execução
#if ModoString
            PreparaVetoresRAMeROM();
#endif
#if ModoBinario
            PreparaVetoresRAM_Binario();
#endif
        }
        /// <summary>
        /// Habilita a execução do programa na forma gráfica.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pbExecutar_Click(object sender, EventArgs e)
        {
            cbExecutar.Checked = !cbExecutar.Checked;
        }
        /// <summary>
        /// Reseta o microcontrolador e para o processamento de instruções na forma gráfica.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pbResetar_Click(object sender, EventArgs e)
        {
            cbResetar.Checked = true;
        }
        /// <summary>
        /// Define o estado do pino RB0.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pbRB0_Click(object sender, EventArgs e)
        {
            cbRB0.Checked = !cbRB0.Checked;
        }
        /// <summary>
        /// Define o estado do pino RB1.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pbRB1_Click(object sender, EventArgs e)
        {
            cbRB1.Checked = !cbRB1.Checked;
        }
        #endregion

        //---------------------------------------------------------------------
        #region Métodos Privados Disparados por Controles da Aba Esquema Elétrico
        /// <summary>
        /// Exibe o esquema eletrico do KIT SENAI PIC16 dividido em blocos funcionais
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbPinos_SelectedIndexChanged(object sender, EventArgs e)
        {
            pbEsquemas.SizeMode = PictureBoxSizeMode.Zoom;

            /*
             * Opções:
             * Microcontrolador
             * Cristais e Reset
             * Comunicação Serial
             * Conectores
             * Displays de 7 Seguimentos
             * Driver de Corrente
             * Entradas Analógicas
             * Fonte de Alimentação
             * LCD
             * LEDs
             * Teclas de Interrupção
             * Teclado Matricial
             */
            switch (cbEsquemas.SelectedIndex)
            {
                case 0: pbEsquemas.Image = Properties.Resources.Esquematico_Microcontrolador; break;
                case 1: pbEsquemas.Image = Properties.Resources.Esquematico_Cristais_e_Reset; break;
                case 2: pbEsquemas.Image = Properties.Resources.Esquematico_Comunicação_Serial; break;
                case 3: pbEsquemas.Image = Properties.Resources.Esquematico_Conectores; break;
                case 4: pbEsquemas.Image = Properties.Resources.Esquematico_Displays; break;
                case 5: pbEsquemas.Image = Properties.Resources.Esquematico_Driver_de_Corrente; break;
                case 6: pbEsquemas.Image = Properties.Resources.Esquematico_Entradas_Analogicas; break;
                case 7: pbEsquemas.Image = Properties.Resources.Esquematico_Fonte_de_Alimentação; break;
                case 8: pbEsquemas.Image = Properties.Resources.Esquematico_LCD; break;
                case 9: pbEsquemas.Image = Properties.Resources.Esquematico_Leds; break;
                case 10: pbEsquemas.Image = Properties.Resources.Esquematicotecla_Interrupção; break;
                case 11: pbEsquemas.Image = Properties.Resources.Esquematico_Teclado; break;
            }
        }
        #endregion

        //---------------------------------------------------------------------
        #region Métodos Privados Disparados pela Serial Virtual----------------
        /// <summary>
        /// Disparado após a serial virtual enviar um byte e executar alteração em outros da RAM.
        /// Normalmente o registrador RCREG (BANK0 1Ah) recebe o byte enviado e
        /// o bit RCIF no registrador PIR1 (PIR1.5 = RCIF, BANK0 0Ch) é setado para sinalizar um byte recebido.
        /// </summary>
        /// <param name="registrador_alterado"></param>
        private void serialVirtual_EventoSerialTx(int[] registrador_alterado)
        {
            if (cbRefresh.Checked)
            {
#if ModoString
                switch (registrador_alterado[0])
                {
                    case 0: dgvBank0.Rows[registrador_alterado[1]].Cells[2].Value = bank[0, registrador_alterado[1], 2]; break;
                    case 1: dgvBank1.Rows[registrador_alterado[1]].Cells[2].Value = bank[1, registrador_alterado[1], 2]; break;
                    case 2: dgvBank2.Rows[registrador_alterado[1]].Cells[2].Value = bank[2, registrador_alterado[1], 2]; break;
                    case 3: dgvBank3.Rows[registrador_alterado[1]].Cells[2].Value = bank[3, registrador_alterado[1], 2]; break;
                }
#endif
#if ModoBinario
                switch (registrador_alterado[0])
                {
                    case 0: dgvBank0.Rows[registrador_alterado[1]].Cells[2].Value = IntToBinString(bank_bin[0, registrador_alterado[1]]); break;
                    case 1: dgvBank1.Rows[registrador_alterado[1]].Cells[2].Value = IntToBinString(bank_bin[1, registrador_alterado[1]]); break;
                    case 2: dgvBank2.Rows[registrador_alterado[1]].Cells[2].Value = IntToBinString(bank_bin[2, registrador_alterado[1]]); break;
                    case 3: dgvBank3.Rows[registrador_alterado[1]].Cells[2].Value = IntToBinString(bank_bin[3, registrador_alterado[1]]); break;
                }
#endif
            }
        }
        /// <summary>
        /// Disparado após a serial virtual receber um byte e executar alteração em outros da RAM.
        /// Normalmente o registrador TXREG (BANK0 19h) é zerado e 
        /// o bit TXIF byte no registrador (PIR1.4 = TXIF, BANK0 0Ch) é setado para sinalizar fim de transmissão.
        /// </summary>
        /// <param name="registrador_alterado">Vetor com o banco e endereço do registrador.</param>
        private void serialVirtual_EventoSerialRx(int[] registrador_alterado)
        {
            if (cbRefresh.Checked)
            {
#if ModoString
                switch (registrador_alterado[0])
                {
                    case 0: dgvBank0.Rows[registrador_alterado[1]].Cells[2].Value = bank[0, registrador_alterado[1], 2]; break;
                    case 1: dgvBank1.Rows[registrador_alterado[1]].Cells[2].Value = bank[1, registrador_alterado[1], 2]; break;
                    case 2: dgvBank2.Rows[registrador_alterado[1]].Cells[2].Value = bank[2, registrador_alterado[1], 2]; break;
                    case 3: dgvBank3.Rows[registrador_alterado[1]].Cells[2].Value = bank[3, registrador_alterado[1], 2]; break;
                }
#endif
#if ModoBinario
                switch (registrador_alterado[0])
                {
                    case 0: dgvBank0.Rows[registrador_alterado[1]].Cells[2].Value = IntToBinString(bank_bin[0, registrador_alterado[1]]); break;
                    case 1: dgvBank1.Rows[registrador_alterado[1]].Cells[2].Value = IntToBinString(bank_bin[1, registrador_alterado[1]]); break;
                    case 2: dgvBank2.Rows[registrador_alterado[1]].Cells[2].Value = IntToBinString(bank_bin[2, registrador_alterado[1]]); break;
                    case 3: dgvBank3.Rows[registrador_alterado[1]].Cells[2].Value = IntToBinString(bank_bin[3, registrador_alterado[1]]); break;
                }
#endif
            }
        }
        #endregion

        //---------------------------------------------------------------------
        #region Método Privado de Tick do Microcontrolador---------------------
        /// <summary>
        /// Controle Clock dispara um tick temporizado para execução de
        /// uma instrução.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Clock_Tick(object sender, EventArgs e)
        {
            //Se está no modo com refresh de tela roda instrução por instrução
            if (cbRefresh.Checked)
            {
                ticks++;
                clock.Interval = tbPeriodo.Value;
#if ModoString
                Clock_Run();
#endif
#if ModoBinario
                Clock_Run_Binario();
#endif
            }
            //Senão entra no modo com thread (muito mais rápido)
            else
            {
                //Tempo mínimo para fazer os refreshes
                clock.Interval = 1;
                //Inicia a Thread
                if (threadRun == null)
                {
                    threadRun = new Thread(Clock_Run_Binario_Thread);
                }
                if (!threadRun.IsAlive)
                {
                    threadRun = new Thread(Clock_Run_Binario_Thread);
                    threadRun.Start();
                }
                //Chama o método de refresh dos periféricos
                Clock_Run_Refresh();
                //Se ocorreu algum erro a Thread sinaliza com pedido de reset e encerra
                if (fazerReset == true)
                    Reset();
            }
            //Sempre
            lTicks.Text = ticks.ToString();
        }
        #endregion

        //---------------------------------------------------------------------
        #region Métodos Motores do Simulador KIT SENAI PIC16-------------------
        /// <summary>
        /// Interpreta e executa cada uma das instruções de acordo com o 
        /// endereço apontado pela program counter (PC).
        /// Atenção: Esse método não está sendo atualizado desde a versão 3.0 do programa.
        /// </summary>
        private void Clock_Run()
        {
            //TODO: Verificar as condições de Carry pois as variáveis utilizadas são byte e não ultrapassam 255!!!

            //Variáveis locais
            string Valor = "";
            int Endereço = -1;

            try
            {
                #region EXECUÇÃO DOS COMANDOS ///////////////////////////////////////////////////////////////////////////////

                Atual = PC;             //Instrução atual é igual ao valor do contador de programas
                PC++;                   //Incrementa o contador de programas para apontar a próxima instrução
                PC %= 8192;             //Limita o valor de PC ao tamanho da memória

                //Executa a instrução atual
                switch (memoria[Atual, 1])
                {
                    #region CALL
                    case "CALL":   //ALTERA STACK
                        {
                            //Se a pilha esta cheia remove o último registro
                            if (picStack.Count >= 8)
                                picStack.Pop();
                            //Insere o valor do program counter na pilha
                            picStack.Push(PC);
                            //Atualiza program counter com o valor de desvio
                            PC = Convert.ToInt32(memoria[Atual, 5], 2);
                            //Sinaliza refresh do stack
                            refreshStack_push = true;

                            break;
                        }
                    #endregion

                    #region GOTO
                    case "GOTO":  //PULO PARA ENDEREÇO
                        {
                            PC = Convert.ToInt32(memoria[Atual, 5], 2);
                            break;
                        }
                    #endregion

                    #region MOVLW
                    case "MOVLW":// MOVE NÚMERO PARA WORK
                        {
                            sWork = memoria[Atual, 5];
                            refreshWork = true;
                            break;
                        }
                    #endregion

                    #region RETLW
                    case "RETLW":  //RETORNA PARA POSIÇÃO DO STACK E RETORNA UM NÚMERO PARA WORK
                        {
                            //Se a pilha não está vazia remove um registro
                            if (picStack.Count > 0)
                                PC = (int)picStack.Pop();
                            //Grava em W o valor deretorno
                            sWork = memoria[Atual, 5];
                            refreshWork = true;
                            //Sinaliza refresh do stack
                            refreshStack_pop = true;

                            break;
                        }
                    #endregion

                    #region RETURN
                    case "RETURN":  //RETORNA DE UMA SUBROTINA
                        {
                            //Se a pilha não está vazia remove um registro
                            if (picStack.Count > 0)
                                PC = (int)picStack.Pop();
                            //Sinaliza refresh do stack
                            refreshStack_pop = true;

                            break;
                        }
                    #endregion

                    #region BSF
                    //TODO: Testar por que o teste de bit está dando problema e não consegue fazeras funções lógicas nem ler as teclas corretamente
                    case "BSF": //SETA UM ÚNICO BIT EM UM REGISTRADOR
                        {
                            Endereço = Convert.ToInt32(memoria[Atual, 3].Substring(2, 2), 16); //ENDEREÇO DO REGISTRADOR 
                            int x = Convert.ToInt32(memoria[Atual, 4]); //ÍNDICE DO SETBIT

                            x = (x - 7) * (-1); //INVERTE LEITURA DO BYTE

                            //Atualização genérica, serve para todos os bancos
                            char[] ValueArray = bank[selBank, Endereço, 2].ToCharArray();   //Converte o valor do registrador endereçado para vetor de caracteres
                            ValueArray[x] = '1';                                            //Atualiza o bit desejado
                            Valor = new string(ValueArray);                                 //Remonta a string
                            bank[selBank, Endereço, 2] = Valor;                             //Atualiza o registrador
                            refreshReg = true;                                              //Sinaliza necessidade de atualização de registrador

                            //Se alterou o registrador Status deve espelhar no 4 bancos
                            if (Endereço == 3)
                            {
                                sStatus = Valor;
                                refreshStatus = true;
                            }
                            break;
                        }
                    #endregion

                    #region BCF
                    case "BCF": //LIMPA UM ÚNICO BIT EM UM REGISTRADOR
                        {
                            Endereço = Convert.ToInt32(memoria[Atual, 3].Substring(2, 2), 16); //ENDEREÇO DO REGISTRADOR 
                            int x = Convert.ToInt32(memoria[Atual, 4]); //ÍNDICE DO SETBIT

                            x = (x - 7) * (-1); //INVERTE LEITURA DO BYTE

                            //Atualização genérica, serve para todos os bancos
                            char[] ValueArray = bank[selBank, Endereço, 2].ToCharArray();   //Converte o valor do registrador endereçado para vetor de caracteres
                            ValueArray[x] = '0';                                            //Atualiza o bit desejado
                            Valor = new string(ValueArray);                                 //Remonta a string
                            bank[selBank, Endereço, 2] = Valor;                             //Atualiza o registrador
                            refreshReg = true;                                              //Sinaliza necessidade de atualização de registrador

                            //Se alterou o registrador Status deve espelhar no 4 bancos
                            if (Endereço == 3)
                            {
                                sStatus = Valor;
                                refreshStatus = true;
                            }

                            break;
                        }
                    #endregion

                    #region BTFSS
                    case "BTFSS":  //TESTA UM ÚNICO BIT EM UM REGISTRADOR, SE FOR 1, PULA 1 ENDEREÇO, SENÃO CONTINUA
                        {
                            Endereço = Convert.ToInt32(memoria[Atual, 3].Substring(2, 2), 16); //ENDEREÇO DO REGISTRADOR 
                            int x = Convert.ToInt32(memoria[Atual, 4]); //ÍNDICE DO SETBIT

                            x = (x - 7) * (-1); //INVERTE LEITURA DO BYTE

                            //Atualização genérica, serve para todos os bancos
                            char[] ValueArray = bank[selBank, Endereço, 2].ToCharArray();   //Converte o valor do registrador endereçado para vetor de caracteres

                            if (ValueArray[x] == '1')
                                PC++;

                            //Permite a visualização do registrador testado
                            refreshReg = true;                                              //Sinaliza necessidade de atualização de registrador

                            break;
                        }
                    #endregion

                    #region BTFSC
                    case "BTFSC":   //TESTA UM ÚNICO BIT EM UM REGISTRADOR, SE FOR 0, PULA 1 ENDEREÇO, SENÃO CONTINUA
                        {
                            Endereço = Convert.ToInt32(memoria[Atual, 3].Substring(2, 2), 16); //ENDEREÇO DO REGISTRADOR 
                            int x = Convert.ToInt32(memoria[Atual, 4]); //ÍNDICE DO SETBIT

                            x = (x - 7) * (-1); //INVERTE LEITURA DO BYTE

                            //Atualização genérica, serve para todos os bancos
                            char[] ValueArray = bank[selBank, Endereço, 2].ToCharArray();   //Converte o valor do registrador endereçado para vetor de caracteres

                            if (ValueArray[x] == '0')
                                PC++;

                            //Permite a visualização do registrador testado
                            refreshReg = true;                                              //Sinaliza necessidade de atualização de registrador

                            break;
                        }
                    #endregion

                    #region CLRF
                    case "CLRF":    //LIMPA UM REGISTRADOR, MANDA '00000000'
                        {
                            Endereço = Convert.ToInt32(memoria[Atual, 3].Substring(2, 2), 16); //ENDEREÇO DO REGISTRADOR 

                            bank[selBank, Endereço, 2] = "00000000";
                            refreshReg = true;

                            //Se alterou o registrador Status deve espelhar no 4 bancos
                            if (Endereço == 3)
                            {
                                sStatus = "00011100";   // /Time out e /Power Down ReadOnly = 1, Zero = 1 por conta do clear
                                refreshStatus = true;
                            }
                            //Se for o PORTB
                            if (selBank == 0 && Endereço == 6)
                            {
                                bank[selBank, 6, 2] = "00011111";
                            }

                            //Seta flag Z
                            char[] ValueArray = sStatus.ToCharArray();
                            //Flag de Zero (bit 2 de status, bit 5 no array (byte invertido))
                            ValueArray[5] = '1';
                            Valor = new string(ValueArray);

                            sStatus = Valor;
                            refreshStatus = true;

                            break;
                        }
                    #endregion

                    #region CLRW
                    case "CLRW":     //LIMPA WORK, MANDA '00000000'
                        {
                            sWork = "00000000";
                            refreshWork = true;

                            //Seta a flag Z
                            char[] ValueArray = sStatus.ToCharArray();
                            //Flag de Zero (bit 2 de status, bit 5 no array (byte invertido))
                            ValueArray[5] = '1';
                            Valor = new string(ValueArray);

                            sStatus = Valor;
                            refreshStatus = true;

                            break;
                        }
                    #endregion

                    #region NOP
                    case "NOP":     //UM CICLO DE INATIVIDADE
                        {
                            break;
                        }
                    #endregion

                    #region ADDWF
                    case "ADDWF":   //FAZ ADIÇÃO DO NÚMERO DA WORK COM O VALOR DE UM REGISTRADOR
                        {
                            Endereço = Convert.ToInt32(memoria[Atual, 3].Substring(2, 2), 16); //ENDEREÇO DO REGISTRADOR
                            //Lê o valor de W
                            Byte byte2 = Convert.ToByte(sWork, 2);
                            //Lê o valor do registrador
                            Byte byte1 = Convert.ToByte(bank[selBank, Endereço, 2], 2);
                            //Realiza a operação
                            int byteR = byte2 + byte1;

                            //Converte o resultado para string binária
                            string s = Convert.ToString((byte)byteR, 2);
                            while (s.Length < 8)
                            {
                                s = "0" + s;
                            }

                            //Testa o destino da operação
                            if ((memoria[Atual, 2] == "R") || (memoria[Atual, 2] == "F")) //Compatibilização
                            {
                                bank[selBank, Endereço, 2] = s;
                                refreshReg = true;
                            }
                            else
                            {
                                sWork = s;
                                refreshWork = true;
                            }

                            //Converte Status em array de bytes
                            char[] ValueArray = sStatus.ToCharArray();
                            //Testa se deu Zero
                            if ((byteR & 0XFF) == 0)
                                ValueArray[5] = '1';
                            else
                                ValueArray[5] = '0';
                            //Testa se deu Carry
                            if (byteR > 255)
                                ValueArray[7] = '1';
                            else
                                ValueArray[7] = '0';
                            //Força estado /Time Out e /Power Down  para manter o valor mesmo se o destino da operação for ele: ADDWF STATUS,F
                            ValueArray[4] = '1';
                            ValueArray[3] = '1';
                            //Atualiza Status
                            string Value = new string(ValueArray);
                            sStatus = Value;
                            refreshStatus = true;

                            break;
                        }
                    #endregion

                    #region ANDWF
                    case "ANDWF":   //LÓGICA AND COM O NÚMERO DA WORK COM O VALOR DE UM REGISTRADOR
                        {
                            Endereço = Convert.ToInt32(memoria[Atual, 3].Substring(2, 2), 16); //ENDEREÇO DO REGISTRADOR
                            //Lê o valor de W
                            Byte byte2 = Convert.ToByte(sWork, 2);
                            //Lê o valor do registrador
                            Byte byte1 = Convert.ToByte(bank[selBank, Endereço, 2], 2);
                            //Realiza a operação
                            int byteR = byte2 & byte1;

                            //Converte o resultado para string binária
                            string s = Convert.ToString((byte)byteR, 2);
                            while (s.Length < 8)
                            {
                                s = "0" + s;
                            }

                            //Testa o destino da operação
                            if ((memoria[Atual, 2] == "R") || (memoria[Atual, 2] == "F")) //Compatibilização
                            {
                                bank[selBank, Endereço, 2] = s;
                                refreshReg = true;
                            }
                            else
                            {
                                sWork = s;
                                refreshWork = true;
                            }

                            //Converte Status em array de bytes
                            char[] ValueArray = sStatus.ToCharArray();
                            //Testa se deu Zero
                            if (byteR == 0)
                                ValueArray[5] = '1';
                            else
                                ValueArray[5] = '0';
                            //Testa se deu Carry
                            //if (byteR > 255)
                            //    ValueArray[7] = '1';
                            //else
                            //    ValueArray[7] = '0';
                            //Força estado /Time Out e /Power Down  para manter o valor mesmo se o destino da operação for ele: ADDWF STATUS,F
                            ValueArray[4] = '1';
                            ValueArray[3] = '1';
                            //Atualiza Status
                            string Value = new string(ValueArray);
                            sStatus = Value;
                            refreshStatus = true;

                            break;
                        }
                    #endregion

                    #region COMF
                    case "COMF":
                        {
                            Endereço = Convert.ToInt32(memoria[Atual, 3].Substring(2, 2), 16); //ENDEREÇO DO REGISTRADOR
                            //Lê o valor de W
                            //Byte byte2 = Convert.ToByte(sWork, 2);
                            //Lê o valor do registrador
                            Byte byte1 = Convert.ToByte(bank[selBank, Endereço, 2], 2);
                            //Realiza a operação
                            int byteR = ~byte1;

                            //Converte o resultado para string binária
                            string s = Convert.ToString((byte)byteR, 2);
                            while (s.Length < 8)
                            {
                                s = "0" + s;
                            }

                            //Testa o destino da operação
                            if ((memoria[Atual, 2] == "R") || (memoria[Atual, 2] == "F")) //Compatibilização
                            {
                                bank[selBank, Endereço, 2] = s;
                                refreshReg = true;
                            }
                            else
                            {
                                sWork = s;
                                refreshWork = true;
                            }

                            //Converte Status em array de bytes
                            char[] ValueArray = sStatus.ToCharArray();
                            //Testa se deu Zero
                            if ((byteR & 0XFF) == 0)
                                ValueArray[5] = '1';
                            else
                                ValueArray[5] = '0';
                            //Testa se deu Carry
                            //if (byteR > 255)
                            //    ValueArray[7] = '1';
                            //else
                            //    ValueArray[7] = '0';
                            //Força estado /Time Out e /Power Down  para manter o valor mesmo se o destino da operação for ele: ADDWF STATUS,F
                            ValueArray[4] = '1';
                            ValueArray[3] = '1';
                            //Atualiza Status
                            string Value = new string(ValueArray);
                            sStatus = Value;
                            refreshStatus = true;

                            break;
                        }
                    #endregion

                    #region DECF
                    case "DECF":    //DECREMETO DE UM VALOR DO REGISTRADOR
                        {
                            Endereço = Convert.ToInt32(memoria[Atual, 3].Substring(2, 2), 16); //ENDEREÇO DO REGISTRADOR
                            //Lê o valor de W
                            //Byte byte2 = Convert.ToByte(sWork, 2);
                            //Lê o valor do registrador
                            Byte byte1 = Convert.ToByte(bank[selBank, Endereço, 2], 2);
                            //Realiza a operação
                            int byteR;
                            if (byte1 == 0)
                                byteR = 255;
                            else
                                byteR = byte1 - 1;

                            //Converte o resultado para string binária
                            string s = Convert.ToString((byte)byteR, 2);
                            while (s.Length < 8)
                            {
                                s = "0" + s;
                            }

                            //Testa o destino da operação
                            if ((memoria[Atual, 2] == "R") || (memoria[Atual, 2] == "F")) //Compatibilização
                            {
                                bank[selBank, Endereço, 2] = s;
                                refreshReg = true;
                            }
                            else
                            {
                                sWork = s;
                                refreshWork = true;
                            }

                            //Converte Status em array de bytes
                            char[] ValueArray = sStatus.ToCharArray();
                            //Testa se deu Zero
                            if (byteR == 0)
                                ValueArray[5] = '1';
                            else
                                ValueArray[5] = '0';
                            //Testa se deu /Borrow (lógica invertida)
                            if (byteR == 255)
                                ValueArray[7] = '0';
                            else
                                ValueArray[7] = '1';
                            //Força estado /Time Out e /Power Down  para manter o valor mesmo se o destino da operação for ele: ADDWF STATUS,F
                            ValueArray[4] = '1';
                            ValueArray[3] = '1';
                            //Atualiza Status
                            string Value = new string(ValueArray);
                            sStatus = Value;
                            refreshStatus = true;

                            break;
                        }
                    #endregion

                    #region DECFSZ
                    case "DECFSZ":  //DECREMENTA UM VALOR DO REGISTRADOR, SE CHEGAR A 0, PULA 1 LINHA
                        {
                            Endereço = Convert.ToInt32(memoria[Atual, 3].Substring(2, 2), 16); //ENDEREÇO DO REGISTRADOR
                            //Lê o valor de W
                            //Byte byte2 = Convert.ToByte(sWork, 2);
                            //Lê o valor do registrador
                            Byte byte1 = Convert.ToByte(bank[selBank, Endereço, 2], 2);
                            //Realiza a operação
                            int byteR;
                            if (byte1 == 0)
                                byteR = 255;
                            else
                                byteR = byte1 - 1;
                            //Testa condição de desvio
                            if (byteR == 0)
                                PC++;

                            //Converte o resultado para string binária
                            string s = Convert.ToString((byte)byteR, 2);
                            while (s.Length < 8)
                            {
                                s = "0" + s;
                            }

                            //Testa o destino da operação
                            if ((memoria[Atual, 2] == "R") || (memoria[Atual, 2] == "F")) //Compatibilização
                            {
                                bank[selBank, Endereço, 2] = s;
                                refreshReg = true;
                            }
                            else
                            {
                                sWork = s;
                                refreshWork = true;
                            }

                            //Converte Status em array de bytes
                            char[] ValueArray = sStatus.ToCharArray();
                            //Testa se deu Zero
                            if (byteR == 0)
                                ValueArray[5] = '1';
                            else
                                ValueArray[5] = '0';
                            //Testa se deu /Borrow (lógica invertida)
                            //if (byteR == 255)
                            //    ValueArray[7] = '0';
                            //else
                            //    ValueArray[7] = '1';
                            //Força estado /Time Out e /Power Down  para manter o valor mesmo se o destino da operação for ele: ADDWF STATUS,F
                            ValueArray[4] = '1';
                            ValueArray[3] = '1';
                            //Atualiza Status
                            string Value = new string(ValueArray);
                            sStatus = Value;
                            refreshStatus = true;

                            break;
                        }
                    #endregion

                    #region INCF
                    case "INCF":    //INCREMENTO DE UM VALOR DO REGISTRADOR
                        {
                            Endereço = Convert.ToInt32(memoria[Atual, 3].Substring(2, 2), 16); //ENDEREÇO DO REGISTRADOR
                            //Lê o valor de W
                            //Byte byte2 = Convert.ToByte(sWork, 2);
                            //Lê o valor do registrador
                            Byte byte1 = Convert.ToByte(bank[selBank, Endereço, 2], 2);
                            //Realiza a operação
                            int byteR;
                            if (byte1 == 255)
                                byteR = 0;
                            else
                                byteR = byte1 + 1;

                            //Converte o resultado para string binária
                            string s = Convert.ToString((byte)byteR, 2);
                            while (s.Length < 8)
                            {
                                s = "0" + s;
                            }

                            //Testa o destino da operação
                            if ((memoria[Atual, 2] == "R") || (memoria[Atual, 2] == "F")) //Compatibilização
                            {
                                bank[selBank, Endereço, 2] = s;
                                refreshReg = true;
                            }
                            else
                            {
                                sWork = s;
                                refreshWork = true;
                            }

                            //Converte Status em array de bytes
                            char[] ValueArray = sStatus.ToCharArray();
                            //Testa se deu Zero
                            if (byteR == 0)
                                ValueArray[5] = '1';
                            else
                                ValueArray[5] = '0';
                            //Testa se deu Carry
                            if (byteR == 0)
                                ValueArray[7] = '1';
                            else
                                ValueArray[7] = '0';
                            //Força estado /Time Out e /Power Down  para manter o valor mesmo se o destino da operação for ele: ADDWF STATUS,F
                            ValueArray[4] = '1';
                            ValueArray[3] = '1';
                            //Atualiza Status
                            string Value = new string(ValueArray);
                            sStatus = Value;
                            refreshStatus = true;

                            break;
                        }
                    #endregion

                    #region INCFSZ
                    case "INCFSZ":  //INCREMENTO DE UM VALOR DO REGISTRADOR, SE ESTOURAR E CHEGA A 0, PULA 1 LINHA
                        {
                            Endereço = Convert.ToInt32(memoria[Atual, 3].Substring(2, 2), 16); //ENDEREÇO DO REGISTRADOR
                            //Lê o valor de W
                            //Byte byte2 = Convert.ToByte(sWork, 2);
                            //Lê o valor do registrador
                            Byte byte1 = Convert.ToByte(bank[selBank, Endereço, 2], 2);
                            //Realiza a operação
                            int byteR;
                            if (byte1 == 255)
                                byteR = 0;
                            else
                                byteR = byte1 + 1;
                            //Testa condição de desvio
                            if (byteR == 0)
                                PC++;

                            //Converte o resultado para string binária
                            string s = Convert.ToString((byte)byteR, 2);
                            while (s.Length < 8)
                            {
                                s = "0" + s;
                            }

                            //Testa o destino da operação
                            if ((memoria[Atual, 2] == "R") || (memoria[Atual, 2] == "F")) //Compatibilização
                            {
                                bank[selBank, Endereço, 2] = s;
                                refreshReg = true;
                            }
                            else
                            {
                                sWork = s;
                                refreshWork = true;
                            }

                            //Converte Status em array de bytes
                            char[] ValueArray = sStatus.ToCharArray();
                            //Testa se deu Zero
                            if (byteR == 0)
                                ValueArray[5] = '1';
                            else
                                ValueArray[5] = '0';
                            //Testa se deu Carry
                            //if (byteR == 255)
                            //    ValueArray[7] = '0';
                            //else
                            //    ValueArray[7] = '1';
                            //Força estado /Time Out e /Power Down  para manter o valor mesmo se o destino da operação for ele: ADDWF STATUS,F
                            ValueArray[4] = '1';
                            ValueArray[3] = '1';
                            //Atualiza Status
                            string Value = new string(ValueArray);
                            sStatus = Value;
                            refreshStatus = true;

                            break;
                        }
                    #endregion

                    #region IORWF
                    case "IORWF":
                        {
                            Endereço = Convert.ToInt32(memoria[Atual, 3].Substring(2, 2), 16); //ENDEREÇO DO REGISTRADOR
                            //Lê o valor de W
                            Byte byte2 = Convert.ToByte(sWork, 2);
                            //Lê o valor do registrador
                            Byte byte1 = Convert.ToByte(bank[selBank, Endereço, 2], 2);
                            //Realiza a operação
                            int byteR = byte2 | byte1;

                            //Converte o resultado para string binária
                            string s = Convert.ToString((byte)byteR, 2);
                            while (s.Length < 8)
                            {
                                s = "0" + s;
                            }

                            //Testa o destino da operação
                            if ((memoria[Atual, 2] == "R") || (memoria[Atual, 2] == "F")) //Compatibilização
                            {
                                bank[selBank, Endereço, 2] = s;
                                refreshReg = true;
                            }
                            else
                            {
                                sWork = s;
                                refreshWork = true;
                            }

                            //Converte Status em array de bytes
                            char[] ValueArray = sStatus.ToCharArray();
                            //Testa se deu Zero
                            if (byteR == 0)
                                ValueArray[5] = '1';
                            else
                                ValueArray[5] = '0';
                            //Testa se deu Carry
                            //if (byteR > 255)
                            //    ValueArray[7] = '1';
                            //else
                            //    ValueArray[7] = '0';
                            //Força estado /Time Out e /Power Down  para manter o valor mesmo se o destino da operação for ele: ADDWF STATUS,F
                            ValueArray[4] = '1';
                            ValueArray[3] = '1';
                            //Atualiza Status
                            string Value = new string(ValueArray);
                            sStatus = Value;
                            refreshStatus = true;

                            break;
                        }
                    #endregion

                    #region MOVF
                    case "MOVF":
                        {
                            Endereço = Convert.ToInt32(memoria[Atual, 3].Substring(2, 2), 16); //ENDEREÇO DO REGISTRADOR
                            //Lê o valor de W
                            //Byte byte2 = Convert.ToByte(sWork, 2);
                            //Lê o valor do registrador
                            Byte byte1 = Convert.ToByte(bank[selBank, Endereço, 2], 2);
                            //Realiza a operação
                            int byteR = byte1;

                            //Testa o destino da operação
                            if ((memoria[Atual, 2] == "R") || (memoria[Atual, 2] == "F")) //Compatibilização
                            {
                                //Não precisa atualizar
                                //bank[selBank, Endereço, 2] = bank[selBank, Endereço, 2];
                                refreshReg = true;
                            }
                            else
                            {
                                sWork = bank[selBank, Endereço, 2];
                                refreshWork = true;
                            }

                            //Converte Status em array de bytes
                            char[] ValueArray = sStatus.ToCharArray();
                            //Testa se deu Zero
                            if (byteR == 0)
                                ValueArray[5] = '1';
                            else
                                ValueArray[5] = '0';
                            //Testa se deu Carry
                            //if (byteR > 255)
                            //    ValueArray[7] = '1';
                            //else
                            //    ValueArray[7] = '0';
                            //Força estado /Time Out e /Power Down  para manter o valor mesmo se o destino da operação for ele: ADDWF STATUS,F
                            //ValueArray[4] = '1';
                            //ValueArray[3] = '1';
                            //Atualiza Status
                            string Value = new string(ValueArray);
                            sStatus = Value;
                            refreshStatus = true;

                            break;
                        }
                    #endregion

                    #region MOVWF
                    case "MOVWF":
                        {
                            Endereço = Convert.ToInt32(memoria[Atual, 3].Substring(2, 2), 16); //ENDEREÇO DO REGISTRADOR

                            bank[selBank, Endereço, 2] = sWork;
                            refreshReg = true;

                            //PODE SER REMOVIDO, TEM TRATAMENTO NO BLOCO Timer0
                            if (Endereço == 1)
                                if (selBank == 0 || selBank == 2)
                                    timer0 = Convert.ToInt32(sWork, 2);

                            //Tratamento especial TXREG (avisa a Serial Virtual que tem byte para ser lido)
                            if ((selBank == 0) && (Endereço == 0x19))
                                serialVirtual.Receber(sWork);

                            break;
                        }
                    #endregion

                    #region RLF
                    case "RLF":
                        {
                            Endereço = Convert.ToInt32(memoria[Atual, 3].Substring(2, 2), 16); //ENDEREÇO DO REGISTRADOR
                            //Lê o valor de W
                            //Byte byte2 = Convert.ToByte(sWork, 2);
                            //Lê o valor do registrador
                            Byte byte1 = Convert.ToByte(bank[selBank, Endereço, 2], 2);

                            //Converte Status em array de bytes
                            char[] ValueArray = sStatus.ToCharArray();

                            //Realiza a operação
                            int byteR;
                            byteR = byte1 << 1;
                            if (ValueArray[7] == '1')   //Se o Carry vale 1 
                                byteR++;                //Insere o Carry a direita do valor

                            //Converte o resultado para string binária
                            string s = Convert.ToString((byte)byteR, 2);
                            while (s.Length < 8)
                            {
                                s = "0" + s;
                            }

                            //Testa o destino da operação
                            if ((memoria[Atual, 2] == "R") || (memoria[Atual, 2] == "F")) //Compatibilização
                            {
                                bank[selBank, Endereço, 2] = s;
                                refreshReg = true;
                            }
                            else
                            {
                                sWork = s;
                                refreshWork = true;
                            }

                            //Converte Status em array de bytes
                            //char[] ValueArray = sStatus.ToCharArray();
                            //Testa se deu Zero
                            //if (byteR == 0)
                            //    ValueArray[5] = '1';
                            //else
                            //    ValueArray[5] = '0';
                            //Testa se deu Carry
                            if (byte1 >= 128)
                                ValueArray[7] = '1';
                            else
                                ValueArray[7] = '0';
                            //Força estado /Time Out e /Power Down  para manter o valor mesmo se o destino da operação for ele: ADDWF STATUS,F
                            ValueArray[4] = '1';
                            ValueArray[3] = '1';
                            //Atualiza Status
                            string Value = new string(ValueArray);
                            sStatus = Value;
                            refreshStatus = true;

                            break;
                        }
                    #endregion

                    #region RRF
                    case "RRF":
                        {
                            Endereço = Convert.ToInt32(memoria[Atual, 3].Substring(2, 2), 16); //ENDEREÇO DO REGISTRADOR
                            //Lê o valor de W
                            //Byte byte2 = Convert.ToByte(sWork, 2);
                            //Lê o valor do registrador
                            Byte byte1 = Convert.ToByte(bank[selBank, Endereço, 2], 2);

                            //Converte Status em array de bytes
                            char[] ValueArray = sStatus.ToCharArray();

                            //Realiza a operação
                            int byteR;
                            byteR = byte1 >> 1;
                            if (ValueArray[7] == '1')   //Se o Carry vale 1 
                                byteR += 128;           //Insere o Carry a esquerda do valor

                            //Converte o resultado para string binária
                            string s = Convert.ToString((byte)byteR, 2);
                            while (s.Length < 8)
                            {
                                s = "0" + s;
                            }

                            //Testa o destino da operação
                            if ((memoria[Atual, 2] == "R") || (memoria[Atual, 2] == "F")) //Compatibilização
                            {
                                bank[selBank, Endereço, 2] = s;
                                refreshReg = true;
                            }
                            else
                            {
                                sWork = s;
                                refreshWork = true;
                            }

                            //Converte Status em array de bytes
                            //char[] ValueArray = sStatus.ToCharArray();
                            //Testa se deu Zero
                            //if (byteR == 0)
                            //    ValueArray[5] = '1';
                            //else
                            //    ValueArray[5] = '0';
                            //Testa se deu Carry
                            if (byte1 % 2 == 1)
                                ValueArray[7] = '1';
                            else
                                ValueArray[7] = '0';
                            //Força estado /Time Out e /Power Down  para manter o valor mesmo se o destino da operação for ele: ADDWF STATUS,F
                            ValueArray[4] = '1';
                            ValueArray[3] = '1';
                            //Atualiza Status
                            string Value = new string(ValueArray);
                            sStatus = Value;
                            refreshStatus = true;

                            break;
                        }
                    #endregion

                    #region SUBWF
                    case "SUBWF":
                        {
                            //Tratei os valores como sinalizados!!!!

                            Endereço = Convert.ToInt32(memoria[Atual, 3].Substring(2, 2), 16); //ENDEREÇO DO REGISTRADOR
                            //Lê o valor de W
                            Byte byte2 = Convert.ToByte(sWork, 2);
                            //Lê o valor do registrador
                            Byte byte1 = Convert.ToByte(bank[selBank, Endereço, 2], 2);
                            //Realiza a operação
                            //TODO: Checar se esse resultado é válido
                            int byteR = byte1 - byte2;

                            //Converte o resultado para string binária
                            string s = Convert.ToString((byte)byteR, 2);
                            while (s.Length < 8)
                            {
                                s = "0" + s;
                            }

                            //Testa o destino da operação
                            if ((memoria[Atual, 2] == "R") || (memoria[Atual, 2] == "F")) //Compatibilização
                            {
                                bank[selBank, Endereço, 2] = s;
                                refreshReg = true;
                            }
                            else
                            {
                                sWork = s;
                                refreshWork = true;
                            }

                            //Converte Status em array de bytes
                            char[] ValueArray = sStatus.ToCharArray();
                            //Testa se deu Zero
                            if (byteR == 0)
                                ValueArray[5] = '1';
                            else
                                ValueArray[5] = '0';
                            //Testa se deu /Borrow (lógica invertida)
                            if (byteR > 0)
                                ValueArray[7] = '1';
                            else
                                ValueArray[7] = '0';
                            //Força estado /Time Out e /Power Down  para manter o valor mesmo se o destino da operação for ele: ADDWF STATUS,F
                            ValueArray[4] = '1';
                            ValueArray[3] = '1';
                            //Atualiza Status
                            string Value = new string(ValueArray);
                            sStatus = Value;
                            refreshStatus = true;

                            break;
                        }
                    #endregion

                    #region SWAPF
                    case "SWAPF":
                        {
                            Endereço = Convert.ToInt32(memoria[Atual, 3].Substring(2, 2), 16); //ENDEREÇO DO REGISTRADOR
                            //Lê o valor de W
                            //Byte byte2 = Convert.ToByte(sWork, 2);
                            //Lê o valor do registrador
                            Byte byte1 = Convert.ToByte(bank[selBank, Endereço, 2], 2);
                            //Realiza a operação
                            int temp1 = (byte1 & 0x0F) << 4;
                            int temp2 = (byte1 & 0xF0) >> 4;
                            int byteR = temp1 + temp2; ;

                            //Converte o resultado para string binária
                            string s = Convert.ToString((byte)byteR, 2);
                            while (s.Length < 8)
                            {
                                s = "0" + s;
                            }

                            //Testa o destino da operação
                            if ((memoria[Atual, 2] == "R") || (memoria[Atual, 2] == "F")) //Compatibilização
                            {
                                bank[selBank, Endereço, 2] = s;
                                //Se é o registrador Status
                                if (Endereço == 3)
                                {
                                    sStatus = s;
                                }
                                refreshReg = true;
                            }
                            else
                            {
                                sWork = s;
                                refreshWork = true;
                            }

                            if (Endereço == 3)
                            {
                                //Converte Status em array de bytes
                                char[] ValueArray = sStatus.ToCharArray();
                                //Testa se deu Zero
                                //if (byteR == 0)
                                //    ValueArray[5] = '1';
                                //else
                                //    ValueArray[5] = '0';
                                //Testa se deu Carry
                                //if (byteR > 255)
                                //    ValueArray[7] = '1';
                                //else
                                //    ValueArray[7] = '0';
                                //Força estado /Time Out e /Power Down  para manter o valor mesmo se o destino da operação for ele: ADDWF STATUS,F
                                ValueArray[4] = '1';
                                ValueArray[3] = '1';
                                //Atualiza Status
                                string Value = new string(ValueArray);
                                sStatus = Value;
                                refreshStatus = true;
                            }
                            break;
                        }
                    #endregion

                    #region XORWF
                    case "XORWF":
                        {
                            Endereço = Convert.ToInt32(memoria[Atual, 3].Substring(2, 2), 16); //ENDEREÇO DO REGISTRADOR
                            //Lê o valor de W
                            Byte byte2 = Convert.ToByte(sWork, 2);
                            //Lê o valor do registrador
                            Byte byte1 = Convert.ToByte(bank[selBank, Endereço, 2], 2);
                            //Realiza a operação
                            int byteR = byte2 ^ byte1;

                            //Converte o resultado para string binária
                            string s = Convert.ToString((byte)byteR, 2);
                            while (s.Length < 8)
                            {
                                s = "0" + s;
                            }

                            //Testa o destino da operação
                            if ((memoria[Atual, 2] == "R") || (memoria[Atual, 2] == "F")) //Compatibilização
                            {
                                bank[selBank, Endereço, 2] = s;
                                refreshReg = true;
                            }
                            else
                            {
                                sWork = s;
                                refreshWork = true;
                            }

                            //Converte Status em array de bytes
                            char[] ValueArray = sStatus.ToCharArray();
                            //Testa se deu Zero
                            if (byteR == 0)
                                ValueArray[5] = '1';
                            else
                                ValueArray[5] = '0';
                            //Testa se deu Carry
                            //if (byteR > 255)
                            //    ValueArray[7] = '1';
                            //else
                            //    ValueArray[7] = '0';
                            //Força estado /Time Out e /Power Down  para manter o valor mesmo se o destino da operação for ele: ADDWF STATUS,F
                            ValueArray[4] = '1';
                            ValueArray[3] = '1';
                            //Atualiza Status
                            string Value = new string(ValueArray);
                            sStatus = Value;
                            refreshStatus = true;

                            break;
                        }
                    #endregion

                    #region ADDLW
                    case "ADDLW":
                        {
                            //TODO: Validar lógica e necessidade de trabalhar com variáveis sinalizadas
                            //Endereço = Convert.ToInt32(memoria[Atual, 3].Substring(2, 2), 16); //ENDEREÇO DO REGISTRADOR
                            //Lê o valor de W
                            Byte byte2 = Convert.ToByte(sWork, 2);
                            //Lê o valor da constante
                            Byte byte1 = Convert.ToByte(memoria[Atual, 5], 2);
                            //Realiza a operação
                            int byteR = byte2 + byte1;

                            //Converte o resultado para string binária
                            string s = Convert.ToString((byte)byteR, 2);
                            while (s.Length < 8)
                            {
                                s = "0" + s;
                            }

                            //Destino sempre em W 
                            sWork = s;
                            refreshWork = true;

                            //Converte Status em array de bytes
                            char[] ValueArray = sStatus.ToCharArray();
                            //Testa se deu Zero
                            if ((byteR & 0XFF) == 0)
                                ValueArray[5] = '1';
                            else
                                ValueArray[5] = '0';
                            //Testa se deu Carry
                            if (byteR > 255)
                                ValueArray[7] = '1';
                            else
                                ValueArray[7] = '0';
                            //Força estado /Time Out e /Power Down  para manter o valor mesmo se o destino da operação for ele: ADDWF STATUS,F
                            //ValueArray[4] = '1';
                            //ValueArray[3] = '1';
                            //Atualiza Status
                            string Value = new string(ValueArray);
                            sStatus = Value;
                            refreshStatus = true;

                            break;
                        }
                    #endregion

                    #region ANDLW
                    case "ANDLW":
                        {
                            //TODO: Validar lógica e necessidade de trabalhar com variáveis sinalizadas
                            //Endereço = Convert.ToInt32(memoria[Atual, 3].Substring(2, 2), 16); //ENDEREÇO DO REGISTRADOR
                            //Lê o valor de W
                            Byte byte2 = Convert.ToByte(sWork, 2);
                            //Lê o valor da constante
                            Byte byte1 = Convert.ToByte(memoria[Atual, 5], 2);
                            //Realiza a operação
                            int byteR = byte2 & byte1;

                            //Converte o resultado para string binária
                            string s = Convert.ToString((byte)byteR, 2);
                            while (s.Length < 8)
                            {
                                s = "0" + s;
                            }

                            //Destino sempre em W 
                            sWork = s;
                            refreshWork = true;

                            //Converte Status em array de bytes
                            char[] ValueArray = sStatus.ToCharArray();
                            //Testa se deu Zero
                            if (byteR == 0)
                                ValueArray[5] = '1';
                            else
                                ValueArray[5] = '0';
                            //Testa se deu Carry
                            //if (byteR > 255)
                            //    ValueArray[7] = '1';
                            //else
                            //    ValueArray[7] = '0';
                            //Força estado /Time Out e /Power Down  para manter o valor mesmo se o destino da operação for ele: ADDWF STATUS,F
                            //ValueArray[4] = '1';
                            //ValueArray[3] = '1';
                            //Atualiza Status
                            string Value = new string(ValueArray);
                            sStatus = Value;
                            refreshStatus = true;

                            break;
                        }
                    #endregion

                    #region CLRWDT
                    case "CLRWDT":
                        {
                            break;
                        }
                    #endregion

                    #region IORLW
                    case "IORLW":
                        {
                            //TODO: Validar lógica e necessidade de trabalhar com variáveis sinalizadas
                            //Endereço = Convert.ToInt32(memoria[Atual, 3].Substring(2, 2), 16); //ENDEREÇO DO REGISTRADOR
                            //Lê o valor de W
                            Byte byte2 = Convert.ToByte(sWork, 2);
                            //Lê o valor da constante
                            Byte byte1 = Convert.ToByte(memoria[Atual, 5], 2);
                            //Realiza a operação
                            int byteR = byte2 | byte1;

                            //Converte o resultado para string binária
                            string s = Convert.ToString((byte)byteR, 2);
                            while (s.Length < 8)
                            {
                                s = "0" + s;
                            }

                            //Destino sempre em W 
                            sWork = s;
                            refreshWork = true;

                            //Converte Status em array de bytes
                            char[] ValueArray = sStatus.ToCharArray();
                            //Testa se deu Zero
                            if (byteR == 0)
                                ValueArray[5] = '1';
                            else
                                ValueArray[5] = '0';
                            //Testa se deu Carry
                            //if (byteR > 255)
                            //    ValueArray[7] = '1';
                            //else
                            //    ValueArray[7] = '0';
                            //Força estado /Time Out e /Power Down  para manter o valor mesmo se o destino da operação for ele: ADDWF STATUS,F
                            //ValueArray[4] = '1';
                            //ValueArray[3] = '1';
                            //Atualiza Status
                            string Value = new string(ValueArray);
                            sStatus = Value;
                            refreshStatus = true;

                            break;
                        }
                    #endregion

                    #region RETFIE
                    case "RETFIE":
                        {
                            //Se a pilha não está vazia remove um registro
                            if (picStack.Count > 0)
                                PC = (int)picStack.Pop();
                            //Seta o bit GIE do registrador INTCON
                            char[] ValueArray = bank[0, 11, 2].ToCharArray();

                            ValueArray[0] = '1';

                            string Value = new string(ValueArray);

                            //Refresh da matriz
                            for (int i = 0; i < 4; i++)
                            {
                                bank[i, 11, 2] = Value;
                            }
                            refreshIntcon = true;
                            //Sinaliza refresh do stack
                            refreshStack_pop = true;

                            break;
                        }
                    #endregion

                    #region SLEEP
                    case "SLEEP":
                        {
                            break;
                        }
                    #endregion

                    #region SUBLW
                    case "SUBLW":
                        {
                            //Tratei os valores como sinalizados!!!!

                            //TODO: Validar lógica e necessidade de trabalhar com variáveis sinalizadas
                            //Endereço = Convert.ToInt32(memoria[Atual, 3].Substring(2, 2), 16); //ENDEREÇO DO REGISTRADOR
                            //Lê o valor de W
                            Byte byte2 = Convert.ToByte(sWork, 2);
                            //Lê o valor da constante
                            Byte byte1 = Convert.ToByte(memoria[Atual, 5], 2);
                            //Realiza a operação
                            int byteR = byte1 - byte2;

                            //Converte o resultado para string binária
                            string s = Convert.ToString((byte)byteR, 2);
                            while (s.Length < 8)
                            {
                                s = "0" + s;
                            }

                            //Destino sempre em W 
                            sWork = s;
                            refreshWork = true;

                            //Converte Status em array de bytes
                            char[] ValueArray = sStatus.ToCharArray();
                            //Testa se deu Zero
                            if (byteR == 0)
                                ValueArray[5] = '1';
                            else
                                ValueArray[5] = '0';
                            //Testa se deu /Borrow (lógica invertida)
                            if (byteR > 0)
                                ValueArray[7] = '1';
                            else
                                ValueArray[7] = '0';
                            //Força estado /Time Out e /Power Down  para manter o valor mesmo se o destino da operação for ele: ADDWF STATUS,F
                            //ValueArray[4] = '1';
                            //ValueArray[3] = '1';
                            //Atualiza Status
                            string Value = new string(ValueArray);
                            sStatus = Value;
                            refreshStatus = true;

                            break;
                        }
                    #endregion

                    #region XORLW
                    case "XORLW":
                        {
                            //TODO: Validar lógica e necessidade de trabalhar com variáveis sinalizadas
                            //Endereço = Convert.ToInt32(memoria[Atual, 3].Substring(2, 2), 16); //ENDEREÇO DO REGISTRADOR
                            //Lê o valor de W
                            Byte byte2 = Convert.ToByte(sWork, 2);
                            //Lê o valor da constante
                            Byte byte1 = Convert.ToByte(memoria[Atual, 5], 2);
                            //Realiza a operação
                            int byteR = byte2 ^ byte1;

                            //Converte o resultado para string binária
                            string s = Convert.ToString((byte)byteR, 2);
                            while (s.Length < 8)
                            {
                                s = "0" + s;
                            }

                            //Destino sempre em W 
                            sWork = s;
                            refreshWork = true;

                            //Converte Status em array de bytes
                            char[] ValueArray = sStatus.ToCharArray();
                            //Testa se deu Zero
                            if (byteR == 0)
                                ValueArray[5] = '1';
                            else
                                ValueArray[5] = '0';
                            //Testa se deu Carry
                            //if (byteR > 255)
                            //    ValueArray[7] = '1';
                            //else
                            //    ValueArray[7] = '0';
                            //Força estado /Time Out e /Power Down  para manter o valor mesmo se o destino da operação for ele: ADDWF STATUS,F
                            //ValueArray[4] = '1';
                            //ValueArray[3] = '1';
                            //Atualiza Status
                            string Value = new string(ValueArray);
                            sStatus = Value;
                            refreshStatus = true;

                            break;
                        }
                    #endregion
                }
                #endregion

                #region Condicionais para refresh (cbRefresh.Checked)
                if (cbRefresh.Checked)
                {
                    //Visualização da próxima instrução a ser executada
                    dgvMemoria.CurrentCell = dgvMemoria.Rows[PC].Cells[0];
                    //dgvMemoria.Refresh();

                    //Refresh do Work
                    if (refreshWork)
                    {
                        dgvWork.Rows[0].Cells[0].Value = sWork;
                        dgvWork.Refresh();
                        refreshWork = false;
                    }

                    //Refresh de registrador
                    if (refreshReg)
                    {
                        switch (selBank)
                        {
                            case 0:
                                dgvBank0.Rows[Endereço].Cells[2].Value = bank[selBank, Endereço, 2];    //Atualiza o valor da célula
                                dgvBank0.CurrentCell = dgvBank0.Rows[Endereço].Cells[0];    //Coloca o foco nesta célula
                                //dgvBank0.Refresh();
                                break;
                            case 1:
                                dgvBank1.Rows[Endereço].Cells[2].Value = bank[selBank, Endereço, 2];    //Atualiza o valor da célula
                                dgvBank1.CurrentCell = dgvBank1.Rows[Endereço].Cells[0];
                                //Coloca o foco nesta célula//dgvBank1.Refresh();
                                break;
                            case 2:
                                dgvBank2.Rows[Endereço].Cells[2].Value = bank[selBank, Endereço, 2];    //Atualiza o valor da célula
                                dgvBank2.CurrentCell = dgvBank2.Rows[Endereço].Cells[0];    //Coloca o foco nesta célula
                                //dgvBank2.Refresh();
                                break;
                            case 3:
                                dgvBank3.Rows[Endereço].Cells[2].Value = bank[selBank, Endereço, 2];    //Atualiza o valor da célula
                                dgvBank3.CurrentCell = dgvBank3.Rows[Endereço].Cells[0];    //Coloca o foco nesta célula
                                //dgvBank3.Refresh();
                                break;
                        }

                        refreshReg = false;
                    }

                    //Refresh do Stack em push
                    if (refreshStack_push)
                    {
                        //Se a pilha esta cheia remove o último registro
                        if (dgvStack.Rows.Count >= 8)
                            dgvStack.Rows.RemoveAt(0);
                        //Insere o valor do program counter na pilha    
                        dgvStack.Rows.Insert(0, picStack.Peek().ToString());
                        dgvStack.CurrentCell = dgvStack.Rows[0].Cells[0];   //Deve vir depois do insert para evitar erros
                        //dgvStack.Refresh();

                        refreshStack_push = false;
                    }

                    //Refresh do Stack em pop
                    if (refreshStack_pop)
                    {
                        //Se a pilha não está vazia remove um registro
                        if (dgvStack.Rows.Count > 0)
                        {
                            dgvStack.CurrentCell = dgvStack.Rows[0].Cells[0];   //Deve vir antes do remove para evitar erros
                            dgvStack.Rows.RemoveAt(0);
                            //dgvStack.Refresh();
                        }

                        refreshStack_pop = false;
                    }

                    //Refresh do Intcon (retorno de interrupção GIE = 1)
                    if (refreshIntcon)
                    {
                        //Refresh de INTCON nos 4 bancos

                        dgvBank0.Rows[11].Cells[2].Value = bank[0, 11, 2];
                        dgvBank0.CurrentCell = dgvBank0.Rows[11].Cells[0];
                        //dgvBank0.Refresh();

                        dgvBank1.Rows[11].Cells[2].Value = bank[1, 11, 2];
                        dgvBank1.CurrentCell = dgvBank1.Rows[11].Cells[0];
                        //dgvBank1.Refresh();

                        dgvBank2.Rows[11].Cells[2].Value = bank[2, 11, 2];
                        dgvBank2.CurrentCell = dgvBank2.Rows[11].Cells[0];
                        //dgvBank2.Refresh();

                        dgvBank3.Rows[11].Cells[2].Value = bank[3, 11, 2];
                        dgvBank3.CurrentCell = dgvBank3.Rows[11].Cells[0];
                        //dgvBank3.Refresh();

                        refreshIntcon = false;
                    }
                }
                #endregion

                #region Tratamento especial para Status
                //Refresh do Status
                if (refreshStatus)  //Deve ser o último a ser atualizado
                {
                    //Atualiza matriz
                    for (int i = 0; i < 4; i++)
                    {
                        bank[i, 3, 2] = sStatus;
                    }
                    //Atualiza banco selecionado
                    selBank = Convert.ToInt32(sStatus.Substring(1, 2), 2);
                    //Atualiza datagrids
                    if (cbRefresh.Checked)
                    {
                        dgvBank0.Rows[3].Cells[2].Value = sStatus;
                        //dgvBank0.Refresh();

                        dgvBank1.Rows[3].Cells[2].Value = sStatus;
                        //dgvBank1.Refresh();

                        dgvBank2.Rows[3].Cells[2].Value = sStatus;
                        //dgvBank2.Refresh();

                        dgvBank3.Rows[3].Cells[2].Value = sStatus;
                        //dgvBank3.Refresh();

                        dgvStatusReg.CurrentRow.Cells[0].Value = sStatus;
                        dgvStatusReg.Refresh();
                    }

                    refreshStatus = false;
                }
                #endregion

                #region Tratamento especial para Intcon
                //Refresh do Intcon
                if (Endereço == 11)   //Intcon (End.=11 em Bank0,1,2,3)
                {
                    //Atualiza matriz
                    for (int i = 0; i < 4; i++)
                    {
                        bank[i, 11, 2] = bank[selBank, 11, 2];    //Atualiza o valor da célula independente do banco selecionado
                    }
                    //Atualiza DataGrids
                    if (cbRefresh.Checked)
                    {
                        dgvBank0.Rows[11].Cells[2].Value = bank[0, 11, 2];  //Atualiza o valor da célula
                        dgvBank0.CurrentCell = dgvBank0.Rows[11].Cells[0];  //Coloca o foco nesta célula e já faz o refresh dela
                        //dgvBank0.Refresh();

                        dgvBank1.Rows[11].Cells[2].Value = bank[1, 11, 2];  //Atualiza o valor da célula
                        dgvBank1.CurrentCell = dgvBank1.Rows[11].Cells[0];  //Coloca o foco nesta célula e já faz o refresh dela
                        //dgvBank1.Refresh();

                        dgvBank2.Rows[11].Cells[2].Value = bank[2, 11, 2];  //Atualiza o valor da célula
                        dgvBank2.CurrentCell = dgvBank2.Rows[11].Cells[0];  //Coloca o foco nesta célula e já faz o refresh dela
                        //dgvBank2.Refresh();

                        dgvBank3.Rows[11].Cells[2].Value = bank[3, 11, 2];  //Atualiza o valor da célula
                        dgvBank3.CurrentCell = dgvBank3.Rows[11].Cells[0];  //Coloca o foco nesta célula e já faz o refresh dela
                        //dgvBank3.Refresh();
                    }
                }
                #endregion

                #region Tratamento especial para Option_Reg
                //Refresh do Option_Reg
                if (Endereço == 1 && ((selBank == 1) || (selBank == 3)))   //Option_Reg (End.=1 em Bank1 e Bank3)
                {
                    bank[1, 1, 2] = bank[3, 1, 2] = bank[selBank, 1, 2];    //Atualiza o valor da célula independente do banco selecionado

                    if (cbRefresh.Checked)
                    {

                        dgvBank1.Rows[1].Cells[2].Value = bank[selBank, 1, 2];  //Atualiza o valor da célula
                        dgvBank1.CurrentCell = dgvBank0.Rows[1].Cells[0];       //Coloca o foco nesta célula e já faz o refresh dela
                        //dgvBank1.Refresh();

                        dgvBank3.Rows[1].Cells[2].Value = bank[selBank, 1, 2];  //Atualiza o valor da célula
                        dgvBank3.CurrentCell = dgvBank3.Rows[1].Cells[0];       //Coloca o foco nesta célula e já faz o refresh dela
                        //dgvBank3.Refresh();
                    }

                    //Teste T0CS (bit 5 --> vetor 2)
                    if (bank[1, 1, 2].Substring(2, 1) == "0")
                    {
                        TMR0 = true;    //Temporizador
                    }
                    else
                    {
                        TMR0 = false;   //Contador (dasativado)
                    }
                    //Teste PS (bit 2,1,0 --> vetor 5,6,7)
                    if (bank[1, 1, 2].Substring(4, 1) == "0")
                    {   //Presacler no Timer0
                        PS = Convert.ToInt32(bank[1, 1, 2].Substring(5, 3), 2);
                        PS = (int)Math.Pow(2, PS) * 2;  //2^PS * 2
                    }
                    else
                    {   //Prescaler no WDT
                        PS = 1;
                    }
                }
                #endregion

                #region Tratamento especial para Timer0
                //Refresh do TMRO e timer0 (variável de controle)
                if (Endereço == 1 && ((selBank == 0) || (selBank == 2)))   //TMR0 (End.=1 em Bank0 e Bank2)
                {
                    bank[0, 1, 2] = bank[2, 1, 2] = bank[selBank, 1, 2];    //Atualiza o valor da célula independente do banco selecionado

                    timer0 = Convert.ToInt32(bank[0, 1, 2], 2);             //Atualiza o TIMER0 com o valor atribuido
                }

                if (TMR0 == true)
                {
                    if ((ticks % PS == 0) || (cbPrescaler.Checked))
                    {

                        if (timer0 == 255)
                        {
                            //Zera o timer (overflow)
                            timer0 = 0;

                            //Transforma a string de INTCON em vetor de caracteres
                            char[] ValueArray = bank[0, 11, 2].ToCharArray();
                            //Seta a flag TMR0IF (INTCON,2 --> ValueArray[5])
                            ValueArray[5] = '1';
                            //Transforma o vetor em string
                            string Value = new string(ValueArray);
                            //Refresh da matriz
                            for (int i = 0; i < 4; i++)
                            {
                                bank[i, 11, 2] = Value;
                            }
                            //Refresh dos bancos INTCON sinaliza flag do estouro do TIMER0 
                            if (cbRefresh.Checked)
                            {
                                dgvBank0.Rows[11].Cells[2].Value = Value;           //Atualiza o valor da célula
                                dgvBank0.CurrentCell = dgvBank0.Rows[11].Cells[0];  //Coloca o foco nesta célula e já faz o refresh dela

                                dgvBank1.Rows[11].Cells[2].Value = Value;           //Atualiza o valor da célula
                                dgvBank1.CurrentCell = dgvBank1.Rows[11].Cells[0];  //Coloca o foco nesta célula e já faz o refresh dela

                                dgvBank2.Rows[11].Cells[2].Value = Value;           //Atualiza o valor da célula
                                dgvBank2.CurrentCell = dgvBank2.Rows[11].Cells[0];  //Coloca o foco nesta célula e já faz o refresh dela

                                dgvBank3.Rows[11].Cells[2].Value = Value;           //Atualiza o valor da célula
                                dgvBank3.CurrentCell = dgvBank3.Rows[11].Cells[0];  //Coloca o foco nesta célula e já faz o refresh dela
                            }
                        }
                        else
                        {
                            timer0++;
                        }
                        string tm = Convert.ToString(timer0, 2);
                        while (tm.Length < 8)
                        {
                            tm = "0" + tm;
                        }
                        //Refresh da matriz
                        bank[0, 1, 2] = tm;
                        bank[2, 1, 2] = tm;
                        //Refresh dos bancos 
                        //Observação: Desativei seleção de célula atual por conta do timer estar sempre ligado e
                        //atrapalhar a visualização dos outros registrados manipulados
                        if (cbRefresh.Checked)
                        {
                            //dgvBank0.CurrentCell = dgvBank0.Rows[1].Cells[0];
                            dgvBank0.Rows[1].Cells[2].Value = tm;
                            //dgvBank2.CurrentCell = dgvBank2.Rows[1].Cells[0];
                            dgvBank2.Rows[1].Cells[2].Value = tm;
                        }
                    }
                }
                #endregion

                #region Tratamento especial endereços 0x70~0x7F espelhados entre os bancos
                //Refresh dos endereços 0x70~0x7F
                if (Endereço >= 0x70)   //Endereços espelhados entre bancos 0, 1, 2 e 3)
                {
                    //Atualiza matriz
                    for (int i = 0; i < 4; i++)
                    {
                        bank[i, Endereço, 2] = bank[selBank, Endereço, 2];    //Atualiza o valor da célula independente do banco selecionado
                    }

                    if (cbRefresh.Checked)
                    {
                        //Não vou forçar o refresh por se tratarem de 4 registradores, apenas alterar o valor
                        dgvBank0.Rows[Endereço].Cells[2].Value = bank[0, Endereço, 2];  //Atualiza o valor da célula
                        dgvBank0.CurrentCell = dgvBank0.Rows[Endereço].Cells[0];    //Coloca o foco nesta célula e já faz o refresh dela

                        dgvBank1.Rows[Endereço].Cells[2].Value = bank[1, Endereço, 2];  //Atualiza o valor da célula
                        dgvBank1.CurrentCell = dgvBank1.Rows[Endereço].Cells[0];    //Coloca o foco nesta célula e já faz o refresh dela

                        dgvBank2.Rows[Endereço].Cells[2].Value = bank[2, Endereço, 2];  //Atualiza o valor da célula
                        dgvBank2.CurrentCell = dgvBank2.Rows[Endereço].Cells[0];    //Coloca o foco nesta célula e já faz o refresh dela

                        dgvBank3.Rows[Endereço].Cells[2].Value = bank[3, Endereço, 2];  //Atualiza o valor da célula
                        dgvBank3.CurrentCell = dgvBank3.Rows[Endereço].Cells[0];    //Coloca o foco nesta célula e já faz o refresh dela                        
                    }
                }
                #endregion

                TestaBotoesTeclado();       //Testa o estado dos botões e o teclado matricial (PORTB)
                AtualizaLedsDisplays();     //Atualiza o estado dos leds e displays de 7 segmentos (PORTD)
            }
            catch (Exception ex)
            {
                clock.Enabled = false;
                MessageBox.Show("Erro ao executar uma instrução.\n" + ex.Message, "ERRO",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Reset();
            }
        }
        /// <summary>
        /// Interpreta e executa cada uma das instruções de acordo com o 
        /// endereço apontado pela program counter (PC).
        /// </summary>
        private void Clock_Run_Binario()
        {
            //Variáveis locais
            int Endereço = -1;

            try
            {
                #region EXECUÇÃO DOS COMANDOS ///////////////////////////////////////////////////////////////////////////////
                //Instrução atual é igual ao valor do contador de programas
                Atual = PC;
                //Incrementa o contador de programas para apontar a próxima instrução
                PC++;
                PC %= 8192;             //Limita o valor de PC ao tamanho da memória
                //Tipo de instrução
                int tipo;
                //Verifica os 2 bits mais significativos das instruções
                tipo = memoria_bin[Atual] & 0x3000;

                //Índice do bit
                int x;
                //Máscara do bit
                int mbit = 1;

                //Literal
                int literal = memoria_bin[Atual] & 0x00FF;
                bool DC;    //Digit Carry (Status.1)

                //Destino
                bool destino = false;   //Padrão salvar em W (fiz isso por conta do refresh do Status e outros registradores duplicados)
                //Temporário
                int temp;
                //Previsão de Carry
                bool preCarry;
                //Endereçamento indireto (06/09/16)
                bool indf = false;

                //PCL sempre tem o valor de PC
                //Refresh de PLC nos 4 bancos
                bank_bin[0, 0x02] = PC & 0xFF;
                bank_bin[1, 0x02] = PC & 0xFF;
                bank_bin[2, 0x02] = PC & 0xFF;
                bank_bin[3, 0x02] = PC & 0xFF;

                //Executa a instrução atual

                //Teste se é CALL ou GOTO
                if (tipo == 0x2000)
                {
                    switch (memoria_bin[Atual] & 0x3800)
                    {
                        #region CALL
                        case 0x2000:    //ALTERA STACK
                            //Se a pilha esta cheia remove o último registro
                            if (picStack.Count >= 8) picStack.Pop();
                            //Insere o valor do program counter na pilha
                            picStack.Push(PC);
                            //Atualiza program counter com o valor de desvio
                            //PC = memoria_bin[Atual] & 0x07FF;
                            //Separa os bits 4 e 3 de PCLATH (06/09/16)
                            int pclath43call = (bank_bin[0, 0x0A] & 0x18) << 8;
                            PC = pclath43call + (memoria_bin[Atual] & 0x07FF);
                            //Sinaliza refresh do stack
                            refreshStack_push = true;
                            break;
                        #endregion
                        #region GOTO
                        case 0x2800:    //PULO PARA ENDEREÇO
                            {
                                //PC = (int)memoria_bin[Atual] & 0x07FF;
                                //Separa os bits 4 e 3 de PCLATH (06/09/16)
                                int pclath43goto = (bank_bin[0, 0x0A] & 0x18) << 8;
                                PC = pclath43goto + (memoria_bin[Atual] & 0x07FF);
                                break;
                            }
                        #endregion
                    }
                }
                //Teste se é orientada ao bit
                else if (tipo == 0x1000)
                {
                    //Endereço do registrador
                    Endereço = memoria_bin[Atual] & 0x007F;
                    //Índice do bit
                    x = (memoria_bin[Atual] & 0x0380) >> 7;
                    //Máscara do bit
                    mbit = 1;
                    mbit = mbit << x;

                    //Tratamento especial para INDF (06/09/16)
                    //Preparação
                    if (Endereço == 0)
                    {
                        //Seta flag de sinalização
                        indf = true;
                        //Lê FSR (File Select Regisiter) bits 6..0
                        Endereço = bank_bin[0, 4] & 0x7F;
                        //Atualiza banco selecionado
                        //          Status,IRP                   + FSR,7
                        selBank = ((bank_bin[0, 3] & 0x80) >> 6) + ((bank_bin[0, 4] & 0x80) >> 7);
                    }

                    switch (memoria_bin[Atual] & 0x3C00)
                    {
                        #region BCF
                        case 0x1000:    //LIMPA UM ÚNICO BIT EM UM REGISTRADOR
                            //Atualiza o registrador
                            bank_bin[selBank, Endereço] = (bank_bin[selBank, Endereço] & (~mbit)) & 0x00FF;
                            //Sinaliza necessidade de atualização de registrador
                            refreshReg = true;
                            //Sianliza que um registrador foi alterado e precisa ser tratados
                            destino = true;
                            //Se alterou o registrador Status deve espelhar no 4 bancos
                            if (Endereço == 3)
                            {
                                Status_bin = bank_bin[selBank, Endereço];
                                refreshStatus = true;
                            }
                            break;
                        #endregion
                        #region BSF
                        case 0x1400:    //SETA UM ÚNICO BIT EM UM REGISTRADOR
                            //Atualiza o registrador
                            bank_bin[selBank, Endereço] = (bank_bin[selBank, Endereço] | mbit) & 0x00FF;
                            //Sinaliza necessidade de atualização de registrador
                            refreshReg = true;
                            //Sianliza que um registrador foi alterado e precisa ser tratados
                            destino = true;
                            //Se alterou o registrador Status deve espelhar no 4 bancos
                            if (Endereço == 3)
                            {
                                Status_bin = bank_bin[selBank, Endereço];
                                refreshStatus = true;
                            }
                            break;
                        #endregion
                        #region BTFSC
                        case 0x1800:    //TESTA UM ÚNICO BIT EM UM REGISTRADOR, SE FOR 0, PULA 1 ENDEREÇO, SENÃO CONTINUA
                            if ((bank_bin[selBank, Endereço] & mbit) == 0)
                                PC++;
                            //Permite a visualização do registrador testado
                            refreshReg = true;
                            break;
                        #endregion
                        #region BTFSS
                        case 0x1C00:    //TESTA UM ÚNICO BIT EM UM REGISTRADOR, SE FOR 1, PULA 1 ENDEREÇO, SENÃO CONTINUA
                            if ((bank_bin[selBank, Endereço] & mbit) != 0)
                                PC++;
                            //Permite a visualização do registrador testado
                            refreshReg = true;
                            break;
                        #endregion
                    }
                }
                //Teste se é literal
                else if (tipo == 0x3000)
                {
                    //Sinaliza necessidade de atualização de registrador
                    refreshWork = true;

                    switch (memoria_bin[Atual] & 0x3F00)
                    {
                        #region ADDLW
                        case 0x3E00:
                        case 0x3F00:
                            //Verifica o estado do 4o bit
                            if ((Work_bin & 0x10) > 0) DC = true;
                            else DC = false;
                            //Realiza a operação
                            Work_bin += literal;
                            //Se deu carry
                            if (Work_bin > 255) Status_bin |= 0x01;   //Seta o bit C do registrador Status
                            else Status_bin &= 0xFE;                  //Reseta o bit C do registrador Status
                            //Se deu digit carry
                            if (((Work_bin & 0x10) > 0) && (DC == false)) Status_bin |= 0x02; //Seta o bit DC do registrador Status
                            else Status_bin &= 0xFD;                                          //Reseta o bit DC do registrador Status
                            //Ajusta o resultado da operação
                            Work_bin &= 0xFF;
                            //Se deu zero
                            if (Work_bin == 0) Status_bin |= 0x04;    //Seta o bit Z do registrador Status
                            else Status_bin &= 0xFB;                  //Reseta o bit Z do registrador Status
                            //Sinaliza necessidade de atualização de registrador
                            refreshStatus = true;
                            break;
                        #endregion
                        #region ANDLW
                        case 0x3900:
                            //Realiza a operação
                            Work_bin &= literal;
                            //Ajusta o resultado da operação
                            Work_bin &= 0xFF;
                            //Se deu zero
                            if (Work_bin == 0) Status_bin |= 0x04;    //Seta o bit Z do registrador Status
                            else Status_bin &= 0xFB;                  //Reseta o bit Z do registrador Status
                            //Sinaliza necessidade de atualização de registrador
                            refreshStatus = true;
                            break;
                        #endregion
                        #region IORLW
                        case 0x3800:
                            //Realiza a operação
                            Work_bin |= literal;
                            //Ajusta o resultado da operação
                            Work_bin &= 0xFF;
                            //Se deu zero
                            if (Work_bin == 0) Status_bin |= 0x04;    //Seta o bit Z do registrador Status
                            else Status_bin &= 0xFB;                  //Reseta o bit Z do registrador Status
                            //Sinaliza necessidade de atualização de registrador
                            refreshStatus = true;
                            break;
                        #endregion
                        #region MOVLW
                        case 0x3000:
                        case 0x3300:
                            //Realiza a operação
                            Work_bin = literal;
                            //Ajusta o resultado da operação
                            Work_bin &= 0xFF;
                            break;
                        #endregion
                        #region RETLW
                        case 0x3400:  //RETORNA PARA POSIÇÃO DO STACK E RETORNA UM NÚMERO PARA WORK
                        case 0x3700:
                            //Se a pilha não está vazia remove um registro
                            if (picStack.Count > 0) PC = (int)picStack.Pop();
                            //Grava em W o valor deretorno
                            Work_bin = literal;
                            //Sinaliza refresh do stack
                            refreshStack_pop = true;
                            break;
                        #endregion
                        #region SUBLW
                        case 0x3C:
                        case 0x3D:
                            //Verifica o estado do 4o bit
                            if ((Work_bin & 0x10) > 0) DC = true;
                            else DC = false;
                            //Realiza a operação
                            Work_bin = literal - Work_bin;
                            //Se deu borrow carry 
                            if (Work_bin > 0) Status_bin |= 0x01; //Seta o bit C do registrador Status
                            else Status_bin &= 0xFE;              //Reseta o bit C do registrador Status
                            //Se deu digit carry
                            if (((Work_bin & 0x10) > 0) && (DC == false)) Status_bin |= 0x02; //Seta o bit DC do registrador Status
                            else Status_bin &= 0xFD;                                          //Reseta o bit DC do registrador Status
                            //Ajusta o resultado da operação
                            Work_bin &= 0xFF;
                            //Se deu zero
                            if (Work_bin == 0) Status_bin |= 0x04;    //Seta o bit Z do registrador Status
                            else Status_bin &= 0xFB;                  //Reseta o bit Z do registrador Status
                            //Sinaliza necessidade de atualização de registrador
                            refreshStatus = true;
                            break;
                        #endregion
                        #region XORLW
                        case 0x3A00:
                            //Realiza a operação
                            Work_bin ^= literal;
                            //Ajusta o resultado da operação
                            Work_bin &= 0xFF;
                            //Se deu zero
                            if (Work_bin == 0) Status_bin |= 0x04;    //Seta o bit Z do registrador Status
                            else Status_bin &= 0xFB;                  //Reseta o bit Z do registrador Status
                            //Sinaliza necessidade de atualização de registrador
                            refreshStatus = true;
                            break;
                        #endregion
                    }
                }
                //Demais orientada ao byte e controle
                else
                {
                    //Controle
                    #region CRLWDT
                    if (memoria_bin[Atual] == 0x0064)
                    { }
                    #endregion
                    #region RETFIE
                    else if (memoria_bin[Atual] == 0x0009)
                    {
                        //Se a pilha não está vazia remove um registro
                        if (picStack.Count > 0) PC = (int)picStack.Pop();
                        //Sinaliza refresh do stack
                        refreshStack_pop = true;
                        //Seta o bit GIE do registrador INTCON
                        bank_bin[selBank, 0x0B] |= 0x80;
                        //Sinaliza necessidade de atualização de registrador
                        refreshIntcon = true;
                    }
                    #endregion
                    #region RETURN
                    else if (memoria_bin[Atual] == 0x0008)
                    {
                        //Se a pilha não está vazia remove um registro
                        if (picStack.Count > 0) PC = (int)picStack.Pop();
                        //Sinaliza refresh do stack
                        refreshStack_pop = true;
                    }
                    #endregion
                    #region SLEEP
                    else if (memoria_bin[Atual] == 0x0003)
                    { }
                    #endregion
                    //Orienta ao byte
                    else
                    {
                        //Endereço do registrador
                        Endereço = memoria_bin[Atual] & 0x007F;
                        //Destino
                        if ((memoria_bin[Atual] & 0x0080) > 0) destino = true;
                        else destino = false;

                        //Tratamento especial para INDF (06/09/16)
                        //Preparação
                        if (Endereço == 0)
                        {
                            //Seta flag de sinalização
                            indf = true;
                            //Lê FSR (File Select Regisiter) bits 6..0
                            Endereço = bank_bin[0, 4] & 0x7F;
                            //Atualiza banco selecionado
                            //          Status,IRP                   + FSR,7
                            selBank = ((bank_bin[0, 3] & 0x80) >> 6) + ((bank_bin[0, 4] & 0x80) >> 7);
                        }

                        switch (memoria_bin[Atual] & 0x3F00)
                        {
                            //Como essas operações podem ser feitas com o registrador Status,
                            //a alteração das flags é feita no registrador do Status do banco selecionado e
                            //por último a variável Stauts_bin é atulizada com seu valor e
                            //a sinalização de refreshStatus é feita
                            #region ADDWF
                            case 0x0700:
                                //Verifica o estado do 4o bit (segundo operador sempre)
                                if ((bank_bin[selBank, Endereço] & 0x10) > 0) DC = true;
                                else DC = false;
                                //Realiza a operação
                                temp = Work_bin + bank_bin[selBank, Endereço];
                                //Se deu carry
                                if (temp > 255) bank_bin[selBank, 3] |= 0x01;   //Seta o bit C do registrador Status
                                else bank_bin[selBank, 3] &= 0xFE;              //Reseta o bit C do registrador Status
                                //Se deu digit carry
                                if (((Work_bin & 0x10) > 0) && (DC == false)) bank_bin[selBank, 3] |= 0x02; //Seta o bit DC do registrador Status
                                else bank_bin[selBank, 3] &= 0xFD;                                          //Reseta o bit DC do registrador Status
                                //Ajusta o resultado da operação
                                temp &= 0xFF;
                                //Se deu zero
                                if (temp == 0) bank_bin[selBank, 3] |= 0x04;    //Seta o bit Z do registrador Status
                                else bank_bin[selBank, 3] &= 0xFB;              //Reseta o bit Z do registrador Status
                                //Atualiza o registrador destino da operação
                                if (destino)
                                {
                                    bank_bin[selBank, Endereço] = temp;
                                    //Sinaliza necessidade de atualização de registrador
                                    refreshReg = true;
                                }
                                else
                                {
                                    Work_bin = temp;
                                    //Sinaliza necessidade de atualização de registrador
                                    refreshWork = true;
                                }
                                //Sinaliza necessidade de atualização de registrador
                                Status_bin = bank_bin[selBank, 3];
                                refreshStatus = true;
                                break;
                            #endregion
                            #region ANDWF
                            case 0x0500:
                                //Realiza a operação
                                temp = Work_bin & bank_bin[selBank, Endereço];
                                //Ajusta o resultado da operação
                                temp &= 0xFF;
                                //Se deu zero
                                if (temp == 0) bank_bin[selBank, 3] |= 0x04;    //Seta o bit Z do registrador Status
                                else bank_bin[selBank, 3] &= 0xFB;              //Reseta o bit Z do registrador Status
                                //Atualiza o registrador destino da operação
                                if (destino)
                                {
                                    bank_bin[selBank, Endereço] = temp;
                                    //Sinaliza necessidade de atualização de registrador
                                    refreshReg = true;
                                }
                                else
                                {
                                    Work_bin = temp;
                                    //Sinaliza necessidade de atualização de registrador
                                    refreshWork = true;
                                }
                                //Sinaliza necessidade de atualização de registrador
                                Status_bin = bank_bin[selBank, 3];
                                refreshStatus = true;
                                break;
                            #endregion
                            #region CLRF ou CLRW
                            case 0x0100:
                                //Realiza a operação
                                temp = 0;
                                //Ajusta o resultado da operação
                                temp &= 0xFF;
                                //Se deu zero
                                if (temp == 0) bank_bin[selBank, 3] |= 0x04;    //Seta o bit Z do registrador Status
                                else bank_bin[selBank, 3] &= 0xFB;              //Reseta o bit Z do registrador Status
                                //Atualiza o registrador destino da operação
                                if (destino)
                                {
                                    //CLRF
                                    bank_bin[selBank, Endereço] = temp;
                                    //Sinaliza necessidade de atualização de registrador
                                    refreshReg = true;
                                }
                                else
                                {
                                    //CLRW
                                    Work_bin = temp;
                                    //Sinaliza necessidade de atualização de registrador
                                    refreshWork = true;
                                }
                                //Sinaliza necessidade de atualização de registrador
                                Status_bin = bank_bin[selBank, 3];
                                refreshStatus = true;
                                break;
                            #endregion
                            #region COMF
                            case 0x0900:
                                //Realiza a operação
                                temp = ~bank_bin[selBank, Endereço];
                                //Ajusta o resultado da operação
                                temp &= 0xFF;
                                //Se deu zero
                                if (temp == 0) bank_bin[selBank, 3] |= 0x04;    //Seta o bit Z do registrador Status
                                else bank_bin[selBank, 3] &= 0xFB;              //Reseta o bit Z do registrador Status
                                //Atualiza o registrador destino da operação
                                if (destino)
                                {
                                    bank_bin[selBank, Endereço] = temp;
                                    //Sinaliza necessidade de atualização de registrador
                                    refreshReg = true;
                                }
                                else
                                {
                                    Work_bin = temp;
                                    //Sinaliza necessidade de atualização de registrador
                                    refreshWork = true;
                                }
                                //Sinaliza necessidade de atualização de registrador
                                Status_bin = bank_bin[selBank, 3];
                                refreshStatus = true;
                                break;
                            #endregion
                            #region DECF
                            case 0x0300:
                                //Realiza a operação
                                temp = bank_bin[selBank, Endereço] - 1;
                                //Ajusta o resultado da operação
                                temp &= 0xFF;
                                //Se deu zero
                                if (temp == 0) bank_bin[selBank, 3] |= 0x04;    //Seta o bit Z do registrador Status
                                else bank_bin[selBank, 3] &= 0xFB;              //Reseta o bit Z do registrador Status
                                //Atualiza o registrador destino da operação
                                if (destino)
                                {
                                    bank_bin[selBank, Endereço] = temp;
                                    //Sinaliza necessidade de atualização de registrador
                                    refreshReg = true;
                                }
                                else
                                {
                                    Work_bin = temp;
                                    //Sinaliza necessidade de atualização de registrador
                                    refreshWork = true;
                                }
                                //Sinaliza necessidade de atualização de registrador
                                Status_bin = bank_bin[selBank, 3];
                                refreshStatus = true;
                                break;
                            #endregion
                            #region DECFSZ
                            case 0x0B00:
                                //Realiza a operação
                                temp = bank_bin[selBank, Endereço] - 1;
                                //Ajusta o resultado da operação
                                temp &= 0xFF;
                                //Se deu zero
                                if (temp == 0) PC++;    //Salta uma instrução
                                //Atualiza o registrador destino da operação
                                if (destino)
                                {
                                    bank_bin[selBank, Endereço] = temp;
                                    //Sinaliza necessidade de atualização de registrador
                                    refreshReg = true;
                                }
                                else
                                {
                                    Work_bin = temp;
                                    //Sinaliza necessidade de atualização de registrador
                                    refreshWork = true;
                                }
                                break;
                            #endregion
                            #region INCF
                            case 0x0A00:
                                //Realiza a operação
                                temp = bank_bin[selBank, Endereço] + 1;
                                //Ajusta o resultado da operação
                                temp &= 0xFF;
                                //Se deu zero
                                if (temp == 0) bank_bin[selBank, 3] |= 0x04;    //Seta o bit Z do registrador Status
                                else bank_bin[selBank, 3] &= 0xFB;              //Reseta o bit Z do registrador Status
                                //Atualiza o registrador destino da operação
                                if (destino)
                                {
                                    bank_bin[selBank, Endereço] = temp;
                                    //Sinaliza necessidade de atualização de registrador
                                    refreshReg = true;
                                }
                                else
                                {
                                    Work_bin = temp;
                                    //Sinaliza necessidade de atualização de registrador
                                    refreshWork = true;
                                }
                                //Sinaliza necessidade de atualização de registrador
                                Status_bin = bank_bin[selBank, 3];
                                refreshStatus = true;
                                break;
                            #endregion
                            #region INFFSZ
                            case 0x0F00:
                                //Realiza a operação
                                temp = bank_bin[selBank, Endereço] + 1;
                                //Ajusta o resultado da operação
                                temp &= 0xFF;
                                //Se deu zero
                                if (temp == 0) PC++;    //Salta uma instrução
                                //Atualiza o registrador destino da operação
                                if (destino)
                                {
                                    bank_bin[selBank, Endereço] = temp;
                                    //Sinaliza necessidade de atualização de registrador
                                    refreshReg = true;
                                }
                                else
                                {
                                    Work_bin = temp;
                                    //Sinaliza necessidade de atualização de registrador
                                    refreshWork = true;
                                }
                                break;
                            #endregion
                            #region IORWF
                            case 0x0400:
                                //Realiza a operação
                                temp = Work_bin | bank_bin[selBank, Endereço];
                                //Ajusta o resultado da operação
                                temp &= 0xFF;
                                //Se deu zero
                                if (temp == 0) bank_bin[selBank, 3] |= 0x04;    //Seta o bit Z do registrador Status
                                else bank_bin[selBank, 3] &= 0xFB;              //Reseta o bit Z do registrador Status
                                //Atualiza o registrador destino da operação
                                if (destino)
                                {
                                    bank_bin[selBank, Endereço] = temp;
                                    //Sinaliza necessidade de atualização de registrador
                                    refreshReg = true;
                                }
                                else
                                {
                                    Work_bin = temp;
                                    //Sinaliza necessidade de atualização de registrador
                                    refreshWork = true;
                                }
                                //Sinaliza necessidade de atualização de registrador
                                Status_bin = bank_bin[selBank, 3];
                                refreshStatus = true;
                                break;
                            #endregion
                            #region MOVF
                            case 0x0800:
                                //Realiza a operação
                                temp = bank_bin[selBank, Endereço];
                                //Ajusta o resultado da operação
                                temp &= 0xFF;
                                //Se deu zero
                                if (temp == 0) bank_bin[selBank, 3] |= 0x04;    //Seta o bit Z do registrador Status
                                else bank_bin[selBank, 3] &= 0xFB;              //Reseta o bit Z do registrador Status
                                //Atualiza o registrador destino da operação
                                if (destino)
                                {
                                    bank_bin[selBank, Endereço] = temp;
                                    //Sinaliza necessidade de atualização de registrador
                                    refreshReg = true;
                                }
                                else
                                {
                                    Work_bin = temp;
                                    //Sinaliza necessidade de atualização de registrador
                                    refreshWork = true;
                                }
                                //Sinaliza necessidade de atualização de registrador
                                Status_bin = bank_bin[selBank, 3];
                                refreshStatus = true;
                                break;
                            #endregion
                            #region MOVWF ou NOP
                            case 0x0000:
                                //Realiza a operação
                                temp = Work_bin;
                                //Ajusta o resultado da operação
                                temp &= 0xFF;
                                //Atualiza o registrador destino da operação
                                if (destino)
                                {
                                    //MOVF
                                    bank_bin[selBank, Endereço] = temp;
                                    //Sinaliza necessidade de atualização de registrador
                                    refreshReg = true;
                                }
                                else
                                {
                                    //NOP
                                }
                                //Sinaliza necessidade de atualização de registrador
                                Status_bin = bank_bin[selBank, 3];
                                refreshStatus = true;
                                break;
                            #endregion
                            #region RLF
                            case 0x0D00:
                                //Preve se vai dar carry (o bit que sair é movido para o bit Carry)
                                if ((bank_bin[selBank, Endereço] & 0x80) != 0) preCarry = true;   //Seta o preCarry
                                else preCarry = false;                                            //Reseta o preCarry
                                //Realiza a operação
                                temp = (bank_bin[selBank, Endereço] << 1) | (bank_bin[selBank, 3] & 0x01);
                                //Se deu carry
                                if (preCarry) bank_bin[selBank, 3] |= 0x01; //Seta o bit C do registrador Status
                                else bank_bin[selBank, 3] &= 0xFE;          //Reseta o bit C do registrador Status
                                //Ajusta o resultado da operação
                                temp &= 0xFF;
                                //Atualiza o registrador destino da operação
                                if (destino)
                                {
                                    bank_bin[selBank, Endereço] = temp;
                                    //Sinaliza necessidade de atualização de registrador
                                    refreshReg = true;
                                }
                                else
                                {
                                    Work_bin = temp;
                                    //Sinaliza necessidade de atualização de registrador
                                    refreshWork = true;
                                }
                                //Sinaliza necessidade de atualização de registrador
                                Status_bin = bank_bin[selBank, 3];
                                refreshStatus = true;
                                break;
                            #endregion
                            #region RRF
                            case 0x0C00:
                                //Preve se vai dar carry (o bit que sair é movido para o bit Carry)
                                if ((bank_bin[selBank, Endereço] & 0x01) != 0) preCarry = true;   //Seta o preCarry
                                else preCarry = false;                                            //Reseta o preCarry
                                //Realiza a operação
                                temp = (bank_bin[selBank, Endereço] >> 1) | ((bank_bin[selBank, 3] & 0x01) << 7);
                                //Se deu carry
                                if (preCarry) bank_bin[selBank, 3] |= 0x01; //Seta o bit C do registrador Status
                                else bank_bin[selBank, 3] &= 0xFE;          //Reseta o bit C do registrador Status
                                //Ajusta o resultado da operação
                                temp &= 0xFF;
                                //Atualiza o registrador destino da operação
                                if (destino)
                                {
                                    bank_bin[selBank, Endereço] = temp;
                                    //Sinaliza necessidade de atualização de registrador
                                    refreshReg = true;
                                }
                                else
                                {
                                    Work_bin = temp;
                                    //Sinaliza necessidade de atualização de registrador
                                    refreshWork = true;
                                }
                                //Sinaliza necessidade de atualização de registrador
                                Status_bin = bank_bin[selBank, 3];
                                refreshStatus = true;
                                break;
                            #endregion
                            #region SUBWF
                            case 0x0200:
                                //Verifica o estado do 4o bit (segundo operador sempre)
                                if ((bank_bin[selBank, Endereço] & 0x10) > 0) DC = true;
                                else DC = false;
                                //Realiza a operação
                                temp = bank_bin[selBank, Endereço] - Work_bin;
                                //Se deu borrow carry 
                                if (temp > 0) bank_bin[selBank, 3] |= 0x01; //Seta o bit C do registrador Status
                                else bank_bin[selBank, 3] &= 0xFE;          //Reseta o bit C do registrador Status
                                //Se deu digit carry
                                if (((temp & 0x10) > 0) && (DC == false)) bank_bin[selBank, 3] |= 0x02; //Seta o bit DC do registrador Status
                                else bank_bin[selBank, 3] &= 0xFD;                                      //Reseta o bit DC do registrador Status
                                //Ajusta o resultado da operação
                                temp &= 0xFF;
                                //Se deu zero
                                if (temp == 0) bank_bin[selBank, 3] |= 0x04;    //Seta o bit Z do registrador Status
                                else bank_bin[selBank, 3] &= 0xFB;                  //Reseta o bit Z do registrador Status
                                //Atualiza o registrador destino da operação
                                if (destino)
                                {
                                    bank_bin[selBank, Endereço] = temp;
                                    //Sinaliza necessidade de atualização de registrador
                                    refreshReg = true;
                                }
                                else
                                {
                                    Work_bin = temp;
                                    //Sinaliza necessidade de atualização de registrador
                                    refreshWork = true;
                                }
                                //Sinaliza necessidade de atualização de registrador
                                Status_bin = bank_bin[selBank, 3];
                                refreshStatus = true;
                                break;
                            #endregion
                            #region SWAPF
                            case 0x0E00:
                                //Realiza a operação
                                temp = (bank_bin[selBank, Endereço] << 4) | (bank_bin[selBank, Endereço] >> 4);
                                //Ajusta o resultado da operação
                                temp &= 0xFF;
                                //Atualiza o registrador destino da operação
                                if (destino)
                                {
                                    bank_bin[selBank, Endereço] = temp;
                                    //Sinaliza necessidade de atualização de registrador
                                    refreshReg = true;
                                }
                                else
                                {
                                    Work_bin = temp;
                                    //Sinaliza necessidade de atualização de registrador
                                    refreshWork = true;
                                }
                                break;
                            #endregion
                            #region XORWF
                            case 0x0600:
                                //Realiza a operação
                                temp = Work_bin ^ bank_bin[selBank, Endereço];
                                //Ajusta o resultado da operação
                                temp &= 0xFF;
                                //Se deu zero
                                if (temp == 0) bank_bin[selBank, 3] |= 0x04;    //Seta o bit Z do registrador Status
                                else bank_bin[selBank, 3] &= 0xFB;              //Reseta o bit Z do registrador Status
                                //Atualiza o registrador destino da operação
                                if (destino)
                                {
                                    bank_bin[selBank, Endereço] = temp;
                                    //Sinaliza necessidade de atualização de registrador
                                    refreshReg = true;
                                }
                                else
                                {
                                    Work_bin = temp;
                                    //Sinaliza necessidade de atualização de registrador
                                    refreshWork = true;
                                }
                                //Sinaliza necessidade de atualização de registrador
                                Status_bin = bank_bin[selBank, 3];
                                refreshStatus = true;
                                break;
                            #endregion
                        }
                    }
                }
                #endregion

                #region Condicionais para refresh (cbRefresh.Checked)
                if (cbRefresh.Checked)
                {
                    //Visualização da próxima instrução a ser executada
                    dgvMemoria.CurrentCell = dgvMemoria.Rows[PC].Cells[0];
                    //dgvMemoria.Refresh();

                    #region Refresh do Work
                    if (refreshWork)
                    {
                        dgvWork.Rows[0].Cells[0].Value = IntToBinString(Work_bin);
                        dgvWork.Refresh();
                        refreshWork = false;
                    }
                    #endregion
                    #region Refresh de registrador
                    if (refreshReg)
                    {
                        switch (selBank)
                        {
                            case 0:
                                dgvBank0.Rows[Endereço].Cells[2].Value = IntToBinString(bank_bin[selBank, Endereço]);    //Atualiza o valor da célula
                                dgvBank0.CurrentCell = dgvBank0.Rows[Endereço].Cells[0];    //Coloca o foco nesta célula
                                //dgvBank0.Refresh();
                                break;
                            case 1:
                                dgvBank1.Rows[Endereço].Cells[2].Value = IntToBinString(bank_bin[selBank, Endereço]);    //Atualiza o valor da célula
                                dgvBank1.CurrentCell = dgvBank1.Rows[Endereço].Cells[0];
                                //Coloca o foco nesta célula//dgvBank1.Refresh();
                                break;
                            case 2:
                                dgvBank2.Rows[Endereço].Cells[2].Value = IntToBinString(bank_bin[selBank, Endereço]);    //Atualiza o valor da célula
                                dgvBank2.CurrentCell = dgvBank2.Rows[Endereço].Cells[0];    //Coloca o foco nesta célula
                                //dgvBank2.Refresh();
                                break;
                            case 3:
                                dgvBank3.Rows[Endereço].Cells[2].Value = IntToBinString(bank_bin[selBank, Endereço]);    //Atualiza o valor da célula
                                dgvBank3.CurrentCell = dgvBank3.Rows[Endereço].Cells[0];    //Coloca o foco nesta célula
                                //dgvBank3.Refresh();
                                break;
                        }
                        refreshReg = false;
                    }
                    #endregion
                    #region Refresh do Stack em push
                    if (refreshStack_push)
                    {
                        //Se a pilha esta cheia remove o último registro
                        if (dgvStack.Rows.Count >= 8)
                            dgvStack.Rows.RemoveAt(0);
                        //Insere o valor do program counter na pilha    
                        dgvStack.Rows.Insert(0, picStack.Peek().ToString());
                        dgvStack.CurrentCell = dgvStack.Rows[0].Cells[0];   //Deve vir depois do insert para evitar erros
                        //dgvStack.Refresh();

                        refreshStack_push = false;
                    }
                    #endregion
                    #region Refresh do Stack em pop
                    if (refreshStack_pop)
                    {
                        //Se a pilha não está vazia remove um registro
                        if (dgvStack.Rows.Count > 0)
                        {
                            dgvStack.CurrentCell = dgvStack.Rows[0].Cells[0];   //Deve vir antes do remove para evitar erros
                            dgvStack.Rows.RemoveAt(0);
                            //dgvStack.Refresh();
                        }

                        refreshStack_pop = false;
                    }
                    #endregion
                    #region Refresh do Intcon (retorno de interrupção GIE = 1)
                    if (refreshIntcon)
                    {
                        //Refresh de INTCON nos 4 bancos
                        bank_bin[0, 0x0B] = bank_bin[selBank, 0x0B];
                        bank_bin[1, 0x0B] = bank_bin[selBank, 0x0B];
                        bank_bin[2, 0x0B] = bank_bin[selBank, 0x0B];
                        bank_bin[3, 0x0B] = bank_bin[selBank, 0x0B];

                        //Converte para string
                        string intcon = IntToBinString(bank_bin[selBank, 0x0B]);

                        //Faz o refresh dos 4 bancos DataGridView
                        dgvBank0.Rows[11].Cells[2].Value = intcon;
                        dgvBank0.CurrentCell = dgvBank0.Rows[11].Cells[0];
                        //dgvBank0.Refresh();

                        dgvBank1.Rows[11].Cells[2].Value = intcon;
                        dgvBank1.CurrentCell = dgvBank1.Rows[11].Cells[0];
                        //dgvBank1.Refresh();

                        dgvBank2.Rows[11].Cells[2].Value = intcon;
                        dgvBank2.CurrentCell = dgvBank2.Rows[11].Cells[0];
                        //dgvBank2.Refresh();

                        dgvBank3.Rows[11].Cells[2].Value = intcon;
                        dgvBank3.CurrentCell = dgvBank3.Rows[11].Cells[0];
                        //dgvBank3.Refresh();

                        refreshIntcon = false;
                    }
                    #endregion
                }
                #endregion

                #region Tratamento especial para Intcon
                //Refresh do Intcon
                if ((Endereço == 11) && (destino == true))   //Intcon (End.=11 em Bank0,1,2,3)
                {
                    //Refresh de INTCON nos 4 bancos
                    bank_bin[0, 0x0B] = bank_bin[selBank, 0x0B];
                    bank_bin[1, 0x0B] = bank_bin[selBank, 0x0B];
                    bank_bin[2, 0x0B] = bank_bin[selBank, 0x0B];
                    bank_bin[3, 0x0B] = bank_bin[selBank, 0x0B];

                    if (cbRefresh.Checked)
                    {
                        //Converte para string
                        string intcon = IntToBinString(bank_bin[selBank, 0x0B]);

                        //Faz o refresh dos 4 bancos DataGridView
                        dgvBank0.Rows[11].Cells[2].Value = intcon;
                        dgvBank0.CurrentCell = dgvBank0.Rows[11].Cells[0];
                        //dgvBank0.Refresh();

                        dgvBank1.Rows[11].Cells[2].Value = intcon;
                        dgvBank1.CurrentCell = dgvBank1.Rows[11].Cells[0];
                        //dgvBank1.Refresh();

                        dgvBank2.Rows[11].Cells[2].Value = intcon;
                        dgvBank2.CurrentCell = dgvBank2.Rows[11].Cells[0];
                        //dgvBank2.Refresh();

                        dgvBank3.Rows[11].Cells[2].Value = intcon;
                        dgvBank3.CurrentCell = dgvBank3.Rows[11].Cells[0];
                        //dgvBank3.Refresh();
                    }
                }
                #endregion

                #region Tratamento especial para Option_Reg
                //Refresh do Option_Reg
                if (Endereço == 1 && ((selBank == 1) || (selBank == 3)) && (destino == true))   //Option_Reg (End.=1 em Bank1 e Bank3)
                {
                    //Refresh de OPTION_REG nos 2 bancos
                    bank_bin[1, 0x01] = bank_bin[selBank, 0x01];
                    bank_bin[3, 0x01] = bank_bin[selBank, 0x01];

                    if (cbRefresh.Checked)
                    {
                        //Converte para string
                        string opreg = IntToBinString(bank_bin[selBank, Endereço]);

                        //Faz o refresh dos 2 bancos DataGridView
                        dgvBank1.Rows[1].Cells[2].Value = opreg;            //Atualiza o valor da célula
                        dgvBank1.CurrentCell = dgvBank0.Rows[1].Cells[0];   //Coloca o foco nesta célula e já faz o refresh dela
                        //dgvBank1.Refresh();

                        dgvBank3.Rows[1].Cells[2].Value = opreg;            //Atualiza o valor da célula
                        dgvBank3.CurrentCell = dgvBank3.Rows[1].Cells[0];   //Coloca o foco nesta célula e já faz o refresh dela
                        //dgvBank3.Refresh();
                    }

                    //Teste o bit 5 (T0CS: TMR0 Clock Source Select bit) (0=Clock Interno, 1=Clock RA4 (desativado))
                    if ((bank_bin[selBank, 0x01] & 0x20) == 0) TMR0 = true; //Temporizador
                    else TMR0 = false;                                      //Contador (dasativado)

                    //Teste o bit 3 (PSA: Prescaler Assignment bit)
                    if ((bank_bin[selBank, 0x01] & 0x08) == 0)
                    {
                        //Presacler no Timer0 (PS2:PS0: Prescaler Rate Select bits)
                        PS = bank_bin[selBank, 0x01] & 0x07;
                        PS = (int)Math.Pow(2, PS) * 2;  //2^PS * 2
                    }
                    else
                    {
                        //Prescaler no WDT
                        PS = 1;
                    }
                }
                #endregion

                #region Tratamento especial para Timer0
                //Refresh do TMR0 e timer0 (variável de controle)
                if (Endereço == 1 && ((selBank == 0) || (selBank == 2)) && (destino == true))   //TMR0 (End.=1 em Bank0 e Bank2)
                {
                    //Refresh de TMR0 nos 2 bancos
                    bank_bin[0, 0x01] = bank_bin[selBank, 0x01];
                    bank_bin[2, 0x01] = bank_bin[selBank, 0x01];

                    //Atualiza o TIMER0 com o valor atribuido
                    timer0 = bank_bin[selBank, 0x01];

                    if (cbRefresh.Checked)
                    {
                        //Converte para string
                        string tmr0 = IntToBinString(timer0);

                        //Faz o refresh dos 2 bancos DataGridView
                        dgvBank0.Rows[1].Cells[2].Value = tmr0;             //Atualiza o valor da célula
                        dgvBank0.CurrentCell = dgvBank0.Rows[1].Cells[0];   //Coloca o foco nesta célula e já faz o refresh dela
                        //dgvBank0.Refresh();

                        dgvBank2.Rows[1].Cells[2].Value = tmr0;             //Atualiza o valor da célula
                        dgvBank2.CurrentCell = dgvBank2.Rows[1].Cells[0];   //Coloca o foco nesta célula e já faz o refresh dela
                        //dgvBank2.Refresh();
                    }
                }

                if (TMR0 == true)
                {
                    if ((ticks % PS == 0) || (cbPrescaler.Checked))
                    {

                        if (timer0 == 255)
                        {
                            //Zera o timer (overflow)
                            timer0 = 0;

                            //Seta a flag TMR0IF em INTCON (bit2) nos 4 bancos
                            bank_bin[0, 0x0B] |= 0x04;
                            bank_bin[1, 0x0B] |= 0x04;
                            bank_bin[2, 0x0B] |= 0x04;
                            bank_bin[3, 0x0B] |= 0x04;

                            if (cbRefresh.Checked)
                            {
                                //Converte para string
                                string intcon = IntToBinString(bank_bin[selBank, 0x0B]);

                                //Faz o refresh dos 4 bancos DataGridView
                                dgvBank0.Rows[11].Cells[2].Value = intcon;
                                dgvBank0.CurrentCell = dgvBank0.Rows[11].Cells[0];
                                //dgvBank0.Refresh();

                                dgvBank1.Rows[11].Cells[2].Value = intcon;
                                dgvBank1.CurrentCell = dgvBank1.Rows[11].Cells[0];
                                //dgvBank1.Refresh();

                                dgvBank2.Rows[11].Cells[2].Value = intcon;
                                dgvBank2.CurrentCell = dgvBank2.Rows[11].Cells[0];
                                //dgvBank2.Refresh();

                                dgvBank3.Rows[11].Cells[2].Value = intcon;
                                dgvBank3.CurrentCell = dgvBank3.Rows[11].Cells[0];
                                //dgvBank3.Refresh();
                            }
                        }
                        else
                        {
                            timer0++;
                        }

                        //Refresh de TMR0 nos 2 bancos após atualização
                        bank_bin[0, 0x01] = timer0;
                        bank_bin[2, 0x01] = timer0;

                        if (cbRefresh.Checked)
                        {
                            //Converte para string
                            string tmr0 = IntToBinString(timer0);

                            //Faz o refresh dos 2 bancos DataGridView
                            dgvBank0.Rows[1].Cells[2].Value = tmr0;             //Atualiza o valor da célula
                            dgvBank0.CurrentCell = dgvBank0.Rows[1].Cells[0];   //Coloca o foco nesta célula e já faz o refresh dela
                            //dgvBank0.Refresh();

                            dgvBank2.Rows[1].Cells[2].Value = tmr0;             //Atualiza o valor da célula
                            dgvBank2.CurrentCell = dgvBank2.Rows[1].Cells[0];   //Coloca o foco nesta célula e já faz o refresh dela
                            //dgvBank2.Refresh();
                        }
                    }
                }
                #endregion

                #region Tratamento especial endereços 0x70~0x7F espelhados entre os bancos
                //Refresh dos endereços 0x70~0x7F
                if ((Endereço >= 0x70) && (destino == true))   //Endereços espelhados entre bancos 0, 1, 2 e 3)
                {
                    //Refresh do registrador nos 4 bancos
                    bank_bin[0, Endereço] = bank_bin[selBank, Endereço];
                    bank_bin[1, Endereço] = bank_bin[selBank, Endereço];
                    bank_bin[2, Endereço] = bank_bin[selBank, Endereço];
                    bank_bin[3, Endereço] = bank_bin[selBank, Endereço];

                    if (cbRefresh.Checked)
                    {
                        //Converte para string
                        string reg = IntToBinString(bank_bin[selBank, Endereço]);

                        //Não vou forçar o refresh por se tratarem de 4 registradores, apenas alterar o valor
                        dgvBank0.Rows[Endereço].Cells[2].Value = reg;               //Atualiza o valor da célula
                        dgvBank0.CurrentCell = dgvBank0.Rows[Endereço].Cells[0];    //Coloca o foco nesta célula e já faz o refresh dela

                        dgvBank1.Rows[Endereço].Cells[2].Value = reg;               //Atualiza o valor da célula
                        dgvBank1.CurrentCell = dgvBank1.Rows[Endereço].Cells[0];    //Coloca o foco nesta célula e já faz o refresh dela

                        dgvBank2.Rows[Endereço].Cells[2].Value = reg;               //Atualiza o valor da célula
                        dgvBank2.CurrentCell = dgvBank2.Rows[Endereço].Cells[0];    //Coloca o foco nesta célula e já faz o refresh dela

                        dgvBank3.Rows[Endereço].Cells[2].Value = reg;               //Atualiza o valor da célula
                        dgvBank3.CurrentCell = dgvBank3.Rows[Endereço].Cells[0];    //Coloca o foco nesta célula e já faz o refresh dela                        
                    }
                }
                #endregion

                #region Tratamento especial para PortB
                //Refresh do PortB
                if (Endereço == 6 && ((selBank == 0) || (selBank == 2)) && (destino == true))   //PORTB (End.=6 em Bank0 e Bank2)
                {
                    //Refresh do registrador nos 4 bancos
                    bank_bin[0, Endereço] = bank_bin[selBank, Endereço];
                    bank_bin[2, Endereço] = bank_bin[selBank, Endereço];

                    if (cbRefresh.Checked)
                    {
                        //Converte para string
                        string reg = IntToBinString(bank_bin[selBank, Endereço]);

                        //Não vou forçar o refresh por se tratarem de 4 registradores, apenas alterar o valor
                        dgvBank0.Rows[Endereço].Cells[2].Value = reg;               //Atualiza o valor da célula
                        dgvBank0.CurrentCell = dgvBank0.Rows[Endereço].Cells[0];    //Coloca o foco nesta célula e já faz o refresh dela

                        dgvBank2.Rows[Endereço].Cells[2].Value = reg;               //Atualiza o valor da célula
                        dgvBank2.CurrentCell = dgvBank2.Rows[Endereço].Cells[0];    //Coloca o foco nesta célula e já faz o refresh dela
                    }
                }
                #endregion

                #region Tratamento especial para TrisB
                //Refresh do TrisB
                if (Endereço == 6 && ((selBank == 1) || (selBank == 3)) && (destino == true))   //TRISB (End.=6 em Bank1 e Bank3)
                {
                    //Refresh do registrador nos 4 bancos
                    bank_bin[1, Endereço] = bank_bin[selBank, Endereço];
                    bank_bin[3, Endereço] = bank_bin[selBank, Endereço];

                    if (cbRefresh.Checked)
                    {
                        //Converte para string
                        string reg = IntToBinString(bank_bin[selBank, Endereço]);

                        //Não vou forçar o refresh por se tratarem de 4 registradores, apenas alterar o valor
                        dgvBank1.Rows[Endereço].Cells[2].Value = reg;               //Atualiza o valor da célula
                        dgvBank1.CurrentCell = dgvBank0.Rows[Endereço].Cells[0];    //Coloca o foco nesta célula e já faz o refresh dela

                        dgvBank3.Rows[Endereço].Cells[2].Value = reg;               //Atualiza o valor da célula
                        dgvBank3.CurrentCell = dgvBank3.Rows[Endereço].Cells[0];    //Coloca o foco nesta célula e já faz o refresh dela
                    }
                }
                #endregion

                #region Tratamento especial para PCL (06/09/16)
                //Refresh do PLC
                if ((Endereço == 0x02) && (destino == true))   //Intcon (End.=02 em Bank0,1,2,3)
                {
                    //Refresh de PLC nos 4 bancos
                    bank_bin[0, 0x02] = bank_bin[selBank, 0x02];
                    bank_bin[1, 0x02] = bank_bin[selBank, 0x02];
                    bank_bin[2, 0x02] = bank_bin[selBank, 0x02];
                    bank_bin[3, 0x02] = bank_bin[selBank, 0x02];

                    //Atualização do PC
                    //PCLATH(4..0) + PCL
                    PC = ((bank_bin[0, 0x0A] & 0x1F) << 8) + bank_bin[0, 0x02];

                    if (cbRefresh.Checked)
                    {
                        //Visualização da próxima instrução a ser executada
                        dgvMemoria.CurrentCell = dgvMemoria.Rows[PC].Cells[0];

                        //Converte para string
                        string PLC = IntToBinString(bank_bin[selBank, 0x02]);

                        //Faz o refresh dos 4 bancos DataGridView
                        dgvBank0.Rows[2].Cells[2].Value = PLC;
                        dgvBank0.CurrentCell = dgvBank0.Rows[2].Cells[0];
                        //dgvBank0.Refresh();

                        dgvBank1.Rows[2].Cells[2].Value = PLC;
                        dgvBank1.CurrentCell = dgvBank1.Rows[2].Cells[0];
                        //dgvBank1.Refresh();

                        dgvBank2.Rows[2].Cells[2].Value = PLC;
                        dgvBank2.CurrentCell = dgvBank2.Rows[2].Cells[0];
                        //dgvBank2.Refresh();

                        dgvBank3.Rows[2].Cells[2].Value = PLC;
                        dgvBank3.CurrentCell = dgvBank3.Rows[2].Cells[0];
                        //dgvBank3.Refresh();
                    }
                }
                else
                {
                    ////PCL sempre tem o valor de PC (está no começo da função para atualizar PLC antes de ser usado)
                    ////Refresh de PLC nos 4 bancos
                    //bank_bin[0, 0x02] = PC & 0xFF;
                    //bank_bin[1, 0x02] = PC & 0xFF;
                    //bank_bin[2, 0x02] = PC & 0xFF;
                    //bank_bin[3, 0x02] = PC & 0xFF;
                    if (cbRefresh.Checked)
                    {
                        //Converte para string
                        string PLC = IntToBinString(bank_bin[0, 0x02]);

                        //Faz o refresh dos 4 bancos DataGridView
                        dgvBank0.Rows[2].Cells[2].Value = PLC;
                        //dgvBank0.CurrentCell = dgvBank0.Rows[2].Cells[0];
                        //dgvBank0.Refresh();

                        dgvBank1.Rows[2].Cells[2].Value = PLC;
                        //dgvBank1.CurrentCell = dgvBank1.Rows[2].Cells[0];
                        //dgvBank1.Refresh();

                        dgvBank2.Rows[2].Cells[2].Value = PLC;
                        //dgvBank2.CurrentCell = dgvBank2.Rows[2].Cells[0];
                        //dgvBank2.Refresh();

                        dgvBank3.Rows[2].Cells[2].Value = PLC;
                        //dgvBank3.CurrentCell = dgvBank3.Rows[2].Cells[0];
                        //dgvBank3.Refresh();
                    }
                }
                #endregion

                #region Tratamento especial para PCLATH (06/09/16)
                //Refresh do PCLATH
                if ((Endereço == 0x0A) && (destino == true))   //PCLATH (End.=10 em Bank0,1,2,3)
                {
                    //Refresh de PCLATH nos 4 bancos
                    bank_bin[0, 0x0A] = bank_bin[selBank, 0x0A];
                    bank_bin[1, 0x0A] = bank_bin[selBank, 0x0A];
                    bank_bin[2, 0x0A] = bank_bin[selBank, 0x0A];
                    bank_bin[3, 0x0A] = bank_bin[selBank, 0x0A];

                    if (cbRefresh.Checked)
                    {
                        //Converte para string
                        string PCLATH = IntToBinString(bank_bin[selBank, 0x0A]);

                        //Faz o refresh dos 4 bancos DataGridView
                        dgvBank0.Rows[10].Cells[2].Value = PCLATH;
                        dgvBank0.CurrentCell = dgvBank0.Rows[10].Cells[0];
                        //dgvBank0.Refresh();

                        dgvBank1.Rows[10].Cells[2].Value = PCLATH;
                        dgvBank1.CurrentCell = dgvBank1.Rows[10].Cells[0];
                        //dgvBank1.Refresh();

                        dgvBank2.Rows[10].Cells[2].Value = PCLATH;
                        dgvBank2.CurrentCell = dgvBank2.Rows[10].Cells[0];
                        //dgvBank2.Refresh();

                        dgvBank3.Rows[10].Cells[2].Value = PCLATH;
                        dgvBank3.CurrentCell = dgvBank3.Rows[10].Cells[0];
                        //dgvBank3.Refresh();
                    }
                }
                #endregion

                #region Tratamento especial para FSR (06/09/16)
                //Refresh do FSR
                if ((Endereço == 0x04) && (destino == true))   //FSR (End.=4 em Bank0,1,2,3)
                {
                    //Refresh de PCLATH nos 4 bancos
                    bank_bin[0, 0x04] = bank_bin[selBank, 0x04];
                    bank_bin[1, 0x04] = bank_bin[selBank, 0x04];
                    bank_bin[2, 0x04] = bank_bin[selBank, 0x04];
                    bank_bin[3, 0x04] = bank_bin[selBank, 0x04];

                    if (cbRefresh.Checked)
                    {
                        //Converte para string
                        string FSR = IntToBinString(bank_bin[selBank, 0x04]);

                        //Faz o refresh dos 4 bancos DataGridView
                        dgvBank0.Rows[4].Cells[2].Value = FSR;
                        dgvBank0.CurrentCell = dgvBank0.Rows[4].Cells[0];
                        //dgvBank0.Refresh();

                        dgvBank1.Rows[4].Cells[2].Value = FSR;
                        dgvBank1.CurrentCell = dgvBank1.Rows[4].Cells[0];
                        //dgvBank1.Refresh();

                        dgvBank2.Rows[4].Cells[2].Value = FSR;
                        dgvBank2.CurrentCell = dgvBank2.Rows[4].Cells[0];
                        //dgvBank2.Refresh();

                        dgvBank3.Rows[4].Cells[2].Value = FSR;
                        dgvBank3.CurrentCell = dgvBank3.Rows[4].Cells[0];
                        //dgvBank3.Refresh();
                    }
                }
                #endregion

                //DONE: Tratamento de condicionais TXREG criar função para receber valor inteiro direto 
                //Corrigir métodos da serial virtual eles trabalham apenas com strings
                /*
                 * OK - bEnviar_Click(...) -> bEnviar_Binario_Click(...) [mudar o método de click do botão Enviar da Serial Virtual]
                 * OK - RodaThreadTx() -> RodaThreadTx_Binario()
                 * OK - RodaThreadRx() //não utilizado no programa -> RodaThreadRx_Binario()
                 * OK - ReferenciaRAM(string[, ,] bancos) -> ReferenciaRAM_Binario(string[,] bancos_bin)
                 * OK - Receber(string RX_bin) -> Receber_Binario(int RX_bin)
                 */
                #region Tratamento especial para TXREG (avisa a Serial Virtual que tem byte para ser lido)
                if ((selBank == 0) && (Endereço == 0x19))
                    serialVirtual.Receber_Binario(Work_bin);
                #endregion

                #region Tratamento especial para ADCON0 (início de conversão do CAD) --> Versão 3.1
                if ((selBank == 0) && (Endereço == 0x1F))   //ADCON0 (End.=1Fh em Bank0)
                {   /*       Bits 7     6     5    4    3    2       1 0  
                     * 1Fh ADCON0 ADCS1 ADCS0 CHS2 CHS1 CHS0 GO/DONE — ADON
                     * 
                     * CHS2 CHS1 CHS0 = 000 (AN0) ou 001 (AN1)
                     * GO/DONE = 1 (iniciar conversão) ou 0 (quando finalizar a conversão)
                     * ADON = 1 (liga periférico) ou 0 (desliga periférico)
                     * 
                     * ADCON0 =             xx00x1x1
                     * Máscara de AND =     00110101
                     * Resultado esperado = 00000101
                     */
                    if ((bank_bin[0, 0x1F] & 0x35) == 0x05) //Se o modulo está ligado e uma conversão foi iniciada nos canais AN0 ou AN1
                    {
                        /*       Bits 7    6     5 4 3     2     1     0
                         * 9Fh ADCON1 ADFM ADCS2 — — PCFG3 PCFG2 PCFG1 PCFG0
                         * 
                         * ADFM = 1 (justificado a direita) ou 0 (justificado a esquerda)
                         * PCFG3 PCFG2 PCFG1 PCFG0 = 011x (AN7-0 D), 1110 ou 1111 (AN1 D, AN0 A) e demais (AN0 e AN1 A)
                         * 
                         * ADCON1 =             xxxx011x
                         * Máscara de AND =     00001110
                         * Resultado esperado = 00000110
                         */
                        if ((bank_bin[1, 0x1F] & 0x0E) != 0x06)  //Se AN0 e/ou AN1 são analógicos
                        {
                            /* ADCON1 =             xx000xxx
                             * Máscara de AND =     00111000
                             * Resultado esperado = 00000000
                             */
                            if ((bank_bin[0, 0x1F] & 0x38) == 0x00)  //Se é o canal AN0
                            {
                                if ((bank_bin[1, 0x05] & 0x01) == 0x01)  //Se TRISA.0 é entrada
                                {
                                    try
                                    {
                                        int step = int.Parse(tbAN0Step.Text);   //Lê a entrada analógica AN0
                                        if ((bank_bin[1, 0x1F] & 0x80) == 0x80)  //Se é justificado a direita
                                        {
                                            bank_bin[0, 0x1E] = step >> 8;      //ADRESH = 2 msb
                                            bank_bin[1, 0x1E] = step & 0x0FF;   //ADRESL = 8 lsb
                                        }
                                        else //Senão, é justificado a esquerda
                                        {
                                            bank_bin[0, 0x1E] = step >> 2;              //ADRESH = 8 msb
                                            bank_bin[1, 0x1E] = (step & 0x003) << 6;    //ADRESL = 2 lsb
                                        }

                                        bank_bin[0, 0x1F] &= 0xFB;  //GO/DONE = 0
                                        bank_bin[0, 0x0C] |= 0x20;  //PIR1.6 = ADIF = 1

                                        if (cbRefresh.Checked)
                                        {
                                            dgvBank0.Rows[0x0C].Cells[2].Value = IntToBinString(bank_bin[0, 0x0C]); //Atualiza o valor da célula PIR1
                                            dgvBank0.Rows[0x1F].Cells[2].Value = IntToBinString(bank_bin[0, 0x1F]); //Atualiza o valor da célula ADCON0
                                            dgvBank0.Rows[0x1E].Cells[2].Value = IntToBinString(bank_bin[0, 0x1E]); //Atualiza o valor da célula ADRESH
                                            dgvBank1.Rows[0x1E].Cells[2].Value = IntToBinString(bank_bin[1, 0x1E]); //Atualiza o valor da célula ADRESL
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        MessageBox.Show("ERRO CAD");
                                    }
                                }
                            }
                            /* ADCON0 =             xx001xxx
                             * Máscara de AND =     00111000
                             * Resultado esperado = 00001000
                             */
                            else if ((bank_bin[0, 0x1F] & 0x38) == 0x08)  //Se é o canal AN1
                            {
                                /* ADCON1 =             xxxx111x
                                 * Máscara de AND =     00001110
                                 * Resultado esperado = 00001110
                                 */
                                if ((bank_bin[1, 0x1F] & 0x0E) != 0x0E)  //Se AN1 é analógico
                                {
                                    if ((bank_bin[1, 0x05] & 0x02) == 0x02)  //Se TRISA.1 é entrada
                                    {
                                        try
                                        {
                                            int step = int.Parse(tbAN1Step.Text);   //Lê a entrada analógica AN1
                                            if ((bank_bin[1, 0x1F] & 0x80) == 0x80)  //Se é justificado a direita
                                            {
                                                bank_bin[0, 0x1E] = step >> 8;      //ADRESH = 2 msb
                                                bank_bin[1, 0x1E] = step & 0x0FF;   //ADRESL = 8 lsb
                                            }
                                            else //Senão, é justificado a esquerda
                                            {
                                                bank_bin[0, 0x1E] = step >> 2;              //ADRESH = 8 msb
                                                bank_bin[1, 0x1E] = (step & 0x003) << 6;    //ADRESL = 2 lsb
                                            }

                                            bank_bin[0, 0x1F] &= 0xFB;  //GO/DONE = 0
                                            bank_bin[0, 0x0C] |= 0x20;  //PIR1.6 = ADIF = 1

                                            if (cbRefresh.Checked)
                                            {
                                                dgvBank0.Rows[0x0C].Cells[2].Value = IntToBinString(bank_bin[0, 0x0C]); //Atualiza o valor da célula PIR1
                                                dgvBank0.Rows[0x1F].Cells[2].Value = IntToBinString(bank_bin[0, 0x1F]); //Atualiza o valor da célula ADCON0
                                                dgvBank0.Rows[0x1E].Cells[2].Value = IntToBinString(bank_bin[0, 0x1E]); //Atualiza o valor da célula ADRESH
                                                dgvBank1.Rows[0x1E].Cells[2].Value = IntToBinString(bank_bin[1, 0x1E]); //Atualiza o valor da célula ADRESL
                                            }
                                        }
                                        catch (Exception)
                                        {
                                            MessageBox.Show("ERRO CAD");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion

                #region Tratamento especial para Status e Refresh do Status (deve ser o último a ser atualizado)
                if ((Endereço == 0x03) && (destino == true))
                {
                    //Sinaliza necessidade de atualização de registrador
                    Status_bin = bank_bin[selBank, 3];
                    refreshStatus = true;
                }
                if (refreshStatus)  //Deve ser o último a ser atualizado
                {
                    //Refresh de STATUS nos 4 bancos
                    bank_bin[0, 0x03] = Status_bin;
                    bank_bin[1, 0x03] = Status_bin;
                    bank_bin[2, 0x03] = Status_bin;
                    bank_bin[3, 0x03] = Status_bin;

                    //Atualiza banco selecionado
                    selBank = (bank_bin[selBank, 0x03] & 0x60) >> 5;

                    if (cbRefresh.Checked)
                    {
                        //Converte para string
                        sStatus = IntToBinString(Status_bin);

                        //Faz o refresh dos 4 bancos DataGridView
                        dgvBank0.Rows[3].Cells[2].Value = sStatus;
                        //dgvBank0.Refresh();

                        dgvBank1.Rows[3].Cells[2].Value = sStatus;
                        //dgvBank1.Refresh();

                        dgvBank2.Rows[3].Cells[2].Value = sStatus;
                        //dgvBank2.Refresh();

                        dgvBank3.Rows[3].Cells[2].Value = sStatus;
                        //dgvBank3.Refresh();

                        dgvStatusReg.CurrentRow.Cells[0].Value = sStatus;
                        dgvStatusReg.Refresh();
                    }
                    refreshStatus = false;
                }
                #endregion

                #region Tratamento especial para INDF (06/09/16)
                //Refresh do banco selecionado
                if (indf)   //INDF (End.=0 em Bank0,1,2,3)
                {
                    //Atualiza banco selecionado
                    selBank = (bank_bin[0, 0x03] & 0x60) >> 5;
                    //Isso foi feito supondo que as instruções indiretas são minoria,
                    //nestes casos o banco selecionado é redefinido antes das instruções
                    //com base nos valores de Status,IRP + FSR,7 e após todo tratamento
                    //é defefinido como Status,RP1 + Status,RP0 
                }
                #endregion

                #region Teste de Interrupções (08/09/16)
                //Se está em condição de interrupção de acordo com o registrador INTCON (0Bh)
                //Timer0    --> GIE(7)=TMR0IE(5)=TMR0IF(2)=1 --> Máscara A4h
                //ou
                //INT (RB0) --> GIE(7)=INTE(4)=INTF(1)=1     --> Máscara 92h

                if (((bank_bin[0, 0x0B] & 0xA4) == 0xA4) || ((bank_bin[0, 0x0B] & 0x92) == 0x92))
                {
                    //Se a pilha esta cheia remove o último registro
                    if (picStack.Count >= 8) picStack.Pop();
                    //Insere o valor do program counter na pilha
                    picStack.Push(PC);

                    if (cbRefresh.Checked)
                    {
                        //Se a pilha esta cheia remove o último registro
                        if (dgvStack.Rows.Count >= 8)
                            dgvStack.Rows.RemoveAt(0);
                        //Insere o valor do program counter na pilha    
                        dgvStack.Rows.Insert(0, picStack.Peek().ToString());
                        dgvStack.CurrentCell = dgvStack.Rows[0].Cells[0];   //Deve vir depois do insert para evitar erros
                        //dgvStack.Refresh();
                    }

                    //Reseta o bit GIE do registrador INTCON
                    bank_bin[selBank, 0x0B] &= ~0x80;
                    //Refresh de INTCON nos 4 bancos
                    bank_bin[0, 0x0B] = bank_bin[selBank, 0x0B];
                    bank_bin[1, 0x0B] = bank_bin[selBank, 0x0B];
                    bank_bin[2, 0x0B] = bank_bin[selBank, 0x0B];
                    bank_bin[3, 0x0B] = bank_bin[selBank, 0x0B];

                    if (cbRefresh.Checked)
                    {
                        //Converte para string
                        string intcon = IntToBinString(bank_bin[selBank, 0x0B]);

                        //Faz o refresh dos 4 bancos DataGridView
                        dgvBank0.Rows[11].Cells[2].Value = intcon;
                        //dgvBank0.CurrentCell = dgvBank0.Rows[11].Cells[0];
                        //dgvBank0.Refresh();

                        dgvBank1.Rows[11].Cells[2].Value = intcon;
                        //dgvBank1.CurrentCell = dgvBank1.Rows[11].Cells[0];
                        //dgvBank1.Refresh();

                        dgvBank2.Rows[11].Cells[2].Value = intcon;
                        //dgvBank2.CurrentCell = dgvBank2.Rows[11].Cells[0];
                        //dgvBank2.Refresh();

                        dgvBank3.Rows[11].Cells[2].Value = intcon;
                        //dgvBank3.CurrentCell = dgvBank3.Rows[11].Cells[0];
                        //dgvBank3.Refresh();
                    }

                    //Atualiza program counter com o valor de desvio para tratamento de interrupções (ISRs)
                    PC = 0x04;
                    if (cbRefresh.Checked)
                    {
                        //Visualização da próxima instrução a ser executada
                        dgvMemoria.CurrentCell = dgvMemoria.Rows[PC].Cells[0];
                    }
                }
                #endregion

                TestaBotoesTeclado_Binario();       //Testa o estado dos botões e o teclado matricial (PORTB)
                AtualizaLedsDisplays_Binario();     //Atualiza o estado dos leds e displays de 7 segmentos (PORTD)
            }
            catch (Exception ex)
            {
                clock.Enabled = false;
                MessageBox.Show("Erro ao executar uma instrução.\n" + ex.Message, "ERRO",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Reset();
            }
        }
        /// <summary>
        /// Testa o estado dos botões e o teclado matricial (PORTB).
        /// Atenção: Esse método não está sendo atualizado desde a versão 3.0 do programa.
        /// </summary>
        private void TestaBotoesTeclado()
        {
            //Atenção: a ordem do vetor é contrária a ordem dos pinos (Vetor 0..7 --> Pinos 7..0)
            /* Vetor    Port   Função
             * 0        7      Coluna 1
             * 1        6      Coluna 2
             * 2        5      Coluna 3
             * 3        4      Linha 1
             * 4        3      Linha 2
             * 5        2      Linha 3
             * 6        1      Botão 1
             * 7        0      Botão 0
             */

            //Teclado UserControl
            //Lê o valor do PORTB (BANK0 END6)
            char[] ValueArray = bank[0, 6, 2].ToCharArray(); //PORTB PARA VETOR DE CARACTER

            //Teste das colunas
            //Coluna 1
            if (ValueArray[0] == '0') ucTeclado.Coluna1 = true;
            else ucTeclado.Coluna1 = false;
            //Coluna 2
            if (ValueArray[1] == '0') ucTeclado.Coluna2 = true;
            else ucTeclado.Coluna2 = false;
            //Coluna 3
            if (ValueArray[2] == '0') ucTeclado.Coluna3 = true;
            else ucTeclado.Coluna3 = false;

            //Teste das linhas
            //Linha 1
            if (ucTeclado.Linha1) ValueArray[3] = '0';      //Linha 1 = 0
            else ValueArray[3] = '1';                       //Linha 1 = 1
            //Linha 2
            if (ucTeclado.Linha2) ValueArray[4] = '0';      //Linha 2 = 0
            else ValueArray[4] = '1';                       //Linha 2 = 1
            //Linha 3
            if (ucTeclado.Linha3) ValueArray[5] = '0';      //Linha 3 = 0
            else ValueArray[5] = '1';                       //Linha 3 = 1

            //Teste do botões RB0 e RB1
            //Botão 0
            if (cbRB0.Checked == true) ValueArray[7] = '0'; //PORTB.0 = 0
            else ValueArray[7] = '1';                       //PORTB.0 = 1
            //Botão 1
            if (cbRB1.Checked == true) ValueArray[6] = '0'; //PORTB.1 = 0               
            else ValueArray[6] = '1';                       //PORTB.1 = 1

            //Atualiza o estado do PORTB
            string Value = new string(ValueArray);
            if (cbRefresh.Checked)
                dgvBank0.Rows[6].Cells[2].Value = Value;

            //Atualiza matriz com bancos
            bank[0, 6, 2] = Value;
        }
        /// <summary>
        /// Testa o estado dos botões e o teclado matricial (PORTB) em binário.
        /// </summary>
        private void TestaBotoesTeclado_Binario()
        {
            //Atenção: a ordem do vetor é contrária a ordem dos pinos (Vetor 0..7 --> Pinos 7..0)
            /* Vetor    Port   Função
             * 0        7      Coluna 1
             * 1        6      Coluna 2
             * 2        5      Coluna 3
             * 3        4      Linha 1
             * 4        3      Linha 2
             * 5        2      Linha 3
             * 6        1      Botão 1
             * 7        0      Botão 0
             */

            //Teclado UserControl
            //Lê o valor do PORTB (BANK0 END6)
            int portb = bank_bin[0, 6];

            //Teste das colunas
            //Coluna 1 (RB.7)
            if ((portb & 0x80) == 0) ucTeclado.Coluna1 = true;
            else ucTeclado.Coluna1 = false;
            //Coluna 2 (RB.6)
            if ((portb & 0x40) == 0) ucTeclado.Coluna2 = true;
            else ucTeclado.Coluna2 = false;
            //Coluna 3 (RB.5)
            if ((portb & 0x20) == 0) ucTeclado.Coluna3 = true;
            else ucTeclado.Coluna3 = false;

            //Teste das linhas
            //Linha 1 (RB.4)
            if (ucTeclado.Linha1) portb &= ~0x10;   //Linha 1 = 0
            else portb |= 0x10;                     //Linha 1 = 1
            //Linha 2 (RB.3)
            if (ucTeclado.Linha2) portb &= ~0x08;   //Linha 2 = 0
            else portb |= 0x08;                     //Linha 2 = 1
            //Linha 3 (RB.2)
            if (ucTeclado.Linha3) portb &= ~0x04;   //Linha 3 = 0
            else portb |= 0x04;                     //Linha 3 = 1

            //Teste do botões RB0 e RB1
            //Botão 0
            if (cbRB0.Checked == true) portb &= ~0x01;  //PORTB.0 = 0
            else portb |= 0x01;                         //PORTB.0 = 1
            //Botão 1
            if (cbRB1.Checked == true) portb &= ~0x02;  //PORTB.1 = 0               
            else portb |= 0x02;                         //PORTB.1 = 1

            //Atualiza o estado do PORTB
            bank_bin[0, 6] = portb;
            bank_bin[2, 6] = portb;

            if (cbRefresh.Checked)
            {
                //Converte para string
                string Value = IntToBinString(portb);

                //Faz o refresh dos 2 bancos DataGridView
                dgvBank0.Rows[6].Cells[2].Value = Value;
                dgvBank2.Rows[6].Cells[2].Value = Value;
            }

            //Verificação da ocorrência de borda em RB0 para fins de interrupção. (08/09/16)
            //Option_Reg.INTEDG (Reg 81h e 181h, bit 6)
            //Intcon.INTF (Reg 0B, 8Bh, 10B e 18Bh, bit 1)
            //Se    INTEDG = 1 (subida) e ocorreu uma borda de subida seta INTF
            //ou se INTEDG = 0 (descida) e ocorreu uma borda de descida seta INTF
            if (((bank_bin[1, 1] & 0x40) == 0x40) && (RB0_ant == 0) && ((portb & 0x01) == 1) ||
                ((bank_bin[1, 1] & 0x40) == 0x00) && (RB0_ant == 1) && ((portb & 0x01) == 0))
            {
                //Seta a flag de interrupção INTF
                bank_bin[0, 0x0B] |= 0x02;
                bank_bin[1, 0x0B] |= 0x02;
                bank_bin[2, 0x0B] |= 0x02;
                bank_bin[3, 0x0B] |= 0x02;

                if (cbRefresh.Checked)
                {
                    //Converte para string
                    string intcon = IntToBinString(bank_bin[0, 0x0B]);

                    //Faz o refresh dos 4 bancos DataGridView
                    dgvBank0.Rows[11].Cells[2].Value = intcon;
                    dgvBank1.Rows[11].Cells[2].Value = intcon;
                    dgvBank2.Rows[11].Cells[2].Value = intcon;
                    dgvBank3.Rows[11].Cells[2].Value = intcon;
                }
            }

            //Atualiza RB0 anterior com o valor atual
            RB0_ant = portb & 0x01;
        }
        /// <summary>
        /// Atualiza o estado dos leds e displays de 7 segmentos (PORTD)
        /// Atenção: Esse método não está sendo atualizado desde a versão 3.0 do programa.
        /// </summary>
        private void AtualizaLedsDisplays()
        {
            //Lê o valor do PORTD, A e E (BANK0 END5, 8 e 9)
            //Se o valor do PORTD, A ou E foi alterado, o tratamento é necessário
            if (PORTD_ant != bank[0, 8, 2] || PORTA_ant != bank[0, 5, 2] || PORTE_ant != bank[0, 9, 2])
            {
                //Atualiza PORTB_ant
                PORTD_ant = bank[0, 8, 2];
                //Atualiza PORTA_ant
                PORTA_ant = bank[0, 5, 2];
                //Atualiza PORTE_ant
                PORTE_ant = bank[0, 9, 2];

                //Leds
                //Lê o valor do PORTA (BANK0 END5)
                char[] ValueArrayPortA = bank[0, 5, 2].ToCharArray();
                //Se PORTA.5 está em 1 os leds estão habilitados
                if (ValueArrayPortA[2] == '1')
                {
                    //Tranforna a string em um array de caracteres (0s e 1s)
                    char[] ValueArrayLed = bank[0, 8, 2].ToCharArray();

                    if (ValueArrayLed[7] == '1') ucLED0.Estado = Library_LPD_UserControl.UserControlLed.Estados.On;
                    else ucLED0.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;

                    if (ValueArrayLed[6] == '1') ucLED1.Estado = Library_LPD_UserControl.UserControlLed.Estados.On;
                    else ucLED1.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;

                    if (ValueArrayLed[5] == '1') ucLED2.Estado = Library_LPD_UserControl.UserControlLed.Estados.On;
                    else ucLED2.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;

                    if (ValueArrayLed[4] == '1') ucLED3.Estado = Library_LPD_UserControl.UserControlLed.Estados.On;
                    else ucLED3.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;

                    if (ValueArrayLed[3] == '1') ucLED4.Estado = Library_LPD_UserControl.UserControlLed.Estados.On;
                    else ucLED4.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;

                    if (ValueArrayLed[2] == '1') ucLED5.Estado = Library_LPD_UserControl.UserControlLed.Estados.On;
                    else
                        ucLED5.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;

                    if (ValueArrayLed[1] == '1') ucLED6.Estado = Library_LPD_UserControl.UserControlLed.Estados.On;
                    else ucLED6.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;

                    if (ValueArrayLed[0] == '1') ucLED7.Estado = Library_LPD_UserControl.UserControlLed.Estados.On;
                    else ucLED7.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;
                }
                else
                {
                    //Todos os leds desligados
                    ucLED0.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;
                    ucLED1.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;
                    ucLED2.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;
                    ucLED3.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;
                    ucLED4.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;
                    ucLED5.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;
                    ucLED6.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;
                    ucLED7.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;
                }

                //Displays
                //Lê o valor do PORTE (BANK0 END9)
                //Tranforna a string em um array de caracteres (0s e 1s)
                char[] ValueArrayPortE = bank[0, 9, 2].ToCharArray();
                //Se PORTE.0 está em 1 os Display1 está habilitado
                if (ValueArrayPortE[7] == '1') ucDisp1.ValorBinario(Convert.ToByte(bank[0, 8, 2], 2));
                else ucDisp1.ValorBinario(0);
                //Se PORTE.1 está em 1 os Display2 está habilitado
                if (ValueArrayPortE[6] == '1') ucDisp2.ValorBinario(Convert.ToByte(bank[0, 8, 2], 2));
                else ucDisp2.ValorBinario(0);
                //Se PORTE.2 está em 1 os Display3 está habilitado
                if (ValueArrayPortE[5] == '1') ucDisp3.ValorBinario(Convert.ToByte(bank[0, 8, 2], 2));
                else ucDisp3.ValorBinario(0);

                //Se PORTA.4 está em 1 o LCD está habilitado
                if (ValueArrayPortA[3] == '1')
                {
                    //Montagem do valor a ser enviado
                    //Formato do dado:
                    //15..11    10  9   8   7   6   5   4   3   2   1   0
                    // x..x     RS  RW  E   DB7 DB6 DB5 DB4 DB3 DB2 DB1 DB0
                    // 0..0     RD4 RD5 RD6 RD3 RD2 RD1 RD0 0   0   0   0
                    // Array    3   2   1   4   5   6   7
                    //RS = 0 --> Comando
                    //RS = 1 --> Dado
                    //RW = 0 --> Escreve
                    //RW = 1 --> Lê
                    //E = 0 = 1 = 0 --> Pulso de escrita no LCD
                    //Tranforna a string em um array de caracteres (0s e 1s)
                    //char[] ValueArrayLCD = bank[0, 8, 2].ToCharArray();
                    string PORTD = bank[0, 8, 2];
                    string LCD = "00000" + PORTD[3] + PORTD[2] + PORTD[1] + PORTD.Substring(4, 4) + "0000";
                    ushort usLCD = Convert.ToUInt16(LCD, 2);
                    ucLCD.EnviaComandoOuDado(usLCD);
                }
            }
        }
        /// <summary>
        /// Atualiza o estado dos leds e displays de 7 segmentos (PORTD) em binário.
        /// </summary>
        private void AtualizaLedsDisplays_Binario()
        {
            //Lê o valor do PORTD, A e E (BANK0 END 8, 5 e 9)
            //Se o valor do PORTD, A ou E foi alterado, o tratamento é necessário
            if ((PORTD_ant_bin != bank_bin[0, 8]) || (PORTA_ant_bin != bank_bin[0, 5]) || (PORTE_ant_bin != bank_bin[0, 9]))
            {
                //Atualiza PORTB_ant_bin
                PORTD_ant_bin = bank_bin[0, 8];
                //Atualiza PORTA_ant_bin
                PORTA_ant_bin = bank_bin[0, 5];
                //Atualiza PORTE_ant_bin
                PORTE_ant_bin = bank_bin[0, 9];

                //Monta a máscara de saída do PortD levando em conta TrisD (0=Output)
                int maskD = bank_bin[0, 8] & ~bank_bin[1, 8];

                //Leds
                //Se PORTA.5 está em 1 os leds estão habilitados (PORTD: BANK0 END8)
                if ((bank_bin[0, 5] & 0x20) != 0)
                {
                    if ((maskD & 0x01) != 0) ucLED0.Estado = Library_LPD_UserControl.UserControlLed.Estados.On;
                    else ucLED0.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;

                    if ((maskD & 0x02) != 0) ucLED1.Estado = Library_LPD_UserControl.UserControlLed.Estados.On;
                    else ucLED1.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;

                    if ((maskD & 0x04) != 0) ucLED2.Estado = Library_LPD_UserControl.UserControlLed.Estados.On;
                    else ucLED2.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;

                    if ((maskD & 0x08) != 0) ucLED3.Estado = Library_LPD_UserControl.UserControlLed.Estados.On;
                    else ucLED3.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;

                    if ((maskD & 0x10) != 0) ucLED4.Estado = Library_LPD_UserControl.UserControlLed.Estados.On;
                    else ucLED4.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;

                    if ((maskD & 0x20) != 0) ucLED5.Estado = Library_LPD_UserControl.UserControlLed.Estados.On;
                    else
                        ucLED5.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;

                    if ((maskD & 0x40) != 0) ucLED6.Estado = Library_LPD_UserControl.UserControlLed.Estados.On;
                    else ucLED6.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;

                    if ((maskD & 0x80) != 0) ucLED7.Estado = Library_LPD_UserControl.UserControlLed.Estados.On;
                    else ucLED7.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;
                }
                else
                {
                    //Todos os leds desligados
                    ucLED0.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;
                    ucLED1.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;
                    ucLED2.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;
                    ucLED3.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;
                    ucLED4.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;
                    ucLED5.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;
                    ucLED6.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;
                    ucLED7.Estado = Library_LPD_UserControl.UserControlLed.Estados.Off;
                }

                //Displays
                //Lê o valor do PORTE (BANK0 END9)
                //Se PORTE.0 está em 1 os Display1 está habilitado
                if ((bank_bin[0, 9] & 0x01) != 0) ucDisp1.ValorBinario((byte)maskD);
                else ucDisp1.ValorBinario(0);
                //Se PORTE.1 está em 1 os Display2 está habilitado
                if ((bank_bin[0, 9] & 0x02) != 0) ucDisp2.ValorBinario((byte)maskD);
                else ucDisp2.ValorBinario(0);
                //Se PORTE.2 está em 1 os Display3 está habilitado
                if ((bank_bin[0, 9] & 0x04) != 0) ucDisp3.ValorBinario((byte)maskD);
                else ucDisp3.ValorBinario(0);

                //Se PORTA.4 está em 1 o LCD está habilitado
                if ((bank_bin[0, 5] & 0x10) != 0)
                {
                    //Montagem do valor a ser enviado
                    //Formato do dado:
                    //15..11    10  9   8   7   6   5   4   3   2   1   0
                    // x..x     RS  RW  E   DB7 DB6 DB5 DB4 DB3 DB2 DB1 DB0
                    // 0..0     RD4 RD5 RD6 RD3 RD2 RD1 RD0 0   0   0   0
                    // Array    3   2   1   4   5   6   7
                    //RS = 0 --> Comando
                    //RS = 1 --> Dado
                    //RW = 0 --> Escreve
                    //RW = 1 --> Lê
                    //E = 0 = 1 = 0 --> Pulso de escrita no LCD

                    int b10 = (maskD & 0x10) << 6;
                    int b9 = (maskD & 0x20) << 4;
                    int b8 = (maskD & 0x40) << 2;
                    int b7a4 = (maskD & 0x0F) << 4;
                    int b10a4 = b10 + b9 + b8 + b7a4;

                    ucLCD.EnviaComandoOuDado((ushort)b10a4);

                    //DEBUG
#if DebugLCD
                    EnviaComandoOuDado_DEBUG((ushort)b10a4);
#endif
                }
            }
        }
        #endregion

        //---------------------------------------------------------------------
        #region Thread para Aumento de Desempenho------------------------------
        #region Variáveis e Objetos da Thread----------------------------------
        Thread threadRun;
        bool fazerReset = false;
        bool pararThread = false;
#if TesteThread
        int periodo_ms = 10;
#endif
        bool refreshLCD = false;
        delegate void PegaChar(int caractere);
        public int ticksThread = 5000;
        public bool turbo = false;         //Criado na versão 3.3
        #endregion

        #region Métodos utilizados pela Thread---------------------------------
        /// <summary>
        /// Interpreta e executa cada uma das instruções de acordo com o 
        /// endereço apontado pela program counter (PC) usando pela Thread.
        /// </summary>
        private void Clock_Run_Binario_Thread()
        {
            //Variáveis locais
            int Endereço = -1;
            fazerReset = false;
            pararThread = false;
            Stopwatch stopWatch = new Stopwatch();

            try
            {
                while (!pararThread)
                {
                    Endereço = -1;
                    ticks++;
#if TesteThread
                    Thread.Sleep(periodo_ms);
#endif
                    if (turbo == false)         //Criado na versão 3.3
                    {
                        stopWatch.Reset();
                        stopWatch.Start();
                        while (stopWatch.ElapsedTicks < ticksThread) ;
                    }
                    else
                    {
                        stopWatch.Reset();
                        stopWatch.Start();
                        while (stopWatch.ElapsedTicks < 3) ;    //Máximo medido 8,2 milhoes de instruções / 10s
                    }

                    #region EXECUÇÃO DOS COMANDOS ///////////////////////////////////////////////////////////////////////////////
                    //Instrução atual é igual ao valor do contador de programas
                    Atual = PC;
                    //Incrementa o contador de programas para apontar a próxima instrução
                    PC++;
                    PC %= 8192;             //Limita o valor de PC ao tamanho da memória
                    //Tipo de instrução
                    int tipo;
                    //Verifica os 2 bits mais significativos das instruções
                    tipo = memoria_bin[Atual] & 0x3000;

                    //Índice do bit
                    int x;
                    //Máscara do bit
                    int mbit = 1;
                    //Endereçamento indireto (06/09/16)
                    bool indf = false;

                    //Literal
                    int literal = memoria_bin[Atual] & 0x00FF;
                    bool DC;    //Digit Carry (Staus.1)

                    //Destino
                    bool destino = false;   //Padrão salvar em W (fiz isso por conta do refresh do Status e outros registradores duplicados)
                    //Temporário
                    int temp;
                    //Previsão de Carry
                    bool preCarry;

                    //PCL sempre tem o valor de PC
                    //Refresh de PLC nos 4 bancos
                    bank_bin[0, 0x02] = PC & 0xFF;
                    bank_bin[1, 0x02] = PC & 0xFF;
                    bank_bin[2, 0x02] = PC & 0xFF;
                    bank_bin[3, 0x02] = PC & 0xFF;

                    //Executa a instrução atual

                    //Teste se é CALL ou GOTO
                    if (tipo == 0x2000)
                    {
                        switch (memoria_bin[Atual] & 0x3800)
                        {
                            #region CALL
                            case 0x2000:    //ALTERA STACK
                                //Se a pilha esta cheia remove o último registro
                                if (picStack.Count >= 8) picStack.Pop();
                                //Insere o valor do program counter na pilha
                                picStack.Push(PC);
                                //Atualiza program counter com o valor de desvio
                                //PC = memoria_bin[Atual] & 0x07FF;
                                //Separa os bits 4 e 3 de PCLATH (06/09/16)
                                int pclath43call = (bank_bin[0, 0x0A] & 0x18) << 8;
                                PC = pclath43call + (memoria_bin[Atual] & 0x07FF);
                                //Sinaliza refresh do stack
                                refreshStack_push = true;
                                break;
                            #endregion
                            #region GOTO
                            case 0x2800:    //PULO PARA ENDEREÇO
                                {
                                    //PC = (int)memoria_bin[Atual] & 0x07FF;
                                    //Separa os bits 4 e 3 de PCLATH (06/09/16)
                                    int pclath43goto = (bank_bin[0, 0x0A] & 0x18) << 8;
                                    PC = pclath43goto + (memoria_bin[Atual] & 0x07FF);
                                    break;
                                }
                            #endregion
                        }
                    }
                    //Teste se é orientada ao bit
                    else if (tipo == 0x1000)
                    {
                        //Endereço do registrador
                        Endereço = memoria_bin[Atual] & 0x007F;
                        //Índice do bit
                        x = (memoria_bin[Atual] & 0x0380) >> 7;
                        //Máscara do bit
                        mbit = 1;
                        mbit = mbit << x;

                        //Tratamento especial para INDF (06/09/16)
                        //Preparação
                        if (Endereço == 0)
                        {
                            //Seta flag de sinalização
                            indf = true;
                            //Lê FSR (File Select Regisiter) bits 6..0
                            Endereço = bank_bin[0, 4] & 0x7F;
                            //Atualiza banco selecionado
                            //        Status,IRP                   + FSR,7
                            selBank = ((bank_bin[0, 3] & 0x80) >> 6) + ((bank_bin[0, 4] & 0x80) >> 7);
                        }

                        switch (memoria_bin[Atual] & 0x3C00)
                        {
                            #region BCF
                            case 0x1000:    //LIMPA UM ÚNICO BIT EM UM REGISTRADOR
                                //Atualiza o registrador
                                bank_bin[selBank, Endereço] = (bank_bin[selBank, Endereço] & (~mbit)) & 0x00FF;
                                //Sinaliza necessidade de atualização de registrador
                                refreshReg = true;
                                //Sianliza que um registrador foi alterado e precisa ser tratados
                                destino = true;
                                //Se alterou o registrador Status deve espelhar no 4 bancos
                                if (Endereço == 3)
                                {
                                    Status_bin = bank_bin[selBank, Endereço];
                                    refreshStatus = true;
                                }
                                break;
                            #endregion
                            #region BSF
                            case 0x1400:    //SETA UM ÚNICO BIT EM UM REGISTRADOR
                                //Atualiza o registrador
                                bank_bin[selBank, Endereço] = (bank_bin[selBank, Endereço] | mbit) & 0x00FF;
                                //Sinaliza necessidade de atualização de registrador
                                refreshReg = true;
                                //Sianliza que um registrador foi alterado e precisa ser tratados
                                destino = true;
                                //Se alterou o registrador Status deve espelhar no 4 bancos
                                if (Endereço == 3)
                                {
                                    Status_bin = bank_bin[selBank, Endereço];
                                    refreshStatus = true;
                                }
                                break;
                            #endregion
                            #region BTFSC
                            case 0x1800:    //TESTA UM ÚNICO BIT EM UM REGISTRADOR, SE FOR 0, PULA 1 ENDEREÇO, SENÃO CONTINUA
                                if ((bank_bin[selBank, Endereço] & mbit) == 0)
                                    PC++;
                                //Permite a visualização do registrador testado
                                refreshReg = true;
                                break;
                            #endregion
                            #region BTFSS
                            case 0x1C00:    //TESTA UM ÚNICO BIT EM UM REGISTRADOR, SE FOR 1, PULA 1 ENDEREÇO, SENÃO CONTINUA
                                if ((bank_bin[selBank, Endereço] & mbit) != 0)
                                    PC++;
                                //Permite a visualização do registrador testado
                                refreshReg = true;
                                break;
                            #endregion
                        }
                    }
                    //Teste se é literal
                    else if (tipo == 0x3000)
                    {
                        //Sinaliza necessidade de atualização de registrador
                        refreshWork = true;

                        switch (memoria_bin[Atual] & 0x3F00)
                        {
                            #region ADDLW
                            case 0x3E00:
                            case 0x3F00:
                                //Verifica o estado do 4o bit
                                if ((Work_bin & 0x10) > 0) DC = true;
                                else DC = false;
                                //Realiza a operação
                                Work_bin += literal;
                                //Se deu carry
                                if (Work_bin > 255) Status_bin |= 0x01;   //Seta o bit C do registrador Status
                                else Status_bin &= 0xFE;                  //Reseta o bit C do registrador Status
                                //Se deu digit carry
                                if (((Work_bin & 0x10) > 0) && (DC == false)) Status_bin |= 0x02; //Seta o bit DC do registrador Status
                                else Status_bin &= 0xFD;                                          //Reseta o bit DC do registrador Status
                                //Ajusta o resultado da operação
                                Work_bin &= 0xFF;
                                //Se deu zero
                                if (Work_bin == 0) Status_bin |= 0x04;    //Seta o bit Z do registrador Status
                                else Status_bin &= 0xFB;                  //Reseta o bit Z do registrador Status
                                //Sinaliza necessidade de atualização de registrador
                                refreshStatus = true;
                                break;
                            #endregion
                            #region ANDLW
                            case 0x3900:
                                //Realiza a operação
                                Work_bin &= literal;
                                //Ajusta o resultado da operação
                                Work_bin &= 0xFF;
                                //Se deu zero
                                if (Work_bin == 0) Status_bin |= 0x04;    //Seta o bit Z do registrador Status
                                else Status_bin &= 0xFB;                  //Reseta o bit Z do registrador Status
                                //Sinaliza necessidade de atualização de registrador
                                refreshStatus = true;
                                break;
                            #endregion
                            #region IORLW
                            case 0x3800:
                                //Realiza a operação
                                Work_bin |= literal;
                                //Ajusta o resultado da operação
                                Work_bin &= 0xFF;
                                //Se deu zero
                                if (Work_bin == 0) Status_bin |= 0x04;    //Seta o bit Z do registrador Status
                                else Status_bin &= 0xFB;                  //Reseta o bit Z do registrador Status
                                //Sinaliza necessidade de atualização de registrador
                                refreshStatus = true;
                                break;
                            #endregion
                            #region MOVLW
                            case 0x3000:
                            case 0x3300:
                                //Realiza a operação
                                Work_bin = literal;
                                //Ajusta o resultado da operação
                                Work_bin &= 0xFF;
                                break;
                            #endregion
                            #region RETLW
                            case 0x3400:  //RETORNA PARA POSIÇÃO DO STACK E RETORNA UM NÚMERO PARA WORK
                            case 0x3700:
                                //Se a pilha não está vazia remove um registro
                                if (picStack.Count > 0) PC = (int)picStack.Pop();
                                //Grava em W o valor deretorno
                                Work_bin = literal;
                                //Sinaliza refresh do stack
                                refreshStack_pop = true;
                                break;
                            #endregion
                            #region SUBLW
                            case 0x3C:
                            case 0x3D:
                                //Verifica o estado do 4o bit
                                if ((Work_bin & 0x10) > 0) DC = true;
                                else DC = false;
                                //Realiza a operação
                                Work_bin = literal - Work_bin;
                                //Se deu borrow carry 
                                if (Work_bin > 0) Status_bin |= 0x01; //Seta o bit C do registrador Status
                                else Status_bin &= 0xFE;              //Reseta o bit C do registrador Status
                                //Se deu digit carry
                                if (((Work_bin & 0x10) > 0) && (DC == false)) Status_bin |= 0x02; //Seta o bit DC do registrador Status
                                else Status_bin &= 0xFD;                                          //Reseta o bit DC do registrador Status
                                //Ajusta o resultado da operação
                                Work_bin &= 0xFF;
                                //Se deu zero
                                if (Work_bin == 0) Status_bin |= 0x04;    //Seta o bit Z do registrador Status
                                else Status_bin &= 0xFB;                  //Reseta o bit Z do registrador Status
                                //Sinaliza necessidade de atualização de registrador
                                refreshStatus = true;
                                break;
                            #endregion
                            #region XORLW
                            case 0x3A00:
                                //Realiza a operação
                                Work_bin ^= literal;
                                //Ajusta o resultado da operação
                                Work_bin &= 0xFF;
                                //Se deu zero
                                if (Work_bin == 0) Status_bin |= 0x04;    //Seta o bit Z do registrador Status
                                else Status_bin &= 0xFB;                  //Reseta o bit Z do registrador Status
                                //Sinaliza necessidade de atualização de registrador
                                refreshStatus = true;
                                break;
                            #endregion
                        }
                    }
                    //Demais orientada ao byte e controle
                    else
                    {
                        //Controle
                        #region CRLWDT
                        if (memoria_bin[Atual] == 0x0064)
                        { }
                        #endregion
                        #region RETFIE
                        else if (memoria_bin[Atual] == 0x0009)
                        {
                            //Se a pilha não está vazia remove um registro
                            if (picStack.Count > 0) PC = (int)picStack.Pop();
                            //Sinaliza refresh do stack
                            refreshStack_pop = true;
                            //Seta o bit GIE do registrador INTCON
                            bank_bin[selBank, 0x0B] |= 0x80;
                            //Sinaliza necessidade de atualização de registrador
                            refreshIntcon = true;
                        }
                        #endregion
                        #region RETURN
                        else if (memoria_bin[Atual] == 0x0008)
                        {
                            //Se a pilha não está vazia remove um registro
                            if (picStack.Count > 0) PC = (int)picStack.Pop();
                            //Sinaliza refresh do stack
                            refreshStack_pop = true;
                        }
                        #endregion
                        #region SLEEP
                        else if (memoria_bin[Atual] == 0x0003)
                        { }
                        #endregion
                        //Orienta ao byte
                        else
                        {
                            //Endereço do registrador
                            Endereço = memoria_bin[Atual] & 0x007F;
                            //Destino
                            if ((memoria_bin[Atual] & 0x0080) > 0)
                                destino = true;
                            else
                                destino = false;

                            //Tratamento especial para INDF (06/09/16)
                            //Preparação
                            if (Endereço == 0)
                            {
                                //Seta flag de sinalização
                                indf = true;
                                //Lê FSR (File Select Regisiter) bits 6..0
                                Endereço = bank_bin[0, 4] & 0x7F;
                                //Atualiza banco selecionado
                                //        Status,IRP                   + FSR,7
                                selBank = ((bank_bin[0, 3] & 0x80) >> 6) + ((bank_bin[0, 4] & 0x80) >> 7);
                            }

                            switch (memoria_bin[Atual] & 0x3F00)
                            {
                                //Como essas operações podem ser feitas com o registrador Status,
                                //a alteração das flags é feita no registrador do Status do banco selecionado e
                                //por último a variável Stauts_bin é atulizada com seu valor e
                                //a sinalização de refreshStatus é feita
                                #region ADDWF
                                case 0x0700:
                                    //Verifica o estado do 4o bit (segundo operador sempre)
                                    if ((bank_bin[selBank, Endereço] & 0x10) > 0) DC = true;
                                    else DC = false;
                                    //Realiza a operação
                                    temp = Work_bin + bank_bin[selBank, Endereço];
                                    //Se deu carry
                                    if (temp > 255) bank_bin[selBank, 3] |= 0x01;   //Seta o bit C do registrador Status
                                    else bank_bin[selBank, 3] &= 0xFE;              //Reseta o bit C do registrador Status
                                    //Se deu digit carry
                                    if (((Work_bin & 0x10) > 0) && (DC == false)) bank_bin[selBank, 3] |= 0x02; //Seta o bit DC do registrador Status
                                    else bank_bin[selBank, 3] &= 0xFD;                                          //Reseta o bit DC do registrador Status
                                    //Ajusta o resultado da operação
                                    temp &= 0xFF;
                                    //Se deu zero
                                    if (temp == 0) bank_bin[selBank, 3] |= 0x04;    //Seta o bit Z do registrador Status
                                    else bank_bin[selBank, 3] &= 0xFB;              //Reseta o bit Z do registrador Status
                                    //Atualiza o registrador destino da operação
                                    if (destino)
                                    {
                                        bank_bin[selBank, Endereço] = temp;
                                        //Sinaliza necessidade de atualização de registrador
                                        refreshReg = true;
                                    }
                                    else
                                    {
                                        Work_bin = temp;
                                        //Sinaliza necessidade de atualização de registrador
                                        refreshWork = true;
                                    }
                                    //Sinaliza necessidade de atualização de registrador
                                    Status_bin = bank_bin[selBank, 3];
                                    refreshStatus = true;
                                    break;
                                #endregion
                                #region ANDWF
                                case 0x0500:
                                    //Realiza a operação
                                    temp = Work_bin & bank_bin[selBank, Endereço];
                                    //Ajusta o resultado da operação
                                    temp &= 0xFF;
                                    //Se deu zero
                                    if (temp == 0) bank_bin[selBank, 3] |= 0x04;    //Seta o bit Z do registrador Status
                                    else bank_bin[selBank, 3] &= 0xFB;              //Reseta o bit Z do registrador Status
                                    //Atualiza o registrador destino da operação
                                    if (destino)
                                    {
                                        bank_bin[selBank, Endereço] = temp;
                                        //Sinaliza necessidade de atualização de registrador
                                        refreshReg = true;
                                    }
                                    else
                                    {
                                        Work_bin = temp;
                                        //Sinaliza necessidade de atualização de registrador
                                        refreshWork = true;
                                    }
                                    //Sinaliza necessidade de atualização de registrador
                                    Status_bin = bank_bin[selBank, 3];
                                    refreshStatus = true;
                                    break;
                                #endregion
                                #region CLRF ou CLRW
                                case 0x0100:
                                    //Realiza a operação
                                    temp = 0;
                                    //Ajusta o resultado da operação
                                    temp &= 0xFF;
                                    //Se deu zero
                                    if (temp == 0) bank_bin[selBank, 3] |= 0x04;    //Seta o bit Z do registrador Status
                                    else bank_bin[selBank, 3] &= 0xFB;              //Reseta o bit Z do registrador Status
                                    //Atualiza o registrador destino da operação
                                    if (destino)
                                    {
                                        //CLRF
                                        bank_bin[selBank, Endereço] = temp;
                                        //Sinaliza necessidade de atualização de registrador
                                        refreshReg = true;
                                    }
                                    else
                                    {
                                        //CLRW
                                        Work_bin = temp;
                                        //Sinaliza necessidade de atualização de registrador
                                        refreshWork = true;
                                    }
                                    //Sinaliza necessidade de atualização de registrador
                                    Status_bin = bank_bin[selBank, 3];
                                    refreshStatus = true;
                                    break;
                                #endregion
                                #region COMF
                                case 0x0900:
                                    //Realiza a operação
                                    temp = ~bank_bin[selBank, Endereço];
                                    //Ajusta o resultado da operação
                                    temp &= 0xFF;
                                    //Se deu zero
                                    if (temp == 0) bank_bin[selBank, 3] |= 0x04;    //Seta o bit Z do registrador Status
                                    else bank_bin[selBank, 3] &= 0xFB;              //Reseta o bit Z do registrador Status
                                    //Atualiza o registrador destino da operação
                                    if (destino)
                                    {
                                        bank_bin[selBank, Endereço] = temp;
                                        //Sinaliza necessidade de atualização de registrador
                                        refreshReg = true;
                                    }
                                    else
                                    {
                                        Work_bin = temp;
                                        //Sinaliza necessidade de atualização de registrador
                                        refreshWork = true;
                                    }
                                    //Sinaliza necessidade de atualização de registrador
                                    Status_bin = bank_bin[selBank, 3];
                                    refreshStatus = true;
                                    break;
                                #endregion
                                #region DECF
                                case 0x0300:
                                    //Realiza a operação
                                    temp = bank_bin[selBank, Endereço] - 1;
                                    //Ajusta o resultado da operação
                                    temp &= 0xFF;
                                    //Se deu zero
                                    if (temp == 0) bank_bin[selBank, 3] |= 0x04;    //Seta o bit Z do registrador Status
                                    else bank_bin[selBank, 3] &= 0xFB;              //Reseta o bit Z do registrador Status
                                    //Atualiza o registrador destino da operação
                                    if (destino)
                                    {
                                        bank_bin[selBank, Endereço] = temp;
                                        //Sinaliza necessidade de atualização de registrador
                                        refreshReg = true;
                                    }
                                    else
                                    {
                                        Work_bin = temp;
                                        //Sinaliza necessidade de atualização de registrador
                                        refreshWork = true;
                                    }
                                    //Sinaliza necessidade de atualização de registrador
                                    Status_bin = bank_bin[selBank, 3];
                                    refreshStatus = true;
                                    break;
                                #endregion
                                #region DECFSZ
                                case 0x0B00:
                                    //Realiza a operação
                                    temp = bank_bin[selBank, Endereço] - 1;
                                    //Ajusta o resultado da operação
                                    temp &= 0xFF;
                                    //Se deu zero
                                    if (temp == 0) PC++;    //Salta uma instrução
                                    //Atualiza o registrador destino da operação
                                    if (destino)
                                    {
                                        bank_bin[selBank, Endereço] = temp;
                                        //Sinaliza necessidade de atualização de registrador
                                        refreshReg = true;
                                    }
                                    else
                                    {
                                        Work_bin = temp;
                                        //Sinaliza necessidade de atualização de registrador
                                        refreshWork = true;
                                    }
                                    break;
                                #endregion
                                #region INCF
                                case 0x0A00:
                                    //Realiza a operação
                                    temp = bank_bin[selBank, Endereço] + 1;
                                    //Ajusta o resultado da operação
                                    temp &= 0xFF;
                                    //Se deu zero
                                    if (temp == 0) bank_bin[selBank, 3] |= 0x04;    //Seta o bit Z do registrador Status
                                    else bank_bin[selBank, 3] &= 0xFB;              //Reseta o bit Z do registrador Status
                                    //Atualiza o registrador destino da operação
                                    if (destino)
                                    {
                                        bank_bin[selBank, Endereço] = temp;
                                        //Sinaliza necessidade de atualização de registrador
                                        refreshReg = true;
                                    }
                                    else
                                    {
                                        Work_bin = temp;
                                        //Sinaliza necessidade de atualização de registrador
                                        refreshWork = true;
                                    }
                                    //Sinaliza necessidade de atualização de registrador
                                    Status_bin = bank_bin[selBank, 3];
                                    refreshStatus = true;
                                    break;
                                #endregion
                                #region INFFSZ
                                case 0x0F00:
                                    //Realiza a operação
                                    temp = bank_bin[selBank, Endereço] + 1;
                                    //Ajusta o resultado da operação
                                    temp &= 0xFF;
                                    //Se deu zero
                                    if (temp == 0) PC++;    //Salta uma instrução
                                    //Atualiza o registrador destino da operação
                                    if (destino)
                                    {
                                        bank_bin[selBank, Endereço] = temp;
                                        //Sinaliza necessidade de atualização de registrador
                                        refreshReg = true;
                                    }
                                    else
                                    {
                                        Work_bin = temp;
                                        //Sinaliza necessidade de atualização de registrador
                                        refreshWork = true;
                                    }
                                    break;
                                #endregion
                                #region IORWF
                                case 0x0400:
                                    //Realiza a operação
                                    temp = Work_bin | bank_bin[selBank, Endereço];
                                    //Ajusta o resultado da operação
                                    temp &= 0xFF;
                                    //Se deu zero
                                    if (temp == 0) bank_bin[selBank, 3] |= 0x04;    //Seta o bit Z do registrador Status
                                    else bank_bin[selBank, 3] &= 0xFB;              //Reseta o bit Z do registrador Status
                                    //Atualiza o registrador destino da operação
                                    if (destino)
                                    {
                                        bank_bin[selBank, Endereço] = temp;
                                        //Sinaliza necessidade de atualização de registrador
                                        refreshReg = true;
                                    }
                                    else
                                    {
                                        Work_bin = temp;
                                        //Sinaliza necessidade de atualização de registrador
                                        refreshWork = true;
                                    }
                                    //Sinaliza necessidade de atualização de registrador
                                    Status_bin = bank_bin[selBank, 3];
                                    refreshStatus = true;
                                    break;
                                #endregion
                                #region MOVF
                                case 0x0800:
                                    //Realiza a operação
                                    temp = bank_bin[selBank, Endereço];
                                    //Ajusta o resultado da operação
                                    temp &= 0xFF;
                                    //Se deu zero
                                    if (temp == 0) bank_bin[selBank, 3] |= 0x04;    //Seta o bit Z do registrador Status
                                    else bank_bin[selBank, 3] &= 0xFB;              //Reseta o bit Z do registrador Status
                                    //Atualiza o registrador destino da operação
                                    if (destino)
                                    {
                                        bank_bin[selBank, Endereço] = temp;
                                        //Sinaliza necessidade de atualização de registrador
                                        refreshReg = true;
                                    }
                                    else
                                    {
                                        Work_bin = temp;
                                        //Sinaliza necessidade de atualização de registrador
                                        refreshWork = true;
                                    }
                                    //Sinaliza necessidade de atualização de registrador
                                    Status_bin = bank_bin[selBank, 3];
                                    refreshStatus = true;
                                    break;
                                #endregion
                                #region MOVWF ou NOP
                                case 0x0000:
                                    //Realiza a operação
                                    temp = Work_bin;
                                    //Ajusta o resultado da operação
                                    temp &= 0xFF;
                                    //Atualiza o registrador destino da operação
                                    if (destino)
                                    {
                                        //MOVF
                                        bank_bin[selBank, Endereço] = temp;
                                        //Sinaliza necessidade de atualização de registrador
                                        refreshReg = true;
                                    }
                                    else
                                    {
                                        //NOP
                                    }
                                    //Sinaliza necessidade de atualização de registrador
                                    Status_bin = bank_bin[selBank, 3];
                                    refreshStatus = true;
                                    break;
                                #endregion
                                #region RLF
                                case 0x0D00:
                                    //Preve se vai dar carry (o bit que sair é movido para o bit Carry)
                                    if ((bank_bin[selBank, Endereço] & 0x80) != 0) preCarry = true;   //Seta o preCarry
                                    else preCarry = false;                                            //Reseta o preCarry
                                    //Realiza a operação
                                    temp = (bank_bin[selBank, Endereço] << 1) | (bank_bin[selBank, 3] & 0x01);
                                    //Se deu carry
                                    if (preCarry) bank_bin[selBank, 3] |= 0x01; //Seta o bit C do registrador Status
                                    else bank_bin[selBank, 3] &= 0xFE;          //Reseta o bit C do registrador Status
                                    //Ajusta o resultado da operação
                                    temp &= 0xFF;
                                    //Atualiza o registrador destino da operação
                                    if (destino)
                                    {
                                        bank_bin[selBank, Endereço] = temp;
                                        //Sinaliza necessidade de atualização de registrador
                                        refreshReg = true;
                                    }
                                    else
                                    {
                                        Work_bin = temp;
                                        //Sinaliza necessidade de atualização de registrador
                                        refreshWork = true;
                                    }
                                    //Sinaliza necessidade de atualização de registrador
                                    Status_bin = bank_bin[selBank, 3];
                                    refreshStatus = true;
                                    break;
                                #endregion
                                #region RRF
                                case 0x0C00:
                                    //Preve se vai dar carry (o bit que sair é movido para o bit Carry)
                                    if ((bank_bin[selBank, Endereço] & 0x01) != 0) preCarry = true;   //Seta o preCarry
                                    else preCarry = false;                                            //Reseta o preCarry
                                    //Realiza a operação
                                    temp = (bank_bin[selBank, Endereço] >> 1) | ((bank_bin[selBank, 3] & 0x01) << 7);
                                    //Se deu carry
                                    if (preCarry) bank_bin[selBank, 3] |= 0x01; //Seta o bit C do registrador Status
                                    else bank_bin[selBank, 3] &= 0xFE;          //Reseta o bit C do registrador Status
                                    //Ajusta o resultado da operação
                                    temp &= 0xFF;
                                    //Atualiza o registrador destino da operação
                                    if (destino)
                                    {
                                        bank_bin[selBank, Endereço] = temp;
                                        //Sinaliza necessidade de atualização de registrador
                                        refreshReg = true;
                                    }
                                    else
                                    {
                                        Work_bin = temp;
                                        //Sinaliza necessidade de atualização de registrador
                                        refreshWork = true;
                                    }
                                    //Sinaliza necessidade de atualização de registrador
                                    Status_bin = bank_bin[selBank, 3];
                                    refreshStatus = true;
                                    break;
                                #endregion
                                #region SUBWF
                                case 0x0200:
                                    //Verifica o estado do 4o bit (segundo operador sempre)
                                    if ((bank_bin[selBank, Endereço] & 0x10) > 0) DC = true;
                                    else DC = false;
                                    //Realiza a operação
                                    temp = bank_bin[selBank, Endereço] - Work_bin;
                                    //Se deu borrow carry 
                                    if (temp > 0) bank_bin[selBank, 3] |= 0x01; //Seta o bit C do registrador Status
                                    else bank_bin[selBank, 3] &= 0xFE;          //Reseta o bit C do registrador Status
                                    //Se deu digit carry
                                    if (((temp & 0x10) > 0) && (DC == false)) bank_bin[selBank, 3] |= 0x02; //Seta o bit DC do registrador Status
                                    else bank_bin[selBank, 3] &= 0xFD;                                      //Reseta o bit DC do registrador Status
                                    //Ajusta o resultado da operação
                                    temp &= 0xFF;
                                    //Se deu zero
                                    if (temp == 0) bank_bin[selBank, 3] |= 0x04;    //Seta o bit Z do registrador Status
                                    else bank_bin[selBank, 3] &= 0xFB;                  //Reseta o bit Z do registrador Status
                                    //Atualiza o registrador destino da operação
                                    if (destino)
                                    {
                                        bank_bin[selBank, Endereço] = temp;
                                        //Sinaliza necessidade de atualização de registrador
                                        refreshReg = true;
                                    }
                                    else
                                    {
                                        Work_bin = temp;
                                        //Sinaliza necessidade de atualização de registrador
                                        refreshWork = true;
                                    }
                                    //Sinaliza necessidade de atualização de registrador
                                    Status_bin = bank_bin[selBank, 3];
                                    refreshStatus = true;
                                    break;
                                #endregion
                                #region SWAPF
                                case 0x0E00:
                                    //Realiza a operação
                                    temp = (bank_bin[selBank, Endereço] << 4) | (bank_bin[selBank, Endereço] >> 4);
                                    //Ajusta o resultado da operação
                                    temp &= 0xFF;
                                    //Atualiza o registrador destino da operação
                                    if (destino)
                                    {
                                        bank_bin[selBank, Endereço] = temp;
                                        //Sinaliza necessidade de atualização de registrador
                                        refreshReg = true;
                                    }
                                    else
                                    {
                                        Work_bin = temp;
                                        //Sinaliza necessidade de atualização de registrador
                                        refreshWork = true;
                                    }
                                    break;
                                #endregion
                                #region XORWF
                                case 0x0600:
                                    //Realiza a operação
                                    temp = Work_bin ^ bank_bin[selBank, Endereço];
                                    //Ajusta o resultado da operação
                                    temp &= 0xFF;
                                    //Se deu zero
                                    if (temp == 0) bank_bin[selBank, 3] |= 0x04;    //Seta o bit Z do registrador Status
                                    else bank_bin[selBank, 3] &= 0xFB;              //Reseta o bit Z do registrador Status
                                    //Atualiza o registrador destino da operação
                                    if (destino)
                                    {
                                        bank_bin[selBank, Endereço] = temp;
                                        //Sinaliza necessidade de atualização de registrador
                                        refreshReg = true;
                                    }
                                    else
                                    {
                                        Work_bin = temp;
                                        //Sinaliza necessidade de atualização de registrador
                                        refreshWork = true;
                                    }
                                    //Sinaliza necessidade de atualização de registrador
                                    Status_bin = bank_bin[selBank, 3];
                                    refreshStatus = true;
                                    break;
                                #endregion
                            }
                        }
                    }
                    #endregion

                    #region Refresh do Intcon (retorno de interrupção GIE = 1)
                    if (refreshIntcon)
                    {
                        //Refresh de INTCON nos 4 bancos
                        bank_bin[0, 0x0B] = bank_bin[selBank, 0x0B];
                        bank_bin[1, 0x0B] = bank_bin[selBank, 0x0B];
                        bank_bin[2, 0x0B] = bank_bin[selBank, 0x0B];
                        bank_bin[3, 0x0B] = bank_bin[selBank, 0x0B];

                        //Converte para string
                        string intcon = IntToBinString(bank_bin[selBank, 0x0B]);
                        refreshIntcon = false;
                    }
                    #endregion

                    #region Tratamento especial para Intcon
                    //Refresh do Intcon
                    if ((Endereço == 11) && (destino == true))   //Intcon (End.=11 em Bank0,1,2,3)
                    {
                        //Refresh de INTCON nos 4 bancos
                        bank_bin[0, 0x0B] = bank_bin[selBank, 0x0B];
                        bank_bin[1, 0x0B] = bank_bin[selBank, 0x0B];
                        bank_bin[2, 0x0B] = bank_bin[selBank, 0x0B];
                        bank_bin[3, 0x0B] = bank_bin[selBank, 0x0B];
                    }
                    #endregion

                    #region Tratamento especial para Option_Reg
                    //Refresh do Option_Reg
                    if (Endereço == 1 && ((selBank == 1) || (selBank == 3)) && (destino == true))   //Option_Reg (End.=1 em Bank1 e Bank3)
                    {
                        //Refresh de OPTION_REG nos 2 bancos
                        bank_bin[1, 0x01] = bank_bin[selBank, 0x01];
                        bank_bin[3, 0x01] = bank_bin[selBank, 0x01];

                        //Teste o bit 5 (T0CS: TMR0 Clock Source Select bit) (0=Clock Interno, 1=Clock RA4 (desativado))
                        if ((bank_bin[selBank, 0x01] & 0x20) == 0) TMR0 = true; //Temporizador
                        else TMR0 = false;                                      //Contador (dasativado)

                        //Teste o bit 3 (PSA: Prescaler Assignment bit)
                        if ((bank_bin[selBank, 0x01] & 0x08) == 0)
                        {
                            //Presacler no Timer0 (PS2:PS0: Prescaler Rate Select bits)
                            PS = bank_bin[selBank, 0x01] & 0x07;
                            PS = (int)Math.Pow(2, PS) * 2;  //2^PS * 2
                        }
                        else
                        {
                            //Prescaler no WDT
                            PS = 1;
                        }
                    }
                    #endregion

                    #region Tratamento especial para Timer0
                    //Refresh do TMR0 e timer0 (variável de controle)
                    if (Endereço == 1 && ((selBank == 0) || (selBank == 2)) && (destino == true))   //TMR0 (End.=1 em Bank0 e Bank2)
                    {
                        //Refresh de TMR0 nos 2 bancos
                        bank_bin[0, 0x01] = bank_bin[selBank, 0x01];
                        bank_bin[2, 0x01] = bank_bin[selBank, 0x01];

                        //Atualiza o TIMER0 com o valor atribuido
                        timer0 = bank_bin[selBank, 0x01];
                    }

                    if (TMR0 == true)
                    {
                        if ((ticks % PS == 0) || (cbPrescaler.Checked))
                        {
                            if (timer0 == 255)
                            {
                                //Zera o timer (overflow)
                                timer0 = 0;

                                //Seta a flag TMR0IF em INTCON (bit2) nos 4 bancos
                                bank_bin[0, 0x0B] |= 0x04;
                                bank_bin[1, 0x0B] |= 0x04;
                                bank_bin[2, 0x0B] |= 0x04;
                                bank_bin[3, 0x0B] |= 0x04;
                            }
                            else
                            {
                                timer0++;
                            }

                            //Refresh de TMR0 nos 2 bancos após atualização
                            bank_bin[0, 0x01] = timer0;
                            bank_bin[2, 0x01] = timer0;
                        }
                    }
                    #endregion

                    #region Tratamento especial endereços 0x70~0x7F espelhados entre os bancos
                    //Refresh dos endereços 0x70~0x7F
                    if ((Endereço >= 0x70) && (destino == true))   //Endereços espelhados entre bancos 0, 1, 2 e 3)
                    {
                        //Refresh do registrador nos 4 bancos
                        bank_bin[0, Endereço] = bank_bin[selBank, Endereço];
                        bank_bin[1, Endereço] = bank_bin[selBank, Endereço];
                        bank_bin[2, Endereço] = bank_bin[selBank, Endereço];
                        bank_bin[3, Endereço] = bank_bin[selBank, Endereço];
                    }
                    #endregion

                    #region Tratamento especial para PortB
                    //Refresh do PortB
                    if ((Endereço == 6) && ((selBank == 0) || (selBank == 2)) && (destino == true)) //PORTB (End.=6 em Bank0 e Bank2)
                    {
                        //Refresh do registrador nos 4 bancos
                        bank_bin[0, Endereço] = bank_bin[selBank, Endereço];
                        bank_bin[2, Endereço] = bank_bin[selBank, Endereço];

                        //Necessário para permitir que o teclado atualiza as saídas (linhas)
                        Thread.Sleep(20);
                    }
                    #endregion

                    #region Tratamento especial para TrisB
                    //Refresh do TrisB
                    if (Endereço == 6 && ((selBank == 1) || (selBank == 3)) && (destino == true))   //TRISB (End.=6 em Bank1 e Bank3)
                    {
                        //Refresh do registrador nos 4 bancos
                        bank_bin[1, Endereço] = bank_bin[selBank, Endereço];
                        bank_bin[3, Endereço] = bank_bin[selBank, Endereço];
                    }
                    #endregion

                    #region Tratamento especial para PortD com display LCD habilitado
                    //Refresh do PortB
                    if ((Endereço == 8) && (selBank == 0) && ((bank_bin[0, 5] & 0x10) != 0) && (destino == true)) //PORTD (End.=8 em Bank0)
                    {
                        //Necessário para permitir que o LCD interprete o sinal (tempo para função AtualizaLedsDisplays_Binario() rode)
                        //Thread.Sleep(20);
                        refreshLCD = false;
                        while ((!refreshLCD) && (!pararThread)) ;
                        refreshLCD = false;
                        while ((!refreshLCD) && (!pararThread)) ;
                        //Thread.Sleep(10);
                    }
                    if ((Endereço == 5) && (selBank == 0) && (destino == true)) //PORTA (End.=5 em Bank0)
                    {
                        //Necessário para permitir que o LCD interprete o sinal (tempo para função AtualizaLedsDisplays_Binario() rode)
                        //Thread.Sleep(20);
                        refreshLCD = false;
                        while ((!refreshLCD) && (!pararThread)) ;
                        refreshLCD = false;
                        while ((!refreshLCD) && (!pararThread)) ;
                        //Thread.Sleep(10);
                    }
                    #endregion

                    #region Tratamento especial para PCL (06/09/16)
                    //Refresh do PLC
                    if ((Endereço == 0x02) && (destino == true))   //Intcon (End.=02 em Bank0,1,2,3)
                    {
                        //Refresh de PLC nos 4 bancos
                        bank_bin[0, 0x02] = bank_bin[selBank, 0x02];
                        bank_bin[1, 0x02] = bank_bin[selBank, 0x02];
                        bank_bin[2, 0x02] = bank_bin[selBank, 0x02];
                        bank_bin[3, 0x02] = bank_bin[selBank, 0x02];

                        //Atualização do PC
                        //PCLATH(4..0) + PCL
                        PC = ((bank_bin[0, 0x0A] & 0x1F) << 8) + bank_bin[0, 0x02];
                    }
                    else
                    {
                        ////PCL sempre tem o valor de PC (está no começo da função para atualizar PLC antes de ser usado)
                        ////Refresh de PLC nos 4 bancos
                        //bank_bin[0, 0x02] = PC & 0xFF;
                        //bank_bin[1, 0x02] = PC & 0xFF;
                        //bank_bin[2, 0x02] = PC & 0xFF;
                        //bank_bin[3, 0x02] = PC & 0xFF;
                    }
                    #endregion

                    #region Tratamento especial para PCLATH (06/09/16)
                    //Refresh do PCLATH
                    if ((Endereço == 0x0A) && (destino == true))   //PCLATH (End.=10 em Bank0,1,2,3)
                    {
                        //Refresh de PCLATH nos 4 bancos
                        bank_bin[0, 0x0A] = bank_bin[selBank, 0x0A];
                        bank_bin[1, 0x0A] = bank_bin[selBank, 0x0A];
                        bank_bin[2, 0x0A] = bank_bin[selBank, 0x0A];
                        bank_bin[3, 0x0A] = bank_bin[selBank, 0x0A];
                    }
                    #endregion

                    #region Tratamento especial para FSR (06/09/16)
                    //Refresh do FSR
                    if ((Endereço == 0x04) && (destino == true))   //FSR (End.=4 em Bank0,1,2,3)
                    {
                        //Refresh de PCLATH nos 4 bancos
                        bank_bin[0, 0x04] = bank_bin[selBank, 0x04];
                        bank_bin[1, 0x04] = bank_bin[selBank, 0x04];
                        bank_bin[2, 0x04] = bank_bin[selBank, 0x04];
                        bank_bin[3, 0x04] = bank_bin[selBank, 0x04];
                    }
                    #endregion

                    //DONE: Tratamento de condicionais TXREG criar função para receber valor inteiro direto 
                    //Corrigir métodos da serial virtual eles trabalham apenas com strings
                    /*
                     * OK - bEnviar_Click(...) -> bEnviar_Binario_Click(...) [mudar o método de click do botão Enviar da Serial Virtual]
                     * OK - RodaThreadTx() -> RodaThreadTx_Binario()
                     * OK - RodaThreadRx() //não utilizado no programa -> RodaThreadRx_Binario()
                     * OK - ReferenciaRAM(string[, ,] bancos) -> ReferenciaRAM_Binario(string[,] bancos_bin)
                     * OK - Receber(string RX_bin) -> Receber_Binario(int RX_bin)
                     */
                    #region Tratamento especial TXREG (avisa a Serial Virtual que tem byte para ser lido)
                    if ((selBank == 0) && (Endereço == 0x19))
                    {
                        //serialVirtual.Receber_Binario(Work_bin); //Não funciona Cross Thread
                        EnviaCharUART(Work_bin);
                    }
                    #endregion

                    #region Tratamento especial para ADCON0 (início de conversão do CAD) --> Versão 3.1
                    if ((selBank == 0) && (Endereço == 0x1F))   //ADCON0 (End.=1Fh em Bank0)
                    {   /*       Bits 7     6     5    4    3    2       1 0  
                     * 1Fh ADCON0 ADCS1 ADCS0 CHS2 CHS1 CHS0 GO/DONE — ADON
                     * 
                     * CHS2 CHS1 CHS0 = 000 (AN0) ou 001 (AN1)
                     * GO/DONE = 1 (iniciar conversão) ou 0 (quando finalizar a conversão)
                     * ADON = 1 (liga periférico) ou 0 (desliga periférico)
                     * 
                     * ADCON0 =             xx00x1x1
                     * Máscara de AND =     00110101
                     * Resultado esperado = 00000101
                     */
                        if ((bank_bin[0, 0x1F] & 0x35) == 0x05) //Se o modulo está ligado e uma conversão foi iniciada nos canais AN0 ou AN1
                        {
                            /*       Bits 7    6     5 4 3     2     1     0
                             * 9Fh ADCON1 ADFM ADCS2 — — PCFG3 PCFG2 PCFG1 PCFG0
                             * 
                             * ADFM = 1 (justificado a direita) ou 0 (justificado a esquerda)
                             * PCFG3 PCFG2 PCFG1 PCFG0 = 011x (AN7-0 D), 1110 ou 1111 (AN1 D, AN0 A) e demais (AN0 e AN1 A)
                             * 
                             * ADCON1 =             xxxx011x
                             * Máscara de AND =     00001110
                             * Resultado esperado = 00000110
                             */
                            if ((bank_bin[1, 0x1F] & 0x0E) != 0x06)  //Se AN0 e/ou AN1 são analógicos
                            {
                                /* ADCON1 =             xx000xxx
                                 * Máscara de AND =     00111000
                                 * Resultado esperado = 00000000
                                 */
                                if ((bank_bin[0, 0x1F] & 0x38) == 0x00)  //Se é o canal AN0
                                {
                                    if ((bank_bin[1, 0x05] & 0x01) == 0x01)  //Se TRISA.0 é entrada
                                    {
                                        try
                                        {
                                            int step = int.Parse(tbAN0Step.Text);   //Lê a entrada analógica AN0
                                            if ((bank_bin[1, 0x1F] & 0x80) == 0x80)  //Se é justificado a direita
                                            {
                                                bank_bin[0, 0x1E] = step >> 8;      //ADRESH = 2 msb
                                                bank_bin[1, 0x1E] = step & 0x0FF;   //ADRESL = 8 lsb
                                            }
                                            else //Senão, é justificado a esquerda
                                            {
                                                bank_bin[0, 0x1E] = step >> 2;              //ADRESH = 8 msb
                                                bank_bin[1, 0x1E] = (step & 0x003) << 6;    //ADRESL = 2 lsb
                                            }

                                            bank_bin[0, 0x1F] &= 0xFB;  //GO/DONE = 0
                                            bank_bin[0, 0x0C] |= 0x20;  //PIR1.6 = ADIF = 1

                                            //dgvBank0.Rows[0x0C].Cells[2].Value = IntToBinString(bank_bin[0, 0x0C]); //Atualiza o valor da célula PIR1
                                            //dgvBank0.Rows[0x1F].Cells[2].Value = IntToBinString(bank_bin[0, 0x1F]); //Atualiza o valor da célula ADCON0
                                            //dgvBank0.Rows[0x1E].Cells[2].Value = IntToBinString(bank_bin[0, 0x1E]); //Atualiza o valor da célula ADRESH
                                            //dgvBank1.Rows[0x1E].Cells[2].Value = IntToBinString(bank_bin[1, 0x1E]); //Atualiza o valor da célula ADRESL
                                        }
                                        catch (Exception)
                                        {
                                            MessageBox.Show("ERRO CAD");
                                        }
                                    }
                                }
                                /* ADCON0 =             xx001xxx
                                 * Máscara de AND =     00111000
                                 * Resultado esperado = 00001000
                                 */
                                else if ((bank_bin[0, 0x1F] & 0x38) == 0x08)  //Se é o canal AN1
                                {
                                    /* ADCON1 =             xxxx111x
                                     * Máscara de AND =     00001110
                                     * Resultado esperado = 00001110
                                     */
                                    if ((bank_bin[1, 0x1F] & 0x0E) != 0x0E)  //Se AN1 é analógico
                                    {
                                        if ((bank_bin[1, 0x05] & 0x02) == 0x02)  //Se TRISA.1 é entrada
                                        {
                                            try
                                            {
                                                int step = int.Parse(tbAN1Step.Text);   //Lê a entrada analógica AN1
                                                if ((bank_bin[1, 0x1F] & 0x80) == 0x80)  //Se é justificado a direita
                                                {
                                                    bank_bin[0, 0x1E] = step >> 8;      //ADRESH = 2 msb
                                                    bank_bin[1, 0x1E] = step & 0x0FF;   //ADRESL = 8 lsb
                                                }
                                                else //Senão, é justificado a esquerda
                                                {
                                                    bank_bin[0, 0x1E] = step >> 2;              //ADRESH = 8 msb
                                                    bank_bin[1, 0x1E] = (step & 0x003) << 6;    //ADRESL = 2 lsb
                                                }

                                                bank_bin[0, 0x1F] &= 0xFB;  //GO/DONE = 0
                                                bank_bin[0, 0x0C] |= 0x20;  //PIR1.6 = ADIF = 1

                                                //dgvBank0.Rows[0x0C].Cells[2].Value = IntToBinString(bank_bin[0, 0x0C]); //Atualiza o valor da célula PIR1
                                                //dgvBank0.Rows[0x1F].Cells[2].Value = IntToBinString(bank_bin[0, 0x1F]); //Atualiza o valor da célula ADCON0
                                                //dgvBank0.Rows[0x1E].Cells[2].Value = IntToBinString(bank_bin[0, 0x1E]); //Atualiza o valor da célula ADRESH
                                                //dgvBank1.Rows[0x1E].Cells[2].Value = IntToBinString(bank_bin[1, 0x1E]); //Atualiza o valor da célula ADRESL
                                            }
                                            catch (Exception)
                                            {
                                                MessageBox.Show("ERRO CAD");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    #endregion

                    #region Tratamento especial para Status e Refresh do Status (deve ser o último a ser atualizado)
                    if ((Endereço == 0x03) && (destino == true))
                    {
                        //Sinaliza necessidade de atualização de registrador
                        Status_bin = bank_bin[selBank, 3];
                        refreshStatus = true;
                    }
                    if (refreshStatus)  //Deve ser o último a ser atualizado
                    {
                        //Refresh de STATUS nos 4 bancos
                        bank_bin[0, 0x03] = Status_bin;
                        bank_bin[1, 0x03] = Status_bin;
                        bank_bin[2, 0x03] = Status_bin;
                        bank_bin[3, 0x03] = Status_bin;

                        //Atualiza banco selecionado
                        selBank = (bank_bin[selBank, 0x03] & 0x60) >> 5;

                        refreshStatus = false;
                    }
                    #endregion

                    #region Tratamento especial para INDF (06/09/16)
                    //Refresh do banco selecionado
                    if (indf)   //INDF (End.=0 em Bank0,1,2,3)
                    {
                        //Atualiza banco selecionado
                        selBank = (bank_bin[0, 0x03] & 0x60) >> 5;
                        //Isso foi feito supondo que as instruções indiretas são minoria,
                        //nestes casos o banco selecionado é redefinido antes das instruções
                        //com base nos valores de Status,IRP + FSR,7 e após todo tratamento
                        //é defefinido como Status,RP1 + Status,RP0 
                    }
                    #endregion

                    #region Teste de Interrupções (08/09/16)
                    //Se está em condição de interrupção de acordo com o registrador INTCON (0Bh)
                    //Timer0    --> GIE(7)=TMR0IE(5)=TMR0IF(2)=1 --> Máscara A4h
                    //ou
                    //INT (RB0) --> GIE(7)=INTE(4)=INTF(1)=1     --> Máscara 92h

                    if (((bank_bin[0, 0x0B] & 0xA4) == 0xA4) || ((bank_bin[0, 0x0B] & 0x92) == 0x92))
                    {
                        //Se a pilha esta cheia remove o último registro
                        if (picStack.Count >= 8) picStack.Pop();
                        //Insere o valor do program counter na pilha
                        picStack.Push(PC);

                        //Reseta o bit GIE do registrador INTCON
                        bank_bin[selBank, 0x0B] &= ~0x80;
                        //Refresh de INTCON nos 4 bancos
                        bank_bin[0, 0x0B] = bank_bin[selBank, 0x0B];
                        bank_bin[1, 0x0B] = bank_bin[selBank, 0x0B];
                        bank_bin[2, 0x0B] = bank_bin[selBank, 0x0B];
                        bank_bin[3, 0x0B] = bank_bin[selBank, 0x0B];

                        //Atualiza program counter com o valor de desvio para tratamento de interrupções (ISRs)
                        PC = 0x04;
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                clock.Enabled = false;
                MessageBox.Show("Erro ao executar uma instrução.\n" + ex.Message, "Thread ERRO",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                fazerReset = true;
            }
            threadRun.Abort();
        }
        /// <summary>
        /// Faz o refresh dos periféricos
        /// </summary>
        private void Clock_Run_Refresh()
        {
            TestaBotoesTeclado_Binario();       //Testa o estado dos botões e o teclado matricial (PORTB)
            AtualizaLedsDisplays_Binario();     //Atualiza o estado dos leds e displays de 7 segmentos (PORTD)
            refreshLCD = true;
        }
        /// <summary>
        /// Envia um caractere para uma outra Thread
        /// </summary>
        /// <param name="caractere"></param>
        void EnviaCharUART(int caractere)
        {
            if (this.serialVirtual.InvokeRequired) //cruzamento de threds
            {
                //Instancia o delegate do tipo PegaChar
                PegaChar d = new PegaChar(EnviaCharUART);
                this.Invoke(d, new object[] { caractere });
            }
            else
            {
                serialVirtual.Receber_Binario(caractere);
            }
        }
        #endregion
        #endregion

        //---------------------------------------------------------------------
        #region DEBUG LCD (habilitar a diretiva #define DebugLCD somente para debug)
#if DebugLCD
        bool E = false; //Enable do LCD
        bool cursor = true; //Cursor do LCD
        bool nibble1 = false;   //Nibble mais signigficativo do modo 4 bits
        ushort nibbleH;
        int[] caractereSel = new int[] { 0, 0 };    //Coluna,Linha
        int colunas = 16;
        int linhas2 = 2;
        Label[,] caractere = new Label[2, 16];

        public bool EnviaComandoOuDado_DEBUG(ushort valor)
        {
            //Formato do dado:
            //15..11    10  9   8   7   ..  0
            // x..x     RS  RW  E   DB7 ..  DB0
            //RS = 0 --> Comando
            //RS = 1 --> Dado
            //RW = 0 --> Escreve
            //RW = 1 --> Lê
            //E = 0 = 1 = 0 --> Pulso de escrita no LCD

            tbDebug.Visible = true;

            int rs = (valor & 0x0400) >> 10;
            int rw = (valor & 0x0200) >> 9;
            int e = (valor & 0x0100) >> 8;
            int d = (valor & 0x00F0) >> 4;

            tbDebug.AppendText("RS=" + rs.ToString() + " RW=" + rw.ToString() + " E=" + e.ToString() + " D= " + Convert.ToString(d, 16) + Environment.NewLine);

            //tbDebug.AppendText("CRU: B>" + Convert.ToString(valor,2) + " H>" + Convert.ToString(valor,16) + Environment.NewLine);

            bool status = false;

            ////Se está em modo de 8 bits de dados
            //if (modoFuncionamento == modo._8bits)
            //{
            //    //Se ocorreu uma borda de descida no Enable (E)
            //    if ((E == true) && ((valor & 0x0100) == 0))
            //    {
            //        //Se for escrita (RW=0)
            //        if ((valor & 0x0200) == 0)
            //        {
            //            //Se for comando (RS=0)
            //            if ((valor & 0x0400) == 0)
            //            {
            //                //Trata comando recebido
            //                status = TrataComando((byte)(valor & 0x00FF));
            //            }
            //            //Se for dado (RS=0)
            //            else
            //            {
            //                //Cverte o valor em um caractere
            //                char dado = Convert.ToChar(valor & 0x00FF);
            //                //Escreve o caractere no LCD
            //                status = EscreveCaractere(dado);
            //                //Se é uma coluna e uma linha válidas
            //                if (caractereSel[0] >= 0 && caractereSel[0] < colunas &&
            //                    caractereSel[1] >= 0 && caractereSel[1] < linhas)
            //                {
            //                    //Remove o efeito do cursor neste caractere
            //                    caractere[caractereSel[1], caractereSel[0]].BackColor = Color.Transparent;
            //                }
            //                //Faz o deslocamento automático
            //                caractereSel[0]++;
            //            }
            //        }
            //    }
            //}
            ////Se está em modo de 4 bits de dados (2 nibbles em DB7..DB4 - 1o DB7..DB4 - 2o DB3..DB0)
            //else
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
                                //status = TrataComando((byte)(valor & 0x00FF));
                                tbDebug.AppendText("Comando: " + Convert.ToString((valor & 0x00FF), 16) + Environment.NewLine);
                                status = true;
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
                                //status = EscreveCaractere(dado);
                                tbDebug.AppendText("Dado: C" + caractereSel[0].ToString() + " L" + caractereSel[1].ToString() + " '" + dado.ToString() + "' " + Convert.ToString((valor & 0x00FF), 16) + Environment.NewLine);
                                status = true;
                                //Se é uma coluna e uma linha válidas
                                if (caractereSel[0] >= 0 && caractereSel[0] < colunas &&
                                    caractereSel[1] >= 0 && caractereSel[1] < linhas2)
                                {
                                    //Remove o efeito do cursor neste caractere
                                    //caractere[caractereSel[1], caractereSel[0]].BackColor = Color.Transparent;
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
#endif
        #endregion
    }
}


// MAPA DOS VETORES

// VETOR DE CARACTER
// VALUEARRAY = [0,1,2,3,4,5,6,7]
//               | | | | | | | |
// PORT =      [7,6,5,4,3,2,1,0]

//LEGENDA       VETOR   PORT
//COLUNA1 =     [0]      [7]
//COLUNA2 =     [1]      [6]
//COLUNA3 =     [2]      [5]

//LINHA1 =      [3]      [4]
//LINHA2 =      [4]      [3]
//LINHA3 =      [5]      [2]

//RB0 =         [6]      [1]
//RB1 =         [7]      [0]

// BOTÕES 1,2,3 DO TECLADO MATRICIAL IMITANDO LÓGICA ANALOGICA. "AO APERTAR O BOTÃO, LINHA TEM MESMO SINAL DA COLUNA, 1 PARA 0"