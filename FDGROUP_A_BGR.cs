//
//  Fiscal Devices Group "A" - Bulgaria
//
//  Created by Rosi, Flex and Doba on 04.07.2019
//  Modified on 04.07.2019
//  Copyright (c) 2019 Datecs. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using ZXing;

namespace TestSDK
{
    /// <summary>
    /// Fiscal Devices Group "A" - Bulgaria
    /// </summary>
    public class FDGROUP_A_BGR : FiscalPrinter
    {
        private string infoLastErrorText;
        public StatusBit[,] fstatusbytes = new StatusBit[6, 8];

        public FDGROUP_A_BGR(FiscalComm comm)
            : base(comm, FiscalPrinterProtocol.Legacy, 1251)
        {
            for (int i = 0; i < 6; i++)
            {

                for (int j = 0; j < 8; j++)
                {
                    fstatusbytes[i, j] = new StatusBit();
                }
            }
        }

        uint sBytesCount = 6;
        uint sBytesInUse = 6;
        string sum = "0.00";
        string qrCodeText = "";
        string fMNumFromReceipt = "";
        private Bitmap bmLogo;

        public override void Initialize_StatusBytes()
        {
            Set_AllsBytesBitsState();
            Set_sBytesBitInUse();
            SetStatusBytes_Errors_Default();
            SetStatusBits_Descriptions();
        }

        // Properties for current state of the device informative status bits
        public bool iSBit_Cover_IsOpen => fstatusbytes[0, 6].fCurrentState; // 0.6 = 1 Cover is open.
        public bool iSBit_No_ClientDisplay => fstatusbytes[0, 3].fCurrentState; // 0.3 = 1 Customer display is not installed
        public bool iSBit_NonFiscal_90Degree => fstatusbytes[1, 5].fCurrentState;//1.5 = 1 A service receipt with 90-degree rotated text printing is open
        public bool iSBit_Receipt_Storno => fstatusbytes[1, 4].fCurrentState; // 1.4 = 1 Storno receipt is open
        public bool iSBit_EJ_VeryNearEnd => fstatusbytes[2, 6].fCurrentState; // 2.6  = 1 The end of the EJ is very near (only certain receipts are allowed)
        public bool iSBit_Receipt_Nonfiscal => fstatusbytes[2, 5].fCurrentState; // 2.5 = 1 Nonfiscal receipt is open
        public bool iSBit_EJ_NearlyFull => fstatusbytes[2, 4].fCurrentState; // 2.4  = 1 EJ nearly full
        public bool iSBit_Receipt_Fiscal => fstatusbytes[2, 3].fCurrentState; // 2.3 = 1 Fiscal receipt is open
        public bool iSBit_Near_PaperEnd => fstatusbytes[2, 1].fCurrentState; // 2.1 = 1 Near paper end
        public bool iSBit_Sw7_Status => fstatusbytes[3, 6].fCurrentState; // 3.6 = 1 Sw7 status
        public bool iSBit_Sw6_Status => fstatusbytes[3, 5].fCurrentState; // 3.5 = 1 Sw6 status
        public bool iSBit_Sw5_Status => fstatusbytes[3, 4].fCurrentState; // 3.4 = 1 Sw5 status
        public bool iSBit_Sw4_Status => fstatusbytes[3, 3].fCurrentState; // 3.3 = 1 Sw4 status
        public bool iSBit_Sw3_Status => fstatusbytes[3, 2].fCurrentState; // 3.2 = 1 Sw3 status
        public bool iSBit_Sw2_Status => fstatusbytes[3, 1].fCurrentState; // 3.1 = 1 Sw2 status
        public bool iSBit_Sw1_Status => fstatusbytes[3, 0].fCurrentState; // 3.0 = 1 Sw1 status
        public bool iSBit_PrintingHead_Overheated => fstatusbytes[4, 6].fCurrentState; // 4.6 = 1 The printing head is overheated.
        public bool iSBit_LessThan_50_Reports => fstatusbytes[4, 3].fCurrentState; // 4.3 = 1 There is space for less then 50 reports in Fiscal memory
        public bool iSBit_Number_SFM_Set => fstatusbytes[4, 2].fCurrentState; // 4.2 = 1 Serial number and number of FM are set
        public bool iSBit_Number_Tax_Set => fstatusbytes[4, 1].fCurrentState; // 4.1 = 1 Tax number is set
        public bool iSBit_VAT_Set => fstatusbytes[5, 4].fCurrentState; // 5.4 = 1 VAT are set at least once.
        public bool iSBit_Device_Fiscalized => fstatusbytes[5, 3].fCurrentState; // 5.3 = 1 Device is fiscalized
        public bool iSBit_FM_formatted => fstatusbytes[5, 1].fCurrentState; // 5.1 = 1 FM is formatted

        // Properties for current state of the device error status bits

        public bool eSBit_GeneralError_Sharp => fstatusbytes[0, 5].fCurrentState; //0.5 = 1# General error - this is OR of all errors marked with #
        public bool eSBit_PrintingMechanism => fstatusbytes[0, 4].fCurrentState; // 0.4 = 1# Failure in printing mechanism.
        public bool eSBit_ClockIsNotSynchronized => fstatusbytes[0, 2].fCurrentState; // 0.2 = 1 The real time clock is not synchronize
        public bool eSBit_CommandCodeIsInvalid => fstatusbytes[0, 1].fCurrentState; //0.1 = 1# Command code is invalid.
        public bool eSBit_SyntaxError => fstatusbytes[0, 0].fCurrentState; //0.0 = 1# Syntax error.
        public bool eSBit_BuildInTaxTerminalNotResponding => fstatusbytes[1, 6].fCurrentState; //1.6 = 1 The built-in tax terminal is not responding.
        public bool eSBit_LowBattery => fstatusbytes[1, 3].fCurrentState; //1.3 = 1 # Low battery (the real-time clock is in RESET status)
        public bool eSBit_RamReset => fstatusbytes[1, 2].fCurrentState; //1.2 = 1 # The RAM has been reset
        public bool eSBit_CommandNotPermitted => fstatusbytes[1, 1].fCurrentState; //1.1 = 1# Command is not permitted.
        public bool eSBit_Overflow => fstatusbytes[1, 0].fCurrentState; // 1.0 = 1# Overflow during command execution.
        public bool eSBit_EJIsFull => fstatusbytes[2, 2].fCurrentState; //2.2 = 1 EJ is full.
        public bool eSBit_EndOfPaper => fstatusbytes[2, 0].fCurrentState; // 2.0 = 1# End of paper.
        public bool eSBit_FM_NotAccess => fstatusbytes[4, 0].fCurrentState; // 4.0 = 1* Error when trying to access data stored in the FM.
        public bool eSBit_FM_Full => fstatusbytes[4, 4].fCurrentState;// 4.4 = 1* Fiscal memory is full.
        public bool eSBit_GeneralError_Star => fstatusbytes[4, 5].fCurrentState;// 4.5 = 1 OR of all errors marked with ‘*’
        public bool eSBit_FM_ReadError => fstatusbytes[5, 5].fCurrentState;// 5.5 = 1 Fiscal memory read error
        public bool eSBit_LastFMOperation_NotSuccessful => fstatusbytes[5, 2].fCurrentState;// 5.2 = 1 The last fiscal memory store operation is not successful
        public bool eSBit_FM_ReadOnly => fstatusbytes[5, 0].fCurrentState;// 5.0 = 1 The fiscal memory is set in READONLY mode (locked)

        public void Calculate_StatusBits()
        {
            foreach (var stbit in fstatusbytes)
            {
                if (stbit.fInUse)
                {
                    if (stbit.fCurrentState && stbit.fErrorForEndUser)
                    {
                        throw new Exception(GetErrorMessage("-23"));

                    }
                }
            }
        }

        public bool Set_sBytesState(int byteIndex, int bitIndex)
        {
            fstatusbytes[byteIndex, bitIndex].fCurrentState = (StatusBytes[byteIndex] & (1 << bitIndex)) != 0;
            return fstatusbytes[byteIndex, bitIndex].fCurrentState;
        }

        /// <summary>
        /// Get status bit state
        /// </summary>
        /// <param name="byteIndex">byte index</param>
        /// <param name="bitIndex">bit index </param>
        /// <returns>
        /// Returns current state bool(1 - bit is raised, 0 - bit is not raised)
        /// </returns>
        public bool Get_SBit_State(int byteIndex, int bitIndex)
        {
            return fstatusbytes[byteIndex, bitIndex].fCurrentState;
        }

        public override void Set_AllsBytesBitsState()
        {
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    Set_sBytesState(i, j);
                }
            }
        }

        /// <summary>Gets status bits in use</summary>
        /// <param name="byteIndex">the index of the byte</param>
        /// <param name="bitIndex">the index of the bit (go figure)</param>
        /// <returns>Returns TRUE if the status bit is in use anf FALSE otherwise.</returns>
        public bool Get_sBytesBitInUse(int byteIndex, int bitIndex)
        {
            return fstatusbytes[byteIndex, bitIndex].fInUse;
        }

        public void Set_sBytesBitInUse()
        {
            fstatusbytes[0, 0].fInUse = true;
            fstatusbytes[0, 1].fInUse = true;
            fstatusbytes[0, 2].fInUse = true;
            fstatusbytes[0, 3].fInUse = true;
            fstatusbytes[0, 4].fInUse = true;
            fstatusbytes[0, 5].fInUse = true;
            fstatusbytes[0, 6].fInUse = true;
            fstatusbytes[1, 0].fInUse = true;
            fstatusbytes[1, 1].fInUse = true;
            fstatusbytes[1, 2].fInUse = true;
            fstatusbytes[1, 3].fInUse = true;
            fstatusbytes[1, 4].fInUse = true;
            fstatusbytes[1, 5].fInUse = true;
            fstatusbytes[1, 6].fInUse = true;
            fstatusbytes[2, 0].fInUse = true;
            fstatusbytes[2, 1].fInUse = true;
            fstatusbytes[2, 2].fInUse = true;
            fstatusbytes[2, 3].fInUse = true;
            fstatusbytes[2, 4].fInUse = true;
            fstatusbytes[2, 5].fInUse = true;
            fstatusbytes[2, 6].fInUse = true;
            fstatusbytes[3, 0].fInUse = true;
            fstatusbytes[3, 1].fInUse = true;
            fstatusbytes[3, 2].fInUse = true;
            fstatusbytes[3, 3].fInUse = true;
            fstatusbytes[3, 4].fInUse = true;
            fstatusbytes[3, 5].fInUse = true;
            fstatusbytes[3, 6].fInUse = true;
            fstatusbytes[4, 0].fInUse = true;
            fstatusbytes[4, 1].fInUse = true;
            fstatusbytes[4, 2].fInUse = true;
            fstatusbytes[4, 3].fInUse = true;
            fstatusbytes[4, 4].fInUse = true;
            fstatusbytes[4, 5].fInUse = true;
            fstatusbytes[4, 6].fInUse = true;
            fstatusbytes[5, 0].fInUse = true;
            fstatusbytes[5, 1].fInUse = true;
            fstatusbytes[5, 2].fInUse = true;
            fstatusbytes[5, 3].fInUse = true;
            fstatusbytes[5, 4].fInUse = true;
            fstatusbytes[5, 5].fInUse = true;
        }

        /// <summary>
        /// Get if desired bit is an error
        /// </summary>
        /// <param name="byteIndex">Desired byte index</param>
        /// <param name="bitIndex">Desired bit index</param>
        /// <param name="IsError">If the bit is considered error or not</param>
        /// <returns>
        /// Returns bool (1 - if the bit is considered error, 0 - is it is informatice)
        /// </returns>
        public void Set_Sbit_ErrorChecking(int byteIndex, int bitIndex, bool IsError) // Да може клиентът да си промени дали един статус бит е грешка или не
        {
            fstatusbytes[byteIndex, bitIndex].fErrorForEndUser = IsError;
        }

        public void SetStatusBytes_Errors_Default()
        {
            fstatusbytes[0, 0].fErrorForEndUser = true;
            fstatusbytes[0, 1].fErrorForEndUser = true;
            fstatusbytes[0, 2].fErrorForEndUser = true;
            fstatusbytes[0, 4].fErrorForEndUser = true;
            fstatusbytes[0, 5].fErrorForEndUser = true;
            if (deviceModel == "FP-700" || deviceModel == "FP-2000" || deviceModel == "FP-800" || deviceModel == "FP-650" || deviceModel == "FМP-10")
            {
                fstatusbytes[0, 6].fErrorForEndUser = true;
            }
            fstatusbytes[1, 0].fErrorForEndUser = true;
            fstatusbytes[1, 1].fErrorForEndUser = true;
            fstatusbytes[1, 2].fErrorForEndUser = true;
            fstatusbytes[1, 3].fErrorForEndUser = true;
            fstatusbytes[1, 6].fErrorForEndUser = true;
            fstatusbytes[2, 0].fErrorForEndUser = true;
            fstatusbytes[2, 2].fErrorForEndUser = true;
            fstatusbytes[4, 0].fErrorForEndUser = true;
            fstatusbytes[4, 4].fErrorForEndUser = true;
            fstatusbytes[4, 5].fErrorForEndUser = true;
            fstatusbytes[5, 0].fErrorForEndUser = true;
            fstatusbytes[5, 2].fErrorForEndUser = true;
            fstatusbytes[5, 5].fErrorForEndUser = true;

        }

        public string Get_SBit_Description(int byteIndex, int bitIndex)
        {
            return fstatusbytes[byteIndex, bitIndex].fTextDescription;
        }

        public void SetStatusBits_Descriptions()
        {
            fstatusbytes[0, 0].fTextDescription = GetErrorMessage("-24");
            fstatusbytes[0, 1].fTextDescription = GetErrorMessage("-25");
            fstatusbytes[0, 2].fTextDescription = GetErrorMessage("-26");
            fstatusbytes[0, 3].fTextDescription = GetErrorMessage("-53");
            fstatusbytes[0, 4].fTextDescription = GetErrorMessage("-28");
            fstatusbytes[0, 5].fTextDescription = GetErrorMessage("-29");
            fstatusbytes[0, 6].fTextDescription = GetErrorMessage("-30");
            fstatusbytes[0, 7].fTextDescription = GetErrorMessage("-31");

            fstatusbytes[1, 0].fTextDescription = GetErrorMessage("-33");
            fstatusbytes[1, 1].fTextDescription = GetErrorMessage("-32");
            fstatusbytes[1, 2].fTextDescription = GetErrorMessage("-57");
            fstatusbytes[1, 3].fTextDescription = GetErrorMessage("-56");
            fstatusbytes[1, 4].fTextDescription = GetErrorMessage("-55");
            fstatusbytes[1, 5].fTextDescription = GetErrorMessage("-54");
            fstatusbytes[1, 6].fTextDescription = GetErrorMessage("-50");
            fstatusbytes[1, 7].fTextDescription = GetErrorMessage("-31");

            fstatusbytes[2, 0].fTextDescription = GetErrorMessage("-39");
            fstatusbytes[2, 1].fTextDescription = GetErrorMessage("-38");
            fstatusbytes[2, 2].fTextDescription = GetErrorMessage("-37");
            fstatusbytes[2, 3].fTextDescription = GetErrorMessage("-36");
            fstatusbytes[2, 4].fTextDescription = GetErrorMessage("-35");
            fstatusbytes[2, 5].fTextDescription = GetErrorMessage("-34");
            fstatusbytes[2, 6].fTextDescription = GetErrorMessage("-58");
            fstatusbytes[2, 7].fTextDescription = GetErrorMessage("-31");

            fstatusbytes[3, 0].fTextDescription = GetErrorMessage("-65");
            fstatusbytes[3, 1].fTextDescription = GetErrorMessage("-64");
            fstatusbytes[3, 2].fTextDescription = GetErrorMessage("-63");
            fstatusbytes[3, 3].fTextDescription = GetErrorMessage("-62");
            fstatusbytes[3, 4].fTextDescription = GetErrorMessage("-61");
            fstatusbytes[3, 5].fTextDescription = GetErrorMessage("-60");
            fstatusbytes[3, 6].fTextDescription = GetErrorMessage("-59");
            fstatusbytes[3, 7].fTextDescription = GetErrorMessage("-31");

            fstatusbytes[4, 0].fTextDescription = GetErrorMessage("-46");
            fstatusbytes[4, 1].fTextDescription = GetErrorMessage("-45");
            fstatusbytes[4, 2].fTextDescription = GetErrorMessage("-44");
            fstatusbytes[4, 3].fTextDescription = GetErrorMessage("-52");
            fstatusbytes[4, 4].fTextDescription = GetErrorMessage("-42");
            fstatusbytes[4, 5].fTextDescription = GetErrorMessage("-41");
            fstatusbytes[4, 6].fTextDescription = GetErrorMessage("-66");
            fstatusbytes[4, 7].fTextDescription = GetErrorMessage("-31");

            fstatusbytes[5, 0].fTextDescription = GetErrorMessage("-69");
            fstatusbytes[5, 1].fTextDescription = GetErrorMessage("-49");
            fstatusbytes[5, 2].fTextDescription = GetErrorMessage("-68");
            fstatusbytes[5, 3].fTextDescription = GetErrorMessage("-48");
            fstatusbytes[5, 4].fTextDescription = GetErrorMessage("-47");
            fstatusbytes[5, 5].fTextDescription = GetErrorMessage("-67");
            fstatusbytes[5, 6].fTextDescription = GetErrorMessage("-70");
            fstatusbytes[5, 7].fTextDescription = GetErrorMessage("-31");

        }

        public bool Get_SBit_ErrorChecking(int byteIndex, int bitIndex)
        {
            return fstatusbytes[byteIndex, bitIndex].fErrorForEndUser;
        }

        private void CheckResult()
        {
            Calculate_StatusBits();
        }

        //AI generated source code  -start

        // Command number(Dec): 33 - please check fiscal device documentation.
        /// <summary>
        /// Clear display
        /// </summary>
        public void display_Clear()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(33, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 35 - please check fiscal device documentation.
        /// <summary>
        /// Show text on lower line of the display
        /// </summary>
        /// <param name="textData">Text, up to 20 symbols,sent directly to the display</param>
        public void display_Show_LowerLine(string textData)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textData);

            string r = CustomCommand(35, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 47 - please check fiscal device documentation.
        public void display_Show_UpperLine(string textData)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textData);

            string r = CustomCommand(47, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 63 - please check fiscal device documentation.
        /// <summary>
        /// Display date and time
        /// </summary>
        public void display_Show_DateTime()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(63, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 100 - please check fiscal device documentation.
        /// <summary>
        /// Show text on the display
        /// </summary>
        /// <param name="textData">Text, up to 40 symbols, sent to display</param>
        public void display_Show_Text(string textData)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textData);

            string r = CustomCommand(100, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 38 - please check fiscal device documentation.
        /// <summary>
        /// Open non-fiscal receipt
        /// </summary>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>allReceipt</term>
        /// <description>The number of all issued receipts (fiscal and service) since the last closing time until now (4 bytes).</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_NonFiscal_Open()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(38, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["allReceipt"] = split[0];
            return result;
        }

        // Command number(Dec): 39 - please check fiscal device documentation.
        /// <summary>
        /// Close non-fiscal receipt
        /// </summary>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>allReceipt</term>
        /// <description>The number of all issued receipts (fiscal and service) since the last closing time until now (4 bytes).</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_NonFiscal_Close()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(39, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["allReceipt"] = split[0];
            return result;
        }

        // Command number(Dec): 42 - please check fiscal device documentation.
        /// <summary>
        /// Print free non-fiscal receipt
        /// </summary>
        /// <param name="inputText">One line of text</param>
        public void receipt_NonFiscal_Text(string inputText)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(inputText);

            string r = CustomCommand(42, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 42 - please check fiscal device documentation.
        /// <summary>
        /// 
        /// </summary>
        /// <param name="height">Integer between 0 and 3: '0' 32 dots(4 mm) height, higher letters; '1' 32 dots(4 mm) height, normal letters; '2' 24 dots(3 mm) height; '3' 16 dots(2 mm) height</param>
        /// <param name="flags">Between one and 3 letters: B Bold (strong);H High(double height); I Italic(oblique)</param>
        /// <param name="inputText">One line of text</param>
        public void receipt_PNonFiscal_Text(string height, string flags, string inputText)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\t");
            inputString.Append(height);
            inputString.Append(flags);
            inputString.Append(",");
            inputString.Append(inputText);

            string r = CustomCommand(42, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 122 - please check fiscal device documentation.
        /// <summary>
        /// Open non-fiscal rotated text
        /// </summary>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>rotRec</term>
        /// <description>The sequential number of the closed 90-degree rotated receipt for the day. 4 bytes without a sign.</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_NonFiscalRotated_Open()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(122, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["rotRec"] = split[0];
            return result;
        }

        // Command number(Dec): 123 - please check fiscal device documentation.
        /// <summary>
        /// Print 90-degree rotated text
        /// </summary>
        /// <param name="inputText">Content of next line (up to 100 symbols)</param>
        public void receipt_NonFiscalRotated_Text(string inputText)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(inputText);

            string r = CustomCommand(123, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 124 - please check fiscal device documentation.
        /// <summary>
        /// Close rotated non-fiscal receipt
        /// </summary>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>rotRec</term>
        /// <description>The sequential number of the closed 90-degree rotated receipt for the day. 4 bytes without a sign.</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_NonFiscalRotated_Close()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(124, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["rotRec"] = split[0];
            return result;
        }

        // Combine variants of open storno commands
        /// <summary>
        /// Combined variant for Strono receipt
        /// </summary>
        /// <param name="operatorNumber">Operator ID (between 1 and 16)</param>
        /// <param name="operatorPassword">Operator password (between 4 and 8 digits)</param>
        /// <param name="tillNumber">Cash register location number (integer between 1 and 99999)</param>
        /// <param name="invoice">'I' - if invoice is wanted</param>
        /// <param name="invoiceNumber">Invoice number</param>
        /// <param name="uNP">Unique Sale ID (format: FD Serial Number-four digits or Latin letters-sequential number of the sale(seven digits with leading zeroes),
        /// for example: DT000600-OP01-0001000).
        /// When opening a sales receipt for the first time, the UNP(Unique Sale ID) must be set at least once, if the parameter is then omitted,\n
        /// the FU will increment the unit by the sale number automatically.</param>
        /// <param name="stornoType">'E': operator error; 'R': return/claim; 'T': tax base reduction</param>
        /// <param name="stornoDocumentNumber">(Global) number of the document</param>
        /// <param name="stornoUNP">Unique Sale ID of the storno document. The whole Unique Sale ID must be set.</param>
        /// <param name="stornoDateTime">Date and time of the reversed document. Format 'DDMMYYhhmmss'.></param>
        /// <param name="stornoFMNumber">Fiscal Memory ID of the FD which has issued the storno receipt</param>
        /// <param name="stornoReason">Reason (up to 30 characters).)</param>
        /// <param name="isFullStorno">If 'stornoUNP', 'stornoDateTime' and 'stornoFMNumber' are set, this parameter must be set to FALSE, otherwise - TRUE</param>
        public void open_StornoReceipt(string operatorNumber, string operatorPassword, string tillNumber, string invoice, string invoiceNumber, string uNP, string stornoType, string stornoDocumentNumber, string stornoUNP, string stornoDateTime, string stornoFMNumber, string stornoReason, ref bool isFullStorno)
        {
            //bool Y(string param) { return !string.IsNullOrEmpty(param) && !param.Equals("0"); } // defined as lambda function Y_0
            // bool N(string param) { return string.IsNullOrEmpty(param) || param.Equals("0"); } // defined as lambda function N_0

            if (Y_0(invoice) && Y_0(invoiceNumber) && Y_0(uNP) && Y_0(stornoUNP) && Y_0(stornoDateTime) && Y_0(stornoFMNumber) && Y_0(stornoReason))
                receipt_StornoOpen_A16(operatorNumber, operatorPassword, tillNumber, invoice, invoiceNumber, uNP, stornoType, stornoDocumentNumber, stornoUNP, stornoDateTime, stornoFMNumber, stornoReason);

            if (Y_0(invoice) && Y_0(invoiceNumber) && N_0(uNP) && Y_0(stornoUNP) && Y_0(stornoDateTime) && Y_0(stornoFMNumber) && Y_0(stornoReason))
                receipt_StornoOpen_A15(operatorNumber, operatorPassword, tillNumber, invoice, invoiceNumber, stornoType, stornoDocumentNumber, stornoUNP, stornoDateTime, stornoFMNumber, stornoReason);

            if (Y_0(invoice) && Y_0(invoiceNumber) && Y_0(uNP) && Y_0(stornoUNP) && Y_0(stornoDateTime) && Y_0(stornoFMNumber) && N_0(stornoReason))
                receipt_StornoOpen_A14(operatorNumber, operatorPassword, tillNumber, invoice, invoiceNumber, uNP, stornoType, stornoDocumentNumber, stornoUNP, stornoDateTime, stornoFMNumber);

            if (Y_0(invoice) && Y_0(invoiceNumber) && Y_0(uNP) && Y_0(stornoUNP) && Y_0(stornoDateTime) && Y_0(stornoFMNumber) && Y_0(stornoReason))
                receipt_StornoOpen_A13(operatorNumber, operatorPassword, tillNumber, invoice, invoiceNumber, stornoType, stornoDocumentNumber, stornoUNP, stornoDateTime, stornoFMNumber);

            if (Y_0(invoice) && Y_0(invoiceNumber) && Y_0(uNP) && N_0(stornoUNP) && N_0(stornoDateTime) && N_0(stornoFMNumber) && Y_0(stornoReason))
                receipt_StornoOpen_A12(operatorNumber, operatorPassword, tillNumber, invoice, invoiceNumber, uNP, stornoType, stornoDocumentNumber, stornoReason);

            if (Y_0(invoice) && Y_0(invoiceNumber) && N_0(uNP) && N_0(stornoUNP) && N_0(stornoDateTime) && N_0(stornoFMNumber) && Y_0(stornoReason))
                receipt_StornoOpen_A11(operatorNumber, operatorPassword, tillNumber, invoice, invoiceNumber, stornoType, stornoDocumentNumber, stornoReason);

            if (Y_0(invoice) && Y_0(invoiceNumber) && Y_0(uNP) && N_0(stornoUNP) && N_0(stornoDateTime) && N_0(stornoFMNumber) && N_0(stornoReason))
                receipt_StornoOpen_A10(operatorNumber, operatorPassword, tillNumber, invoice, invoiceNumber, uNP, stornoType, stornoDocumentNumber);

            if (Y_0(invoice) && Y_0(invoiceNumber) && N_0(uNP) && N_0(stornoUNP) && N_0(stornoDateTime) && N_0(stornoFMNumber) && N_0(stornoReason))
                receipt_StornoOpen_A09(operatorNumber, operatorPassword, tillNumber, invoice, invoiceNumber, stornoType, stornoDocumentNumber);

            if (N_0(invoice) && N_0(invoiceNumber) && Y_0(uNP) && Y_0(stornoUNP) && Y_0(stornoDateTime) && Y_0(stornoFMNumber) && Y_0(stornoReason))
                receipt_StornoOpen_A08(operatorNumber, operatorPassword, tillNumber, uNP, stornoType, stornoDocumentNumber, stornoUNP, stornoDateTime, stornoFMNumber, stornoReason);

            if (N_0(invoice) && N_0(invoiceNumber) && N_0(uNP) && Y_0(stornoUNP) && Y_0(stornoDateTime) && Y_0(stornoFMNumber) && Y_0(stornoReason))
                receipt_StornoOpen_A07(operatorNumber, operatorPassword, tillNumber, stornoType, stornoDocumentNumber, stornoUNP, stornoDateTime, stornoFMNumber, stornoReason);

            if (N_0(invoice) && N_0(invoiceNumber) && Y_0(uNP) && Y_0(stornoUNP) && Y_0(stornoDateTime) && Y_0(stornoFMNumber) && N_0(stornoReason))
                receipt_StornoOpen_A06(operatorNumber, operatorPassword, tillNumber, uNP, stornoType, stornoDocumentNumber, stornoUNP, stornoDateTime, stornoFMNumber);

            if (N_0(invoice) && N_0(invoiceNumber) && N_0(uNP) && Y_0(stornoUNP) && Y_0(stornoDateTime) && Y_0(stornoFMNumber) && N_0(stornoReason))
                receipt_StornoOpen_A05(operatorNumber, operatorPassword, tillNumber, stornoType, stornoDocumentNumber, stornoUNP, stornoDateTime, stornoFMNumber);

            if (N_0(invoice) && N_0(invoiceNumber) && Y_0(uNP) && N_0(stornoUNP) && N_0(stornoDateTime) && N_0(stornoFMNumber) && Y_0(stornoReason))
                receipt_StornoOpen_A04(operatorNumber, operatorPassword, tillNumber, uNP, stornoType, stornoDocumentNumber, stornoReason);

            if (N_0(invoice) && N_0(invoiceNumber) && N_0(uNP) && N_0(stornoUNP) && N_0(stornoDateTime) && N_0(stornoFMNumber) && Y_0(stornoReason))
                receipt_StornoOpen_A03(operatorNumber, operatorPassword, tillNumber, stornoType, stornoDocumentNumber, stornoReason);

            if (N_0(invoice) && N_0(invoiceNumber) && Y_0(uNP) && N_0(stornoUNP) && N_0(stornoDateTime) && N_0(stornoFMNumber) && N_0(stornoReason))
                receipt_StornoOpen_A02(operatorNumber, operatorPassword, tillNumber, uNP, stornoType, stornoDocumentNumber);

            if (N_0(invoice) && N_0(invoiceNumber) && N_0(uNP) && N_0(stornoUNP) && N_0(stornoDateTime) && N_0(stornoFMNumber) && N_0(stornoReason))
                receipt_StornoOpen_A01(operatorNumber, operatorPassword, tillNumber, stornoType, stornoDocumentNumber);

            if (Y_0(stornoUNP) && Y_0(stornoDateTime) && Y_0(stornoFMNumber)) isFullStorno = false;
            else isFullStorno = true;

            CheckResult();

        }

        // Command number(Dec): 46 - please check fiscal device documentation.
        /// <summary>
        /// Open storno document - variant 1
        /// </summary>
        /// <param name="operatorNumber">Operator ID (between 1 and 16)</param>
        /// <param name="operatorPassword">Operator password (between 4 and 8 digits)</param>
        /// <param name="tillNumber">Cash register location number (integer between 1 and 99999)</param>
        /// <param name="stornoType">'E': operator error; 'R': return/claim; 'T': tax base reduction</param>
        /// <param name="stornoDocumentNumber">(Global) number of the document</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>The number of all issued receipts (fiscal and service) since the last closing time until now (4 bytes).</description>
        /// </item> 
        /// <item>
        /// <term>stornoReceiptCount</term>
        /// <description>The number of all fiscal receipts since the last closing time until now. (4 bytes).</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_StornoOpen_A01(string operatorNumber, string operatorPassword, string tillNumber, string stornoType, string stornoDocumentNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(stornoType);
            inputString.Append(stornoDocumentNumber);

            string r = CustomCommand(46, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["allReceiptCount"] = split[0];
            if (split.Length >= 2)
                result["stornoReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 46 - please check fiscal device documentation.
        /// <summary>
        /// Open storno document - variant 2
        /// </summary>
        /// <param name="operatorNumber">Operator ID (between 1 and 16)</param>
        /// <param name="operatorPassword">Operator password (between 4 and 8 digits)</param>
        /// <param name="tillNumber">Cash register location number (integer between 1 and 99999)</param>
        /// <param name="uNP">Unique Sale ID (format: FD Serial Number-four digits or Latin letters-sequential number of the sale(seven digits with leading zeroes),\n
        /// for example: DT000600-OP01-0001000).</param>
        /// <param name="stornoType">'E': operator error; 'R': return/claim; 'T': tax base reduction</param>
        /// <param name="stornoDocumentNumber">(Global) number of the document being reversed</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>The number of all issued receipts (fiscal and service) since the last closing time until now (4 bytes).</description>
        /// </item> 
        /// <item>
        /// <term>stornoReceiptCount</term>
        /// <description>The number of all fiscal receipts since the last closing time until now. (4 bytes).</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_StornoOpen_A02(string operatorNumber, string operatorPassword, string tillNumber, string uNP, string stornoType, string stornoDocumentNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(uNP);
            inputString.Append(",");
            inputString.Append(stornoType);
            inputString.Append(stornoDocumentNumber);

            string r = CustomCommand(46, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["allReceiptCount"] = split[0];
            if (split.Length >= 2)
                result["stornoReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 46 - please check fiscal device documentation.
        /// <summary>
        /// Open storno document - variant 3
        /// </summary>
        /// <param name="operatorNumber">Operator ID (between 1 and 16)</param>
        /// <param name="operatorPassword">Operator password (between 4 and 8 digits)</param>
        /// <param name="tillNumber">Cash register location number (integer between 1 and 99999)</param>
        /// <param name="stornoType">'E': operator error; 'R': return/claim; 'T': tax base reduction</param>
        /// <param name="stornoDocumentNumber">(Global) number of the document being reversed</param>
        /// <param name="stornoReason">The number of all fiscal receipts since the last closing time until now. (4 bytes)</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>The number of all issued receipts (fiscal and service) since the last closing time until now (4 bytes).</description>
        /// </item> 
        /// <item>
        /// <term>stornoReceiptCount</term>
        /// <description>The number of all fiscal receipts since the last closing time until now. (4 bytes).</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_StornoOpen_A03(string operatorNumber, string operatorPassword, string tillNumber, string stornoType, string stornoDocumentNumber, string stornoReason)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(stornoType);
            inputString.Append(stornoDocumentNumber);
            inputString.Append("#");
            inputString.Append(stornoReason);

            string r = CustomCommand(46, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["allReceiptCount"] = split[0];
            if (split.Length >= 2)
                result["stornoReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 46 - please check fiscal device documentation.
        /// <summary>
        /// Open storno document - variant 4
        /// </summary>
        /// <param name="operatorNumber">Operator ID (between 1 and 16)</param>
        /// <param name="operatorPassword">Operator password (between 4 and 8 digits)</param>
        /// <param name="tillNumber">Cash register location number (integer between 1 and 99999)</param>
        /// <param name="uNP">Leave blank</param>
        /// <param name="stornoType">'E': operator error; 'R': return/claim; 'T': tax base reduction</param>
        /// <param name="stornoDocumentNumber">(Global) number of the document being reversed</param>
        /// <param name="stornoReason">The number of all fiscal receipts since the last closing time until now. (4 bytes)</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>The number of all issued receipts (fiscal and service) since the last closing time until now (4 bytes).</description>
        /// </item> 
        /// <item>
        /// <term>stornoReceiptCount</term>
        /// <description>The number of all fiscal receipts since the last closing time until now. (4 bytes).</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_StornoOpen_A04(string operatorNumber, string operatorPassword, string tillNumber, string uNP, string stornoType, string stornoDocumentNumber, string stornoReason)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(uNP);
            inputString.Append(",");
            inputString.Append(stornoType);
            inputString.Append(stornoDocumentNumber);
            inputString.Append("#");
            inputString.Append(stornoReason);

            string r = CustomCommand(46, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["allReceiptCount"] = split[0];
            if (split.Length >= 2)
                result["stornoReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 46 - please check fiscal device documentation.
        /// <summary>
        /// Open storno document - variant 5
        /// </summary>
        /// <param name="operatorNumber">Operator ID (between 1 and 16)</param>
        /// <param name="operatorPassword">Operator password (between 4 and 8 digits)</param>
        /// <param name="tillNumber">Cash register location number (integer between 1 and 99999)</param>
        /// <param name="stornoType">'E': operator error; 'R': return/claim; 'T': tax base reduction</param>
        /// <param name="stornoDocumentNumber">(Global) number of the document being reversed</param>
        /// <param name="stornoUNP">Unique Sale ID of the document being reversed. The whole Unique Sale ID must be set.</param>
        /// <param name="stornoDateTime">Date and time of the reversed document. Format “DDMMYYhhmmss”.</param>
        /// <param name="stornoFMNumber">Fiscal Memory ID of the FD which has issued the storno receipt.</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>The number of all issued receipts (fiscal and service) since the last closing time until now (4 bytes).</description>
        /// </item> 
        /// <item>
        /// <term>stornoReceiptCount</term>
        /// <description>The number of all fiscal receipts since the last closing time until now. (4 bytes).</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_StornoOpen_A05(string operatorNumber, string operatorPassword, string tillNumber, string stornoType, string stornoDocumentNumber, string stornoUNP, string stornoDateTime, string stornoFMNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(stornoType);
            inputString.Append(stornoDocumentNumber);
            inputString.Append(",");
            inputString.Append(stornoUNP);
            inputString.Append(",");
            inputString.Append(stornoDateTime);
            inputString.Append(",");
            inputString.Append(stornoFMNumber);

            string r = CustomCommand(46, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["allReceiptCount"] = split[0];
            if (split.Length >= 2)
                result["stornoReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 46 - please check fiscal device documentation.
        /// <summary>
        /// Open storno document - variant 6
        /// </summary>
        /// <param name="operatorNumber">Operator ID (between 1 and 16)</param>
        /// <param name="operatorPassword">Operator password (between 4 and 8 digits)</param>
        /// <param name="tillNumber">Cash register location number (integer between 1 and 99999)</param>
        /// <param name="uNP">Unique Sale ID (format: FD Serial Number-four digits or Latin letters-sequential number of the sale(seven digits with leading zeroes)</param>
        /// <param name="stornoType">'E': operator error; 'R': return/claim; 'T': tax base reduction</param>
        /// <param name="stornoDocumentNumber">(Global) number of the document being reversed</param>
        /// <param name="stornoUNP">Unique Sale ID of the document being reversed. The whole Unique Sale ID must be set.</param>
        /// <param name="stornoDateTime">Date and time of the reversed document. Format “DDMMYYhhmmss”.</param>
        /// <param name="stornoFMNumber">Fiscal Memory ID of the FD which has issued the storno receipt.</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>The number of all issued receipts (fiscal and service) since the last closing time until now (4 bytes).</description>
        /// </item> 
        /// <item>
        /// <term>stornoReceiptCount</term>
        /// <description>The number of all fiscal receipts since the last closing time until now. (4 bytes).</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_StornoOpen_A06(string operatorNumber, string operatorPassword, string tillNumber, string uNP, string stornoType, string stornoDocumentNumber, string stornoUNP, string stornoDateTime, string stornoFMNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(uNP);
            inputString.Append(",");
            inputString.Append(stornoType);
            inputString.Append(stornoDocumentNumber);
            inputString.Append(",");
            inputString.Append(stornoUNP);
            inputString.Append(",");
            inputString.Append(stornoDateTime);
            inputString.Append(",");
            inputString.Append(stornoFMNumber);

            string r = CustomCommand(46, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["allReceiptCount"] = split[0];
            if (split.Length >= 2)
                result["stornoReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 46 - please check fiscal device documentation.
        /// <summary>
        /// Open storno document - variant 7
        /// </summary>
        /// <param name="operatorNumber">Operator ID (between 1 and 16)</param>
        /// <param name="operatorPassword">Operator password (between 4 and 8 digits)</param>
        /// <param name="tillNumber">Cash register location number (integer between 1 and 99999)</param>
        /// <param name="stornoType">'E': operator error; 'R': return/claim; 'T': tax base reduction</param>
        /// <param name="stornoDocumentNumber">(Global) number of the document being reversed</param>
        /// <param name="stornoUNP">Unique Sale ID of the document being reversed. The whole Unique Sale ID must be set.</param>
        /// <param name="stornoDateTime">Date and time of the reversed document. Format “DDMMYYhhmmss”.</param>
        /// <param name="stornoFMNumber">Fiscal Memory ID of the FD which has issued the storno receipt.</param>
        /// <param name="stornoReason">Reason (up to 30 characters).</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>The number of all issued receipts (fiscal and service) since the last closing time until now (4 bytes).</description>
        /// </item> 
        /// <item>
        /// <term>stornoReceiptCount</term>
        /// <description>The number of all fiscal receipts since the last closing time until now. (4 bytes).</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_StornoOpen_A07(string operatorNumber, string operatorPassword, string tillNumber, string stornoType, string stornoDocumentNumber, string stornoUNP, string stornoDateTime, string stornoFMNumber, string stornoReason)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(stornoType);
            inputString.Append(stornoDocumentNumber);
            inputString.Append(",");
            inputString.Append(stornoUNP);
            inputString.Append(",");
            inputString.Append(stornoDateTime);
            inputString.Append(",");
            inputString.Append(stornoFMNumber);
            inputString.Append("#");
            inputString.Append(stornoReason);

            string r = CustomCommand(46, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["allReceiptCount"] = split[0];
            if (split.Length >= 2)
                result["stornoReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 46 - please check fiscal device documentation.
        /// <summary>
        /// Open storno document - variant 8
        /// </summary>
        /// <param name="operatorNumber">Operator ID (between 1 and 16)</param>
        /// <param name="operatorPassword">Operator password (between 4 and 8 digits)</param>
        /// <param name="tillNumber">Cash register location number (integer between 1 and 99999)</param>
        /// <param name="uNP">Unique Sale ID (format: FD Serial Number-four digits or Latin letters-sequential number of the sale(seven digits with leading zeroes)</param>
        /// <param name="stornoType">'E': operator error; 'R': return/claim; 'T': tax base reduction</param>
        /// <param name="stornoDocumentNumber">(Global) number of the document being reversed</param>
        /// <param name="stornoUNP">Unique Sale ID of the document being reversed. The whole Unique Sale ID must be set.</param>
        /// <param name="stornoDateTime">Date and time of the reversed document. Format “DDMMYYhhmmss”.</param>
        /// <param name="stornoFMNumber">Fiscal Memory ID of the FD which has issued the storno receipt.</param>
        /// <param name="stornoReason">Reason (up to 30 characters).</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>The number of all issued receipts (fiscal and service) since the last closing time until now (4 bytes).</description>
        /// </item> 
        /// <item>
        /// <term>stornoReceiptCount</term>
        /// <description>The number of all fiscal receipts since the last closing time until now. (4 bytes).</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_StornoOpen_A08(string operatorNumber, string operatorPassword, string tillNumber, string uNP, string stornoType, string stornoDocumentNumber, string stornoUNP, string stornoDateTime, string stornoFMNumber, string stornoReason)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(uNP);
            inputString.Append(",");
            inputString.Append(stornoType);
            inputString.Append(stornoDocumentNumber);
            inputString.Append(",");
            inputString.Append(stornoUNP);
            inputString.Append(",");
            inputString.Append(stornoDateTime);
            inputString.Append(",");
            inputString.Append(stornoFMNumber);
            inputString.Append("#");
            inputString.Append(stornoReason);

            string r = CustomCommand(46, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["allReceiptCount"] = split[0];
            if (split.Length >= 2)
                result["stornoReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 46 - please check fiscal device documentation.
        /// <summary>
        /// Open storno document - variant 9
        /// </summary>
        /// <param name="operatorNumber">Operator ID (between 1 and 16)</param>
        /// <param name="operatorPassword">Operator password (between 4 and 8 digits)</param>
        /// <param name="tillNumber">Cash register location number (integer between 1 and 99999)</param>
        /// <param name="invoice">'I' - for invoice</param>
        /// <param name="invoiceNumber">Invoice number</param>
        /// <param name="stornoType">'E': operator error; 'R': return/claim; 'T': tax base reduction</param>
        /// <param name="stornoDocumentNumber">(Global) number of the document being reversed</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>The number of all issued receipts (fiscal and service) since the last closing time until now (4 bytes).</description>
        /// </item> 
        /// <item>
        /// <term>stornoReceiptCount</term>
        /// <description>The number of all fiscal receipts since the last closing time until now. (4 bytes).</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_StornoOpen_A09(string operatorNumber, string operatorPassword, string tillNumber, string invoice, string invoiceNumber, string stornoType, string stornoDocumentNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(invoice);
            inputString.Append(invoiceNumber);
            inputString.Append(",");
            inputString.Append(stornoType);
            inputString.Append(stornoDocumentNumber);

            string r = CustomCommand(46, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["allReceiptCount"] = split[0];
            if (split.Length >= 2)
                result["stornoReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 46 - please check fiscal device documentation.
        /// <summary>
        /// Open storno document - variant 10
        /// </summary>
        /// <param name="operatorNumber">Operator ID (between 1 and 16)</param>
        /// <param name="operatorPassword">Operator password (between 4 and 8 digits)</param>
        /// <param name="tillNumber">Cash register location number (integer between 1 and 99999)</param>
        /// <param name="invoice">'I' - for invoice</param>
        /// <param name="invoiceNumber">Invoice number</param>
        /// <param name="uNP">Unique Sale ID (format: FD Serial Number-four digits or Latin letters-sequential number of the sale(seven digits with leading zeroes)</param>
        /// <param name="stornoType">'E': operator error; 'R': return/claim; 'T': tax base reduction</param>
        /// <param name="stornoDocumentNumber">(Global) number of the document being reversed</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>The number of all issued receipts (fiscal and service) since the last closing time until now (4 bytes).</description>
        /// </item> 
        /// <item>
        /// <term>stornoReceiptCount</term>
        /// <description>The number of all fiscal receipts since the last closing time until now. (4 bytes).</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_StornoOpen_A10(string operatorNumber, string operatorPassword, string tillNumber, string invoice, string invoiceNumber, string uNP, string stornoType, string stornoDocumentNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(invoice);
            inputString.Append(invoiceNumber);
            inputString.Append(",");
            inputString.Append(uNP);
            inputString.Append(",");
            inputString.Append(stornoType);
            inputString.Append(stornoDocumentNumber);

            string r = CustomCommand(46, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["allReceiptCount"] = split[0];
            if (split.Length >= 2)
                result["stornoReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 46 - please check fiscal device documentation.
        /// <summary>
        /// Open storno document - variant 11
        /// </summary>
        /// <param name="operatorNumber">Operator ID (between 1 and 16)</param>
        /// <param name="operatorPassword">Operator password (between 4 and 8 digits)</param>
        /// <param name="tillNumber">Cash register location number (integer between 1 and 99999)</param>
        /// <param name="invoice">'I' - for invoice</param>
        /// <param name="invoiceNumber">Invoice number</param>
        /// <param name="stornoType">'E': operator error; 'R': return/claim; 'T': tax base reduction</param>
        /// <param name="stornoDocumentNumber">(Global) number of the document being reversed</param>
        /// <param name="stornoReason">Reason (up to 30 symbols)</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>The number of all issued receipts (fiscal and service) since the last closing time until now (4 bytes).</description>
        /// </item> 
        /// <item>
        /// <term>stornoReceiptCount</term>
        /// <description>The number of all fiscal receipts since the last closing time until now. (4 bytes).</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_StornoOpen_A11(string operatorNumber, string operatorPassword, string tillNumber, string invoice, string invoiceNumber, string stornoType, string stornoDocumentNumber, string stornoReason)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(invoice);
            inputString.Append(invoiceNumber);
            inputString.Append(",");
            inputString.Append(stornoType);
            inputString.Append(stornoDocumentNumber);
            inputString.Append("#");
            inputString.Append(stornoReason);

            string r = CustomCommand(46, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["allReceiptCount"] = split[0];
            if (split.Length >= 2)
                result["stornoReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 46 - please check fiscal device documentation.
        /// <summary>
        /// Open storno document - variant 12
        /// </summary>
        /// <param name="operatorNumber">Operator ID (between 1 and 16)</param>
        /// <param name="operatorPassword">Operator password (between 4 and 8 digits)</param>
        /// <param name="tillNumber">Cash register location number (integer between 1 and 99999)</param>
        /// <param name="invoice">'I' - for invoice</param>
        /// <param name="invoiceNumber">Invoice number</param>
        /// <param name="uNP">Unique Sale ID (format: FD Serial Number-four digits or Latin letters-sequential number of the sale(seven digits with leading zeroes)</param>
        /// <param name="stornoType">'E': operator error; 'R': return/claim; 'T': tax base reduction</param>
        /// <param name="stornoDocumentNumber">(Global) number of the document being reversed</param>
        /// <param name="stornoReason">Reason (up to 30 symbols)</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>The number of all issued receipts (fiscal and service) since the last closing time until now (4 bytes).</description>
        /// </item> 
        /// <item>
        /// <term>stornoReceiptCount</term>
        /// <description>The number of all fiscal receipts since the last closing time until now. (4 bytes).</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_StornoOpen_A12(string operatorNumber, string operatorPassword, string tillNumber, string invoice, string invoiceNumber, string uNP, string stornoType, string stornoDocumentNumber, string stornoReason)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(invoice);
            inputString.Append(invoiceNumber);
            inputString.Append(",");
            inputString.Append(uNP);
            inputString.Append(",");
            inputString.Append(stornoType);
            inputString.Append(stornoDocumentNumber);
            inputString.Append("#");
            inputString.Append(stornoReason);

            string r = CustomCommand(46, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["allReceiptCount"] = split[0];
            if (split.Length >= 2)
                result["stornoReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 46 - please check fiscal device documentation.
        /// <summary>
        /// Open storno document - variant 13
        /// </summary>
        /// <param name="operatorNumber">Operator ID (between 1 and 16)</param>
        /// <param name="operatorPassword">Operator password (between 4 and 8 digits)</param>
        /// <param name="tillNumber">Cash register location number (integer between 1 and 99999)</param>
        /// <param name="invoice">'I' - for invoice</param>
        /// <param name="invoiceNumber">Invoice number</param>
        /// <param name="stornoType">'E': operator error; 'R': return/claim; 'T': tax base reduction</param>
        /// <param name="stornoDocumentNumber">(Global) number of the document being reversed</param>
        /// <param name="stornoUNP">Unique Sale ID of the document being reversed. The whole Unique Sale ID must be set.</param>
        /// <param name="stornoDateTime">Date and time of the reversed document. Format “DDMMYYhhmmss”.</param>
        /// <param name="stornoFMNumber">Fiscal Memory ID of the FD which has issued the storno receipt.</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>The number of all issued receipts (fiscal and service) since the last closing time until now (4 bytes).</description>
        /// </item> 
        /// <item>
        /// <term>stornoReceiptCount</term>
        /// <description>The number of all fiscal receipts since the last closing time until now. (4 bytes).</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_StornoOpen_A13(string operatorNumber, string operatorPassword, string tillNumber, string invoice, string invoiceNumber, string stornoType, string stornoDocumentNumber, string stornoUNP, string stornoDateTime, string stornoFMNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(invoice);
            inputString.Append(invoiceNumber);
            inputString.Append(",");
            inputString.Append(stornoType);
            inputString.Append(stornoDocumentNumber);
            inputString.Append(",");
            inputString.Append(stornoUNP);
            inputString.Append(",");
            inputString.Append(stornoDateTime);
            inputString.Append(",");
            inputString.Append(stornoFMNumber);

            string r = CustomCommand(46, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["allReceiptCount"] = split[0];
            if (split.Length >= 2)
                result["stornoReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 46 - please check fiscal device documentation.
        /// <summary>
        /// Open storno document - variant 14
        /// </summary>
        /// <param name="operatorNumber">Operator ID (between 1 and 16)</param>
        /// <param name="operatorPassword">Operator password (between 4 and 8 digits)</param>
        /// <param name="tillNumber">Cash register location number (integer between 1 and 99999)</param>
        /// <param name="invoice">'I' - for invoice</param>
        /// <param name="invoiceNumber">Invoice number</param>
        /// <param name="uNP">Unique Sale ID (format: FD Serial Number-four digits or Latin letters-sequential number of the sale(seven digits with leading zeroes)</param>
        /// <param name="stornoType">'E': operator error; 'R': return/claim; 'T': tax base reduction</param>
        /// <param name="stornoDocumentNumber">(Global) number of the document being reversed</param>
        /// <param name="stornoUNP">Unique Sale ID of the document being reversed. The whole Unique Sale ID must be set.</param>
        /// <param name="stornoDateTime">Date and time of the reversed document. Format “DDMMYYhhmmss”.</param>
        /// <param name="stornoFMNumber">Fiscal Memory ID of the FD which has issued the storno receipt.</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>The number of all issued receipts (fiscal and service) since the last closing time until now (4 bytes).</description>
        /// </item> 
        /// <item>
        /// <term>stornoReceiptCount</term>
        /// <description>The number of all fiscal receipts since the last closing time until now. (4 bytes).</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_StornoOpen_A14(string operatorNumber, string operatorPassword, string tillNumber, string invoice, string invoiceNumber, string uNP, string stornoType, string stornoDocumentNumber, string stornoUNP, string stornoDateTime, string stornoFMNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(invoice);
            inputString.Append(invoiceNumber);
            inputString.Append(",");
            inputString.Append(uNP);
            inputString.Append(",");
            inputString.Append(stornoType);
            inputString.Append(stornoDocumentNumber);
            inputString.Append(",");
            inputString.Append(stornoUNP);
            inputString.Append(",");
            inputString.Append(stornoDateTime);
            inputString.Append(",");
            inputString.Append(stornoFMNumber);

            string r = CustomCommand(46, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["allReceiptCount"] = split[0];
            if (split.Length >= 2)
                result["stornoReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 46 - please check fiscal device documentation.
        /// <summary>
        /// Open storno document - variant 15
        /// </summary>
        /// <param name="operatorNumber">Operator ID (between 1 and 16)</param>
        /// <param name="operatorPassword">Operator password (between 4 and 8 digits)</param>
        /// <param name="tillNumber">Cash register location number (integer between 1 and 99999)</param>
        /// <param name="invoice">'I' - for invoice</param>
        /// <param name="invoiceNumber">Invoice number</param>
        /// <param name="stornoType">'E': operator error; 'R': return/claim; 'T': tax base reduction</param>
        /// <param name="stornoDocumentNumber">(Global) number of the document being reversed</param>
        /// <param name="stornoUNP">Unique Sale ID of the document being reversed. The whole Unique Sale ID must be set.</param>
        /// <param name="stornoDateTime">Date and time of the reversed document. Format “DDMMYYhhmmss”.</param>
        /// <param name="stornoFMNumber">Fiscal Memory ID of the FD which has issued the storno receipt.</param>
        /// <param name="stornoReason">Reason (up to 30 symbols)</param>
        /// <returns></returns>
        public Dictionary<string, string> receipt_StornoOpen_A15(string operatorNumber, string operatorPassword, string tillNumber, string invoice, string invoiceNumber, string stornoType, string stornoDocumentNumber, string stornoUNP, string stornoDateTime, string stornoFMNumber, string stornoReason)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(invoice);
            inputString.Append(invoiceNumber);
            inputString.Append(",");
            inputString.Append(stornoType);
            inputString.Append(stornoDocumentNumber);
            inputString.Append(",");
            inputString.Append(stornoUNP);
            inputString.Append(",");
            inputString.Append(stornoDateTime);
            inputString.Append(",");
            inputString.Append(stornoFMNumber);
            inputString.Append("#");
            inputString.Append(stornoReason);

            string r = CustomCommand(46, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["allReceiptCount"] = split[0];
            if (split.Length >= 2)
                result["stornoReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 46 - please check fiscal device documentation.
        /// <summary>
        /// Open storno document - variant 16
        /// </summary>
        /// <param name="operatorNumber">Operator ID (between 1 and 16)</param>
        /// <param name="operatorPassword">Operator password (between 4 and 8 digits)</param>
        /// <param name="tillNumber">Cash register location number (integer between 1 and 99999)</param>
        /// <param name="invoice">'I' - for invoice</param>
        /// <param name="invoiceNumber">Invoice number</param>
        /// <param name="uNP">Unique Sale ID (format: FD Serial Number-four digits or Latin letters-sequential number of the sale(seven digits with leading zeroes)</param>
        /// <param name="stornoType">'E': operator error; 'R': return/claim; 'T': tax base reduction</param>
        /// <param name="stornoDocumentNumber">(Global) number of the document being reversed</param>
        /// <param name="stornoUNP">Unique Sale ID of the document being reversed. The whole Unique Sale ID must be set.</param>
        /// <param name="stornoDateTime">Date and time of the reversed document. Format “DDMMYYhhmmss”.</param>
        /// <param name="stornoFMNumber">Fiscal Memory ID of the FD which has issued the storno receipt.</param>
        /// <param name="stornoReason">Reason (up to 30 symbols)</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>The number of all issued receipts (fiscal and service) since the last closing time until now (4 bytes).</description>
        /// </item> 
        /// <item>
        /// <term>stornoReceiptCount</term>
        /// <description>The number of all fiscal receipts since the last closing time until now. (4 bytes).</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_StornoOpen_A16(string operatorNumber, string operatorPassword, string tillNumber, string invoice, string invoiceNumber, string uNP, string stornoType, string stornoDocumentNumber, string stornoUNP, string stornoDateTime, string stornoFMNumber, string stornoReason)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(invoice);
            inputString.Append(invoiceNumber);
            inputString.Append(",");
            inputString.Append(uNP);
            inputString.Append(",");
            inputString.Append(stornoType);
            inputString.Append(stornoDocumentNumber);
            inputString.Append(",");
            inputString.Append(stornoUNP);
            inputString.Append(",");
            inputString.Append(stornoDateTime);
            inputString.Append(",");
            inputString.Append(stornoFMNumber);
            inputString.Append("#");
            inputString.Append(stornoReason);

            string r = CustomCommand(46, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["allReceiptCount"] = split[0];
            if (split.Length >= 2)
                result["stornoReceiptCount"] = split[1];
            return result;
        }

        public void open_FiscalReceipt(string operatorNumber, string operatorPassword, string uNP, string tillNumber, string invoice)
        {

            if (Y(uNP) && Y(invoice)) receipt_Invoice_Open(operatorNumber, operatorPassword, tillNumber, invoice, uNP);
            if (N(uNP) && Y(invoice)) receipt_FiscalOpen_A02(operatorNumber, operatorPassword, tillNumber, invoice);
            if (Y(uNP) && N(invoice)) receipt_Fiscal_Open(operatorNumber, operatorPassword, tillNumber, uNP);
            if (N(uNP) && N(invoice)) receipt_FiscalOpen_A01(operatorNumber, operatorPassword, tillNumber);
        }

        // Command number(Dec): 48 - please check fiscal device documentation.
        /// <summary>
        /// Open fiscal receipt
        /// </summary>
        /// <param name="operatorNumber">Operator ID (between 1 and 16)</param>
        /// <param name="operatorPassword">Operator password (between 4 and 8 digits)</param>
        /// <param name="tillNumber">Cash register location number (integer between 1 and 99999)</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>The number of all issued receipts (fiscal and service) since the last closing time until now (4 bytes).</description>
        /// </item> 
        /// <item>
        /// <term>fiscalReceiptCount</term>
        /// <description>The number of all fiscal receipts since the last closing time until now. (4 bytes).</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Fiscal_01_Open(string operatorNumber, string operatorPassword, string tillNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(tillNumber);

            string r = CustomCommand(48, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["allReceiptCount"] = split[0];
            if (split.Length >= 2)
                result["fiscalReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 48 - please check fiscal device documentation.
        /// <summary>
        /// Open fiscal receipt with UNP
        /// </summary>
        /// <param name="operatorNumber">Operator ID (between 1 and 16)</param>
        /// <param name="operatorPassword">Operator password (between 4 and 8 digits)</param>
        /// <param name="tillNumber">Cash register location number (integer between 1 and 99999)</param>
        /// <param name="uNP">Unique Sale ID (format: FD Serial Number-four digits or Latin letters-sequential number of the sale(seven digits with leading zeroes),\n
        /// for example: DT000600-OP01-0001000)</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>The number of all issued receipts (fiscal and service) since the last closing time until now (4 bytes).</description>
        /// </item> 
        /// <item>
        /// <term>fiscalReceiptCount</term>
        /// <description>The number of all fiscal receipts since the last closing time until now. (4 bytes).</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Fiscal_02_Open(string operatorNumber, string operatorPassword, string tillNumber, string uNP)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(uNP);

            string r = CustomCommand(48, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["allReceiptCount"] = split[0];
            if (split.Length >= 2)
                result["fiscalReceiptCount"] = split[1];
            return result;
        }

        public Dictionary<string, string> receipt_FiscalOpen_A01(string operatorNumber, string operatorPassword, string tillNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(tillNumber);

            string r = CustomCommand(48, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["allReceiptCount"] = split[0];
            if (split.Length >= 2)
                result["fiscalReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 48 - please check fiscal device documentation.
        public Dictionary<string, string> receipt_FiscalOpen_A03(string operatorNumber, string operatorPassword, string tillNumber, string uNP)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(uNP);

            string r = CustomCommand(48, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["allReceiptCount"] = split[0];
            if (split.Length >= 2)
                result["fiscalReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 48 - please check fiscal device documentation.
        public Dictionary<string, string> receipt_Fiscal_Open(string operatorNumber, string operatorPassword, string tillNumber, string uNP)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(uNP);

            string r = CustomCommand(48, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["allReceiptCount"] = split[0];
            if (split.Length >= 2)
                result["fiscalReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 56 - please check fiscal device documentation.
        /// <summary>
        /// Close fiscal receipt
        /// </summary>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>The number of all issued receipts (fiscal and service) since the last closing time until now (4 bytes).</description>
        /// </item> 
        /// <item>
        /// <term>fiscalReceiptCount</term>
        /// <description>The number of all fiscal receipts since the last closing time until now. (4 bytes).</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Fiscal_Close()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(56, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["allReceiptCount"] = split[0];
            if (split.Length >= 2)
                result["fiscalReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 48 - please check fiscal device documentation.
        /// <summary>
        /// Open invoice 
        /// </summary>
        /// <param name="operatorNumber">Operator ID (between 1 and 16)</param>
        /// <param name="operatorPassword">Operator password (between 4 and 8 digits)</param>
        /// <param name="tillNumber">Cash register location number (integer between 1 and 99999)</param>
        /// <param name="invoice">'I' - for invoice</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>The number of all issued receipts (fiscal and service) since the last closing time until now (4 bytes).</description>
        /// </item> 
        /// <item>
        /// <term>fiscalReceiptCount</term>
        /// <description>The number of all fiscal receipts since the last closing time until now. (4 bytes).</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Invoice_01_Open(string operatorNumber, string operatorPassword, string tillNumber, string invoice)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(invoice);

            string r = CustomCommand(48, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["allReceiptCount"] = split[0];
            if (split.Length >= 2)
                result["fiscalReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 48 - please check fiscal device documentation.
        /// <summary>
        /// Open invoice 
        /// </summary>
        /// <param name="operatorNumber">Operator ID (between 1 and 16)</param>
        /// <param name="operatorPassword">Operator password (between 4 and 8 digits)</param>
        /// <param name="tillNumber">Cash register location number (integer between 1 and 99999)</param>
        /// <param name="invoice">'I' - for invoice</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>The number of all issued receipts (fiscal and service) since the last closing time until now (4 bytes).</description>
        /// </item> 
        /// <item>
        /// <term>fiscalReceiptCount</term>
        /// <description>The number of all fiscal receipts since the last closing time until now. (4 bytes).</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_FiscalOpen_A02(string operatorNumber, string operatorPassword, string tillNumber, string invoice)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(invoice);

            string r = CustomCommand(48, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["allReceiptCount"] = split[0];
            if (split.Length >= 2)
                result["fiscalReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 48 - please check fiscal device documentation.
        /// <summary>
        /// Open invoice with UNP
        /// </summary>
        /// <param name="operatorNumber">Operator ID (between 1 and 16)</param>
        /// <param name="operatorPassword">Operator password (between 4 and 8 digits)</param>
        /// <param name="tillNumber">Cash register location number (integer between 1 and 99999)</param>
        /// <param name="invoice">'I' - for invoice</param>
        /// <param name="uNP">Unique Sale ID (format: FD Serial Number-four digits or Latin letters-sequential number of the sale(seven digits with leading zeroes),\n
        /// for example: DT000600-OP01-0001000)</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>The number of all issued receipts (fiscal and service) since the last closing time until now (4 bytes).</description>
        /// </item> 
        /// <item>
        /// <term>fiscalReceiptCount</term>
        /// <description>The number of all fiscal receipts since the last closing time until now. (4 bytes).</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_FiscalOpen_A04(string operatorNumber, string operatorPassword, string tillNumber, string invoice, string uNP)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(invoice);
            inputString.Append(",");
            inputString.Append(uNP);

            string r = CustomCommand(48, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["allReceiptCount"] = split[0];
            if (split.Length >= 2)
                result["fiscalReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 48 - please check fiscal device documentation.
        /// <summary>
        /// Open invoice with UNP
        /// </summary>
        /// <param name="operatorNumber">Operator ID (between 1 and 16)</param>
        /// <param name="operatorPassword">Operator password (between 4 and 8 digits)</param>
        /// <param name="tillNumber">Cash register location number (integer between 1 and 99999)</param>
        /// <param name="invoice">'I' - for invoice</param>
        /// <param name="uNP">Unique Sale ID (format: FD Serial Number-four digits or Latin letters-sequential number of the sale(seven digits with leading zeroes),\n
        /// for example: DT000600-OP01-0001000)</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>The number of all issued receipts (fiscal and service) since the last closing time until now (4 bytes).</description>
        /// </item> 
        /// <item>
        /// <term>fiscalReceiptCount</term>
        /// <description>The number of all fiscal receipts since the last closing time until now. (4 bytes).</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Invoice_Open(string operatorNumber, string operatorPassword, string tillNumber, string invoice, string uNP)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(invoice);
            inputString.Append(",");
            inputString.Append(uNP);

            string r = CustomCommand(48, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["allReceiptCount"] = split[0];
            if (split.Length >= 2)
                result["fiscalReceiptCount"] = split[1];
            return result;
        }

        public void receipt_PrintClientInfo(string eIK, string eIKType, string sellerName, string receiverName, string clientName, string taxNo, string address1, string address2)
        {

            if (Y(eIK) && Y(eIKType) && N(sellerName) && N(receiverName) && N(clientName) && N(taxNo) && N(address1) && N(address2)) receipt_PrintClientInfo_01(eIKType, eIK);
            if (Y(eIK) && Y(eIKType) && Y(sellerName) && N(receiverName) && N(clientName) && N(taxNo) && N(address1) && N(address2)) receipt_PrintClientInfo_02(eIKType, eIK, sellerName);
            if (Y(eIK) && Y(eIKType) && Y(sellerName) && Y(receiverName) && N(clientName) && N(taxNo) && N(address1) && N(address2)) receipt_PrintClientInfo_03(eIKType, eIK, sellerName, receiverName);
            if (Y(eIK) && Y(eIKType) && Y(sellerName) && Y(receiverName) && Y(clientName) && N(taxNo) && N(address1) && N(address2)) receipt_PrintClientInfo_04(eIKType, eIK, sellerName, receiverName, clientName);
            if (Y(eIK) && Y(eIKType) && Y(sellerName) && Y(receiverName) && Y(clientName) && Y(taxNo) && N(address1) && N(address2)) receipt_PrintClientInfo_05(eIKType, eIK, sellerName, receiverName, clientName, taxNo);
            if (Y(eIK) && Y(eIKType) && Y(sellerName) && Y(receiverName) && Y(clientName) && Y(taxNo) && Y(address1) && N(address2)) receipt_PrintClientInfo_06(eIKType, eIK, sellerName, receiverName, clientName, taxNo, address1);
            if (Y(eIK) && Y(eIKType) && Y(sellerName) && Y(receiverName) && Y(clientName) && Y(taxNo) && Y(address1) && Y(address2)) receipt_PrintClientInfo_07(eIKType, eIK, sellerName, receiverName, clientName, taxNo, address1, address2);
        }

        // Command number(Dec): 57 - please check fiscal device documentation.
        /// <summary>
        /// Print customer information
        /// </summary>
        /// <param name="eIKType">If preceded by the ‘#’ character, the data are treated as a PIN, if ‘*’ - personal number, if ‘^’ - work number</param>
        /// <param name="eIK">Customer’s UIC. Between 9 and 14 characters</param>
        public void receipt_PrintClientInfo_01(string eIKType, string eIK)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(eIKType);
            inputString.Append(eIK);

            string r = CustomCommand(57, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 57 - please check fiscal device documentation.
        /// <summary>
        /// Print customer information
        /// </summary>
        /// <param name="eIKType">If preceded by the ‘#’ character, the data are treated as a PIN, if ‘*’ - personal number, if ‘^’ - work number</param>
        /// <param name="eIK">Customer’s UIC. Between 9 and 14 characters</param>
        /// <param name="sellerName">Seller’s name. Up to 26 characters.</param>
        public void receipt_PrintClientInfo_02(string eIKType, string eIK, string sellerName)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(eIKType);
            inputString.Append(eIK);
            inputString.Append("\t");
            inputString.Append(sellerName);

            string r = CustomCommand(57, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 57 - please check fiscal device documentation.
        /// <summary>
        /// Print customer information
        /// </summary>
        /// <param name="eIKType">If preceded by the ‘#’ character, the data are treated as a PIN, if ‘*’ - personal number, if ‘^’ - work number</param>
        /// <param name="eIK">Customer’s UIC. Between 9 and 14 characters</param>
        /// <param name="sellerName">Seller’s name. Up to 26 characters.</param>
        /// <param name="receiverName">Recipient’s name. Up to 26 characters</param>
        public void receipt_PrintClientInfo_03(string eIKType, string eIK, string sellerName, string receiverName)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(eIKType);
            inputString.Append(eIK);
            inputString.Append("\t");
            inputString.Append(sellerName);
            inputString.Append("\t");
            inputString.Append(receiverName);

            string r = CustomCommand(57, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 57 - please check fiscal device documentation.
        /// <summary>
        /// Print customer information
        /// </summary>
        /// <param name="eIKType">If preceded by the ‘#’ character, the data are treated as a PIN, if ‘*’ - personal number, if ‘^’ - work number</param>
        /// <param name="eIK">Customer’s UIC. Between 9 and 14 characters</param>
        /// <param name="sellerName">Seller’s name. Up to 26 characters.</param>
        /// <param name="receiverName">Recipient’s name. Up to 26 characters</param>
        /// <param name="clientName">Customer’s name. Up to 26 characters</param>
        public void receipt_PrintClientInfo_04(string eIKType, string eIK, string sellerName, string receiverName, string clientName)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(eIKType);
            inputString.Append(eIK);
            inputString.Append("\t");
            inputString.Append(sellerName);
            inputString.Append("\t");
            inputString.Append(receiverName);
            inputString.Append("\t");
            inputString.Append(clientName);

            string r = CustomCommand(57, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 57 - please check fiscal device documentation.
        /// <summary>
        /// Print customer information
        /// </summary>
        /// <param name="eIKType">If preceded by the ‘#’ character, the data are treated as a PIN, if ‘*’ - personal number, if ‘^’ - work number</param>
        /// <param name="eIK">Customer’s UIC. Between 9 and 14 characters</param>
        /// <param name="sellerName">Seller’s name. Up to 26 characters.</param>
        /// <param name="receiverName">Recipient’s name. Up to 26 characters</param>
        /// <param name="clientName">Customer’s name. Up to 26 characters</param>
        /// <param name="taxNo">Customer’s VAT number. Between 10 and 14 characters</param>
        public void receipt_PrintClientInfo_05(string eIKType, string eIK, string sellerName, string receiverName, string clientName, string taxNo)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(eIKType);
            inputString.Append(eIK);
            inputString.Append("\t");
            inputString.Append(sellerName);
            inputString.Append("\t");
            inputString.Append(receiverName);
            inputString.Append("\t");
            inputString.Append(clientName);
            inputString.Append("\t");
            inputString.Append(taxNo);

            string r = CustomCommand(57, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 57 - please check fiscal device documentation.
        /// <summary>
        /// Print customer information
        /// </summary>
        /// <param name="eIKType">If preceded by the ‘#’ character, the data are treated as a PIN, if ‘*’ - personal number, if ‘^’ - work number</param>
        /// <param name="eIK">Customer’s UIC. Between 9 and 14 characters</param>
        /// <param name="sellerName">Seller’s name. Up to 26 characters.</param>
        /// <param name="receiverName">Recipient’s name. Up to 26 characters</param>
        /// <param name="clientName">Customer’s name. Up to 26 characters</param>
        /// <param name="taxNo">Customer’s VAT number. Between 10 and 14 characters</param>
        /// <param name="address1">Customer’s address (up to 28 symbols)</param>
        public void receipt_PrintClientInfo_06(string eIKType, string eIK, string sellerName, string receiverName, string clientName, string taxNo, string address1)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(eIKType);
            inputString.Append(eIK);
            inputString.Append("\t");
            inputString.Append(sellerName);
            inputString.Append("\t");
            inputString.Append(receiverName);
            inputString.Append("\t");
            inputString.Append(clientName);
            inputString.Append("\t");
            inputString.Append(taxNo);
            inputString.Append("\t");
            inputString.Append(address1);

            string r = CustomCommand(57, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 57 - please check fiscal device documentation.
        /// <summary>
        /// Print customer information
        /// </summary>
        /// <param name="eIKType">If preceded by the ‘#’ character, the data are treated as a PIN, if ‘*’ - personal number, if ‘^’ - work number</param>
        /// <param name="eIK">Customer’s UIC. Between 9 and 14 characters</param>
        /// <param name="sellerName">Seller’s name. Up to 26 characters.</param>
        /// <param name="receiverName">Recipient’s name. Up to 26 characters</param>
        /// <param name="clientName">Customer’s name. Up to 26 characters</param>
        /// <param name="taxNo">Customer’s VAT number. Between 10 and 14 characters</param>
        /// <param name="address1">Customer’s address (up to 28 symbols)</param>
        /// <param name="address2">Customer’s address second line (up to 34 symbols)</param>
        public void receipt_PrintClientInfo_07(string eIKType, string eIK, string sellerName, string receiverName, string clientName, string taxNo, string address1, string address2)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(eIKType);
            inputString.Append(eIK);
            inputString.Append("\t");
            inputString.Append(sellerName);
            inputString.Append("\t");
            inputString.Append(receiverName);
            inputString.Append("\t");
            inputString.Append(clientName);
            inputString.Append("\t");
            inputString.Append(taxNo);
            inputString.Append("\t");
            inputString.Append(address1);
            inputString.Append("\n");
            inputString.Append(address2);

            string r = CustomCommand(57, inputString.ToString());
            CheckResult();


        }

        public void execute_Sale(string textRow1, string textRow2, string department, string taxGroup, string singlePrice, string quantity, string measure, string percent, string abs)
        {

            if (Y(taxGroup))
            {
                if (Y(textRow1) && Y(textRow2) && Y(singlePrice) && Y(quantity) && N(department) && N(measure) && N(percent) && N(abs)) receipt_Sale(textRow1, textRow2, taxGroup, singlePrice, quantity);
                if (Y(textRow1) && Y(singlePrice) && Y(quantity) && N(textRow2) && N(department) && N(measure) && N(percent) && N(abs)) receipt_Sale_TextRow1(textRow1, taxGroup, singlePrice, quantity);
                if (Y(textRow1) && Y(singlePrice) && N(quantity) && N(textRow2) && N(department) && N(measure) && N(percent) && N(abs)) receipt_Sale_TextRow1_WQuan(textRow1, taxGroup, singlePrice);
                if (Y(textRow2) && Y(singlePrice) && Y(quantity) && N(textRow1) && N(department) && N(measure) && N(percent) && N(abs)) receipt_Sale_TextRow2(textRow2, taxGroup, singlePrice, quantity);
                if (Y(textRow1) && Y(singlePrice) && N(quantity) && N(textRow2) && N(department) && N(measure) && N(percent) && N(abs)) receipt_Sale_Minimum(taxGroup, singlePrice);
                if (Y(textRow1) && Y(textRow2) && Y(singlePrice) && Y(quantity) && Y(measure) && N(department) && N(measure) && N(abs)) receipt_Sale_Un(textRow1, textRow2, taxGroup, singlePrice, quantity, measure);
                if (Y(textRow1) && N(textRow2) && Y(singlePrice) && Y(quantity) && Y(measure) && N(department) && N(percent) && N(abs)) receipt_Sale_TextRow1Un(textRow1, taxGroup, singlePrice, quantity, measure);
                if (Y(textRow2) && N(textRow1) && Y(singlePrice) && Y(quantity) && Y(measure) && N(department) && N(percent) && N(abs)) receipt_Sale_TextRow2Un(textRow2, taxGroup, singlePrice, quantity, measure);
                if (Y(singlePrice) && Y(quantity) && Y(measure) && N(textRow1) && N(textRow2) && N(department) && N(percent) && N(abs)) receipt_Sale_UnWText(taxGroup, singlePrice, quantity, measure);
                if (Y(textRow1) && Y(textRow2) && Y(singlePrice) && Y(quantity) && Y(percent) && N(measure) && N(department) && N(abs)) receipt_Sale_CByPercent(textRow1, textRow2, taxGroup, singlePrice, quantity, percent);
                if (Y(textRow1) && Y(singlePrice) && Y(quantity) && Y(percent) && N(textRow2) && N(measure) && N(department) && N(abs)) receipt_Sale_TextRow1CByPercent(textRow1, taxGroup, singlePrice, quantity, percent);
                if (Y(textRow2) && Y(singlePrice) && Y(quantity) && Y(percent) && N(measure) && N(textRow1) && N(department) && N(abs)) receipt_Sale_TextRow2CByPercent(textRow2, taxGroup, singlePrice, quantity, percent);
                if (Y(singlePrice) && Y(quantity) && Y(percent) && N(measure) && N(textRow1) && N(textRow2) && N(department) && N(abs)) receipt_Sale_CByPercentWText(taxGroup, singlePrice, quantity, percent);
                if (Y(textRow1) && Y(textRow2) && Y(singlePrice) && Y(quantity) && Y(measure) && Y(percent) && N(department) && N(abs)) receipt_Sale_UnCByPercent(textRow1, textRow2, taxGroup, singlePrice, quantity, measure, percent);
                if (Y(textRow1) && N(textRow2) && Y(singlePrice) && Y(quantity) && Y(measure) && Y(percent) && N(department) && N(abs)) receipt_Sale_TextRow1UnCByPercent(textRow1, taxGroup, singlePrice, quantity, measure, percent);
                if (N(textRow1) && Y(textRow2) && Y(singlePrice) && Y(quantity) && Y(measure) && Y(percent) && N(department) && N(abs)) receipt_Sale_TextRow2UnCByPercent(textRow2, taxGroup, singlePrice, quantity, measure, percent);
                if (N(textRow1) && N(textRow2) && Y(singlePrice) && Y(quantity) && Y(measure) && Y(percent) && N(department) && N(abs)) receipt_Sale_UnCByPercentWText(taxGroup, singlePrice, quantity, measure, percent);
                if (Y(textRow1) && Y(textRow2) && Y(singlePrice) && Y(quantity) && Y(abs) && N(measure) && N(percent) && N(department)) receipt_Sale_CBySum(textRow1, textRow2, taxGroup, singlePrice, quantity, abs);
                if (Y(textRow1) && N(textRow2) && Y(singlePrice) && Y(quantity) && Y(abs) && N(measure) && N(percent) && N(department)) receipt_Sale_TextRow1CBySum(textRow1, taxGroup, singlePrice, quantity, abs);
                if (N(textRow1) && Y(textRow2) && Y(singlePrice) && Y(quantity) && Y(abs) && N(measure) && N(percent) && N(department)) receipt_Sale_TextRow2CBySum(textRow2, taxGroup, singlePrice, quantity, abs);
                if (N(textRow1) && N(textRow2) && Y(singlePrice) && Y(quantity) && Y(abs) && N(measure) && N(percent) && N(department)) receipt_Sale_CBySumWText(taxGroup, singlePrice, quantity, abs);
                if (Y(textRow1) && Y(textRow2) && Y(singlePrice) && Y(quantity) && Y(abs) && Y(measure) && N(percent) && N(department)) receipt_Sale_UnCBySum(textRow1, textRow2, taxGroup, singlePrice, quantity, measure, abs);
                if (Y(textRow1) && N(textRow2) && Y(singlePrice) && Y(quantity) && Y(abs) && Y(measure) && N(percent) && N(department)) receipt_Sale_TextRow1UnCBySum(textRow1, taxGroup, singlePrice, quantity, measure, abs);
                if (N(textRow1) && Y(textRow2) && Y(singlePrice) && Y(quantity) && Y(abs) && Y(measure) && N(percent) && N(department)) receipt_Sale_TextRow2UnCBySum(textRow2, taxGroup, singlePrice, quantity, measure, abs);
                if (N(textRow1) && N(textRow2) && Y(singlePrice) && Y(quantity) && Y(abs) && Y(measure) && N(percent) && N(department)) receipt_Sale_UnCBySumWText(taxGroup, singlePrice, quantity, measure, abs);

            }
            if (Y(department))
            {
                if (Y(textRow1) && Y(textRow2) && Y(singlePrice) && Y(quantity) && N(taxGroup) && N(abs) && N(measure) && N(percent)) receipt_DSale(textRow1, textRow2, department, singlePrice, quantity);
                if (Y(textRow1) && N(textRow2) && Y(singlePrice) && Y(quantity) && N(taxGroup) && N(abs) && N(measure) && N(percent)) receipt_DSale_TextRow1(textRow1, department, singlePrice, quantity);
                if (Y(textRow1) && N(textRow2) && Y(singlePrice) && N(quantity) && N(taxGroup) && N(abs) && N(measure) && N(percent)) receipt_DSale_TextRow1_WQuan(textRow1, department, singlePrice);
                if (Y(textRow2) && N(textRow1) && Y(singlePrice) && Y(quantity) && N(taxGroup) && N(abs) && N(measure) && N(percent)) receipt_DSale_TextRow2(textRow2, department, singlePrice, quantity);
                if (N(textRow2) && N(textRow1) && Y(singlePrice) && N(quantity) && N(taxGroup) && N(abs) && N(measure) && N(percent)) receipt_DSale_Minimum(department, singlePrice);
                if (Y(textRow2) && Y(textRow1) && Y(singlePrice) && Y(quantity) && N(taxGroup) && N(abs) && Y(measure) && N(percent)) receipt_DSale_Un(textRow1, textRow2, department, singlePrice, quantity, measure);
                if (Y(textRow1) && N(textRow2) && Y(singlePrice) && Y(quantity) && N(taxGroup) && N(abs) && Y(measure) && N(percent)) receipt_DSale_TextRow1Un(textRow1, department, singlePrice, quantity, measure);
                if (Y(textRow2) && N(textRow1) && Y(singlePrice) && Y(quantity) && N(taxGroup) && N(abs) && Y(measure) && N(percent)) receipt_DSale_TextRow2Un(textRow2, department, singlePrice, quantity, measure);
                if (N(textRow2) && N(textRow1) && Y(singlePrice) && Y(quantity) && N(taxGroup) && N(abs) && Y(measure) && N(percent)) receipt_DSale_UnWText(department, singlePrice, quantity, measure);
                if (Y(textRow2) && Y(textRow1) && Y(singlePrice) && Y(quantity) && Y(percent) && N(taxGroup) && N(abs) && Y(measure)) receipt_DSale_CByPercent(textRow1, textRow2, department, singlePrice, quantity, percent);
                if (Y(textRow1) && N(textRow2) && Y(singlePrice) && Y(quantity) && Y(percent) && N(taxGroup) && N(abs) && N(measure)) receipt_DSale_TextRow1CByPercent(textRow1, department, singlePrice, quantity, percent);
                if (N(textRow1) && Y(textRow2) && Y(singlePrice) && Y(quantity) && Y(percent) && N(taxGroup) && N(abs) && N(measure)) receipt_DSale_TextRow2CByPercent(textRow2, department, singlePrice, quantity, percent);
                if (N(textRow1) && N(textRow2) && Y(singlePrice) && Y(quantity) && Y(percent) && N(taxGroup) && N(abs) && N(measure)) receipt_DSale_CByPercentWText(department, singlePrice, quantity, percent);
                if (Y(textRow1) && Y(textRow2) && Y(singlePrice) && Y(quantity) && Y(percent) && N(taxGroup) && N(abs) && Y(measure)) receipt_DSale_UnCByPercent(textRow1, textRow2, department, singlePrice, quantity, measure, percent);
                if (Y(textRow1) && N(textRow2) && Y(singlePrice) && Y(quantity) && Y(percent) && N(taxGroup) && N(abs) && Y(measure)) receipt_DSale_TextRow1UnCByPercent(textRow1, department, singlePrice, quantity, measure, percent);
                if (N(textRow1) && Y(textRow2) && Y(singlePrice) && Y(quantity) && Y(percent) && N(taxGroup) && N(abs) && Y(measure)) receipt_DSale_TextRow2UnCByPercent(textRow2, department, singlePrice, quantity, measure, percent);
                if (N(textRow1) && N(textRow2) && Y(singlePrice) && Y(quantity) && Y(percent) && N(taxGroup) && N(abs) && Y(measure)) receipt_DSale_UnCByPercentWText(department, singlePrice, quantity, measure, percent);
                if (Y(textRow1) && Y(textRow2) && Y(singlePrice) && Y(quantity) && N(percent) && N(taxGroup) && Y(abs) && N(measure)) receipt_DSale_CBySum(textRow1, textRow2, department, singlePrice, quantity, abs);
                if (Y(textRow1) && N(textRow2) && Y(singlePrice) && Y(quantity) && N(percent) && N(taxGroup) && Y(abs) && N(measure)) receipt_DSale_TextRow1CBySum(textRow1, department, singlePrice, quantity, abs);
                if (N(textRow1) && Y(textRow2) && Y(singlePrice) && Y(quantity) && N(percent) && N(taxGroup) && Y(abs) && N(measure)) receipt_DSale_TextRow2CBySum(textRow2, department, singlePrice, quantity, abs);
                if (N(textRow1) && N(textRow2) && Y(singlePrice) && Y(quantity) && N(percent) && N(taxGroup) && Y(abs) && N(measure)) receipt_DSale_CBySumWText(department, singlePrice, quantity, abs);
                if (Y(textRow1) && Y(textRow2) && Y(singlePrice) && Y(quantity) && N(percent) && N(taxGroup) && Y(abs) && Y(measure)) receipt_DSale_UnCBySum(textRow1, textRow2, department, singlePrice, quantity, measure, abs);
                if (Y(textRow1) && N(textRow2) && Y(singlePrice) && Y(quantity) && N(percent) && N(taxGroup) && Y(abs) && Y(measure)) receipt_DSale_TextRow1UnCBySum(textRow1, department, singlePrice, quantity, measure, abs);
                if (N(textRow1) && Y(textRow2) && Y(singlePrice) && Y(quantity) && N(percent) && N(taxGroup) && Y(abs) && Y(measure)) receipt_DSale_TextRow2UnCBySum(textRow2, department, singlePrice, quantity, measure, abs);
                if (N(textRow1) && N(textRow2) && Y(singlePrice) && Y(quantity) && N(percent) && N(taxGroup) && Y(abs) && Y(measure)) receipt_DSale_UnCBySumWText(department, singlePrice, quantity, measure, abs);
            }
            CheckResult();
        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow1">Text, up to 42 bytes, containing a line describing the sale</param>
        /// <param name="textRow2">Text, up to 42 bytes, containing a second line describing the sale</param>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...).</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        public void receipt_Sale(string textRow1, string textRow2, string taxGroup, string singlePrice, string quantity)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\n");
            inputString.Append(textRow2);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);

            string r = CustomCommand(49, inputString.ToString());

            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow1">Text, up to 42 bytes, containing a line describing the sale</param>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...).</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        public void receipt_Sale_TextRow1(string textRow1, string taxGroup, string singlePrice, string quantity)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow1">Text, up to 42 bytes, containing a line describing the sale</param>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...).</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        public void receipt_Sale_TextRow1_WQuan(string textRow1, string taxGroup, string singlePrice)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();
        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        ///  Enter a product (sale)
        /// </summary>
        /// <param name="textRow2">Text, up to 42 bytes, containing a second line describing the sale</param>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...).</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        public void receipt_Sale_TextRow2(string textRow2, string taxGroup, string singlePrice, string quantity)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\n");
            inputString.Append(textRow2);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...).</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        public void receipt_Sale_Minimum(string taxGroup, string singlePrice)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow1">Text, up to 42 bytes, containing a line describing the sale</param>
        /// <param name="textRow2">Text, up to 42 bytes, containing a second line describing the sale</param>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...).</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Text for the unit of measurement for the quantity, up to 8 characters, for instance, “kg”.</param>
        public void receipt_Sale_Un(string textRow1, string textRow2, string taxGroup, string singlePrice, string quantity, string measure)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\n");
            inputString.Append(textRow2);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow1">Text, up to 42 bytes, containing a line describing the sale</param>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...).</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Text for the unit of measurement for the quantity, up to 8 characters, for instance, “kg”.</param>
        public void receipt_Sale_TextRow1Un(string textRow1, string taxGroup, string singlePrice, string quantity, string measure)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();
        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow2">Text, up to 42 bytes, containing a second line describing the sale</param>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...).</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Text for the unit of measurement for the quantity, up to 8 characters, for instance, “kg”.</param>
        public void receipt_Sale_TextRow2Un(string textRow2, string taxGroup, string singlePrice, string quantity, string measure)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\n");
            inputString.Append(textRow2);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();
        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...).</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Text for the unit of measurement for the quantity, up to 8 characters, for instance, “kg”.</param>
        public void receipt_Sale_UnWText(string taxGroup, string singlePrice, string quantity, string measure)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();
        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow1">Text, up to 42 bytes, containing a line describing the sale</param>
        /// <param name="textRow2">Text, up to 42 bytes, containing a second line describing the sale</param>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...).</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="percent">This is an optional parameter indicating the value of the mark-up or discount (depending on the sign),\n
        /// as a percentage of the current sale.The allowable values are between - 99.00% and 99.00%. Up to 2 decimal digits are accepted.</param>
        public void receipt_Sale_CByPercent(string textRow1, string textRow2, string taxGroup, string singlePrice, string quantity, string percent)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\n");
            inputString.Append(textRow2);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow1">Text, up to 42 bytes, containing a line describing the sale</param>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...).</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="percent">This is an optional parameter indicating the value of the mark-up or discount (depending on the sign),\n
        /// as a percentage of the current sale.The allowable values are between - 99.00% and 99.00%. Up to 2 decimal digits are accepted.</param>
        public void receipt_Sale_TextRow1CByPercent(string textRow1, string taxGroup, string singlePrice, string quantity, string percent)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();
        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow2">Text, up to 42 bytes, containing a second line describing the sale</param>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...).</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="percent">This is an optional parameter indicating the value of the mark-up or discount (depending on the sign),\n
        /// as a percentage of the current sale.The allowable values are between - 99.00% and 99.00%. Up to 2 decimal digits are accepted.</param>
        public void receipt_Sale_TextRow2CByPercent(string textRow2, string taxGroup, string singlePrice, string quantity, string percent)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\n");
            inputString.Append(textRow2);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...).</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="percent">This is an optional parameter indicating the value of the mark-up or discount (depending on the sign),\n
        /// as a percentage of the current sale.The allowable values are between - 99.00% and 99.00%. Up to 2 decimal digits are accepted.</param>
        public void receipt_Sale_CByPercentWText(string taxGroup, string singlePrice, string quantity, string percent)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow1">Text, up to 42 bytes, containing a line describing the sale</param>
        /// <param name="textRow2">Text, up to 42 bytes, containing a second line describing the sale</param>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...).</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Text for the unit of measurement for the quantity, up to 8 characters, for instance, “kg”.</param>
        /// <param name="percent">This is an optional parameter indicating the value of the mark-up or discount (depending on the sign),\n
        /// as a percentage of the current sale.The allowable values are between - 99.00% and 99.00%. Up to 2 decimal digits are accepted.</param>
        public void receipt_Sale_UnCByPercent(string textRow1, string textRow2, string taxGroup, string singlePrice, string quantity, string measure, string percent)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\n");
            inputString.Append(textRow2);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow1">Text, up to 42 bytes, containing a line describing the sale</param>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...).</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Text for the unit of measurement for the quantity, up to 8 characters, for instance, “kg”.</param>
        /// <param name="percent">This is an optional parameter indicating the value of the mark-up or discount (depending on the sign),\n
        /// as a percentage of the current sale.The allowable values are between - 99.00% and 99.00%. Up to 2 decimal digits are accepted.</param>
        public void receipt_Sale_TextRow1UnCByPercent(string textRow1, string taxGroup, string singlePrice, string quantity, string measure, string percent)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow2">Text, up to 42 bytes, containing a second line describing the sale</param>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...).</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Text for the unit of measurement for the quantity, up to 8 characters, for instance, “kg”.</param>
        /// <param name="percent">This is an optional parameter indicating the value of the mark-up or discount (depending on the sign),\n
        /// as a percentage of the current sale.The allowable values are between - 99.00% and 99.00%. Up to 2 decimal digits are accepted.</param>
        public void receipt_Sale_TextRow2UnCByPercent(string textRow2, string taxGroup, string singlePrice, string quantity, string measure, string percent)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\n");
            inputString.Append(textRow2);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...).</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Text for the unit of measurement for the quantity, up to 8 characters, for instance, “kg”.</param>
        /// <param name="percent">This is an optional parameter indicating the value of the mark-up or discount (depending on the sign),\n
        /// as a percentage of the current sale.The allowable values are between - 99.00% and 99.00%. Up to 2 decimal digits are accepted.</param>
        public void receipt_Sale_UnCByPercentWText(string taxGroup, string singlePrice, string quantity, string measure, string percent)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow1">Text, up to 42 bytes, containing a line describing the sale</param>
        /// <param name="textRow2">Text, up to 42 bytes, containing a second line describing the sale</param>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...).</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="abs">This is an optional parameter indicating the value of the mark-up or discount (depending on the sign), as an amount.A discount\n
        /// amount exceeding the value of the sale is not allowed.</param>
        public void receipt_Sale_CBySum(string textRow1, string textRow2, string taxGroup, string singlePrice, string quantity, string abs)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\n");
            inputString.Append(textRow2);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(";");
            inputString.Append(abs);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow1">Text, up to 42 bytes, containing a line describing the sale</param>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...).</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="abs">This is an optional parameter indicating the value of the mark-up or discount (depending on the sign), as an amount.A discount\n
        /// amount exceeding the value of the sale is not allowed.</param>
        public void receipt_Sale_TextRow1CBySum(string textRow1, string taxGroup, string singlePrice, string quantity, string abs)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(";");
            inputString.Append(abs);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow2">Text, up to 42 bytes, containing a second line describing the sale</param>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...).</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="abs">This is an optional parameter indicating the value of the mark-up or discount (depending on the sign), as an amount.A discount\n
        /// amount exceeding the value of the sale is not allowed.</param>
        public void receipt_Sale_TextRow2CBySum(string textRow2, string taxGroup, string singlePrice, string quantity, string abs)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\n");
            inputString.Append(textRow2);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(";");
            inputString.Append(abs);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...).</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="abs">This is an optional parameter indicating the value of the mark-up or discount (depending on the sign), as an amount.A discount\n
        /// amount exceeding the value of the sale is not allowed.</param>
        public void receipt_Sale_CBySumWText(string taxGroup, string singlePrice, string quantity, string abs)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(";");
            inputString.Append(abs);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();
        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow1">Text, up to 42 bytes, containing a line describing the sale</param>
        /// <param name="textRow2">Text, up to 42 bytes, containing a second line describing the sale</param>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...).</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Text for the unit of measurement for the quantity, up to 8 characters, for instance, “kg”.</param>
        /// <param name="abs">This is an optional parameter indicating the value of the mark-up or discount (depending on the sign), as an amount.A discount\n
        /// amount exceeding the value of the sale is not allowed.</param>
        public void receipt_Sale_UnCBySum(string textRow1, string textRow2, string taxGroup, string singlePrice, string quantity, string measure, string abs)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\n");
            inputString.Append(textRow2);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);
            inputString.Append(";");
            inputString.Append(abs);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow1">Text, up to 42 bytes, containing a line describing the sale</param>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...).</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Text for the unit of measurement for the quantity, up to 8 characters, for instance, “kg”.</param>
        /// <param name="abs">This is an optional parameter indicating the value of the mark-up or discount (depending on the sign), as an amount.A discount\n
        /// amount exceeding the value of the sale is not allowed.</param>
        public void receipt_Sale_TextRow1UnCBySum(string textRow1, string taxGroup, string singlePrice, string quantity, string measure, string abs)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);
            inputString.Append(";");
            inputString.Append(abs);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow2">Text, up to 42 bytes, containing a second line describing the sale</param>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...).</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Text for the unit of measurement for the quantity, up to 8 characters, for instance, “kg”.</param>
        /// <param name="abs">This is an optional parameter indicating the value of the mark-up or discount (depending on the sign), as an amount.A discount\n
        /// amount exceeding the value of the sale is not allowed.</param>
        public void receipt_Sale_TextRow2UnCBySum(string textRow2, string taxGroup, string singlePrice, string quantity, string measure, string abs)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\n");
            inputString.Append(textRow2);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);
            inputString.Append(";");
            inputString.Append(abs);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...).</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Text for the unit of measurement for the quantity, up to 8 characters, for instance, “kg”.</param>
        /// <param name="abs">This is an optional parameter indicating the value of the mark-up or discount (depending on the sign), as an amount.A discount\n
        /// amount exceeding the value of the sale is not allowed.</param>
        public void receipt_Sale_UnCBySumWText(string taxGroup, string singlePrice, string quantity, string measure, string abs)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);
            inputString.Append(";");
            inputString.Append(abs);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow1">Text, up to 42 bytes, containing a line describing the sale</param>
        /// <param name="textRow2">Text, up to 42 bytes, containing a second line describing the sale</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the\n
        /// tax group associated with the department when it is programmed</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        public void receipt_DSale(string textRow1, string textRow2, string department, string singlePrice, string quantity)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\n");
            inputString.Append(textRow2);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow1">Text, up to 42 bytes, containing a line describing the sale</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the\n
        /// tax group associated with the department when it is programmed</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        public void receipt_DSale_TextRow1(string textRow1, string department, string singlePrice, string quantity)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow1">Text, up to 42 bytes, containing a line describing the sale</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the\n
        /// tax group associated with the department when it is programmed</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        public void receipt_DSale_TextRow1_WQuan(string textRow1, string department, string singlePrice)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();
        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow2">Text, up to 42 bytes, containing a second line describing the sale</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the\n
        /// tax group associated with the department when it is programmed</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        public void receipt_DSale_TextRow2(string textRow2, string department, string singlePrice, string quantity)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\n");
            inputString.Append(textRow2);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the\n
        /// tax group associated with the department when it is programmed</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        public void receipt_DSale_Minimum(string department, string singlePrice)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow1">Text, up to 42 bytes, containing a line describing the sale</param>
        /// <param name="textRow2">Text, up to 42 bytes, containing a second line describing the sale</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the\n
        /// tax group associated with the department when it is programmed</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Optional text for the unit of measurement for the\n
        /// quantity, up to 8 characters, for instance, “kg”.</param>
        public void receipt_DSale_Un(string textRow1, string textRow2, string department, string singlePrice, string quantity, string measure)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\n");
            inputString.Append(textRow2);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow1">Text, up to 42 bytes, containing a line describing the sale</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the\n
        /// tax group associated with the department when it is programmed</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Optional text for the unit of measurement for the\n
        /// quantity, up to 8 characters, for instance, “kg”.</param>
        public void receipt_DSale_TextRow1Un(string textRow1, string department, string singlePrice, string quantity, string measure)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow2">Text, up to 42 bytes, containing a second line describing the sale</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the\n
        /// tax group associated with the department when it is programmed</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Optional text for the unit of measurement for the\n
        /// quantity, up to 8 characters, for instance, “kg”.</param>
        public void receipt_DSale_TextRow2Un(string textRow2, string department, string singlePrice, string quantity, string measure)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\n");
            inputString.Append(textRow2);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        ///  Enter a product (sale)
        /// </summary>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the\n
        /// tax group associated with the department when it is programmed</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Optional text for the unit of measurement for the\n
        /// quantity, up to 8 characters, for instance, “kg”.</param>
        public void receipt_DSale_UnWText(string department, string singlePrice, string quantity, string measure)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow1">Text, up to 42 bytes, containing a line describing the sale</param>
        /// <param name="textRow2">Text, up to 42 bytes, containing a second line describing the sale</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the\n
        /// tax group associated with the department when it is programmed</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="percent">Parameter indicating the value of the mark-up or discount (depending on the sign), as a percentage of the current sale.\n
        /// The allowable values are between - 99.00% and 99.00%. Up to 2 decimal digits are accepted</param>
        public void receipt_DSale_CByPercent(string textRow1, string textRow2, string department, string singlePrice, string quantity, string percent)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\n");
            inputString.Append(textRow2);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow1">Text, up to 42 bytes, containing a line describing the sale</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the\n
        /// tax group associated with the department when it is programmed</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="percent">Parameter indicating the value of the mark-up or discount (depending on the sign), as a percentage of the current sale.\n
        /// The allowable values are between - 99.00% and 99.00%. Up to 2 decimal digits are accepted</param>
        public void receipt_DSale_TextRow1CByPercent(string textRow1, string department, string singlePrice, string quantity, string percent)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow2">Text, up to 42 bytes, containing a second line describing the sale</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the\n
        /// tax group associated with the department when it is programmed</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="percent">Parameter indicating the value of the mark-up or discount (depending on the sign), as a percentage of the current sale.\n
        /// The allowable values are between - 99.00% and 99.00%. Up to 2 decimal digits are accepted</param>
        public void receipt_DSale_TextRow2CByPercent(string textRow2, string department, string singlePrice, string quantity, string percent)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\n");
            inputString.Append(textRow2);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the\n
        /// tax group associated with the department when it is programmed</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="percent">Parameter indicating the value of the mark-up or discount (depending on the sign), as a percentage of the current sale.\n
        /// The allowable values are between - 99.00% and 99.00%. Up to 2 decimal digits are accepted</param>
        public void receipt_DSale_CByPercentWText(string department, string singlePrice, string quantity, string percent)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow1">Text, up to 42 bytes, containing a line describing the sale</param>
        /// <param name="textRow2">Text, up to 42 bytes, containing a second line describing the sale</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the\n
        /// tax group associated with the department when it is programmed</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Optional text for the unit of measurement for the\n
        /// quantity, up to 8 characters, for instance, “kg”.</param>
        /// <param name="percent">Parameter indicating the value of the mark-up or discount (depending on the sign), as a percentage of the current sale.\n
        /// The allowable values are between - 99.00% and 99.00%. Up to 2 decimal digits are accepted</param>
        public void receipt_DSale_UnCByPercent(string textRow1, string textRow2, string department, string singlePrice, string quantity, string measure, string percent)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\n");
            inputString.Append(textRow2);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow1">Text, up to 42 bytes, containing a line describing the sale</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the\n
        /// tax group associated with the department when it is programmed</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Optional text for the unit of measurement for the\n
        /// quantity, up to 8 characters, for instance, “kg”.</param>
        /// <param name="percent">Parameter indicating the value of the mark-up or discount (depending on the sign), as a percentage of the current sale.\n
        /// The allowable values are between - 99.00% and 99.00%. Up to 2 decimal digits are accepted</param>
        public void receipt_DSale_TextRow1UnCByPercent(string textRow1, string department, string singlePrice, string quantity, string measure, string percent)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow2">Text, up to 42 bytes, containing a second line describing the sale</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the\n
        /// tax group associated with the department when it is programmed</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Optional text for the unit of measurement for the\n
        /// quantity, up to 8 characters, for instance, “kg”.</param>
        /// <param name="percent">Parameter indicating the value of the mark-up or discount (depending on the sign), as a percentage of the current sale.\n
        /// The allowable values are between - 99.00% and 99.00%. Up to 2 decimal digits are accepted</param>
        public void receipt_DSale_TextRow2UnCByPercent(string textRow2, string department, string singlePrice, string quantity, string measure, string percent)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\n");
            inputString.Append(textRow2);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the\n
        /// tax group associated with the department when it is programmed</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Optional text for the unit of measurement for the\n
        /// quantity, up to 8 characters, for instance, “kg”.</param>
        /// <param name="percent">Parameter indicating the value of the mark-up or discount (depending on the sign), as a percentage of the current sale.\n
        /// The allowable values are between - 99.00% and 99.00%. Up to 2 decimal digits are accepted</param>
        public void receipt_DSale_UnCByPercentWText(string department, string singlePrice, string quantity, string measure, string percent)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow1">Text, up to 42 bytes, containing a line describing the sale</param>
        /// <param name="textRow2">Text, up to 42 bytes, containing a second line describing the sale</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the\n
        /// tax group associated with the department when it is programmed</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="abs">Parameter indicating the value of the mark-up or discount (depending on the sign), as an amount.\n
        /// A discount amount exceeding the value of the sale is not allowed.</param>
        public void receipt_DSale_CBySum(string textRow1, string textRow2, string department, string singlePrice, string quantity, string abs)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\n");
            inputString.Append(textRow2);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(";");
            inputString.Append(abs);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow1">Text, up to 42 bytes, containing a line describing the sale</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the\n
        /// tax group associated with the department when it is programmed</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="abs">Parameter indicating the value of the mark-up or discount (depending on the sign), as an amount.\n
        /// A discount amount exceeding the value of the sale is not allowed.</param>
        public void receipt_DSale_TextRow1CBySum(string textRow1, string department, string singlePrice, string quantity, string abs)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(";");
            inputString.Append(abs);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow2">Text, up to 42 bytes, containing a second line describing the sale</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the\n
        /// tax group associated with the department when it is programmed</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="abs">Parameter indicating the value of the mark-up or discount (depending on the sign), as an amount.\n
        /// A discount amount exceeding the value of the sale is not allowed.</param>
        public void receipt_DSale_TextRow2CBySum(string textRow2, string department, string singlePrice, string quantity, string abs)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\n");
            inputString.Append(textRow2);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(";");
            inputString.Append(abs);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the\n
        /// tax group associated with the department when it is programmed</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="abs">Parameter indicating the value of the mark-up or discount (depending on the sign), as an amount.\n
        /// A discount amount exceeding the value of the sale is not allowed.</param>
        public void receipt_DSale_CBySumWText(string department, string singlePrice, string quantity, string abs)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(";");
            inputString.Append(abs);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow1">Text, up to 42 bytes, containing a line describing the sale</param>
        /// <param name="textRow2">Text, up to 42 bytes, containing a second line describing the sale</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the\n
        /// tax group associated with the department when it is programmed</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Optional text for the unit of measurement for the\n
        /// quantity, up to 8 characters, for instance, “kg”.</param>
        /// <param name="abs">Parameter indicating the value of the mark-up or discount (depending on the sign), as an amount.\n
        /// A discount amount exceeding the value of the sale is not allowed.</param>
        public void receipt_DSale_UnCBySum(string textRow1, string textRow2, string department, string singlePrice, string quantity, string measure, string abs)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\n");
            inputString.Append(textRow2);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);
            inputString.Append(";");
            inputString.Append(abs);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow1">Text, up to 42 bytes, containing a line describing the sale</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the\n
        /// tax group associated with the department when it is programmed</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Optional text for the unit of measurement for the\n
        /// quantity, up to 8 characters, for instance, “kg”.</param>
        /// <param name="abs">Parameter indicating the value of the mark-up or discount (depending on the sign), as an amount.\n
        /// A discount amount exceeding the value of the sale is not allowed.</param>
        public void receipt_DSale_TextRow1UnCBySum(string textRow1, string department, string singlePrice, string quantity, string measure, string abs)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);
            inputString.Append(";");
            inputString.Append(abs);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="textRow2">Text, up to 42 bytes, containing a second line describing the sale</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the\n
        /// tax group associated with the department when it is programmed</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Optional text for the unit of measurement for the\n
        /// quantity, up to 8 characters, for instance, “kg”.</param>
        /// <param name="abs">Parameter indicating the value of the mark-up or discount (depending on the sign), as an amount.\n
        /// A discount amount exceeding the value of the sale is not allowed.</param>
        public void receipt_DSale_TextRow2UnCBySum(string textRow2, string department, string singlePrice, string quantity, string measure, string abs)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\n");
            inputString.Append(textRow2);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);
            inputString.Append(";");
            inputString.Append(abs);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Enter a product (sale)
        /// </summary>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the\n
        /// tax group associated with the department when it is programmed</param>
        /// <param name="singlePrice">This is the unit price, with up to 8 significant digits and sign</param>
        /// <param name="quantity">By default, it is 1.000. Length, up to 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Optional text for the unit of measurement for the\n
        /// quantity, up to 8 characters, for instance, “kg”.</param>
        /// <param name="abs">Parameter indicating the value of the mark-up or discount (depending on the sign), as an amount.\n
        /// A discount amount exceeding the value of the sale is not allowed.</param>
        public void receipt_DSale_UnCBySumWText(string department, string singlePrice, string quantity, string measure, string abs)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);
            inputString.Append(";");
            inputString.Append(abs);

            string r = CustomCommand(49, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 52 - please check fiscal device documentation.
        /// <summary>
        /// Register and display of a sale
        /// </summary>
        /// <param name="textRow">String, up to 20 bytes, containing a line of text describing the sale</param>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...)</param>
        /// <param name="singlePrice">This is the price, with up to 8 significant digits</param>
        /// <param name="quantity">Optional parameter indicating the product quantity. By default, it is 1.000. Length, up to 8 significant digits</param>
        public void receipt_DisplaySale(string textRow, string taxGroup, string singlePrice, string quantity)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);

            string r = CustomCommand(52, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 52 - please check fiscal device documentation.
        /// <summary>
        /// Register and display of a sale
        /// </summary>
        /// <param name="textRow">String, up to 20 bytes, containing a line of text describing the sale</param>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...)</param>
        /// <param name="singlePrice">This is the price, with up to 8 significant digits</param>
        /// <param name="quantity">Optional parameter indicating the product quantity. By default, it is 1.000. Length, up to 8 significant digits</param>
        /// <param name="measure">Name of the unit of measurement. Text for the unit of measurement for the quantity, up to 8 characters, for instance, “kg”.</param>
        public void receipt_DisplaySale_Un(string textRow, string taxGroup, string singlePrice, string quantity, string measure)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);

            string r = CustomCommand(52, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 52 - please check fiscal device documentation.
        /// <summary>
        /// Register and display of a sale
        /// </summary>
        /// <param name="textRow">String, up to 20 bytes, containing a line of text describing the sale</param>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...)</param>
        /// <param name="singlePrice">This is the price, with up to 8 significant digits</param>
        /// <param name="quantity">Optional parameter indicating the product quantity. By default, it is 1.000. Length, up to 8 significant digits</param>
        /// <param name="percent">parameter indicating the value of the mark-up or discount (depending on the sign), as a percentage of the\n
        /// current sale.The allowable values are between -99.00% and  99.00%..</param>
        public void receipt_DisplaySale_CByPercent(string textRow, string taxGroup, string singlePrice, string quantity, string percent)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(52, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 52 - please check fiscal device documentation.
        /// <summary>
        /// Register and display of a sale
        /// </summary>
        /// <param name="textRow">String, up to 20 bytes, containing a line of text describing the sale</param>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...)</param>
        /// <param name="singlePrice">This is the price, with up to 8 significant digits</param>
        /// <param name="quantity">Optional parameter indicating the product quantity. By default, it is 1.000. Length, up to 8 significant digits</param>
        /// <param name="measure">Name of the unit of measurement. Text for the unit of measurement for the quantity, up to 8 characters, for instance, “kg”.</param>
        /// <param name="percent">Parameter indicating the value of the mark-up or discount (depending on the sign), as a percentage of the\n
        /// current sale.The allowable values are between -99.00% and  99.00%..</param>
        public void receipt_DisplaySale_UnCByPercent(string textRow, string taxGroup, string singlePrice, string quantity, string measure, string percent)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(52, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 52 - please check fiscal device documentation.
        /// <summary>
        /// Register and display of a sale
        /// </summary>
        /// <param name="textRow">String, up to 20 bytes, containing a line of text describing the sale</param>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...)</param>
        /// <param name="singlePrice">This is the price, with up to 8 significant digits</param>
        /// <param name="quantity">Optional parameter indicating the product quantity. By default, it is 1.000. Length, up to 8 significant digits</param>
        /// <param name="abs">This is an optional parameter indicating the value of the mark-up or discount (depending on \n
        /// the sign), as an amount.A discount amount exceeding the value of the sale is not allowed</param>
        public void receipt_DisplaySale_CBySum(string textRow, string taxGroup, string singlePrice, string quantity, string abs)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(";");
            inputString.Append(abs);

            string r = CustomCommand(52, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 52 - please check fiscal device documentation.
        /// <summary>
        /// Register and display of a sale
        /// </summary>
        /// <param name="textRow">String, up to 20 bytes, containing a line of text describing the sale</param>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...)</param>
        /// <param name="singlePrice">This is the price, with up to 8 significant digits</param>
        /// <param name="quantity">Optional parameter indicating the product quantity. By default, it is 1.000. Length, up to 8 significant digits</param>
        /// <param name="measure">Name of the unit of measurement. Text for the unit of measurement for the quantity, up to 8 characters, for instance, “kg”.</param>
        /// <param name="abs">This is an optional parameter indicating the value of the mark-up or discount (depending on \n
        /// the sign), as an amount.A discount amount exceeding the value of the sale is not allowed</param>
        public void receipt_DisplaySale_UnCBySum(string textRow, string taxGroup, string singlePrice, string quantity, string measure, string abs)

        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);
            inputString.Append(";");
            inputString.Append(abs);

            string r = CustomCommand(52, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 52 - please check fiscal device documentation.
        /// <summary>
        /// Register and display of a sale
        /// </summary>
        /// <param name="textRow">String, up to 20 bytes, containing a line of text describing the sale</param>
        /// <param name="taxGroup">One byte containing the letter indicating the tax type (‘A’, ‘B’, ‘C’, ...)</param>
        /// <param name="singlePrice">This is the price, with up to 8 significant digits</param>
        public void receipt_DisplaySale_Minimum(string textRow, string taxGroup, string singlePrice)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);

            string r = CustomCommand(52, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 52 - please check fiscal device documentation.
        /// <summary>
        /// Register and display of a sale
        /// </summary>
        /// <param name="textRow">String, up to 20 bytes, containing a line of text describing the sale</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the tax\n
        /// group associated with the department when it is programmed.</param>
        /// <param name="singlePrice">This is the price, with up to 8 significant digits</param>
        /// <param name="quantity">Optional parameter indicating the product quantity. By default, it is 1.000. Length, up to 8 significant digits</param>
        public void receipt_DisplayDSale(string textRow, string department, string singlePrice, string quantity)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);

            string r = CustomCommand(52, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 52 - please check fiscal device documentation.
        /// <summary>
        /// Register and display of a sale
        /// </summary>
        /// <param name="textRow">String, up to 20 bytes, containing a line of text describing the sale</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the tax\n
        /// group associated with the department when it is programmed.</param>
        /// <param name="singlePrice">This is the price, with up to 8 significant digits</param>
        /// <param name="quantity">Optional parameter indicating the product quantity. By default, it is 1.000. Length, up to 8 significant digits</param>
        /// <param name="measure">Name of the unit of measurement. Text for the unit of measurement for the quantity, up to 8 characters, for instance, “kg”.</param>
        public void receipt_DisplayDSale_Un(string textRow, string department, string singlePrice, string quantity, string measure)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);

            string r = CustomCommand(52, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 52 - please check fiscal device documentation.
        /// <summary>
        /// Register and display of a sale
        /// </summary>
        /// <param name="textRow">String, up to 20 bytes, containing a line of text describing the sale</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the tax\n
        /// group associated with the department when it is programmed.</param>
        /// <param name="singlePrice">This is the price, with up to 8 significant digits</param>
        /// <param name="quantity">Optional parameter indicating the product quantity. By default, it is 1.000. Length, up to 8 significant digits</param>
        /// <param name="percent">Parameter indicating the value of the mark-up or discount (depending on the sign), as a percentage of the\n
        /// current sale.The allowable values are between -99.00% and 99.00%.</param>
        public void receipt_DisplayDSale_CByPercent(string textRow, string department, string singlePrice, string quantity, string percent)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(52, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 52 - please check fiscal device documentation.
        /// <summary>
        /// Register and display of a sale
        /// </summary>
        /// <param name="textRow">String, up to 20 bytes, containing a line of text describing the sale</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the tax\n
        /// group associated with the department when it is programmed.</param>
        /// <param name="singlePrice">This is the price, with up to 8 significant digits</param>
        /// <param name="quantity">Optional parameter indicating the product quantity. By default, it is 1.000. Length, up to 8 significant digits</param>
        /// <param name="measure">Name of the unit of measurement. Text for the unit of measurement for the quantity, up to 8 characters, for instance, “kg”.</param>
        /// <param name="percent">Parameter indicating the value of the mark-up or discount (depending on the sign), as a percentage of the\n
        /// current sale.The allowable values are between -99.00% and 99.00%.</param>
        public void receipt_DisplayDSale_UnCByPercent(string textRow, string department, string singlePrice, string quantity, string measure, string percent)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(52, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 52 - please check fiscal device documentation.
        /// <summary>
        /// Register and display of a sale
        /// </summary>
        /// <param name="textRow">String, up to 20 bytes, containing a line of text describing the sale</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the tax\n
        /// group associated with the department when it is programmed.</param>
        /// <param name="singlePrice">This is the price, with up to 8 significant digits</param>
        /// <param name="quantity">Optional parameter indicating the product quantity. By default, it is 1.000. Length, up to 8 significant digits</param>
        /// <param name="abs">Parameter indicating the value of the mark-up or discount (depending on  the sign),\n
        /// as an amount.A discount amount exceeding the value of the sale is not allowed.</param>
        public void receipt_DisplayDSale_CBySum(string textRow, string department, string singlePrice, string quantity, string abs)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(";");
            inputString.Append(abs);

            string r = CustomCommand(52, inputString.ToString());
            CheckResult();
        }

        // Command number(Dec): 52 - please check fiscal device documentation.
        /// <summary>
        /// Register and display of a sale
        /// </summary>
        /// <param name="textRow">String, up to 20 bytes, containing a line of text describing the sale</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the tax\n
        /// group associated with the department when it is programmed.</param>
        /// <param name="singlePrice">This is the price, with up to 8 significant digits</param>
        /// <param name="quantity">Optional parameter indicating the product quantity. By default, it is 1.000. Length, up to 8 significant digits</param>
        /// <param name="measure">Name of the unit of measurement. Text for the unit of measurement for the quantity, up to 8 characters, for instance, “kg”.</param>
        /// <param name="abs">Parameter indicating the value of the mark-up or discount (depending on  the sign),\n
        /// as an amount.A discount amount exceeding the value of the sale is not allowed.</param>
        public void receipt_DisplayDSale_UnCBySum(string textRow, string department, string singlePrice, string quantity, string measure, string abs)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);
            inputString.Append(";");
            inputString.Append(abs);

            string r = CustomCommand(52, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 52 - please check fiscal device documentation.
        /// <summary>
        /// Register and display of a sale
        /// </summary>
        /// <param name="textRow">String, up to 20 bytes, containing a line of text describing the sale</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. The sale is assigned to the tax\n
        /// group associated with the department when it is programmed.</param>
        /// <param name="singlePrice">This is the price, with up to 8 significant digits</param>
        public void receipt_DisplayDSale_Minimum(string textRow, string department, string singlePrice)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);

            string r = CustomCommand(52, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item
        /// </summary>
        /// <param name="targetPLU">This is the PLU code. Integer between 1 and 999999999 (up to 9 digits).</param>
        /// <param name="quantity">Parameter indicating the product quantity. By default, it is 1.000. Length, up to\n
        /// 8 significant digits(no more than 3 after the decimal point).</param>
        public void receipt_PLU_Sale(string targetPLU, string quantity)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(targetPLU);
            inputString.Append("*");
            inputString.Append(quantity);

            string r = CustomCommand(58, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item
        /// </summary>
        /// <param name="targetPLU">This is the PLU code. Integer between 1 and 999999999 (up to 9 digits).</param>
        /// <param name="quantity">Parameter indicating the product quantity. By default, it is 1.000. Length, up to\n
        /// 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="percent">This is an optional parameter indicating the value of the mark-up or discount (depending\n
        /// on the sign), as a percentage of the current sale.The allowable values are between - 99.00% and 99.00%. Up to 2 decimal digits are accepted</param>
        public void receipt_PLUSale_CByPercent(string targetPLU, string quantity, string percent)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(targetPLU);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(58, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item
        /// </summary>
        /// <param name="targetPLU">This is the PLU code. Integer between 1 and 999999999 (up to 9 digits).</param>
        /// <param name="quantity">Parameter indicating the product quantity. By default, it is 1.000. Length, up to\n
        /// 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="abs">Parameter indicating the value of the mark-up or discount (depending\n
        /// on the sign), as an amount.A discount amount exceeding the value of the sale is not allowed.</param>
        public void receipt_PLUSale_CBySum(string targetPLU, string quantity, string abs)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(targetPLU);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(";");
            inputString.Append(abs);

            string r = CustomCommand(58, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item
        /// </summary>
        /// <param name="targetPLU">This is the PLU code. Integer between 1 and 999999999 (up to 9 digits).</param>
        /// <param name="quantity">Parameter indicating the product quantity. By default, it is 1.000. Length, up to\n
        /// 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Optional text for the unit of measurement for the\n
        /// quantity, up to 8 characters, for instance, “kg”</param>
        public void receipt_PLUSale_Un(string targetPLU, string quantity, string measure)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(targetPLU);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);

            string r = CustomCommand(58, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item
        /// </summary>
        /// <param name="targetPLU">This is the PLU code. Integer between 1 and 999999999 (up to 9 digits).</param>
        /// <param name="quantity">Parameter indicating the product quantity. By default, it is 1.000. Length, up to\n
        /// 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Optional text for the unit of measurement for the\n
        /// quantity, up to 8 characters, for instance, “kg”</param>
        /// <param name="percent">This is an optional parameter indicating the value of the mark-up or discount (depending\n
        /// on the sign), as a percentage of the current sale.The allowable values are between - 99.00% and 99.00%. Up to 2 decimal digits are accepted</param>
        public void receipt_PLUSale_UnCByPercent(string targetPLU, string quantity, string measure, string percent)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(targetPLU);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(58, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item
        /// </summary>
        /// <param name="targetPLU">This is the PLU code. Integer between 1 and 999999999 (up to 9 digits).</param>
        /// <param name="quantity">Parameter indicating the product quantity. By default, it is 1.000. Length, up to\n
        /// 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Optional text for the unit of measurement for the\n
        /// quantity, up to 8 characters, for instance, “kg”</param>
        /// <param name="abs">Parameter indicating the value of the mark-up or discount (depending\n
        /// on the sign), as an amount.A discount amount exceeding the value of the sale is not allowed.</param>
        public void receipt_PLUSale_UnCBySum(string targetPLU, string quantity, string measure, string abs)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(targetPLU);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);
            inputString.Append(";");
            inputString.Append(abs);

            string r = CustomCommand(58, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item
        /// </summary>
        /// <param name="targetPLU">This is the PLU code. Integer between 1 and 999999999 (up to 9 digits).</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. If this parameter is present,
        /// then the tab separators need to be there as well</param>
        /// <param name="quantity">Parameter indicating the product quantity. By default, it is 1.000. Length, up to\n
        /// 8 significant digits(no more than 3 after the decimal point).</param>
        public void receipt_PLUDep_Sale(string targetPLU, string department, string quantity)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(targetPLU);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append("*");
            inputString.Append(quantity);

            string r = CustomCommand(58, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item
        /// </summary>
        /// <param name="targetPLU">This is the PLU code. Integer between 1 and 999999999 (up to 9 digits).</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. If this parameter is present,
        /// then the tab separators need to be there as well</param>
        /// <param name="quantity">Parameter indicating the product quantity. By default, it is 1.000. Length, up to\n
        /// 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="percent">This is an optional parameter indicating the value of the mark-up or discount (depending\n
        /// on the sign), as a percentage of the current sale.The allowable values are between - 99.00% and 99.00%. Up to 2 decimal digits are accepted</param>
        public void receipt_PLUDep_Sale_CByPercent(string targetPLU, string department, string quantity, string percent)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(targetPLU);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(58, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item
        /// </summary>
        /// <param name="targetPLU">This is the PLU code. Integer between 1 and 999999999 (up to 9 digits).</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. If this parameter is present,
        /// then the tab separators need to be there as well</param>
        /// <param name="quantity">Parameter indicating the product quantity. By default, it is 1.000. Length, up to\n
        /// 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="abs">Parameter indicating the value of the mark-up or discount (depending\n
        /// on the sign), as an amount.A discount amount exceeding the value of the sale is not allowed.</param>
        public void receipt_PLUDep_Sale_CBySum(string targetPLU, string department, string quantity, string abs)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(targetPLU);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(";");
            inputString.Append(abs);

            string r = CustomCommand(58, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item
        /// </summary>
        /// <param name="targetPLU">This is the PLU code. Integer between 1 and 999999999 (up to 9 digits).</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. If this parameter is present,
        /// then the tab separators need to be there as well</param>
        /// <param name="quantity">Parameter indicating the product quantity. By default, it is 1.000. Length, up to\n
        /// 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Optional text for the unit of measurement for the\n
        /// quantity, up to 8 characters, for instance, “kg”</param>
        public void receipt_PLUDep_Sale_Un(string targetPLU, string department, string quantity, string measure)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(targetPLU);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);

            string r = CustomCommand(58, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item
        /// </summary>
        /// <param name="targetPLU">This is the PLU code. Integer between 1 and 999999999 (up to 9 digits).</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. If this parameter is present,
        /// then the tab separators need to be there as well</param>
        /// <param name="quantity">Parameter indicating the product quantity. By default, it is 1.000. Length, up to\n
        /// 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Optional text for the unit of measurement for the\n
        /// quantity, up to 8 characters, for instance, “kg”</param>
        /// <param name="percent">This is an optional parameter indicating the value of the mark-up or discount (depending\n
        /// on the sign), as a percentage of the current sale.The allowable values are between - 99.00% and 99.00%. Up to 2 decimal digits are accepted</param>
        public void receipt_PLUDep_Sale_UnCByPercent(string targetPLU, string department, string quantity, string measure, string percent)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(targetPLU);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(58, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item
        /// </summary>
        /// <param name="targetPLU">This is the PLU code. Integer between 1 and 999999999 (up to 9 digits).</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. If this parameter is present,
        /// then the tab separators need to be there as well</param>
        /// <param name="quantity">Parameter indicating the product quantity. By default, it is 1.000. Length, up to\n
        /// 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Optional text for the unit of measurement for the\n
        /// quantity, up to 8 characters, for instance, “kg”</param>
        /// <param name="abs">Parameter indicating the value of the mark-up or discount (depending\n
        /// on the sign), as an amount.A discount amount exceeding the value of the sale is not allowed.</param>
        public void receipt_PLUDep_Sale_UnCBySum(string targetPLU, string department, string quantity, string measure, string abs)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(targetPLU);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);
            inputString.Append(";");
            inputString.Append(abs);

            string r = CustomCommand(58, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item
        /// </summary>
        /// <param name="dChar">'D' - If present, the sale is shown on the customer display. If the\n
        /// length of the PLU name is more than 20, the characters after 20 are truncated.</param>
        /// <param name="targetPLU">This is the PLU code. Integer between 1 and 999999999 (up to 9 digits).</param>
        /// <param name="quantity">Parameter indicating the product quantity. By default, it is 1.000. Length, up to\n
        /// 8 significant digits(no more than 3 after the decimal point).</param>
        public void receipt_DisplayPLUSale(string dChar, string targetPLU, string quantity)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(dChar);
            inputString.Append(targetPLU);
            inputString.Append("*");
            inputString.Append(quantity);

            string r = CustomCommand(58, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        ///  Registering the sale of a programmed item
        /// </summary>
        /// <param name="dChar">'D' - If present, the sale is shown on the customer display. If the\n
        /// length of the PLU name is more than 20, the characters after 20 are truncated.</param>
        /// <param name="targetPLU">This is the PLU code. Integer between 1 and 999999999 (up to 9 digits).</param>
        /// <param name="quantity">Parameter indicating the product quantity. By default, it is 1.000. Length, up to\n
        /// 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="percent">This is an optional parameter indicating the value of the mark-up or discount (depending\n
        /// on the sign), as a percentage of the current sale.The allowable values are between - 99.00% and 99.00%. Up to 2 decimal digits are accepted</param>
        public void receipt_DisplayPLUSale_CByPercent(string dChar, string targetPLU, string quantity, string percent)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(dChar);
            inputString.Append(targetPLU);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(58, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        ///  Registering the sale of a programmed item
        /// </summary>
        /// <param name="dChar">'D' - If present, the sale is shown on the customer display. If the\n
        /// length of the PLU name is more than 20, the characters after 20 are truncated.</param>
        /// <param name="targetPLU">This is the PLU code. Integer between 1 and 999999999 (up to 9 digits).</param>
        /// <param name="quantity">Parameter indicating the product quantity. By default, it is 1.000. Length, up to\n
        /// 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="abs">Parameter indicating the value of the mark-up or discount (depending\n
        /// on the sign), as an amount.A discount amount exceeding the value of the sale is not allowed.</param>
        public void receipt_DisplayPLUSale_CBySum(string dChar, string targetPLU, string quantity, string abs)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(dChar);
            inputString.Append(targetPLU);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(";");
            inputString.Append(abs);

            string r = CustomCommand(58, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item
        /// </summary>
        /// <param name="dChar">'D' - If present, the sale is shown on the customer display. If the\n
        /// length of the PLU name is more than 20, the characters after 20 are truncated.</param>
        /// <param name="targetPLU">This is the PLU code. Integer between 1 and 999999999 (up to 9 digits).</param>
        /// <param name="quantity">Parameter indicating the product quantity. By default, it is 1.000. Length, up to\n
        /// 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Optional text for the unit of measurement for the\n
        /// quantity, up to 8 characters, for instance, “kg”</param>
        public void receipt_DisplayPLUSale_Un(string dChar, string targetPLU, string quantity, string measure)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(dChar);
            inputString.Append(targetPLU);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);

            string r = CustomCommand(58, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item
        /// </summary>
        /// <param name="dChar">'D' - If present, the sale is shown on the customer display. If the\n
        /// length of the PLU name is more than 20, the characters after 20 are truncated.</param>
        /// <param name="targetPLU">This is the PLU code. Integer between 1 and 999999999 (up to 9 digits).</param>
        /// <param name="quantity">Parameter indicating the product quantity. By default, it is 1.000. Length, up to\n
        /// 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Optional text for the unit of measurement for the\n
        /// quantity, up to 8 characters, for instance, “kg”</param>
        /// <param name="percent">This is an optional parameter indicating the value of the mark-up or discount (depending\n
        /// on the sign), as a percentage of the current sale.The allowable values are between - 99.00% and 99.00%. Up to 2 decimal digits are accepted</param>
        public void receipt_DisplayPLUSale_UnCByPercent(string dChar, string targetPLU, string quantity, string measure, string percent)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(dChar);
            inputString.Append(targetPLU);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(58, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item
        /// </summary>
        /// <param name="dChar">'D' - If present, the sale is shown on the customer display. If the\n
        /// length of the PLU name is more than 20, the characters after 20 are truncated.</param>
        /// <param name="targetPLU">This is the PLU code. Integer between 1 and 999999999 (up to 9 digits).</param>
        /// <param name="quantity">Parameter indicating the product quantity. By default, it is 1.000. Length, up to\n
        /// 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Optional text for the unit of measurement for the\n
        /// quantity, up to 8 characters, for instance, “kg”</param>
        /// <param name="abs">Parameter indicating the value of the mark-up or discount (depending\n
        /// on the sign), as an amount.A discount amount exceeding the value of the sale is not allowed.</param>
        public void receipt_DisplayPLUSale_UnCBySum(string dChar, string targetPLU, string quantity, string measure, string abs)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(dChar);
            inputString.Append(targetPLU);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);
            inputString.Append(";");
            inputString.Append(abs);

            string r = CustomCommand(58, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item
        /// </summary>
        /// <param name="dChar">'D' - If present, the sale is shown on the customer display. If the\n
        /// length of the PLU name is more than 20, the characters after 20 are truncated.</param>
        /// <param name="targetPLU">This is the PLU code. Integer between 1 and 999999999 (up to 9 digits).</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. If this parameter is present,\n
        /// then the tab separators need to be there as well.</param>
        /// <param name="quantity">Parameter indicating the product quantity. By default, it is 1.000. Length, up to\n
        /// 8 significant digits(no more than 3 after the decimal point).</param>
        public void receipt_DisplayDepPLU_Sale(string dChar, string targetPLU, string department, string quantity)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(dChar);
            inputString.Append(targetPLU);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append("*");
            inputString.Append(quantity);

            string r = CustomCommand(58, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item
        /// </summary>
        /// <param name="dChar">'D' - If present, the sale is shown on the customer display. If the\n
        /// length of the PLU name is more than 20, the characters after 20 are truncated.</param>
        /// <param name="targetPLU">This is the PLU code. Integer between 1 and 999999999 (up to 9 digits).</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. If this parameter is present,\n
        /// then the tab separators need to be there as well.</param>
        /// <param name="quantity">Parameter indicating the product quantity. By default, it is 1.000. Length, up to\n
        /// 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name = "percent" > This is an optional parameter indicating the value of the mark-up or discount(depending\n
        /// on the sign), as a percentage of the current sale.The allowable values are between - 99.00% and 99.00%. Up to 2 decimal digits are accepted</param>
        public void receipt_DisplayDepPLU_Sale_CByPercent(string dChar, string targetPLU, string department, string quantity, string percent)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(dChar);
            inputString.Append(targetPLU);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(58, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item
        /// </summary>
        /// <param name="dChar">'D' - If present, the sale is shown on the customer display. If the\n
        /// length of the PLU name is more than 20, the characters after 20 are truncated.</param>
        /// <param name="targetPLU">This is the PLU code. Integer between 1 and 999999999 (up to 9 digits).</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. If this parameter is present,\n
        /// then the tab separators need to be there as well.</param>
        /// <param name="quantity">Parameter indicating the product quantity. By default, it is 1.000. Length, up to\n
        /// 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="abs">Parameter indicating the value of the mark-up or discount (depending\n
        /// on the sign), as an amount.A discount amount exceeding the value of the sale is not allowed.</param>
        public void receipt_DisplayDepPLU_Sale_CBySum(string dChar, string targetPLU, string department, string quantity, string abs)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(dChar);
            inputString.Append(targetPLU);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(";");
            inputString.Append(abs);

            string r = CustomCommand(58, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item
        /// </summary>
        /// <param name="dChar">'D' - If present, the sale is shown on the customer display. If the\n
        /// length of the PLU name is more than 20, the characters after 20 are truncated.</param>
        /// <param name="targetPLU">This is the PLU code. Integer between 1 and 999999999 (up to 9 digits).</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. If this parameter is present,\n
        /// then the tab separators need to be there as well.</param>
        /// <param name="quantity">Parameter indicating the product quantity. By default, it is 1.000. Length, up to\n
        /// 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Optional text for the unit of measurement for the\n
        /// quantity, up to 8 characters, for instance, “kg”</param>
        public void receipt_DisplayDepPLU_SaleUn(string dChar, string targetPLU, string department, string quantity, string measure)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(dChar);
            inputString.Append(targetPLU);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);

            string r = CustomCommand(58, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item
        /// </summary>
        /// <param name="dChar">'D' - If present, the sale is shown on the customer display. If the\n
        /// length of the PLU name is more than 20, the characters after 20 are truncated.</param>
        /// <param name="targetPLU">This is the PLU code. Integer between 1 and 999999999 (up to 9 digits).</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. If this parameter is present,\n
        /// then the tab separators need to be there as well.</param>
        /// <param name="quantity">Parameter indicating the product quantity. By default, it is 1.000. Length, up to\n
        /// 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Optional text for the unit of measurement for the\n
        /// quantity, up to 8 characters, for instance, “kg”</param>
        /// <param name = "percent" > This is an optional parameter indicating the value of the mark-up or discount(depending\n
        /// on the sign), as a percentage of the current sale.The allowable values are between - 99.00% and 99.00%. Up to 2 decimal digits are accepted</param>
        public void receipt_DisplayDepPLU_SaleUn_CByPercent(string dChar, string targetPLU, string department, string quantity, string measure, string percent)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(dChar);
            inputString.Append(targetPLU);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(58, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item
        /// </summary>
        /// <param name="dChar">'D' - If present, the sale is shown on the customer display. If the\n
        /// length of the PLU name is more than 20, the characters after 20 are truncated.</param>
        /// <param name="targetPLU">This is the PLU code. Integer between 1 and 999999999 (up to 9 digits).</param>
        /// <param name="department">Department number. Integer between 1 and 1200 inclusive. If this parameter is present,\n
        /// then the tab separators need to be there as well.</param>
        /// <param name="quantity">Parameter indicating the product quantity. By default, it is 1.000. Length, up to\n
        /// 8 significant digits(no more than 3 after the decimal point).</param>
        /// <param name="measure">Name of the unit of measurement. Optional text for the unit of measurement for the\n
        /// quantity, up to 8 characters, for instance, “kg”</param>
        /// <param name="abs">Parameter indicating the value of the mark-up or discount (depending\n
        /// on the sign), as an amount.A discount amount exceeding the value of the sale is not allowed.</param>
        public void receipt_DisplayDepPLU_SaleUn_CBySum(string dChar, string targetPLU, string department, string quantity, string measure, string abs)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(dChar);
            inputString.Append(targetPLU);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append("#");
            inputString.Append(measure);
            inputString.Append(";");
            inputString.Append(abs);

            string r = CustomCommand(58, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 51 - please check fiscal device documentation.
        /// <summary>
        /// Subtotal
        /// </summary>
        /// <param name="toPrint">One byte: if it is ‘1’, the subtotal will be printed</param>
        /// <param name="toDisplay">One byte: if it is ‘1’, the subtotal will be displayed</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>subtotal</term>
        /// <description>The sum until now for the current fiscal receipt (up to 10 bytes)</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupA</term>
        /// <description>Turnover for tax group A (up to 10 bytes)</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupB</term>
        /// <description>Turnover for tax group B (up to 10 bytes)</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupC</term>
        /// <description>Turnover for tax group C (up to 10 bytes)</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupD</term>
        /// <description>Turnover for tax group D (up to 10 bytes)</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupE</term>
        /// <description>Turnover for tax group E (up to 10 bytes)</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupF</term>
        /// <description>Turnover for tax froup F (up to 10 bytes)</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupG</term>
        /// <description>Turnover for tax froup G (up to 10 bytes)</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupH</term>
        /// <description>Turnover for tax froup H (up to 10 bytes)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Subtotal(string toPrint, string toDisplay)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(toPrint);
            inputString.Append(toDisplay);

            string r = CustomCommand(51, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["subtotal"] = split[0];
            if (split.Length >= 2)
                result["sumTaxGroupA"] = split[1];
            if (split.Length >= 3)
                result["sumTaxGroupB"] = split[2];
            if (split.Length >= 4)
                result["sumTaxGroupC"] = split[3];
            if (split.Length >= 5)
                result["sumTaxGroupD"] = split[4];
            if (split.Length >= 6)
                result["sumTaxGroupE"] = split[5];
            if (split.Length >= 7)
                result["sumTaxGroupF"] = split[6];
            if (split.Length >= 8)
                result["sumTaxGroupG"] = split[7];
            if (split.Length >= 9)
                result["sumTaxGroupH"] = split[8];
            return result;
        }

        // Command number(Dec): 51 - please check fiscal device documentation.
        /// <summary>
        /// Subtotal
        /// </summary>
        /// <param name="toPrint">One byte: if it is ‘1’, the subtotal will be printed.</param>
        /// <param name="toDisplay">One byte: if it is ‘1’, the subtotal will be displayed.</param>
        /// <param name="percent">Parameter indicating the percentage value of the discount or mark-up on the sum accumulated until now</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>subtotal</term>
        /// <description>The sum until now for the current fiscal receipt (up to 10 bytes)</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupA</term>
        /// <description>Turnover for tax group A (up to 10 bytes)</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupB</term>
        /// <description>Turnover for tax group B (up to 10 bytes)</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupC</term>
        /// <description>Turnover for tax group C (up to 10 bytes)</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupD</term>
        /// <description>Turnover for tax group D (up to 10 bytes)</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupE</term>
        /// <description>Turnover for tax group E (up to 10 bytes)</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupF</term>
        /// <description>Turnover for tax froup F (up to 10 bytes)</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupG</term>
        /// <description>Turnover for tax froup G (up to 10 bytes)</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupH</term>
        /// <description>Turnover for tax froup H (up to 10 bytes)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Subtotal_CByPercent(string toPrint, string toDisplay, string percent)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(toPrint);
            inputString.Append(toDisplay);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(51, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["subtotal"] = split[0];
            if (split.Length >= 2)
                result["sumTaxGroupA"] = split[1];
            if (split.Length >= 3)
                result["sumTaxGroupB"] = split[2];
            if (split.Length >= 4)
                result["sumTaxGroupC"] = split[3];
            if (split.Length >= 5)
                result["sumTaxGroupD"] = split[4];
            if (split.Length >= 6)
                result["sumTaxGroupE"] = split[5];
            if (split.Length >= 7)
                result["sumTaxGroupF"] = split[6];
            if (split.Length >= 8)
                result["sumTaxGroupG"] = split[7];
            if (split.Length >= 9)
                result["sumTaxGroupH"] = split[8];
            return result;
        }

        // Command number(Dec): 51 - please check fiscal device documentation.
        /// <summary>
        /// Subtotal
        /// </summary>
        /// <param name="toPrint">One byte: if it is ‘1’, the subtotal will be printed.</param>
        /// <param name="toDisplay">One byte: if it is ‘1’, the subtotal will be displayed.</param>
        /// <param name="abs">Parameter indicating the value of the mark-up or discount (depending\n
        /// on the sign), as an amount.A discount amount exceeding the value of the sale is not allowed.</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>subtotal</term>
        /// <description>The sum until now for the current fiscal receipt (up to 10 bytes)</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupA</term>
        /// <description>Turnover for tax group A (up to 10 bytes)</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupB</term>
        /// <description>Turnover for tax group B (up to 10 bytes)</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupC</term>
        /// <description>Turnover for tax group C (up to 10 bytes)</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupD</term>
        /// <description>Turnover for tax group D (up to 10 bytes)</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupE</term>
        /// <description>Turnover for tax group E (up to 10 bytes)</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupF</term>
        /// <description>Turnover for tax froup F (up to 10 bytes)</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupG</term>
        /// <description>Turnover for tax froup G (up to 10 bytes)</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupH</term>
        /// <description>Turnover for tax froup H (up to 10 bytes)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Subtotal_CBySum(string toPrint, string toDisplay, string abs)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(toPrint);
            inputString.Append(toDisplay);
            inputString.Append(";");
            inputString.Append(abs);

            string r = CustomCommand(51, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["subtotal"] = split[0];
            if (split.Length >= 2)
                result["sumTaxGroupA"] = split[1];
            if (split.Length >= 3)
                result["sumTaxGroupB"] = split[2];
            if (split.Length >= 4)
                result["sumTaxGroupC"] = split[3];
            if (split.Length >= 5)
                result["sumTaxGroupD"] = split[4];
            if (split.Length >= 6)
                result["sumTaxGroupE"] = split[5];
            if (split.Length >= 7)
                result["sumTaxGroupF"] = split[6];
            if (split.Length >= 8)
                result["sumTaxGroupG"] = split[7];
            if (split.Length >= 9)
                result["sumTaxGroupH"] = split[8];
            return result;
        }

        public void execute_Total(string textRow1, string textRow2, string paidMode, string inputAmount)
        {

            if (Y(textRow1) && Y(textRow2) && Y(paidMode) && Y(inputAmount)) receipt_Total_PAmount(textRow1, textRow2, paidMode, inputAmount);
            if (Y(textRow1) && N(textRow2) && Y(paidMode) && Y(inputAmount)) receipt_Total_PAmountTextRow1(textRow1, paidMode, inputAmount);
            if (N(textRow1) && Y(textRow2) && Y(paidMode) && Y(inputAmount)) receipt_Total_PAmountTextRow2(textRow2, paidMode, inputAmount);
            if (N(textRow1) && N(textRow2) && Y(paidMode) && Y(inputAmount)) receipt_Total_PAmountWithoutText(paidMode, inputAmount);
            if (Y(textRow1) && Y(textRow2) && N(paidMode) && N(inputAmount)) receipt_Total(textRow1, textRow2);
            if (Y(textRow1) && N(textRow2) && N(paidMode) && N(inputAmount)) receipt_Total_TextRow1(textRow1);
            if (N(textRow1) && Y(textRow2) && N(paidMode) && N(inputAmount)) receipt_Total_TextRow2(textRow2);
            if (N(textRow1) && N(textRow2) && N(paidMode) && N(inputAmount)) receipt_Total_WithoutText();

        }

        // Command number(Dec): 53 - please check fiscal device documentation.
        /// <summary>
        /// Total
        /// </summary>
        /// <param name="textRow1">Text, up to 36 bytes, containing the first line</param>
        /// <param name="textRow2">Text, up to 36 bytes, containing the second line</param>
        /// <param name="paidMode">Optional code indicating the payment method: ‘P’ - Cash payment (default); ‘N’ - Credit payment;
        /// ‘C’ - Check payment; ‘D’ - Debit card payment; ‘I’ - Programmable payment type 1; ‘J’ - Programmable payment type 2; 
        /// ‘K’ - Programmable payment type 3; ‘L’ - Programmable payment type 4; ‘i’ - Programmable payment type 1; ‘j’ - Programmable payment type 2
        /// ‘k’ - Programmable payment type 3; ‘l’ - Programmable payment type 4; ‘m’ - Coupons; ‘n’ - External coupons; ‘o’ - Packing; ‘p’ - Internal service
        /// ‘q’ - Damages; ‘r’ - Bank transfers; ‘s’ - Check</param>
        /// <param name="inputAmount">The amount payable (up to 10 significant digits).</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>deviceCode</term>
        /// <description>‘F’ Error; ‘E’ The calculated subtotal is negative.No payment is made and Amount will contain the negative subtotal\n
        /// ‘D’ If the amount paid is less than the amount on the receipt.The remaining amount to  be paid is returned to Amount.
        /// ‘R’ If the amount paid is more than the sum on the receipt.A “CHANGE” message will be printed and the change is returned to Amount.
        /// ‘I’ The sum under any tax group is negative, causing an error.The current subtotal is returned to Amount.</description>
        /// </item>
        /// <item>
        /// <term>outputAmount</term>
        /// <description>Up to 9 digits with a sign. Depends on 'deviceCode'</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Total_PAmount(string textRow1, string textRow2, string paidMode, string inputAmount)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\n");
            inputString.Append(textRow2);
            inputString.Append("\t");
            inputString.Append(paidMode);
            inputString.Append(inputAmount);

            string r = CustomCommand(53, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["deviceCode"] = split[0];
            if (split.Length >= 2)
                result["outputAmount"] = split[1];
            return result;
        }

        // Command number(Dec): 53 - please check fiscal device documentation.
        /// <summary>
        /// 
        /// </summary>
        /// <param name="textRow1">Text, up to 36 bytes, containing the first line</param>
        /// <param name="paidMode">Optional code indicating the payment method: ‘P’ - Cash payment (default); ‘N’ - Credit payment;
        /// ‘C’ - Check payment; ‘D’ - Debit card payment; ‘I’ - Programmable payment type 1; ‘J’ - Programmable payment type 2; 
        /// ‘K’ - Programmable payment type 3; ‘L’ - Programmable payment type 4; ‘i’ - Programmable payment type 1; ‘j’ - Programmable payment type 2
        /// ‘k’ - Programmable payment type 3; ‘l’ - Programmable payment type 4; ‘m’ - Coupons; ‘n’ - External coupons; ‘o’ - Packing; ‘p’ - Internal service
        /// ‘q’ - Damages; ‘r’ - Bank transfers; ‘s’ - Check</param>
        /// <param name="inputAmount">The amount payable (up to 10 significant digits).</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>deviceCode</term>
        /// <description>‘F’ Error; ‘E’ The calculated subtotal is negative.No payment is made and Amount will contain the negative subtotal\n
        /// ‘D’ If the amount paid is less than the amount on the receipt.The remaining amount to  be paid is returned to Amount.
        /// ‘R’ If the amount paid is more than the sum on the receipt.A “CHANGE” message will be printed and the change is returned to Amount.
        /// ‘I’ The sum under any tax group is negative, causing an error.The current subtotal is returned to Amount.</description>
        /// </item>
        /// <item>
        /// <term>outputAmount</term>
        /// <description>Up to 9 digits with a sign. Depends on 'deviceCode'</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Total_PAmountTextRow1(string textRow1, string paidMode, string inputAmount)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\t");
            inputString.Append(paidMode);
            inputString.Append(inputAmount);

            string r = CustomCommand(53, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["deviceCode"] = split[0];
            if (split.Length >= 2)
                result["outputAmount"] = split[1];
            return result;
        }

        // Command number(Dec): 53 - please check fiscal device documentation.
        /// <summary>
        /// Total
        /// </summary>
        /// <param name="textRow2">Text, up to 36 bytes, containing the second line</param>
        /// <param name="paidMode">Optional code indicating the payment method: ‘P’ - Cash payment (default); ‘N’ - Credit payment;
        /// ‘C’ - Check payment; ‘D’ - Debit card payment; ‘I’ - Programmable payment type 1; ‘J’ - Programmable payment type 2; 
        /// ‘K’ - Programmable payment type 3; ‘L’ - Programmable payment type 4; ‘i’ - Programmable payment type 1; ‘j’ - Programmable payment type 2
        /// ‘k’ - Programmable payment type 3; ‘l’ - Programmable payment type 4; ‘m’ - Coupons; ‘n’ - External coupons; ‘o’ - Packing; ‘p’ - Internal service
        /// ‘q’ - Damages; ‘r’ - Bank transfers; ‘s’ - Check</param>
        /// <param name="inputAmount">The amount payable (up to 10 significant digits).</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>deviceCode</term>
        /// <description>‘F’ Error; ‘E’ The calculated subtotal is negative.No payment is made and Amount will contain the negative subtotal\n
        /// ‘D’ If the amount paid is less than the amount on the receipt.The remaining amount to  be paid is returned to Amount.
        /// ‘R’ If the amount paid is more than the sum on the receipt.A “CHANGE” message will be printed and the change is returned to Amount.
        /// ‘I’ The sum under any tax group is negative, causing an error.The current subtotal is returned to Amount.</description>
        /// </item>
        /// <item>
        /// <term>outputAmount</term>
        /// <description>Up to 9 digits with a sign. Depends on 'deviceCode'</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Total_PAmountTextRow2(string textRow2, string paidMode, string inputAmount)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\n");
            inputString.Append(textRow2);
            inputString.Append("\t");
            inputString.Append(paidMode);
            inputString.Append(inputAmount);

            string r = CustomCommand(53, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["deviceCode"] = split[0];
            if (split.Length >= 2)
                result["outputAmount"] = split[1];
            return result;
        }

        // Command number(Dec): 53 - please check fiscal device documentation.
        /// <summary>
        /// Total
        /// </summary>
        /// <param name="paidMode">Optional code indicating the payment method: ‘P’ - Cash payment (default); ‘N’ - Credit payment;
        /// ‘C’ - Check payment; ‘D’ - Debit card payment; ‘I’ - Programmable payment type 1; ‘J’ - Programmable payment type 2; 
        /// ‘K’ - Programmable payment type 3; ‘L’ - Programmable payment type 4; ‘i’ - Programmable payment type 1; ‘j’ - Programmable payment type 2
        /// ‘k’ - Programmable payment type 3; ‘l’ - Programmable payment type 4; ‘m’ - Coupons; ‘n’ - External coupons; ‘o’ - Packing; ‘p’ - Internal service
        /// ‘q’ - Damages; ‘r’ - Bank transfers; ‘s’ - Check</param>
        /// <param name="inputAmount">The amount payable (up to 10 significant digits).</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>deviceCode</term>
        /// <description>‘F’ Error; ‘E’ The calculated subtotal is negative.No payment is made and Amount will contain the negative subtotal\n
        /// ‘D’ If the amount paid is less than the amount on the receipt.The remaining amount to  be paid is returned to Amount.
        /// ‘R’ If the amount paid is more than the sum on the receipt.A “CHANGE” message will be printed and the change is returned to Amount.
        /// ‘I’ The sum under any tax group is negative, causing an error.The current subtotal is returned to Amount.</description>
        /// </item>
        /// <item>
        /// <term>outputAmount</term>
        /// <description>Up to 9 digits with a sign. Depends on 'deviceCode'</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Total_PAmountWithoutText(string paidMode, string inputAmount)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\t");
            inputString.Append(paidMode);
            inputString.Append(inputAmount);

            string r = CustomCommand(53, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["deviceCode"] = split[0];
            if (split.Length >= 2)
                result["outputAmount"] = split[1];
            return result;
        }

        // Command number(Dec): 53 - please check fiscal device documentation.
        /// <summary>
        /// Total
        /// </summary>
        /// <param name="textRow1">Text, up to 36 bytes, containing the first line</param>
        /// <param name="textRow2">Text, up to 36 bytes, containing the second line</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>deviceCode</term>
        /// <description>‘F’ Error; ‘E’ The calculated subtotal is negative.No payment is made and Amount will contain the negative subtotal\n
        /// ‘D’ If the amount paid is less than the amount on the receipt.The remaining amount to  be paid is returned to Amount.
        /// ‘R’ If the amount paid is more than the sum on the receipt.A “CHANGE” message will be printed and the change is returned to Amount.
        /// ‘I’ The sum under any tax group is negative, causing an error.The current subtotal is returned to Amount.</description>
        /// </item>
        /// <item>
        /// <term>outputAmount</term>
        /// <description>Up to 9 digits with a sign. Depends on 'deviceCode'</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Total(string textRow1, string textRow2)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\n");
            inputString.Append(textRow2);
            inputString.Append("\t");

            string r = CustomCommand(53, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["deviceCode"] = split[0];
            if (split.Length >= 2)
                result["outputAmount"] = split[1];
            return result;
        }

        // Command number(Dec): 53 - please check fiscal device documentation.
        /// <summary>
        /// Total
        /// </summary>
        /// <param name="textRow1">Text, up to 36 bytes, containing the first line</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>deviceCode</term>
        /// <description>‘F’ Error; ‘E’ The calculated subtotal is negative.No payment is made and Amount will contain the negative subtotal\n
        /// ‘D’ If the amount paid is less than the amount on the receipt.The remaining amount to  be paid is returned to Amount.
        /// ‘R’ If the amount paid is more than the sum on the receipt.A “CHANGE” message will be printed and the change is returned to Amount.
        /// ‘I’ The sum under any tax group is negative, causing an error.The current subtotal is returned to Amount.</description>
        /// </item>
        /// <item>
        /// <term>outputAmount</term>
        /// <description>Up to 9 digits with a sign. Depends on 'deviceCode'</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Total_TextRow1(string textRow1)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\t");

            string r = CustomCommand(53, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["deviceCode"] = split[0];
            if (split.Length >= 2)
                result["outputAmount"] = split[1];
            return result;
        }

        // Command number(Dec): 53 - please check fiscal device documentation.
        /// <summary>
        /// Total
        /// </summary>
        /// <param name="textRow2">Text, up to 36 bytes, containing the second line</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>deviceCode</term>
        /// <description>‘F’ Error; ‘E’ The calculated subtotal is negative.No payment is made and Amount will contain the negative subtotal\n
        /// ‘D’ If the amount paid is less than the amount on the receipt.The remaining amount to  be paid is returned to Amount.
        /// ‘R’ If the amount paid is more than the sum on the receipt.A “CHANGE” message will be printed and the change is returned to Amount.
        /// ‘I’ The sum under any tax group is negative, causing an error.The current subtotal is returned to Amount.</description>
        /// </item>
        /// <item>
        /// <term>outputAmount</term>
        /// <description>Up to 9 digits with a sign. Depends on 'deviceCode'</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Total_TextRow2(string textRow2)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\n");
            inputString.Append(textRow2);
            inputString.Append("\t");

            string r = CustomCommand(53, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["deviceCode"] = split[0];
            if (split.Length >= 2)
                result["outputAmount"] = split[1];
            return result;
        }

        // Command number(Dec): 53 - please check fiscal device documentation.
        /// <summary>
        /// Total
        /// </summary>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>deviceCode</term>
        /// <description>‘F’ Error; ‘E’ The calculated subtotal is negative.No payment is made and Amount will contain the negative subtotal\n
        /// ‘D’ If the amount paid is less than the amount on the receipt.The remaining amount to  be paid is returned to Amount.
        /// ‘R’ If the amount paid is more than the sum on the receipt.A “CHANGE” message will be printed and the change is returned to Amount.
        /// ‘I’ The sum under any tax group is negative, causing an error.The current subtotal is returned to Amount.</description>
        /// </item>
        /// <item>
        /// <term>outputAmount</term>
        /// <description>Up to 9 digits with a sign. Depends on 'deviceCode'</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Total_WithoutText()
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\t");

            string r = CustomCommand(53, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["deviceCode"] = split[0];
            if (split.Length >= 2)
                result["outputAmount"] = split[1];
            return result;
        }

        // Command number(Dec): 44 - please check fiscal device documentation.
        /// <summary>
        /// Paper feed
        /// </summary>
        /// <param name="linesCount">The number of lines to feed the paper. Must be a positive number, at least 99 (1 or 2 bytes). If the parameter is missing, the default 1 line is used.</param>
        public void receipt_Paper_Feed(string linesCount)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(linesCount);

            string r = CustomCommand(44, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 44 - please check fiscal device documentation.
        /// <summary>
        /// Paper feed (ONLY for models SK1-21F and SK1-31F)
        /// </summary>
        public void receipt_Paper_Feed_InnerContainer(string param)
        {
            StringBuilder inputString = new StringBuilder();
            inputString.Append(param);

            string r = CustomCommand(44, inputString.ToString());
            CheckResult();
        }

        // Command number(Dec): 45 - please check fiscal device documentation.
        /// <summary>
        /// Paper cut
        /// </summary>
        public void receipt_Paper_Cut()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(45, inputString.ToString());
            CheckResult();
        }

        // Command number(Dec): 54 - please check fiscal device documentation.
        /// <summary>
        /// Print free fiscal text
        /// </summary>
        /// <param name="inputText">Free text to print. The ‘#’ character is printed at the start and the end of the line. The text may be\n
        /// with any length, but only the characters fitting on the line are used(without raising a truncation error).</param>
        public void receipt_Fiscal_Text(string inputText)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(inputText);

            string r = CustomCommand(54, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 54 - please check fiscal device documentation.
        /// <summary>
        /// Print free fiscal text
        /// </summary>
        /// <param name="font">Integer between 0 and 3: '0' - 32 dots(4 mm) height, higher letters; '1' - 32 dots(4 mm) height, normal letters
        /// '2' 24 dots(3 mm) height; '3' - 16 dots(2 mm) height</param>
        /// <param name="flags">Between one and 3 letters: ‘B’, ‘H’ or ‘I’. Each may appear only once. Setting, respectively: B Bold(strong); H High(double height); I Italic(oblique)</param>
        /// <param name="inputText">Free text to print. The ‘#’ character is printed at the start and the end of the line. The text may be\n
        /// with any length, but only the characters fitting on the line are used(without raising a truncation error).</param>
        public void receipt_PFiscalText_01(string font, string flags, string inputText)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\t");
            inputString.Append(font);
            inputString.Append(flags);
            inputString.Append(",");
            inputString.Append(inputText);

            string r = CustomCommand(54, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 54 - please check fiscal device documentation.
        /// <summary>
        /// Print free fiscal text
        /// </summary>
        /// <param name="font">Integer between 0 and 3: '0' - 32 dots(4 mm) height, higher letters; '1' - 32 dots(4 mm) height, normal letters
        /// '2' 24 dots(3 mm) height; '3' - 16 dots(2 mm) height</param>
        /// <param name="inputText">Free text to print. The ‘#’ character is printed at the start and the end of the line. The text may be\n
        /// with any length, but only the characters fitting on the line are used(without raising a truncation error).</param>
        public void receipt_PFiscalText_02(string font, string inputText)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\t");
            inputString.Append(font);
            inputString.Append(",");
            inputString.Append(inputText);

            string r = CustomCommand(54, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 60 - please check fiscal device documentation.
        /// <summary>
        /// Cancel fiscal receipt
        /// </summary>
        public void receipt_Fiscal_Cancel()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(60, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 70 - please check fiscal device documentation.
        /// <summary>
        /// Cash in and cash out
        /// </summary>
        /// <param name="amount">The amount to be registered (up to 10 significant digits). Depending on the sign of the
        /// number, it is interpreted as a deposit or withdrawal</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>‘P’ The request is executed. If the requested amount is not zero, the printer prints a service receipt to register the transaction.
        /// ‘F’ The request is denied.This happens, if: The available cash is less than the requested service withdrawal; There is an open fiscal or service receipt.</description>
        /// </item>
        /// <item>
        /// <term>cashSum</term>
        /// <description>Available cash. The amount is increased both with this command and with each cash payment.</description>
        /// </item>
        /// <item>
        /// <term>servIn</term>
        /// <description>The sum of all “Cash in” commands</description>
        /// </item>
        /// <item>
        /// <term>servOut</term>
        /// <description>The sum of all “cash out” commands</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_CashIn_CashOut(string amount)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(amount);

            string r = CustomCommand(70, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["cashSum"] = split[1];
            if (split.Length >= 3)
                result["servIn"] = split[2];
            if (split.Length >= 4)
                result["servOut"] = split[3];
            return result;
        }

        // Command number(Dec): 84 - please check fiscal device documentation.
        /// <summary>
        /// Print barcode
        /// </summary>
        /// <param name="barcodeType">‘1’ EAN8. The data contain only digits and are 7 bytes long. The checksum is calculated by the printer.\n
        /// ‘2’ EAN13.The data contain only digits and are 12 bytes long. The checksum is calculated by the printer.\n
        /// ‘3’ Code 128. The data are characters with ASCII codes between 32 and 127. Their length is between 22 and 42 characters (depends on the contents—the maximum length is achieved\n
        /// when all characters are digits). The checksum is calculated by the printer.When printing on narrow paper, the data length is obviously smaller.\n
        /// ‘4’ ITF (Interleaved 2 of 5). The data contain only digits.
        /// ‘5’ ITF(Interleaved 2 of 5). The data contain only digits.The printer automatically generates and prints a checksum.
        /// ‘D’ Two-dimensional barcode Data Matrix. The data are any printable characters (with length up to 140).
        /// ‘Q’ Two-dimensional barcode QR Code.The data are any printable characters (with length up to 140).\n
        /// ‘P’ Two-dimensional barcode PDF417.The data are any printable characters(with length up to 140).</param>
        /// <param name="alignment">One byte: 'L', 'R' or 'C': left-, right-aligned or centred, respectively. It is allowed only in the\n
        /// two-dimensional barcodes.They are printed as text as well.</param>
        /// <param name="barcodeData">EAN8 - The data contain only digits and are 7 bytes long; EAN13 - The data contain only digits and are 12 bytes long
        /// Code 128 - The data are characters with ASCII codes between 32 and 127; ITF (Interleaved 2 of 5) - The data contain only digits\n
        /// ITF(Interleaved 2 of 5). The data contain only digits.The printer automatically generates and prints a checksum\n
        /// ‘D’ Two-dimensional barcode Data Matrix - data are any printable characters (with length up to 140)\n
        /// ‘Q’ Two-dimensional barcode QR Code - data are any printable characters (with length up to 140)
        /// ‘P’ Two-dimensional barcode PDF417 - data are any printable characters(with length up to 140)
        /// </param>
        public void receipt_Print_Barcode_01(string barcodeType, string alignment, string barcodeData)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(barcodeType);
            inputString.Append(alignment);
            inputString.Append(",");
            inputString.Append(barcodeData);

            string r = CustomCommand(84, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 84 - please check fiscal device documentation.
        /// <summary>
        /// Print barcode without text
        /// </summary>
        /// <param name="barcodeType">‘1’ EAN8. The data contain only digits and are 7 bytes long. The checksum is calculated by the printer.\n
        /// ‘2’ EAN13.The data contain only digits and are 12 bytes long. The checksum is calculated by the printer.\n
        /// ‘3’ Code 128. The data are characters with ASCII codes between 32 and 127. Their length is between 22 and 42 characters (depends on the contents—the maximum length is achieved\n
        /// when all characters are digits). The checksum is calculated by the printer.When printing on narrow paper, the data length is obviously smaller.\n
        /// ‘4’ ITF (Interleaved 2 of 5). The data contain only digits.
        /// ‘5’ ITF(Interleaved 2 of 5). The data contain only digits.The printer automatically generates and prints a checksum.
        /// ‘D’ Two-dimensional barcode Data Matrix. The data are any printable characters (with length up to 140).
        /// ‘Q’ Two-dimensional barcode QR Code.The data are any printable characters (with length up to 140).\n
        /// ‘P’ Two-dimensional barcode PDF417.The data are any printable characters(with length up to 140).</param>
        /// <param name="alignment">One byte: 'L', 'R' or 'C': left-, right-aligned or centred, respectively. It is allowed only in the\n
        /// two-dimensional barcodes.</param>
        /// <param name="barcodeData">EAN8 - The data contain only digits and are 7 bytes long; EAN13 - The data contain only digits and are 12 bytes long
        /// Code 128 - The data are characters with ASCII codes between 32 and 127; ITF (Interleaved 2 of 5) - The data contain only digits\n
        /// ITF(Interleaved 2 of 5). The data contain only digits.The printer automatically generates and prints a checksum\n
        /// ‘D’ Two-dimensional barcode Data Matrix - data are any printable characters (with length up to 140)\n
        /// ‘Q’ Two-dimensional barcode QR Code - data are any printable characters (with length up to 140)
        /// ‘P’ Two-dimensional barcode PDF417 - data are any printable characters(with length up to 140)
        /// </param>
        public void receipt_Print_Barcode_02(string barcodeType, string alignment, string barcodeData)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(barcodeType);
            inputString.Append(alignment);
            inputString.Append(";");
            inputString.Append(barcodeData);

            string r = CustomCommand(84, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 92 - please check fiscal device documentation.
        /// <summary>
        /// Print separating line
        /// </summary>
        /// <param name="lineType">'1' Fill with the '-' character; '2' Fill with alternating '-' and ' ' characters; '3' Fill with the '=' character.\n
        /// '4' Fill with the double-width '*' character</param>
        public void receipt_Separating_Line(string lineType)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(lineType);

            string r = CustomCommand(92, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 92 - please check fiscal device documentation.
        /// <summary>
        /// Print separating line. The text “not payable!” with a triple height is added unconditionally
        /// </summary>
        /// <param name="lineType">'W' - mask</param>
        /// <param name="linemask">bit mask:number btw 0 and 3 -  1: adds the text “under this receipt”; 2: adds a frame of double-width '*' characters above and below</param>
        public void receipt_Separating_LineW(string lineType, string linemask)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(lineType);
            inputString.Append(linemask);

            string r = CustomCommand(92, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 103 - please check fiscal device documentation.
        /// <summary>
        /// Information about current receipt
        /// </summary>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>canVd</term>
        /// <description>Is it possible refund.</description>
        /// </item>
        /// <item>
        /// <term>taxA</term>
        /// <description>Accumulated sum from tax group A.</description>
        /// </item>
        /// <item>
        /// <term>taxB</term>
        /// <description>Accumulated sum from tax group B.</description>
        /// </item>
        /// <item>
        /// <term>taxC</term>
        /// <description>Accumulated sum from tax group B.</description>
        /// </item>
        /// <item>
        /// <term>taxD</term>
        /// <description>Accumulated sum from tax group B.</description>
        /// </item>
        /// <item>
        /// <term>taxE</term>
        /// <description>Accumulated sum from tax group B.</description>
        /// </item>
        /// <item>
        /// <term>taxF</term>
        /// <description>Accumulated sum from tax group B.</description>
        /// </item>
        /// <item>
        /// <term>taxG</term>
        /// <description>Accumulated sum from tax group B.</description>
        /// </item>
        /// <item>
        /// <term>taxH</term>
        /// <description>Accumulated sum from tax group B.</description>
        /// </item>
        /// <item>
        /// <term>inv</term>
        /// <description>Is it open invoice? '0' - no, '1' - yes.</description>
        /// </item>
        /// <item>
        /// <term>invNumber</term>
        /// <description>Next invoice number (up to 10 digits).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Current_Info()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(103, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["canVd"] = split[0];
            if (split.Length >= 2)
                result["taxA"] = split[1];
            if (split.Length >= 3)
                result["taxB"] = split[2];
            if (split.Length >= 4)
                result["taxC"] = split[3];
            if (split.Length >= 5)
                result["taxD"] = split[4];
            if (split.Length >= 6)
                result["taxE"] = split[5];
            if (split.Length >= 7)
                result["taxF"] = split[6];
            if (split.Length >= 8)
                result["taxG"] = split[7];
            if (split.Length >= 9)
                result["taxH"] = split[8];
            if (split.Length >= 10)
                result["inv"] = split[9];
            if (split.Length >= 11)
                result["invNumber"] = split[10];
            return result;
        }

        // Command number(Dec): 106 - please check fiscal device documentation.
        /// <summary>
        /// Drawer opening
        /// </summary>
        /// <param name="mSec">The length of the impulse in milliseconds. ( 5...100 )</param>
        public void receipt_Drawer_KickOut(string mSec)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(mSec);

            string r = CustomCommand(106, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 109 - please check fiscal device documentation.
        /// <summary>
        /// Print duplicate receipt
        /// </summary>
        /// <param name="count">Duplicate receipt count (allowed value: 1)</param>
        public void receipt_Print_Duplicate(string count)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(count);

            string r = CustomCommand(109, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 34 - please check fiscal device documentation.
        /// <summary>
        /// Report of the registered service agreements
        /// </summary>
        /// <param name="option">'P'</param>
        /// <returns>
        /// Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description>'F' - failed or 'Left' - integer parameter: number of remaining service agreement registration fields.
        /// 'RegDtTm parameter' - Service agreement date and time: “DD-MM-YYYY hh:mm:ss”.
        /// EndDate parameter -  Last service agreement expiration date in the “DD-MM-YYYY” format.
        /// 'UIC parameter' 9 or 13 digits: service provider’s UIC.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_Service_Contracts(string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(34, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["fAnswer"] = split[0];
            return result;
        }

        // Command number(Dec): 69 - please check fiscal device documentation.
        /// <summary>
        /// Daily closure
        /// </summary>
        /// <param name="option">'0' - Z report or'2' - X report</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>closure</term>
        /// <description>The fiscal record number: 4 bytes.</description>
        /// </item>
        /// <item>
        /// <term>fMTotal</term>
        /// <description>The sum of all non-VAT sales: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumA</term>
        /// <description>The sums under each tax group A: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumB</term>
        /// <description>The sums under each tax group B: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumC</term>
        /// <description>The sums under each tax group C: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumD</term>
        /// <description>The sums under each tax group D: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumE</term>
        /// <description>The sums under each tax group E: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumF</term>
        /// <description>The sums under each tax group F: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumG</term>
        /// <description>The sums under each tax group G: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumH</term>
        /// <description>The sums under each tax group H: 12 bytes with a sign.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_DailyClosure_01(string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(69, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["closure"] = split[0];
            if (split.Length >= 2)
                result["fMTotal"] = split[1];
            if (split.Length >= 3)
                result["totalsumA"] = split[2];
            if (split.Length >= 4)
                result["totalsumB"] = split[3];
            if (split.Length >= 5)
                result["totalsumC"] = split[4];
            if (split.Length >= 6)
                result["totalsumD"] = split[5];
            if (split.Length >= 7)
                result["totalsumE"] = split[6];
            if (split.Length >= 8)
                result["totalsumF"] = split[7];
            if (split.Length >= 9)
                result["totalsumG"] = split[8];
            if (split.Length >= 10)
                result["totalsumH"] = split[9];
            return result;
        }

        // Command number(Dec): 69 - please check fiscal device documentation.
        /// <summary>
        /// Daily closure
        /// </summary>
        /// <param name="option">'0' - Z report</param>
        /// <param name="withoutClearOpInfo">'N' - disables the clearing of accumulated data by operator</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>closure</term>
        /// <description>The fiscal record number: 4 bytes.</description>
        /// </item>
        /// <item>
        /// <term>fMTotal</term>
        /// <description>The sum of all non-VAT sales: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumA</term>
        /// <description>The sums under each tax group A: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumB</term>
        /// <description>The sums under each tax group B: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumC</term>
        /// <description>The sums under each tax group C: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumD</term>
        /// <description>The sums under each tax group D: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumE</term>
        /// <description>The sums under each tax group E: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumF</term>
        /// <description>The sums under each tax group F: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumG</term>
        /// <description>The sums under each tax group G: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumH</term>
        /// <description>The sums under each tax group H: 12 bytes with a sign.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_DailyClosure_02(string option, string withoutClearOpInfo)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(withoutClearOpInfo);

            string r = CustomCommand(69, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["closure"] = split[0];
            if (split.Length >= 2)
                result["fMTotal"] = split[1];
            if (split.Length >= 3)
                result["totalsumA"] = split[2];
            if (split.Length >= 4)
                result["totalsumB"] = split[3];
            if (split.Length >= 5)
                result["totalsumC"] = split[4];
            if (split.Length >= 6)
                result["totalsumD"] = split[5];
            if (split.Length >= 7)
                result["totalsumE"] = split[6];
            if (split.Length >= 8)
                result["totalsumF"] = split[7];
            if (split.Length >= 9)
                result["totalsumG"] = split[8];
            if (split.Length >= 10)
                result["totalsumH"] = split[9];
            return result;
        }

        // Command number(Dec): 73 - please check fiscal device documentation.
        /// <summary>
        /// Full fiscal memory report by block number
        /// </summary>
        /// <param name="startNumber">Start fiscal block number. 4 bytes</param>
        /// <param name="endNumber">End fiscal block number. 4 bytes</param>
        public void report_FMByNumRange(string startNumber, string endNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(startNumber);
            inputString.Append(",");
            inputString.Append(endNumber);

            string r = CustomCommand(73, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 73 - please check fiscal device documentation.
        /// <summary>
        /// Full fiscal memory report by block number - prints also the checksum using the SHA-1 algorithm.
        /// </summary>
        /// <param name="startNumber">Start fiscal block number. 4 bytes</param>
        /// <param name="endNumber">End fiscal block number. 4 bytes</param>
        public void report_FMByNumRange_SHA1(string startNumber, string endNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("#");
            inputString.Append(startNumber);
            inputString.Append(",");
            inputString.Append(endNumber);

            string r = CustomCommand(73, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 79 - please check fiscal device documentation.
        /// <summary>
        /// Summary tax memory report from-to date
        /// </summary>
        /// <param name="fromDate">Start date: 6 bytes (DDMMYY)</param>
        /// <param name="toDate">End date: 6 bytes (DDMMYY)</param>
        public void report_FMByDateRange_Short(string fromDate, string toDate)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(fromDate);
            inputString.Append(",");
            inputString.Append(toDate);

            string r = CustomCommand(79, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 94 - please check fiscal device documentation.
        /// <summary>
        /// Full fiscal memory report by fiscal record date
        /// </summary>
        /// <param name="fromDate">The start fiscal record date. 6 bytes (DDMMYY).</param>
        /// <param name="toDate">The end fiscal record date. 6 bytes (DDMMYY).</param>
        public void report_FMByDateRange(string fromDate, string toDate)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(fromDate);
            inputString.Append(",");
            inputString.Append(toDate);

            string r = CustomCommand(94, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 94 - please check fiscal device documentation.
        /// <summary>
        /// Full fiscal memory report by fiscal record date - each Z-report, prints also the checksum using the SHA-1 algorithm.
        /// </summary>
        /// <param name="fromDate">The start fiscal record date. 6 bytes (DDMMYY).</param>
        /// <param name="toDate">The end fiscal record date. 6 bytes (DDMMYY).</param>
        public void report_FMByDateRange_SHA1(string fromDate, string toDate)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("#");
            inputString.Append(fromDate);
            inputString.Append(",");
            inputString.Append(toDate);

            string r = CustomCommand(94, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 95 - please check fiscal device documentation.
        /// <summary>
        /// Summary fiscal memory report for a period
        /// </summary>
        /// <param name="startNumber">Start fiscal record number.</param>
        /// <param name="endNumber">End fiscal record number.</param>
        public void report_FMByNumRange_Short(string startNumber, string endNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(startNumber);
            inputString.Append(",");
            inputString.Append(endNumber);

            string r = CustomCommand(95, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 105 - please check fiscal device documentation.
        /// <summary>
        /// Operator's report
        /// </summary>
        public void report_Operators()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(105, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 108 - please check fiscal device documentation.
        /// <summary>
        /// Extended daily financial report
        /// </summary>
        /// <param name="option">'0' - Z report or'2' - X report</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>closure</term>
        /// <description>The fiscal record number: 4 bytes.</description>
        /// </item>
        /// <item>
        /// <term>fMTotal</term>
        /// <description>The sum of all non-VAT sales: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumA</term>
        /// <description>The sums under each tax group A: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumB</term>
        /// <description>The sums under each tax group B: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumC</term>
        /// <description>The sums under each tax group C: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumD</term>
        /// <description>The sums under each tax group D: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumE</term>
        /// <description>The sums under each tax group E: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumF</term>
        /// <description>The sums under each tax group F: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumG</term>
        /// <description>The sums under each tax group G: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumH</term>
        /// <description>The sums under each tax group H: 12 bytes with a sign.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_ExtDailyClosure_01(string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(108, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["closure"] = split[0];
            if (split.Length >= 2)
                result["fMTotal"] = split[1];
            if (split.Length >= 3)
                result["totalsumA"] = split[2];
            if (split.Length >= 4)
                result["totalsumB"] = split[3];
            if (split.Length >= 5)
                result["totalsumC"] = split[4];
            if (split.Length >= 6)
                result["totalsumD"] = split[5];
            if (split.Length >= 7)
                result["totalsumE"] = split[6];
            if (split.Length >= 8)
                result["totalsumF"] = split[7];
            if (split.Length >= 9)
                result["totalsumG"] = split[8];
            if (split.Length >= 10)
                result["totalsumH"] = split[9];
            return result;
        }

        // Command number(Dec): 108 - please check fiscal device documentation.
        /// <summary>
        /// Extended daily financial report
        /// </summary>
        /// <param name = "option" > '0' - Z report</param>
        /// <param name="withoutClearOpInfo">'N' - disables the clearing of accumulated data by operator</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>closure</term>
        /// <description>The fiscal record number: 4 bytes.</description>
        /// </item>
        /// <item>
        /// <term>fMTotal</term>
        /// <description>The sum of all non-VAT sales: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumA</term>
        /// <description>The sums under each tax group A: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumB</term>
        /// <description>The sums under each tax group B: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumC</term>
        /// <description>The sums under each tax group C: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumD</term>
        /// <description>The sums under each tax group D: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumE</term>
        /// <description>The sums under each tax group E: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumF</term>
        /// <description>The sums under each tax group F: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumG</term>
        /// <description>The sums under each tax group G: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumH</term>
        /// <description>The sums under each tax group H: 12 bytes with a sign.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_ExtDailyClosure_02(string option, string withoutClearOpInfo)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(withoutClearOpInfo);

            string r = CustomCommand(108, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["closure"] = split[0];
            if (split.Length >= 2)
                result["fMTotal"] = split[1];
            if (split.Length >= 3)
                result["totalsumA"] = split[2];
            if (split.Length >= 4)
                result["totalsumB"] = split[3];
            if (split.Length >= 5)
                result["totalsumC"] = split[4];
            if (split.Length >= 6)
                result["totalsumD"] = split[5];
            if (split.Length >= 7)
                result["totalsumE"] = split[6];
            if (split.Length >= 8)
                result["totalsumF"] = split[7];
            if (split.Length >= 9)
                result["totalsumG"] = split[8];
            if (split.Length >= 10)
                result["totalsumH"] = split[9];
            return result;
        }

        // Command number(Dec): 111 - please check fiscal device documentation.
        /// <summary>
        /// Report by PLU
        /// </summary>
        /// <param name="option">‘S’ Prints only the PLUs having sales for the day. For each PLU, prints the code, tax group,
        /// product group, name, unit price, sold quantity and sales.\n
        /// ‘P’ All PLUs with their codes, tax groups, product groups, names, sold quantities, available quantities and unit prices are printed.</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed.</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_Items(string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(111, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 111 - please check fiscal device documentation.
        /// <summary>
        /// Report items in range
        /// </summary>
        /// <param name="option">‘S’ Prints only the PLUs having sales for the day. For each PLU, prints the code, tax group,
        /// product group, name, unit price, sold quantity and sales.\n
        /// ‘P’ All PLUs with their codes, tax groups, product groups, names, sold quantities, available quantities and unit prices are printed.</param>
        /// <param name="startPLU">First PLU code to be included in the report.</param>
        /// <param name="endPLU">Last PLU code to be included in the report.</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed.</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_Items_InRange(string option, string startPLU, string endPLU)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(startPLU);
            inputString.Append(",");
            inputString.Append(endPLU);

            string r = CustomCommand(111, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 111 - please check fiscal device documentation.
        /// <summary>
        /// Items in range by by group
        /// </summary>
        /// <param name="option">'S' Prints only the PLUs having sales for the day. For each PLU, prints the code, tax group,
        /// product group, name, unit price, sold quantity and sales.\n
        /// 'P' All PLUs with their codes, tax groups, product groups, names, sold quantities, available quantities and unit prices are printed.</param>
        /// <param name="startPLU">First PLU code to be included in the report.</param>
        /// <param name="endPLU">Last PLU code to be included in the report.</param>
        /// <param name="group">Number between 1 and 99.</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed.</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_Items_InRangeByGroup(string option, string startPLU, string endPLU, string group)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(startPLU);
            inputString.Append(",");
            inputString.Append(endPLU);
            inputString.Append(",");
            inputString.Append(group);

            string r = CustomCommand(111, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 117 - please check fiscal device documentation.
        /// <summary>
        /// Daily financial report with data printed by department
        /// </summary>
        /// <param name="option">'0' - Z report or'2' - X report</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>closure</term>
        /// <description>The fiscal record number: 4 bytes.</description>
        /// </item>
        /// <item>
        /// <term>fMTotal</term>
        /// <description>The sum of all non-VAT sales: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumA</term>
        /// <description>The sums under each tax group A: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumB</term>
        /// <description>The sums under each tax group B: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumC</term>
        /// <description>The sums under each tax group C: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumD</term>
        /// <description>The sums under each tax group D: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumE</term>
        /// <description>The sums under each tax group E: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumF</term>
        /// <description>The sums under each tax group F: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumG</term>
        /// <description>The sums under each tax group G: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumH</term>
        /// <description>The sums under each tax group H: 12 bytes with a sign.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_DailyClosureByDepartments_01(string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(117, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["closure"] = split[0];
            if (split.Length >= 2)
                result["fMTotal"] = split[1];
            if (split.Length >= 3)
                result["totalsumA"] = split[2];
            if (split.Length >= 4)
                result["totalsumB"] = split[3];
            if (split.Length >= 5)
                result["totalsumC"] = split[4];
            if (split.Length >= 6)
                result["totalsumD"] = split[5];
            if (split.Length >= 7)
                result["totalsumE"] = split[6];
            if (split.Length >= 8)
                result["totalsumF"] = split[7];
            if (split.Length >= 9)
                result["totalsumG"] = split[8];
            if (split.Length >= 10)
                result["totalsumH"] = split[9];
            return result;
        }

        // Command number(Dec): 117 - please check fiscal device documentation.
        /// <summary>
        /// Daily financial report with data printed by department
        /// </summary>
        /// <param name="option">'0' - Z report</param>
        /// <param name="withoutClearOpInfo">'N' - disables the clearing of accumulated data by operator</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>closure</term>
        /// <description>The fiscal record number: 4 bytes.</description>
        /// </item>
        /// <item>
        /// <term>fMTotal</term>
        /// <description>The sum of all non-VAT sales: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumA</term>
        /// <description>The sums under each tax group A: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumB</term>
        /// <description>The sums under each tax group B: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumC</term>
        /// <description>The sums under each tax group C: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumD</term>
        /// <description>The sums under each tax group D: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumE</term>
        /// <description>The sums under each tax group E: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumF</term>
        /// <description>The sums under each tax group F: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumG</term>
        /// <description>The sums under each tax group G: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumH</term>
        /// <description>The sums under each tax group H: 12 bytes with a sign.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_DailyClosureByDepartments_02(string option, string withoutClearOpInfo)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(withoutClearOpInfo);

            string r = CustomCommand(117, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["closure"] = split[0];
            if (split.Length >= 2)
                result["fMTotal"] = split[1];
            if (split.Length >= 3)
                result["totalsumA"] = split[2];
            if (split.Length >= 4)
                result["totalsumB"] = split[3];
            if (split.Length >= 5)
                result["totalsumC"] = split[4];
            if (split.Length >= 6)
                result["totalsumD"] = split[5];
            if (split.Length >= 7)
                result["totalsumE"] = split[6];
            if (split.Length >= 8)
                result["totalsumF"] = split[7];
            if (split.Length >= 9)
                result["totalsumG"] = split[8];
            if (split.Length >= 10)
                result["totalsumH"] = split[9];
            return result;
        }

        // Command number(Dec): 118 - please check fiscal device documentation.
        /// <summary>
        /// Daily financial report with department and PLU printing
        /// </summary>
        /// <param name="option">'0' - Z report or '2' - X report</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>closure</term>
        /// <description>The fiscal record number: 4 bytes.</description>
        /// </item>
        /// <item>
        /// <term>fMTotal</term>
        /// <description>The sum of all non-VAT sales: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumA</term>
        /// <description>The sums under each tax group A: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumB</term>
        /// <description>The sums under each tax group B: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumC</term>
        /// <description>The sums under each tax group C: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumD</term>
        /// <description>The sums under each tax group D: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumE</term>
        /// <description>The sums under each tax group E: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumF</term>
        /// <description>The sums under each tax group F: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumG</term>
        /// <description>The sums under each tax group G: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumH</term>
        /// <description>The sums under each tax group H: 12 bytes with a sign.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_DailyClosureByDepartmentsAndItems_01(string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(118, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["closure"] = split[0];
            if (split.Length >= 2)
                result["fMTotal"] = split[1];
            if (split.Length >= 3)
                result["totalsumA"] = split[2];
            if (split.Length >= 4)
                result["totalsumB"] = split[3];
            if (split.Length >= 5)
                result["totalsumC"] = split[4];
            if (split.Length >= 6)
                result["totalsumD"] = split[5];
            if (split.Length >= 7)
                result["totalsumE"] = split[6];
            if (split.Length >= 8)
                result["totalsumF"] = split[7];
            if (split.Length >= 9)
                result["totalsumG"] = split[8];
            if (split.Length >= 10)
                result["totalsumH"] = split[9];
            return result;
        }

        // Command number(Dec): 118 - please check fiscal device documentation.
        /// <summary>
        /// Daily financial report with department and PLU printing
        /// </summary>
        /// <param name="option">'0' - Z report</param>
        /// <param name="withoutClearOpInfo">'N' - disables the clearing of accumulated data by operator</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>closure</term>
        /// <description>The fiscal record number: 4 bytes.</description>
        /// </item>
        /// <item>
        /// <term>fMTotal</term>
        /// <description>The sum of all non-VAT sales: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumA</term>
        /// <description>The sums under each tax group A: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumB</term>
        /// <description>The sums under each tax group B: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumC</term>
        /// <description>The sums under each tax group C: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumD</term>
        /// <description>The sums under each tax group D: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumE</term>
        /// <description>The sums under each tax group E: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumF</term>
        /// <description>The sums under each tax group F: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumG</term>
        /// <description>The sums under each tax group G: 12 bytes with a sign.</description>
        /// </item>
        /// <item>
        /// <term>totalsumH</term>
        /// <description>The sums under each tax group H: 12 bytes with a sign.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_DailyClosureByDepartmentsAndItems_02(string option, string withoutClearOpInfo)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(withoutClearOpInfo);

            string r = CustomCommand(118, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["closure"] = split[0];
            if (split.Length >= 2)
                result["fMTotal"] = split[1];
            if (split.Length >= 3)
                result["totalsumA"] = split[2];
            if (split.Length >= 4)
                result["totalsumB"] = split[3];
            if (split.Length >= 5)
                result["totalsumC"] = split[4];
            if (split.Length >= 6)
                result["totalsumD"] = split[5];
            if (split.Length >= 7)
                result["totalsumE"] = split[6];
            if (split.Length >= 8)
                result["totalsumF"] = split[7];
            if (split.Length >= 9)
                result["totalsumG"] = split[8];
            if (split.Length >= 10)
                result["totalsumH"] = split[9];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Get item information
        /// </summary>
        /// <param name="option">'I' - get item information</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - passed, 'F' - failed.</description>
        /// </item>
        /// <item>
        /// <term>total</term>
        /// <description>Total number of programmable PLUs (3000 for this printer).</description>
        /// </item>
        /// <item>
        /// <term>prog</term>
        /// <description>Number of programmed PLUs.</description>
        /// </item>
        /// <item>
        /// <term>len</term>
        /// <description>Maximum PLU name length.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Get_ItemsInformation(string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(107, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["total"] = split[1];
            if (split.Length >= 3)
                result["prog"] = split[2];
            if (split.Length >= 4)
                result["len"] = split[3];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Program an item
        /// </summary>
        /// <param name="option">'P' - program an item</param>
        /// <param name="taxGroup">Tax group (‘A’,’B’,’C’,’D’,’E’,’F’,’G’,’H’)</param>
        /// <param name="targetPLU">PLU code (between 1 and 999999999)</param>
        /// <param name="group">Product group (between 1 and 99)</param>
        /// <param name="singlePrice">Unit price. Up to 8 significant digits</param>
        /// <param name="quantity">Number with up to 3 decimal digits: the available quantity for this PLU.</param>
        /// <param name="itemName">PLU name. Up to 36 bytes.</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed.</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Set_Item(string option, string taxGroup, string targetPLU, string group, string singlePrice, string quantity, string itemName)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(targetPLU);
            inputString.Append(",");
            inputString.Append(group);
            inputString.Append(",");
            inputString.Append(singlePrice);
            inputString.Append(",");
            inputString.Append(quantity);
            inputString.Append(",");
            inputString.Append(itemName);

            string r = CustomCommand(107, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Program an item
        /// </summary>
        /// <param name="option">'P' - program an item</param>
        /// <param name="taxGroup">Tax group (‘A’,’B’,’C’,’D’,’E’,’F’,’G’,’H’)</param>
        /// <param name="targetPLU">PLU code (between 1 and 999999999)</param>
        /// <param name="group">Product group (between 1 and 99)</param>
        /// <param name="singlePrice">Unit price. Up to 8 significant digits</param>
        /// <param name="replace">One byte with a value of ‘A’, available quantity is replaced by 'quantity'</param>
        /// <param name="quantity">Number with up to 3 decimal digits: the available quantity for this PLU.</param>
        /// <param name="itemName">PLU name. Up to 36 bytes.</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed.</description>
        /// </item> 
        /// </list>
        /// </returns>

        public Dictionary<string, string> items_Set_ItemWithReplace(string option, string taxGroup, string targetPLU, string group, string singlePrice, string replace, string quantity, string itemName)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(targetPLU);
            inputString.Append(",");
            inputString.Append(group);
            inputString.Append(",");
            inputString.Append(singlePrice);
            inputString.Append(",");
            inputString.Append(replace);
            inputString.Append(quantity);
            inputString.Append(",");
            inputString.Append(itemName);

            string r = CustomCommand(107, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Edit the available PLU quantity.
        /// </summary>
        /// <param name="option">'A' - edit qantity</param>
        /// <param name="targetPLU">PLU code (between 1 and 999999999).</param>
        /// <param name="quantity">Quantity adjustment: a floating-point number with up to 3 decimal digits.\n 
        /// Positive numbers increase and negative numbers decrease the quantity.</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed.</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Set_ItemQuantity(string option, string targetPLU, string quantity)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(targetPLU);
            inputString.Append(",");
            inputString.Append(quantity);

            string r = CustomCommand(107, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Deletes the specified PLU code, if it has no accumulated sums
        /// </summary>
        /// <param name="option">'D' - delete item</param>
        /// <param name="targetPLU">PLU code</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed.</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Delete_Item(string option, string targetPLU)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(targetPLU);

            string r = CustomCommand(107, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Delete items in range
        /// </summary>
        /// <param name="option">'D' - delete items</param>
        /// <param name="startPLU">Start PLU code without accumulated sum.</param>
        /// <param name="endPLU">End PLU code without accumulated sum.</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed.</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Delete_ItemsInRange(string option, string startPLU, string endPLU)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(startPLU);
            inputString.Append(",");
            inputString.Append(endPLU);

            string r = CustomCommand(107, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Delete all items
        /// </summary>
        /// <param name="dOption">'D' - delete items</param>
        /// <param name="aOption">'A' - all items</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed.</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Delete_All_Items(string dOption, string aOption)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(dOption);
            inputString.Append(aOption);

            string r = CustomCommand(107, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Read item
        /// </summary>
        /// <param name="option">'R' - read PLU item</param>
        /// <param name="targetPLU">PLU code. Between 1 and 999999999.</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed.</description>
        /// </item> 
        /// <item>
        /// <term>PLU</term>
        /// <description>PLU code. Between 1 and 999999999.</description>
        /// </item> 
        /// <item>
        /// <term>taxGroup</term>
        /// <description>Tax group. One byte.</description>
        /// </item> 
        /// <item>
        /// <term>group</term>
        /// <description>Product group. Between 1 and 99.</description>
        /// </item> 
        /// <item>
        /// <term>singlePrice</term>
        /// <description>loating-point number with the current decimal digits for the printer.</description>
        /// </item> 
        /// <item>
        /// <term>total</term>
        /// <description>Accumulated sum for this PLU.</description>
        /// </item> 
        /// <item>
        /// <term>sold</term>
        /// <description>Sold quantity. Floating-point number with 3 decimal digits.</description>
        /// </item> 
        /// <item>
        /// <term>available</term>
        /// <description>Available quantity. Floating-point number with 3 decimal digits.</description>
        /// </item>
        /// <item>
        /// <term>itemName</term>
        /// <description>PLU name. Up to 36 bytes.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Get_Item(string option, string targetPLU)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(targetPLU);

            string r = CustomCommand(107, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["PLU"] = split[1];
            if (split.Length >= 3)
                result["taxGroup"] = split[2];
            if (split.Length >= 4)
                result["group"] = split[3];
            if (split.Length >= 5)
                result["singlePrice"] = split[4];
            if (split.Length >= 6)
                result["total"] = split[5];
            if (split.Length >= 7)
                result["sold"] = split[6];
            if (split.Length >= 8)
                result["available"] = split[7];
            if (split.Length >= 9)
                result["itemName"] = split[8];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Get first found item
        /// </summary>
        /// <param name="option">'F' - for first found item</param>
        /// <param name="targetPLU">PLU code. Between 1 and 999999999</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed.</description>
        /// </item> 
        /// <item>
        /// <term>PLU</term>
        /// <description>PLU code. Between 1 and 999999999.</description>
        /// </item> 
        /// <item>
        /// <term>taxGroup</term>
        /// <description>Tax group. One byte.</description>
        /// </item> 
        /// <item>
        /// <term>group</term>
        /// <description>Product group. Between 1 and 99.</description>
        /// </item> 
        /// <item>
        /// <term>singlePrice</term>
        /// <description>loating-point number with the current decimal digits for the printer.</description>
        /// </item> 
        /// <item>
        /// <term>total</term>
        /// <description>Accumulated sum for this PLU.</description>
        /// </item> 
        /// <item>
        /// <term>sold</term>
        /// <description>Sold quantity. Floating-point number with 3 decimal digits.</description>
        /// </item> 
        /// <item>
        /// <term>available</term>
        /// <description>Available quantity. Floating-point number with 3 decimal digits.</description>
        /// </item>
        /// <item>
        /// <term>itemName</term>
        /// <description>PLU name. Up to 36 bytes.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Get_FirstFoundItem(string option, string targetPLU)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(targetPLU);

            string r = CustomCommand(107, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["PLU"] = split[1];
            if (split.Length >= 3)
                result["taxGroup"] = split[2];
            if (split.Length >= 4)
                result["group"] = split[3];
            if (split.Length >= 5)
                result["singlePrice"] = split[4];
            if (split.Length >= 6)
                result["total"] = split[5];
            if (split.Length >= 7)
                result["sold"] = split[6];
            if (split.Length >= 8)
                result["available"] = split[7];
            if (split.Length >= 9)
                result["itemName"] = split[8];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Get last found item
        /// </summary>
        /// <param name="option">'L' - for last found item</param>
        /// <param name="targetPLU">PLU code. Between 1 and 999999999</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed.</description>
        /// </item> 
        /// <item>
        /// <term>PLU</term>
        /// <description>PLU code. Between 1 and 999999999.</description>
        /// </item> 
        /// <item>
        /// <term>taxGroup</term>
        /// <description>Tax group. One byte.</description>
        /// </item> 
        /// <item>
        /// <term>group</term>
        /// <description>Product group. Between 1 and 99.</description>
        /// </item> 
        /// <item>
        /// <term>singlePrice</term>
        /// <description>loating-point number with the current decimal digits for the printer.</description>
        /// </item> 
        /// <item>
        /// <term>total</term>
        /// <description>Accumulated sum for this PLU.</description>
        /// </item> 
        /// <item>
        /// <term>sold</term>
        /// <description>Sold quantity. Floating-point number with 3 decimal digits.</description>
        /// </item> 
        /// <item>
        /// <term>available</term>
        /// <description>Available quantity. Floating-point number with 3 decimal digits.</description>
        /// </item>
        /// <item>
        /// <term>itemName</term>
        /// <description>PLU name. Up to 36 bytes.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Get_LastFoundItem(string option, string targetPLU)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(targetPLU);

            string r = CustomCommand(107, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["PLU"] = split[1];
            if (split.Length >= 3)
                result["taxGroup"] = split[2];
            if (split.Length >= 4)
                result["group"] = split[3];
            if (split.Length >= 5)
                result["singlePrice"] = split[4];
            if (split.Length >= 6)
                result["total"] = split[5];
            if (split.Length >= 7)
                result["sold"] = split[6];
            if (split.Length >= 8)
                result["available"] = split[7];
            if (split.Length >= 9)
                result["itemName"] = split[8];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Get next item
        /// </summary>
        /// <param name="option">'N' - for next item</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed.</description>
        /// </item> 
        /// <item>
        /// <term>PLU</term>
        /// <description>PLU code. Between 1 and 999999999.</description>
        /// </item> 
        /// <item>
        /// <term>taxGroup</term>
        /// <description>Tax group. One byte.</description>
        /// </item> 
        /// <item>
        /// <term>group</term>
        /// <description>Product group. Between 1 and 99.</description>
        /// </item> 
        /// <item>
        /// <term>singlePrice</term>
        /// <description>loating-point number with the current decimal digits for the printer.</description>
        /// </item> 
        /// <item>
        /// <term>total</term>
        /// <description>Accumulated sum for this PLU.</description>
        /// </item> 
        /// <item>
        /// <term>sold</term>
        /// <description>Sold quantity. Floating-point number with 3 decimal digits.</description>
        /// </item> 
        /// <item>
        /// <term>available</term>
        /// <description>Available quantity. Floating-point number with 3 decimal digits.</description>
        /// </item>
        /// <item>
        /// <term>itemName</term>
        /// <description>PLU name. Up to 36 bytes.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Get_NextItem(string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(107, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["PLU"] = split[1];
            if (split.Length >= 3)
                result["taxGroup"] = split[2];
            if (split.Length >= 4)
                result["group"] = split[3];
            if (split.Length >= 5)
                result["singlePrice"] = split[4];
            if (split.Length >= 6)
                result["total"] = split[5];
            if (split.Length >= 7)
                result["sold"] = split[6];
            if (split.Length >= 8)
                result["available"] = split[7];
            if (split.Length >= 9)
                result["itemName"] = split[8];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Get first sold item
        /// </summary>
        /// <param name="option">'f' - return the data on the first found PLU</param>
        /// <param name="targetPLU">PLU code. Between 1 and 999999999.</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed.</description>
        /// </item> 
        /// <item>
        /// <term>PLU</term>
        /// <description>PLU code. Between 1 and 999999999.</description>
        /// </item> 
        /// <item>
        /// <term>taxGroup</term>
        /// <description>Tax group. One byte.</description>
        /// </item> 
        /// <item>
        /// <term>group</term>
        /// <description>Product group. Between 1 and 99.</description>
        /// </item> 
        /// <item>
        /// <term>singlePrice</term>
        /// <description>loating-point number with the current decimal digits for the printer.</description>
        /// </item> 
        /// <item>
        /// <term>total</term>
        /// <description>Accumulated sum for this PLU.</description>
        /// </item> 
        /// <item>
        /// <term>sold</term>
        /// <description>Sold quantity. Floating-point number with 3 decimal digits.</description>
        /// </item> 
        /// <item>
        /// <term>available</term>
        /// <description>Available quantity. Floating-point number with 3 decimal digits.</description>
        /// </item>
        /// <item>
        /// <term>itemName</term>
        /// <description>PLU name. Up to 36 bytes.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Get_FirstSoldItem(string option, string targetPLU)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(targetPLU);

            string r = CustomCommand(107, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["PLU"] = split[1];
            if (split.Length >= 3)
                result["taxGroup"] = split[2];
            if (split.Length >= 4)
                result["group"] = split[3];
            if (split.Length >= 5)
                result["singlePrice"] = split[4];
            if (split.Length >= 6)
                result["total"] = split[5];
            if (split.Length >= 7)
                result["sold"] = split[6];
            if (split.Length >= 8)
                result["available"] = split[7];
            if (split.Length >= 9)
                result["itemName"] = split[8];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Return the data on the last found PLU item shaving sales
        /// </summary>
        /// <param name="option">'l' - for last found sold item</param>
        /// <param name="targetPLU">PLU code. Between 1 and 999999999.</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed.</description>
        /// </item> 
        /// <item>
        /// <term>PLU</term>
        /// <description>PLU code. Between 1 and 999999999.</description>
        /// </item> 
        /// <item>
        /// <term>taxGroup</term>
        /// <description>Tax group. One byte.</description>
        /// </item> 
        /// <item>
        /// <term>group</term>
        /// <description>Product group. Between 1 and 99.</description>
        /// </item> 
        /// <item>
        /// <term>singlePrice</term>
        /// <description>loating-point number with the current decimal digits for the printer.</description>
        /// </item> 
        /// <item>
        /// <term>total</term>
        /// <description>Accumulated sum for this PLU.</description>
        /// </item> 
        /// <item>
        /// <term>sold</term>
        /// <description>Sold quantity. Floating-point number with 3 decimal digits.</description>
        /// </item> 
        /// <item>
        /// <term>available</term>
        /// <description>Available quantity. Floating-point number with 3 decimal digits.</description>
        /// </item>
        /// <item>
        /// <term>itemName</term>
        /// <description>PLU name. Up to 36 bytes.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Get_LastSoldItem(string option, string targetPLU)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(targetPLU);

            string r = CustomCommand(107, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["PLU"] = split[1];
            if (split.Length >= 3)
                result["taxGroup"] = split[2];
            if (split.Length >= 4)
                result["group"] = split[3];
            if (split.Length >= 5)
                result["singlePrice"] = split[4];
            if (split.Length >= 6)
                result["total"] = split[5];
            if (split.Length >= 7)
                result["sold"] = split[6];
            if (split.Length >= 8)
                result["available"] = split[7];
            if (split.Length >= 9)
                result["itemName"] = split[8];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Return the data on the next found sold PLU
        /// </summary>
        /// <param name="option">'n' - for next found sold item</param>
        /// <param name="targetPLU">PLU code (between 1 and 999999999).</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed.</description>
        /// </item> 
        /// <item>
        /// <term>PLU</term>
        /// <description>PLU code. Between 1 and 999999999.</description>
        /// </item> 
        /// <item>
        /// <term>taxGroup</term>
        /// <description>Tax group. One byte.</description>
        /// </item> 
        /// <item>
        /// <term>group</term>
        /// <description>Product group. Between 1 and 99.</description>
        /// </item> 
        /// <item>
        /// <term>singlePrice</term>
        /// <description>loating-point number with the current decimal digits for the printer.</description>
        /// </item> 
        /// <item>
        /// <term>total</term>
        /// <description>Accumulated sum for this PLU.</description>
        /// </item> 
        /// <item>
        /// <term>sold</term>
        /// <description>Sold quantity. Floating-point number with 3 decimal digits.</description>
        /// </item> 
        /// <item>
        /// <term>available</term>
        /// <description>Available quantity. Floating-point number with 3 decimal digits.</description>
        /// </item>
        /// <item>
        /// <term>itemName</term>
        /// <description>PLU name. Up to 36 bytes.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Get_NextSoldItem(string option, string targetPLU)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(targetPLU);

            string r = CustomCommand(107, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["PLU"] = split[1];
            if (split.Length >= 3)
                result["taxGroup"] = split[2];
            if (split.Length >= 4)
                result["group"] = split[3];
            if (split.Length >= 5)
                result["singlePrice"] = split[4];
            if (split.Length >= 6)
                result["total"] = split[5];
            if (split.Length >= 7)
                result["sold"] = split[6];
            if (split.Length >= 8)
                result["available"] = split[7];
            if (split.Length >= 9)
                result["itemName"] = split[8];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Find the first not programmed item
        /// </summary>
        /// <param name="option">'X' - for first not programmed item</param>
        /// <param name="targetPLU">PLU code (between 1 and 999999999).</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>PLU</term>
        /// <description>PLU code (1 - 999999999)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Get_FirstNotProgrammedItem(string option, string targetPLU)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(targetPLU);

            string r = CustomCommand(107, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["PLU"] = split[0];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Find the last not programmed PLU
        /// </summary>
        /// <param name="option">'x' - for last not programmed item</param>
        /// <param name="targetPLU">PLU code (between 1 and 999999999).</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>PLU</term>
        /// <description>PLU code (1 - 999999999)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Get_LastNotProgrammedItem(string option, string targetPLU)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(targetPLU);

            string r = CustomCommand(107, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["PLU"] = split[0];
            return result;
        }

        // Command number(Dec): 36 - please check fiscal device documentation.
        /// <summary>
        /// Set LAN settings
        /// </summary>
        /// <param name="iPAddress">4 numbers, between 0 and 255, separated by a dot, representing the IP address of the device</param>
        /// <param name="subnetMask">4 numbers, between 0 and 255, separated by a dot, representing the IP address of the device</param>
        /// <param name="portNumber">Number, between 1 and 65535, representing the IP port of the device</param>
        /// <param name="defaultGateway">4 numbers, between 0 and 255, separated by a dot, representing the Default Gateway address of the device</param>
        /// <param name="dHCP">One character: "0" or "1". Enables or disables DHCP (Receiving automatic LAN settings from the server).</param>
        /// <param name="mACAddress">Up to 12 hexadecimal characters representing the MAC address of the device. Works only if the service jumper is present!!!</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description>Data for LAN</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_SetLAN(string iPAddress, string subnetMask, string portNumber, string defaultGateway, string dHCP, string mACAddress)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(iPAddress);
            inputString.Append(",");
            inputString.Append(subnetMask);
            inputString.Append(",");
            inputString.Append(portNumber);
            inputString.Append(",");
            inputString.Append(defaultGateway);
            inputString.Append(",");
            inputString.Append("*");
            inputString.Append(dHCP);
            inputString.Append(",");
            inputString.Append(mACAddress);

            string r = CustomCommand(36, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["fAnswer"] = split[0];
            return result;
        }

        // Command number(Dec): 41 - please check fiscal device documentation.
        /// <summary>
        /// Store the settings and the switches in the flash memory
        /// </summary>
        /// <param name="switches">8 or 16 bytes with values of ‘0’ or ‘1’: the configuration 'switches'.</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>"P" - successful recording; "F" - unsuccessful recording of the settings</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_Switches(string switches)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(switches);

            string r = CustomCommand(41, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Set header line
        /// </summary>
        /// <param name="item">Between '0' and '5' for header lines</param>
        /// <param name="value">Text up to 48 symbols</param>
        public void config_Set_HeaderLine(string item, string value)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(item);
            inputString.Append(value);

            string r = CustomCommand(43, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Set footer lines
        /// </summary>
        /// <param name="item">Between '6' and '7'</param>
        /// <param name="value">Text up to 48 symbols</param>
        public void config_Set_FooterLine(string item, string value)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(item);
            inputString.Append(value);

            string r = CustomCommand(43, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Set auto format
        /// </summary>
        /// <param name="option">'A' - Automatic invoice-like sales formatting (4 separate lines)</param>
        /// <param name="offOn">'0' - disables invoice; '1' - enables invoice</param>
        public void config_Set_AutoFormat(string option, string offOn)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(offOn);

            string r = CustomCommand(43, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Set barcode height
        /// </summary>
        /// <param name="option">'B'</param>
        /// <param name="value">Number setting the barcode height in pixels</param>
        public void config_Set_BarcodeHeight(string option, string value)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(value);

            string r = CustomCommand(43, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Set auto paper cutting
        /// </summary>
        /// <param name="option">'C' - for auto paper cutting</param>
        /// <param name="offOn">'0' - disables; '1' - enables auto paper cutting</param>
        public void config_Set_AutoCutting(string option, string offOn)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(offOn);

            string r = CustomCommand(43, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Set print density
        /// </summary>
        /// <param name="option">'D' - for print density</param>
        /// <param name="value">Between 1 and 5</param>
        public void config_Set_PrintDensity(string option, string value)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(value);

            string r = CustomCommand(43, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Set enabled EUR
        /// </summary>
        /// <param name="option">'E' - for enable EUR</param>
        /// <param name="on">'1' - enabled</param>
        /// <param name="rate">currently programmed exchange rate</param>
        public void config_enable_EUR(string option, string on, string rate)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(on);
            inputString.Append(",");
            inputString.Append(rate);

            string r = CustomCommand(43, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Set disabled EUR
        /// </summary>
        /// <param name="option">'E' - for disable EUR</param>
        /// <param name="off">'0' - disabled</param>
        public void config_disable_EUR(string option, string off)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(off);

            string r = CustomCommand(43, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Set font size
        /// </summary>
        /// <param name="option">'F' - printer font</param>
        /// <param name="value">'0' - 32 dots (4 mm) height, higher letters; '1' - 32 dots(4 mm) height, normal letters; '2' - 24 dots(3 mm) height</param>
        public void config_Set_FontSize(string option, string value)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(value);

            string r = CustomCommand(43, inputString.ToString());
            CheckResult();
        }

        /// <summary>
        /// Enables / disables the printer's readiness print message.
        /// </summary>
        /// <param name="option">"G" - Enables / disables the printer's readiness print message</param>
        /// <param name="offOn">One byte with value of '1' (enabled) or '0' (disabled).</param>
        public void config_Set_PrinterReadinessPrintMsgs(string option, string offOn)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(offOn);

            string r = CustomCommand(43, inputString.ToString());
            CheckResult();
        }

        /// <summary>
        /// Enable / disable automatic Z-report printing at 23:59:59 o'clock if issuance of a fiscal or service entry for the day.
        /// </summary>
        /// <param name="option">"H" - Enable / disable automatic Z-report printing at 23:59:59 o'clock if issuance of a fiscal or service entry for the day.</param>
        /// <param name="offOn">One byte with value of '1' (enabled) or '0' (disabled).</param>
        /// <param name="optZRep">'p': The Z-report is printed (otherwise it is only recorded in the KLEN);\n
        ///'a': Z-report contains data for sales of articles / fuels;\n
        ///'d': The Z-report contains departments data;\n
        ///'N': The Z-report does not reset the operator data;\n
        ///'A': The Z-report does not reset the item data;\n
        ///'D': The Z-report does not reset the departments data.</param>
        public void config_Set_AutomaticZReport(string option, string offOn, string optZRep)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(offOn);
            inputString.Append(optZRep);

            string r = CustomCommand(43, inputString.ToString());
            CheckResult();
        }

        /// <summary>
        /// Enable / disable UNP printing of fiscal and reversal receipts.
        /// </summary>
        /// <param name="option">"K" - Enables / disables the printer's readiness print message</param>
        /// <param name="offOn">One byte with value of '1' (enabled) or '0' (disabled).</param>
        public void config_Set_UNPPrinting(string option, string offOn)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(offOn);

            string r = CustomCommand(43, inputString.ToString());
            CheckResult();
        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Enable/disable printing the logo graphic right before the HEADER
        /// </summary>
        /// <param name="option">'L' - for printing graphic logo</param>
        /// <param name="offOn">One byte with value of '1' (enabled) or '0' (disabled).</param>
        /// <param name="height">Height</param>
        public void config_Set_PrintLogo(string option, string offOn, string height)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(offOn);
            inputString.Append(",");
            inputString.Append(height);

            string r = CustomCommand(43, inputString.ToString());
            CheckResult();

        }

        /// <summary>
        /// Enable / disable the sound signal when attaching / disconnecting to the device on TCP / IP.
        /// </summary>
        /// <param name="option">"M" - Enable / disable the sound signal when attaching / disconnecting to the device on TCP / IP.</param>
        /// <param name="offOn">One byte with value of '1' (enabled) or '0' (disabled).</param>
        public void config_Set_LANSignal(string option, string offOn)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(offOn);

            string r = CustomCommand(43, inputString.ToString());
            CheckResult();
        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Set print department
        /// </summary>
        /// <param name="option">'N' - one byte for print department</param>
        /// <param name="offOn">'0' - disabled; '1' - enabled</param>
        public void config_Set_PrintDepartment(string option, string offOn)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(offOn);

            string r = CustomCommand(43, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Enable suppressed printing in a fiscal or non-fiscal receipt
        /// </summary>
        /// <param name="option">'Q' - one byte for suppressed printing</param>
        /// <param name="on">'1' - enables accumulation of unprinted lines in the receipt</param>
        /// <param name="lines">Between 4 and 1000 or 0. If the value is 0, printing starts when the print buffer is full.</param>
        /// <param name="seconds">Between 2 and 120 or 0. If the value is 0, there is no time limit</param>
        public void config_enable_PrintSuppression(string option, string on, string lines, string seconds)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(on);
            inputString.Append(",");
            inputString.Append(lines);
            inputString.Append(",");
            inputString.Append(seconds);

            string r = CustomCommand(43, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Disable suppressed printing in a fiscal or non-fiscal receipt
        /// </summary>
        /// <param name="option">'Q' - one byte for suppressed printing</param>
        /// <param name="off">'0' - disables accumulation of unprinted lines in the receipt</param>
        public void config_disable_PrintSuppression(string option, string off)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(off);

            string r = CustomCommand(43, inputString.ToString());
            CheckResult();


        }

        /// <summary>
        /// Enable / disable 2G forced modem operation (for 3G models) - one byte of '1' (enabled) or '0' (disabled).
        /// </summary>
        /// <param name="option">"V" - Enable / disable 2G forced modem operation (for 3G models) - one byte of '1' (enabled) or '0' (disabled).</param>
        /// <param name="offOn">'0' - disabled; '1' - enabled</param>
        public void config_Set_2GForcedModem(string option, string offOn)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(offOn);

            string r = CustomCommand(43, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Set extra space
        /// </summary>
        /// <param name="option">'R' - for additional letter spacing in pixels</param>
        /// <param name="value">Values are between 0 and 4</param>
        public void config_Set_ExtraSpace(string option, string value)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(value);

            string r = CustomCommand(43, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Set SIM
        /// </summary>
        /// <param name="option">'SIM' - Selects the SIM card to be used by the GPRS modem</param>
        /// <param name="indexValue">The value can be either 0 or 1</param>
        public void config_Set_SIM(string option, string indexValue)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(indexValue);

            string r = CustomCommand(43, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Enable/disable print tax
        /// </summary>
        /// <param name="option">'T' - accumulated VAT for the receipt in a normal (not extended) fiscal receipt.</param>
        /// <param name="offOn">'0' - off; '1' - on</param>
        public void config_Set_PrintTaxDDS(string option, string offOn)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(offOn);

            string r = CustomCommand(43, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Set cash drawer pulse
        /// </summary>
        /// <param name="option">'X' - one byte for cash drawer pulse</param>
        /// <param name="offOn">'0' - off; '1' - on</param>
        public void config_Set_CashDrawerPulse(string option, string offOn)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(offOn);

            string r = CustomCommand(43, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Set warning time range
        /// </summary>
        /// <param name="option">'W' - Sets the time to trigger the warning for unsent data on payment documents and not updated data on the level measuring system</param>
        /// <param name="warnTimeRec">Time in minutes, between 10 and 1440 or 0. If it is zero, there is no warning</param>
        /// <param name="warnTimeGInfo">Time in minutes, between 10 and 1440 or 0. If it is zero, there is no warning</param>
        public void config_Set_WarningTimeRange(string option, string warnTimeRec, string warnTimeGInfo)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(warnTimeRec);
            inputString.Append(",");
            inputString.Append(warnTimeGInfo);

            string r = CustomCommand(43, inputString.ToString());
            CheckResult();
        }

        /// <summary>
        /// Set fiscal device to work with a Datecs payment terminal
        /// </summary>
        /// <param name="enable">0 - disabled, 1 - enabled</param>
        public void config_Set_WorkingWithPinpad(string enable)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("Z");
            inputString.Append(enable);

            string r = CustomCommand(43, inputString.ToString());
            CheckResult();
        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Disable/enable the FEED button functionality
        /// </summary>
        /// <param name="option">'c' - one byte for feed button functionality</param>
        /// <param name="offOn">'0' - off; '1' - on</param>
        public void config_Set_FeedButton(string option, string offOn)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(offOn);

            string r = CustomCommand(43, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Disable/enable returning optional text in the command response when an error occurs
        /// </summary>
        /// <param name="option">'d' - one byte optional error text</param>
        /// <param name="offOn">'0' - off; '1' - on</param>
        public void config_Set_OptionalErrorText(string option, string offOn)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(offOn);

            string r = CustomCommand(43, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Enable / disable automatic roll-up of paper in a container after each document. After
        /// turning on the printer, the printer does not pick up the paper in a container. ONLY FOR PRINTERS SK1-21F AND SK1-31F.
        /// </summary>
        /// <param name="option">'P' - Enable / disable automatic roll-up of paper in a container after each document. After
        /// turning on the printer, the printer does not pick up the paper in a container</param>
        /// <param name="offOn">'0' - off; '1' - on</param>
        public void config_Set_RollUpPaper(string option, string offOn)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(offOn);

            string r = CustomCommand(43, inputString.ToString());
            CheckResult();
        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Set time to automatically retract paper in a container after each printed and waiting to takeout document. ONLY FOR PRINTERS SK1-21F AND SK1-31F.
        /// </summary>
        /// <param name="option">'O' - Set time to automatically retract paper in a container after each printed and waiting to takeout document</param>
        /// <param name="offOn">'0' - off; '1' - on</param>
        public void config_Set_AutomaticRetractPaper(string option, string offOn)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(offOn);

            string r = CustomCommand(43, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 61 - please check fiscal device documentation.
        /// <summary>
        /// Set date and time
        /// </summary>
        /// <param name="dateTime">In format DD-MM-YY HH:MM[:SS]</param>
        public void config_Set_DateTime(string dateTime)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(dateTime);

            string r = CustomCommand(61, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 66 - please check fiscal device documentation.
        /// <summary>
        /// Set invoice range
        /// </summary>
        /// <param name="startValue">Sets the range start value. Integer up to 10 digits</param>
        /// <param name="endValue">Sets the range end value. Integer up to 10 digits</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>valueStart</term>
        /// <description>The starting number of the interval. Max 10 digits (1...9999999999).</description>
        /// </item>
        /// <item>
        /// <term>valueEnd</term>
        /// <description>The ending number of the interval. Max 10 digits (1...9999999999).</description>
        /// </item>
        /// <item>
        /// <term>valueCurrent</term>
        /// <description>The current invoice receipt number (1...9999999999).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_InvoiceRange(string startValue, string endValue)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(startValue);
            inputString.Append(",");
            inputString.Append(endValue);

            string r = CustomCommand(66, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["valueStart"] = split[0];
            if (split.Length >= 2)
                result["valueEnd"] = split[1];
            if (split.Length >= 3)
                result["valueCurrent"] = split[2];
            return result;
        }

        // Command number(Dec): 75 - please check fiscal device documentation.
        /// <summary>
        /// Force suppressed printing
        /// </summary>
        /// <param name="restoreOption">One byte with the following possible values:\n
        /// '0' - After the end of printing, printing is reenabled—next data will be printed immediately.
        /// '1' - After the print buffer is cleared, printing remains suppressed until the end of the receipt.</param>
        public void config_Restore_PrintSuppression(string restoreOption)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(restoreOption);

            string r = CustomCommand(75, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 85 - please check fiscal device documentation.
        /// <summary>
        /// Set additional payment names
        /// </summary>
        /// <param name="option">‘I’ Additional payment 1; ‘J’ Additional payment 2; ‘K’ Additional payment 3; ‘L’ Additional payment 4.
        /// ‘i’ Additional payment 1. Identical to ‘I’; ‘j’ Additional payment 2. Identical to ‘J’; ‘k’ Additional payment 3. Identical to ‘K’;
        /// ‘l’ Additional payment 4. Identical to ‘L’; ‘m’ Additional payment 5; ‘n’ Additional payment 6; ‘o’ Additional payment 7; ‘p’ Additional payment 8\n
        /// ‘q’ Additional payment 9; ‘r’ Additional payment 10; ‘s’ Additional payment 11</param>
        /// <param name="additionalPaymentName">Name of the respective payment type (up to 24 characters).</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed.</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_AdditionalPaymentName(string option, string additionalPaymentName)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(additionalPaymentName);

            string r = CustomCommand(85, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 87 - please check fiscal device documentation.
        /// <summary>
        /// Program a department name
        /// </summary>
        /// <param name="departmentNumber">Department number. Integer between 1 and 1200.</param>
        /// <param name="taxGroup">Tax group associated with the department.</param>
        /// <param name="textRow1">Department name or descriptive text. Up to 28 characters.</param>
        public void config_Set_DepartmentName(string departmentNumber, string taxGroup, string textRow1)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(departmentNumber);
            inputString.Append(",");
            inputString.Append(taxGroup);
            inputString.Append(",");
            inputString.Append(textRow1);

            string r = CustomCommand(87, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 87 - please check fiscal device documentation.
        /// <summary>
        /// Program a department name for two lines
        /// </summary>
        /// <param name="departmentNumber">Department number. Integer between 1 and 1200.</param>
        /// <param name="taxGroup">Tax group associated with the department.</param>
        /// <param name="textRow1">Department name or descriptive text. Up to 28 characters.</param>
        /// <param name="textRow2">Department name or descriptive text—second line. Optional parameter: up to 34 characters</param>
        public void config_Set_DepartmentNameTwoRows(string departmentNumber, string taxGroup, string textRow1, string textRow2)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(departmentNumber);
            inputString.Append(",");
            inputString.Append(taxGroup);
            inputString.Append(",");
            inputString.Append(textRow1);
            inputString.Append("\n");
            inputString.Append(textRow2);

            string r = CustomCommand(87, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 101 - please check fiscal device documentation.
        /// <summary>
        /// Set operator's password
        /// </summary>
        /// <param name="operatorCode">Operator code. Between 1 and 16.</param>
        /// <param name="oldPassword">Old password (between 4 and 8 digits).</param>
        /// <param name="newPassword">New password (between 4 and 8 digits).</param>
        public void config_Set_OperatorPassword(string operatorCode, string oldPassword, string newPassword)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorCode);
            inputString.Append(",");
            inputString.Append(oldPassword);
            inputString.Append(",");
            inputString.Append(newPassword);

            string r = CustomCommand(101, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 102 - please check fiscal device documentation.
        /// <summary>
        /// Set operator's name
        /// </summary>
        /// <param name="operatorCode">Operator code. Between 1 and 16.</param>
        /// <param name="password">Password (between 4 and 8 digits).</param>
        /// <param name="operatorName">Operator name (up to 24 characters).</param>
        public void config_Set_OperatorName(string operatorCode, string password, string operatorName)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorCode);
            inputString.Append(",");
            inputString.Append(password);
            inputString.Append(",");
            inputString.Append(operatorName);

            string r = CustomCommand(102, inputString.ToString());
            CheckResult();


        }

        // 115 program a graphic logo
        /// <summary>
        /// Program graphic logo
        /// </summary>
        /// <param name="rowNum">The line being programmed. Number between 0 and 95.</param>
        /// <param name="data">Graphic data. Set as hexadecimals, two characters for each byte of information.
        /// The data length is up to 72 bytes.If it is less than that, the data are automatically padded with 00.</param>
        public void config_Set_Logo(string rowNum, string data)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(rowNum);
            inputString.Append(",");
            inputString.Append(data);

            string r = CustomCommand(115, inputString.ToString());
            CheckResult();
        }

        /// <summary>
        /// Safety turn off the printer or modem
        /// </summary>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>data</term>
        /// <description>'SHUTDOWN' for devices FP-800 / FP-2000 / FP-650 / SK1-21F / SK1-31F or "OFF" for devices FMP-10 / FP-700</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_SafetyTurnOffDevice()
        {
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(120, inputString.ToString());
            CheckResult();
            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["data"] = split[0];
            return result;
        }

        /// <summary>
        /// The command allows to obtain information on the last command completed with an error. This information is\n
        /// saved for successfully completed commands and also after the printer is powered off.It is cleared only on RAM\n
        /// reset and on execution of the command with a “CLEAR” text input.
        /// </summary>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>cmd</term>
        /// <description>Last error command number.</description>
        /// </item>
        /// <item>
        /// <term>errorCode</term>
        /// <description>Number: error code.</description>
        /// </item>
        /// <item>
        /// <term>dateTime</term>
        /// <description>Error date and time in the DD-MM-YY hh:mm:ss format.</description>
        /// </item>
        /// <item>
        /// <term>errorText</term>
        /// <description>Text description of the last error.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_LastErrorExtendedInfo()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(32, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["cmd"] = split[0];
            if (split.Length >= 2)
                result["errorCode"] = split[1];
            if (split.Length >= 3)
                result["dateTime"] = split[2];
            if (split.Length >= 4)
                result["errorText"] = split[3];
            return result;
        }

        /// <summary>
        /// Clear extended information about last error
        /// </summary>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>cmd</term>
        /// <description>Last error command number.</description>
        /// </item>
        /// <item>
        /// <term>errorCode</term>
        /// <description>Number: error code.</description>
        /// </item>
        /// <item>
        /// <term>dateTime</term>
        /// <description>Error date and time in the DD-MM-YY hh:mm:ss format.</description>
        /// </item>
        /// <item>
        /// <term>errorText</term>
        /// <description>Text description of the last error.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_ClearLastErrorExtendedInfo()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();
            inputString.Append("CLEAR");
            string r = CustomCommand(32, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["cmd"] = split[0];
            if (split.Length >= 2)
                result["errorCode"] = split[1];
            if (split.Length >= 3)
                result["dateTime"] = split[2];
            if (split.Length >= 4)
                result["errorText"] = split[3];
            return result;
        }

        // Command number(Dec): 34 - please check fiscal device documentation.
        /// <summary>
        /// Service contracts information and report
        /// </summary>
        /// <returns>
        /// Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description>'F' - failed or 'Left' - integer parameter: number of remaining service agreement registration fields.
        /// 'RegDtTm parameter' - Service agreement date and time: “DD-MM-YYYY hh:mm:ss”.
        /// EndDate parameter -  Last service agreement expiration date in the “DD-MM-YYYY” format.
        /// 'UIC parameter' 9 or 13 digits: service provider’s UIC.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Service_Contracts()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(34, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["fAnswer"] = split[0];
            return result;
        }

        // Command number(Dec): 36 - please check fiscal device documentation.
        /// <summary>
        /// Get last settings
        /// </summary>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>iPAddress</term>
        /// <description>4 numbers, between 0 and 255, separated by a dot, representing the IP address of the device.</description>
        /// </item>
        /// <item>
        /// <term>subnetMask</term>
        /// <description>4 numbers, between 0 and 255, separated by a dot, representing the Subnet mask of the device.</description>
        /// </item>
        /// <item>
        /// <term>portNumber</term>
        /// <description>Number, between 1 and 65535, representing the IP port of the device.</description>
        /// </item>
        /// <item>
        /// <term>defaultGateway</term>
        /// <description>4 numbers, between 0 and 255, separated by a dot, representing the Default Gateway address of the device.</description>
        /// </item>
        /// <item>
        /// <term>dHCP</term>
        /// <description>One character: "0" or "1". Enables or disables DHCP (Receiving automatic LAN settings from the server).</description>
        /// </item>
        /// <item>
        /// <term>mACAddress</term>
        /// <description>Up to 12 hexadecimal characters representing the MAC address of the device. Works only if the service jumper is present!</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_LANSettings()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(36, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["iPAddress"] = split[0];
            if (split.Length >= 2)
                result["subnetMask"] = split[1];
            if (split.Length >= 3)
                result["portNumber"] = split[2];
            if (split.Length >= 4)
                result["defaultGateway"] = split[3];
            if (split.Length >= 5)
                result["dHCP"] = split[4];
            if (split.Length >= 6)
                result["mACAddress"] = split[5];
            return result;
        }

        /// <summary>
        /// Read NRA data type 1
        /// </summary>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>P if command passes, F - otherwise.</description>
        /// </item>
        /// <item>
        /// <term>dateTime</term>
        /// <description>Date and time in the “DD-MM-YYYY hh:mm:ss” format.</description>
        /// </item>
        /// <item>
        /// <term>closure</term>
        /// <description>Next daily report number.</description>
        /// </item>
        /// <item>
        /// <term>fiscRec</term>
        /// <description>Number of fiscal receipts (customers) for the day.</description>
        /// </item>
        /// <item>
        /// <term>lastFiscal</term>
        /// <description>Global number of the last issued fiscal receipt.</description>
        /// </item>
        /// <item>
        /// <term>lastDoc</term>
        /// <description>Global number of the last issued document.</description>
        /// </item>
        /// <item>
        /// <term>journal</term>
        /// <description>Current journal number.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_NRAData_Type1()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();
            inputString.Append("1");
            string r = CustomCommand(37, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["dateTime"] = split[1];
            if (split.Length >= 3)
                result["closure"] = split[2];
            if (split.Length >= 4)
                result["fiscRec"] = split[3];
            if (split.Length >= 5)
                result["lastFiscal"] = split[4];
            if (split.Length >= 6)
                result["lastDoc"] = split[5];
            if (split.Length >= 7)
                result["journal"] = split[6];
            return result;
        }

        /// <summary>
        /// NRA data type 2 - Sales for tax groups for the day
        /// </summary>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>P if command passes, F - otherwise.</description>
        /// </item>
        /// <item>
        /// <term>s1</term>
        /// <description>Sales for a tax group 1, for the day.</description>
        /// </item>
        /// <item>
        /// <term>s2</term>
        /// <description>Sales for a tax group 2, for the day.</description>
        /// </item>
        /// <item>
        /// <term>s3</term>
        /// <description>Sales for a tax group 3, for the day.</description>
        /// </item>
        /// <item>
        /// <term>s4</term>
        /// <description>Sales for a tax group 4, for the day.</description>
        /// </item>
        /// <item>
        /// <term>s5</term>
        /// <description>Sales for a tax group 5, for the day.</description>
        /// </item>
        /// <item>
        /// <term>s6</term>
        /// <description>Sales for a tax group 6, for the day.</description>
        /// </item>
        /// <item>
        /// <term>s7</term>
        /// <description>Sales for a tax group 7, for the day.</description>
        /// </item>
        /// <item>
        /// <term>s8</term>
        /// <description>Sales for a tax group 8, for the day.</description>
        /// </item>
        /// <item>
        /// <term>total</term>
        /// <description>Total sales for the day.</description>
        /// </item>
        /// <item>
        /// <term>gTotal</term>
        /// <description>Total lifetime sales.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_NRAData_Type2()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();
            inputString.Append("2");
            string r = CustomCommand(37, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["s1"] = split[1];
            if (split.Length >= 3)
                result["s2"] = split[2];
            if (split.Length >= 4)
                result["s3"] = split[3];
            if (split.Length >= 5)
                result["s4"] = split[4];
            if (split.Length >= 6)
                result["s5"] = split[5];
            if (split.Length >= 7)
                result["s6"] = split[6];
            if (split.Length >= 8)
                result["s7"] = split[7];
            if (split.Length >= 9)
                result["s8"] = split[8];
            if (split.Length >= 10)
                result["total"] = split[9];
            if (split.Length >= 11)
                result["gTotal"] = split[10];
            return result;
        }

        /// <summary>
        /// Read NRA type 3
        /// </summary>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>P if command passes, F - otherwise.</description>
        /// </item>
        /// <item>
        /// <term>discountN</term>
        /// <description>Number of discounts for the day.</description>
        /// </item>
        /// <item>
        /// <term>discountS</term>
        /// <description>Total amount of discounts for the day.</description>
        /// </item>
        /// <item>
        /// <term>markUpN</term>
        /// <description>Number of mark-ups for the day.</description>
        /// </item>
        /// <item>
        /// <term>markUpS</term>
        /// <description>Total amount of mark-ups for the day.</description>
        /// </item>
        /// <item>
        /// <term>voidN</term>
        /// <description>Number of adjustments for the day.</description>
        /// </item>
        /// <item>
        /// <term>voidS</term>
        /// <description>Total amount of adjustments for the day.</description>
        /// </item>
        /// <item>
        /// <term>cancelN</term>
        /// <description>Number of cancelled receipts for the day.</description>
        /// </item>
        /// <item>
        /// <term>cancelS</term>
        /// <description>Total amount of cancelled receipts for the day.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_NRAData_Type3()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();
            inputString.Append("3");
            string r = CustomCommand(37, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["discountN"] = split[1];
            if (split.Length >= 3)
                result["discountS"] = split[2];
            if (split.Length >= 4)
                result["markUpN"] = split[3];
            if (split.Length >= 5)
                result["markUpS"] = split[4];
            if (split.Length >= 6)
                result["voidN"] = split[5];
            if (split.Length >= 7)
                result["voidS"] = split[6];
            if (split.Length >= 8)
                result["cancelN"] = split[7];
            if (split.Length >= 9)
                result["cancelS"] = split[8];
            return result;
        }

        /// <summary>
        /// Read NRA type 4
        /// </summary>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>P if command passes, F - otherwise.</description>
        /// </item>
        /// <item>
        /// <term>cashInN</term>
        /// <description>Number of service deposit operations for the day.</description>
        /// </item>
        /// <item>
        /// <term>cashInS</term>
        /// <description>Total amount of service deposits for the day.</description>
        /// </item>
        /// <item>
        /// <term>cashOutN</term>
        /// <description>Number of service withdrawal operations for the day.</description>
        /// </item>
        /// <item>
        /// <term>cashOutS</term>
        /// <description>Total amount of service withdrawals for the day.</description>
        /// </item>
        /// <item>
        /// <term>cashSum</term>
        /// <description>Available cash.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_NRAData_Type4()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();
            inputString.Append("4");
            string r = CustomCommand(37, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["cashInN"] = split[1];
            if (split.Length >= 3)
                result["cashInS"] = split[2];
            if (split.Length >= 4)
                result["cashOutN"] = split[3];
            if (split.Length >= 5)
                result["cashOutS"] = split[4];
            if (split.Length >= 6)
                result["cashSum"] = split[5];

            return result;
        }

        /// <summary>
        /// Read NRA type 5
        /// </summary>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>P if command passes, F - otherwise.</description>
        /// </item>
        /// <item>
        /// <term>cashPaid</term>
        /// <description>Paid in cash.</description>
        /// </item>
        /// <item>
        /// <term>credPaid</term>
        /// <description>Paid by credit card.</description>
        /// </item>
        /// <item>
        /// <term>cardPaid</term>
        /// <description>Paid by debit card.</description>
        /// </item>
        /// <item>
        /// <term>checkPaid</term>
        /// <description>Paid by check.</description>
        /// </item>
        /// <item>
        /// <term>paid1</term>
        /// <description>Paid by programmed payment type 1.</description>
        /// </item>
        /// <item>
        /// <term>paid2</term>
        /// <description>Paid by programmed payment type 2.</description>
        /// </item>
        /// <item>
        /// <term>paid3</term>
        /// <description>Paid by programmed payment type 3.</description>
        /// </item>
        /// <item>
        /// <term>paid4</term>
        /// <description>Paid by programmed payment type 4.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_NRAData_Type5()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();
            inputString.Append("5");
            string r = CustomCommand(37, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["cashPaid"] = split[1];
            if (split.Length >= 3)
                result["credPaid"] = split[2];
            if (split.Length >= 4)
                result["cardPaid"] = split[3];
            if (split.Length >= 5)
                result["checkPaid"] = split[4];
            if (split.Length >= 6)
                result["paid1"] = split[5];
            if (split.Length >= 7)
                result["paid2"] = split[6];
            if (split.Length >= 8)
                result["paid3"] = split[7];
            if (split.Length >= 9)
                result["paid4"] = split[8];
            return result;
        }

        /// <summary>
        /// Read NRA type 6
        /// </summary>
        /// <param name="closure">Daily report number</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>P if command passes, F - otherwise.</description>
        /// </item>
        /// <item>
        /// <term>dt</term>
        /// <description>Daily report date and time in the “DD-MM-YYYY hh:mm:ss” format.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_NRAData_Type6(string closure)
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();
            inputString.Append("6");
            inputString.Append(",");
            inputString.Append(closure);
            string r = CustomCommand(37, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["dt"] = split[1];
            return result;
        }

        /// <summary>
        /// Read NRA type 7
        /// </summary>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>P if command passes, F - otherwise.</description>
        /// </item>
        /// <item>
        /// <term>dt</term>
        /// <description>Returns the date and time of fiscal registration and time in the “DD-MM-YYYY hh:mm:ss” format.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_NRAData_Type7()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();
            inputString.Append("7");
            string r = CustomCommand(37, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["dt"] = split[1];
            return result;
        }

        /// <summary>
        /// Read NRA type 8 - Returns PLU sales data by PLU codes in ascending order
        /// </summary>
        /// <param name="option">"F" - for the first time, and then "N" until read all and printer returns "F" errorCode</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>P if command passes, F - otherwise.</description>
        /// </item>
        /// <item>
        /// <term>PLUcode</term>
        /// <description>PLU code.</description>
        /// </item>
        /// <item>
        /// <term>PLUtotal</term>
        /// <description>Accumulated sales sum for the PLU.</description>
        /// </item>
        /// <item>
        /// <term>PLUname</term>
        /// <description>PLU name.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_NRAData_Type8(string option)
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();
            inputString.Append("8");
            inputString.Append(",");
            inputString.Append(option);
            string r = CustomCommand(37, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["PLUcode"] = split[1];
            if (split.Length >= 3)
                result["PLUtotal"] = split[2];
            if (split.Length >= 4)
                result["PLUname"] = split[3];
            return result;
        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Get print options
        /// </summary>
        /// <param name="option">'I' - get info</param>
        /// <param name="targetOption">All types of command 43 - 'A','B','C','D','E','F','L' ...</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description>Data for selected option</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_PrintOption(string option, string targetOption)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(targetOption);

            string r = CustomCommand(43, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["fAnswer"] = split[0];
            return result;
        }

        // Command number(Dec): 50 - please check fiscal device documentation.
        /// <summary>
        /// Get tax rates by period
        /// </summary>
        /// <param name="startDate">Period start date: DDMMYY (6 bytes).</param>
        /// <param name="endDate">Period end date: DDMMYY (6 bytes).</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed.</description>
        /// </item>
        /// <item>
        /// <term>aA</term>
        /// <description>Information about tax group A.</description>
        /// </item>
        /// <item>
        /// <term>bB</term>
        /// <description>Information about tax group B.</description>
        /// </item>
        /// <item>
        /// <term>cC</term>
        /// <description>Information about tax group C.</description>
        /// </item>
        /// <item>
        /// <term>dD</term>
        /// <description>Information about tax group D.</description>
        /// </item>
        /// <item>
        /// <term>eE</term>
        /// <description>Information about tax group E.</description>
        /// </item>
        /// <item>
        /// <term>fF</term>
        /// <description>Information about tax group F.</description>
        /// </item>
        /// <item>
        /// <term>gG</term>
        /// <description>Information about tax group G.</description>
        /// </item>
        /// <item>
        /// <term>hH</term>
        /// <description>Information about tax group H.</description>
        /// </item>
        /// <item>
        /// <term>dDMMYY
        /// </term>
        /// <description>Date of tax rates set</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_TaxRatesByPeriod(string startDate, string endDate)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(startDate);
            inputString.Append(",");
            inputString.Append(endDate);

            string r = CustomCommand(50, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["aA"] = split[1];
            if (split.Length >= 3)
                result["bB"] = split[2];
            if (split.Length >= 4)
                result["cC"] = split[3];
            if (split.Length >= 5)
                result["dD"] = split[4];
            if (split.Length >= 6)
                result["eE"] = split[5];
            if (split.Length >= 7)
                result["fF"] = split[6];
            if (split.Length >= 8)
                result["gG"] = split[7];
            if (split.Length >= 9)
                result["hH"] = split[8];
            if (split.Length >= 10)
                result["dDMMYY"] = split[9];
            return result;
        }

        /// <summary>
        /// Finds the last issued fiscal or reversal receipt before a daily report with clearing is made /ONLY FOR TYPE 21/
        /// </summary>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorParam</term>
        /// <description>'P' - pass; 'F' - failed; '*' - End of receipt + first parameter ot the answer</description>
        /// </item>
        /// <item>
        /// <term>data</term>
        /// <description>Returned data.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DataForLastIssuedReceipt()
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("*");

            string r = CustomCommand(59, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorParam"] = split[0];
            if (split.Length >= 2)
                result["data"] = split[1];
            return result;
        }

        /// <summary>
        /// Finds a document with documnt number only if it is an electronic document. (ONLY FOR TYPE 21)
        /// </summary>
        /// <param name="docNum">Document number - only if it is electronic document.</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorParam</term>
        /// <description>'P' - pass; 'F' - failed; '*' - End of receipt + first parameter ot the answer</description>
        /// </item>
        /// <item>
        /// <term>data</term>
        /// <description>Returned data.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DataForLastIssuedReceipt(string docNum)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("E");
            inputString.Append(docNum);

            string r = CustomCommand(59, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorParam"] = split[0];
            if (split.Length >= 2)
                result["data"] = split[1];
            return result;
        }

        // Command number(Dec): 62 - please check fiscal device documentation.
        /// <summary>
        /// Get date and time
        /// </summary>
        /// <returns>
        /// Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>dateTime</term>
        /// <description>Date and time in format: DD-MM-YY HH:MM:SS</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DateTime()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(62, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["dateTime"] = split[0];
            return result;
        }

        // Command number(Dec): 62 - please check fiscal device documentation.
        /// <summary>
        /// Get date and time separated
        /// </summary>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>day</term>
        /// <description>Day - DD</description>
        /// </item>
        /// <item>
        /// <term>month</term>
        /// <description>Month - MM</description>
        /// </item>
        /// <item>
        /// <term>year</term>
        /// <description>Year - YY</description>
        /// </item>
        /// <item>
        /// <term>hour</term>
        /// <description>Hours - HH</description>
        /// </item>
        /// <item>
        /// <term>minute</term>
        /// <description>Minutes - mm</description>
        /// </item>
        /// <item>
        /// <term>second</term>
        /// <description>Seconds - ss</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DateTime_01()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(62, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["day"] = split[0];
            if (split.Length >= 2)
                result["month"] = split[1];
            if (split.Length >= 3)
                result["year"] = split[2];
            if (split.Length >= 4)
                result["hour"] = split[3];
            if (split.Length >= 5)
                result["minute"] = split[4];
            if (split.Length >= 6)
                result["second"] = split[5];
            return result;
        }

        // Command number(Dec): 64 - please check fiscal device documentation.
        /// <summary>
        /// Get last fiscal record
        /// </summary>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed</description>
        /// </item>
        /// <item>
        /// <term>lastFRecordNumber</term>
        /// <description>Last fiscal record</description>
        /// </item>
        /// <item>
        /// <term>taxA</term>
        /// <description>Turnover for tax group A</description>
        /// </item>
        /// <item>
        /// <term>taxB</term>
        /// <description>Turnover for tax group B</description>
        /// </item>
        /// <item>
        /// <term>taxC</term>
        /// <description>Turnover for tax group C</description>
        /// </item>
        /// <item>
        /// <term>taxD</term>
        /// <description>Turnover for tax group D</description>
        /// </item>
        /// <item>
        /// <term>taxE</term>
        /// <description>Turnover for tax group E</description>
        /// </item>
        /// <item>
        /// <term>taxF</term>
        /// <description>Turnover for tax froup F</description>
        /// </item>
        /// <item>
        /// <term>taxG</term>
        /// <description>Turnover for tax froup G</description>
        /// </item>
        /// <item>
        /// <term>taxH</term>
        /// <description>Turnover for tax froup H</description>
        /// </item>
        /// <item>
        /// <term>date</term>
        /// <description>Fiscal record date /DDMMYY/</description>
        /// </item>
        /// </list>
        public Dictionary<string, string> info_Get_LastFiscRecord()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(64, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["lastFRecordNumber"] = split[1];
            if (split.Length >= 3)
                result["taxA"] = split[2];
            if (split.Length >= 4)
                result["taxB"] = split[3];
            if (split.Length >= 5)
                result["taxC"] = split[4];
            if (split.Length >= 6)
                result["taxD"] = split[5];
            if (split.Length >= 7)
                result["taxE"] = split[6];
            if (split.Length >= 8)
                result["taxF"] = split[7];
            if (split.Length >= 9)
                result["taxG"] = split[8];
            if (split.Length >= 10)
                result["taxH"] = split[9];
            if (split.Length >= 11)
                result["date"] = split[10];
            return result;
        }

        // Command number(Dec): 64 - please check fiscal device documentation.
        /// <summary>
        /// Get last fiscal record with time
        /// </summary>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed</description>
        /// </item>
        /// <item>
        /// <term>lastFRecordNumber</term>
        /// <description>Last fiscal record</description>
        /// </item>
        /// <item>
        /// <term>taxA</term>
        /// <description>Turnover for tax group A</description>
        /// </item>
        /// <item>
        /// <term>taxB</term>
        /// <description>Turnover for tax group B</description>
        /// </item>
        /// <item>
        /// <term>taxC</term>
        /// <description>Turnover for tax group C</description>
        /// </item>
        /// <item>
        /// <term>taxD</term>
        /// <description>Turnover for tax group D</description>
        /// </item>
        /// <item>
        /// <term>taxE</term>
        /// <description>Turnover for tax group E</description>
        /// </item>
        /// <item>
        /// <term>taxF</term>
        /// <description>Turnover for tax froup F</description>
        /// </item>
        /// <item>
        /// <term>taxG</term>
        /// <description>Turnover for tax froup G</description>
        /// </item>
        /// <item>
        /// <term>taxH</term>
        /// <description>Turnover for tax froup H</description>
        /// </item>
        /// <item>
        /// <term>date</term>
        /// <description>Fiscal record date /DDMMYY/</description>
        /// </item>
        /// </list>
        public Dictionary<string, string> info_Get_LastFiscRecord_01()
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("*");

            string r = CustomCommand(64, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["lastFRecordNumber"] = split[1];
            if (split.Length >= 3)
                result["taxA"] = split[2];
            if (split.Length >= 4)
                result["taxB"] = split[3];
            if (split.Length >= 5)
                result["taxC"] = split[4];
            if (split.Length >= 6)
                result["taxD"] = split[5];
            if (split.Length >= 7)
                result["taxE"] = split[6];
            if (split.Length >= 8)
                result["taxF"] = split[7];
            if (split.Length >= 9)
                result["taxG"] = split[8];
            if (split.Length >= 10)
                result["taxH"] = split[9];
            if (split.Length >= 11)
                result["date"] = split[10];
            return result;
        }

        // Command number(Dec): 65 - please check fiscal device documentation.
        /// <summary>
        /// Information of current sums for the day
        /// </summary>
        /// <param name="option">'0' - total sales; '1' - Accumulated VAT</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>taxA</term>
        /// <description>Turnover for tax group A</description>
        /// </item>
        /// <item>
        /// <term>taxB</term>
        /// <description>Turnover for tax group B</description>
        /// </item>
        /// <item>
        /// <term>taxC</term>
        /// <description>Turnover for tax group C</description>
        /// </item>
        /// <item>
        /// <term>taxD</term>
        /// <description>Turnover for tax group D</description>
        /// </item>
        /// <item>
        /// <term>taxE</term>
        /// <description>Turnover for tax group E</description>
        /// </item>
        /// <item>
        /// <term>taxF</term>
        /// <description>Turnover for tax froup F</description>
        /// </item>
        /// <item>
        /// <term>taxG</term>
        /// <description>Turnover for tax froup G</description>
        /// </item>
        /// <item>
        /// <term>taxH</term>
        /// <description>Turnover for tax froup H</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_AdditionalDailyInfo_01(string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(65, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["sumA"] = split[0];
            if (split.Length >= 2)
                result["sumB"] = split[1];
            if (split.Length >= 3)
                result["sumC"] = split[2];
            if (split.Length >= 4)
                result["sumD"] = split[3];
            if (split.Length >= 5)
                result["sumE"] = split[4];
            if (split.Length >= 6)
                result["sumF"] = split[5];
            if (split.Length >= 7)
                result["sumG"] = split[6];
            if (split.Length >= 8)
                result["sumH"] = split[7];
            return result;
        }

        // Command number(Dec): 66 - please check fiscal device documentation.
        /// <summary>
        /// Get invoice range
        /// </summary>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>valueStart</term>
        /// <description>Start value of invoice range</description>
        /// </item>
        /// <item>
        /// <term>valueEnd</term>
        /// <description>End value of invoice range</description>
        /// </item>
        /// <item>
        /// <term>valueCurrent</term>
        /// <description>Current invoice value</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_InvoiceRange()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(66, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["valueStart"] = split[0];
            if (split.Length >= 2)
                result["valueEnd"] = split[1];
            if (split.Length >= 3)
                result["valueCurrent"] = split[2];
            return result;
        }

        // Command number(Dec): 68 - please check fiscal device documentation.
        /// <summary>
        /// Get free fiscal memory records
        /// </summary>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>countLogical</term>
        /// <description>Count of logical places for fiscal memory</description>
        /// </item>
        /// <item>
        /// <term>countTotal</term>
        /// <description>Count of logical places for fiscal memory (repeats the above record)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_FreeFMRecords()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(68, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["countLogical"] = split[0];
            if (split.Length >= 2)
                result["countTotal"] = split[1];
            return result;
        }

        // Command number(Dec): 70 - please check fiscal device documentation.
        /// <summary>
        /// Get cash in/ cash out
        /// </summary>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass or 'F' - failed</description>
        /// </item>
        /// <item>
        /// <term>cashSum</term>
        /// <description>Cash in safe sum</description>
        /// </item>
        /// <item>
        /// <term>servIn</term>
        /// <description>Total sum of cash in operations</description>
        /// </item>
        /// <item>
        /// <term>servOut</term>
        /// <description>Total sum of cash out operations</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_CashIn_CashOut()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(70, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["cashSum"] = split[1];
            if (split.Length >= 3)
                result["servIn"] = split[2];
            if (split.Length >= 4)
                result["servOut"] = split[3];
            return result;
        }

        // Command number(Dec): 71 - please check fiscal device documentation.
        /// <summary>
        /// Print diagnostic information
        /// </summary>
        public void info_Print_Diagnostic_0()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(71, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 74 - please check fiscal device documentation.
        /// <summary>
        /// Depends on option you choose: 'W' - First, waits for the printer to finish printing everything in the print buffer \n
        /// or 'X' - Does not wait for the printer, but responds immediately
        /// </summary>
        /// <param name="option">'W' - First, waits for the printer to finish printing everything in the print buffer.
        /// 'X' - Does not wait for the printer, but responds immediately.</param>
        /// <returns>
        /// Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>statusBytes</term>
        /// <description>status bytes</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_StatusBytes(string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(74, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["statusBytes"] = split[0];
            return result;
        }



        // Command number(Dec): 74 - please check fiscal device documentation.
        /// <summary>
        /// Get not printed rows count
        /// </summary>
        /// <param name="option">'L' - Returns the number of lines remaining to be printed</param>
        /// <returns>
        /// Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>nLines</term>
        /// <description>Number of pending lines in the print buffer. A value of 0 means that there are no pending print data.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_NotPrintedRowsCount(string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(74, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["nLines"] = split[0];
            return result;
        }

        // Command number(Dec): 74 - please check fiscal device documentation.
        /// <summary>
        /// Returns data on the customer documents sent to the NRA server
        /// </summary>
        /// <param name="option">'R' - returns data on the customer documents sent to the NRA server</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>lastPrintDoc</term>
        /// <description>Number of the last printed document.</description>
        /// </item>
        /// <item>
        /// <term>nLastSentDoc</term>
        /// <description>Number of the last successfully sent document.</description>
        /// </item>
        /// <item>
        /// <term>dTLastSentDoc</term>
        /// <description>Date and time of the last successfully sent document.</description>
        /// </item>
        /// <item>
        /// <term>minFromLastSuccessSent</term>
        /// <description>Minutes since the last successfully sent document.</description>
        /// </item>
        /// <item>
        /// <term>nFirstNotSentDoc</term>
        /// <description>Number of the first unsent document.</description>
        /// </item>
        /// <item>
        /// <term>dTFirstNotSentDoc</term>
        /// <description>Date and time of the first unsent document.</description>
        /// </item>
        /// <item>
        /// <term>minFromFirstNotSuccessSent</term>
        /// <description>Minutes since the first unsent document.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_NRADocuments(string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(74, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["lastPrintDoc"] = split[0];
            if (split.Length >= 2)
                result["nLastSentDoc"] = split[1];
            if (split.Length >= 3)
                result["dTLastSentDoc"] = split[2];
            if (split.Length >= 4)
                result["minFromLastSuccessSent"] = split[3];
            if (split.Length >= 5)
                result["nFirstNotSentDoc"] = split[4];
            if (split.Length >= 6)
                result["dTFirstNotSentDoc"] = split[5];
            if (split.Length >= 7)
                result["minFromFirstNotSuccessSent"] = split[6];
            return result;
        }

        // Command number(Dec): 74 - please check fiscal device documentation.
        /// <summary>
        /// If option is 'D' - Returns cash drawer status (0 - closed drawer, 1 - open drawer) for models that \n
        /// support it(FP-2000 or FP-700) if supported by the connected drawer. If option is 'B' - Returns the status of open shift lock
        /// </summary>
        /// <param name="option">'D' - Returns cash drawer status (0 - closed drawer, 1 - open drawer) for models that \n
        /// support it(FP-2000 or FP-700) if supported by the connected drawer.'B' - Returns the status of open shift lock</param>
        /// <returns>
        /// Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>status</term>
        /// <description>Returns the status of the drawer (0 - closed drawer, 1 - open drawer) or for option 'B' - Returns 0 - not blocked, 1 - blocked</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_CashDrawerStatusOrOpenShift(string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(74, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["status"] = split[0];
            return result;
        }

        // Command number(Dec): 76 - please check fiscal device documentation.
        /// <summary>
        /// 
        /// </summary>
        /// <param name="option">'Т' (the command will return the information on the current
        /// status of the bill payable by the customer until now) or leave blank</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>open</term>
        /// <description>One byte, '1' - if receipt is open, '0' - otherwise</description>
        /// </item>
        /// <item>
        /// <term>items</term>
        /// <description>Sales count from current or last fiscal receipt (4 bytes)</description>
        /// </item>
        /// <item>
        /// <term>amount</term>
        /// <description>Sum of the last fiscal receipt</description>
        /// </item>
        /// <item>
        /// <term>tender</term>
        /// <description>Sum, payed from next or last receipt</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_FTransactionStatus(string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(76, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["open"] = split[0];
            if (split.Length >= 2)
                result["items"] = split[1];
            if (split.Length >= 3)
                result["amount"] = split[2];
            if (split.Length >= 4)
                result["tender"] = split[3];
            return result;
        }

        // Command number(Dec): 79 - please check fiscal device documentation.
        /// <summary>
        /// Summary fiscal memory report by period
        /// </summary>
        /// <param name="fromDate">Start date: 6 bytes (DDMMYY)</param>
        /// <param name="toDate">End date: 6 bytes (DDMMYY)</param>
        public void info_Print_Short_FMReportByDTRange(string fromDate, string toDate)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(fromDate);
            inputString.Append(",");
            inputString.Append(toDate);

            string r = CustomCommand(79, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 79 - please check fiscal device documentation.
        /// <summary>
        /// Monthly fiscal memory report 
        /// </summary>
        /// <param name="startValue">Month: 4 bytes (MMYY) for a monthly report</param>
        public void info_Print_Short_MonthlyFMReport(string startValue)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(startValue);

            string r = CustomCommand(79, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 79 - please check fiscal device documentation.
        /// <summary>
        /// Annual fiscal memory report
        /// </summary>
        /// <param name="startValue">Year: 2 bytes (YY) for an annual report</param>
        public void info_Print_Short_YearlyFMReport(string startValue)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(startValue);

            string r = CustomCommand(79, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 81 - please check fiscal device documentation.
        /// <summary>
        /// Get the supply voltage and temperature
        /// </summary>
        /// <param name="voltage">The supply voltage in volts</param>
        /// <param name="temperature">The printing head temperature in degrees</param>
        public void info_Get_VoltageAndTemperature(string voltage, string temperature)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(voltage);
            inputString.Append(",");
            inputString.Append(temperature);

            string r = CustomCommand(81, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 83 - please check fiscal device documentation.
        /// <summary>
        /// Get decimals and tax rates
        /// </summary>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>multiplier</term>
        /// <description>One byte, '1' - if receipt is open, '0' - otherwise</description>
        /// </item>
        /// <item>
        /// <term>decimals</term>
        /// <description>One byte (0...2), digits after decimal point</description>
        /// </item>
        /// <item>
        /// <term>currencyName</term>
        /// <description>Currency name (3 bytes)</description>
        /// </item>
        /// <item>
        /// <term>enabledMask</term>
        /// <description>Eight bytes, with 0 or 1, for enabled or disabled tax group</description>
        /// </item>
        /// <item>
        /// <term>taxA</term>
        /// <description>Tax A value</description>
        /// </item>
        /// <item>
        /// <term>taxB</term>
        /// <description>Tax B value</description>
        /// </item>
        /// <item>
        /// <term>taxC</term>
        /// <description>Tax C value</description>
        /// </item>
        /// <item>
        /// <term>taxD</term>
        /// <description>Tax D value</description>
        /// </item>
        /// <item>
        /// <term>taxE</term>
        /// <description>Tax E value</description>
        /// </item>
        /// <item>
        /// <term>taxF</term>
        /// <description>Tax F value</description>
        /// </item>
        /// <item>
        /// <term>taxG</term>
        /// <description>Tax G value</description>
        /// </item>
        /// <item>
        /// <term>taxH</term>
        /// <description>Tax H value</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DecimalsAndTaxRates()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(83, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["multiplier"] = split[0];
            if (split.Length >= 2)
                result["decimals"] = split[1];
            if (split.Length >= 3)
                result["currencyName"] = split[2];
            if (split.Length >= 4)
                result["enabledMask"] = split[3];
            if (split.Length >= 5)
                result["taxA"] = split[4];
            if (split.Length >= 6)
                result["taxB"] = split[5];
            if (split.Length >= 7)
                result["taxC"] = split[6];
            if (split.Length >= 8)
                result["taxD"] = split[7];
            if (split.Length >= 9)
                result["taxE"] = split[8];
            if (split.Length >= 10)
                result["taxF"] = split[9];
            if (split.Length >= 11)
                result["taxG"] = split[10];
            if (split.Length >= 12)
                result["taxH"] = split[11];
            return result;
        }

        // Command number(Dec): 85 - please check fiscal device documentation.
        /// <summary>
        /// Get additional payment names
        /// </summary>
        /// <param name="option">‘I’ Additional payment 1; ‘J’ Additional payment 2; ‘K’ Additional payment 3; ‘L’ Additional payment 4.
        /// ‘i’ Additional payment 1. Identical to ‘I’; ‘j’ Additional payment 2. Identical to ‘J’; ‘k’ Additional payment 3. Identical to ‘K’;
        /// ‘l’ Additional payment 4. Identical to ‘L’; ‘m’ Additional payment 5; ‘n’ Additional payment 6; ‘o’ Additional payment 7; ‘p’ Additional payment 8\n
        /// ‘q’ Additional payment 9; ‘r’ Additional payment 10; ‘s’ Additional payment 11</param></param>
        /// <returns>
        /// Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>paymentName</term>
        /// <description>Payment name (up to 24 symbols)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_AdditionalPaymentNames(string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(85, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["paymentName"] = split[0];
            return result;
        }

        // Command number(Dec): 86 - please check fiscal device documentation.
        /// <summary>
        /// Read date of last fiscal memory record 
        /// </summary>
        /// <param name="option">Blank or 'T' - If present, also returns the time of the last record</param>
        /// <returns>
        /// Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>dateTime</term>
        /// <description>Format: DD-MM-YYYY or DD-MM-YYYY hh:mm:ss</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_FMRecord_LastDateTime(string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(86, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["dateTime"] = split[0];
            return result;
        }

        // Command number(Dec): 86 - please check fiscal device documentation.
        /// <summary>
        /// Get date and time from last FM records (separated)
        /// </summary>
        /// <param name="option">Blank or 'T' - If present, also returns the time of the last record</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>day</term>
        /// <description>Day - DD</description>
        /// </item>
        /// <item>
        /// <term>month</term>
        /// <description>Month - MM</description>
        /// </item>
        /// <item>
        /// <term>year</term>
        /// <description>Year - YY</description>
        /// </item>
        /// <item>
        /// <term>hour</term>
        /// <description>Hours - HH</description>
        /// </item>
        /// <item>
        /// <term>minute</term>
        /// <description>Minutes - mm</description>
        /// </item>
        /// <item>
        /// <term>second</term>
        /// <description>Seconds - ss</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_FMRecord_LastDateTime_01(string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(86, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["day"] = split[0];
            if (split.Length >= 2)
                result["month"] = split[1];
            if (split.Length >= 3)
                result["year"] = split[2];
            if (split.Length >= 4)
                result["hour"] = split[3];
            if (split.Length >= 5)
                result["minute"] = split[4];
            if (split.Length >= 6)
                result["second"] = split[5];
            return result;
        }

        // Command number(Dec): 88 - please check fiscal device documentation.
        /// <summary>
        /// Get department information
        /// </summary>
        /// <param name="departmentNumber">Department number. Integer between 1 and 1200. Value 0 returns data on the sales made without department specified.In this case, the tax group is missing.</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass, 'F' - failed</description>
        /// </item>
        /// <item>
        /// <term>taxGroup</term>
        /// <description>Tax group for department (А - И)</description>
        /// </item>
        /// <item>
        /// <term>recSales</term>
        /// <description>Sales count for departments in receipt.</description>
        /// </item>
        /// <item>
        /// <term>recSum</term>
        /// <description>Sum for current or last fiscal receipt for department.</description>
        /// </item>
        /// <item>
        /// <term>totSales</term>
        /// <description>Sales count for department for the day.</description>
        /// </item>
        /// <item>
        /// <term>totSum</term>
        /// <description>Day sum by department.</description>
        /// </item>
        /// <item>
        /// <term>line1</term>
        /// <description>Text for department.</description>
        /// </item>
        /// <item>
        /// <term>line2</term>
        /// <description>Text for department line 2.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DepartmentInfo(string departmentNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(departmentNumber);

            string r = CustomCommand(88, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["outputCode"] = split[0];
            if (split.Length >= 2)
                result["taxGroup"] = split[1];
            if (split.Length >= 3)
                result["recSales"] = split[2];
            if (split.Length >= 4)
                result["recSum"] = split[3];
            if (split.Length >= 5)
                result["totSales"] = split[4];
            if (split.Length >= 6)
                result["totSum"] = split[5];
            if (split.Length >= 7)
                result["line1"] = split[6];
            if (split.Length >= 8)
                result["line2"] = split[7];
            return result;
        }

        // Command number(Dec): 90 - please check fiscal device documentation.
        /// <summary>
        /// Get diagnostic information
        /// </summary>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>deviceName</term>
        /// <description>Device name</description>
        /// </item>
        /// <item>
        /// <term>firmware</term>
        /// <description>Firmware version</description>
        /// </item>
        /// <item>
        /// <term>checkSum</term>
        /// <description>Checksum.</description>
        /// </item>
        /// <item>
        /// <term>switches</term>
        /// <description>Not in fiscal device</description>
        /// </item>
        /// <item>
        /// <term>serialNumber</term>
        /// <description>Serial number of device.</description>
        /// </item>
        /// <item>
        /// <term>fiscalMemoryNumber</term>
        /// <description>Fiscal memory number.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DiagnosticInfo()
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("*");
            inputString.Append("1");

            string r = CustomCommand(90, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["deviceName"] = split[0];
            if (split.Length >= 2)
                result["firmware"] = split[1];
            if (split.Length >= 3)
                result["checkSum"] = split[2];
            if (split.Length >= 4)
                result["switches"] = split[3];
            if (split.Length >= 5)
                result["serialNumber"] = split[4];
            if (split.Length >= 6)
                result["fiscalMemoryNumber"] = split[5];
            return result;
        }

        // Command number(Dec): 93 - please check fiscal device documentation.
        /// <summary>
        /// Get daily corrections
        /// </summary>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>countDiscounts</term>
        /// <description>Number of discounts after the last Z-report</description>
        /// </item>
        /// <item>
        /// <term>sumDiscounts</term>
        /// <description>Sum of discounts after the last Z-report</description>
        /// </item>
        /// <item>
        /// <term>countSurcharges</term>
        /// <description>Number of surcharges after the last Z-report.</description>
        /// </item>
        /// <item>
        /// <term>sumSurcharges</term>
        /// <description>Sum of surcharges after the last Z-report</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DailyCorrections()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(93, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["countDiscounts"] = split[0];
            if (split.Length >= 2)
                result["sumDiscounts"] = split[1];
            if (split.Length >= 3)
                result["countSurcharges"] = split[2];
            if (split.Length >= 4)
                result["sumSurcharges"] = split[3];
            return result;
        }

        // Command number(Dec): 97 - please check fiscal device documentation.
        /// <summary>
        /// Get tax rates
        /// </summary>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>taxA</term>
        /// <description>Tax rate A</description>
        /// </item>
        /// <item>
        /// <term>taxB</term>
        /// <description>Tax rate B</description>
        /// </item>
        /// <item>
        /// <term>taxC</term>
        /// <description>Tax rate C</description>
        /// </item>
        /// <item>
        /// <term>taxD</term>
        /// <description>Tax rate D</description>
        /// </item>
        /// <item>
        /// <term>taxE</term>
        /// <description>Tax rate E</description>
        /// </item>
        /// <item>
        /// <term>taxF</term>
        /// <description>Tax rate F</description>
        /// </item>
        /// <item>
        /// <term>taxG</term>
        /// <description>Tax rate G</description>
        /// </item>
        /// <item>
        /// <term>taxH</term>
        /// <description>Tax rate H</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_TaxRates()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(97, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["taxA"] = split[0];
            if (split.Length >= 2)
                result["taxB"] = split[1];
            if (split.Length >= 3)
                result["taxC"] = split[2];
            if (split.Length >= 4)
                result["taxD"] = split[3];
            if (split.Length >= 5)
                result["taxE"] = split[4];
            if (split.Length >= 6)
                result["taxF"] = split[5];
            if (split.Length >= 7)
                result["taxG"] = split[6];
            if (split.Length >= 8)
                result["taxH"] = split[7];
            return result;
        }

        // Command number(Dec): 99 - please check fiscal device documentation.
        /// <summary>
        /// Read UIC
        /// </summary>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>taxA</term>
        /// <description>Tax rate A</description>
        /// </item>
        /// <item>
        /// <term>taxB</term>
        /// <description>Tax rate B</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_EIK()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(99, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["eikValue"] = split[0];
            if (split.Length >= 2)
                result["eikName"] = split[1];
            return result;
        }

        // Command number(Dec): 103 - please check fiscal device documentation.
        /// <summary>
        /// Get current receipt information
        /// </summary>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>canVd</term>
        /// <description>Is it possible return (strono)</description>
        /// </item>
        /// <item>
        /// <term>taxA</term>
        /// <description>Tax A accumulated sum</description>
        /// </item>
        /// <item>
        /// <term>taxB</term>
        /// <description>Tax B accumulated sum</description>
        /// </item>
        /// <item>
        /// <term>taxC</term>
        /// <description>Tax C accumulated sum</description>
        /// </item>
        /// <item>
        /// <term>taxD</term>
        /// <description>Tax D accumulated sum</description>
        /// </item>
        /// <item>
        /// <term>taxE</term>
        /// <description>Tax E accumulated sum</description>
        /// </item>
        /// <item>
        /// <term>taxF</term>
        /// <description>Tax F accumulated sum</description>
        /// </item>
        /// <item>
        /// <term>taxG</term>
        /// <description>Tax G accumulated sum</description>
        /// </item>
        /// <item>
        /// <term>taxH</term>
        /// <description>Tax H accumulated sum</description>
        /// </item>
        /// <item>
        /// <term>inv</term>
        /// <description>Is it invoice open? '0' - No; '1' - Yes</description>
        /// </item>
        /// <item>
        /// <term>invNum</term>
        /// <description>Next invoice number</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_CurrentRecieptInfo()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(103, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["canVd"] = split[0];
            if (split.Length >= 2)
                result["taxA"] = split[1];
            if (split.Length >= 3)
                result["taxB"] = split[2];
            if (split.Length >= 4)
                result["taxC"] = split[3];
            if (split.Length >= 5)
                result["taxD"] = split[4];
            if (split.Length >= 6)
                result["taxE"] = split[5];
            if (split.Length >= 7)
                result["taxF"] = split[6];
            if (split.Length >= 8)
                result["taxG"] = split[7];
            if (split.Length >= 9)
                result["taxH"] = split[8];
            if (split.Length >= 10)
                result["inv"] = split[9];
            if (split.Length >= 11)
                result["invNum"] = split[10];
            return result;
        }

        // Command number(Dec): 110 - please check fiscal device documentation.
        /// <summary>
        /// Get additional daily information
        /// </summary>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>cash</term>
        /// <description>Payed in cash</description>
        /// </item>
        /// <item>
        /// <term>credit</term>
        /// <description>Payed with credit</description>
        /// </item>
        /// <item>
        /// <term>debit</term>
        /// <description>Payed with debit</description>
        /// </item>
        /// <item>
        /// <term>cheque</term>
        /// <description>Payed by check</description>
        /// </item>
        /// <item>
        /// <term>payment01</term>
        /// <description>Paid by additional payment types 1</description>
        /// </item>
        /// <item>
        /// <term>payment02</term>
        /// <description>Paid by additional payment types 2</description>
        /// </item>
        /// <item>
        /// <term>payment03</term>
        /// <description>Paid by additional payment types 3</description>
        /// </item>
        /// <item>
        /// <term>payment04</term>
        /// <description>Paid by additional payment types 4</description>
        /// </item>
        /// <item>
        /// <term>closure</term>
        /// <description>Current fiscal closure</description>
        /// </item>
        /// <item>
        /// <term>nextFReceiptNumber</term>
        /// <description>Next fiscal receipt number</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_AdditionalDailyInfo_02()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(110, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["cash"] = split[0];
            if (split.Length >= 2)
                result["credit"] = split[1];
            if (split.Length >= 3)
                result["debit"] = split[2];
            if (split.Length >= 4)
                result["cheque"] = split[3];
            if (split.Length >= 5)
                result["payment01"] = split[4];
            if (split.Length >= 6)
                result["payment02"] = split[5];
            if (split.Length >= 7)
                result["payment03"] = split[6];
            if (split.Length >= 8)
                result["payment04"] = split[7];
            if (split.Length >= 9)
                result["closure"] = split[8];
            if (split.Length >= 10)
                result["nextFReceiptNumber"] = split[9];
            return result;
        }

        // Command number(Dec): 110 - please check fiscal device documentation.
        /// <summary>
        /// Get additional daily information - extended
        /// </summary>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>cash</term>
        /// <description>Payed in cash</description>
        /// </item>
        /// <item>
        /// <term>credit</term>
        /// <description>Payed with credit</description>
        /// </item>
        /// <item>
        /// <term>debit</term>
        /// <description>Payed with debit</description>
        /// </item>
        /// <item>
        /// <term>cheque</term>
        /// <description>Payed by check</description>
        /// </item>
        /// <item>
        /// <term>payment01</term>
        /// <description>Paid by additional payment types 1</description>
        /// </item>
        /// <item>
        /// <term>payment02</term>
        /// <description>Paid by additional payment types 2</description>
        /// </item>
        /// <item>
        /// <term>payment03</term>
        /// <description>Paid by additional payment types 3</description>
        /// </item>
        /// <item>
        /// <term>payment04</term>
        /// <description>Paid by additional payment types 4</description>
        /// </item>
        /// <item>
        /// <term>payment05</term>
        /// <description>Paid by additional payment types 5</description>
        /// </item>
        /// <item>
        /// <term>payment06</term>
        /// <description>Paid by additional payment types 6</description>
        /// </item>
        /// <item>
        /// <term>payment07</term>
        /// <description>Paid by additional payment types 7</description>
        /// </item>
        /// <item>
        /// <term>payment08</term>
        /// <description>Paid by additional payment types 8</description>
        /// </item>
        /// <item>
        /// <term>payment09</term>
        /// <description>Paid by additional payment types 9</description>
        /// </item>
        /// <item>
        /// <term>payment10</term>
        /// <description>Paid by additional payment types 10</description>
        /// </item>
        /// <item>
        /// <term>payment11</term>
        /// <description>Paid by additional payment types 11</description>
        /// </item>
        /// <item>
        /// <term>closure</term>
        /// <description>Current fiscal closure</description>
        /// </item>
        /// <item>
        /// <term>nextFReceiptNumber</term>
        /// <description>Next fiscal receipt number</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_AdditionalDailyInfo_03()
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("*");

            string r = CustomCommand(110, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["cash"] = split[0];
            if (split.Length >= 2)
                result["credit"] = split[1];
            if (split.Length >= 3)
                result["debit"] = split[2];
            if (split.Length >= 4)
                result["cheque"] = split[3];
            if (split.Length >= 5)
                result["payment01"] = split[4];
            if (split.Length >= 6)
                result["payment02"] = split[5];
            if (split.Length >= 7)
                result["payment03"] = split[6];
            if (split.Length >= 8)
                result["payment04"] = split[7];
            if (split.Length >= 9)
                result["closure"] = split[8];
            if (split.Length >= 10)
                result["nextFReceiptNumber"] = split[9];
            if (split.Length >= 11)
                result["payment05"] = split[10];
            if (split.Length >= 12)
                result["payment06"] = split[11];
            if (split.Length >= 13)
                result["payment07"] = split[12];
            if (split.Length >= 14)
                result["payment08"] = split[13];
            if (split.Length >= 15)
                result["payment09"] = split[14];
            if (split.Length >= 16)
                result["payment10"] = split[15];
            if (split.Length >= 17)
                result["payment11"] = split[16];
            return result;
        }

        // Command number(Dec): 112 - please check fiscal device documentation.
        /// <summary>
        /// Get operator's data
        /// </summary>
        /// <param name="wpOperator">Operator ID (between 1 and 16).</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>receiptsCount</term>
        /// <description>Receipts count from selected operator</description>
        /// </item>
        /// <item>
        /// <term>salesCount</term>
        /// <description>Sales count</description>
        /// </item>
        /// <item>
        /// <term>salesSum</term>
        /// <description>Sales sum</description>
        /// </item>
        /// <item>
        /// <term>discountsCount</term>
        /// <description>Discounts count</description>
        /// </item>
        /// <item>
        /// <term>discountsSum</term>
        /// <description>Discounts sum</description>
        /// </item>
        /// <item>
        /// <term>surchargesCount</term>
        /// <description>Surcharges count</description>
        /// </item>
        /// <item>
        /// <term>surchargesSum</term>
        /// <description>Surcharges sum</description>
        /// </item>
        /// <item>
        /// <term>voidsCount</term>
        /// <description>Voids count</description>
        /// </item>
        /// <item>
        /// <term>voidsSum</term>
        /// <description>Void sum</description>
        /// </item>
        /// <item>
        /// <term>operatorName</term>
        /// <description>Operator's name</description>
        /// </item>
        /// <item>
        /// <term>operatorPassword</term>
        /// <description>Operator's password</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_OperatorsData(string wpOperator)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(wpOperator);

            string r = CustomCommand(112, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["receiptsCount"] = split[0];
            if (split.Length >= 2)
                result["salesCount"] = split[1];
            if (split.Length >= 3)
                result["salesSum"] = split[2];
            if (split.Length >= 4)
                result["discountsCount"] = split[3];
            if (split.Length >= 5)
                result["discountsSum"] = split[4];
            if (split.Length >= 6)
                result["surchargesCount"] = split[5];
            if (split.Length >= 7)
                result["surchargesSum"] = split[6];
            if (split.Length >= 8)
                result["voidsCount"] = split[7];
            if (split.Length >= 9)
                result["voidsSum"] = split[8];
            if (split.Length >= 10)
                result["operatorName"] = split[9];
            if (split.Length >= 11)
                result["operatorPassword"] = split[10];
            return result;
        }

        // Command number(Dec): 113 - please check fiscal device documentation.
        /// <summary>
        /// Get last document number
        /// </summary>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>documentNumber</term>
        /// <description>Last issued document number (7 digits).</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_LastDocumentNumber()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(113, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["documentNumber"] = split[0];
            return result;
        }

        // Command number(Dec): 114 - please check fiscal device documentation.
        /// <summary>
        /// Get active taxes
        /// </summary>
        /// <param name="closureNumber">Fiscal record number</param>
        /// <param name="option">'0' - Information on the active tax rates for the Z-report record in question</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - Command is declined.
        /// </description>
        /// </item>
        /// <item>
        /// <term>taxRecordNumber</term>
        /// <description>Tax record number.</description>
        /// </item>
        /// <item>
        /// <term>decimalsCount</term>
        /// <description>Decimal point count for Z report record .</description>
        /// </item>
        /// <item>
        /// <term>enabled</term>
        /// <description>Enabled tax rates (1 - enabled).</description>
        /// </item>
        /// <item>
        /// <term>taxRateA</term>
        /// <description> tax rate A in percents.</description>
        /// </item>
        /// <item>
        /// <term>taxRateB</term>
        /// <description>tax rate B in percents.</description>
        /// </item>
        /// <item>
        /// <term>taxRateC</term>
        /// <description>tax rate C in percents.</description>
        /// </item>
        /// <item>
        /// <term>taxRateD</term>
        /// <description>tax rate D in percents.</description>
        /// </item>
        /// <item>
        /// <term>taxRateE</term>
        /// <description>tax rate E in percents.</description>
        /// </item>
        /// <item>
        /// <term>taxRateF</term>
        /// <description>tax rate F in percents.</description>
        /// </item>
        /// <item>
        /// <term>taxRateG</term>
        /// <description>tax rate G in percents.</description>
        /// </item>
        /// <item>
        /// <term>taxRateH</term>
        /// <description>tax rate H in percents.</description>
        /// </item>
        /// <item>
        /// <term>dateTime</term>
        /// <description>Date and time (format: DD-MM-YY hh:mm:ss).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_ActiveTaxes(string closureNumber, string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(closureNumber);
            inputString.Append(",");
            inputString.Append(option);

            string r = CustomCommand(114, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["taxRecordNumber"] = split[1];
            if (split.Length >= 3)
                result["decimalsCount"] = split[2];
            if (split.Length >= 4)
                result["enabled"] = split[3];
            if (split.Length >= 5)
                result["taxRateA"] = split[4];
            if (split.Length >= 6)
                result["taxRateB"] = split[5];
            if (split.Length >= 7)
                result["taxRateC"] = split[6];
            if (split.Length >= 8)
                result["taxRateD"] = split[7];
            if (split.Length >= 9)
                result["taxRateE"] = split[8];
            if (split.Length >= 10)
                result["taxRateF"] = split[9];
            if (split.Length >= 11)
                result["taxRateG"] = split[10];
            if (split.Length >= 12)
                result["taxRateH"] = split[11];
            if (split.Length >= 13)
                result["dateTime"] = split[12];
            return result;
        }

        // Command number(Dec): 114 - please check fiscal device documentation.
        /// <summary>
        /// Information on the sales for the specified record or period
        /// </summary>
        /// <param name="closure1">Fiscal memory record number.</param>
        /// <param name="option">'1' - Information on the sales for the specified record or period.</param>
        /// <param name="closure2">Fiscal record number</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - Command is declined.
        /// </description>
        /// </item>
        /// <item>
        /// <term>closureNumber</term>
        /// <description>Closure number for given period.</description>
        /// </item>
        /// <item>
        /// <term>receiptsCount</term>
        /// <description>Decimal point count for Z report record .</description>
        /// </item>
        /// <item>
        /// <term>totTaxA</term>
        /// <description> Turnover for tax group A.</description>
        /// </item>
        /// <item>
        /// <term>totTaxB</term>
        /// <description>Turnover for tax group B.</description>
        /// </item>
        /// <item>
        /// <term>totTaxC</term>
        /// <description>Turnover for tax group C.</description>
        /// </item>
        /// <item>
        /// <term>totTaxD</term>
        /// <description>Turnover for tax group D.</description>
        /// </item>
        /// <item>
        /// <term>totTaxE</term>
        /// <description>Turnover for tax group E.</description>
        /// </item>
        /// <item>
        /// <term>totTaxF</term>
        /// <description>Turnover for tax group F.</description>
        /// </item>
        /// <item>
        /// <term>totTaxG</term>
        /// <description>Turnover for tax group G.</description>
        /// </item>
        /// <item>
        /// <term>totTaxH</term>
        /// <description>Turnover for tax group H.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_Sums(string closure1, string option, string closure2)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(closure1);
            inputString.Append(",");
            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(closure2);

            string r = CustomCommand(114, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["closureNumber"] = split[1];
            if (split.Length >= 3)
                result["receiptsCount"] = split[2];
            if (split.Length >= 4)
                result["totTaxA"] = split[3];
            if (split.Length >= 5)
                result["totTaxB"] = split[4];
            if (split.Length >= 6)
                result["totTaxC"] = split[5];
            if (split.Length >= 7)
                result["totTaxD"] = split[6];
            if (split.Length >= 8)
                result["totTaxE"] = split[7];
            if (split.Length >= 9)
                result["totTaxF"] = split[8];
            if (split.Length >= 10)
                result["totTaxG"] = split[9];
            if (split.Length >= 11)
                result["totTaxH"] = split[10];
            return result;
        }

        // Command number(Dec): 114 - please check fiscal device documentation.
        /// <summary>
        /// Information on the storno sales for the specified record or period
        /// </summary>
        /// <param name="closure1">Fiscal memory record number.</param>
        /// <param name="option">'1*' - Information on the sales for the specified record or period for STORNO.</param>
        /// <param name="closure2">Fiscal record number</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - Command is declined.
        /// </description>
        /// </item>
        /// <item>
        /// <term>closureCnt</term>
        /// <description>Closure count for given period.</description>
        /// </item>
        /// <item>
        /// <term>refundStornoCnt</term>
        /// <description>Count of STORNO receipts - type REFUND.</description>
        /// </item>
        /// <item>
        /// <term>operatorErrStornoCnt</term>
        /// <description>Count of STORNO receipts - type OPERATOR'S ERROR.</description>
        /// </item>
        /// <item>
        /// <term>taxBaseStornoCnt</term>
        /// <description>Count of STORNO receipts - type TAX BASE REDUCTION.</description>
        /// </item>
        /// <item>
        /// <term>refundSalesCnt</term>
        /// <description>Sales count of STORNO receipts type REFUND.</description>
        /// </item>
        /// <item>
        /// <term>operatorErrSalesCnt</term>
        /// <description>Sales count of STORNO receipts type OPERATOR'S ERROR.</description>
        /// </item>
        /// <item>
        /// <term>taxBaseSalesCnt</term>
        /// <description>Sales count of STORNO receipts type TAX BASE REDUCTION.</description>
        /// </item>
        /// <item>
        /// <term>totTaxA</term>
        /// <description> Turnover for tax group A.</description>
        /// </item>
        /// <item>
        /// <term>totTaxB</term>
        /// <description>Turnover for tax group B.</description>
        /// </item>
        /// <item>
        /// <term>totTaxC</term>
        /// <description>Turnover for tax group C.</description>
        /// </item>
        /// <item>
        /// <term>totTaxD</term>
        /// <description>Turnover for tax group D.</description>
        /// </item>
        /// <item>
        /// <term>totTaxE</term>
        /// <description>Turnover for tax group E.</description>
        /// </item>
        /// <item>
        /// <term>totTaxF</term>
        /// <description>Turnover for tax group F.</description>
        /// </item>
        /// <item>
        /// <term>totTaxG</term>
        /// <description>Turnover for tax group G.</description>
        /// </item>
        /// <item>
        /// <term>totTaxH</term>
        /// <description>Turnover for tax group H.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_StornoSums(string closure1, string option, string closure2)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(closure1);
            inputString.Append(",");
            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(closure2);

            string r = CustomCommand(114, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["closureCnt"] = split[1];
            if (split.Length >= 3)
                result["refundStornoCnt"] = split[2];
            if (split.Length >= 4)
                result["operatorErrStornoCnt"] = split[3];
            if (split.Length >= 5)
                result["taxBaseStornoCnt"] = split[4];
            if (split.Length >= 6)
                result["refundSalesCnt"] = split[5];
            if (split.Length >= 7)
                result["operatorErrSalesCnt"] = split[6];
            if (split.Length >= 8)
                result["taxBaseSalesCnt"] = split[7];
            if (split.Length >= 9)
                result["totTaxA"] = split[8];
            if (split.Length >= 10)
                result["totTaxB"] = split[9];
            if (split.Length >= 11)
                result["totTaxC"] = split[10];
            if (split.Length >= 12)
                result["totTaxD"] = split[11];
            if (split.Length >= 13)
                result["totTaxE"] = split[12];
            if (split.Length >= 14)
                result["totTaxF"] = split[13];
            if (split.Length >= 15)
                result["totTaxG"] = split[14];
            if (split.Length >= 16)
                result["totTaxH"] = split[15];
            return result;
        }

        // Command number(Dec): 114 - please check fiscal device documentation.
        /// <summary>
        /// Information on the net amounts for the specified record or period
        /// </summary>
        /// <param name="closure1">Fiscal memory record number</param>
        /// <param name="option">'2' - Information on the net amounts for the specified record or period</param>
        /// <param name="closure2">Fiscal memory record number</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - Command is declined.
        /// </description>
        /// </item>
        /// <item>
        /// <term>closureNumber</term>
        /// <description>Closure number for given period.</description>
        /// </item>
        /// <item>
        /// <term>receiptsCount</term>
        /// <description>Decimal point count for Z report record .</description>
        /// </item>
        /// <item>
        /// <term>nettoTaxA</term>
        /// <description> Netto amount for tax group A.</description>
        /// </item>
        /// <item>
        /// <term>nettoTaxB</term>
        /// <description>Turnover for tax group B.</description>
        /// </item>
        /// <item>
        /// <term>nettoTaxC</term>
        /// <description>Turnover for tax group C.</description>
        /// </item>
        /// <item>
        /// <term>nettoTaxD</term>
        /// <description>Turnover for tax group D.</description>
        /// </item>
        /// <item>
        /// <term>nettoTaxE</term>
        /// <description>Turnover for tax group E.</description>
        /// </item>
        /// <item>
        /// <term>nettoTaxF</term>
        /// <description>Turnover for tax group F.</description>
        /// </item>
        /// <item>
        /// <term>nettoTaxG</term>
        /// <description>Turnover for tax group G.</description>
        /// </item>
        /// <item>
        /// <term>nettoTaxH</term>
        /// <description>Turnover for tax group H.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_NettoSums(string closure1, string option, string closure2)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(closure1);
            inputString.Append(",");
            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(closure2);

            string r = CustomCommand(114, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["closureNumber"] = split[1];
            if (split.Length >= 3)
                result["receiptsCount"] = split[2];
            if (split.Length >= 4)
                result["nettoTaxA"] = split[3];
            if (split.Length >= 5)
                result["nettoTaxB"] = split[4];
            if (split.Length >= 6)
                result["nettoTaxC"] = split[5];
            if (split.Length >= 7)
                result["nettoTaxD"] = split[6];
            if (split.Length >= 8)
                result["nettoTaxE"] = split[7];
            if (split.Length >= 9)
                result["nettoTaxF"] = split[8];
            if (split.Length >= 10)
                result["nettoTaxG"] = split[9];
            if (split.Length >= 11)
                result["nettoTaxH"] = split[10];
            return result;
        }

        // Command number(Dec): 114 - please check fiscal device documentation.
        /// <summary>
        /// Information on the storno netto sums for the specified record or period
        /// </summary>
        /// <param name="closure1">Fiscal memory record number.</param>
        /// <param name="option">'2*' - Information on the netto sums for the specified record or period for STORNO.</param>
        /// <param name="closure2">Fiscal record number</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - Command is declined.
        /// </description>
        /// </item>
        /// <item>
        /// <term>closureCnt</term>
        /// <description>Closure count for given period.</description>
        /// </item>
        /// <item>
        /// <term>refundStornoCnt</term>
        /// <description>Count of STORNO receipts - type REFUND.</description>
        /// </item>
        /// <item>
        /// <term>operatorErrStornoCnt</term>
        /// <description>Count of STORNO receipts - type OPERATOR'S ERROR.</description>
        /// </item>
        /// <item>
        /// <term>taxBaseStornoCnt</term>
        /// <description>Count of STORNO receipts - type TAX BASE REDUCTION.</description>
        /// </item>
        /// <item>
        /// <term>refundSalesCnt</term>
        /// <description>Sales count of STORNO receipts type REFUND.</description>
        /// </item>
        /// <item>
        /// <term>operatorErrSalesCnt</term>
        /// <description>Sales count of STORNO receipts type OPERATOR'S ERROR.</description>
        /// </item>
        /// <item>
        /// <term>taxBaseSalesCnt</term>
        /// <description>Sales count of STORNO receipts type TAX BASE REDUCTION.</description>
        /// </item>
        /// <item>
        /// <term>nettoTaxA</term>
        /// <description> Netto sum for tax group A.</description>
        /// </item>
        /// <item>
        /// <term>nettoTaxB</term>
        /// <description>Netto sum for tax group B.</description>
        /// </item>
        /// <item>
        /// <term>nettoTaxC</term>
        /// <description>Netto sum for tax group C.</description>
        /// </item>
        /// <item>
        /// <term>nettoTaxD</term>
        /// <description>Netto sum for tax group D.</description>
        /// </item>
        /// <item>
        /// <term>nettoTaxE</term>
        /// <description>Netto sum for tax group E.</description>
        /// </item>
        /// <item>
        /// <term>nettoTaxF</term>
        /// <description>Netto sum for tax group F.</description>
        /// </item>
        /// <item>
        /// <term>nettoTaxG</term>
        /// <description>Netto sum for tax group G.</description>
        /// </item>
        /// <item>
        /// <term>nettoTaxH</term>
        /// <description>Netto sum for tax group H.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_Storno_NettoSums(string closure1, string option, string closure2)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(closure1);
            inputString.Append(",");
            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(closure2);

            string r = CustomCommand(114, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["closureCnt"] = split[1];
            if (split.Length >= 3)
                result["refundStornoCnt"] = split[2];
            if (split.Length >= 4)
                result["operatorErrStornoCnt"] = split[3];
            if (split.Length >= 5)
                result["taxBaseStornoCnt"] = split[4];
            if (split.Length >= 6)
                result["refundSalesCnt"] = split[5];
            if (split.Length >= 7)
                result["operatorErrSalesCnt"] = split[6];
            if (split.Length >= 8)
                result["taxBaseSalesCnt"] = split[7];
            if (split.Length >= 9)
                result["nettoTaxA"] = split[8];
            if (split.Length >= 10)
                result["nettoTaxB"] = split[9];
            if (split.Length >= 11)
                result["nettoTaxC"] = split[10];
            if (split.Length >= 12)
                result["nettoTaxD"] = split[11];
            if (split.Length >= 13)
                result["nettoTaxE"] = split[12];
            if (split.Length >= 14)
                result["nettoTaxF"] = split[13];
            if (split.Length >= 15)
                result["nettoTaxG"] = split[14];
            if (split.Length >= 16)
                result["nettoTaxH"] = split[15];
            return result;
        }

        // Command number(Dec): 114 - please check fiscal device documentation.
        /// <summary>
        /// Information on the VAT assessed for the specified record or period
        /// </summary>
        /// <param name="closure1">Fiscal memory record number</param>
        /// <param name="option">'3' - Information on the VAT assessed for the specified record or period</param>
        /// <param name="closure2">Fiscal memory record number</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - Command is declined.
        /// </description>
        /// </item>
        /// <item>
        /// <term>closureNumber</term>
        /// <description>Closure number for given period.</description>
        /// </item>
        /// <item>
        /// <term>receiptsCount</term>
        /// <description>Decimal point count for Z report record .</description>
        /// </item>
        /// <item>
        /// <term>vATTaxA</term>
        /// <description> Vat (DDS) charged for tax group A.</description>
        /// </item>
        /// <item>
        /// <term>vATTaxB</term>
        /// <description>Vat (DDS) charged for tax group B.</description>
        /// </item>
        /// <item>
        /// <term>vATTaxC</term>
        /// <description>Vat (DDS) charged for tax group C.</description>
        /// </item>
        /// <item>
        /// <term>vATTaxD</term>
        /// <description>Vat (DDS) charged for tax group D.</description>
        /// </item>
        /// <item>
        /// <term>vATTaxE</term>
        /// <description>Vat (DDS) charged for tax group E.</description>
        /// </item>
        /// <item>
        /// <term>vATTaxF</term>
        /// <description>Vat (DDS) charged for tax group F.</description>
        /// </item>
        /// <item>
        /// <term>vATTaxG</term>
        /// <description>Vat (DDS) charged for tax group G.</description>
        /// </item>
        /// <item>
        /// <term>vATTaxH</term>
        /// <description>Vat (DDS) charged for tax group H.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_VATSums(string closure1, string option, string closure2)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(closure1);
            inputString.Append(",");
            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(closure2);

            string r = CustomCommand(114, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["closureNumber"] = split[1];
            if (split.Length >= 3)
                result["receiptsCount"] = split[2];
            if (split.Length >= 4)
                result["vATTaxA"] = split[3];
            if (split.Length >= 5)
                result["vATTaxB"] = split[4];
            if (split.Length >= 6)
                result["vATTaxC"] = split[5];
            if (split.Length >= 7)
                result["vATTaxD"] = split[6];
            if (split.Length >= 8)
                result["vATTaxE"] = split[7];
            if (split.Length >= 9)
                result["vATTaxF"] = split[8];
            if (split.Length >= 10)
                result["vATTaxG"] = split[9];
            if (split.Length >= 11)
                result["vATTaxH"] = split[10];
            return result;
        }

        // Command number(Dec): 114 - please check fiscal device documentation.
        /// <summary>
        /// Information on the storno VAT assessed for the specified record or period
        /// </summary>
        /// <param name="closure1">Fiscal memory record number</param>
        /// <param name="option">'3*' - Information on Storno VAT assessed for the specified record or period</param>
        /// <param name="closure2">Fiscal memory record number</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - Command is declined.
        /// </description>
        /// </item>
        /// <item>
        /// <term>closureCnt</term>
        /// <description>Closure count for given period.</description>
        /// </item>
        /// <item>
        /// <term>refundStornoCnt</term>
        /// <description>Count of STORNO receipts - type REFUND.</description>
        /// </item>
        /// <item>
        /// <term>operatorErrStornoCnt</term>
        /// <description>Count of STORNO receipts - type OPERATOR'S ERROR.</description>
        /// </item>
        /// <item>
        /// <term>taxBaseStornoCnt</term>
        /// <description>Count of STORNO receipts - type TAX BASE REDUCTION.</description>
        /// </item>
        /// <item>
        /// <term>refundSalesCnt</term>
        /// <description>Sales count of STORNO receipts type REFUND.</description>
        /// </item>
        /// <item>
        /// <term>operatorErrSalesCnt</term>
        /// <description>Sales count of STORNO receipts type OPERATOR'S ERROR.</description>
        /// </item>
        /// <item>
        /// <term>taxBaseSalesCnt</term>
        /// <description>Sales count of STORNO receipts type TAX BASE REDUCTION.</description>
        /// </item>
        /// <item>
        /// <term>vATTaxA</term>
        /// <description> Vat (DDS) charged for tax group A.</description>
        /// </item>
        /// <item>
        /// <term>vATTaxB</term>
        /// <description>Vat (DDS) charged for tax group B.</description>
        /// </item>
        /// <item>
        /// <term>vATTaxC</term>
        /// <description>Vat (DDS) charged for tax group C.</description>
        /// </item>
        /// <item>
        /// <term>vATTaxD</term>
        /// <description>Vat (DDS) charged for tax group D.</description>
        /// </item>
        /// <item>
        /// <term>vATTaxE</term>
        /// <description>Vat (DDS) charged for tax group E.</description>
        /// </item>
        /// <item>
        /// <term>vATTaxF</term>
        /// <description>Vat (DDS) charged for tax group F.</description>
        /// </item>
        /// <item>
        /// <term>vATTaxG</term>
        /// <description>Vat (DDS) charged for tax group G.</description>
        /// </item>
        /// <item>
        /// <term>vATTaxH</term>
        /// <description>Vat (DDS) charged for tax group H.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_Storno_VATSums(string closure1, string option, string closure2)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(closure1);
            inputString.Append(",");
            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(closure2);

            string r = CustomCommand(114, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["closureCnt"] = split[1];
            if (split.Length >= 3)
                result["refundStornoCnt"] = split[2];
            if (split.Length >= 4)
                result["operatorErrStornoCnt"] = split[3];
            if (split.Length >= 5)
                result["taxBaseStornoCnt"] = split[4];
            if (split.Length >= 6)
                result["refundSalesCnt"] = split[5];
            if (split.Length >= 7)
                result["operatorErrSalesCnt"] = split[6];
            if (split.Length >= 8)
                result["taxBaseSalesCnt"] = split[7];
            if (split.Length >= 9)
                result["vATTaxA"] = split[8];
            if (split.Length >= 10)
                result["vATTaxB"] = split[9];
            if (split.Length >= 11)
                result["vATTaxC"] = split[10];
            if (split.Length >= 12)
                result["vATTaxD"] = split[11];
            if (split.Length >= 13)
                result["vATTaxE"] = split[12];
            if (split.Length >= 14)
                result["vATTaxF"] = split[13];
            if (split.Length >= 15)
                result["vATTaxG"] = split[14];
            if (split.Length >= 16)
                result["vATTaxH"] = split[15];
            return result;
        }

        // Command number(Dec): 114 - please check fiscal device documentation.
        /// <summary>
        /// Additional information on the specified record
        /// </summary>
        /// <param name="closure1">Fiscal memory record number</param>
        /// <param name="option">'4' - Additional information on the specified record</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - Command is declined.</description>
        /// </item>
        /// <item>
        /// <term>closureNumber</term>
        /// <description>Closure number for given period.</description>
        /// </item>
        /// <item>
        /// <term>taxRecordNumber</term>
        /// <description>Last active tax record number for given period.</description>
        /// </item>
        /// <item>
        /// <term>resetRecordNumber</term>
        /// <description> Last RAM reset to this fiscal block.</description>
        /// </item>
        /// <item>
        /// <term>kLENNumber</term>
        /// <description>Number of electronic journal for this fiscal block.</description>
        /// </item>
        /// <item>
        /// <term>dateTime</term>
        /// <description>Date and time for the data (format: DD-MM-YY hh:mm:ss).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_Numbers(string closure1, string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(closure1);
            inputString.Append(",");
            inputString.Append(option);

            string r = CustomCommand(114, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["closureNumber"] = split[1];
            if (split.Length >= 3)
                result["taxRecordNumber"] = split[2];
            if (split.Length >= 4)
                result["resetRecordNumber"] = split[3];
            if (split.Length >= 5)
                result["kLENNumber"] = split[4];
            if (split.Length >= 6)
                result["dateTime"] = split[5];
            return result;
        }

        // Command number(Dec): 114 - please check fiscal device documentation.
        /// <summary>
        /// Information on the specified fiscal memory record used to set tax rates
        /// </summary>
        /// <param name="closure1">Fiscal memory record number</param>
        /// <param name="option">'5' - Information on the specified fiscal memory record used to set tax rates</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - Command is declined.
        /// </description>
        /// </item>
        /// <item>
        /// <term>decimalsCount</term>
        /// <description>Decimal point count for Z report record .</description>
        /// </item>
        /// <item>
        /// <term>enabled</term>
        /// <description>Enabled tax rates (1 - enabled).</description>
        /// </item>
        /// <item>
        /// <term>taxRateA</term>
        /// <description> tax rate A in percents.</description>
        /// </item>
        /// <item>
        /// <term>taxRateB</term>
        /// <description>tax rate B in percents.</description>
        /// </item>
        /// <item>
        /// <term>taxRateC</term>
        /// <description>tax rate C in percents.</description>
        /// </item>
        /// <item>
        /// <term>taxRateD</term>
        /// <description>tax rate D in percents.</description>
        /// </item>
        /// <item>
        /// <term>taxRateE</term>
        /// <description>tax rate E in percents.</description>
        /// </item>
        /// <item>
        /// <term>taxRateF</term>
        /// <description>tax rate F in percents.</description>
        /// </item>
        /// <item>
        /// <term>taxRateG</term>
        /// <description>tax rate G in percents.</description>
        /// </item>
        /// <item>
        /// <term>taxRateH</term>
        /// <description>tax rate H in percents.</description>
        /// </item>
        /// <item>
        /// <term>dateTime</term>
        /// <description>Date and time (format: DD-MM-YY hh:mm:ss).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_TaxRateValues(string closure1, string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(closure1);
            inputString.Append(",");
            inputString.Append(option);

            string r = CustomCommand(114, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["decimalsCount"] = split[1];
            if (split.Length >= 3)
                result["enabled"] = split[2];
            if (split.Length >= 4)
                result["taxRateA"] = split[3];
            if (split.Length >= 5)
                result["taxRateB"] = split[4];
            if (split.Length >= 6)
                result["taxRateC"] = split[5];
            if (split.Length >= 7)
                result["taxRateD"] = split[6];
            if (split.Length >= 8)
                result["taxRateE"] = split[7];
            if (split.Length >= 9)
                result["taxRateF"] = split[8];
            if (split.Length >= 10)
                result["taxRateG"] = split[9];
            if (split.Length >= 11)
                result["taxRateH"] = split[10];
            if (split.Length >= 12)
                result["dateTime"] = split[11];
            return result;
        }

        // Command number(Dec): 114 - please check fiscal device documentation.
        /// <summary>
        /// Information on the specified FM record with RAM reset
        /// </summary>
        /// <param name="closure1">Fiscal memory record number</param>
        /// <param name="option">'6' - Information on the specified FM record with RAM reset</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - Command is declined.
        /// </description>
        /// </item>
        /// <item>
        /// <term>dateTime</term>
        /// <description>Date and time (format: DD-MM-YY hh:mm:ss).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_RAMResetDateTime(string closure1, string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(closure1);
            inputString.Append(",");
            inputString.Append(option);

            string r = CustomCommand(114, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["dateTime"] = split[1];
            return result;
        }

        // Command number(Dec): 114 - please check fiscal device documentation.
        /// <summary>
        /// Information on the amounts by payment type for the specified record or period
        /// </summary>
        /// <param name="closure1">Fiscal memory record number</param>
        /// <param name="option">'7' - Information on the amounts by payment type for the specified record or period</param>
        /// <param name="closure2">Fiscal memory record number</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - Command is declined.
        /// </description>
        /// </item>
        /// <item>
        /// <term>cashpaymentSum</term>
        /// <description>Paid in cash.</description>
        /// </item>
        /// <item>
        /// <term>debitcardpaymentSum</term>
        /// <description>Paid by debit card.</description>
        /// </item>
        /// <item>
        /// <term>creditcardpaymentSum</term>
        /// <description>Paid by credit card.</description>
        /// </item>
        /// <item>
        /// <term>checkpaymentSum</term>
        /// <description>Paid by check.</description>
        /// </item>
        /// <item>
        /// <term>payment01Sum</term>
        /// <description>Additional payment type 1.</description>
        /// </item>
        /// <item>
        /// <term>payment02Sum</term>
        /// <description>Additional payment type 2.</description>
        /// </item>
        /// <item>
        /// <term>payment03Sum</term>
        /// <description>Additional payment type 3.</description>
        /// </item>
        /// <item>
        /// <term>payment04Sum</term>
        /// <description>Additional payment type 4.</description>
        /// </item>
        /// <item>
        /// <term>payment05Sum</term>
        /// <description>Additional payment type 5.</description>
        /// </item>
        /// <item>
        /// <term>payment06Sum</term>
        /// <description>Additional payment type 6.</description>
        /// </item>
        /// <item>
        /// <term>payment07Sum</term>
        /// <description>Additional payment type 7.</description>
        /// </item>
        /// <item>
        /// <term>payment08Sum</term>
        /// <description>Additional payment type 8.</description>
        /// </item>
        /// <item>
        /// <term>payment09Sum</term>
        /// <description>Additional payment type 9.</description>
        /// </item>
        /// <item>
        /// <term>payment10Sum</term>
        /// <description>Additional payment type 10.</description>
        /// </item>
        /// <item>
        /// <term>payment11Sum</term>
        /// <description>Additional payment type 11.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_AdditionalPayments(string closure1, string option, string closure2)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(closure1);
            inputString.Append(",");
            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(closure2);

            string r = CustomCommand(114, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["cashpaymentSum"] = split[1];
            if (split.Length >= 3)
                result["debitcardpaymentSum"] = split[2];
            if (split.Length >= 4)
                result["creditcardpaymentSum"] = split[3];
            if (split.Length >= 5)
                result["checkpaymentSum"] = split[4];
            if (split.Length >= 6)
                result["payment01Sum"] = split[5];
            if (split.Length >= 7)
                result["payment02Sum"] = split[6];
            if (split.Length >= 8)
                result["payment03Sum"] = split[7];
            if (split.Length >= 9)
                result["payment04Sum"] = split[8];
            if (split.Length >= 10)
                result["payment05Sum"] = split[9];
            if (split.Length >= 11)
                result["payment06Sum"] = split[10];
            if (split.Length >= 12)
                result["payment07Sum"] = split[11];
            if (split.Length >= 13)
                result["payment08Sum"] = split[12];
            if (split.Length >= 14)
                result["payment09Sum"] = split[13];
            if (split.Length >= 15)
                result["payment10Sum"] = split[14];
            if (split.Length >= 16)
                result["payment11Sum"] = split[15];
            return result;
        }

        // Command number(Dec): 114 - please check fiscal device documentation.
        /// <summary>
        /// Information on the amounts by payment type for the specified record or period
        /// </summary>
        /// <param name="closure1">Fiscal record number</param>
        /// <param name="option">'8' - Information on the amounts by payment type for the specified record or period</param>
        /// <param name="closure2">Fiscal record number</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - Command is declined.
        /// </description>
        /// </item>
        /// <item>
        /// <term>discountsCount</term>
        /// <description>Number of discounts.</description>
        /// </item>
        /// <item>
        /// <term>discountsSum</term>
        /// <description>Accumulated discount sum.</description>
        /// </item>
        /// <item>
        /// <term>surchargesCount</term>
        /// <description>Number of surcharges.</description>
        /// </item>
        /// <item>
        /// <term>surchargesSum</term>
        /// <description>Accumulated surcharges sum.</description>
        /// </item>
        /// <item>
        /// <term>voidedCount</term>
        /// <description>Number of voided operations.</description>
        /// </item>
        /// <item>
        /// <term>voidedSum</term>
        /// <description>Accumulated voided sum.</description>
        /// </item>
        /// <item>
        /// <term>canceledCount</term>
        /// <description>Number of canceled receipts.</description>
        /// </item>
        /// <item>
        /// <term>canceledSum</term>
        /// <description>Accumulated canceled sum.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_Corrections(string closure1, string option, string closure2)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(closure1);
            inputString.Append(",");
            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(closure2);

            string r = CustomCommand(114, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["discountsCount"] = split[1];
            if (split.Length >= 3)
                result["discountsSum"] = split[2];
            if (split.Length >= 4)
                result["surchargesCount"] = split[3];
            if (split.Length >= 5)
                result["surchargesSum"] = split[4];
            if (split.Length >= 6)
                result["voidedCount"] = split[5];
            if (split.Length >= 7)
                result["voidedSum"] = split[6];
            if (split.Length >= 8)
                result["canceledCount"] = split[7];
            if (split.Length >= 9)
                result["canceledSum"] = split[8];
            return result;
        }

        // Command number(Dec): 114 - please check fiscal device documentation.
        /// <summary>
        /// Get cash in/ cash out
        /// </summary>
        /// <param name="closure1">Fiscal record number</param>
        /// <param name="option">'9' - Information on the amounts by payment type for the specified record or period</param>
        /// <param name="closure2">Fiscal record number</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - Command is declined.
        /// </description>
        /// </item>
        /// <item>
        /// <term>cashinCount</term>
        /// <description>Number of cash in operations.</description>
        /// </item>
        /// <item>
        /// <term>cashinSum</term>
        /// <description>Accumulated cash in sum.</description>
        /// </item>
        /// <item>
        /// <term>cashoutCount</term>
        /// <description>Number of cash out operations.</description>
        /// </item>
        /// <item>
        /// <term>cashoutSum</term>
        /// <description>Accumulated cash out sum.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_CashInCashOut(string closure1, string option, string closure2)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(closure1);
            inputString.Append(",");
            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(closure2);

            string r = CustomCommand(114, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["cashinCount"] = split[1];
            if (split.Length >= 3)
                result["cashinSum"] = split[2];
            if (split.Length >= 4)
                result["cashoutCount"] = split[3];
            if (split.Length >= 5)
                result["cashoutSum"] = split[4];
            return result;
        }

        // Command number(Dec): 114 - please check fiscal device documentation.
        /// <summary>
        /// Information on the total sales and VAT before the specified Z-report
        /// </summary>
        /// <param name="closure1">Fiscal memory record number</param>
        /// <param name="option">'10' - Information on the total sales and VAT before the specified Z-report</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - Command is declined.
        /// </description>
        /// </item>
        /// <item>
        /// <term>turnoverSum</term>
        /// <description>Accumulated sales up to and including the specified daily report.</description>
        /// </item>
        /// <item>
        /// <term>turnoverVATSum</term>
        /// <description>Accumulated VAT up to and including the specified daily report.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_Turnover(string closure1, string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(closure1);
            inputString.Append(",");
            inputString.Append(option);

            string r = CustomCommand(114, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["turnoverSum"] = split[1];
            if (split.Length >= 3)
                result["turnoverVATSum"] = split[2];
            return result;
        }

        /// <summary>
        /// ONLY for type 3 and 31 printers! Information on the programmed fuel FTT and names for this Z-report. Only for type 3
        /// and 31 printers.
        /// </summary>
        /// <param name="closure1">Fiscal memory record number</param>
        /// <param name="option">'11' - Information on the programmed fuel FTT and names for this Z-report. Only for type 3 and 31 printers.</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - Command is declined.
        /// </description>
        /// </item>
        /// <item>
        /// <term>data</term>
        /// <description>Data in format: Programmed fuel FTT1,Programmed fuel name ... FFTn,Fuel name n</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_ProgrammedFuel(string closure1, string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(closure1);
            inputString.Append(",");
            inputString.Append(option);

            string r = CustomCommand(114, inputString.ToString());
            CheckResult();
            char[] charSeparator = new char[] { ',' };
            string[] split = r.Split(charSeparator, 2);

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["data"] = split[1];
            return result;
        }

        /// <summary>
        /// Information on the sold quantities and accumulated sales for the programmed fuels for
        ///this Z-report.Only for type 3 and 31 printers.
        /// </summary>
        /// <param name="closure1">Fiscal memory record number</param>
        /// <param name="option">'12' - Information on the sold quantities and accumulated sales for the programmed fuels for this Z-report.</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - Command is declined.
        /// </description>
        /// </item>
        /// <item>
        /// <term>data</term>
        /// <description>Data in format: Programmed fuel FTT1,Vol1(fuel sales volume),S1(fuel sales amount) ... FFTn,Voln,Sn</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_SoldQuant_AccumulatedSales(string closure1, string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(closure1);
            inputString.Append(",");
            inputString.Append(option);

            string r = CustomCommand(114, inputString.ToString());
            CheckResult();
            char[] charSeparator = new char[] { ',' };
            string[] split = r.Split(charSeparator, 2);

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["data"] = split[1];
            return result;
        }

        /// <summary>
        /// Information on the programmed pumps for this Z-report. Only for type 31 printers. 
        /// </summary>
        /// <param name="closure1">Fiscal memory record number</param>
        /// <param name="option">'13' - Information on the programmed pumps for this Z-report. Only for type 31 printers.</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - Command is declined.
        /// </description>
        /// </item>
        /// <item>
        /// <term>data</term>
        /// <description>Data in format: Disp1(Programmed pump number), ... ,Dispn</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_ProgrammedPumps(string closure1, string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(closure1);
            inputString.Append(",");
            inputString.Append(option);

            string r = CustomCommand(114, inputString.ToString());
            CheckResult();
            char[] charSeparator = new char[] { ',' };
            string[] split = r.Split(charSeparator, 2);

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["data"] = split[1];
            return result;
        }

        /// <summary>
        /// Information on the programmed pumps for this Z-report -0 - Tank volume at current temperature.
        /// Vol15Ci Tank volume at 15 degrees.. Only for type 31 printers
        /// </summary>
        /// <param name="closure1">Fiscal memory record number</param>
        /// <param name="option">'14' - Information on the programmed pumps for this Z-report -0 - Tank volume at current temperature.
        /// Vol15Ci Tank volume at 15 degrees</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - Command is declined.
        /// </description>
        /// </item>
        /// <item>
        /// <term>data</term>
        /// <description>Data in format: Term1(Programmed tank number),VolAct1(Tank volume at current temperature),Vol15C1(Tank volume at 15 degrees),FL1(Tank fuel level),Tmpr1(Tank fuel temperature),...</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_ProgrammedPumps2(string closure1, string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(closure1);
            inputString.Append(",");
            inputString.Append(option);

            string r = CustomCommand(114, inputString.ToString());
            CheckResult();
            char[] charSeparator = new char[] { ',' };
            string[] split = r.Split(charSeparator, 2);

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["data"] = split[1];
            return result;
        }

        /// <summary>
        /// Information on a programmed pump for this Z-report. ONLY for type 31 printers - Nozzle number, Nozzle counter value
        /// </summary>
        /// <param name="closure1">Fiscal memory record number</param>
        /// <param name="option">'15' - Information on a programmed pump for this Z-report.</param>
        /// <param name="pumpNum">Pump number.</param>
        /// <returns></returns>
        public Dictionary<string, string> info_Get_ProgrammedPumps3(string closure1, string option, string pumpNum)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(closure1);
            inputString.Append(",");
            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(pumpNum);

            string r = CustomCommand(114, inputString.ToString());
            CheckResult();
            char[] charSeparator = new char[] { ',' };
            string[] split = r.Split(charSeparator, 2);

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["data"] = split[1];
            return result;
        }

        // 115 Get logo line
        /// <summary>
        /// Get logo data from device by line number.
        /// </summary>
        /// <param name="rowNum">The line being programmed. Number between 0 and 95.</param>
        /// <returns>
        /// Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>fRow</term>
        /// <description>Data in a row</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_LogoLine(string rowNum)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("R");
            inputString.Append(rowNum);

            string r = CustomCommand(115, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["fRow"] = split[0];
            return result;
        }

        public string ReadDeviceLogo()
        {
            string logoData = "";
            try
            {
                for (int i = 0; i < 96; i++)
                {
                    var answer = info_Get_LogoLine(i.ToString());
                    logoData += answer["fRow"];
                }
                //Image logoImg = LogoToBitmap(logoData);
            }
            catch (Exception)
            {

                throw;
            }
            return logoData;
        }

        private Bitmap LogoToBitmap(string logoData)
        {
            Bitmap bm = new Bitmap(1, 1);
            int width = 0;
            if (deviceModel == "FP-700" || deviceModel == "FP-2000" || deviceModel == "FP-800" || deviceModel == "FP-650")
            {
                bm = new Bitmap(576, 96);
                width = 576;
            }
            else
            {
                bm = new Bitmap(384, 96);
                width = 384;
            }
            try
            {
                byte[] decoded = stringToHex(logoData);
                for (int y = 0; y < 96; y++)
                {
                    for (int x = 0; x < width / 8; x++)
                    {
                        int index = x * 8;
                        bool[] bwPixel = byteToBoolArr(decoded[y * width / 8 + x]);
                        bm.SetPixel(index, y, bwPixel[0] ? Color.Black : Color.White);
                        bm.SetPixel(index + 1, y, bwPixel[1] ? Color.Black : Color.White);
                        bm.SetPixel(index + 2, y, bwPixel[2] ? Color.Black : Color.White);
                        bm.SetPixel(index + 3, y, bwPixel[3] ? Color.Black : Color.White);
                        bm.SetPixel(index + 4, y, bwPixel[4] ? Color.Black : Color.White);
                        bm.SetPixel(index + 5, y, bwPixel[5] ? Color.Black : Color.White);
                        bm.SetPixel(index + 6, y, bwPixel[6] ? Color.Black : Color.White);
                        bm.SetPixel(index + 7, y, bwPixel[7] ? Color.Black : Color.White);

                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return bm;
        }

        public static byte[] stringToHex(string str)
        {
            str = str.Trim().ToLower();
            List<byte> r = new List<byte>();

            int count = 0;
            byte b = 0;
            for (int i = 0; i < str.Length; i++)
            {
                b <<= 4;
                char c = str[i];
                if (c < '0' || (c > '9' && c < 'a') || c > 'f')
                {
                    b = 0;
                    count = 0;
                    continue;
                }
                if (c >= '0' && c <= '9')
                    b |= (byte)(c - '0');
                else
                    b |= (byte)(c - 'a' + 10);
                count++;
                if (count == 2)
                {
                    r.Add(b);
                    b = 0;
                    count = 0;
                }
            }
            return r.ToArray();
        }

        public static bool[] byteToBoolArr(byte b)
        {
            bool[] boolArr = new bool[8];
            for (int i = 0; i < 8; i++) boolArr[i] = (b & (byte)(128 / Math.Pow(2, i))) != 0;
            return boolArr;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Print validation
        /// </summary>
        /// <param name="subcommand01">'C' - Check the validity of the EJ or a part of it</param>
        /// <param name="subcommand02">'P' - Prints report on the validity of all SHA-1 checksums for the Z-reports found in the\n
        /// EJ.It compares the EJ and fiscal memory SHA-1 checksums.For each mismatch, prints one line with the Z-report number, date and time</param>
        public void klen_Print_Validation(string subcommand01, string subcommand02)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(subcommand01);
            inputString.Append(",");
            inputString.Append(subcommand02);

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Print with SHA1 - prints information on each found SHA-1, valid or not
        /// </summary>
        /// <param name="subcommand01">'C' - Check the validity of the EJ or a part of it</param>
        /// <param name="subcommand02">'P' - Prints report on the validity of all SHA-1 checksums for the Z-reports found in the\n
        /// EJ.It compares the EJ and fiscal memory SHA-1 checksums.For each mismatch, prints one line with the Z-report number, date and time</param>
        public void klen_Print_SHA1(string subcommand01, string subcommand02)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(subcommand01);
            inputString.Append(",");
            inputString.Append(subcommand02);
            inputString.Append("#");

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Get EJ information for Z report number
        /// </summary>
        /// <param name="subcommand01">'C' - Verify EJ data</param>
        /// <param name="subcommand02">'R' - Returns EJ information for the Z-report number</param>
        /// <param name="zReportNumber">Z report number</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - No EJ data are found for this Z-report.
        /// </description>
        /// </item>
        /// <item>
        /// <term>fDocNumber</term>
        /// <description>Document number of the Z-report.</description>
        /// </item>
        /// <item>
        /// <term>dateTime</term>
        /// <description>Date and time of the report in the “DD-MM-YYYY hh:mm:ss” format.</description>
        /// </item>
        /// <item>
        /// <term>zReportSHA1</term>
        /// <description>40 characters: hexadecimal SHA-1 of the Z-report.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_Get_KLENInfo_ZReport(string subcommand01, string subcommand02, string zReportNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(subcommand01);
            inputString.Append(",");
            inputString.Append(subcommand02);
            inputString.Append(zReportNumber);

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["fDocNumber"] = split[1];
            if (split.Length >= 3)
                result["dateTime"] = split[2];
            if (split.Length >= 4)
                result["zReportSHA1"] = split[3];
            return result;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Reads the actual text of the documents stored in the EJ, calculates the SHA-1 and compares it to the SHA-1\n
        /// from the Z-report.The command can take a long time to complete, if there are many documents in the daily report.
        /// </summary>
        /// <param name="subcommand01">'C' - Verify EJ data</param>
        /// <param name="subcommand02">'Z' - Reads the actual text of the documents stored in the EJ</param>
        /// <param name="zReportNumber">Z report number</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - No EJ data are found for this Z-report.
        /// </description>
        /// </item>
        /// <item>
        /// <term>documentsCount</term>
        /// <description>Documents.</description>
        /// </item>
        /// <item>
        /// <term>bytesCount</term>
        /// <description>Data bytes.</description>
        /// </item>
        /// <item>
        /// <term>sha1KLEN</term>
        /// <description>40 characters: the hexadecimal checksum.</description>
        /// </item>
        /// <item>
        /// <term>sha1Calculated</term>
        /// <description>40 characters each: the EJ checksum and the calculated checksum, respectively.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_Get_ZReportInfo(string subcommand01, string subcommand02, string zReportNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(subcommand01);
            inputString.Append(",");
            inputString.Append(subcommand02);
            inputString.Append(zReportNumber);

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["documentsCount"] = split[1];
            if (split.Length >= 3)
                result["bytesCount"] = split[2];
            if (split.Length >= 4)
                result["sha1KLEN"] = split[3];
            if (split.Length >= 5)
                result["sha1Calculated"] = split[4];
            return result;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Electronic journal information
        /// </summary>
        /// <param name="subcommand01">'I' - EJ information</param>
        /// <param name="subcommand02">'X'</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - No EJ data are found.
        /// </description>
        /// </item>
        /// <item>
        /// <term>sizeTotal</term>
        /// <description>Total journal size in bytes</description>
        /// </item>
        /// <item>
        /// <term>sizeUsed</term>
        /// <description>Used journal size in bytes.</description>
        /// </item>
        /// <item>
        /// <term>firstZReportNumber</term>
        /// <description>First Z-report number in the EJ.</description>
        /// </item>
        /// <item>
        /// <term>lastZReportNumber</term>
        /// <description>Last Z-report number in the EJ.</description>
        /// </item>
        /// <item>
        /// <term>firstDocumentNumber</term>
        /// <description>First document number in the EJ.</description>
        /// </item>
        /// <item>
        /// <term>lastDocumentNumber</term>
        /// <description>Last document number in the EJ.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_Get_Info(string subcommand01, string subcommand02)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(subcommand01);
            inputString.Append(",");
            inputString.Append(subcommand02);

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["sizeTotal"] = split[1];
            if (split.Length >= 3)
                result["sizeUsed"] = split[2];
            if (split.Length >= 4)
                result["firstZReportNumber"] = split[3];
            if (split.Length >= 5)
                result["lastZReportNumber"] = split[4];
            if (split.Length >= 6)
                result["firstDocumentNumber"] = split[5];
            if (split.Length >= 7)
                result["lastDocumentNumber"] = split[6];
            return result;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Read documents
        /// </summary>
        /// <param name="subcommand01">'R' - Read EJ data.</param>
        /// <param name="documentType">'A' All document types; 'F' Fiscal(customer) receipts; 'V' Refund(storno) receipts; 'C' Cancelled(customer) receipts.\n
        /// 'N' Service receipts; 'I' Service deposit receipts; ‘O’ Service withdrawal receipts.</param>
        /// <param name="documentNumber">Document number</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - No EJ data are found.
        /// </description>
        /// </item>
        /// <item>
        /// <term>fRow</term>
        /// <description>Text in a row</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_Find_Document(string subcommand01, string documentType, string documentNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(subcommand01);
            inputString.Append(",");
            inputString.Append("#");
            inputString.Append(documentType);
            inputString.Append(",");
            inputString.Append(documentNumber);

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();

            string[] split = r.Split(new[] { ',' }, 2);

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["fAnswer"] = split[0];
            if (split.Length >= 2)
                result["fRow"] = split[1];
            return result;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Read EJ in range
        /// </summary>
        /// <param name="subcommand01">'R' - read EJ data</param>
        /// <param name="documentType">'A' All document types; 'F' Fiscal(customer) receipts; 'V' Refund(storno) receipts; 'C' Cancelled(customer) receipts.\n
        /// 'N' Service receipts; 'I' Service deposit receipts; ‘O’ Service withdrawal receipts.</param>
        /// <param name="startNumber">Start document number</param>
        /// <param name="endNumber">End document number</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - No EJ data are found.
        /// </description>
        /// </item>
        /// <item>
        /// <term>fRow</term>
        /// <description>Text in a row</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_Set_DocsRange(string subcommand01, string documentType, string startNumber, string endNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(subcommand01);
            inputString.Append(",");
            inputString.Append("#");
            inputString.Append(documentType);
            inputString.Append(",");
            inputString.Append(startNumber);
            inputString.Append(",");
            inputString.Append(endNumber);

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();

            string[] split = r.Split(new[] { ',' }, 2);

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["fAnswer"] = split[0];
            if (split.Length >= 2)
                result["fRow"] = split[1];
            return result;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Read EJ data (Z reports numbers)
        /// </summary>
        /// <param name="subcommand01">'R' - read EJ data</param>
        /// <param name="documentType">'A' All document types; 'F' Fiscal(customer) receipts; 'V' Refund(storno) receipts; 'C' Cancelled(customer) receipts.\n
        /// 'N' Service receipts; 'I' Service deposit receipts; ‘O’ Service withdrawal receipts.</param>
        /// <param name="zReportNumber">Z report number</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - No EJ data are found.
        /// </description>
        /// </item>
        /// <item>
        /// <term>fRow</term>
        /// <description>Text in a row</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_Set_DocsRange_InZReport_01(string subcommand01, string documentType, string zReportNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(subcommand01);
            inputString.Append(",");
            inputString.Append("#");
            inputString.Append(documentType);
            inputString.Append(",");
            inputString.Append("*");
            inputString.Append(zReportNumber);

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();

            string[] split = r.Split(new[] { ',' }, 2);

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["fAnswer"] = split[0];
            if (split.Length >= 2)
                result["fRow"] = split[1];
            return result;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Read EJ data in range (Z reports)
        /// </summary>
        /// <param name="subcommand01">'R' - read EJ data</param>
        /// <param name="documentType">'A' All document types; 'F' Fiscal(customer) receipts; 'V' Refund(storno) receipts; 'C' Cancelled(customer) receipts.\n
        /// 'N' Service receipts; 'I' Service deposit receipts; ‘O’ Service withdrawal receipts.</param>
        /// <param name="zReportNumber">Z report number</param>
        /// <param name="documentNumber">Document number</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - No EJ data are found.
        /// </description>
        /// </item>
        /// <item>
        /// <term>fRow</term>
        /// <description>Text in a row</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_Set_DocsRange_InZReport_02(string subcommand01, string documentType, string zReportNumber, string documentNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(subcommand01);
            inputString.Append(",");
            inputString.Append("#");
            inputString.Append(documentType);
            inputString.Append(",");
            inputString.Append("*");
            inputString.Append(zReportNumber);
            inputString.Append(",");
            inputString.Append(documentNumber);

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();

            string[] split = r.Split(new[] { ',' }, 2);

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["fAnswer"] = split[0];
            if (split.Length >= 2)
                result["fRow"] = split[1];
            return result;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Read EJ data in range with document numbers and Z report
        /// </summary>
        /// <param name="subcommand01">'R' - read EJ data</param>
        /// <param name="documentType">'A' All document types; 'F' Fiscal(customer) receipts; 'V' Refund(storno) receipts; 'C' Cancelled(customer) receipts.\n
        /// 'N' Service receipts; 'I' Service deposit receipts; ‘O’ Service withdrawal receipts.</param>
        /// <param name="zReportNumber">Z report number</param>
        /// <param name="startNumber">Start document number</param>
        /// <param name="endNumber">End document number</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - No EJ data are found.
        /// </description>
        /// </item>
        /// <item>
        /// <term>fRow</term>
        /// <description>Text in a row</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_Set_DocsRange_InZReport(string subcommand01, string documentType, string zReportNumber, string startNumber, string endNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(subcommand01);
            inputString.Append(",");
            inputString.Append("#");
            inputString.Append(documentType);
            inputString.Append(",");
            inputString.Append("*");
            inputString.Append(zReportNumber);
            inputString.Append(",");
            inputString.Append(startNumber);
            inputString.Append(",");
            inputString.Append(endNumber);

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();

            string[] split = r.Split(new[] { ',' }, 2);

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["fAnswer"] = split[0];
            if (split.Length >= 2)
                result["fRow"] = split[1];
            return result;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Read EJ data in range by date and time
        /// </summary>
        /// <param name="subcommand01">'R' - read EJ data</param>
        /// <param name="documentType">'A' All document types; 'F' Fiscal(customer) receipts; 'V' Refund(storno) receipts; 'C' Cancelled(customer) receipts.\n
        /// 'N' Service receipts; 'I' Service deposit receipts; ‘O’ Service withdrawal receipts.</param>
        /// <param name="fromDateTime">Report start date and time in the DDMMYY[hhmmss] format. If the time is omitted, “000000” is assumed, i.e., 00:00:00.</param>
        /// <param name="toDateTime">Report end date and time in the DDMMYY[hhmmss] format. If the time is omitted, “235959” is assumed, i.e., 23:59:59.</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - No EJ data are found.
        /// </description>
        /// </item>
        /// <item>
        /// <term>fRow</term>
        /// <description>Text in a row</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_Set_DocsRange_ByDateTime(string subcommand01, string documentType, string fromDateTime, string toDateTime)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(subcommand01);
            inputString.Append(",");
            inputString.Append(documentType);
            inputString.Append(",");
            inputString.Append(fromDateTime);
            inputString.Append(",");
            inputString.Append(toDateTime);

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();

            string[] split = r.Split(new[] { ',' }, 2);

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["fAnswer"] = split[0];
            if (split.Length >= 2)
                result["fRow"] = split[1];
            return result;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Get next text row
        /// </summary>
        /// <param name="subcommand01">'N' - for next row</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - No EJ data are found.
        /// </description>
        /// </item>
        /// <item>
        /// <term>fRow</term>
        /// <description>Text in a row</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_Get_NextTextRow(string subcommand01)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(subcommand01);

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();

            string[] split = r.Split(new[] { ',' }, 2);

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["fAnswer"] = split[0];
            if (split.Length >= 2)
                result["fRow"] = split[1];
            return result;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Print EJ data
        /// </summary>
        /// <param name="subcommand01">'P' - print data</param>
        /// <param name="fontSize">One byte with the following allowed values: '>' Print with a normal font size.
        /// '<' Print with a Ѕ font size.</param>
        /// <param name="documentType">'A' All document types; 'F' Fiscal(customer) receipts; 'V' Refund(storno) receipts; 'C' Cancelled(customer) receipts.\n
        /// 'N' Service receipts; 'I' Service deposit receipts; ‘O’ Service withdrawal receipts.</param>
        /// <param name="documentNumber">Document number</param>
        /// <returns>
        /// Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - No EJ data are found.
        /// </description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_Print_Document(string subcommand01, string fontSize, string documentType, string documentNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(subcommand01);
            inputString.Append(",");
            inputString.Append(fontSize);
            inputString.Append("#");
            inputString.Append(documentType);
            inputString.Append(",");
            inputString.Append(documentNumber);

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["fAnswer"] = split[0];
            return result;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Print EJ data
        /// </summary>
        /// <param name="subcommand01">'P' - print data</param>
        /// <param name="fontSize">One byte with the following allowed values: '>' Print with a normal font size.
        /// '<' Print with a Ѕ font size.</param>
        /// <param name="documentType">'A' All document types; 'F' Fiscal(customer) receipts; 'V' Refund(storno) receipts; 'C' Cancelled(customer) receipts.\n
        /// 'N' Service receipts; 'I' Service deposit receipts; ‘O’ Service withdrawal receipts.</param>
        /// <param name="startNumber">Start document number</param>
        /// <param name="endNumber">End document number</param>
        /// <returns>
        /// Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - No EJ data are found.
        /// </description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_Print_DocsInRange(string subcommand01, string fontSize, string documentType, string startNumber, string endNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(subcommand01);
            inputString.Append(",");
            inputString.Append(fontSize);
            inputString.Append("#");
            inputString.Append(documentType);
            inputString.Append(",");
            inputString.Append(startNumber);
            inputString.Append(",");
            inputString.Append(endNumber);

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["fAnswer"] = split[0];
            return result;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Print EJ data
        /// </summary>
        /// <param name="subcommand01">'P' - print data</param>
        /// <param name="fontSize">One byte with the following allowed values: '>' Print with a normal font size.
        /// '<' Print with a Ѕ font size.</param>
        /// <param name="documentType">'A' All document types; 'F' Fiscal(customer) receipts; 'V' Refund(storno) receipts; 'C' Cancelled(customer) receipts.\n
        /// 'N' Service receipts; 'I' Service deposit receipts; ‘O’ Service withdrawal receipts.</param>
        /// <param name="zReportNumber">Z report number</param>
        /// <param name="startNumber">Start document number</param>
        /// <param name="endNumber">End document number</param>
        /// <returns>
        /// Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - No EJ data are found.
        /// </description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_Print_DocsInRange_InZReport(string subcommand01, string fontSize, string documentType, string zReportNumber, string startNumber, string endNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(subcommand01);
            inputString.Append(",");
            inputString.Append(fontSize);
            inputString.Append("#");
            inputString.Append(documentType);
            inputString.Append(",");
            inputString.Append("*");
            inputString.Append(zReportNumber);
            inputString.Append(",");
            inputString.Append(startNumber);
            inputString.Append(",");
            inputString.Append(endNumber);

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["fAnswer"] = split[0];
            return result;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Print documents by date and time
        /// </summary>
        /// <param name="subcommand01">'P' - print data</param>
        /// <param name="fontSize">One byte with the following allowed values: '>' Print with a normal font size.
        /// '<' Print with a Ѕ font size.</param>
        /// <param name="documentType">'A' All document types; 'F' Fiscal(customer) receipts; 'V' Refund(storno) receipts; 'C' Cancelled(customer) receipts.\n
        /// 'N' Service receipts; 'I' Service deposit receipts; ‘O’ Service withdrawal receipts.</param>
        /// <param name="fromDateTime">Report start date and time in the DDMMYY[hhmmss] format. If the time is omitted, “000000” is assumed, i.e., 00:00:00.</param>
        /// <param name="toDateTime">Report end date and time in the DDMMYY[hhmmss] format. If the time is omitted, “235959” is assumed, i.e., 23:59:59</param>
        /// <returns>
        /// Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - No EJ data are found.
        /// </description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_Print_DocsFromDateToDate(string subcommand01, string fontSize, string documentType, string fromDateTime, string toDateTime)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(subcommand01);
            inputString.Append(",");
            inputString.Append(fontSize);
            inputString.Append(",");
            inputString.Append(documentType);
            inputString.Append(",");
            inputString.Append(fromDateTime);
            inputString.Append(",");
            inputString.Append(toDateTime);

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["fAnswer"] = split[0];
            return result;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Read structured information
        /// </summary>
        /// <param name="option">'W' - return structured information</param>
        /// <param name="startNumber">Number of the first document for the report.</param>
        /// <returns>
        /// Depends on the type. Check protocol for more!</returns>
        public Dictionary<string, string> klen_SInfo_W_ByNumber(string option, string startNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(startNumber);

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["datafield01"] = split[0];
            if (split.Length >= 2)
                result["datafield02"] = split[1];
            if (split.Length >= 3)
                result["datafield03"] = split[2];
            if (split.Length >= 4)
                result["datafield04"] = split[3];
            if (split.Length >= 5)
                result["datafield05"] = split[4];
            if (split.Length >= 6)
                result["datafield06"] = split[5];
            if (split.Length >= 7)
                result["datafield07"] = split[6];
            if (split.Length >= 8)
                result["datafield08"] = split[7];
            if (split.Length >= 9)
                result["datafield09"] = split[8];
            if (split.Length >= 10)
                result["datafield10"] = split[9];
            if (split.Length >= 11)
                result["datafield11"] = split[10];
            if (split.Length >= 12)
                result["datafield12"] = split[11];
            if (split.Length >= 13)
                result["datafield13"] = split[12];
            if (split.Length >= 14)
                result["datafield14"] = split[13];
            if (split.Length >= 15)
                result["datafield15"] = split[14];
            if (split.Length >= 16)
                result["datafield16"] = split[15];
            if (split.Length >= 17)
                result["datafield17"] = split[16];
            return result;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Read structured information by range
        /// </summary>
        /// <param name="option">'W' - return structured information</param>
        /// <param name="startNumber">Number of the first document for the report.</param>
        /// <param name="endNumber">Number of the last document for the report.</param>
        /// <returns>
        /// Depends on the type. Check protocol for more!</returns>
        public Dictionary<string, string> klen_SInfo_W_ByNumbersRange(string option, string startNumber, string endNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(startNumber);
            inputString.Append(",");
            inputString.Append(endNumber);

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["datafield01"] = split[0];
            if (split.Length >= 2)
                result["datafield02"] = split[1];
            if (split.Length >= 3)
                result["datafield03"] = split[2];
            if (split.Length >= 4)
                result["datafield04"] = split[3];
            if (split.Length >= 5)
                result["datafield05"] = split[4];
            if (split.Length >= 6)
                result["datafield06"] = split[5];
            if (split.Length >= 7)
                result["datafield07"] = split[6];
            if (split.Length >= 8)
                result["datafield08"] = split[7];
            if (split.Length >= 9)
                result["datafield09"] = split[8];
            if (split.Length >= 10)
                result["datafield10"] = split[9];
            if (split.Length >= 11)
                result["datafield11"] = split[10];
            if (split.Length >= 12)
                result["datafield12"] = split[11];
            if (split.Length >= 13)
                result["datafield13"] = split[12];
            if (split.Length >= 14)
                result["datafield14"] = split[13];
            if (split.Length >= 15)
                result["datafield15"] = split[14];
            if (split.Length >= 16)
                result["datafield16"] = split[15];
            if (split.Length >= 17)
                result["datafield17"] = split[16];
            return result;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Read structured information by date and time
        /// </summary>
        /// <param name="option">'W' - return structured information</param>
        /// <param name="optionD">'D' - Receipt start and end date and time</param>
        /// <param name="fromDateTime">Start date and time in the DD-MM-YYYY hh:mm:ss format</param>
        /// <param name="toDateTime">End date and time in the DD-MM-YYYY hh:mm:ss format</param>
        /// <returns>
        /// Depends on the type. Check protocol for more!</returns>
        public Dictionary<string, string> klen_SInfo_W_ByDateRange(string option, string optionD, string fromDateTime, string toDateTime)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(optionD);
            inputString.Append(fromDateTime);
            inputString.Append(",");
            inputString.Append(toDateTime);

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["datafield01"] = split[0];
            if (split.Length >= 2)
                result["datafield02"] = split[1];
            if (split.Length >= 3)
                result["datafield03"] = split[2];
            if (split.Length >= 4)
                result["datafield04"] = split[3];
            if (split.Length >= 5)
                result["datafield05"] = split[4];
            if (split.Length >= 6)
                result["datafield06"] = split[5];
            if (split.Length >= 7)
                result["datafield07"] = split[6];
            if (split.Length >= 8)
                result["datafield08"] = split[7];
            if (split.Length >= 9)
                result["datafield09"] = split[8];
            if (split.Length >= 10)
                result["datafield10"] = split[9];
            if (split.Length >= 11)
                result["datafield11"] = split[10];
            if (split.Length >= 12)
                result["datafield12"] = split[11];
            if (split.Length >= 13)
                result["datafield13"] = split[12];
            if (split.Length >= 14)
                result["datafield14"] = split[13];
            if (split.Length >= 15)
                result["datafield15"] = split[14];
            if (split.Length >= 16)
                result["datafield16"] = split[15];
            if (split.Length >= 17)
                result["datafield17"] = split[16];
            return result;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Get next text line structured information
        /// </summary>
        /// <param name="option">'w' - get structured info</param>
        /// <returns>
        /// Depends on the type. Check protocol for more!</returns>
        public Dictionary<string, string> klen_SInfo_W_GetNext(string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["datafield01"] = split[0];
            if (split.Length >= 2)
                result["datafield02"] = split[1];
            if (split.Length >= 3)
                result["datafield03"] = split[2];
            if (split.Length >= 4)
                result["datafield04"] = split[3];
            if (split.Length >= 5)
                result["datafield05"] = split[4];
            if (split.Length >= 6)
                result["datafield06"] = split[5];
            if (split.Length >= 7)
                result["datafield07"] = split[6];
            if (split.Length >= 8)
                result["datafield08"] = split[7];
            if (split.Length >= 9)
                result["datafield09"] = split[8];
            if (split.Length >= 10)
                result["datafield10"] = split[9];
            if (split.Length >= 11)
                result["datafield11"] = split[10];
            if (split.Length >= 12)
                result["datafield12"] = split[11];
            if (split.Length >= 13)
                result["datafield13"] = split[12];
            if (split.Length >= 14)
                result["datafield14"] = split[13];
            if (split.Length >= 15)
                result["datafield15"] = split[14];
            if (split.Length >= 16)
                result["datafield16"] = split[15];
            if (split.Length >= 17)
                result["datafield17"] = split[16];
            return result;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Return structured information
        /// </summary>
        /// <param name="option">'Y' - return structured information</param>
        /// <param name="startNumber">Start document number</param>
        /// <returns>
        /// Depends on the type. Check protocol for more!</returns>
        public Dictionary<string, string> klen_SInfo_Y_ByNumber(string option, string startNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(startNumber);

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["datafield01"] = split[0];
            if (split.Length >= 2)
                result["datafield02"] = split[1];
            if (split.Length >= 3)
                result["datafield03"] = split[2];
            if (split.Length >= 4)
                result["datafield04"] = split[3];
            if (split.Length >= 5)
                result["datafield05"] = split[4];
            if (split.Length >= 6)
                result["datafield06"] = split[5];
            if (split.Length >= 7)
                result["datafield07"] = split[6];
            if (split.Length >= 8)
                result["datafield08"] = split[7];
            if (split.Length >= 9)
                result["datafield09"] = split[8];
            if (split.Length >= 10)
                result["datafield10"] = split[9];
            if (split.Length >= 11)
                result["datafield11"] = split[10];
            if (split.Length >= 12)
                result["datafield12"] = split[11];
            if (split.Length >= 13)
                result["datafield13"] = split[12];
            if (split.Length >= 14)
                result["datafield14"] = split[13];
            if (split.Length >= 15)
                result["datafield15"] = split[14];
            if (split.Length >= 16)
                result["datafield16"] = split[15];
            if (split.Length >= 17)
                result["datafield17"] = split[16];
            return result;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Get structured information
        /// </summary>
        /// <param name="option">'Y' - Get structured info</param>
        /// <param name="startNumber">Start document number</param>
        /// <param name="endNumber">End document number</param>
        /// <returns>
        /// Depends on the type. Check protocol for more!</returns>
        public Dictionary<string, string> klen_SInfo_Y_ByNumbersRange(string option, string startNumber, string endNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(startNumber);
            inputString.Append(",");
            inputString.Append(endNumber);

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["datafield01"] = split[0];
            if (split.Length >= 2)
                result["datafield02"] = split[1];
            if (split.Length >= 3)
                result["datafield03"] = split[2];
            if (split.Length >= 4)
                result["datafield04"] = split[3];
            if (split.Length >= 5)
                result["datafield05"] = split[4];
            if (split.Length >= 6)
                result["datafield06"] = split[5];
            if (split.Length >= 7)
                result["datafield07"] = split[6];
            if (split.Length >= 8)
                result["datafield08"] = split[7];
            if (split.Length >= 9)
                result["datafield09"] = split[8];
            if (split.Length >= 10)
                result["datafield10"] = split[9];
            if (split.Length >= 11)
                result["datafield11"] = split[10];
            if (split.Length >= 12)
                result["datafield12"] = split[11];
            if (split.Length >= 13)
                result["datafield13"] = split[12];
            if (split.Length >= 14)
                result["datafield14"] = split[13];
            if (split.Length >= 15)
                result["datafield15"] = split[14];
            if (split.Length >= 16)
                result["datafield16"] = split[15];
            if (split.Length >= 17)
                result["datafield17"] = split[16];
            return result;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Get structured information
        /// </summary>
        /// <param name="option">'Y' - Get structured info</param>
        /// <param name="optionD">'D' - date and time</param>
        /// <param name="fromDateTime">Start date and time</param>
        /// <param name="toDateTime">End date and time</param>
        /// <returns>
        /// Depends on the type. Check protocol for more!</returns>
        public Dictionary<string, string> klen_SInfo_Y_ByDateRange(string option, string optionD, string fromDateTime, string toDateTime)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(optionD);
            inputString.Append(fromDateTime);
            inputString.Append(",");
            inputString.Append(toDateTime);

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["datafield01"] = split[0];
            if (split.Length >= 2)
                result["datafield02"] = split[1];
            if (split.Length >= 3)
                result["datafield03"] = split[2];
            if (split.Length >= 4)
                result["datafield04"] = split[3];
            if (split.Length >= 5)
                result["datafield05"] = split[4];
            if (split.Length >= 6)
                result["datafield06"] = split[5];
            if (split.Length >= 7)
                result["datafield07"] = split[6];
            if (split.Length >= 8)
                result["datafield08"] = split[7];
            if (split.Length >= 9)
                result["datafield09"] = split[8];
            if (split.Length >= 10)
                result["datafield10"] = split[9];
            if (split.Length >= 11)
                result["datafield11"] = split[10];
            if (split.Length >= 12)
                result["datafield12"] = split[11];
            if (split.Length >= 13)
                result["datafield13"] = split[12];
            if (split.Length >= 14)
                result["datafield14"] = split[13];
            if (split.Length >= 15)
                result["datafield15"] = split[14];
            if (split.Length >= 16)
                result["datafield16"] = split[15];
            if (split.Length >= 17)
                result["datafield17"] = split[16];
            return result;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Get next line structured information
        /// </summary>
        /// <param name="option">'y' - Get next line structured information</param>
        /// <returns>
        /// Depends on the type. Check protocol for more!</returns>
        public Dictionary<string, string> klen_SInfo_Y_GetNext(string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["datafield01"] = split[0];
            if (split.Length >= 2)
                result["datafield02"] = split[1];
            if (split.Length >= 3)
                result["datafield03"] = split[2];
            if (split.Length >= 4)
                result["datafield04"] = split[3];
            if (split.Length >= 5)
                result["datafield05"] = split[4];
            if (split.Length >= 6)
                result["datafield06"] = split[5];
            if (split.Length >= 7)
                result["datafield07"] = split[6];
            if (split.Length >= 8)
                result["datafield08"] = split[7];
            if (split.Length >= 9)
                result["datafield09"] = split[8];
            if (split.Length >= 10)
                result["datafield10"] = split[9];
            if (split.Length >= 11)
                result["datafield11"] = split[10];
            if (split.Length >= 12)
                result["datafield12"] = split[11];
            if (split.Length >= 13)
                result["datafield13"] = split[12];
            if (split.Length >= 14)
                result["datafield14"] = split[13];
            if (split.Length >= 15)
                result["datafield15"] = split[14];
            if (split.Length >= 16)
                result["datafield16"] = split[15];
            if (split.Length >= 17)
                result["datafield17"] = split[16];
            return result;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Return structured information by number
        /// </summary>
        /// <param name="option">'V' - return structured information</param>
        /// <param name="startNumber">Number of the first document for the report</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed.</description>
        /// </item> 
        /// <item>
        /// <term>datafield01</term>
        /// <description>The data returned in the order described below is separated by a tab symbol enclosed in quotation marks: \n
        /// fiscal device ID; kind of receipt - ФБ(fiscal receipt), РФБ(Invoice), СФБ(Refund receipt) or РФБ(Credit notification); fiscal receipt), РФБ (fiscal receipt), РФБ (Invoice), СФБ (Refund receipt) or РФБ(Credit notification); Invoice), СФБ(fiscal receipt),\n
        /// РФБ(Invoice), СФБ(Refund receipt) or РФБ(Credit notification); Refund receipt) or РФБ(fiscal receipt), РФБ(Invoice), СФБ(Refund receipt) or РФБ(Credit notification);
        /// global number of receipt; Unique Sale ID; commodity/service - name; commodity/service – single price; commodity/service - quantity; commodity/service - price; total price for the receipt;\n
        /// Fiscal printer programming interface; Invoice number/Credit notification - if the entry is for Invoice or Credit notification; UIC of recipient – if the entry is for Invoice or Credit notification;\n
        /// global number of the refund receipt – if the entry is for Invoice or Credit notification;  number of the refunded invoice номер на сторнирана фактура – if the entry is for Invoice or Credit notification;
        /// reason for issue – in case if entry is for refund receipt or Credit notification</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_SInfo_V_ByNumber(string option, string startNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(startNumber);

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["datafield01"] = split[1];
            return result;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Return structured information by number
        /// </summary>
        /// <param name="option">'V' - return structured information</param>
        /// <param name="startNumber">Number of the first document for the report</param>
        /// <param name="endNumber">Number of the last document for the report</param>
        /// <returns>Dictionary with keys:
        /// The data returned in the order described below is separated by a tab symbol enclosed in quotation marks: \n
        /// fiscal device ID; kind of receipt - ФБ(fiscal receipt), РФБ(Invoice), СФБ(Refund receipt) or РФБ(Credit notification); fiscal receipt), РФБ (fiscal receipt), РФБ (Invoice), СФБ (Refund receipt) or РФБ(Credit notification); Invoice), СФБ(fiscal receipt),\n
        /// РФБ(Invoice), СФБ(Refund receipt) or РФБ(Credit notification); Refund receipt) or РФБ(fiscal receipt), РФБ(Invoice), СФБ(Refund receipt) or РФБ(Credit notification);
        /// global number of receipt; Unique Sale ID; commodity/service - name; commodity/service – single price; commodity/service - quantity; commodity/service - price; total price for the receipt;\n
        /// Fiscal printer programming interface; Invoice number/Credit notification - if the entry is for Invoice or Credit notification; UIC of recipient – if the entry is for Invoice or Credit notification;\n
        /// global number of the refund receipt – if the entry is for Invoice or Credit notification;  number of the refunded invoice номер на сторнирана фактура – if the entry is for Invoice or Credit notification;
        /// reason for issue – in case if entry is for refund receipt or Credit notification
        /// </returns>
        public Dictionary<string, string> klen_SInfo_V_ByNumbersRange(string option, string startNumber, string endNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(startNumber);
            inputString.Append(",");
            inputString.Append(endNumber);

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["datafield01"] = split[0];
            if (split.Length >= 2)
                result["datafield02"] = split[1];
            if (split.Length >= 3)
                result["datafield03"] = split[2];
            if (split.Length >= 4)
                result["datafield04"] = split[3];
            if (split.Length >= 5)
                result["datafield05"] = split[4];
            if (split.Length >= 6)
                result["datafield06"] = split[5];
            if (split.Length >= 7)
                result["datafield07"] = split[6];
            if (split.Length >= 8)
                result["datafield08"] = split[7];
            if (split.Length >= 9)
                result["datafield09"] = split[8];
            if (split.Length >= 10)
                result["datafield10"] = split[9];
            if (split.Length >= 11)
                result["datafield11"] = split[10];
            if (split.Length >= 12)
                result["datafield12"] = split[11];
            if (split.Length >= 13)
                result["datafield13"] = split[12];
            if (split.Length >= 14)
                result["datafield14"] = split[13];
            if (split.Length >= 15)
                result["datafield15"] = split[14];
            if (split.Length >= 16)
                result["datafield16"] = split[15];
            if (split.Length >= 17)
                result["datafield17"] = split[16];
            return result;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Get structured information by date and time
        /// </summary>
        /// <param name="option">'V' - for structured information</param>
        /// <param name="optionD">'D' - for dates</param>
        /// <param name="fromDateTime">DDMMYY [hhmmss] format.If you skip the hour, the start time is 00:00:00</param>
        /// <param name="toDateTime">DDMMYY [hhmmss] format.If you skip the hour, the end time is 23:59:59</param>
        /// <returns>Dictionary with keys:
        /// The data returned in the order described below is separated by a tab symbol enclosed in quotation marks: \n
        /// fiscal device ID; kind of receipt - ФБ(fiscal receipt), РФБ(Invoice), СФБ(Refund receipt) or РФБ(Credit notification); fiscal receipt), РФБ (fiscal receipt), РФБ (Invoice), СФБ (Refund receipt) or РФБ(Credit notification); Invoice), СФБ(fiscal receipt),\n
        /// РФБ(Invoice), СФБ(Refund receipt) or РФБ(Credit notification); Refund receipt) or РФБ(fiscal receipt), РФБ(Invoice), СФБ(Refund receipt) or РФБ(Credit notification);
        /// global number of receipt; Unique Sale ID; commodity/service - name; commodity/service – single price; commodity/service - quantity; commodity/service - price; total price for the receipt;\n
        /// Fiscal printer programming interface; Invoice number/Credit notification - if the entry is for Invoice or Credit notification; UIC of recipient – if the entry is for Invoice or Credit notification;\n
        /// global number of the refund receipt – if the entry is for Invoice or Credit notification;  number of the refunded invoice номер на сторнирана фактура – if the entry is for Invoice or Credit notification;
        /// reason for issue – in case if entry is for refund receipt or Credit notification
        /// </returns>
        public Dictionary<string, string> klen_SInfo_V_ByDateRange(string option, string optionD, string fromDateTime, string toDateTime)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(optionD);
            inputString.Append(fromDateTime);
            inputString.Append(",");
            inputString.Append(toDateTime);

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["datafield01"] = split[0];
            if (split.Length >= 2)
                result["datafield02"] = split[1];
            if (split.Length >= 3)
                result["datafield03"] = split[2];
            if (split.Length >= 4)
                result["datafield04"] = split[3];
            if (split.Length >= 5)
                result["datafield05"] = split[4];
            if (split.Length >= 6)
                result["datafield06"] = split[5];
            if (split.Length >= 7)
                result["datafield07"] = split[6];
            if (split.Length >= 8)
                result["datafield08"] = split[7];
            if (split.Length >= 9)
                result["datafield09"] = split[8];
            if (split.Length >= 10)
                result["datafield10"] = split[9];
            if (split.Length >= 11)
                result["datafield11"] = split[10];
            if (split.Length >= 12)
                result["datafield12"] = split[11];
            if (split.Length >= 13)
                result["datafield13"] = split[12];
            if (split.Length >= 14)
                result["datafield14"] = split[13];
            if (split.Length >= 15)
                result["datafield15"] = split[14];
            if (split.Length >= 16)
                result["datafield16"] = split[15];
            if (split.Length >= 17)
                result["datafield17"] = split[16];
            return result;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Get next line structured information
        /// </summary>
        /// <param name="option">'v' - for next line structured info</param>
        /// <returns>Dictionary with keys:
        /// The data returned in the order described below is separated by a tab symbol enclosed in quotation marks: \n
        /// fiscal device ID; kind of receipt - ФБ(fiscal receipt), РФБ(Invoice), СФБ(Refund receipt) or РФБ(Credit notification); fiscal receipt), РФБ (fiscal receipt), РФБ (Invoice), СФБ (Refund receipt) or РФБ(Credit notification); Invoice), СФБ(fiscal receipt),\n
        /// РФБ(Invoice), СФБ(Refund receipt) or РФБ(Credit notification); Refund receipt) or РФБ(fiscal receipt), РФБ(Invoice), СФБ(Refund receipt) or РФБ(Credit notification);
        /// global number of receipt; Unique Sale ID; commodity/service - name; commodity/service – single price; commodity/service - quantity; commodity/service - price; total price for the receipt;\n
        /// Fiscal printer programming interface; Invoice number/Credit notification - if the entry is for Invoice or Credit notification; UIC of recipient – if the entry is for Invoice or Credit notification;\n
        /// global number of the refund receipt – if the entry is for Invoice or Credit notification;  number of the refunded invoice номер на сторнирана фактура – if the entry is for Invoice or Credit notification;
        /// reason for issue – in case if entry is for refund receipt or Credit notification
        /// </returns>
        public Dictionary<string, string> klen_SInfo_V_GetNext(string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["datafield01"] = split[0];
            if (split.Length >= 2)
                result["datafield02"] = split[1];
            if (split.Length >= 3)
                result["datafield03"] = split[2];
            if (split.Length >= 4)
                result["datafield04"] = split[3];
            if (split.Length >= 5)
                result["datafield05"] = split[4];
            if (split.Length >= 6)
                result["datafield06"] = split[5];
            if (split.Length >= 7)
                result["datafield07"] = split[6];
            if (split.Length >= 8)
                result["datafield08"] = split[7];
            if (split.Length >= 9)
                result["datafield09"] = split[8];
            if (split.Length >= 10)
                result["datafield10"] = split[9];
            if (split.Length >= 11)
                result["datafield11"] = split[10];
            if (split.Length >= 12)
                result["datafield12"] = split[11];
            if (split.Length >= 13)
                result["datafield13"] = split[12];
            if (split.Length >= 14)
                result["datafield14"] = split[13];
            if (split.Length >= 15)
                result["datafield15"] = split[14];
            if (split.Length >= 16)
                result["datafield16"] = split[15];
            if (split.Length >= 17)
                result["datafield17"] = split[16];
            return result;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Get structured information by date and time plus date and time
        /// </summary>
        /// <param name="option">'V' - for structured information</param>
        /// <param name="exOption">'+' - the date and time of the document are added in the format: DD-MM-YYYY HH: MM: SS + pasted tab symbol</param>
        /// <param name="startNumber">Number of the first document for the report</param>
        /// <returns>Dictionary with keys:
        /// The data returned in the order described below is separated by a tab symbol enclosed in quotation marks: \n
        /// fiscal device ID; kind of receipt - ФБ(fiscal receipt), РФБ(Invoice), СФБ(Refund receipt) or РФБ(Credit notification); fiscal receipt), РФБ (fiscal receipt), РФБ (Invoice), СФБ (Refund receipt) or РФБ(Credit notification); Invoice), СФБ(fiscal receipt),\n
        /// РФБ(Invoice), СФБ(Refund receipt) or РФБ(Credit notification); Refund receipt) or РФБ(fiscal receipt), РФБ(Invoice), СФБ(Refund receipt) or РФБ(Credit notification);
        /// global number of receipt; Unique Sale ID; commodity/service - name; commodity/service – single price; commodity/service - quantity; commodity/service - price; total price for the receipt;\n
        /// Fiscal printer programming interface; Invoice number/Credit notification - if the entry is for Invoice or Credit notification; UIC of recipient – if the entry is for Invoice or Credit notification;\n
        /// global number of the refund receipt – if the entry is for Invoice or Credit notification;  number of the refunded invoice номер на сторнирана фактура – if the entry is for Invoice or Credit notification;
        /// reason for issue – in case if entry is for refund receipt or Credit notification
        /// </returns>
        public Dictionary<string, string> klen_SInfo_ExV_ByNumber(string option, string exOption, string startNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(exOption);
            inputString.Append(",");
            inputString.Append(startNumber);

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["datafield01"] = split[0];
            if (split.Length >= 2)
                result["datafield02"] = split[1];
            if (split.Length >= 3)
                result["datafield03"] = split[2];
            if (split.Length >= 4)
                result["datafield04"] = split[3];
            if (split.Length >= 5)
                result["datafield05"] = split[4];
            if (split.Length >= 6)
                result["datafield06"] = split[5];
            if (split.Length >= 7)
                result["datafield07"] = split[6];
            if (split.Length >= 8)
                result["datafield08"] = split[7];
            if (split.Length >= 9)
                result["datafield09"] = split[8];
            if (split.Length >= 10)
                result["datafield10"] = split[9];
            if (split.Length >= 11)
                result["datafield11"] = split[10];
            if (split.Length >= 12)
                result["datafield12"] = split[11];
            if (split.Length >= 13)
                result["datafield13"] = split[12];
            if (split.Length >= 14)
                result["datafield14"] = split[13];
            if (split.Length >= 15)
                result["datafield15"] = split[14];
            if (split.Length >= 16)
                result["datafield16"] = split[15];
            if (split.Length >= 17)
                result["datafield17"] = split[16];
            return result;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Return structured information by numbers range plus date and time
        /// </summary>
        /// <param name="option">'V' - return structured information</param>
        /// <param name="exOption">'+' - the date and time of the document are added in the format: DD-MM-YYYY HH: MM: SS + pasted tab symbol</param>
        /// <param name="startNumber">Number of the first document for the report</param>
        /// <param name="endNumber">Number of the last document for the report</param>
        /// <returns>Dictionary with keys:
        /// The data returned in the order described below is separated by a tab symbol enclosed in quotation marks: \n
        /// fiscal device ID; kind of receipt - ФБ(fiscal receipt), РФБ(Invoice), СФБ(Refund receipt) or РФБ(Credit notification); fiscal receipt), РФБ (fiscal receipt), РФБ (Invoice), СФБ (Refund receipt) or РФБ(Credit notification); Invoice), СФБ(fiscal receipt),\n
        /// РФБ(Invoice), СФБ(Refund receipt) or РФБ(Credit notification); Refund receipt) or РФБ(fiscal receipt), РФБ(Invoice), СФБ(Refund receipt) or РФБ(Credit notification);
        /// global number of receipt; Unique Sale ID; commodity/service - name; commodity/service – single price; commodity/service - quantity; commodity/service - price; total price for the receipt;\n
        /// Fiscal printer programming interface; Invoice number/Credit notification - if the entry is for Invoice or Credit notification; UIC of recipient – if the entry is for Invoice or Credit notification;\n
        /// global number of the refund receipt – if the entry is for Invoice or Credit notification;  number of the refunded invoice номер на сторнирана фактура – if the entry is for Invoice or Credit notification;
        /// reason for issue – in case if entry is for refund receipt or Credit notification
        /// </returns>
        public Dictionary<string, string> klen_SInfo_ExV_ByNumbersRange(string option, string exOption, string startNumber, string endNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(exOption);
            inputString.Append(",");
            inputString.Append(startNumber);
            inputString.Append(",");
            inputString.Append(endNumber);

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["datafield01"] = split[0];
            if (split.Length >= 2)
                result["datafield02"] = split[1];
            if (split.Length >= 3)
                result["datafield03"] = split[2];
            if (split.Length >= 4)
                result["datafield04"] = split[3];
            if (split.Length >= 5)
                result["datafield05"] = split[4];
            if (split.Length >= 6)
                result["datafield06"] = split[5];
            if (split.Length >= 7)
                result["datafield07"] = split[6];
            if (split.Length >= 8)
                result["datafield08"] = split[7];
            if (split.Length >= 9)
                result["datafield09"] = split[8];
            if (split.Length >= 10)
                result["datafield10"] = split[9];
            if (split.Length >= 11)
                result["datafield11"] = split[10];
            if (split.Length >= 12)
                result["datafield12"] = split[11];
            if (split.Length >= 13)
                result["datafield13"] = split[12];
            if (split.Length >= 14)
                result["datafield14"] = split[13];
            if (split.Length >= 15)
                result["datafield15"] = split[14];
            if (split.Length >= 16)
                result["datafield16"] = split[15];
            if (split.Length >= 17)
                result["datafield17"] = split[16];
            return result;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Get structured information by date and time range plus date and time
        /// </summary>
        /// <param name="option">'V' - for structured information</param>
        /// <param name="exOption">'+' - the date and time of the document are added in the format: DD-MM-YYYY HH: MM: SS + pasted tab symbol</param>
        /// <param name="optionD">'D' - for dates</param>
        /// <param name="fromDateTime">Format is: DDMMYY [hhmmss].If you skip the hour, the start time is 00:00:00</param>
        /// <param name="toDateTime">Format is: DDMMYY [hhmmss] format.If you skip the hour, the end time is 23:59:59</param>
        /// <returns>Dictionary with keys:
        /// The data returned in the order described below is separated by a tab symbol enclosed in quotation marks: \n
        /// fiscal device ID; kind of receipt - ФБ(fiscal receipt), РФБ(Invoice), СФБ(Refund receipt) or РФБ(Credit notification); fiscal receipt), РФБ (fiscal receipt), РФБ (Invoice), СФБ (Refund receipt) or РФБ(Credit notification); Invoice), СФБ(fiscal receipt),\n
        /// РФБ(Invoice), СФБ(Refund receipt) or РФБ(Credit notification); Refund receipt) or РФБ(fiscal receipt), РФБ(Invoice), СФБ(Refund receipt) or РФБ(Credit notification);
        /// global number of receipt; Unique Sale ID; commodity/service - name; commodity/service – single price; commodity/service - quantity; commodity/service - price; total price for the receipt;\n
        /// Fiscal printer programming interface; Invoice number/Credit notification - if the entry is for Invoice or Credit notification; UIC of recipient – if the entry is for Invoice or Credit notification;\n
        /// global number of the refund receipt – if the entry is for Invoice or Credit notification;  number of the refunded invoice номер на сторнирана фактура – if the entry is for Invoice or Credit notification;
        /// reason for issue – in case if entry is for refund receipt or Credit notification
        /// </returns>
        public Dictionary<string, string> klen_SInfo_ExV_ByDateRange(string option, string exOption, string optionD, string fromDateTime, string toDateTime)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(exOption);
            inputString.Append(",");
            inputString.Append(optionD);
            inputString.Append(fromDateTime);
            inputString.Append(",");
            inputString.Append(toDateTime);

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["datafield01"] = split[0];
            if (split.Length >= 2)
                result["datafield02"] = split[1];
            if (split.Length >= 3)
                result["datafield03"] = split[2];
            if (split.Length >= 4)
                result["datafield04"] = split[3];
            if (split.Length >= 5)
                result["datafield05"] = split[4];
            if (split.Length >= 6)
                result["datafield06"] = split[5];
            if (split.Length >= 7)
                result["datafield07"] = split[6];
            if (split.Length >= 8)
                result["datafield08"] = split[7];
            if (split.Length >= 9)
                result["datafield09"] = split[8];
            if (split.Length >= 10)
                result["datafield10"] = split[9];
            if (split.Length >= 11)
                result["datafield11"] = split[10];
            if (split.Length >= 12)
                result["datafield12"] = split[11];
            if (split.Length >= 13)
                result["datafield13"] = split[12];
            if (split.Length >= 14)
                result["datafield14"] = split[13];
            if (split.Length >= 15)
                result["datafield15"] = split[14];
            if (split.Length >= 16)
                result["datafield16"] = split[15];
            if (split.Length >= 17)
                result["datafield17"] = split[16];
            return result;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Get next line structured information
        /// </summary>
        /// <param name="option">'v' - for next line structured info</param>
        /// <returns>Dictionary with keys:
        /// The data returned in the order described below is separated by a tab symbol enclosed in quotation marks: \n
        /// fiscal device ID; kind of receipt - ФБ(fiscal receipt), РФБ(Invoice), СФБ(Refund receipt) or РФБ(Credit notification); fiscal receipt), РФБ (fiscal receipt), РФБ (Invoice), СФБ (Refund receipt) or РФБ(Credit notification); Invoice), СФБ(fiscal receipt),\n
        /// РФБ(Invoice), СФБ(Refund receipt) or РФБ(Credit notification); Refund receipt) or РФБ(fiscal receipt), РФБ(Invoice), СФБ(Refund receipt) or РФБ(Credit notification);
        /// global number of receipt; Unique Sale ID; commodity/service - name; commodity/service – single price; commodity/service - quantity; commodity/service - price; total price for the receipt;\n
        /// Fiscal printer programming interface; Invoice number/Credit notification - if the entry is for Invoice or Credit notification; UIC of recipient – if the entry is for Invoice or Credit notification;\n
        /// global number of the refund receipt – if the entry is for Invoice or Credit notification;  number of the refunded invoice номер на сторнирана фактура – if the entry is for Invoice or Credit notification;
        /// reason for issue – in case if entry is for refund receipt or Credit notification
        /// </returns>
        public Dictionary<string, string> klen_SInfo_ExV_GetNext(string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(119, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["datafield01"] = split[0];
            if (split.Length >= 2)
                result["datafield02"] = split[1];
            if (split.Length >= 3)
                result["datafield03"] = split[2];
            if (split.Length >= 4)
                result["datafield04"] = split[3];
            if (split.Length >= 5)
                result["datafield05"] = split[4];
            if (split.Length >= 6)
                result["datafield06"] = split[5];
            if (split.Length >= 7)
                result["datafield07"] = split[6];
            if (split.Length >= 8)
                result["datafield08"] = split[7];
            if (split.Length >= 9)
                result["datafield09"] = split[8];
            if (split.Length >= 10)
                result["datafield10"] = split[9];
            if (split.Length >= 11)
                result["datafield11"] = split[10];
            if (split.Length >= 12)
                result["datafield12"] = split[11];
            if (split.Length >= 13)
                result["datafield13"] = split[12];
            if (split.Length >= 14)
                result["datafield14"] = split[13];
            if (split.Length >= 15)
                result["datafield15"] = split[14];
            if (split.Length >= 16)
                result["datafield16"] = split[15];
            if (split.Length >= 17)
                result["datafield17"] = split[16];
            return result;
        }

        //148
        /// <summary>
        /// Check recently recorded RRN code in fiscal printer with RRN from last transaction in payment terminal
        /// </summary>
        /// <returns>Reurns dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description>P - passed or F - failed</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Pinpad_CheckLastRecordedRRN()
        {
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(148, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["fAnswer"] = split[0];
            return result;
        }

        //149
        /// <summary>
        /// Data for last transaction
        /// </summary>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>currentRRN</term>
        /// <description>Last RRN code, saved in FD.</description>
        /// </item>
        /// <item>
        /// <term>transactionNum</term>
        /// <description>Transaction number.</description>
        /// </item>
        /// <item>
        /// <term>transactionRRN</term>
        /// <description>RRN code of transaction.</description>
        /// </item>
        /// <item>
        /// <term>AC</term>
        /// <description>Transaction authorization code.</description>
        /// </item>
        /// <item>
        /// <term>price</term>
        /// <description>Amount of transaction.</description>
        /// </item>
        /// <item>
        /// <term>cardNo</term>
        /// <description>The last four digits of the card number.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Pinpad_DataForLastTransaction()
        {
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(149, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();
            if (split.Length >= 1)
                result["currentRRN"] = split[0];
            if (split.Length >= 2)
                result["transactionNum"] = split[1];
            if (split.Length >= 3)
                result["transactionRRN"] = split[2];
            if (split.Length >= 4)
                result["AC"] = split[3];
            if (split.Length >= 5)
                result["price"] = split[4];
            if (split.Length >= 6)
                result["cardNo"] = split[5];
            return result;
        }

        //151
        /// <summary>
        /// Prints the last successful transaction
        /// </summary>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description>'P' - command passed, 'F' - command failed.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Pinpad_PrintLastSuccesfulTransaction()
        {
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(151, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["fAnswer"] = split[0];
            return result;
        }

        // Command number(Dec): 152 - please check fiscal device documentation.
        /// <summary>
        /// Get date and time of payment terminal.The command is forbidden in an open fiscal receipt
        /// </summary>
        /// <returns>
        /// Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>dateTime</term>
        /// <description>Date and time in format: YYYY-MM-DD HH:MM:SS</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Pinpad_Get_DateTime()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(152, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["dateTime"] = split[0];
            return result;
        }

        // Command number(Dec): 153 - please check fiscal device documentation.
        /// <summary>
        /// Set date and time.The command is forbidden in an open fiscal receipt
        /// </summary>
        /// <param name="dateTime">In format YY-MM-DD HH:MM:SS</param>
        public void config_Pinpad_Set_DateTime(string dateTime)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(dateTime);

            string r = CustomCommand(153, inputString.ToString());
            CheckResult();
        }

        /// <summary>
        /// Test connection to a payment terminal.The command is forbidden in an open fiscal receipt
        /// </summary>
        /// <returns> Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description>'P' - command passed, 'F' - command failed.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Pinpad_TestConnection()
        {
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(154, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["fAnswer"] = split[0];
            return result;
        }

        // 155
        /// <summary>
        /// Number of entries in the payment terminal.The command is forbidden in an open fiscal receipt
        /// </summary>
        /// <returns> Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description>'P' - command passed + Number of entries, 'F' - command failed.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Pinpad_NumberOfEntries()
        {
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(155, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["fAnswer"] = split[0];
            return result;
        }

        // 156
        /// <summary>
        /// Payment terminal information.The command is forbidden in an open fiscal receipt
        /// </summary>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>model</term>
        /// <description>Payment terminal model or 'F' - if commandd failed.</description>
        /// </item> 
        /// <item>
        /// <term>serialNum</term>
        /// <description>Payment terminal serial number</description>
        /// </item>
        /// <item>
        /// <term>softVer</term>
        /// <description>Software version of payment terminal</description>
        /// </item>
        /// <item>
        /// <term>terminalID</term>
        /// <description>ID of payment terminal</description>
        /// </item>
        /// <item>
        /// <term>menuType</term>
        /// <description>Menu type of payment terminal</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Pinpad_Information()
        {
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(156, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["model"] = split[0];
            if (split.Length >= 2)
                result["serialNum"] = split[1];
            if (split.Length >= 3)
                result["softVer"] = split[2];
            if (split.Length >= 4)
                result["termialID"] = split[3];
            if (split.Length >= 5)
                result["menuType"] = split[4];
            return result;
        }

        //157
        /// <summary>
        /// Set batch number.The command is forbidden in an open fiscal receipt
        /// </summary>
        /// /// <param name="batch">Number up to 65535</param>
        /// <returns> Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description>'P' - command passed, 'F' - command failed.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Pinpad_SetBatchNumber(string batch)
        {
            StringBuilder inputString = new StringBuilder();
            inputString.Append(batch);

            string r = CustomCommand(157, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["fAnswer"] = split[0];
            return result;
        }

        /// <summary>
        /// Deletes all transactions from a payment terminal.
        ///The command is forbidden in an open fiscal receipt.
        /// </summary>
        /// <returns> Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description>'P' - command passed, 'F' - command failed.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Pinpad_DeleteBatchNumber()
        {
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(159, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["fAnswer"] = split[0];
            return result;
        }

        /// <summary>
        /// Deletes (unsuccessful) freeze transactions from a payment terminal, if any.
        ///The command is forbidden in an open fiscal receipt.
        /// </summary>
        /// <returns> Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description>'P' - command passed, 'F' - command failed.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Pinpad_DeleteFreezeTransaction()
        {
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(160, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["fAnswer"] = split[0];
            return result;
        }

        /// <summary>
        /// The next receipt will be issued under the assigned number.
        ///The command is forbidden in an open fiscal receipt.
        /// </summary>
        /// <param name="stan">Receipt number up to 65535</param>
        /// <returns> Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description>'P' - command passed, 'F' - command failed.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Pinpad_SetReceiptNumber(string stan)
        {
            StringBuilder inputString = new StringBuilder();
            inputString.Append(stan);

            string r = CustomCommand(161, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["fAnswer"] = split[0];
            return result;
        }

        /// <summary>
        /// Print a receipt by number.The command is forbidden in an open fiscal receipt
        /// </summary>
        /// <param name="number">Receipt number</param>
        /// <returns> Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description>'P' - command passed or 'F' - command failed.</description>
        /// </item>
        /// <item>
        /// <term>errorMessage</term>
        /// <description>Can be text "No data for entered number" or error code.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Pinpad_PrintByNumber(string number)
        {
            StringBuilder inputString = new StringBuilder();
            inputString.Append(number);

            string r = CustomCommand(162, inputString.ToString());
            CheckResult();

            string[] split = r.Split(' ');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["fAnswer"] = split[0];
            if (split.Length >= 2)
                result["errorMessage"] = split[1];
            return result;
        }

        //163
        /// <summary>
        /// Printing the last successful transaction in a payment terminal.\n
        /// It can only be executed if a receipt is open and the last transaction is completed without a reply from a\n
        ///payment terminal.If the A4h command (164 - receipt_Pinpad_ReturnLastSuccessfultransaction) has been executed before, or with card payment it will\n
        ///also be successfully rejected.The receipt is printed automatically after the fiscal receipt has been closed.\n
        ///Requires execution before returning command 148(info_Pinpad_CheckLastRecordedRRN()) for reference and return data collection.\n
        /// </summary>
        /// <returns> Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description>'P' - command passed, 'F' - command failed.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Pinpad_PrintLastSuccessfultransaction()
        {
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(163, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["fAnswer"] = split[0];
            return result;
        }

        /// <summary>
        /// Return the last successful transaction. It can only be executed if a receipt is open and the last transaction is completed without a reply from a
        ///payment terminal.If the A3h command (163 - receipt_Pinpad_PrintLastSuccessfultransaction()) was previously executed or the card payment is
        ///successful, it will also be rejected.The receipt is printed automatically after the fiscal receipt has been
        ///closed.Requires execution before returning command 148(info_Pinpad_CheckLastRecordedRRN()) for reference and return data.
        /// </summary>
        /// <returns> Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description>'P' - command passed, 'F' - command failed.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Pinpad_ReturnLastSuccessfultransaction()
        {
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(164, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["fAnswer"] = split[0];
            return result;
        }

        /// <summary>
        /// Establishment of RRN code in fiscal device with RRN code from payment terminal.\n
        /// It is used when changing the Fiscal Device and / or payment terminal. The command is forbidden in an open fiscal receipt.
        /// </summary>
        /// <returns> Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description>'P' - command passed, 'F' - command failed.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Pinpad_SetRRNFromPPToFD()
        {
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(165, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["fAnswer"] = split[0];
            return result;
        }

        /// <summary>
        /// Test connection to server.The command is forbidden in an open fiscal receipt
        /// </summary>
        /// <returns> Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description>'P' - command passed, 'F' - command failed.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Pinpad_TestConnectionToServer()
        {
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(166, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["fAnswer"] = split[0];
            return result;
        }

        /// <summary>
        /// End of day report.The command first prints the (unsuccessful) freeze reversal receipts, then sends data to the server, and\n
        ///finishes with printing a short report in response to the successful or unsuccessful clearing of the registers.The command is forbidden in an open fiscal receipt
        /// </summary>
        /// <returns> Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description>'P' - command passed, 'F' - command failed.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_Pinpad_EndOfDay()
        {
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(167, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["fAnswer"] = split[0];
            return result;
        }

        /// <summary>
        /// Checking and updating of payment terminal software. The command is forbidden in an open fiscal receipt
        /// </summary>
        /// <returns> Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description>'P' - command passed, 'F' - command failed.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Pinpad_GetUpdate()
        {
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(168, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["fAnswer"] = split[0];
            return result;
        }

        /// <summary>
        /// Reports. The command is forbidden in an open fiscal receipt
        /// </summary>
        /// <param name="type">1 – short report, 2 – extended report</param>
        /// <returns> Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description>'P' - command passed, 'F' - command failed.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_Pinpad_Reports(string type)
        {
            StringBuilder inputString = new StringBuilder();
            inputString.Append(type);
            string r = CustomCommand(169, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["fAnswer"] = split[0];
            return result;
        }

        /// <summary>
        /// Status of last transaction
        /// </summary>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>timeout</term>
        /// <description>Flag, if its value is 1, then the previous transaction ended with timeout</description>
        /// </item> 
        /// <item>
        /// <term>voidReceiptPrint</term>
        /// <description>Flag, if its value is 1, then the previous transaction was a reversal and the purchase reversal receipt was not printed.</description>
        /// </item>
        /// <item>
        /// <term>inTransaction</term>
        /// <description>Flag, if its value is 1, then the printer power was turned off before the end of the printer transaction.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Pinpad_StatusOfLastTransaction()
        {
            StringBuilder inputString = new StringBuilder();
            string r = CustomCommand(172, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["timeout"] = split[0];
            if (split.Length >= 2)
                result["voidReceiptPrint"] = split[1];
            if (split.Length >= 3)
                result["inTransaction>"] = split[2];
            return result;
        }

        /// <summary>
        /// Clearing transaction status flags.Clears the flags specified in the command 172(info_Pinpad_StatusOfLastTransaction()).
        /// The command is forbidden in an open fiscal receipt.
        /// </summary>
        /// <returns> Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description>'P' - command passed.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Pinpad_ClearTransactionStatusFlags()
        {
            StringBuilder inputString = new StringBuilder();
            string r = CustomCommand(173, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["fAnswer"] = split[0];
            return result;
        }

        //174
        /// <summary>
        /// Header printing options.The command is forbidden in an open fiscal receipt.
        /// </summary>
        /// <param name="option">number in the range [0 - 4], which determines the printing of the header in repeated receipt information(city, address, po box, phone.):\n
        /// 0 – short repeated receipt;\n
        /// 1 – full information;\n
        /// 2 – full information without phone;\n
        /// 3 - the current status of the print options and the number of repeated receipts;\n
        /// 4 - allows to set the number of repeated receipts</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description>'P' or current print option</description>
        /// </item> 
        /// <item>
        /// <term>fAnswer1</term>
        /// <description>If present - the current number of reprinted receipts.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Pinpad_HeaderPrintingOptions(string option)
        {
            StringBuilder inputString = new StringBuilder();
            inputString.Append(option);
            string r = CustomCommand(174, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["fAnswer"] = split[0];
            if (split.Length >= 2)
                result["fAnswer1"] = split[1];
            return result;
        }

        /// <summary>
        /// Header printing options.The command is forbidden in an open fiscal receipt.
        /// </summary>
        /// <param name="option">number in the range [0 - 4], which determines the printing of the header in repeated receipt information(city, address, po box, phone.):\n
        /// 0 – short repeated receipt;\n
        /// 1 – full information;\n
        /// 2 – full information without phone;\n
        /// 3 - the current status of the print options and the number of repeated receipts;\n
        /// 4 - allows to set the number of repeated receipts</param>
        /// <param name="numReceipts">Number representing the number of repeat receipts - max10</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description>'P' or current print option</description>
        /// </item> 
        /// <item>
        /// <term>fAnswer1</term>
        /// <description>If present - the current number of reprinted receipts.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Pinpad_HeaderPrintingOptions(string option, string numReceipts)
        {
            StringBuilder inputString = new StringBuilder();
            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(numReceipts);
            string r = CustomCommand(174, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["fAnswer"] = split[0];
            if (split.Length >= 2)
                result["fAnswer1"] = split[1];
            return result;
        }

        // 175
        /// <summary>
        /// Printing a copy of an receipt from the EJ by receipt number
        /// </summary>
        /// <param name="receiptNum">Global number of original receipt</param>
        /// <returns> Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description>'P' - command passed or 'F' - command failed.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Pinpad_PrintReceiptCopyFromEJ_ByNum(string receiptNum)
        {
            StringBuilder inputString = new StringBuilder();
            inputString.Append("#");
            inputString.Append(receiptNum);
            string r = CustomCommand(175, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["fAnswer"] = split[0];
            return result;
        }

        /// <summary>
        /// Printing a copy of an receipt from the EJ by date
        /// </summary>
        /// <param name="date">Date and time of original receipt in format DDMMYYhhmmss</param>
        /// <returns> Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description>'P' - command passed or 'F' - command failed.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Pinpad_PrintReceiptCopyFromEJ_ByDate(string date)
        {
            StringBuilder inputString = new StringBuilder();
            inputString.Append(date);
            string r = CustomCommand(175, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["fAnswer"] = split[0];
            return result;
        }

        /// <summary>
        /// Transaction status (unsuccessful). The command is forbidden in an open fiscal receipt
        /// </summary>
        /// <returns>Dictionaty with keys:
        /// <list type="table">
        /// <item>
        /// <term>reversal</term>
        /// <description>00 – no (unsuccessful) frozen transactions\n
        ///R – (unsuccessful) freeze purchase\n
        ///C – (unsuccessful) freeze reversal or\n 'F' - if commandd failed.</description>
        /// </item> 
        /// <item>
        /// <term>endDay</term>
        /// <description>'00' – no need to close the day or '01' – day closing required</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Pinpad_TransactionStatus()
        {
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(158, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();
            if (split.Length >= 1)
                result["reversal"] = split[0];
            if (split.Length >= 2)
                result["endDay"] = split[1];
            return result;
        }



        // Command number(Dec): 80 - please check fiscal device documentation.
        /// <summary>
        /// Set sound signal
        /// </summary>
        /// <param name="soundData">‘C’ Do; ‘D’ Re; ‘E’ Mi; ‘F’ Fa; ‘G’ Sol; ‘A’ La; ‘B’ Ti</param>
        public void other_Sound_Signal(string soundData)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(soundData);

            string r = CustomCommand(80, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 72 - please check fiscal device documentation.
        public Dictionary<string, string> service_Fiscalization(string serialNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(serialNumber);

            string r = CustomCommand(72, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 83 - please check fiscal device documentation.
        /// <summary>
        /// Set decimals and tax rates
        /// </summary>
        /// <param name="mutiplier">From 0 to 3. Currently deactivated and not used.</param>
        /// <param name="decimals">1 byte with value between 0 and 2, shows number of digits after decimal point.</param>
        /// <param name="currencyName">Currency name (up to 3 bytes)</param>
        /// <param name="enabledMask">8 bytes with value 0 or 1, shows if a tax group A,B ... or H is enabled or not</param>
        /// <param name="taxA">Tax rate A value</param>
        /// <param name="taxB">Tax rate B value</param>
        /// <param name="taxC">Tax rate C value</param>
        /// <param name="taxD">Tax rate D value</param>
        /// <param name="taxE">Tax rate E value</param>
        /// <param name="taxF">Tax rate F value</param>
        /// <param name="taxG">Tax rate G value</param>
        /// <param name="taxH">Tax rate H value</param>

        public void service_Set_DecimalsAndTaxRates(string mutiplier, string decimals, string currencyName, string enabledMask, string taxA, string taxB, string taxC, string taxD, string taxE, string taxF, string taxG, string taxH)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(mutiplier);
            inputString.Append(",");
            inputString.Append(decimals);
            inputString.Append(",");
            inputString.Append(currencyName);
            inputString.Append(",");
            inputString.Append(enabledMask);
            inputString.Append(",");
            inputString.Append(taxA);
            inputString.Append(",");
            inputString.Append(taxB);
            inputString.Append(",");
            inputString.Append(taxC);
            inputString.Append(",");
            inputString.Append(taxD);
            inputString.Append(",");
            inputString.Append(taxE);
            inputString.Append(",");
            inputString.Append(taxF);
            inputString.Append(",");
            inputString.Append(taxG);
            inputString.Append(",");
            inputString.Append(taxH);

            string r = CustomCommand(83, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 89 - please check fiscal device documentation.
        /// <summary>
        /// Set production test area
        /// </summary>
        /// <param name="option">'T' - makes a record in the fiscal memory</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed.</description>
        /// </item> 
        /// <item>
        /// <term>freeRecords</term>
        /// <description>The number of remaining blocks available to record such blocks. 4 bytes</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> service_Set_ProductionTestArea(string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(89, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["freeRecords"] = split[1];
            return result;
        }

        // Command number(Dec): 91 - please check fiscal device documentation.
        /// <summary>
        /// Set serial number and fiscal memory number
        /// </summary>
        /// <param name="serialNumber">These are 8 bytes: Unique Printer ID containing 2 Latin letters and at least 6 digits</param>
        /// <param name="fMNumber">These are 8 bytes: the Fiscal Memory Unit ID. Contains only digits.</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed.</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> service_Set_SerialNumber(string serialNumber, string fMNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(serialNumber);
            inputString.Append(",");
            inputString.Append(fMNumber);

            string r = CustomCommand(91, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["country"] = split[1];
            return result;
        }

        // Command number(Dec): 98 - please check fiscal device documentation.
        /// <summary>
        /// Set UIC
        /// </summary>
        /// <param name="eikValue">These are up to 14 bytes containing the UIC as text</param>
        /// <param name="eikName">This is the comment text before the UIC. By default, it is “UIC”.</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed.</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> service_Set_EIK(string eikValue, string eikName)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(eikValue);
            inputString.Append(",");
            inputString.Append(eikName);

            string r = CustomCommand(98, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 128 - please check fiscal device documentation.
        /// <summary>
        /// Service RAM reset
        /// </summary>
        public void service_RAM_Reset()
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(128, inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 133 - please check fiscal device documentation.
        /// <summary>
        /// Service disabled printing
        /// </summary>
        /// <param name="disabled">One byte with the following allowed values: '0' Printing is enabled; '1' Printing is disabled.</param>
        public void service_Disable_Print(string disabled)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(disabled);

            string r = CustomCommand(133, inputString.ToString());
            CheckResult();
        }

        // Command number(Dec): 134 - please check fiscal device documentation.
        /// <summary>
        /// Service EJ maintenance
        /// </summary>
        /// <param name="option">One byte with a value of ‘F'.</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed.</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> service_Format_KLEN(string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(134, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 135 - please check fiscal device documentation.
        /// <summary>
        /// GPRS modem test
        /// </summary>
        /// <param name="option">One byte with a value of ‘M'</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed.</description>
        /// </item> 
        /// <item>
        /// <term>signal</term>
        /// <description>Signal</description>
        /// </item>
        /// <item>
        /// <term>iMEI</term>
        /// <description>IMEI</description>
        /// </item>
        /// <item>
        /// <term>iMSI</term>
        /// <description>IMSI</description>
        /// </item>
        /// <item>
        /// <term>oper</term>
        /// <description>Operator</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> service_Test_GPRS(string option)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(135, inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["signal"] = split[1];
            if (split.Length >= 3)
                result["iMEI"] = split[2];
            if (split.Length >= 4)
                result["iMSI"] = split[3];
            if (split.Length >= 5)
                result["oper"] = split[4];
            return result;
        }
        //AI generated source code  -end
        public bool ItIs_SummerDT(string dateTime)
        {
            DateTime dt;
            // in format ddMMyyHHmmss 
            dt = DateTime.ParseExact(dateTime, "ddMMyyHHmmss", null);
            return dt.IsDaylightSavingTime();

        }

        private static int Checksum_ean8(String data)
        {
            // Test string for correct length
            if (data.Length != 8)
                return -1;

            // Test string for being numeric
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] < 0x30 || data[i] > 0x39)
                    return -1;
            }

            int sum = 0;

            for (int i = 6; i >= 0; i--)
            {
                int digit = data[i] - 0x30;
                if ((i & 0x01) == 1)
                    sum += digit;
                else
                    sum += digit * 3;
            }
            int mod = sum % 10;
            return mod == 0 ? 0 : 10 - mod;
        }

        private static int Checksum_ean13(String data)
        {
            // Test string for correct length
            if (data.Length != 13)
                return -1;

            // Test string for being numeric
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] < 0x30 || data[i] > 0x39)
                    return -1;
            }

            int sum = 0;

            for (int i = 11; i >= 0; i--)
            {
                int digit = data[i] - 0x30;
                if ((i & 0x01) == 1)
                    sum += digit;
                else
                    sum += digit * 3;
            }
            int mod = sum % 10;
            return mod == 0 ? 0 : 10 - mod;
        }

        private string[] ReadDocumentLines(int documentNum)
        {
            bool present = false;
            string formBarcodeText1 = "", formBarcodeText2 = "";

            if (!device_Connected)
                throw new Exception("Fiscal device not connected");

            List<string> lines = new List<string>();

            // command 125 - Information from EJ,reading by document number.                                
            var result = klen_Find_Document("R", userDocumentType.ToString(), documentNum.ToString());
            if (result["fAnswer"] != "P")
                throw new Exception("Reading document failed!");
            lines.Add(result["fRow"]);
            do
            {
                var rowReslt = klen_Get_NextTextRow("N"); // read line by line as text                               
                if (rowReslt["fAnswer"] == "F") break;
                string[] rowsData = rowReslt["fRow"].Split(new string[] { "\r\n" }, StringSplitOptions.None);

                if (rowsData[0].Contains("О б щ а   с у м а"))
                {
                    var tr = rowsData[0].Replace("О б щ а   с у м а", "");
                    var trr = tr.Replace(" ", "");
                    if (trr != "") sum = trr;
                }
                if (rowsData[0].Contains(SerialNumber + " "))
                {
                    var part = rowsData[0].Replace(SerialNumber, "");
                    fMNumFromReceipt = part.Replace(" ", "");
                }
                lines.AddRange(rowsData);
            } while (true);

            string[] myArray = lines.ToArray();
            //string row = "Ф И С К А Л Е Н   Б О Н";
            var index = Array.FindIndex(myArray, row => row.Contains("Ф И С К А Л Е Н   Б О Н"));
            var indx = Array.FindIndex(myArray, row => row.Contains(" С Л У Ж Е Б Е Н   Б О Н")); // for duplicate receipt 
            if (index > 0 && myArray[index - 1].Contains("БК ["))
            {
                qrCodeText = myArray[index - 1].Replace("БК [", "");
                qrCodeText = qrCodeText.Replace("]", "");
                myArray = myArray.Where(w => w != myArray[index - 1]).ToArray();
            }
            if (indx > 0 && myArray[indx - 1].Contains("БК ["))
            {
                qrCodeText = myArray[indx - 1].Replace("БК [", "");
                qrCodeText = qrCodeText.Replace("]", "");
                myArray = myArray.Where(w => w != myArray[indx - 1]).ToArray();
            }
            return myArray;
        }


        private SizeF DrawReceiptOnGraphics(Graphics gr, string documentNumber, Font font, string[] lines, bool calculate)
        {
            //void DrawImageUnscaled(Image img, float x, float y)
            //{
            //    if (!calculate)
            //        return;
            //    gr.DrawImageUnscaled(img, x, y);
            //}


            string dt = "", date = "", time = "";
            bool barcodeFlag = false;
            var receiptSize = new SizeF(0, 0);
            Brush textBrush = new SolidBrush(Color.Black);
            string receiptNumber = documentNumber.PadLeft(7, '0');
            var maxCharsPerLine = 0;
            bool IsFiscal = false;

            gr.Clear(Color.White);
            Font boldFont = new Font(font.Name, 16, FontStyle.Bold);
            Font boldFontRegular = new Font(font.Name, 12, FontStyle.Bold);
            Image bgMap = Image.FromFile(Directory.GetCurrentDirectory() + "\\Resources\\BGmapS.bmp");
            if (bmLogo != null)
            {
                if (deviceModel == "FP-700" || deviceModel == "FP-2000" || deviceModel == "FP-800" || deviceModel == "FP-650")

                    gr.DrawImage(bmLogo, 0, 0, 514F, 96F);
                else gr.DrawImage(bmLogo, 0, 0, 322F, 96F);
                receiptSize.Height += bmLogo.Height + 5; //bit of a space
                //receiptSize.Width = Math.Max(receiptSize.Width, bmLogo.Width);
            }
            foreach (var line in lines)
            {
                if (line.Contains("БК "))
                {
                    int first = line.IndexOf('[');
                    int last = line.IndexOf(']');

                    var barcodeText = line.Substring(first + 1, last - first - 1);
                    //barcodeText = barcodeText.Replace("]", "");

                    // We use Code128 barcode, because we can not say which is the type of barcode, because we don't know the checksum
                    // If you want you can use Ean8 or Ean13 as well. These types of barcodes can be printed with commands (84) - receipt_Print_Barcode_01 or receipt_Print_Barcode_02
                    //barcodeText += AppendCode128CheckDigit(barcodeText);

                    var barcodeWriter = new BarcodeWriter<Bitmap>
                    {
                        Format = BarcodeFormat.CODE_128,
                        Options = new ZXing.Common.EncodingOptions
                        {
                            Height = 30,
                            Margin = 0
                        }
                    };
                    Image picBarcode = barcodeWriter.Write(barcodeText);

                    if (!calculate)
                        gr.DrawImageUnscaled(picBarcode, new Point((int)(receiptSize.Width - picBarcode.Width) / 2, (int)receiptSize.Height));
                    receiptSize.Height += picBarcode.Height + 30; //bit of a space
                    receiptSize.Width = Math.Max(receiptSize.Width, picBarcode.Width);
                    continue;
                }

                var qrCodeWriter = new BarcodeWriter<Bitmap>
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new ZXing.Common.EncodingOptions
                    {
                        Height = 20,
                        Margin = 0,
                    }
                };
                var pictureBarcode = qrCodeWriter.Write(qrCodeText);


                if (line.Contains("Ф И С К А Л Е Н   Б О Н"))
                {
                    //float charSize = ((float)receiptSize.Width / (float)maxCharsPerLine);
                    IsFiscal = true;

                    if (!calculate && qrCodeText != "")
                        gr.DrawImageUnscaled(pictureBarcode, new Point((int)(receiptSize.Width - pictureBarcode.Width) / 2, (int)receiptSize.Height));
                    receiptSize.Height += pictureBarcode.Height + 5; //bit of a space

                    var newLine = "ФИСКАЛЕН БОН";
                    var fiscBonSize = gr.MeasureString(newLine, boldFont);
                    if (!calculate)
                    {
                        PointF mapPoint = new PointF((receiptSize.Width - (bgMap.Width + fiscBonSize.Width)) / 2, receiptSize.Height);
                        gr.DrawImage(bgMap, mapPoint);
                        gr.DrawString(newLine, boldFont, textBrush, mapPoint.X + bgMap.Width + 10, receiptSize.Height);
                    }

                    receiptSize.Height += bgMap.Height + 30; //bit of a space
                    receiptSize.Width = Math.Max(receiptSize.Width, bgMap.Width + fiscBonSize.Width);
                }
                else
                {
                    if (!calculate)
                    {

                        if (line.Contains("           С Т О Р Н О") || line.Contains(" О Р И Г И Н А Л"))
                        {
                            string f = line.Trim().Replace(" ", "");
                            var tSize = gr.MeasureString(f, boldFont);
                            gr.DrawString(f, boldFont, textBrush, (receiptSize.Width - tSize.Width) / 2, receiptSize.Height);
                        }
                        else if (line.Contains(" Д Н Е В Е Н   О Т Ч Е Т") || line.Contains(" Д Н Е В Е Н") || line.Contains(" Ф И Н А Н С О В   О Т Ч Е Т")//
                            || line.Contains(" О Б Щ О ") || line.Contains("         Д У Б Л И К А Т") || line.Contains(" А Н У Л И Р А Н О"))
                        {
                            string newLine = line.Substring(1);
                            if (line.Contains("         Д У Б Л И К А Т") || line.Contains(" А Н У Л И Р А Н О")) newLine = line;
                            float charSize = ((float)receiptSize.Width / (float)maxCharsPerLine);
                            for (int bi = 0; bi < newLine.Length; bi++)
                            {
                                if (newLine[bi] != ' ')
                                    gr.DrawString(newLine.Substring(bi, 1), boldFont, textBrush, (float)bi * charSize, receiptSize.Height);

                            }
                        }

                        else if (line.Contains("МЕЖДИННА СУМА"))
                        {
                            var firstPart = line.Substring(0, 13);
                            var secondPart = line.Substring(13);
                            secondPart = secondPart.Replace(" ", "");
                            if (secondPart.Contains("#")) secondPart.Substring(0, secondPart.Length - 1);//at the end
                            var tSize = gr.MeasureString(secondPart, boldFontRegular);
                            gr.DrawString(firstPart, boldFontRegular, textBrush, 0f, receiptSize.Height);
                            gr.DrawString(secondPart, boldFontRegular, textBrush, (receiptSize.Width - tSize.Width), receiptSize.Height);
                        }

                        else if (line.Contains("ДНЕВЕН ОБОРОТ, ДДС") || line.Contains("СТОРНО ОБОРОТ, ДДС") || line.Contains("НОМЕР БЛОК ФИСКАЛНА ПАМЕТ"))
                        {

                            gr.DrawString(line, boldFontRegular, textBrush, 0f, receiptSize.Height);
                        }
                        else if (line.Contains(" С Л У Ж Е Б Е Н   Б О Н"))
                        {
                            var newLine = "СЛУЖЕБЕН БОН";
                            var serviceBonSize = gr.MeasureString(newLine, boldFont);
                            if (qrCodeText != "")
                            {
                                gr.DrawImageUnscaled(pictureBarcode, new Point((int)(receiptSize.Width - pictureBarcode.Width) / 2, (int)receiptSize.Height));
                                receiptSize.Height += pictureBarcode.Height + 5;
                            }
                            gr.DrawString(newLine, boldFont, textBrush, (receiptSize.Width - serviceBonSize.Width) / 2, receiptSize.Height);
                        }

                        else if (line.Contains("НЕ СЕ ДЪЛЖИ ПЛАЩАНЕ"))
                        {
                            var txtSize = gr.MeasureString(line.Trim(), boldFont);
                            Bitmap bm = new Bitmap((int)txtSize.Width, (int)txtSize.Height * 3);
                            Graphics g = Graphics.FromImage(bm);
                            g.ScaleTransform(1, 3);
                            g.Clear(Color.White);
                            g.DrawString(line.Trim(), boldFont, textBrush, 0F, 0F);

                            gr.DrawImageUnscaled(bm, (int)(receiptSize.Width - bm.Width) / 2, (int)receiptSize.Height);
                            //gr.ScaleTransform(1, 3);
                            //gr.DrawString(line, boldFontRegular, textBrush, 0f, receiptSize.Height / 3);
                            //gr.ScaleTransform(1, 1);
                        }

                        else gr.DrawString(line, font, textBrush, 0f, receiptSize.Height);

                        if (line.Length > maxCharsPerLine)
                            maxCharsPerLine = line.Length;
                    }

                    var textSize = gr.MeasureString(line, font);
                    if (line.Contains("НЕ СЕ ДЪЛЖИ ПЛАЩАНЕ"))
                        receiptSize.Height += textSize.Height * 3; //bit of a space
                    else receiptSize.Height += textSize.Height + 5;
                    if (line.Contains("С Л У Ж Е Б Е Н   Б О Н") && !IsFiscal && qrCodeText != "")
                    {
                        if (calculate)
                            receiptSize.Height += pictureBarcode.Height + 5; //bit of a space
                        receiptSize.Width = Math.Max(receiptSize.Width, pictureBarcode.Width);
                    }
                    receiptSize.Width = Math.Max(receiptSize.Width, textSize.Width);
                }
            }

            return receiptSize;
        }

        private Image DrawReceipt(string[] lines, string documentNumber)
        {
            Font font = new Font("Courier New", 12);

            //calculate the size first using 1x1 bitmap
            Image img = new Bitmap(1, 1);
            Graphics gr = Graphics.FromImage(img);
            SizeF imgSize = DrawReceiptOnGraphics(gr, documentNumber, font, lines, true);

            //now that we have it, make real image and draw to it
            img = new Bitmap((int)imgSize.Width, (int)imgSize.Height);
            gr = Graphics.FromImage(img);
            DrawReceiptOnGraphics(gr, documentNumber, font, lines, false);

            return img;
        }

        public Image ReadAndDrawReceipt(string documentNumber, string serialNum, string fiscMemNum)
        {

            var lines = ReadDocumentLines(int.Parse(documentNumber));
            return DrawReceipt(lines, documentNumber);

        }

        public void ReadAndDrawReceiptToFile(string FileName, string documentNumber, string serialNum, string fiscMemNum, bool drawLogoFromDevice)
        {
            bmLogo = null;
            qrCodeText = "";
            try
            {
                if (drawLogoFromDevice)
                {
                    bmLogo = LogoToBitmap(ReadDeviceLogo());
                }

                if (serialNum != SerialNumber) throw new Exception("Entered serial number is not equal to device serial number!");
                var image = ReadAndDrawReceipt(documentNumber, serialNum, fiscMemNum);
                if (fiscMemNum != fMNumFromReceipt) throw new Exception("Entered fiscal memory number is not equal to current device FM number!");
                ImageFormat format = ImageFormat.Png;
                image.Save(FileName, format);
            }
            catch
            {
                throw;
            }
        }
    }
}
