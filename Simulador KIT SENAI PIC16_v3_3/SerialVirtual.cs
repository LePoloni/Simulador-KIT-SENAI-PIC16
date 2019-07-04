using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//Incluido manualmente
using System.Collections;
using System.Threading;

namespace Simulador_KIT_SENAI_PIC16_v3_3
{

    public partial class SerialVirtual : UserControl
    {
        #region Método construtor----------------------------------------------
        /// <summary>
        /// Método construtor.
        /// </summary>
        public SerialVirtual()
        {
            InitializeComponent();
            CarregaBaudRate();
            reference = false;
            QueueTxSize = 20;
            baudrate = -1;
        }
        #endregion
        //---------------------------------------------------------------------
        #region Variáveis globais----------------------------------------------
        string[, ,] banks;  //Banco,Endereço,Campo (dado no campo 2)
        bool reference;
        Queue QueueTx;      //O PIC18F877A possui uma FIFO de dois níveis, o terceiro byte é perdido quando a FIFO está cheia
        int QueueTxSize;
        Thread ThreadTx;
        Thread ThreadRx;
        int baudrate;

        int[,] banks_bin;  //Banco,Endereço
        bool ModoBinario = false;
        #endregion
        //---------------------------------------------------------------------
        #region Delegates e Eventos--------------------------------------------
        //Cria um Delegate
        public delegate void EventoSerial(int[] registrador_alterado);
        //Cria um Evento
        /// <summary>
        /// Gerado a cada byte enviado pela Serial Virtual. Informa o 0-Banco e 1-Endereço.
        /// </summary>
        public event EventoSerial EventoSerialTx;   //Usado quando a Serial Virtual enviar um dado para o PIC
        /// <summary>
        /// Gerado a cada byte enviado para Serial Virtual.
        /// </summary>
        public event EventoSerial EventoSerialRx;   //Usado quando a Serial Virtual precisar receber um dado do PIC
        #endregion
        //---------------------------------------------------------------------
        #region Propriedades---------------------------------------------------
        public string Título
        {
            get { return gbSerialVirtual.Text; }
            set { gbSerialVirtual.Text = value; }
        }
        #endregion
        //---------------------------------------------------------------------
        #region Métodos privados-----------------------------------------------
        /// <summary>
        /// Lista as opções de baudrate
        /// </summary>
        void CarregaBaudRate()
        {
            Int32[] BaudRateValores ={ 
                                     100,300,600,1200,2400,4800,9600,14400,19200,
                                     38400,56000,57600,115200,128000,256000
                                     };

            for (int i = 0; i < BaudRateValores.Length; i++)
            {
                cbBaudRate.Items.Add(BaudRateValores[i].ToString());
            }
            cbBaudRate.SelectedIndex = 6; //9600bps
        }
        /// <summary>
        /// Conecta a porta Serial Virtual à UART do PIC.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bConectar_Click(object sender, EventArgs e)
        {
            if (reference)
            {
                if (bConectar.Text == "Conectar")
                {
                    bConectar.Text = "Desconectar";
                    //Define a profundidade do stack e limpa ele
                    QueueTx = new Queue(QueueTxSize);
                    QueueTx.Clear();
                    //Habilita o botão enviar
                    bEnviar.Enabled = true;
                    //Armazena o baud rate selecionado
                    baudrate = int.Parse(cbBaudRate.SelectedItem.ToString());
                    //Define o método executada pela thread
                    //ThreadRx = new Thread(new ThreadStart(RodaThreadRx));
                    //Inicia a thread (acho que essa thread não é necessária)
                    //ThreadRx.Start();
                }
                else
                {
                    bConectar.Text = "Conectar";
                    //Desabilita o botão enviar
                    bEnviar.Enabled = false;
                    //Para as threads em execusão
                    //if (ThreadTx.IsAlive)
                    if (ThreadTx != null)
                        ThreadTx.Abort();
                    //if (ThreadRx.IsAlive)
                    if (ThreadRx != null)
                        ThreadRx.Abort();
                }
            }
        }
        /// <summary>
        /// Envia uma string para o PIC18F877A.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bEnviar_Click(object sender, EventArgs e)
        {
            //Se está no ModoString
            if (!ModoBinario)
            {
                //Se existe texto para enviar
                if (tbTX.TextLength > 0)
                {
                    //Analiza os caracteres e faz sua conversão
                    for (int i = 0; i < tbTX.TextLength; i++)
                    {
                        //Se a fila não está cheia
                        if (QueueTx.Count < QueueTxSize)
                        {
                            //Transforma o texto em um array de caracteres
                            char[] array = tbTX.Text.ToCharArray();
                            //Converte para binário
                            string valor_bin = Convert.ToString(array[i], 2);
                            while (valor_bin.Length < 8)
                                valor_bin = "0" + valor_bin;
                            //Coloca a string na fila
                            QueueTx.Enqueue(valor_bin);
                        }
                    }
                    //Verifica se deve enviar o Enter ao final
                    if (cbEnter.Checked)
                    {
                        //Se a fila não está cheia
                        if (QueueTx.Count < QueueTxSize) QueueTx.Enqueue("00001101");   //0x0D - Carrier Return
                        if (QueueTx.Count < QueueTxSize) QueueTx.Enqueue("00001010");   //0x0A - Line Feed
                    }

                    //Dispara um processo para gerir o stack e comunicar a UART do PIC
                    if (QueueTx.Count > 0)
                    {
                        //Define o método executada pela thread
                        ThreadTx = new Thread(new ThreadStart(RodaThreadTx));
                        //Inicia a thread
                        ThreadTx.Start();
                    }

                    //Se a chechbox cbLimpar está marcado, limpa o texto (evita cross thread)
                    if (cbLimpar.Checked)
                        tbTX.Clear();
                }
            }
            //Se está no ModoBinario
            else
            {
                //Se existe texto para enviar
                if (tbTX.TextLength > 0)
                {
                    //Transforma o texto em um array de caracteres
                    char[] array = tbTX.Text.ToCharArray();

                    //Analiza os caracteres
                    for (int i = 0; i < tbTX.TextLength; i++)
                    {
                        //Se a fila não está cheia
                        if (QueueTx.Count < QueueTxSize)
                        {
                            //Coloca a string na fila na forma de caracteres
                            QueueTx.Enqueue((byte)array[i]);
                        }
                    }
                    //Verifica se deve enviar o Enter ao final
                    if (cbEnter.Checked)
                    {
                        //Se a fila não está cheia
                        if (QueueTx.Count < QueueTxSize) QueueTx.Enqueue((byte)0x0D);   //0x0D - Carrier Return
                        if (QueueTx.Count < QueueTxSize) QueueTx.Enqueue((byte)0x0A);   //0x0A - Line Feed
                    }

                    //Dispara um processo para gerir o stack e comunicar a UART do PIC
                    if (QueueTx.Count > 0)
                    {
                        //Define o método executada pela thread
                        ThreadTx = new Thread(new ThreadStart(RodaThreadTx_Binario));
                        //Inicia a thread
                        ThreadTx.Start();
                    }

                    //Se a chechbox cbLimpar está marcado, limpa o texto (evita cross thread)
                    if (cbLimpar.Checked)
                        tbTX.Clear();
                }
            }
        }
        /// <summary>
        /// Envia uma string para o PIC18F877A usado pela thread ThreadTx.
        /// </summary>
        private void RodaThreadTx()
        {
            //banks = Banco[4],Endereço[128],Campo[3] = Address,Register,Value

            //Verifica se a porta está habilitada (RCSTA.7 = SPEN = 1, BANK0 18h)
            if (banks[0, 0x18, 2].Substring(0, 1) != "1") { MessageBox.Show("Porta serial do PIC não está habilitada (RCSTA.7 = SPEN = 0, BANK0 18h)"); ThreadTx.Abort(); }
            //Verifica se RX está habilitado (RCSTA.4 = CREN = 1, BANK0 18h)
            if (banks[0, 0x18, 2].Substring(3, 1) != "1") { MessageBox.Show("Recepção serial do PIC não está habilitado (RCSTA.4 = CREN = 0, BANK0 18h)"); ThreadTx.Abort(); }
            //Verifica se é modo assíncrono (TXSTA.4 = SYNC = 0, BANK1 18h)
            if (banks[1, 0x18, 2].Substring(3, 1) != "0") { MessageBox.Show("Porta serial não está em modo assíncrono (TXSTA.4 = SYNC = 1, BANK1 18h)"); ThreadTx.Abort(); }
            //Verifica o baud rate (SPBRG, PORT1 19h e TXSTA.2 = BRGH = 0/1, BANK1 18h)
            if (banks[1, 0x18, 2].Substring(5, 1) == "0")
            {
                string br = banks[1, 0x19, 2];
                if ((baudrate == 300) && (banks[1, 0x19, 2] == "11001111")) { }         //207
                else if ((baudrate == 1200) && (banks[1, 0x19, 2] == "00110011")) { }   //51
                else if ((baudrate == 2400) && (banks[1, 0x19, 2] == "00011001")) { }   //25
                else if ((baudrate == 9600) && (banks[1, 0x19, 2] == "00000110")) { }   //6
                else if ((baudrate == 19200) && (banks[1, 0x19, 2] == "00000010")) { }  //2
                else if ((baudrate == 57600) && (banks[1, 0x19, 2] == "00000000")) { }  //0
                else
                {
                    //Erro de Over Run (RCSTA.1 = OERR = 1, BANK0 18h)
                    banks[1, 0x18, 2] = banks[1, 0x18, 2].Substring(0, 6) + "1" + banks[1, 0x18, 2].Substring(7, 1);

                    //Dispara evento de SerialVirtual
                    //Usei try..catch por conta do método de tramento depender do usuário
                    //Passe o registrador que foi alterado (Bank, Address)
                    int[] reg = { 1, 0x18 };
                    try { EventoSerialTx(reg); }
                    catch { }

                    MessageBox.Show("O baud rate do PIC não é compatível com o baud rate da serial virtual (SPBRG, PORT1 19h e TXSTA.2 = BRGH = 0, BANK1 18h)");

                    //Aborta a thread
                    ThreadTx.Abort();
                }
            }
            else
            {
                if ((baudrate == 1200) && (banks[1, 0x19, 2] == "11001111")) { }        //207
                else if ((baudrate == 2400) && (banks[1, 0x19, 2] == "01010111")) { }   //103
                else if ((baudrate == 9600) && (banks[1, 0x19, 2] == "00011001")) { }   //25
                else if ((baudrate == 19200) && (banks[1, 0x19, 2] == "00001100")) { }  //12
                else if ((baudrate == 57600) && (banks[1, 0x19, 2] == "00000011")) { }  //3
                else
                {
                    //Erro de Over Run (RCSTA.1 = OERR = 1, BANK0 18h)
                    banks[1, 0x18, 2] = banks[1, 0x18, 2].Substring(0, 6) + "1" + banks[1, 0x18, 2].Substring(7, 1);
                    //Dispara evento de SerialVirtual
                    int[] reg = { 1, 0x18 };
                    try { EventoSerialTx(reg); }
                    catch { }

                    MessageBox.Show("O baud rate do PIC não é compatível com o baud rate da serial virtual (SPBRG, PORT1 19h e TXSTA.2 = BRGH = 1, BANK1 18h)");

                    //Aborta a thread
                    ThreadTx.Abort();
                }
            }
            //Envia os bytes do stack para o registrador RCREG do PIC (BANK0 1Ah)
            while (QueueTx.Count > 0)
            {
                //Se o registrador esta limpo
                if (banks[0, 0x1A, 2] == "00000000")
                {
                    //Escreve um byte no registrador RCREG (BANK0 1Ah)
                    banks[0, 0x1A, 2] = (string)QueueTx.Dequeue();
                    //Dispara evento de SerialVirtual
                    int[] reg = { 0, 0x1A };
                    try { EventoSerialTx(reg); }
                    catch { }

                    //Escreve no bit RCIF no registrador (PIR1.5 = RCIF, BANK0 0Ch)
                    //Erro de Over Run (PIR1.5 = RCIF = 1, BANK0 0Ch)
                    banks[0, 0x0C, 2] = banks[0, 0x0C, 2].Substring(0, 2) + "1" + banks[0, 0x1C, 2].Substring(3, 5);
                    //Dispara evento de SerialVirtual
                    int[] reg2 = { 0, 0x0C };
                    try { EventoSerialTx(reg2); }
                    catch { }
                }
            }
            //Se a chechbox cbLimpar está marcado, limpa o texto (cross thread)
            //if (cbLimpar.Checked)
            //    tbTX.Clear(); 

            ThreadTx.Abort();   //Após o laço encerro a thread
        }
        /// <summary>
        /// Envia uma string para o PIC18F877A usado pela thread ThreadTx baseada em valores inteiros (char).
        /// </summary>
        private void RodaThreadTx_Binario()
        {
            //banks_bin = Banco[4],Endereço[128]

            //Verifica se a porta está habilitada (RCSTA.7 = SPEN = 1, BANK0 18h)
            if ((banks_bin[0, 0x18] & 0x80) == 0) { MessageBox.Show("Porta serial do PIC não está habilitada (RCSTA.7 = SPEN = 0, BANK0 18h)"); ThreadTx.Abort(); }
            //Verifica se RX está habilitado (RCSTA.4 = CREN = 1, BANK0 18h)
            if ((banks_bin[0, 0x18] & 0x10) == 0) { MessageBox.Show("Recepção serial do PIC não está habilitado (RCSTA.4 = CREN = 0, BANK0 18h)"); ThreadTx.Abort(); }
            //Verifica se é modo assíncrono (TXSTA.4 = SYNC = 0, BANK1 18h)
            if ((banks_bin[1, 0x18] & 0x80) != 0) { MessageBox.Show("Porta serial não está em modo assíncrono (TXSTA.4 = SYNC = 1, BANK1 18h)"); ThreadTx.Abort(); }
            //Verifica o baud rate (SPBRG, PORT1 19h e TXSTA.2 = BRGH = 0/1, BANK1 18h)
            if ((banks_bin[1, 0x18] & 0x04) == 0)
            {
                int br = banks_bin[1, 0x19];
                if ((baudrate == 300) && (br == 207)) { }       //207
                else if ((baudrate == 1200) && (br == 51)) { }  //51
                else if ((baudrate == 2400) && (br == 25)) { }  //25
                else if ((baudrate == 9600) && (br == 6)) { }   //6
                else if ((baudrate == 19200) && (br == 2)) { }  //2
                else if ((baudrate == 57600) && (br == 0)) { }  //0
                else
                {
                    //Erro de Over Run (RCSTA.1 = OERR = 1, BANK0 18h)
                    banks_bin[1, 0x18] |= 0x02;

                    //Dispara evento de SerialVirtual
                    //Usei try..catch por conta do método de tramento depender do usuário
                    //Passe o registrador que foi alterado (Bank, Address)
                    int[] reg = { 1, 0x18 };
                    try { EventoSerialTx(reg); }
                    catch { }

                    MessageBox.Show("O baud rate do PIC não é compatível com o baud rate da serial virtual (SPBRG, PORT1 19h e TXSTA.2 = BRGH = 0, BANK1 18h)");

                    //Aborta a thread
                    ThreadTx.Abort();
                }
            }
            else
            {
                int br = banks_bin[1, 0x19];
                if ((baudrate == 1200) && (br == 207)) { }      //207
                else if ((baudrate == 2400) && (br == 103)) { } //103
                else if ((baudrate == 9600) && (br == 25)) { }  //25
                else if ((baudrate == 19200) && (br == 12)) { } //12
                else if ((baudrate == 57600) && (br == 3)) { }  //3
                else
                {
                    //Erro de Over Run (RCSTA.1 = OERR = 1, BANK0 18h)
                    banks_bin[1, 0x18] |= 0x02;

                    //Dispara evento de SerialVirtual
                    int[] reg = { 1, 0x18 };
                    try { EventoSerialTx(reg); }
                    catch { }

                    MessageBox.Show("O baud rate do PIC não é compatível com o baud rate da serial virtual (SPBRG, PORT1 19h e TXSTA.2 = BRGH = 1, BANK1 18h)");

                    //Aborta a thread
                    ThreadTx.Abort();
                }
            }
            //Envia os bytes do stack para o registrador RCREG do PIC (BANK0 1Ah)
            while (QueueTx.Count > 0)
            {
                //Se o registrador esta limpo
                if (banks_bin[0, 0x1A] == 0)
                {
                    //Escreve um byte no registrador RCREG (BANK0 1Ah)
                    banks_bin[0, 0x1A] = (byte)QueueTx.Dequeue();
                    //Dispara evento de SerialVirtual
                    int[] reg = { 0, 0x1A };
                    try { EventoSerialTx(reg); }
                    catch { }

                    //Escreve no bit RCIF no registrador (PIR1.5 = RCIF, BANK0 0Ch)
                    //Erro de Over Run (PIR1.5 = RCIF = 1, BANK0 0Ch)
                    banks_bin[0, 0x0C] |= 0x20;
                    //Dispara evento de SerialVirtual
                    int[] reg2 = { 0, 0x0C };
                    try { EventoSerialTx(reg2); }
                    catch { }
                }
            }
            //Se a chechbox cbLimpar está marcado, limpa o texto (cross thread)
            //if (cbLimpar.Checked)
            //    tbTX.Clear(); 

            ThreadTx.Abort();   //Após o laço encerro a thread
        }
        /// <summary>
        /// Monitora porta serial do PIC.
        /// </summary>
        private void RodaThreadRx()
        {
            //banks = Banco[4],Endereço[128],Campo[3] = Address,Register,Value

            //Verifica se a porta está habilitada (RCSTA.7 = SPEN = 1, BANK0 18h)
            if (banks[0, 0x18, 2].Substring(0, 1) != "1") ThreadTx.Abort();
            //Verifica se TX está habilitado (TXSTA.5 = TXEN = 1, BANK1 18h)
            if (banks[1, 0x18, 2].Substring(2, 1) != "1") ThreadTx.Abort();
            //Verifica se é modo assíncrono (TXSTA.4 = SYNC = 0, BANK1 18h)
            if (banks[1, 0x18, 2].Substring(3, 1) != "0") ThreadTx.Abort();
            //Verifica o baud rate (SPBRG, PORT1 19h e TXSTA.2 = BRGH = 0/1, BANK1 18h)
            if (banks[1, 0x18, 2].Substring(5, 1) == "0")
            {
                if ((baudrate == 300) && (banks[1, 0x19, 2] == "11001111")) { }         //207
                else if ((baudrate == 1200) && (banks[1, 0x19, 2] == "00110011")) { }   //51
                else if ((baudrate == 2400) && (banks[1, 0x19, 2] == "00011001")) { }   //25
                else if ((baudrate == 9600) && (banks[1, 0x19, 2] == "00000110")) { }   //6
                else if ((baudrate == 19200) && (banks[1, 0x19, 2] == "00000010")) { }  //2
                else if ((baudrate == 57600) && (banks[1, 0x19, 2] == "00000000")) { }  //0
                else
                {
                    //Aborta a thread
                    ThreadRx.Abort();
                }
            }
            else
            {
                if ((baudrate == 1200) && (banks[1, 0x19, 2] == "11001111")) { }        //207
                else if ((baudrate == 2400) && (banks[1, 0x19, 2] == "01010111")) { }   //103
                else if ((baudrate == 9600) && (banks[1, 0x19, 2] == "00011001")) { }   //25
                else if ((baudrate == 19200) && (banks[1, 0x19, 2] == "00001100")) { }  //12
                else if ((baudrate == 57600) && (banks[1, 0x19, 2] == "00000011")) { }  //3
                else
                {
                    //Aborta a thread
                    ThreadRx.Abort();
                }
            }
            while (true)
            {
                //Acho que essa thread é desnecessária!!!!
            }
        }
        /// <summary>
        /// Monitora porta serial do PIC.
        /// </summary>
        private void RodaThreadRx_Binario()
        {
            //banks_bin = Banco[4],Endereço[128]

            //Verifica se a porta está habilitada (RCSTA.7 = SPEN = 1, BANK0 18h)
            if ((banks_bin[0, 0x18] & 0x80) == 0) { ThreadTx.Abort(); }
            //Verifica se TX está habilitado (TXSTA.5 = TXEN = 1, BANK1 18h)
            if ((banks_bin[1, 0x18] & 0x20) == 0) { ThreadTx.Abort(); }
            //Verifica se é modo assíncrono (TXSTA.4 = SYNC = 0, BANK1 18h)
            if ((banks_bin[1, 0x18] & 0x10) != 0) { ThreadTx.Abort(); }
            //Verifica o baud rate (SPBRG, PORT1 19h e TXSTA.2 = BRGH = 0/1, BANK1 18h)
            if ((banks_bin[1, 0x18] & 0x04) == 0)
            {
                int br = banks_bin[1, 0x19];
                if ((baudrate == 300) && (br == 207)) { }       //207
                else if ((baudrate == 1200) && (br == 51)) { }  //51
                else if ((baudrate == 2400) && (br == 25)) { }  //25
                else if ((baudrate == 9600) && (br == 6)) { }   //6
                else if ((baudrate == 19200) && (br == 2)) { }  //2
                else if ((baudrate == 57600) && (br == 0)) { }  //0
                else
                {
                    //Aborta a thread
                    ThreadRx.Abort();
                }
            }
            else
            {
                int br = banks_bin[1, 0x19];
                if ((baudrate == 1200) && (br == 207)) { }      //207
                else if ((baudrate == 2400) && (br == 103)) { } //103
                else if ((baudrate == 9600) && (br == 25)) { }  //25
                else if ((baudrate == 19200) && (br == 12)) { } //12
                else if ((baudrate == 57600) && (br == 3)) { }  //3
                else
                {
                    //Aborta a thread
                    ThreadRx.Abort();
                }
            }
            while (true)
            {
                //Acho que essa thread é desnecessária!!!!
            }
        }
        /// <summary>
        /// Limpa o textBox tbRX
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bLimpar_Click(object sender, EventArgs e)
        {
            tbRX.Clear();
        }
        #endregion
        //---------------------------------------------------------------------
        #region Métodos públicos-----------------------------------------------
        /// <summary>
        /// Passagem de referência da memória RAM.
        /// </summary>
        /// <param name="bancos">Matriz com a memória RAM dividida em 4 bancos de registrads.</param>
        /// <returns>Se a referência é valida ou não.</returns>
        public bool ReferenciaRAM(string[, ,] bancos)
        {
            if (bancos.Length == 4 * 128 * 3)   //Banco,Endereço,Campo
            {
                //Copia a referência da matriz com a memória RAM
                banks = bancos;
                //Sinaliza que existe uma referência válida para a memória RAM
                reference = true;
                //Habilita controles
                gbSerialVirtual.Enabled = true;
                bConectar.Enabled = true;
                //Sinaliza ModoBinario
                ModoBinario = false;

                return true;
            }
            else
            {
                //Sinaliza que não existe uma referência válida para a memória RAM
                reference = false;
                //Desabilita controles
                gbSerialVirtual.Enabled = false;
                bConectar.Enabled = false;
                bEnviar.Enabled = false;

                return false;
            }
        }
        /// <summary>
        /// Passagem de referência da memória RAM com valores binários.
        /// </summary>
        /// <param name="bancos">Matriz com a memória RAM dividida em 4 bancos de registrads.</param>
        /// <returns>Se a referência é valida ou não.</returns>
        public bool ReferenciaRAM_Binario(int[,] bancos_bin)
        {
            if (bancos_bin.Length == 4 * 128)   //Banco, Endereço
            {
                //Copia a referência da matriz com a memória RAM
                banks_bin = bancos_bin;
                //Sinaliza que existe uma referência válida para a memória RAM
                reference = true;
                //Habilita controles
                gbSerialVirtual.Enabled = true;
                bConectar.Enabled = true;
                //Sinaliza ModoBinario
                ModoBinario = true;

                return true;
            }
            else
            {
                //Sinaliza que não existe uma referência válida para a memória RAM
                reference = false;
                //Desabilita controles
                gbSerialVirtual.Enabled = false;
                bConectar.Enabled = false;
                bEnviar.Enabled = false;

                return false;
            }
        }
        /// <summary>
        /// Dispara recepção da Serial Virtual do byte enviado.
        /// </summary>
        /// <param name="RX_bin">String binária com o valor enviado para Serial Virtual.</param>
        /// <returns>Verdadeiro se pode receber e não há erro nas configurações.</returns>
        public bool Receber(string RX_bin)
        {
            //banks = Banco[4],Endereço[128],Campo[3] = Address,Register,Value

            //Verifica se a porta está habilitada (RCSTA.7 = SPEN = 1, BANK0 18h)
            if (banks[0, 0x18, 2].Substring(0, 1) != "1") return false;
            //Verifica se TX está habilitado (TXSTA.5 = TXEN = 1, BANK1 18h)
            if (banks[1, 0x18, 2].Substring(2, 1) != "1") return false;
            //Verifica se é modo assíncrono (TXSTA.4 = SYNC = 0, BANK1 18h)
            if (banks[1, 0x18, 2].Substring(3, 1) != "0") return false;
            //Verifica o baud rate (SPBRG, PORT1 19h e TXSTA.2 = BRGH = 0/1, BANK1 18h)
            if (banks[1, 0x18, 2].Substring(5, 1) == "0")
            {
                if ((baudrate == 300) && (banks[1, 0x19, 2] == "11001111")) { }         //207
                else if ((baudrate == 1200) && (banks[1, 0x19, 2] == "00110011")) { }   //51
                else if ((baudrate == 2400) && (banks[1, 0x19, 2] == "00011001")) { }   //25
                else if ((baudrate == 9600) && (banks[1, 0x19, 2] == "00000110")) { }   //6
                else if ((baudrate == 19200) && (banks[1, 0x19, 2] == "00000010")) { }  //2
                else if ((baudrate == 57600) && (banks[1, 0x19, 2] == "00000000")) { }  //0
                else
                {
                    return false;
                }
            }
            else
            {
                if ((baudrate == 1200) && (banks[1, 0x19, 2] == "11001111")) { }        //207
                else if ((baudrate == 2400) && (banks[1, 0x19, 2] == "01010111")) { }   //103
                else if ((baudrate == 9600) && (banks[1, 0x19, 2] == "00011001")) { }   //25
                else if ((baudrate == 19200) && (banks[1, 0x19, 2] == "00001100")) { }  //12
                else if ((baudrate == 57600) && (banks[1, 0x19, 2] == "00000011")) { }  //3
                else
                {
                    return false;
                }
            }

            byte RX = Convert.ToByte(RX_bin, 2);
            if ((RX >= 0x20) && (RX <= 0x7E))
            {
                char RX_char = Convert.ToChar(RX);
                string RX_str = Convert.ToString(RX_char);
                tbRX.AppendText(RX_str);
            }
            else
            {
                tbRX.AppendText(".");
            }

            //Escreve apaga o byte no registrador TXREG (BANK0 19h)
            //Assim que ler o byte o registrador é limpo
            banks[0, 0x19, 2] = "00000000";
            //Dispara evento de SerialVirtual
            int[] reg = { 0, 0x19 };
            try { EventoSerialRx(reg); }
            catch { }

            //Escreve no bit TXIF no registrador (PIR1.4 = TXIF, BANK0 0Ch)
            //Flag de fim de transmissão (PIR1.4 = TXIF = 1, BANK0 0Ch)
            banks[0, 0x0C, 2] = banks[0, 0x0C, 2].Substring(0, 3) + "1" + banks[0, 0x1C, 2].Substring(4, 4);
            //Dispara evento de SerialVirtual
            int[] reg2 = { 0, 0x0C };
            try { EventoSerialRx(reg2); }
            catch { }

            return true;
        }
        /// <summary>
        /// Dispara recepção da Serial Virtual do byte enviado no formato inteiro.
        /// </summary>
        /// <param name="RX_bin">String binária com o valor enviado para Serial Virtual.</param>
        /// <returns>Verdadeiro se pode receber e não há erro nas configurações.</returns>
        public bool Receber_Binario(int RX_bin)
        {
            //banks = Banco[4],Endereço[128]

            //Verifica se a porta está habilitada (RCSTA.7 = SPEN = 1, BANK0 18h)
            if ((banks_bin[0, 0x18] & 0x80) == 0) { ThreadTx.Abort(); }
            //Verifica se TX está habilitado (TXSTA.5 = TXEN = 1, BANK1 18h)
            if ((banks_bin[1, 0x18] & 0x20) == 0) { ThreadTx.Abort(); }
            //Verifica se é modo assíncrono (TXSTA.4 = SYNC = 0, BANK1 18h)
            if ((banks_bin[1, 0x18] & 0x10) != 0) { ThreadTx.Abort(); }
            //Verifica o baud rate (SPBRG, PORT1 19h e TXSTA.2 = BRGH = 0/1, BANK1 18h)
            if ((banks_bin[1, 0x18] & 0x04) == 0)
            {
                int br = banks_bin[1, 0x19];
                if ((baudrate == 300) && (br == 207)) { }       //207
                else if ((baudrate == 1200) && (br == 51)) { }  //51
                else if ((baudrate == 2400) && (br == 25)) { }  //25
                else if ((baudrate == 9600) && (br == 6)) { }   //6
                else if ((baudrate == 19200) && (br == 2)) { }  //2
                else if ((baudrate == 57600) && (br == 0)) { }  //0
                else
                {
                    return false;
                }
            }
            else
            {
                int br = banks_bin[1, 0x19];
                if ((baudrate == 1200) && (br == 207)) { }      //207
                else if ((baudrate == 2400) && (br == 103)) { } //103
                else if ((baudrate == 9600) && (br == 25)) { }  //25
                else if ((baudrate == 19200) && (br == 12)) { } //12
                else if ((baudrate == 57600) && (br == 3)) { }  //3
                else
                {
                    return false;
                }
            }

            byte RX = Convert.ToByte(RX_bin);
            if ((RX >= 0x20) && (RX <= 0x7E))
            {
                char RX_char = Convert.ToChar(RX);
                string RX_str = Convert.ToString(RX_char);
                tbRX.AppendText(RX_str);
            }
            else
            {
                tbRX.AppendText(".");
            }

            //Escreve apaga o byte no registrador TXREG (BANK0 19h)
            //Assim que ler o byte o registrador é limpo
            banks_bin[0, 0x19] = 0;
            //Dispara evento de SerialVirtual
            int[] reg = { 0, 0x19 };
            try { EventoSerialRx(reg); }
            catch { }

            //Escreve no bit TXIF no registrador (PIR1.4 = TXIF, BANK0 0Ch)
            //Flag de fim de transmissão (PIR1.4 = TXIF = 1, BANK0 0Ch)
            banks_bin[0, 0x0C] |= 0x10;
            //Dispara evento de SerialVirtual
            int[] reg2 = { 0, 0x0C };
            try { EventoSerialRx(reg2); }
            catch { }

            return true;
        }
        #endregion


    }
}
