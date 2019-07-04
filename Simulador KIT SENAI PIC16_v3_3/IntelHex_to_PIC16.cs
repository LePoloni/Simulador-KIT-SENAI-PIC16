using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//Incluido manualment
using System.IO;

namespace Simulador_KIT_SENAI_PIC16_v3_3
{
    public partial class IntelHex_to_PIC16 : UserControl
    {
        public IntelHex_to_PIC16()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Lê arquivo IntelHex.
        /// </summary>
        /// <param name="IntelHexFileName">String com o nome do arquivo.</param>
        /// <returns>Matriz com Endereço e OpCode.</returns>
        public ushort[,] IntelHex_To_Rom(string IntelHexFileName)
        {
            //Define variável de retorno
            ushort[,] ROM = new ushort[0, 0];
            try
            {
                //Lê todas as linhas do arquivo e salva em uma matriz de strings
                var lines = File.ReadAllLines(IntelHexFileName);
                //Define o tamanho da ROM (endereço,instrução)
                ROM = IntelHex_To_Rom(lines);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERRO");
            }

            return ROM;
        }
        /// <summary>
        /// Lê strings de um arquivo IntelHex.
        /// </summary>
        /// <param name="IntelHexaLines">Matriz de strings.</param>
        /// <returns>Matriz com Endereço e OpCode.</returns>
        public ushort[,] IntelHex_To_Rom(string[] IntelHexaLines)
        {
            //Lê todas todas as linhas salva em uma matriz de strings
            var lines = IntelHexaLines;

            //Descobre quantas instruções existem no programa
            int intruções = 0;
            foreach (var item in lines)
            {
                //Se for uma linha de dados (instruções)
                if (item.Substring(7, 2) == "00")                               //Campo RECORD
                {
                    intruções += Convert.ToInt32(item.Substring(1, 2), 16) / 2; //Campo BYTE COUNT (byte a byte, por isso divido por 2)
                }
            }

            //Define o tamanho da ROM (endereço,instrução)
            ushort[,] ROM = new ushort[intruções, 2];

            //Lê novamento o arquivo separando os endereços e instruções
            int ROM_add = 0;
            foreach (var item in lines)
            {
                //Se for uma linha de dados (instruções)
                if (item.Substring(7, 2) == "00")                                //Campo RECORD
                {
                    //Endereço inicial
                    ushort endereço = (ushort)(Convert.ToUInt16(item.Substring(3, 4), 16) / 2);   //Campo ADDRESS (byte a byte, por isso divido ppor 2)
                    //Quantidade de instruções na linha
                    intruções = Convert.ToInt32(item.Substring(1, 2), 16) / 2;   //Campo BYTE COUNT (byte a byte, por isso divido ppor 2)
                    //Lê as instruções
                    for (int i = 0; i < intruções; i++)
                    {
                        //Define o endereço
                        ROM[ROM_add, 0] = endereço++;
                        //Lê os bytes que compõe uma instrução (byte high + byte low)
                        string temp = item.Substring(9 + i * 4 + 2, 2) + item.Substring(9 + i * 4, 2);  //Campo DATA
                        ROM[ROM_add++, 1] = Convert.ToUInt16(temp, 16);
                    }
                }
            }
            //Retorno
            return ROM;
        }
        /// <summary>
        /// Converte a memória de programa em múltiplas inteiros.
        /// </summary>
        /// <param name="Rom">Matriz com Endereço e OpCode.</param>
        /// <returns>0-Endereço, 1-Opcode, 2-Byte OpCode High, 3-Byte OpCode Low, 4-Mnemônico, 5-Destino, 6-Registrador, 7-Endereço do Bit, 8-Constante</returns>
        public int[,] Rom_To_Instruction(ushort[,] Rom)
        {
            //Modelo de cada célula da matriz
            //0         1       2                 3                4          5        6            7                8
            //Endereço, Opcode, Byte OpCode High, Byte OpCode Low, Mnemônico, Destino, Registrador, Endereço do Bit, Constante

            //Copia a memória de programa
            ushort[,] rom = Rom;
            //Define a quantidade deinstruções
            int quantidade = rom.Length / 2;
            //Define a matriz de inteiros de retorno
            int[,] instruction = new int[quantidade, 9];

            //Separa todos as instruções
            for (int i = 0; i < quantidade; i++)
            {
                //Endereço
                instruction[i, 0] = rom[i, 0];
                //Opcode
                instruction[i, 1] = rom[i, 1];
                //Byte OpCode High
                instruction[i, 2] = rom[i, 1] >> 8;
                //Byte OpCode Low
                instruction[i, 3] = rom[i, 1] & 0xFF;
                //Mnemônico, Destino, Registrador, Endereço do Bit, Constante
                #region Literal e Controle
                //CLRWDT
                if (rom[i, 1] == 0x0064) instruction[i, 4] = 0x0064;        //Mnemônico
                //RETFEI
                else if (rom[i, 1] == 0x0009) instruction[i, 4] = 0x0009;   //Mnemônico
                //RETURN
                else if (rom[i, 1] == 0x0008) instruction[i, 4] = 0x0008;   //Mnemônico
                //SLEEP
                else if (rom[i, 1] == 0x0063) instruction[i, 4] = 0x0063;   //Mnemônico
                //CALL ou GOTO
                else if ((rom[i, 1] & 0x3000) == 0x2000)
                {
                    instruction[i, 4] = rom[i, 1] & 0x3800; //Mnemônico
                    instruction[i, 8] = rom[i, 1] & 0x07FF; //Constante
                }
                //Demais
                else if ((rom[i, 1] & 0x3000) == 0x3000)
                {
                    instruction[i, 4] = rom[i, 1] & 0x3F00; //Mnemônico
                    instruction[i, 8] = rom[i, 1] & 0x00FF; //Constante
                }
                #endregion
                #region Orientada ao Bit
                else if ((rom[i, 1] & 0x3000) == 0x1000)
                {
                    instruction[i, 4] = rom[i, 1] & 0x3C00; //Mnemônico
                    instruction[i, 6] = rom[i, 1] & 0x007F; //Registrador
                    instruction[i, 7] = (rom[i, 1] & 0x0380) >> 7; //Bit
                }
                #endregion
                #region Orientada ao Byte
                else if ((rom[i, 1] & 0x3000) == 0x0000)
                {
                    //CLFF, CLRW, MOVWF, NOP
                    if (((rom[i, 1] & 0x0F00) == 0x0000) || ((rom[i, 1] & 0x0F00) == 0x0100))
                    {
                        instruction[i, 4] = rom[i, 1] & 0x3F80; //Mnemônico
                        instruction[i, 6] = rom[i, 1] & 0x007F; //Registrador
                        instruction[i, 5] = (rom[i, 1] & 0x0080) >> 7; //Destino
                        //Nessas instruções o bit 7, além de caracterizar a instrução, define o destino
                    }
                    //Demais
                    else
                    {
                        instruction[i, 4] = rom[i, 1] & 0x3F00; //Mnemônico
                        instruction[i, 6] = rom[i, 1] & 0x007F; //Registrador
                        instruction[i, 5] = (rom[i, 1] & 0x0080) >> 7; //Destino
                    }
                }
                #endregion
            }

            //Retorno
            return instruction;
        }
        /// <summary>
        /// Converte a memória de programa em múltiplas strings.
        /// </summary>
        /// <param name="Rom">Matriz com Endereço e OpCode.</param>
        /// <returns>0-Endereço, 1-Byte OpCode High (hexa), 2-Byte OpCode Low (hexa), 3-Byte OpCode High (bi), 4-Byte OpCode Low (bi), 5-Mnemônico, 6-Destino, 7-Registrador, 8-Endereço do Bit, 9-Constante</returns>
        public string[,] Rom_To_String(ushort[,] Rom)
        {
            //Modelo de cada célula da matriz
            //0         1                        2                       3                      4                     5          6        7            8                9
            //Endereço, Byte OpCode High (hexa), Byte OpCode Low (hexa), Byte OpCode High (bi), Byte OpCode Low (bi), Mnemônico, Destino, Registrador, Endereço do Bit, Constante

            //Copia a memória de programa
            ushort[,] rom = Rom;
            //Define a quantidade deinstruções
            int quantidade = rom.Length / 2;
            //Define a matriz de strings de retorno
            string[,] strings = new string[quantidade, 10];
            //Define a matriz de inteiros e chama o método para separação dos campos
            int[,] instruction = Rom_To_Instruction(rom);
            //Flags de atualização
            bool f_dest, f_reg, f_bit, f_cons;

            //Transforma tudo em strings
            for (int i = 0; i < quantidade; i++)
            {
                //Flags
                f_dest = f_reg = f_bit = f_cons = false;
                //Endereço
                strings[i, 0] = instruction[i, 0].ToString();
                //Byte OpCode High (hexa)
                strings[i, 1] = Convert.ToString(instruction[i, 2], 16).ToUpper();
                if (strings[i, 1].Length == 1)
                    strings[i, 1] = "0" + strings[i, 1];
                strings[i, 1] = "0x" + strings[i, 1];
                //Byte OpCode Low (hexa)
                strings[i, 2] = Convert.ToString(instruction[i, 3], 16).ToUpper();
                if (strings[i, 2].Length == 1)
                    strings[i, 2] = "0" + strings[i, 2];
                strings[i, 2] = "0x" + strings[i, 2];
                //Byte OpCode High (bi)
                strings[i, 3] = Convert.ToString(instruction[i, 2], 2);
                while (strings[i, 3].Length < 8)
                    strings[i, 3] = "0" + strings[i, 3];
                //Byte OpCode Low (bi)
                strings[i, 4] = Convert.ToString(instruction[i, 3], 2);
                while (strings[i, 4].Length < 8)
                    strings[i, 4] = "0" + strings[i, 4];

                //Destino (padrão)
                strings[i, 6] = "";
                //Registrador (padrão)
                strings[i, 7] = "";
                //Endereço do Bit (padrão)
                strings[i, 8] = "";
                //Constante (padrão)
                strings[i, 9] = "";

                //Mnemônico
                #region Literal e Controle
                //CLRWDT
                if (instruction[i, 4] == 0x0064) strings[i, 5] = "CLRWDT";
                //RETFEI
                else if (instruction[i, 4] == 0x0009) strings[i, 5] = "RETFEI";
                //RETURN
                else if (instruction[i, 4] == 0x0008) strings[i, 5] = "RETURN";
                //SLEEP
                else if (instruction[i, 4] == 0x0063) strings[i, 5] = "SLEEP";
                //CALL
                else if (instruction[i, 4] == 0x2000) { strings[i, 5] = "CALL"; f_cons = true; }
                //GOTO
                else if (instruction[i, 4] == 0x2800) { strings[i, 5] = "GOTO"; f_cons = true; }
                //ADDLW
                else if ((instruction[i, 4] & 0xFEFF) == 0x3E00) { strings[i, 5] = "ADDLW"; f_cons = true; }
                //ANDLW
                else if (instruction[i, 4] == 0x3900) { strings[i, 5] = "ANDLW"; f_cons = true; }
                //IORLW
                else if (instruction[i, 4] == 0x3800) { strings[i, 5] = "IORLW"; f_cons = true; }
                //MOVLW
                else if ((instruction[i, 4] & 0xFCFF) == 0x3000) { strings[i, 5] = "MOVLW"; f_cons = true; }
                //RETLW
                else if ((instruction[i, 4] & 0xFCFF) == 0x3400) { strings[i, 5] = "RETLW"; f_cons = true; }
                //SUBLW
                else if ((instruction[i, 4] & 0xFEFF) == 0x3C00) { strings[i, 5] = "SUBLW"; f_cons = true; }
                //XORLW
                else if (instruction[i, 4] == 0x3A00) { strings[i, 5] = "XORLW"; f_cons = true; }
                #endregion
                #region Orientada ao Bit
                //BCF
                else if (instruction[i, 4] == 0x1000) { strings[i, 5] = "BCF"; f_bit = f_reg = true; }
                //BSF
                else if (instruction[i, 4] == 0x1400) { strings[i, 5] = "BSF"; f_bit = f_reg = true; }
                //BTFSC
                else if (instruction[i, 4] == 0x1800) { strings[i, 5] = "BTFSC"; f_bit = f_reg = true; }
                //BTFSS
                else if (instruction[i, 4] == 0x1C00) { strings[i, 5] = "BTFSS"; f_bit = f_reg = true; }
                #endregion
                #region Orientada ao Byte
                //ADDWF
                else if (instruction[i, 4] == 0x0700) { strings[i, 5] = "ADDWF"; f_dest = f_reg = true; }
                //ANDWF
                else if (instruction[i, 4] == 0x0500) { strings[i, 5] = "ANDWF"; f_dest = f_reg = true; }
                //CLRF
                else if (instruction[i, 4] == 0x0180) { strings[i, 5] = "CLRF"; f_dest = f_reg = true; }
                //CLRW
                else if (instruction[i, 4] == 0x0100) strings[i, 5] = "CLRW";
                //COMF
                else if (instruction[i, 4] == 0x0900) { strings[i, 5] = "COMF"; f_dest = f_reg = true; }
                //DECF
                else if (instruction[i, 4] == 0x0300) { strings[i, 5] = "DECF"; f_dest = f_reg = true; }
                //DECFSZ
                else if (instruction[i, 4] == 0x0B00) { strings[i, 5] = "DECFSZ"; f_dest = f_reg = true; }
                //INCF
                else if (instruction[i, 4] == 0x0A00) { strings[i, 5] = "INCF"; f_dest = f_reg = true; }
                //INCFSZ
                else if (instruction[i, 4] == 0x0F00) { strings[i, 5] = "INCFSZ"; f_dest = f_reg = true; }
                //IORWF
                else if (instruction[i, 4] == 0x0400) { strings[i, 5] = "IORWF"; f_dest = f_reg = true; }
                //MOVF
                else if (instruction[i, 4] == 0x0800) { strings[i, 5] = "MOVF"; f_dest = f_reg = true; }
                //MOVWF
                else if (instruction[i, 4] == 0x0080) { strings[i, 5] = "MOVWF"; f_dest = f_reg = true; }
                //NOP
                else if (instruction[i, 4] == 0x0000) strings[i, 5] = "NOP";
                //RLF
                else if (instruction[i, 4] == 0x0D00) { strings[i, 5] = "RLF"; f_dest = f_reg = true; }
                //RRF
                else if (instruction[i, 4] == 0x0C00) { strings[i, 5] = "RRF"; f_dest = f_reg = true; }
                //SUBWF
                else if (instruction[i, 4] == 0x0200) { strings[i, 5] = "SUBWF"; f_dest = f_reg = true; }
                //SWAPWF
                else if (instruction[i, 4] == 0x0E00) { strings[i, 5] = "SWAPWF"; f_dest = f_reg = true; }
                //XORWF
                else if (instruction[i, 4] == 0x0600) { strings[i, 5] = "XORWF"; f_dest = f_reg = true; }
                #endregion

                //Destino
                if (f_dest)
                {
                    if (instruction[i, 5] == 0) strings[i, 6] = "W";
                    else strings[i, 6] = "F";
                }
                //Registrador
                if (f_reg)
                {
                    strings[i, 7] = Convert.ToString(instruction[i, 6], 16).ToUpper();
                    if (strings[i, 7].Length == 1)
                        strings[i, 7] = "0" + strings[i, 7];
                    strings[i, 7] = "0x" + strings[i, 7];
                }
                //Endereço do Bit
                if (f_bit)
                {
                    strings[i, 8] = instruction[i, 7].ToString();
                }
                //Constante
                if (f_cons)
                {
                    strings[i, 9] = Convert.ToString(instruction[i, 8], 2);
                    while (strings[i, 9].Length < 8)
                        strings[i, 9] = "0" + strings[i, 9];
                }
            }

            //Retorno
            return strings;
        }
        /// <summary>
        /// Converto os bits de configuração em múltiplas strings.
        /// </summary>
        /// <param name="Rom">0-ADDRESS, 1-CP, 2-DEBUG, 3-WRT1:WRT0, 4-CPD, 5-LVP, 6-BOREN, 7-PWRTEN, 8-WDTEN, 9-FOSC1:FOSC0</param>
        /// <returns></returns>
        public string[,] Rom_To_ConfigurationBits(ushort[,] Rom)
        {
            //Modelo de cada célula da matriz
            //0        1   2      3          4    5    6      7       8      9
            //ADDRESS, CP, DEBUG, WRT1:WRT0, CPD, LVP, BOREN, PWRTEN, WDTEN, FOSC1:FOSC0
            //1a linha - Valor binário
            //2a linha - Interpretação

            //Copia a memória de programa
            ushort[,] rom = Rom;
            //Define a quantidade deinstruções
            int quantidade = rom.Length / 2;
            //Define a matriz de strings de retorno
            string[,] strings = new string[2, 10];

            //O Configuration Word fica na última linha da memória
            //Lê o endereço do Configuration Word
            ushort address = rom[quantidade - 1, 0];
            //Se o endereço está conrreto
            if (address == 0x2007)
            {
                //Lê o valor do Configuration Word
                ushort config = rom[quantidade - 1, 1];
                //Converte para string binária
                string s_config = Convert.ToString(config, 2);
                while (s_config.Length < 14)
                {
                    s_config = "0" + s_config;
                }
                //Percebi que os bits 14 e 15 do valor lido (que não são gravados na memória) estão sempre em 1
                //Isso provocou uma leitura com 2 bits defazados para esquerda (versão 3.3)
                s_config = s_config.Substring(s_config.Length - 14, 14);    //Correção (versão 3.3)
                //Converte para string hexa (versão 3.3)
                string s_config_hexa = Convert.ToString(config, 16);
                while (s_config_hexa.Length < 4)
                {
                    s_config_hexa = "0" + s_config_hexa;
                }
                s_config_hexa = s_config_hexa.ToUpper();
                s_config_hexa = "0x" + s_config_hexa;
                //ADDRESS
                strings[0, 0] = "0x2007";
                strings[1, 0] = s_config_hexa;
                //CP
                strings[0, 1] = s_config.Substring(0, 1);
                if (strings[0, 1] == "0")
                    strings[1, 1] = "On";       //Correção (versão 3.3)
                else
                    strings[1, 1] = "Off";      //Correção (versão 3.3)
                //DEBUG
                strings[0, 2] = s_config.Substring(2, 1);
                if (strings[0, 2] == "0")
                    strings[1, 2] = "On";       //Correção (versão 3.3)
                else
                    strings[1, 2] = "Off";      //Correção (versão 3.3)
                //WRT1:WRT0
                strings[0, 3] = s_config.Substring(3, 2);
                switch (strings[0, 3])
                {
                    case "00": strings[1, 3] = "0000h to 0FFFh write-protected"; break;
                    case "01": strings[1, 3] = "0000h to 07FFh write-protected"; break;
                    case "10": strings[1, 3] = "0000h to 00FFh write-protected"; break;
                    case "11": strings[1, 3] = "Off"; break;
                }
                //CPD
                strings[0, 4] = s_config.Substring(5, 1);
                if (strings[0, 4] == "0")
                    strings[1, 4] = "On";       //Correção (versão 3.3)
                else
                    strings[1, 4] = "Off";      //Correção (versão 3.3)
                //LVP
                strings[0, 5] = s_config.Substring(6, 1);
                if (strings[0, 5] == "0")
                    strings[1, 5] = "Off";
                else
                    strings[1, 5] = "On";
                //BOREN
                strings[0, 6] = s_config.Substring(7, 1);
                if (strings[0, 6] == "0")
                    strings[1, 6] = "Off";
                else
                    strings[1, 6] = "On";
                //PWRTEN
                strings[0, 7] = s_config.Substring(10, 1);
                if (strings[0, 7] == "0")
                    strings[1, 7] = "On";
                else
                    strings[1, 7] = "Off";
                //WDTEN
                strings[0, 8] = s_config.Substring(11, 1);
                if (strings[0, 8] == "0")
                    strings[1, 8] = "Off";
                else
                    strings[1, 8] = "On";
                //FOSC1:FOSC0
                strings[0, 9] = s_config.Substring(12, 2);
                switch (strings[0, 9])
                {
                    case "00": strings[1, 9] = "LP"; break;
                    case "01": strings[1, 9] = "XT"; break;
                    case "10": strings[1, 9] = "HS"; break;
                    case "11": strings[1, 9] = "RC"; break;
                }
            }
            //Retorno
            return strings;
        }
    }
}

