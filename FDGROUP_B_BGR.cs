//
//  Fiscal Devices Group "B" - Bulgaria
//
//  Created by Rosi and Doba on 04.07.2019
//  Modified on 04.07.2019
//  Copyright (c) 2019 Datecs. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using ZXing;

namespace TestSDK
{
    /// <summary>
    /// Fiscal Devices Group "B" - Bulgaria
    /// </summary>
    public class FDGROUP_B_BGR : FiscalPrinter
    {
        private string infoLastErrorText;
        public StatusBit[,] fstatusbytes = new StatusBit[6, 8];

        public FDGROUP_B_BGR(FiscalComm comm)
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
        string fMNumFromReceipt = "";
        string documentDateTime = "";

        public override void Initialize_StatusBytes()
        {
            Set_AllsBytesBitsState();
            Set_sBytesBitInUse();
            SetStatusBytes_Errors_Default();
            SetStatusBits_Descriptions();
        }

        // Properties for current state of the device informative status bits
        public bool iSBit_No_ClientDisplay => fstatusbytes[0, 3].fCurrentState; // 0.3 = 1 No client display installed
        public bool iSBit_TimeoutWithUnsentDocs => fstatusbytes[1,5].fCurrentState; // 1.5 = 1 Timeout, but there are unsent documents
        public bool iSBit_Receipt_Nonfiscal => fstatusbytes[2, 5].fCurrentState; // 2.5 = 1 Nonfiscal receipt is open
        public bool iSBit_EJ_NearlyFull => fstatusbytes[2, 4].fCurrentState; // 2.4  = 1 EJ nearly full
        public bool iSBit_Receipt_Fiscal => fstatusbytes[2, 3].fCurrentState; // 2.3 = 1 Fiscal receipt is open
        public bool iSBit_LessThan_50_Reports => fstatusbytes[4, 3].fCurrentState; // 4.3 = 1 There is space for less then 50 reports in Fiscal memory
        public bool iSBit_Number_SFM_Set => fstatusbytes[4, 2].fCurrentState; // 4.2 = 1 Serial number and number of FM are set
        public bool iSBit_Number_Tax_Set => fstatusbytes[4, 1].fCurrentState; // 4.1 = 1 Tax number is set
        public bool iSBit_VAT_Set => fstatusbytes[5, 4].fCurrentState; // 5.4 = 1 VAT are set at least once.
        public bool iSBit_Device_Fiscalized => fstatusbytes[5, 3].fCurrentState; // 5.3 = 1 Device is fiscalized
        public bool iSBit_FM_formatted => fstatusbytes[5, 1].fCurrentState; // 5.1 = 1 FM is formatted

        // Properties for current state of the device error status bits

        public bool eSBit_GeneralError_Sharp => fstatusbytes[0, 5].fCurrentState; //0.5 = 1# General error - this is OR of all errors marked with #
        public bool eSBit_ClockIsNotSynchronized => fstatusbytes[0, 2].fCurrentState; // 0.2 = 1 The real time clock is not synchronize
        public bool eSBit_CommandCodeIsInvalid => fstatusbytes[0, 1].fCurrentState; //0.1 = 1# Command code is invalid.
        public bool eSBit_SyntaxError => fstatusbytes[0, 0].fCurrentState; //0.0 = 1# Syntax error.
        public bool eSBit_BuildInTaxTerminalNotResponding => fstatusbytes[1, 6].fCurrentState; //1.6 = 1 The built-in tax terminal is not responding.
        public bool eSBit_CommandNotPermitted => fstatusbytes[1, 1].fCurrentState; //1.1 = 1# Command is not permitted.
        public bool eSBit_Overflow => fstatusbytes[1, 0].fCurrentState; // 1.0 = 1# Overflow during command execution.
        public bool eSBit_EJIsFull => fstatusbytes[2, 2].fCurrentState; //2.2 = 1 EJ is full.
        public bool eSBit_EndOfPaper => fstatusbytes[2, 0].fCurrentState; // 2.0 = 1# End of paper.
        public bool eSBit_FM_NotAccess => fstatusbytes[4, 0].fCurrentState; // 4.0 = 1* Error when trying to access data stored in the FM.
        public bool eSBit_FM_Full => fstatusbytes[4, 4].fCurrentState;// 4.4 = 1* Fiscal memory is full.
        public bool eSBit_GeneralError_Star => fstatusbytes[4, 5].fCurrentState;// 4.5 = 1 OR of all errors marked with ‘*’

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

        /// <summary>
        /// Get if desired bit is an error
        /// </summary>
        /// <param name="byteIndex">Desired byte index</param>
        /// <param name="bitIndex">Desired bit index</param>
        /// <returns>
        /// Returns bool (1 - if the bit is considered error, 0 - is it is informatice)
        /// </returns>
        public bool Get_SBit_ErrorChecking(int byteIndex, int bitIndex)
        {
            return fstatusbytes[byteIndex, bitIndex].fErrorForEndUser;
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
            fstatusbytes[0, 5].fInUse = true;
            fstatusbytes[1, 0].fInUse = true;
            fstatusbytes[1, 1].fInUse = true;
            fstatusbytes[1, 5].fInUse = true;
            fstatusbytes[1, 6].fInUse = true;
            fstatusbytes[2, 0].fInUse = true;
            fstatusbytes[2, 2].fInUse = true;
            fstatusbytes[2, 3].fInUse = true;
            fstatusbytes[2, 4].fInUse = true;
            fstatusbytes[2, 5].fInUse = true;
            fstatusbytes[4, 0].fInUse = true;
            fstatusbytes[4, 1].fInUse = true;
            fstatusbytes[4, 2].fInUse = true;
            fstatusbytes[4, 3].fInUse = true;
            fstatusbytes[4, 4].fInUse = true;
            fstatusbytes[4, 5].fInUse = true;
            fstatusbytes[5, 1].fInUse = true;
            fstatusbytes[5, 3].fInUse = true;
            fstatusbytes[5, 4].fInUse = true;
        }

        public void Set_Sbit_ErrorChecking(int byteIndex, int bitIndex, bool IsError) // Да може клиентът да си промени дали един статус бит е грешка или не
        {
            fstatusbytes[byteIndex, bitIndex].fErrorForEndUser = IsError;
        }

        public void SetStatusBytes_Errors_Default()
        {
            fstatusbytes[0, 0].fErrorForEndUser = true;
            fstatusbytes[0, 1].fErrorForEndUser = true;
            fstatusbytes[0, 2].fErrorForEndUser = true;
            fstatusbytes[0, 5].fErrorForEndUser = true;
            fstatusbytes[1, 0].fErrorForEndUser = true;
            fstatusbytes[1, 1].fErrorForEndUser = true;
            fstatusbytes[1, 6].fErrorForEndUser = true;
            fstatusbytes[2, 0].fErrorForEndUser = true;
            fstatusbytes[2, 2].fErrorForEndUser = true;
            fstatusbytes[4, 0].fErrorForEndUser = true;
            fstatusbytes[4, 4].fErrorForEndUser = true;
            fstatusbytes[4, 5].fErrorForEndUser = true;

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
            fstatusbytes[0, 4].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[0, 5].fTextDescription = GetErrorMessage("-29");
            fstatusbytes[0, 6].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[0, 7].fTextDescription = GetErrorMessage("-31");

            fstatusbytes[1, 0].fTextDescription = GetErrorMessage("-33");
            fstatusbytes[1, 1].fTextDescription = GetErrorMessage("-32");
            fstatusbytes[1, 2].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[1, 3].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[1, 4].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[1, 5].fTextDescription = GetErrorMessage("-51");
            fstatusbytes[1, 6].fTextDescription = GetErrorMessage("-50");
            fstatusbytes[1, 7].fTextDescription = GetErrorMessage("-31");

            fstatusbytes[2, 0].fTextDescription = GetErrorMessage("-39");
            fstatusbytes[2, 1].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[2, 2].fTextDescription = GetErrorMessage("-37");
            fstatusbytes[2, 3].fTextDescription = GetErrorMessage("-36");
            fstatusbytes[2, 4].fTextDescription = GetErrorMessage("-35");
            fstatusbytes[2, 5].fTextDescription = GetErrorMessage("-34");
            fstatusbytes[2, 6].fTextDescription = GetErrorMessage("-70");
            fstatusbytes[2, 7].fTextDescription = GetErrorMessage("-31");

            fstatusbytes[3, 0].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[3, 1].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[3, 2].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[3, 3].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[3, 4].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[3, 5].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[3, 6].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[3, 7].fTextDescription = GetErrorMessage("-31");

            fstatusbytes[4, 0].fTextDescription = GetErrorMessage("-46");
            fstatusbytes[4, 1].fTextDescription = GetErrorMessage("-45");
            fstatusbytes[4, 2].fTextDescription = GetErrorMessage("-44");
            fstatusbytes[4, 3].fTextDescription = GetErrorMessage("-52");
            fstatusbytes[4, 4].fTextDescription = GetErrorMessage("-42");
            fstatusbytes[4, 5].fTextDescription = GetErrorMessage("-41");
            fstatusbytes[4, 6].fTextDescription = GetErrorMessage("-70");
            fstatusbytes[4, 7].fTextDescription = GetErrorMessage("-31");

            fstatusbytes[5, 0].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[5, 1].fTextDescription = GetErrorMessage("-49");
            fstatusbytes[5, 2].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[5, 3].fTextDescription = GetErrorMessage("-48");
            fstatusbytes[5, 4].fTextDescription = GetErrorMessage("-47");
            fstatusbytes[5, 5].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[5, 6].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[5, 7].fTextDescription = GetErrorMessage("-31");

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

            string r = CustomCommand(33 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 35 - please check fiscal device documentation.
        /// <summary>
        /// Show text on lower line of the external display
        /// </summary>
        /// <param name="textData">Text up to 20 symbols</param>
        public void display_Show_LowerLine(string textData) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textData);

            string r = CustomCommand(35 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 47 - please check fiscal device documentation.
        /// <summary>
        /// Show text on upper line of the external display
        /// </summary>
        /// <param name="textData">Text up to 20 symbols</param>
        public void display_Show_UpperLine(string textData) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textData);

            string r = CustomCommand(47 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 63 - please check fiscal device documentation.
        /// <summary>
        /// Show date and time
        /// </summary>
        public void display_Show_DateTime() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(63 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 100 - please check fiscal device documentation.
        /// <summary>
        /// Show text on the external display
        /// </summary>
        public void display_Show_Text() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(100 , inputString.ToString());
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
        /// <description>All executed receipts count(fiscal and non-fiscal) since last Z report (4 bytes).</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_NonFiscal_Open() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(38 , inputString.ToString());
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
        /// <description>All executed receipts count(fiscal and non-fiscal) since last Z report (4 bytes).</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_NonFiscal_Close() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(39 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["allReceipt"] = split[0];
            return result;
        }

        // Command number(Dec): 42 - please check fiscal device documentation.
        /// <summary>
        /// Print free non-fiscal text
        /// </summary>
        /// <param name="inputText">Text up to 40 symbols</param>
        public void receipt_NonFiscal_Text(string inputText) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(inputText);

            string r = CustomCommand(42 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 42 - please check fiscal device documentation.
        /// <summary>
        /// Print free non-fiscal text
        /// </summary>
        /// <param name="height">'1' - Print normal height; '2' - Print 2xheight; '3' - Print 3x height.</param>
        /// <param name="inputText">Text up to 40 symbols</param>
        public void receipt_PNonFiscal_Text(string height, string inputText) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(height);
            inputString.Append("\t");
            inputString.Append(inputText);

            string r = CustomCommand(42 , inputString.ToString());
            CheckResult();
        }

       
        // command 46 - storno combined all variants
        /// <summary>
        /// Open storno receipt
        /// </summary>
        /// <param name="operatorNumber">Operator code (1...30)</param>
        /// <param name="operatorPassword">Operator password up to 8 symbols</param>
        /// <param name="stornoUNP">UNP - 21 symbols: format - CCCCCCCC-CCCC-DDDDDDD: [0-9A-Za-z]{8}-[0-9A-Za-z]{4}-[0-9]{7} )</param>
        /// <param name="tillNumber">Till number (1-99999)</param>
        /// <param name="stornoType">Reason for storno: 0 - operator's error; 1 - refund.</param>
        /// <param name="stornoDocumentNumber">Number of the original document ( global 1...9999999)</param>
        /// <param name="stornoDateTime">Document date and time (format: ddMMYYHHmm or ddMMYYHHmmss)</param>
        /// <param name="stornoFMNumber">Fiscal memory number for the storno document (8 symbols)</param>
        /// <param name="invoice">If it is storno invoice - enter parameter "I"</param>
        /// <param name="invoiceNumber">Invoice number (1 - 9999999999).</param>
        /// <param name="stornoReason">Additional reason up to 42 symbols.</param>
        public void open_StornoReceipt(string operatorNumber, string operatorPassword, string stornoUNP, string tillNumber, string stornoType, string stornoDocumentNumber, string stornoDateTime, string stornoFMNumber, string invoice, string invoiceNumber, string stornoReason)
        {
            //bool Y(string param) { return !string.IsNullOrEmpty(param); } // defined as lambda function above
            //bool N(string param) { return string.IsNullOrEmpty(param); }

            if (Y(stornoUNP) && Y(invoice) && Y(invoiceNumber) && Y(stornoReason))
                receipt_StornoOpen_B06(operatorNumber,operatorPassword,stornoUNP,tillNumber,stornoType,stornoDocumentNumber,stornoDateTime,stornoFMNumber,invoice,invoiceNumber,stornoReason);

            if (Y(stornoUNP) && Y(invoice) && Y(invoiceNumber) && N(stornoReason))
                receipt_StornoOpen_B05(operatorNumber, operatorPassword, stornoUNP, tillNumber, stornoType, stornoDocumentNumber, stornoDateTime, stornoFMNumber, invoice, invoiceNumber);

            if (N(stornoUNP) && Y(invoice) && Y(invoiceNumber) && Y(stornoReason))
                receipt_StornoOpen_B04(operatorNumber, operatorPassword, tillNumber, stornoType, stornoDocumentNumber, stornoDateTime, stornoFMNumber, invoice, invoiceNumber, stornoReason);

            if (N(stornoUNP) && Y(invoice) && Y(invoiceNumber) && N(stornoReason))
                receipt_StornoOpen_B03(operatorNumber, operatorPassword, tillNumber, stornoType, stornoDocumentNumber, stornoDateTime, stornoFMNumber, invoice, invoiceNumber);

            if (Y(stornoUNP) && N(invoice) && N(invoiceNumber) && N(stornoReason))
                receipt_StornoOpen_B02(operatorNumber, operatorPassword, stornoUNP, tillNumber, stornoType, stornoDocumentNumber, stornoDateTime, stornoFMNumber);

            if (N(stornoUNP) && N(invoice) && N(invoiceNumber) && N(stornoReason))
                receipt_StornoOpen_B01(operatorNumber, operatorPassword, tillNumber, stornoType, stornoDocumentNumber, stornoDateTime, stornoFMNumber);

            CheckResult();
        }

        // Command number(Dec): 46 - please check fiscal device documentation.
        /// <summary>
        /// Open storno receipt
        /// </summary>
        /// <param name="operatorNumber">Operator code (1...30)</param>
        /// <param name="operatorPassword">Operator password up to 8 symbols</param>
        /// <param name="tillNumber">Till number (1-99999)</param>
        /// <param name="stornoType">Reason for storno: 0 - operator's error; 1 - refund.</param>
        /// <param name="stornoDocumentNumber">Number of the original document ( global 1...9999999)</param>
        /// <param name="stornoDateTime">Document date and time (format: ddMMYYHHmm or ddMMYYHHmmss)</param>
        /// <param name="stornoFMNumber">Fiscal memory number for the storno document (8 symbols)</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>All executed receipts count(fiscal and non-fiscal) since last Z report (4 bytes).</description>
        /// </item>
        /// <item>
        /// <term>stornoReceiptCount</term>
        /// <description>All executed storno receipts count since last Z report (4 bytes).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_StornoOpen_B01(string operatorNumber, string operatorPassword, string tillNumber, string stornoType, string stornoDocumentNumber, string stornoDateTime, string stornoFMNumber) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(stornoType);
            inputString.Append(",");
            inputString.Append(stornoDocumentNumber);
            inputString.Append(",");
            inputString.Append(stornoDateTime);
            inputString.Append(",");
            inputString.Append(stornoFMNumber);

            string r = CustomCommand(46 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["allReceiptCount"] = split[0];
            if(split.Length >= 2) 
                result["stornoReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 46 - please check fiscal device documentation.
        /// <summary>
        /// Open storno receipt
        /// </summary>
        /// <param name="operatorNumber">Operator code (1...30)</param>
        /// <param name="operatorPassword">Operator password up to 8 symbols</param>
        /// <param name="stornoUNP">UNP - 21 symbols: format - CCCCCCCC-CCCC-DDDDDDD: [0-9A-Za-z]{8}-[0-9A-Za-z]{4}-[0-9]{7} )</param>
        /// <param name="tillNumber">Till number (1-99999)</param>
        /// <param name="stornoType">Reason for storno: 0 - operator's error; 1 - refund.</param>
        /// <param name="stornoDocumentNumber">Number of the original document ( global 1...9999999)</param>
        /// <param name="stornoDateTime">Document date and time (format: ddMMYYHHmm or ddMMYYHHmmss)</param>
        /// <param name="stornoFMNumber">Fiscal memory number for the storno document (8 symbols)</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>All executed receipts count(fiscal and non-fiscal) since last Z report (4 bytes).</description>
        /// </item>
        /// <item>
        /// <term>stornoReceiptCount</term>
        /// <description>All executed storno receipts count since last Z report (4 bytes).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_StornoOpen_B02(string operatorNumber, string operatorPassword, string stornoUNP, string tillNumber, string stornoType, string stornoDocumentNumber, string stornoDateTime, string stornoFMNumber) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(stornoUNP);
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(stornoType);
            inputString.Append(",");
            inputString.Append(stornoDocumentNumber);
            inputString.Append(",");
            inputString.Append(stornoDateTime);
            inputString.Append(",");
            inputString.Append(stornoFMNumber);

            string r = CustomCommand(46 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["allReceiptCount"] = split[0];
            if(split.Length >= 2) 
                result["stornoReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 46 - please check fiscal device documentation.
        /// <summary>
        /// Open storno receipt
        /// </summary>
        /// <param name="operatorNumber">Operator code (1...30)</param>
        /// <param name="operatorPassword">Operator password up to 8 symbols</param>
        /// <param name="tillNumber">Till number (1-99999)</param>
        /// <param name="stornoType">Reason for storno: 0 - operator's error; 1 - refund.</param>
        /// <param name="stornoDocumentNumber">Number of the original document ( global 1...9999999)</param>
        /// <param name="stornoDateTime">Document date and time (format: ddMMYYHHmm or ddMMYYHHmmss)</param>
        /// <param name="stornoFMNumber">Fiscal memory number for the storno document (8 symbols)</param>
        /// <param name="invoice">If it is storno invoice - enter parameter "I"</param>
        /// <param name="invoiceNumber">Invoice number (1 - 9999999999).</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>All executed receipts count(fiscal and non-fiscal) since last Z report (4 bytes).</description>
        /// </item>
        /// <item>
        /// <term>stornoReceiptCount</term>
        /// <description>All executed storno receipts count since last Z report (4 bytes).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_StornoOpen_B03(string operatorNumber, string operatorPassword, string tillNumber, string stornoType, string stornoDocumentNumber, string stornoDateTime, string stornoFMNumber, string invoice, string invoiceNumber) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(stornoType);
            inputString.Append(",");
            inputString.Append(stornoDocumentNumber);
            inputString.Append(",");
            inputString.Append(stornoDateTime);
            inputString.Append(",");
            inputString.Append(stornoFMNumber);
            inputString.Append(",");
            inputString.Append(invoice);
            inputString.Append(",");
            inputString.Append(invoiceNumber);
            inputString.Append(",");

            string r = CustomCommand(46 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["allReceiptCount"] = split[0];
            if(split.Length >= 2) 
                result["stornoReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 46 - please check fiscal device documentation.
        /// <summary>
        /// Open storno receipt
        /// </summary>
        /// <param name="operatorNumber">Operator code (1...30)</param>
        /// <param name="operatorPassword">Operator password up to 8 symbols</param>
        /// <param name="tillNumber">Till number (1-99999)</param>
        /// <param name="stornoType">Reason for storno: 0 - operator's error; 1 - refund.</param>
        /// <param name="stornoDocumentNumber">Number of the original document ( global 1...9999999)</param>
        /// <param name="stornoDateTime">Document date and time (format: ddMMYYHHmm or ddMMYYHHmmss)</param>
        /// <param name="stornoFMNumber">Fiscal memory number for the storno document (8 symbols)</param>
        /// <param name="invoice">If it is storno invoice - enter parameter "I"</param>
        /// <param name="invoiceNumber">Invoice number (1 - 9999999999).</param>
        /// <param name="stornoReason">Additional storno reason up to 42 symbols </param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>All executed receipts count(fiscal and non-fiscal) since last Z report (4 bytes).</description>
        /// </item>
        /// <item>
        /// <term>stornoReceiptCount</term>
        /// <description>All executed storno receipts count since last Z report (4 bytes).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_StornoOpen_B04(string operatorNumber, string operatorPassword, string tillNumber, string stornoType, string stornoDocumentNumber, string stornoDateTime, string stornoFMNumber, string invoice, string invoiceNumber, string stornoReason) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(stornoType);
            inputString.Append(",");
            inputString.Append(stornoDocumentNumber);
            inputString.Append(",");
            inputString.Append(stornoDateTime);
            inputString.Append(",");
            inputString.Append(stornoFMNumber);
            inputString.Append(",");
            inputString.Append(invoice);
            inputString.Append(",");
            inputString.Append(invoiceNumber);
            inputString.Append(",");
            inputString.Append(stornoReason);

            string r = CustomCommand(46 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["allReceiptCount"] = split[0];
            if(split.Length >= 2) 
                result["stornoReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 46 - please check fiscal device documentation.
        /// <summary>
        /// Open storno receipt
        /// </summary>
        /// <param name="operatorNumber">Operator code (1...30)</param>
        /// <param name="operatorPassword">Operator password up to 8 symbols</param>
        /// <param name="stornoUNP">UNP - 21 symbols: format - CCCCCCCC-CCCC-DDDDDDD: [0-9A-Za-z]{8}-[0-9A-Za-z]{4}-[0-9]{7} )</param>
        /// <param name="tillNumber">Till number (1-99999)</param>
        /// <param name="stornoType">Reason for storno: 0 - operator's error; 1 - refund.</param>
        /// <param name="stornoDocumentNumber">Number of the original document ( global 1...9999999)</param>
        /// <param name="stornoDateTime">Document date and time (format: ddMMYYHHmm or ddMMYYHHmmss)</param>
        /// <param name="stornoFMNumber">Fiscal memory number for the storno document (8 symbols)</param>
        /// <param name="invoice">If it is storno invoice - enter parameter "I"</param>
        /// <param name="invoiceNumber">Invoice number (1 - 9999999999).</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>All executed receipts count(fiscal and non-fiscal) since last Z report (4 bytes).</description>
        /// </item>
        /// <item>
        /// <term>stornoReceiptCount</term>
        /// <description>All executed storno receipts count since last Z report (4 bytes).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_StornoOpen_B05(string operatorNumber, string operatorPassword, string stornoUNP, string tillNumber, string stornoType, string stornoDocumentNumber, string stornoDateTime, string stornoFMNumber, string invoice, string invoiceNumber) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(stornoUNP);
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(stornoType);
            inputString.Append(",");
            inputString.Append(stornoDocumentNumber);
            inputString.Append(",");
            inputString.Append(stornoDateTime);
            inputString.Append(",");
            inputString.Append(stornoFMNumber);
            inputString.Append(",");
            inputString.Append(invoice);
            inputString.Append(",");
            inputString.Append(invoiceNumber);
            inputString.Append(",");

            string r = CustomCommand(46 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["allReceiptCount"] = split[0];
            if(split.Length >= 2) 
                result["stornoReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 46 - please check fiscal device documentation.
        /// <summary>
        /// Open storno receipt
        /// </summary>
        /// <param name="operatorNumber">Operator code (1...30)</param>
        /// <param name="operatorPassword">Operator password up to 8 symbols</param>
        /// <param name="stornoUNP">UNP - 21 symbols: format - CCCCCCCC-CCCC-DDDDDDD: [0-9A-Za-z]{8}-[0-9A-Za-z]{4}-[0-9]{7} )</param>
        /// <param name="tillNumber">Till number (1-99999)</param>
        /// <param name="stornoType">Reason for storno: 0 - operator's error; 1 - refund.</param>
        /// <param name="stornoDocumentNumber">Number of the original document ( global 1...9999999)</param>
        /// <param name="stornoDateTime">Document date and time (format: ddMMYYHHmm or ddMMYYHHmmss)</param>
        /// <param name="stornoFMNumber">Fiscal memory number for the storno document (8 symbols)</param>
        /// <param name="invoice">If it is storno invoice - enter parameter "I"</param>
        /// <param name="invoiceNumber">Invoice number (1 - 9999999999).</param>
        /// <param name="stornoReason">Additional storno reason up to 42 symbols</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>All executed receipts count(fiscal and non-fiscal) since last Z report (4 bytes).</description>
        /// </item>
        /// <item>
        /// <term>stornoReceiptCount</term>
        /// <description>All executed storno receipts count since last Z report (4 bytes).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_StornoOpen_B06(string operatorNumber, string operatorPassword, string stornoUNP, string tillNumber, string stornoType, string stornoDocumentNumber, string stornoDateTime, string stornoFMNumber, string invoice, string invoiceNumber, string stornoReason) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(stornoUNP);
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(stornoType);
            inputString.Append(",");
            inputString.Append(stornoDocumentNumber);
            inputString.Append(",");
            inputString.Append(stornoDateTime);
            inputString.Append(",");
            inputString.Append(stornoFMNumber);
            inputString.Append(",");
            inputString.Append(invoice);
            inputString.Append(",");
            inputString.Append(invoiceNumber);
            inputString.Append(",");
            inputString.Append(stornoReason);

            string r = CustomCommand(46 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["allReceiptCount"] = split[0];
            if(split.Length >= 2) 
                result["stornoReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 48 - please check fiscal device documentation.
        /// <summary>
        /// Open fiscal receipt
        /// </summary>
        /// <param name="operatorNumber">Operator code (1...30)</param>
        /// <param name="operatorPassword">Operator password up to 8 symbols</param>
        /// <param name="tillNumber">Till number (1-99999)</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>All executed receipts count(fiscal and non-fiscal) since last Z report (4 bytes).</description>
        /// </item>
        /// <item>
        /// <term>stornoReceiptCount</term>
        /// <description>All executed storno receipts count since last Z report (4 bytes).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_FiscalOpen_B01(string operatorNumber, string operatorPassword, string tillNumber) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(tillNumber);

            string r = CustomCommand(48 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["allReceiptCount"] = split[0];
            if(split.Length >= 2) 
                result["fiscalReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 48 - please check fiscal device documentation.
        /// <summary>
        /// Open fiscal receipt with UNP
        /// </summary>
        /// <param name="operatorNumber">Operator code (1...30)</param>
        /// <param name="operatorPassword">Operator password up to 8 symbols</param>
        /// <param name="uNP">UNP - 21 symbols: format - CCCCCCCC-CCCC-DDDDDDD: [0-9A-Za-z]{8}-[0-9A-Za-z]{4}-[0-9]{7} )</param>
        /// <param name="tillNumber">Till number (1-99999)</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>All executed receipts count(fiscal and non-fiscal) since last Z report (4 bytes).</description>
        /// </item>
        /// <item>
        /// <term>stornoReceiptCount</term>
        /// <description>All executed storno receipts count since last Z report (4 bytes).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_FiscalOpen_B03(string operatorNumber, string operatorPassword, string uNP, string tillNumber) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(uNP);
            inputString.Append(",");
            inputString.Append(tillNumber);

            string r = CustomCommand(48 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["allReceiptCount"] = split[0];
            if(split.Length >= 2) 
                result["fiscalReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 48 - please check fiscal device documentation.
        /// <summary>
        /// Open fiscal receipt with UNP
        /// </summary>
        /// <param name="operatorNumber">Operator code (1...30)</param>
        /// <param name="operatorPassword">Operator password up to 8 symbols</param>
        /// <param name="uNP">UNP - 21 symbols: format - CCCCCCCC-CCCC-DDDDDDD: [0-9A-Za-z]{8}-[0-9A-Za-z]{4}-[0-9]{7} )</param>
        /// <param name="tillNumber">Till number (1-99999)</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>All executed receipts count(fiscal and non-fiscal) since last Z report (4 bytes).</description>
        /// </item>
        /// <item>
        /// <term>stornoReceiptCount</term>
        /// <description>All executed storno receipts count since last Z report (4 bytes).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Fiscal_Open(string operatorNumber, string operatorPassword, string uNP, string tillNumber) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(uNP);
            inputString.Append(",");
            inputString.Append(tillNumber);

            string r = CustomCommand(48 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["allReceiptCount"] = split[0];
            if(split.Length >= 2) 
                result["fiscalReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 56 - please check fiscal device documentation.
        /// <summary>
        /// Close fiscal receipt
        /// </summary>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>All executed receipts count(fiscal and non-fiscal) since last Z report (4 bytes).</description>
        /// </item>
        /// <item>
        /// <term>stornoReceiptCount</term>
        /// <description>All executed storno receipts count since last Z report (4 bytes).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Fiscal_Close() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(56 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["allReceiptCount"] = split[0];
            if(split.Length >= 2) 
                result["fiscalReceiptCount"] = split[1];
            return result;
        }

        public void open_FiscalReceipt(string operatorNumber, string operatorPassword, string uNP, string tillNumber, string invoice)
        {
            
            if (Y(uNP) && Y(invoice)) receipt_Invoice_Open(operatorNumber, operatorPassword, uNP, tillNumber, invoice);
            if (N(uNP) && Y(invoice)) receipt_FiscalOpen_B02(operatorNumber, operatorPassword, tillNumber, invoice);
            if (Y(uNP) && N(invoice)) receipt_Fiscal_Open(operatorNumber, operatorPassword, uNP, tillNumber);
            if (N(uNP) && N(invoice)) receipt_FiscalOpen_B01(operatorNumber, operatorPassword, tillNumber);
        }

        // Command number(Dec): 48 - please check fiscal device documentation.
        /// <summary>
        /// Open Invoice
        /// </summary>
        /// <param name="operatorNumber">Operator code (1...30)</param>
        /// <param name="operatorPassword">Operator password up to 8 symbols</param>
        /// <param name="tillNumber">Till number (1-99999)</param>
        /// <param name="invoice">parameter "I" - for Invoice</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>All executed receipts count(fiscal and non-fiscal) since last Z report (4 bytes).</description>
        /// </item>
        /// <item>
        /// <term>stornoReceiptCount</term>
        /// <description>All executed storno receipts count since last Z report (4 bytes).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_FiscalOpen_B02(string operatorNumber, string operatorPassword, string tillNumber, string invoice) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(invoice);

            string r = CustomCommand(48 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["allReceiptCount"] = split[0];
            if(split.Length >= 2) 
                result["fiscalReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 48 - please check fiscal device documentation.
        /// <summary>
        /// Open invoice with UNP
        /// </summary>
        /// <param name="operatorNumber">Operator code (1...30)</param>
        /// <param name="operatorPassword">Operator password up to 8 symbols</param>
        /// <param name="uNP">UNP - 21 symbols: format - CCCCCCCC-CCCC-DDDDDDD: [0-9A-Za-z]{8}-[0-9A-Za-z]{4}-[0-9]{7} )</param>
        /// <param name="tillNumber">Till number (1-99999)</param>
        /// <param name="invoice">parameter "I" - for Invoice</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>All executed receipts count(fiscal and non-fiscal) since last Z report (4 bytes).</description>
        /// </item>
        /// <item>
        /// <term>stornoReceiptCount</term>
        /// <description>All executed storno receipts count since last Z report (4 bytes).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_FiscalOpen_B04(string operatorNumber, string operatorPassword, string uNP, string tillNumber, string invoice) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(uNP);
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(invoice);

            string r = CustomCommand(48 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["allReceiptCount"] = split[0];
            if(split.Length >= 2) 
                result["fiscalReceiptCount"] = split[1];
            return result;
        }

        // Command number(Dec): 48 - please check fiscal device documentation.
        /// <summary>
        /// Open invoice with UNP
        /// </summary>
        /// <param name="operatorNumber">Operator code (1...30)</param>
        /// <param name="operatorPassword">Operator password up to 8 symbols</param>
        /// <param name="uNP">UNP - 21 symbols: format - CCCCCCCC-CCCC-DDDDDDD: [0-9A-Za-z]{8}-[0-9A-Za-z]{4}-[0-9]{7} )</param>
        /// <param name="tillNumber">Till number (1-99999)</param>
        /// <param name="invoice">parameter "I" - for Invoice</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>allReceiptCount</term>
        /// <description>All executed receipts count(fiscal and non-fiscal) since last Z report (4 bytes).</description>
        /// </item>
        /// <item>
        /// <term>stornoReceiptCount</term>
        /// <description>All executed storno receipts count since last Z report (4 bytes).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Invoice_Open(string operatorNumber, string operatorPassword, string uNP, string tillNumber, string invoice) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append(",");
            inputString.Append(operatorPassword);
            inputString.Append(",");
            inputString.Append(uNP);
            inputString.Append(",");
            inputString.Append(tillNumber);
            inputString.Append(",");
            inputString.Append(invoice);

            string r = CustomCommand(48 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["allReceiptCount"] = split[0];
            if(split.Length >= 2) 
                result["fiscalReceiptCount"] = split[1];
            return result;
        }

        public void receipt_PrintClientInfo(string eIK, string eIKType, string sellerName, string receiverName, string clientName, string taxNo, string address1, string address2)
        {
            
            if (Y(eIK) && Y(eIKType) && N(sellerName) && N(receiverName) && N(clientName) && N(taxNo) && N(address1) && N(address2)) receipt_PrintClientInfo_08(eIK, eIKType);
            if (Y(eIK) && Y(eIKType) && Y(sellerName) && N(receiverName) && N(clientName) && N(taxNo) && N(address1) && N(address2)) receipt_PrintClientInfo_09(eIK, eIKType, sellerName);
            if (Y(eIK) && Y(eIKType) && Y(sellerName) && Y(receiverName) && N(clientName) && N(taxNo) && N(address1) && N(address2)) receipt_PrintClientInfo_10(eIK, eIKType, sellerName,receiverName);
            if (Y(eIK) && Y(eIKType) && Y(sellerName) && Y(receiverName) && Y(clientName) && N(taxNo) && N(address1) && N(address2)) receipt_PrintClientInfo_11(eIK, eIKType, sellerName, receiverName,clientName);
            if (Y(eIK) && Y(eIKType) && Y(sellerName) && Y(receiverName) && Y(clientName) && Y(taxNo) && N(address1) && N(address2)) receipt_PrintClientInfo_12(eIK, eIKType, sellerName, receiverName, clientName,taxNo);
            if (Y(eIK) && Y(eIKType) && Y(sellerName) && Y(receiverName) && Y(clientName) && Y(taxNo) && Y(address1) && N(address2)) receipt_PrintClientInfo_13(eIK, eIKType, sellerName, receiverName, clientName, taxNo,address1);
            if (Y(eIK) && Y(eIKType) && Y(sellerName) && Y(receiverName) && Y(clientName) && Y(taxNo) && Y(address1) && Y(address2)) receipt_PrintClientInfo_14(eIK, eIKType, sellerName, receiverName, clientName, taxNo, address1,address2);
        }

        // Command number(Dec): 57 - please check fiscal device documentation.
        /// <summary>
        /// Enter and print basic invoice data
        /// </summary>
        /// <param name="eIK">EIK number (between 9 and 14 symbols)</param>
        /// <param name="eIKType">0-BULSTAT; 1-EGN; 2-LNCH; 3-service number</param>
        public void receipt_PrintClientInfo_08(string eIK, string eIKType) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(eIK);
            inputString.Append("\t");
            inputString.Append(eIKType);

            string r = CustomCommand(57 , inputString.ToString());
            CheckResult();
        }

        // Command number(Dec): 57 - please check fiscal device documentation.
        /// <summary>
        /// Enter and print invoice data with seller name
        /// </summary>
        /// <param name="eIK">EIK number (between 9 and 14 symbols)</param>
        /// <param name="eIKType">0-BULSTAT; 1-EGN; 2-LNCH; 3-service number</param>
        /// <param name="sellerName">Name of the seller; 36 symbols max</param>
        public void receipt_PrintClientInfo_09(string eIK, string eIKType, string sellerName) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(eIK);
            inputString.Append("\t");
            inputString.Append(eIKType);
            inputString.Append("\t");
            inputString.Append(sellerName);

            string r = CustomCommand(57 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 57 - please check fiscal device documentation.
        /// <summary>
        ///  Enter and print invoice data with seller and receiver
        /// </summary>
        /// <param name="eIK">EIK number (between 9 and 14 symbols)</param>
        /// <param name="eIKType">0-BULSTAT; 1-EGN; 2-LNCH; 3-service number</param>
        /// <param name="sellerName">Name of seller (up to 36 symbols)</param>
        /// <param name="receiverName">Name of receiver (up to 36 symbols)</param>
        public void receipt_PrintClientInfo_10(string eIK, string eIKType, string sellerName, string receiverName) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(eIK);
            inputString.Append("\t");
            inputString.Append(eIKType);
            inputString.Append("\t");
            inputString.Append(sellerName);
            inputString.Append("\t");
            inputString.Append(receiverName);

            string r = CustomCommand(57 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 57 - please check fiscal device documentation.
        /// <summary>
        ///  Enter and print invoice data with seller, receiver and client
        /// </summary>
        /// <param name="eIK">EIK number (between 9 and 14 symbols)</param>
        /// <param name="eIKType">0-BULSTAT; 1-EGN; 2-LNCH; 3-service number</param>
        /// <param name="sellerName">Name of seller (up to 36 symbols)</param>
        /// <param name="receiverName">Name of receiver (up to 36 symbols)</param>
        /// <param name="clientName">Name of the buyer (up to 36 symbols)</param>
        public void receipt_PrintClientInfo_11(string eIK, string eIKType, string sellerName, string receiverName, string clientName) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(eIK);
            inputString.Append("\t");
            inputString.Append(eIKType);
            inputString.Append("\t");
            inputString.Append(sellerName);
            inputString.Append("\t");
            inputString.Append(receiverName);
            inputString.Append("\t");
            inputString.Append(clientName);

            string r = CustomCommand(57 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 57 - please check fiscal device documentation.
        /// <summary>
        ///  Enter and print invoice data with seller, receiver, client and tax number
        /// </summary>
        /// <param name="eIK">EIK number (between 9 and 14 symbols)</param>
        /// <param name="eIKType">0-BULSTAT; 1-EGN; 2-LNCH; 3-service number</param>
        /// <param name="sellerName">Name of seller (up to 36 symbols)</param>
        /// <param name="receiverName">Name of receiver (up to 36 symbols)</param>
        /// <param name="clientName">Name of the buyer (up to 36 symbols)</param>
        /// <param name="taxNo">Tax number of the client. 10...14 symbols</param>
        public void receipt_PrintClientInfo_12(string eIK, string eIKType, string sellerName, string receiverName, string clientName, string taxNo) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(eIK);
            inputString.Append("\t");
            inputString.Append(eIKType);
            inputString.Append("\t");
            inputString.Append(sellerName);
            inputString.Append("\t");
            inputString.Append(receiverName);
            inputString.Append("\t");
            inputString.Append(clientName);
            inputString.Append("\t");
            inputString.Append(taxNo);

            string r = CustomCommand(57 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 57 - please check fiscal device documentation.
        /// <summary>
        ///  Enter and print invoice data with seller, receiver, client, tax number and address
        /// </summary>
        /// <param name="eIK">EIK number (between 9 and 14 symbols)</param>
        /// <param name="eIKType">0-BULSTAT; 1-EGN; 2-LNCH; 3-service number</param>
        /// <param name="sellerName">Name of seller (up to 36 symbols)</param>
        /// <param name="receiverName">Name of receiver (up to 36 symbols)</param>
        /// <param name="clientName">Name of the buyer (up to 36 symbols)</param>
        /// <param name="taxNo">Tax number of the client. 10...14 symbols</param>
        /// <param name="address1">Client's address</param>
        public void receipt_PrintClientInfo_13(string eIK, string eIKType, string sellerName, string receiverName, string clientName, string taxNo, string address1) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(eIK);
            inputString.Append("\t");
            inputString.Append(eIKType);
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

            string r = CustomCommand(57 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 57 - please check fiscal device documentation.
        /// <summary>
        ///  Enter and print invoice data with seller, receiver, client, tax number and address
        /// </summary>
        /// <param name="eIK">EIK number (between 9 and 14 symbols)</param>
        /// <param name="eIKType">0-BULSTAT; 1-EGN; 2-LNCH; 3-service number</param>
        /// <param name="sellerName">Name of seller (up to 36 symbols)</param>
        /// <param name="receiverName">Name of receiver (up to 36 symbols)</param>
        /// <param name="clientName">Name of the buyer (up to 36 symbols)</param>
        /// <param name="taxNo">Tax number of the client. 10...14 symbols</param>
        /// <param name="address1">Client's address</param>
        /// <param name="address2">Client's address second line</param>
        public void receipt_PrintClientInfo_14(string eIK, string eIKType, string sellerName, string receiverName, string clientName, string taxNo, string address1, string address2) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(eIK);
            inputString.Append("\t");
            inputString.Append(eIKType);
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

            string r = CustomCommand(57 , inputString.ToString());
            CheckResult();

        }

        public void execute_Sale(string textRow1, string textRow2, string department, string taxGroup, string singlePrice, string quantity, string measure, string percent, string abs)
        {
           
            if (Y(taxGroup))
            {
                if (Y(textRow1) && Y(textRow2) && Y(singlePrice) && Y(quantity) && N(department) && N(measure) && N(percent) && N(abs)) receipt_Sale(textRow1, textRow2, taxGroup, singlePrice, quantity);
                if (Y(textRow1) && Y(singlePrice) && Y(quantity) && N(textRow2) && N(department) && N(measure) && N(percent) && N(abs)) receipt_Sale_TextRow1(textRow1, taxGroup, singlePrice, quantity);
                if (Y(textRow2) && Y(singlePrice) && Y(quantity) && N(textRow1) && N(department) && N(measure) && N(percent) && N(abs)) receipt_Sale_TextRow2(textRow2, taxGroup, singlePrice, quantity);
                if (Y(textRow1) && Y(singlePrice) && N(quantity) && N(textRow2) && N(department) && N(measure) && N(percent) && N(abs)) receipt_Sale_Minimum(taxGroup, singlePrice);
                if (Y(textRow1) && Y(textRow2) && Y(singlePrice) && Y(quantity) && Y(percent) && N(measure) && N(department) && N(abs)) receipt_Sale_CByPercent(textRow1, textRow2, taxGroup, singlePrice, quantity, percent);
                if (Y(textRow1) && Y(singlePrice) && Y(quantity) && Y(percent) && N(textRow2) && N(measure) && N(department) && N(abs)) receipt_Sale_TextRow1CByPercent(textRow1, taxGroup, singlePrice, quantity, percent);
                if (Y(textRow2) && Y(singlePrice) && Y(quantity) && Y(percent) && N(measure) && N(textRow1) && N(department) && N(abs)) receipt_Sale_TextRow2CByPercent(textRow2, taxGroup, singlePrice, quantity, percent);
                if (Y(textRow1) && Y(textRow2) && Y(singlePrice) && Y(quantity) && Y(abs) && N(measure) && N(percent) && N(department)) receipt_Sale_CBySum(textRow1, textRow2, taxGroup, singlePrice, quantity, abs);
                if (Y(textRow1) && N(textRow2) && Y(singlePrice) && Y(quantity) && Y(abs) && N(measure) && N(percent) && N(department)) receipt_Sale_TextRow1CBySum(textRow1, taxGroup, singlePrice, quantity, abs);
                if (N(textRow1) && Y(textRow2) && Y(singlePrice) && Y(quantity) && Y(abs) && N(measure) && N(percent) && N(department)) receipt_Sale_TextRow2CBySum(textRow2, taxGroup, singlePrice, quantity, abs);
                if (N(textRow1) && N(textRow2) && Y(singlePrice) && Y(quantity) && Y(abs) && N(measure) && N(percent) && N(department)) receipt_Sale_CBySumWText(taxGroup, singlePrice, quantity, abs);
                
            }
            if (Y(department))
            {
                if (Y(textRow1) && Y(textRow2) && Y(singlePrice) && Y(quantity) && N(taxGroup) && N(abs) && N(measure) && N(percent)) receipt_DSale(textRow1, textRow2, department, singlePrice, quantity);
                if (Y(textRow1) && N(textRow2) && Y(singlePrice) && Y(quantity) && N(taxGroup) && N(abs) && N(measure) && N(percent)) receipt_DSale_TextRow1(textRow1, department, singlePrice, quantity);
                if (Y(textRow2) && N(textRow1) && Y(singlePrice) && Y(quantity) && N(taxGroup) && N(abs) && N(measure) && N(percent)) receipt_DSale_TextRow2(textRow2, department, singlePrice, quantity);
                if (N(textRow2) && N(textRow1) && Y(singlePrice) && N(quantity) && N(taxGroup) && N(abs) && N(measure) && N(percent)) receipt_DSale_Minimum(department, singlePrice);
                if (Y(textRow2) && Y(textRow1) && Y(singlePrice) && Y(quantity) && Y(percent) && N(taxGroup) && N(abs) && Y(measure)) receipt_DSale_CByPercent(textRow1, textRow2, department, singlePrice, quantity, percent);
                if (Y(textRow1) && N(textRow2) && Y(singlePrice) && Y(quantity) && Y(percent) && N(taxGroup) && N(abs) && N(measure)) receipt_DSale_TextRow1CByPercent(textRow1, department, singlePrice, quantity, percent);
                if (N(textRow1) && Y(textRow2) && Y(singlePrice) && Y(quantity) && Y(percent) && N(taxGroup) && N(abs) && N(measure)) receipt_DSale_TextRow2CByPercent(textRow2, department, singlePrice, quantity, percent);
                if (N(textRow1) && N(textRow2) && Y(singlePrice) && Y(quantity) && Y(percent) && N(taxGroup) && N(abs) && N(measure)) receipt_DSale_CByPercentWText(department, singlePrice, quantity, percent);
                if (Y(textRow1) && Y(textRow2) && Y(singlePrice) && Y(quantity) && N(percent) && N(taxGroup) && Y(abs) && N(measure)) receipt_DSale_CBySum(textRow1, textRow2, department, singlePrice, quantity, abs);
                if (Y(textRow1) && N(textRow2) && Y(singlePrice) && Y(quantity) && N(percent) && N(taxGroup) && Y(abs) && N(measure)) receipt_DSale_TextRow1CBySum(textRow1, department, singlePrice, quantity, abs);
                if (N(textRow1) && Y(textRow2) && Y(singlePrice) && Y(quantity) && N(percent) && N(taxGroup) && Y(abs) && N(measure)) receipt_DSale_TextRow2CBySum(textRow2, department, singlePrice, quantity, abs);
                if (N(textRow1) && N(textRow2) && Y(singlePrice) && Y(quantity) && N(percent) && N(taxGroup) && Y(abs) && N(measure)) receipt_DSale_CBySumWText(department, singlePrice, quantity, abs);
            }
            CheckResult();
        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Registration of sale
        /// </summary>
        /// <param name="textRow1">Text up to 22 bytes</param>
        /// <param name="textRow2">Text up to 22 bytes</param>
        /// <param name="taxGroup">Tax group (А,Б,В ...)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        /// <param name="quantity">Quantity (8 digits,3 after decimal point)</param>
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

            string r = CustomCommand(49 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Registration of a sale
        /// </summary>
        /// <param name="textRow1">Text up to 22 bytes</param>
        /// <param name="taxGroup">Tax group (А,Б,В ...)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        /// <param name="quantity">Quantity (8 digits,3 after decimal point)</param>
        public void receipt_Sale_TextRow1(string textRow1, string taxGroup, string singlePrice, string quantity) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);

            string r = CustomCommand(49 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        ///  Registration of a sale
        /// </summary>
        /// <param name="textRow2">Text up to 22 bytes</param>
        /// <param name="taxGroup">Tax group (А,Б,В ...)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        /// <param name="quantity">Quantity (8 digits,3 after decimal point)</param>
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

            string r = CustomCommand(49 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        ///  Registration of a sale
        /// </summary>
        /// <param name="taxGroup">Tax group (А,Б,В ...)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        public void receipt_Sale_Minimum(string taxGroup, string singlePrice) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);

            string r = CustomCommand(49 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Registration of a sale
        /// </summary>
        /// <param name="textRow1">Text up to 22 bytes</param>
        /// <param name="textRow2">Text up to 22 bytes</param>
        /// <param name="taxGroup">Tax group (А,Б,В ...)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        /// <param name="quantity">Quantity (8 digits,3 after decimal point)</param>
        /// <param name="percent">Discount/ surcharge percent(depends on the sign)</param>
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

            string r = CustomCommand(49 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Registration of a sale
        /// </summary>
        /// <param name="textRow1">Text up to 22 bytes</param>
        /// <param name="taxGroup">Tax group (А,Б,В ...)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        /// <param name="quantity">Quantity (8 digits,3 after decimal point)</param>
        /// <param name="percent">Discount/ surcharge percent (depends on the sign)</param>
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

            string r = CustomCommand(49 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        ///  Registration of a sale
        /// </summary>
        /// <param name="textRow2">Text up to 22 bytes</param>
        /// <param name="taxGroup">Tax group (А,Б,В ...)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        /// <param name="quantity">Quantity (8 digits,3 after decimal point)</param>
        /// <param name="percent">Discount/ surcharge by percent (depends on the sign)</param>
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

            string r = CustomCommand(49 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        ///  Registration of a sale
        /// </summary>
        /// <param name="taxGroup">Tax group (А,Б,В ...)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        /// <param name="percent">Discount/ surcharge (depends on the sign)</param>
        public void receipt_Sale_CByPercentWText(string taxGroup, string singlePrice, string percent) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(49 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Registration of a sale
        /// </summary>
        /// <param name="textRow1">Text up to 22 bytes</param>
        /// <param name="textRow2">Text up to 22 bytes</param>
        /// <param name="taxGroup">Tax group (А,Б,В ...)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        /// <param name="quantity">Quantity (8 digits,3 after decimal point)</param>
        /// <param name="abs">Discount/ surcharge sum (depends on the sign)</param>
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

            string r = CustomCommand(49 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Registration of a sale with sum discount/surcharge
        /// </summary>
        /// <param name="textRow1">Text up to 22 bytes</param>
        /// <param name="taxGroup">Tax group (А,Б,В ...)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        /// <param name="quantity">Quantity (8 digits,3 after decimal point)</param>
        /// <param name="abs">Discount/ surcharge sum (depends on the sign)</param>
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

            string r = CustomCommand(49 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Registration of a sale with sum discount/surcharge
        /// </summary>
        /// <param name="textRow2">Text up to 22 bytes</param>
        /// <param name="taxGroup">Tax group (А,Б,В ...)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        /// <param name="quantity">Quantity (8 digits,3 after decimal point)</param>
        /// <param name="abs">Discount/ surcharge sum (depends on the sign)</param>
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

            string r = CustomCommand(49 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Registration of a sale with sum discount/surcharge
        /// </summary>
        /// <param name="taxGroup">Tax group (А,Б,В ...)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        /// <param name="quantity">Quantity (8 digits,3 after decimal point)</param>
        /// <param name="abs">Discount/ surcharge sum (depends on the sign)</param>
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

            string r = CustomCommand(49 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Registration of a sale with department
        /// </summary>
        /// <param name="textRow1">Text up to 22 bytes</param>
        /// <param name="textRow2">Text up to 22 bytes</param>
        /// <param name="department">Department number (1 ...9)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        /// <param name="quantity">Quantity (8 digits,3 after decimal point)</param>
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

            string r = CustomCommand(49 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Registration of a sale with department
        /// </summary>
        /// <param name="textRow1">Text up to 22 bytes</param>
        /// <param name="department">Department number (1 ...9)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        /// <param name="quantity">Quantity (8 digits,3 after decimal point)</param>
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

            string r = CustomCommand(49 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Registration of a sale with department
        /// </summary>
        /// <param name="textRow2">Text up to 22 bytes</param>
        /// <param name="department">Department number (1 ...9)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        /// <param name="quantity">Quantity (8 digits,3 after decimal point)</param>
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

            string r = CustomCommand(49 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Registration of a sale with department minimum
        /// </summary>
        /// <param name="department">Department number (1 ...9)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        public void receipt_DSale_Minimum(string department, string singlePrice) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);

            string r = CustomCommand(49 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Registration of a sale with department and discount/surcharge by percent
        /// </summary>
        /// <param name="textRow1">Text up to 22 bytes</param>
        /// <param name="textRow2">Text up to 22 bytes</param>
        /// <param name="department">Department number (1 ...9)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        /// <param name="quantity">Quantity (8 digits,3 after decimal point)</param>
        /// <param name="percent">Discount/ surcharge by percent (depends on the sign)</param>
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

            string r = CustomCommand(49 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Registration of a sale with department and discount/surcharge by percent
        /// </summary>
        /// <param name="textRow1">Text up to 22 bytes</param>
        /// <param name="department">Department number (1 ...9)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        /// <param name="quantity">Quantity (8 digits,3 after decimal point)</param>
        /// <param name="percent">Discount/ surcharge by percent (depends on the sign)</param>
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

            string r = CustomCommand(49 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Registration of a sale with department and discount/surcharge by percent
        /// </summary>
        /// <param name="textRow2">Text up to 22 bytes</param>
        /// <param name="department">Department number (1 ...9)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        /// <param name="quantity">Quantity (8 digits,3 after decimal point)</param>
        /// <param name="percent">Discount/ surcharge by percent (depends on the sign)</param>
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

            string r = CustomCommand(49 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Registration of a sale with department and discount/surcharge by percent
        /// </summary>
        /// <param name="department">Department number (1 ...9)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        /// <param name="quantity">Quantity (8 digits,3 after decimal point)</param>
        /// <param name="percent">Discount/ surcharge by percent (depends on the sign)</param>
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

            string r = CustomCommand(49 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Registration of a sale with department and discount/surcharge by sum
        /// </summary>
        /// <param name="textRow1">Text up to 22 bytes</param>
        /// <param name="textRow2">Text up to 22 bytes</param>
        /// <param name="department">Department number (1 ...9)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        /// <param name="quantity">Quantity (8 digits,3 after decimal point)</param>
        /// <param name="abs">Discount/ surcharge sum (depends on the sign)</param>
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

            string r = CustomCommand(49 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Registration of a sale with department and discount/surcharge by sum
        /// </summary>
        /// <param name="textRow1">Text up to 22 bytes</param>
        /// <param name="department">Department number (1 ...9)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        /// <param name="quantity">Quantity (8 digits,3 after decimal point)</param>
        /// <param name="abs">Discount/ surcharge sum (depends on the sign)</param>
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

            string r = CustomCommand(49 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Registration of a sale with department and discount/surcharge by sum
        /// </summary>
        /// <param name="textRow2">Text up to 22 bytes</param>
        /// <param name="department">Department number (1 ...9)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        /// <param name="quantity">Quantity (8 digits,3 after decimal point)</param>
        /// <param name="abs">Discount/ surcharge sum (depends on the sign)</param>
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

            string r = CustomCommand(49 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Registration of a sale with department and discount/surcharge by sum
        /// </summary>
        /// <param name="department">Department number (1 ...9)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        /// <param name="quantity">Quantity (8 digits,3 after decimal point)</param>
        /// <param name="abs">Discount/ surcharge sum (depends on the sign)</param>
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

            string r = CustomCommand(49 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 52 - please check fiscal device documentation.
        /// <summary>
        /// Registration of a sale and display
        /// </summary>
        /// <param name="textRow">Text up to 22 bytes</param>
        /// <param name="taxGroup">Tax group (A,Б,В ...)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        /// <param name="quantity">Quantity (8 digits,3 after decimal point)</param>
        public void receipt_DisplaySale(string textRow, string taxGroup, string singlePrice, string quantity) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);
            inputString.Append("*");
            inputString.Append(quantity);

            string r = CustomCommand(52 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 52 - please check fiscal device documentation.
        /// <summary>
        /// Registration of a sale and display with percent
        /// </summary>
        /// <param name="textRow">Text up to 22 bytes</param>
        /// <param name="taxGroup">Tax group (A,Б,В ...)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        /// <param name="quantity">Quantity (8 digits,3 after decimal point)</param>
        /// <param name="percent">Discount/ surcharge by percent (depends on the sign)</param>
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

            string r = CustomCommand(52 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 52 - please check fiscal device documentation.
        /// <summary>
        /// Registration of a sale and display with sum
        /// </summary>
        /// <param name="textRow">Text up to 22 bytes</param>
        /// <param name="taxGroup">Tax group (A,Б,В ...)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        /// <param name="quantity">Quantity (8 digits,3 after decimal point)</param>
        /// <param name="abs">Discount/ surcharge sum (depends on the sign)</param>
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

            string r = CustomCommand(52 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 52 - please check fiscal device documentation.
        /// <summary>
        /// Registration of a sale and display
        /// </summary>
        /// <param name="textRow">Text up to 22 bytes</param>
        /// <param name="taxGroup">Tax group (A,Б,В ...)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        public void receipt_DisplaySale_Minimum(string textRow, string taxGroup, string singlePrice) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append(singlePrice);

            string r = CustomCommand(52 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 52 - please check fiscal device documentation.
        /// <summary>
        /// Registration of a sale and display
        /// </summary>
        /// <param name="textRow">Text up to 22 bytes</param>
        /// <param name="department">Department (1...9)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        /// <param name="quantity">Quantity (8 digits,3 after decimal point)</param>
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

            string r = CustomCommand(52 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 52 - please check fiscal device documentation.
        /// <summary>
        /// Registration of a sale and display by department and discount/surcharge by percent
        /// </summary>
        /// <param name="textRow">Text up to 22 bytes</param>
        /// <param name="department">Department (1...9)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        /// <param name="quantity">Quantity (8 digits,3 after decimal point)</param>
        /// <param name="percent">Discount/ surcharge by percent (depends on the sign)</param>
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

            string r = CustomCommand(52 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 52 - please check fiscal device documentation.
        /// <summary>
        /// Registration of a sale and display by department and discount/surcharge by percent
        /// </summary>
        /// <param name="textRow">Text up to 22 bytes</param>
        /// <param name="department">Department (1...9)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        /// <param name="quantity">Quantity (8 digits,3 after decimal point)</param>
        /// <param name="abs">Discount/ surcharge sum (depends on the sign)</param>
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

            string r = CustomCommand(52 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 52 - please check fiscal device documentation.
        /// <summary>
        /// Registration of a sale and display by department 
        /// </summary>
        /// <param name="textRow">Text up to 22 bytes</param>
        /// <param name="department">Department (1...9)</param>
        /// <param name="singlePrice">Single price up to 8 digits</param>
        public void receipt_DisplayDSale_Minimum(string textRow, string department, string singlePrice) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(singlePrice);

            string r = CustomCommand(52 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item
        /// </summary>
        /// <param name="targetPLU">The code of the item. From 1 to 999999999</param>
        /// <param name="quantity">Quantity of the product ( default: 1.000 ); Format: 3 decimals; up to *999999.999</param>
        public void receipt_PLU_Sale(string targetPLU, string quantity) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(targetPLU);
            inputString.Append("*");
            inputString.Append(quantity);

            string r = CustomCommand(58 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item with discount/surcharge in percents
        /// </summary>
        /// <param name="targetPLU">The code of the item. From 1 to 999999999</param>
        /// <param name="quantity">Quantity of the product ( default: 1.000 ); Format: 3 decimals; up to *999999.999</param>
        /// <param name="percent">Discount/ surcharge by percent (depends on the sign)</param>
        public void receipt_PLUSale_CByPercent(string targetPLU, string quantity, string percent) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(targetPLU);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(58 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item with discount/surcharge in sum
        /// </summary>
        /// <param name="targetPLU">The code of the item. From 1 to 999999999</param>
        /// <param name="quantity">Quantity of the product ( default: 1.000 ); Format: 3 decimals; up to *999999.999</param>
        /// <param name="abs">Discount/ surcharge sum (depends on the sign)</param>
        public void receipt_PLUSale_CBySum(string targetPLU, string quantity, string abs) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(targetPLU);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(";");
            inputString.Append(abs);

            string r = CustomCommand(58 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item 
        /// </summary>
        /// <param name="targetPLU">The code of the item. From 1 to 999999999</param>
        /// <param name="department">Department (1...9)</param>
        /// <param name="quantity">Quantity of the product ( default: 1.000 ); Format: 3 decimals; up to *999999.999</param>
        public void receipt_PLUDep_Sale(string targetPLU, string department, string quantity) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(targetPLU);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append("*");
            inputString.Append(quantity);

            string r = CustomCommand(58 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item 
        /// </summary>
        /// <param name="targetPLU">The code of the item. From 1 to 999999999</param>
        /// <param name="department">Department (1...9)</param>
        /// <param name="quantity">Quantity of the product ( default: 1.000 ); Format: 3 decimals; up to *999999.999</param>
        /// <param name="percent">Discount/ surcharge by percent (depends on the sign)</param>
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

            string r = CustomCommand(58 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item with discount/surcharge by sum
        /// </summary>
        /// <param name="targetPLU">The code of the item. From 1 to 999999999</param>
        /// <param name="department">Department (1...9)</param>
        /// <param name="quantity">Quantity of the product ( default: 1.000 ); Format: 3 decimals; up to *999999.999</param>
        /// <param name="abs">Discount/ surcharge sum (depends on the sign)</param>
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

            string r = CustomCommand(58 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item 
        /// </summary>
        /// <param name="dChar">parameter "D" to dispaly the sell on external display </param>
        /// <param name="targetPLU">The code of the item. From 1 to 999999999</param>
        /// <param name="quantity">Quantity of the product ( default: 1.000 ); Format: 3 decimals; up to *999999.999</param>
        public void receipt_DisplayPLUSale(string dChar, string targetPLU, string quantity) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(dChar);
            inputString.Append(targetPLU);
            inputString.Append("*");
            inputString.Append(quantity);

            string r = CustomCommand(58 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item with discount/surcharge by percents.
        /// </summary>
        /// <param name="dChar">parameter "D" to dispaly the sell on external display</param>
        /// <param name="targetPLU">The code of the item. From 1 to 999999999</param>
        /// <param name="quantity">Quantity of the product ( default: 1.000 ); Format: 3 decimals; up to *999999.999</param>
        /// <param name="percent">Discount/ surcharge by percent (depends on the sign)</param>
        public void receipt_DisplayPLUSale_CByPercent(string dChar, string targetPLU, string quantity, string percent) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(dChar);
            inputString.Append(targetPLU);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(",");
            inputString.Append(percent);

            string r = CustomCommand(58 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item with discount/surcharge by sum.
        /// </summary>
        /// <param name="dChar">parameter "D" to dispaly the sell on external display</param>
        /// <param name="targetPLU">The code of the item. From 1 to 999999999</param>
        /// <param name="quantity">Quantity of the product ( default: 1.000 ); Format: 3 decimals; up to *999999.999</param>
        /// <param name="abs">Discount/ surcharge sum (depends on the sign)</param>
        public void receipt_DisplayPLUSale_CBySum(string dChar, string targetPLU, string quantity, string abs) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(dChar);
            inputString.Append(targetPLU);
            inputString.Append("*");
            inputString.Append(quantity);
            inputString.Append(";");
            inputString.Append(abs);

            string r = CustomCommand(58 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item
        /// </summary>
        /// <param name="dChar">parameter "D" to dispaly the sell on external display</param>
        /// <param name="targetPLU">The code of the item. From 1 to 999999999</param>
        /// <param name="department">Department (1...9)</param>
        /// <param name="quantity">Quantity of the product ( default: 1.000 ); Format: 3 decimals; up to *999999.999</param>
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

            string r = CustomCommand(58 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item
        /// </summary>
        /// <param name="dChar">parameter "D" to dispaly the sell on external display</param>
        /// <param name="targetPLU">The code of the item. From 1 to 999999999</param>
        /// <param name="department">Department (1...9)</param>
        /// <param name="quantity">Quantity of the product ( default: 1.000 ); Format: 3 decimals; up to *999999.999</param>
        /// <param name="percent">Discount/ surcharge by percent (depends on the sign)</param>
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

            string r = CustomCommand(58 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item
        /// </summary>
        /// <param name="dChar">parameter "D" to dispaly the sell on external display</param>
        /// <param name="targetPLU">The code of the item. From 1 to 999999999</param>
        /// <param name="department">Department (1...9)</param>
        /// <param name="quantity">Quantity of the product ( default: 1.000 ); Format: 3 decimals; up to *999999.999</param>
        /// <param name="abs">Discount/ surcharge sum (depends on the sign)</param>
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

            string r = CustomCommand(58 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 51 - please check fiscal device documentation.
        /// <summary>
        /// Subtotal
        /// </summary>
        /// <param name="toPrint">1 - Subtotal will be printed; 0 - subtotal will not be printed</param>
        /// <param name="toDisplay">1 - Subtotal will be shown on display; 0 - subtotal will not be shown on display</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>subtotal</term>
        /// <description>Subtotal for current receipt (up to 10 bytes).</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupA</term>
        /// <description>Sum for tax group A (up to 10 bytes).</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupB</term>
        /// <description>Sum for tax group B (up to 10 bytes).</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupC</term>
        /// <description>Sum for tax group C (up to 10 bytes).</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupD</term>
        /// <description>Sum for tax group D (up to 10 bytes).</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupE</term>
        /// <description>Sum for tax group E (up to 10 bytes).</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupF</term>
        /// <description>Sum for tax group F (up to 10 bytes).</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupG</term>
        /// <description>Sum for tax group G (up to 10 bytes).</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupH</term>
        /// <description>Sum for tax group H (up to 10 bytes).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Subtotal(string toPrint, string toDisplay) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(toPrint);
            inputString.Append(toDisplay);

            string r = CustomCommand(51 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["subtotal"] = split[0];
            if(split.Length >= 2) 
                result["sumTaxGroupA"] = split[1];
            if(split.Length >= 3) 
                result["sumTaxGroupB"] = split[2];
            if(split.Length >= 4) 
                result["sumTaxGroupC"] = split[3];
            if(split.Length >= 5) 
                result["sumTaxGroupD"] = split[4];
            if(split.Length >= 6) 
                result["sumTaxGroupE"] = split[5];
            if(split.Length >= 7) 
                result["sumTaxGroupF"] = split[6];
            if(split.Length >= 8) 
                result["sumTaxGroupG"] = split[7];
            if(split.Length >= 9) 
                result["sumTaxGroupH"] = split[8];
            return result;
        }

        // Command number(Dec): 51 - please check fiscal device documentation.
        /// <summary>
        /// Subtotal with discount/surchage by percentage
        /// </summary>
        /// <param name="toPrint">1 - Subtotal will be printed; 0 - subtotal will not be printed</param>
        /// <param name="toDisplay">1 - Subtotal will be shown on display; 0 - subtotal will not be shown on display</param>
        /// <param name="percent">Discount/ surcharge by percent (depends on the sign)</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>subtotal</term>
        /// <description>Subtotal for current receipt (up to 10 bytes).</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupA</term>
        /// <description>Sum for tax group A (up to 10 bytes).</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupB</term>
        /// <description>Sum for tax group B (up to 10 bytes).</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupC</term>
        /// <description>Sum for tax group C (up to 10 bytes).</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupD</term>
        /// <description>Sum for tax group D (up to 10 bytes).</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupE</term>
        /// <description>Sum for tax group E (up to 10 bytes).</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupF</term>
        /// <description>Sum for tax group F (up to 10 bytes).</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupG</term>
        /// <description>Sum for tax group G (up to 10 bytes).</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupH</term>
        /// <description>Sum for tax group H (up to 10 bytes).</description>
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

            string r = CustomCommand(51 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["subtotal"] = split[0];
            if(split.Length >= 2) 
                result["sumTaxGroupA"] = split[1];
            if(split.Length >= 3) 
                result["sumTaxGroupB"] = split[2];
            if(split.Length >= 4) 
                result["sumTaxGroupC"] = split[3];
            if(split.Length >= 5) 
                result["sumTaxGroupD"] = split[4];
            if(split.Length >= 6) 
                result["sumTaxGroupE"] = split[5];
            if(split.Length >= 7) 
                result["sumTaxGroupF"] = split[6];
            if(split.Length >= 8) 
                result["sumTaxGroupG"] = split[7];
            if(split.Length >= 9) 
                result["sumTaxGroupH"] = split[8];
            return result;
        }

        // Command number(Dec): 51 - please check fiscal device documentation.
        /// <summary>
        /// Subtotal with discount/surchage by sum
        /// </summary>
        /// <param name="toPrint">1 - Subtotal will be printed; 0 - subtotal will not be printed</param>
        /// <param name="toDisplay">1 - Subtotal will be shown on display; 0 - subtotal will not be shown on display</param>
        /// <param name="abs">Discount/ surcharge sum (depends on the sign)</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>subtotal</term>
        /// <description>Subtotal for current receipt (up to 10 bytes).</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupA</term>
        /// <description>Sum for tax group A (up to 10 bytes).</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupB</term>
        /// <description>Sum for tax group B (up to 10 bytes).</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupC</term>
        /// <description>Sum for tax group C (up to 10 bytes).</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupD</term>
        /// <description>Sum for tax group D (up to 10 bytes).</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupE</term>
        /// <description>Sum for tax group E (up to 10 bytes).</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupF</term>
        /// <description>Sum for tax group F (up to 10 bytes).</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupG</term>
        /// <description>Sum for tax group G (up to 10 bytes).</description>
        /// </item>
        /// <item>
        /// <term>sumTaxGroupH</term>
        /// <description>Sum for tax group H (up to 10 bytes).</description>
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

            string r = CustomCommand(51 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["subtotal"] = split[0];
            if(split.Length >= 2) 
                result["sumTaxGroupA"] = split[1];
            if(split.Length >= 3) 
                result["sumTaxGroupB"] = split[2];
            if(split.Length >= 4) 
                result["sumTaxGroupC"] = split[3];
            if(split.Length >= 5) 
                result["sumTaxGroupD"] = split[4];
            if(split.Length >= 6) 
                result["sumTaxGroupE"] = split[5];
            if(split.Length >= 7) 
                result["sumTaxGroupF"] = split[6];
            if(split.Length >= 8) 
                result["sumTaxGroupG"] = split[7];
            if(split.Length >= 9) 
                result["sumTaxGroupH"] = split[8];
            return result;
        }

        public void execute_Total(string textRow1,string textRow2,string paidMode, string inputAmount)
        {
            //bool Y(string param) { return !string.IsNullOrEmpty(param); }  // defined as lambda function above
            //bool N(string param) { return string.IsNullOrEmpty(param); }

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
        /// <param name="textRow1">Text up to 42 bytes</param>
        /// <param name="textRow2">Text up to 42 bytes</param>
        /// <param name="paidMode">‘P’ - Cash payment; ‘N’ - Credit payment; ‘C’ - Debit card payment; ‘D’ - NZOK payment;
        /// ‘I’ - Check payment; ‘J’ - Coupon payment</param>
        /// <param name="inputAmount">Sum to be payed (10 digits)</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>deviceCode</term>
        /// <description>‘F’ - Error;‘E’ - subtotal is negative.Pay is not executed an Amount is with negative sum.
        ///‘D’ - If payed sum is smaller than the sum in the receipt.The rest to pay is returned in "Amount.
        ///‘R’ - If payed sum is greater than the sum form the receipt.Will be printed message "РЕСТО", and the change will be returned in "Amount.
        ///‘I’ - Sum from any of the tax group is negative and the error is occured. In "Amount" is returned subtotal.
        /// </description>
        /// </item>
        /// <item>
        /// <term>outputAmount</term>
        /// <description>Sum (up to 9 dogits)Depends on "deviceCode".</description>
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

            string r = CustomCommand(53 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["deviceCode"] = split[0];
            if(split.Length >= 2) 
                result["outputAmount"] = split[1];
            return result;
        }

        // Command number(Dec): 53 - please check fiscal device documentation.
        /// <summary>
        /// Total 
        /// </summary>
        /// <param name="textRow1">Text up to 42 bytes</param>
        /// <param name="paidMode">‘P’ - Cash payment; ‘N’ - Credit payment; ‘C’ - Debit card payment; ‘D’ - NZOK payment;
        /// ‘I’ - Check payment; ‘J’ - Coupon payment</param>
        /// <param name="inputAmount">Sum to be payed (10 digits)</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>deviceCode</term>
        /// <description>‘F’ - Error;‘E’ - subtotal is negative.Pay is not executed an Amount is with negative sum.
        ///‘D’ - If payed sum is smaller than the sum in the receipt.The rest to pay is returned in "Amount.
        ///‘R’ - If payed sum is greater than the sum form the receipt.Will be printed message "РЕСТО", and the change will be returned in "Amount.
        ///‘I’ - Sum from any of the tax group is negative and the error is occured. In "Amount" is returned subtotal.
        /// </description>
        /// </item>
        /// <item>
        /// <term>outputAmount</term>
        /// <description>Sum (up to 9 dogits)Depends on "deviceCode".</description>
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

            string r = CustomCommand(53 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["deviceCode"] = split[0];
            if(split.Length >= 2) 
                result["outputAmount"] = split[1];
            return result;
        }

        // Command number(Dec): 53 - please check fiscal device documentation.
        /// <summary>
        /// Total 
        /// </summary>
        /// <param name="textRow2">Text up to 42 bytes</param>
        /// <param name="paidMode">‘P’ - Cash payment; ‘N’ - Credit payment; ‘C’ - Debit card payment; ‘D’ - NZOK payment;
        /// ‘I’ - Check payment; ‘J’ - Coupon payment</param>
        /// <param name="inputAmount">Sum to be payed (10 digits)</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>deviceCode</term>
        /// <description>‘F’ - Error;‘E’ - subtotal is negative.Pay is not executed an Amount is with negative sum.
        ///‘D’ - If payed sum is smaller than the sum in the receipt.The rest to pay is returned in "Amount.
        ///‘R’ - If payed sum is greater than the sum form the receipt.Will be printed message "РЕСТО", and the change will be returned in "Amount.
        ///‘I’ - Sum from any of the tax group is negative and the error is occured. In "Amount" is returned subtotal.
        /// </description>
        /// </item>
        /// <item>
        /// <term>outputAmount</term>
        /// <description>Sum (up to 9 dogits)Depends on "deviceCode".</description>
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

            string r = CustomCommand(53 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["deviceCode"] = split[0];
            if(split.Length >= 2) 
                result["outputAmount"] = split[1];
            return result;
        }

        // Command number(Dec): 53 - please check fiscal device documentation.
        /// <summary>
        /// Total 
        /// </summary>
        /// <param name="paidMode">‘P’ - Cash payment; ‘N’ - Credit payment; ‘C’ - Debit card payment; ‘D’ - NZOK payment;
        /// ‘I’ - Check payment; ‘J’ - Coupon payment</param>
        /// <param name="inputAmount">Sum to be payed (10 digits)</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>deviceCode</term>
        /// <description>‘F’ - Error;‘E’ - subtotal is negative.Pay is not executed an Amount is with negative sum.
        ///‘D’ - If payed sum is smaller than the sum in the receipt.The rest to pay is returned in "Amount.
        ///‘R’ - If payed sum is greater than the sum form the receipt.Will be printed message "РЕСТО", and the change will be returned in "Amount.
        ///‘I’ - Sum from any of the tax group is negative and the error is occured. In "Amount" is returned subtotal.
        /// </description>
        /// </item>
        /// <item>
        /// <term>outputAmount</term>
        /// <description>Sum (up to 9 dogits)Depends on "deviceCode".</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Total_PAmountWithoutText(string paidMode, string inputAmount) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\t");
            inputString.Append(paidMode);
            inputString.Append(inputAmount);

            string r = CustomCommand(53 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["deviceCode"] = split[0];
            if(split.Length >= 2) 
                result["outputAmount"] = split[1];
            return result;
        }

        // Command number(Dec): 53 - please check fiscal device documentation.
        /// <summary>
        /// Total only executed with text
        /// </summary>
        /// <param name="textRow1">Text up to 42 bytes</param>
        /// <param name="textRow2">Text up to 42 bytes</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>deviceCode</term>
        /// <description>‘F’ - Error;‘E’ - subtotal is negative.Pay is not executed an Amount is with negative sum.
        ///‘D’ - If payed sum is smaller than the sum in the receipt.The rest to pay is returned in "Amount.
        ///‘R’ - If payed sum is greater than the sum form the receipt.Will be printed message "РЕСТО", and the change will be returned in "Amount.
        ///‘I’ - Sum from any of the tax group is negative and the error is occured. In "Amount" is returned subtotal.
        /// </description>
        /// </item>
        /// <item>
        /// <term>outputAmount</term>
        /// <description>Sum (up to 9 dogits)Depends on "deviceCode".</description>
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

            string r = CustomCommand(53 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["deviceCode"] = split[0];
            if(split.Length >= 2) 
                result["outputAmount"] = split[1];
            return result;
        }

        // Command number(Dec): 53 - please check fiscal device documentation.
        /// <summary>
        /// Total with one line of text
        /// </summary>
        /// <param name="textRow1">Text up to 42 bytes</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>deviceCode</term>
        /// <description>‘F’ - Error;‘E’ - subtotal is negative.Pay is not executed an Amount is with negative sum.
        ///‘D’ - If payed sum is smaller than the sum in the receipt.The rest to pay is returned in "Amount.
        ///‘R’ - If payed sum is greater than the sum form the receipt.Will be printed message "РЕСТО", and the change will be returned in "Amount.
        ///‘I’ - Sum from any of the tax group is negative and the error is occured. In "Amount" is returned subtotal.
        /// </description>
        /// </item>
        /// <item>
        /// <term>outputAmount</term>
        /// <description>Sum (up to 9 dogits)Depends on "deviceCode".</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Total_TextRow1(string textRow1) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\t");

            string r = CustomCommand(53 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["deviceCode"] = split[0];
            if(split.Length >= 2) 
                result["outputAmount"] = split[1];
            return result;
        }

        // Command number(Dec): 53 - please check fiscal device documentation.
        /// <summary>
        /// Total executed with second line of text
        /// </summary>
        /// <param name="textRow2">Text up to 42 bytes</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>deviceCode</term>
        /// <description>‘F’ - Error;‘E’ - subtotal is negative.Pay is not executed an Amount is with negative sum.
        ///‘D’ - If payed sum is smaller than the sum in the receipt.The rest to pay is returned in "Amount.
        ///‘R’ - If payed sum is greater than the sum form the receipt.Will be printed message "РЕСТО", and the change will be returned in "Amount.
        ///‘I’ - Sum from any of the tax group is negative and the error is occured. In "Amount" is returned subtotal.
        /// </description>
        /// </item>
        /// <item>
        /// <term>outputAmount</term>
        /// <description>Sum (up to 9 dogits)Depends on "deviceCode".</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Total_TextRow2(string textRow2) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\n");
            inputString.Append(textRow2);
            inputString.Append("\t");

            string r = CustomCommand(53 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["deviceCode"] = split[0];
            if(split.Length >= 2) 
                result["outputAmount"] = split[1];
            return result;
        }

        // Command number(Dec): 53 - please check fiscal device documentation.
        /// <summary>
        /// Total without text
        /// </summary>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>deviceCode</term>
        /// <description>‘F’ - Error;‘E’ - subtotal is negative.Pay is not executed an Amount is with negative sum.
        ///‘D’ - If payed sum is smaller than the sum in the receipt.The rest to pay is returned in "Amount.
        ///‘R’ - If payed sum is greater than the sum form the receipt.Will be printed message "РЕСТО", and the change will be returned in "Amount.
        ///‘I’ - Sum from any of the tax group is negative and the error is occured. In "Amount" is returned subtotal.
        /// </description>
        /// </item>
        /// <item>
        /// <term>outputAmount</term>
        /// <description>Sum (up to 9 dogits)Depends on "deviceCode".</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Total_WithoutText() 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\t");

            string r = CustomCommand(53 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["deviceCode"] = split[0];
            if(split.Length >= 2) 
                result["outputAmount"] = split[1];
            return result;
        }

        // Command number(Dec): 53 - please check fiscal device documentation.
        /// <summary>
        /// Total for PipPad
        /// </summary>
        /// <param name="textRow1">Text up to 42 bytes</param>
        /// <param name="textRow2">Text up to 42 bytes</param>
        /// <param name="paidMode">‘C’ - Debit card payment</param>
        /// <param name="inputAmount">Sum to be payed (10 digits)</param>
        /// <param name="pinpadPaidMode"> '1'- Payment with money; '12' - Payment with client points (only for BORICA)</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>deviceCode</term>
        /// <description>‘F’ - Error;‘E’ - subtotal is negative.Pay is not executed an Amount is with negative sum.
        ///‘D’ - If payed sum is smaller than the sum in the receipt.The rest to pay is returned in "Amount.
        ///‘R’ - If payed sum is greater than the sum form the receipt.Will be printed message "РЕСТО", and the change will be returned in "Amount.
        ///‘I’ - Sum from any of the tax group is negative and the error is occured. In "Amount" is returned subtotal.
        /// </description>
        /// </item>
        /// <item>
        /// <term>outputAmount</term>
        /// <description>Sum (up to 9 dogits)Depends on "deviceCode".</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_PinPad_Total(string textRow1, string textRow2, string paidMode, string inputAmount, string pinpadPaidMode) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\n");
            inputString.Append(textRow2);
            inputString.Append("\t");
            inputString.Append(paidMode);
            inputString.Append(inputAmount);
            inputString.Append("*");
            inputString.Append(pinpadPaidMode);

            string r = CustomCommand(53 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["fAnswer"] = split[0];
            return result;
        }

        // Command number(Dec): 53 - please check fiscal device documentation.
        /// <summary>
        /// 
        /// </summary>
        /// <param name="textRow1">Text up to 42 bytes</param>
        /// <param name="paidMode">‘C’ - Debit card payment</param>
        /// <param name="inputAmount">Sum to be payed (10 digits)</param>
        /// <param name="pinpadPaidMode"> '1'- Payment with money; '12' - Payment with client points (only for BORICA)</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>deviceCode</term>
        /// <description>‘F’ - Error;‘E’ - subtotal is negative.Pay is not executed an Amount is with negative sum.
        ///‘D’ - If payed sum is smaller than the sum in the receipt.The rest to pay is returned in "Amount.
        ///‘R’ - If payed sum is greater than the sum form the receipt.Will be printed message "РЕСТО", and the change will be returned in "Amount.
        ///‘I’ - Sum from any of the tax group is negative and the error is occured. In "Amount" is returned subtotal.
        /// </description>
        /// </item>
        /// <item>
        /// <term>outputAmount</term>
        /// <description>Sum (up to 9 dogits)Depends on "deviceCode".</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_PinPad_TotalTextRow1(string textRow1, string paidMode, string inputAmount, string pinpadPaidMode) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\t");
            inputString.Append(paidMode);
            inputString.Append(inputAmount);
            inputString.Append("*");
            inputString.Append(pinpadPaidMode);

            string r = CustomCommand(53 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["fAnswer"] = split[0];
            return result;
        }

        // Command number(Dec): 53 - please check fiscal device documentation.
        /// <summary>
        /// Total for PinPad and text line 
        /// </summary>
        /// <param name="textRow2">Text up to 42 bytes</param>
        /// <param name="paidMode">‘C’ - Debit card payment</param>
        /// <param name="inputAmount">Sum to be payed (10 digits)</param>
        /// <param name="pinpadPaidMode"> '1'- Payment with money; '12' - Payment with client points (only for BORICA)</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>deviceCode</term>
        /// <description>‘F’ - Error;‘E’ - subtotal is negative.Pay is not executed an Amount is with negative sum.
        ///‘D’ - If payed sum is smaller than the sum in the receipt.The rest to pay is returned in "Amount.
        ///‘R’ - If payed sum is greater than the sum form the receipt.Will be printed message "РЕСТО", and the change will be returned in "Amount.
        ///‘I’ - Sum from any of the tax group is negative and the error is occured. In "Amount" is returned subtotal.
        /// </description>
        /// </item>
        /// <item>
        /// <term>outputAmount</term>
        /// <description>Sum (up to 9 dogits)Depends on "deviceCode".</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_PinPad_TotalTextRow2(string textRow2, string paidMode, string inputAmount, string pinpadPaidMode) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\n");
            inputString.Append(textRow2);
            inputString.Append("\t");
            inputString.Append(paidMode);
            inputString.Append(inputAmount);
            inputString.Append("*");
            inputString.Append(pinpadPaidMode);

            string r = CustomCommand(53 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["fAnswer"] = split[0];
            return result;
        }

        // Command number(Dec): 53 - please check fiscal device documentation.
        /// <summary>
        /// Total without text
        /// </summary>
        /// <param name="paidMode">‘C’ - Debit card payment</param>
        /// <param name="inputAmount">Sum to be payed (10 digits)</param>
        /// <param name="pinpadPaidMode"> '1'- Payment with money; '12' - Payment with client points (only for BORICA)</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>deviceCode</term>
        /// <description>‘F’ - Error;‘E’ - subtotal is negative.Pay is not executed an Amount is with negative sum.
        ///‘D’ - If payed sum is smaller than the sum in the receipt.The rest to pay is returned in "Amount.
        ///‘R’ - If payed sum is greater than the sum form the receipt.Will be printed message "РЕСТО", and the change will be returned in "Amount.
        ///‘I’ - Sum from any of the tax group is negative and the error is occured. In "Amount" is returned subtotal.
        /// </description>
        /// </item>
        /// <item>
        /// <term>outputAmount</term>
        /// <description>Sum (up to 9 dogits)Depends on "deviceCode".</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_PinPad_TotalWText(string paidMode, string inputAmount, string pinpadPaidMode) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\t");
            inputString.Append(paidMode);
            inputString.Append(inputAmount);
            inputString.Append("*");
            inputString.Append(pinpadPaidMode);

            string r = CustomCommand(53 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["fAnswer"] = split[0];
            return result;
        }

        // Command number(Dec): 44 - please check fiscal device documentation.
        /// <summary>
        /// Paper feed
        /// </summary>
        /// <param name="linesCount">Lines count for paper feed. Number (1 or 2 bytes; max 99 number)</param>
        public void receipt_Paper_Feed(string linesCount) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(linesCount);

            string r = CustomCommand(44 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 54 - please check fiscal device documentation.
        /// <summary>
        /// Print of free fiscal text
        /// </summary>
        /// <param name="inputText">Text up to 36 bytes</param>
        public void receipt_Fiscal_Text(string inputText) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(inputText);

            string r = CustomCommand(54 , inputString.ToString());
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

            string r = CustomCommand(60 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 70 - please check fiscal device documentation.
        /// <summary>
        /// Cash in/out
        /// </summary>
        /// <param name="amount">Sum (up to 10 digits). Depending on the sign it is considered as cash in/cash out operation</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description> 'P' -  Command is executed correctly, if sum is non-zero, fiscal device prints non-fiscal receipt for the operation.
        /// 'F' - Command is declined. If available cash is smaller than cash in sum or if there is open fiscal or non-fiscal receipt.
        /// </description>
        /// </item>
        /// <item>
        /// <term>cashSum</term>
        /// <description>Available cash. This sum increases also from every payment.</description>
        /// </item>
        /// <item>
        /// <term>servIn</term>
        /// <description>Sum from cash in information.</description>
        /// </item>
        /// <item>
        /// <term>servOut</term>
        /// <description>Sum from cash out information.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_CashIn_CashOut(string amount) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(amount);

            string r = CustomCommand(70 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["cashSum"] = split[1];
            if(split.Length >= 3) 
                result["servIn"] = split[2];
            if(split.Length >= 4) 
                result["servOut"] = split[3];
            return result;
        }

        // Command number(Dec): 84 - please check fiscal device documentation.
        /// <summary>
        /// Barcode print with text
        /// </summary>
        /// <param name="barcodeType">'1' - EAN8, '2' - EAN13 '3' - Code 128.</param>
        /// <param name="barcodeData">EAN8 - 7 digits. Checksum (8 digits) is calculated by device; 
        /// EAN13 - 12 digits.Checksum (12 digits) is calculated by device; 
        /// Code128 - Up to 30 symbols</param>
        public void receipt_Print_TextBarcode(string barcodeType, string barcodeData)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(barcodeType);
            inputString.Append(",");
            inputString.Append(barcodeData);

            string r = CustomCommand(84, inputString.ToString());
            CheckResult();
        }

        // Command number(Dec): 84 - please check fiscal device documentation.
        /// <summary>
        /// Barcode print without text
        /// </summary>
        /// <param name="barcodeType">'1' - EAN8, '2' - EAN13 '3' - Code 128.</param>
        /// <param name="barcodeData">EAN8 - 7 digits. Checksum (8 digits) is calculated by device; 
        /// EAN13 - 12 digits.Checksum (12 digits) is calculated by device; 
        /// Code128 - Up to 30 symbols</param>
        public void receipt_Print_Barcode(string barcodeType, string barcodeData)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(barcodeType);
            inputString.Append(";");
            inputString.Append(barcodeData);

            string r = CustomCommand(84, inputString.ToString());
            CheckResult();
        }

        // Command number(Dec): 92 - please check fiscal device documentation.
        /// <summary>
        /// Printing of separating line
        /// </summary>
        /// <param name="lineType">
        /// '1' Separating line with the symbol '-'.
        ///'2' Separating line with the symbol '-' and ' '.
        ///'3' Separating line with the symbol '='.
        /// </param>
        public void receipt_Separating_Line(string lineType) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(lineType);

            string r = CustomCommand(92 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 103 - please check fiscal device documentation.
        /// <summary>
        /// Information of current recipt
        /// </summary>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - Command is declined.Error while reading the last record.
        /// </description>
        /// </item>
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
        /// <description>Invoice number (up to 10 digits).</description>
        /// </item>
        /// <item>
        /// <term>fReceiptType</term>
        /// <description>Receipt type:
        /// '0' - fiscal; '1' - Strono operator's error; '2' - Storno refund; '3' - Storno tax base reduction</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Current_Info() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(103 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["canVd"] = split[1];
            if(split.Length >= 3) 
                result["taxA"] = split[2];
            if(split.Length >= 4) 
                result["taxB"] = split[3];
            if(split.Length >= 5) 
                result["taxC"] = split[4];
            if(split.Length >= 6) 
                result["taxD"] = split[5];
            if(split.Length >= 7) 
                result["taxE"] = split[6];
            if(split.Length >= 8) 
                result["taxF"] = split[7];
            if(split.Length >= 9) 
                result["taxG"] = split[8];
            if(split.Length >= 10) 
                result["taxH"] = split[9];
            if(split.Length >= 11) 
                result["inv"] = split[10];
            if(split.Length >= 12) 
                result["invNumber"] = split[11];
            if(split.Length >= 13) 
                result["fReceiptType"] = split[12];
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

            string r = CustomCommand(106 , inputString.ToString());
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

            string r = CustomCommand(109 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 69 - please check fiscal device documentation.
        /// <summary>
        /// Daily closure
        /// </summary>
        /// <param name="option">'0' - print Z report, '2' - print X report</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>closure</term>
        /// <description>Number of Z-report (4 bytes).</description>
        /// </item>
        /// <item>
        /// <term>fMTotal</term>
        /// <description>Total accumulated sum (12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumA</term>
        /// <description>Total accumulated sum by tax group A(12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumB</term>
        /// <description>Total accumulated sum by tax group B(12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumC</term>
        /// <description>Total accumulated sum by tax group C(12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumD</term>
        /// <description>Total accumulated sum by tax group D(12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumE</term>
        /// <description>Total accumulated sum by tax group E(12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumF</term>
        /// <description>Total accumulated sum by tax group F(12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumG</term>
        /// <description>Total accumulated sum by tax group G(12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumH</term>
        /// <description>Total accumulated sum by tax group H(12 bytes with sign).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_DailyClosure_01(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(69 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["closure"] = split[0];
            if(split.Length >= 2) 
                result["fMTotal"] = split[1];
            if(split.Length >= 3) 
                result["totalsumA"] = split[2];
            if(split.Length >= 4) 
                result["totalsumB"] = split[3];
            if(split.Length >= 5) 
                result["totalsumC"] = split[4];
            if(split.Length >= 6) 
                result["totalsumD"] = split[5];
            if(split.Length >= 7) 
                result["totalsumE"] = split[6];
            if(split.Length >= 8) 
                result["totalsumF"] = split[7];
            if(split.Length >= 9) 
                result["totalsumG"] = split[8];
            if(split.Length >= 10) 
                result["totalsumH"] = split[9];
            return result;
        }

        // Command number(Dec): 73 - please check fiscal device documentation.
        /// <summary>
        /// Fiscal memory report by numbers
        /// </summary>
        /// <param name="startNumber">Start block number (4 bytes)</param>
        /// <param name="endNumber">End block number (4 bytes)</param>
        public void report_FMByNumRange(string startNumber, string endNumber) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(startNumber);
            inputString.Append(",");
            inputString.Append(endNumber);

            string r = CustomCommand(73 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 79 - please check fiscal device documentation.
        /// <summary>
        /// Short fiscal memory report by date
        /// </summary>
        /// <param name="fromDate">Start date (DDMMYY)</param>
        /// <param name="toDate">End date (DDMMYY)</param>
        public void report_FMByDateRange_Short(string fromDate, string toDate) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(fromDate);
            inputString.Append(",");
            inputString.Append(toDate);

            string r = CustomCommand(79 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 94 - please check fiscal device documentation.
        /// <summary>
        /// Extended fiscal memory report by date
        /// </summary>
        /// <param name="fromDate">Start date (DDMMYY)</param>
        /// <param name="toDate">End date (DDMMYY)</param>
        public void report_FMByDateRange(string fromDate, string toDate) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(fromDate);
            inputString.Append(",");
            inputString.Append(toDate);

            string r = CustomCommand(94 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 95 - please check fiscal device documentation.
        /// <summary>
        /// Short fiscal memory by blocks
        /// </summary>
        /// <param name="startNumber">Start number of fiscal record</param>
        /// <param name="endNumber">End number of fiscal record</param>
        public void report_FMByNumRange_Short(string startNumber, string endNumber) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(startNumber);
            inputString.Append(",");
            inputString.Append(endNumber);

            string r = CustomCommand(95 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 105 - please check fiscal device documentation.
        /// <summary>
        /// Operators' report
        /// </summary>
        public void report_Operators() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(105 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 108 - please check fiscal device documentation.
        /// <summary>
        /// Extended daily closure
        /// </summary>
        /// <param name="option">'0' - print Z report, '2' - print X report</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>closure</term>
        /// <description>Number of Z-report (4 bytes).</description>
        /// </item>
        /// <item>
        /// <term>fMTotal</term>
        /// <description>Total accumulated sum (12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumA</term>
        /// <description>Total accumulated sum by tax group A(12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumB</term>
        /// <description>Total accumulated sum by tax group B(12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumC</term>
        /// <description>Total accumulated sum by tax group C(12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumD</term>
        /// <description>Total accumulated sum by tax group D(12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumE</term>
        /// <description>Total accumulated sum by tax group E(12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumF</term>
        /// <description>Total accumulated sum by tax group F(12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumG</term>
        /// <description>Total accumulated sum by tax group G(12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumH</term>
        /// <description>Total accumulated sum by tax group H(12 bytes with sign).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_ExtDailyClosure_01(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(108 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["closure"] = split[0];
            if(split.Length >= 2) 
                result["fMTotal"] = split[1];
            if(split.Length >= 3) 
                result["totalsumA"] = split[2];
            if(split.Length >= 4) 
                result["totalsumB"] = split[3];
            if(split.Length >= 5) 
                result["totalsumC"] = split[4];
            if(split.Length >= 6) 
                result["totalsumD"] = split[5];
            if(split.Length >= 7) 
                result["totalsumE"] = split[6];
            if(split.Length >= 8) 
                result["totalsumF"] = split[7];
            if(split.Length >= 9) 
                result["totalsumG"] = split[8];
            if(split.Length >= 10) 
                result["totalsumH"] = split[9];
            return result;
        }

        // Command number(Dec): 111 - please check fiscal device documentation.
        /// <summary>
        /// PLU report
        /// </summary>
        /// <param name="option">'S' - Printed PLU sale only from the current day.For every item is printed item's number,tax group,name,sold quantity and turnover;
        /// 'P' - Printed all PLU sales. For every item is printed item's number,tax group,stock group,name,barcode,department, price type,and single price</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>P(pass) or F(fail).</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_Items(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(111 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 111 - please check fiscal device documentation.
        /// <summary>
        /// PLU report
        /// </summary>
        /// <param name="option">'S' - Printed PLU sale only from the current day.For every item is printed item's number,tax group,name,sold quantity and turnover;
        /// 'P' - Printed all PLU sales. For every item is printed item's number,tax group,stock group,name,barcode,department, price type,and single price</param>
        /// <param name="startPLU">Start PLU (1 ... 999999999.)</param>
        /// <param name="endPLU">End PLU (1 ... 999999999.)</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>P(pass) or F(fail).</description>
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

            string r = CustomCommand(111 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 111 - please check fiscal device documentation.
        /// <summary>
        /// PLU report
        /// </summary>
        /// <param name="option">'S' - Printed PLU sale only from the current day.For every item is printed item's number,tax group,name,sold quantity and turnover;
        /// 'P' - Printed all PLU sales. For every item is printed item's number,tax group,stock group,name,barcode,department, price type,and single price</param>
        /// <param name="startPLU">Start PLU (1 ... 999999999.)</param>
        /// <param name="endPLU">End PLU (1 ... 999999999.)</param>
        /// <param name="group">Stock group (1...9)</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>P(pass) or F(fail).</description>
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

            string r = CustomCommand(111 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 117 - please check fiscal device documentation.
        /// <summary>
        /// Daily closure by departments
        /// </summary>
        /// <param name="option">'0' - print Z report, '2' - print X report</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>closure</term>
        /// <description>Number of Z-report (4 bytes).</description>
        /// </item>
        /// <item>
        /// <term>fMTotal</term>
        /// <description>Total accumulated sum (12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumA</term>
        /// <description>Total accumulated sum by tax group A(12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumB</term>
        /// <description>Total accumulated sum by tax group B(12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumC</term>
        /// <description>Total accumulated sum by tax group C(12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumD</term>
        /// <description>Total accumulated sum by tax group D(12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumE</term>
        /// <description>Total accumulated sum by tax group E(12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumF</term>
        /// <description>Total accumulated sum by tax group F(12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumG</term>
        /// <description>Total accumulated sum by tax group G(12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumH</term>
        /// <description>Total accumulated sum by tax group H(12 bytes with sign).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_DailyClosureByDepartments_01(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(117 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["closure"] = split[0];
            if(split.Length >= 2) 
                result["fMTotal"] = split[1];
            if(split.Length >= 3) 
                result["totalsumA"] = split[2];
            if(split.Length >= 4) 
                result["totalsumB"] = split[3];
            if(split.Length >= 5) 
                result["totalsumC"] = split[4];
            if(split.Length >= 6) 
                result["totalsumD"] = split[5];
            if(split.Length >= 7) 
                result["totalsumE"] = split[6];
            if(split.Length >= 8) 
                result["totalsumF"] = split[7];
            if(split.Length >= 9) 
                result["totalsumG"] = split[8];
            if(split.Length >= 10) 
                result["totalsumH"] = split[9];
            return result;
        }

        // Command number(Dec): 118 - please check fiscal device documentation.
        /// <summary>
        /// Daily closure by departments and items 
        /// </summary>
        /// <param name="option">'0' - print Z report, '2' - print X report</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>closure</term>
        /// <description>Number of Z-report (4 bytes).</description>
        /// </item>
        /// <item>
        /// <term>fMTotal</term>
        /// <description>Total accumulated sum (12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumA</term>
        /// <description>Total accumulated sum by tax group A(12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumB</term>
        /// <description>Total accumulated sum by tax group B(12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumC</term>
        /// <description>Total accumulated sum by tax group C(12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumD</term>
        /// <description>Total accumulated sum by tax group D(12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumE</term>
        /// <description>Total accumulated sum by tax group E(12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumF</term>
        /// <description>Total accumulated sum by tax group F(12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumG</term>
        /// <description>Total accumulated sum by tax group G(12 bytes with sign).</description>
        /// </item>
        /// <item>
        /// <term>totalsumH</term>
        /// <description>Total accumulated sum by tax group H(12 bytes with sign).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_DailyClosureByDepartmentsAndItems_01(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(118 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["closure"] = split[0];
            if(split.Length >= 2) 
                result["fMTotal"] = split[1];
            if(split.Length >= 3) 
                result["totalsumA"] = split[2];
            if(split.Length >= 4) 
                result["totalsumB"] = split[3];
            if(split.Length >= 5) 
                result["totalsumC"] = split[4];
            if(split.Length >= 6) 
                result["totalsumD"] = split[5];
            if(split.Length >= 7) 
                result["totalsumE"] = split[6];
            if(split.Length >= 8) 
                result["totalsumF"] = split[7];
            if(split.Length >= 9) 
                result["totalsumG"] = split[8];
            if(split.Length >= 10) 
                result["totalsumH"] = split[9];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Get item information
        /// </summary>
        /// <param name="option">‘I’ - Item information</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass, 'F' - failed.</description>
        /// </item>
        /// <item>
        /// <term>total</term>
        /// <description>Total number of programmable PLUs.</description>
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

            string r = CustomCommand(107 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["total"] = split[1];
            if(split.Length >= 3) 
                result["prog"] = split[2];
            if(split.Length >= 4) 
                result["len"] = split[3];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Program a PLU
        /// </summary>
        /// <param name="option">'P' - Program a PLU</param>
        /// <param name="taxGroup">Tax group (‘А’,’Б’,’В’,’Г’...)</param>
        /// <param name="targetPLU">Item number (1 ... 999999999)</param>
        /// <param name="group">Stock group (1...9)</param>
        /// <param name="singlePrice">Single price (up to 8 digits)</param>
        /// <param name="quantity">Quantity, number with 3 digits, after the decimal point</param>
        /// <param name="itemName">Item  name (up to 22 bytes)</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Possible values: 'P' - pass; 'F' - failed.</description>
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

            string r = CustomCommand(107 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Program a PLU with replace
        /// </summary>
        /// <param name="option">'P' - Program a PLU</param>
        /// <param name="taxGroup">Tax group (‘А’,’Б’,’В’,’Г’...)</param>
        /// <param name="targetPLU">Item number (1 ... 999999999)</param>
        /// <param name="group">Stock group (1...9)</param>
        /// <param name="singlePrice">Single price (up to 8 digits)</param>
        /// <param name="replace">Parameter with value: 'A'</param>
        /// <param name="quantity">Quantity, number with 3 digits, after the decimal point</param>
        /// <param name="itemName">Item  name (up to 22 bytes)</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Possible values: 'P' - pass; 'F' - failed.</description>
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

            string r = CustomCommand(107 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Change of the available quantity for item
        /// </summary>
        /// <param name="option">'A' - Change of the available quantity for item </param>
        /// <param name="targetPLU">Item number (For ECRs 1...100000; For FPs 1...3000)</param>
        /// <param name="quantity">Stock quantity (0.001...99999.999)</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Possible values: 'P' - pass; 'F' - failed.</description>
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

            string r = CustomCommand(107 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Deleting items
        /// </summary>
        /// <param name="option">'D' - Item deleting</param>
        /// <param name="targetPLU">PLU of the item that you want to delete</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Possible values: 'P' - pass; 'F' - failed.</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Delete_Item(string option, string targetPLU) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(targetPLU);

            string r = CustomCommand(107 , inputString.ToString());
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
        /// <param name="option">'D' - Item deleting</param>
        /// <param name="startPLU">Start PLU of the item that you want to delete</param>
        /// <param name="endPLU">End PLU of the item that you want to delete</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Possible values: 'P' - pass; 'F' - failed.</description>
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

            string r = CustomCommand(107 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Deleting all items
        /// </summary>
        /// <param name="dOption">'D' - Item deleting</param>
        /// <param name="aOption">'A' - For all items</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Possible values: 'P' - pass; 'F' - failed.</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Delete_All_Items(string dOption, string aOption) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(dOption);
            inputString.Append(aOption);

            string r = CustomCommand(107 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Get item
        /// </summary>
        /// <param name="option">'R' - read item</param>
        /// <param name="targetPLU">PLU of the item that you want to read</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed</description>
        /// </item>
        /// <item>
        /// <term>PLU</term>
        /// <description>Item PLU (1 ... 999999999).</description>
        /// </item>
        /// <item>
        /// <term>taxGroup</term>
        /// <description>Tax group.</description>
        /// </item>
        /// <item>
        /// <term>group</term>
        /// <description>Stock group (1 ... 9).</description>
        /// </item>
        /// <item>
        /// <term>singlePrice</term>
        /// <description>Single price with decimal point.</description>
        /// </item>
        /// <item>
        /// <term>total</term>
        /// <description>Total for current item.</description>
        /// </item>
        /// <item>
        /// <term>sold</term>
        /// <description>Sold quantity (3 digits after decimal points).</description>
        /// </item>
        /// <item>
        /// <term>available</term>
        /// <description>Current quantity.</description>
        /// </item>
        /// <item>
        /// <term>itemName</term>
        /// <description>Item name (up to 22 bytes).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Get_Item(string option, string targetPLU) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(targetPLU);

            string r = CustomCommand(107 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["PLU"] = split[1];
            if(split.Length >= 3) 
                result["taxGroup"] = split[2];
            if(split.Length >= 4) 
                result["group"] = split[3];
            if(split.Length >= 5) 
                result["singlePrice"] = split[4];
            if(split.Length >= 6) 
                result["total"] = split[5];
            if(split.Length >= 7) 
                result["sold"] = split[6];
            if(split.Length >= 8) 
                result["available"] = split[7];
            if(split.Length >= 9) 
                result["itemName"] = split[8];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Get first found item
        /// </summary>
        /// <param name="option">'F' - get first found item</param>
        /// <param name="targetPLU">PLU of item</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed</description>
        /// </item>
        /// <item>
        /// <term>PLU</term>
        /// <description>Item PLU (1 ... 999999999).</description>
        /// </item>
        /// <item>
        /// <term>taxGroup</term>
        /// <description>Tax group.</description>
        /// </item>
        /// <item>
        /// <term>group</term>
        /// <description>Stock group (1 ... 9).</description>
        /// </item>
        /// <item>
        /// <term>singlePrice</term>
        /// <description>Single price with decimal point.</description>
        /// </item>
        /// <item>
        /// <term>total</term>
        /// <description>Total for current item.</description>
        /// </item>
        /// <item>
        /// <term>sold</term>
        /// <description>Sold quantity (3 digits after decimal points).</description>
        /// </item>
        /// <item>
        /// <term>available</term>
        /// <description>Current quantity.</description>
        /// </item>
        /// <item>
        /// <term>itemName</term>
        /// <description>Item name (up to 22 bytes).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Get_FirstFoundItem(string option, string targetPLU) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(targetPLU);

            string r = CustomCommand(107 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["PLU"] = split[1];
            if(split.Length >= 3) 
                result["taxGroup"] = split[2];
            if(split.Length >= 4) 
                result["group"] = split[3];
            if(split.Length >= 5) 
                result["singlePrice"] = split[4];
            if(split.Length >= 6) 
                result["total"] = split[5];
            if(split.Length >= 7) 
                result["sold"] = split[6];
            if(split.Length >= 8) 
                result["available"] = split[7];
            if(split.Length >= 9) 
                result["itemName"] = split[8];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Get last found item
        /// </summary>
        /// <param name="option">'L' - last found item</param>
        /// <param name="targetPLU">PLU of item</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed</description>
        /// </item>
        /// <item>
        /// <term>PLU</term>
        /// <description>Item PLU (1 ... 999999999).</description>
        /// </item>
        /// <item>
        /// <term>taxGroup</term>
        /// <description>Tax group.</description>
        /// </item>
        /// <item>
        /// <term>group</term>
        /// <description>Stock group (1 ... 9).</description>
        /// </item>
        /// <item>
        /// <term>singlePrice</term>
        /// <description>Single price with decimal point.</description>
        /// </item>
        /// <item>
        /// <term>total</term>
        /// <description>Total for current item.</description>
        /// </item>
        /// <item>
        /// <term>sold</term>
        /// <description>Sold quantity (3 digits after decimal points).</description>
        /// </item>
        /// <item>
        /// <term>available</term>
        /// <description>Current quantity.</description>
        /// </item>
        /// <item>
        /// <term>itemName</term>
        /// <description>Item name (up to 22 bytes).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Get_LastFoundItem(string option, string targetPLU) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(targetPLU);

            string r = CustomCommand(107 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["PLU"] = split[1];
            if(split.Length >= 3) 
                result["taxGroup"] = split[2];
            if(split.Length >= 4) 
                result["group"] = split[3];
            if(split.Length >= 5) 
                result["singlePrice"] = split[4];
            if(split.Length >= 6) 
                result["total"] = split[5];
            if(split.Length >= 7) 
                result["sold"] = split[6];
            if(split.Length >= 8) 
                result["available"] = split[7];
            if(split.Length >= 9) 
                result["itemName"] = split[8];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Get next item
        /// </summary>
        /// <param name="option">'N' - for next item</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed</description>
        /// </item>
        /// <item>
        /// <term>PLU</term>
        /// <description>Item PLU (1 ... 999999999).</description>
        /// </item>
        /// <item>
        /// <term>taxGroup</term>
        /// <description>Tax group.</description>
        /// </item>
        /// <item>
        /// <term>group</term>
        /// <description>Stock group (1 ... 9).</description>
        /// </item>
        /// <item>
        /// <term>singlePrice</term>
        /// <description>Single price with decimal point.</description>
        /// </item>
        /// <item>
        /// <term>total</term>
        /// <description>Total for current item.</description>
        /// </item>
        /// <item>
        /// <term>sold</term>
        /// <description>Sold quantity (3 digits after decimal points).</description>
        /// </item>
        /// <item>
        /// <term>available</term>
        /// <description>Current quantity.</description>
        /// </item>
        /// <item>
        /// <term>itemName</term>
        /// <description>Item name (up to 22 bytes).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Get_NextItem(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(107 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["PLU"] = split[1];
            if(split.Length >= 3) 
                result["taxGroup"] = split[2];
            if(split.Length >= 4) 
                result["group"] = split[3];
            if(split.Length >= 5) 
                result["singlePrice"] = split[4];
            if(split.Length >= 6) 
                result["total"] = split[5];
            if(split.Length >= 7) 
                result["sold"] = split[6];
            if(split.Length >= 8) 
                result["available"] = split[7];
            if(split.Length >= 9) 
                result["itemName"] = split[8];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Get first found item with non-zero sales
        /// </summary>
        /// <param name="option">'f' - First item with non-zero sales</param>
        /// <param name="targetPLU">Item PLU (1 ... 999999999)</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed</description>
        /// </item>
        /// <item>
        /// <term>PLU</term>
        /// <description>Item PLU (1 ... 999999999).</description>
        /// </item>
        /// <item>
        /// <term>taxGroup</term>
        /// <description>Tax group.</description>
        /// </item>
        /// <item>
        /// <term>group</term>
        /// <description>Stock group (1 ... 9).</description>
        /// </item>
        /// <item>
        /// <term>singlePrice</term>
        /// <description>Single price with decimal point.</description>
        /// </item>
        /// <item>
        /// <term>total</term>
        /// <description>Total for current item.</description>
        /// </item>
        /// <item>
        /// <term>sold</term>
        /// <description>Sold quantity (3 digits after decimal points).</description>
        /// </item>
        /// <item>
        /// <term>available</term>
        /// <description>Current quantity.</description>
        /// </item>
        /// <item>
        /// <term>itemName</term>
        /// <description>Item name (up to 22 bytes).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Get_FirstSoldItem(string option, string targetPLU) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(targetPLU);

            string r = CustomCommand(107 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["PLU"] = split[1];
            if(split.Length >= 3) 
                result["taxGroup"] = split[2];
            if(split.Length >= 4) 
                result["group"] = split[3];
            if(split.Length >= 5) 
                result["singlePrice"] = split[4];
            if(split.Length >= 6) 
                result["total"] = split[5];
            if(split.Length >= 7) 
                result["sold"] = split[6];
            if(split.Length >= 8) 
                result["available"] = split[7];
            if(split.Length >= 9) 
                result["itemName"] = split[8];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Get last sold item
        /// </summary>
        /// <param name="option">'l' - Last sold item</param>
        /// <param name="targetPLU">Item PLU (1 ... 999999999)</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed</description>
        /// </item>
        /// <item>
        /// <term>PLU</term>
        /// <description>Item PLU (1 ... 999999999).</description>
        /// </item>
        /// <item>
        /// <term>taxGroup</term>
        /// <description>Tax group.</description>
        /// </item>
        /// <item>
        /// <term>group</term>
        /// <description>Stock group (1 ... 9).</description>
        /// </item>
        /// <item>
        /// <term>singlePrice</term>
        /// <description>Single price with decimal point.</description>
        /// </item>
        /// <item>
        /// <term>total</term>
        /// <description>Total for current item.</description>
        /// </item>
        /// <item>
        /// <term>sold</term>
        /// <description>Sold quantity (3 digits after decimal points).</description>
        /// </item>
        /// <item>
        /// <term>available</term>
        /// <description>Current quantity.</description>
        /// </item>
        /// <item>
        /// <term>itemName</term>
        /// <description>Item name (up to 22 bytes).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Get_LastSoldItem(string option, string targetPLU) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(targetPLU);

            string r = CustomCommand(107 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["PLU"] = split[1];
            if(split.Length >= 3) 
                result["taxGroup"] = split[2];
            if(split.Length >= 4) 
                result["group"] = split[3];
            if(split.Length >= 5) 
                result["singlePrice"] = split[4];
            if(split.Length >= 6) 
                result["total"] = split[5];
            if(split.Length >= 7) 
                result["sold"] = split[6];
            if(split.Length >= 8) 
                result["available"] = split[7];
            if(split.Length >= 9) 
                result["itemName"] = split[8];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Returns data for the next found item with non-zero sell
        /// </summary>
        /// <param name="option">'n' - next item with non-zero sell</param>
        /// <param name="targetPLU">Item PLU (1 ... 999999999)</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed</description>
        /// </item>
        /// <item>
        /// <term>PLU</term>
        /// <description>Item PLU (1 ... 999999999).</description>
        /// </item>
        /// <item>
        /// <term>taxGroup</term>
        /// <description>Tax group.</description>
        /// </item>
        /// <item>
        /// <term>group</term>
        /// <description>Stock group (1 ... 9).</description>
        /// </item>
        /// <item>
        /// <term>singlePrice</term>
        /// <description>Single price with decimal point.</description>
        /// </item>
        /// <item>
        /// <term>total</term>
        /// <description>Total for current item.</description>
        /// </item>
        /// <item>
        /// <term>sold</term>
        /// <description>Sold quantity (3 digits after decimal points).</description>
        /// </item>
        /// <item>
        /// <term>available</term>
        /// <description>Current quantity.</description>
        /// </item>
        /// <item>
        /// <term>itemName</term>
        /// <description>Item name (up to 22 bytes).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Get_NextSoldItem(string option, string targetPLU) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(targetPLU);

            string r = CustomCommand(107 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["PLU"] = split[1];
            if(split.Length >= 3) 
                result["taxGroup"] = split[2];
            if(split.Length >= 4) 
                result["group"] = split[3];
            if(split.Length >= 5) 
                result["singlePrice"] = split[4];
            if(split.Length >= 6) 
                result["total"] = split[5];
            if(split.Length >= 7) 
                result["sold"] = split[6];
            if(split.Length >= 8) 
                result["available"] = split[7];
            if(split.Length >= 9) 
                result["itemName"] = split[8];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Find the first not programmed item
        /// </summary>
        /// <param name="option">'X' - First not programmed item</param>
        /// <param name="targetPLU">Item PLU (1 ... 999999999)</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>PLU</term>
        /// <description>Item number for first not programmed item.</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Get_FirstNotProgrammedItem(string option, string targetPLU) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(targetPLU);

            string r = CustomCommand(107 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["PLU"] = split[0];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Find the last not programmed item
        /// </summary>
        /// <param name="option">'x' - last not programmed item</param>
        /// <param name="targetPLU">Item PLU (1 ... 999999999)</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>PLU</term>
        /// <description>Item number for first not programmed item.</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Get_LastNotProgrammedItem(string option, string targetPLU) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(targetPLU);

            string r = CustomCommand(107 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["PLU"] = split[0];
            return result;
        }

        // Command number(Dec): 140 - please check fiscal device documentation.
        /// <summary>
        /// Set client data
        /// </summary>
        /// <param name="option">'+'</param>
        /// <param name="eikValue">EIK number (9 ... 13)</param>
        /// <param name="eikType">'0' - BULSTAT; '1' - EGN; '2' - LNCH; '3' - service number</param>
        /// <param name="receiver">Reciever's name (up to 36 chars)</param>
        /// <param name="client">Clent's name (up to 36 chars);</param>
        /// <param name="taxNo">Client's tax number (10  - 14 symbols)</param>
        /// <param name="address1">Client's address (up to 36 symbols)</param>
        /// <param name="address2">Client's address second line (up to 36 symbols)</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed.</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> clients_Set_ClientData(string option, string eikValue, string eikType, string receiver, string client, string taxNo, string address1, string address2) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(eikValue);
            inputString.Append("\t");
            inputString.Append(eikType);
            inputString.Append("\t");
            inputString.Append(receiver);
            inputString.Append("\t");
            inputString.Append(client);
            inputString.Append("\t");
            inputString.Append(taxNo);
            inputString.Append("\t");
            inputString.Append(address1);
            inputString.Append("\t");
            inputString.Append(address2);

            string r = CustomCommand(140 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 140 - please check fiscal device documentation.
        /// <summary>
        /// Delete client item
        /// </summary>
        /// <param name="option">'-' - delete client item</param>
        /// <param name="eikValue">EIK number (9 ... 13)</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed.</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> clients_Del_ClientData(string option, string eikValue) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(eikValue);

            string r = CustomCommand(140 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 140 - please check fiscal device documentation.
        /// <summary>
        /// Get client data
        /// </summary>
        /// <param name="option">'#' - get client data</param>
        /// <param name="eikValue">EIK number (9 ... 13)</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>value</term>
        /// <description>'P' - pass or 'F' - failed.</description>
        /// </item>
        /// <item>
        /// <term>eikType</term>
        /// <description>'0' - BULSTAT; '1' - EGN; '2' - LNCH; '3' - service number.</description>
        /// </item>
        /// <item>
        /// <term>receiver</term>
        /// <description>Reciever's name (up to 36 chars).</description>
        /// </item>
        /// <item>
        /// <term>client</term>
        /// <description>Client's name.</description>
        /// </item>
        /// <item>
        /// <term>taxNo</term>
        /// <description>Tax number (Between 10 and 14 symbols).</description>
        /// </item>
        /// <item>
        /// <term>address1</term>
        /// <description>Client's address (up to 36 symbols).</description>
        /// </item>
        /// <item>
        /// <term>address2</term>
        /// <description>Client's address (up to 36 symbols).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> clients_Get_ClientData(string option, string eikValue) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(eikValue);

            string r = CustomCommand(140 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["value"] = split[0];
            if(split.Length >= 2) 
                result["eikType"] = split[1];
            if(split.Length >= 3) 
                result["receiver"] = split[2];
            if(split.Length >= 4) 
                result["client"] = split[3];
            if(split.Length >= 5) 
                result["taxNo"] = split[4];
            if(split.Length >= 6) 
                result["address1"] = split[5];
            if(split.Length >= 7) 
                result["address2"] = split[6];
            return result;
        }

        // Command number(Dec): 140 - please check fiscal device documentation.
        /// <summary>
        /// Set seller's data
        /// </summary>
        /// <param name="option">'^' - Set seller's data</param>
        /// <param name="seller">Seller's name (up to 36 symbols)</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed.</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> clients_Set_SellerName(string option, string seller) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(seller);

            string r = CustomCommand(140 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 140 - please check fiscal device documentation.
        /// <summary>
        ///  Get seller's data
        /// </summary>
        /// <param name="option">'$' - Get seller's data</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>seller</term>
        /// <description>Seller's name</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> clients_Get_SellerName(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(140 , inputString.ToString());
            CheckResult();
            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["seller"] = split[0];
            return result;

        }

        // Command number(Dec): 140 - please check fiscal device documentation.
        /// <summary>
        /// Get first set client's data
        /// </summary>
        /// <param name="option">'~' - Get first set client's data</param>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>value</term>
        /// <description>'P' - pass or 'F' - failed.</description>
        /// </item>
        /// <item>
        /// <term>eikType</term>
        /// <description>'0' - BULSTAT; '1' - EGN; '2' - LNCH; '3' - service number.</description>
        /// </item>
        /// <item>
        /// <term>receiver</term>
        /// <description>Reciever's name (up to 36 chars).</description>
        /// </item>
        /// <item>
        /// <term>client</term>
        /// <description>Client's name.</description>
        /// </item>
        /// <item>
        /// <term>taxNo</term>
        /// <description>Tax number (Between 10 and 14 symbols).</description>
        /// </item>
        /// <item>
        /// <term>address1</term>
        /// <description>Client's address (up to 36 symbols).</description>
        /// </item>
        /// <item>
        /// <term>address2</term>
        /// <description>Client's address (up to 36 symbols).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> clients_Get_FirstClientData(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(140 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["value"] = split[0];
            if(split.Length >= 2) 
                result["eikType"] = split[1];
            if(split.Length >= 3) 
                result["receiver"] = split[2];
            if(split.Length >= 4) 
                result["client"] = split[3];
            if(split.Length >= 5) 
                result["taxNo"] = split[4];
            if(split.Length >= 6) 
                result["address1"] = split[5];
            if(split.Length >= 7) 
                result["address2"] = split[6];
            return result;
        }

        // Command number(Dec): 140 - please check fiscal device documentation.
        /// <summary>
        /// Get next client data
        /// </summary>
        /// <param name="option">'@' - next clien data</param>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>value</term>
        /// <description>'P' - pass or 'F' - failed.</description>
        /// </item>
        /// <item>
        /// <term>eikType</term>
        /// <description>'0' - BULSTAT; '1' - EGN; '2' - LNCH; '3' - service number.</description>
        /// </item>
        /// <item>
        /// <term>receiver</term>
        /// <description>Reciever's name (up to 36 chars).</description>
        /// </item>
        /// <item>
        /// <term>client</term>
        /// <description>Client's name.</description>
        /// </item>
        /// <item>
        /// <term>taxNo</term>
        /// <description>Tax number (Between 10 and 14 symbols).</description>
        /// </item>
        /// <item>
        /// <term>address1</term>
        /// <description>Client's address (up to 36 symbols).</description>
        /// </item>
        /// <item>
        /// <term>address2</term>
        /// <description>Client's address (up to 36 symbols).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> clients_Get_NextClientData(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(140 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["value"] = split[0];
            if(split.Length >= 2) 
                result["eikType"] = split[1];
            if(split.Length >= 3) 
                result["receiver"] = split[2];
            if(split.Length >= 4) 
                result["client"] = split[3];
            if(split.Length >= 5) 
                result["taxNo"] = split[4];
            if(split.Length >= 6) 
                result["address1"] = split[5];
            if(split.Length >= 7) 
                result["address2"] = split[6];
            return result;
        }

        // Command number(Dec): 140 - please check fiscal device documentation.
        /// <summary>
        /// Delete all client data
        /// </summary>
        /// <param name="option">'!' - Delete all client data</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass; 'F' - failed.</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> clients_Del_AllClientData(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(140 , inputString.ToString());
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
        /// <param name="item">0 to 5 (for line number)</param>
        /// <param name="value">Text (up to 42 symbols)</param>
        public void config_Set_HeaderLine(string item, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(item);
            inputString.Append(value);

            string r = CustomCommand(43 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Set footer line
        /// </summary>
        /// <param name="item">6 or 7 (for line number)</param>
        /// <param name="value">Text (up to 42 symbols)</param>
        public void config_Set_FooterLine(string item, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(item);
            inputString.Append(value);

            string r = CustomCommand(43 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Set auto format
        /// </summary>
        /// <param name="option">'A'</param>
        /// <param name="offOn">'0' - forbid or '1' - allow auto format</param>
        public void config_Set_AutoFormat(string option, string offOn) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(offOn);

            string r = CustomCommand(43 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Set print density
        /// </summary>
        /// <param name="option">'D'</param>
        /// <param name="value">‘1’: Very light; ‘2’: Light; ‘3’: Normal; ‘4’: Dense; ‘5’: Very dense</param>
        public void config_Set_PrintDensity(string option, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(value);

            string r = CustomCommand(43 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Enable EUR
        /// </summary>
        /// <param name="option">'E'</param>
        /// <param name="on">'0' - forbid, '1' - allow</param>
        /// <param name="rate">exchange rate - 8 digits, 5 after decimal point</param>
        public void config_enable_EUR(string option, string on, string rate) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(on);
            inputString.Append(",");
            inputString.Append(rate);

            string r = CustomCommand(43 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Disable EUR
        /// </summary>
        /// <param name="option">'E'</param>
        /// <param name="off">'0' - forbid, '1' - allow</param>
        public void config_disable_EUR(string option, string off) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(off);

            string r = CustomCommand(43 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Set print logo
        /// </summary>
        /// <param name="option">'L'</param>
        /// <param name="offOn">'1' - Enable; '0' - disable</param>
        /// <param name="height">Logo height</param>
        public void config_Set_PrintLogo(string option, string offOn, string height) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(offOn);
            inputString.Append(",");
            inputString.Append(height);

            string r = CustomCommand(43 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Allow printing of department's name on receipt
        /// </summary>
        /// <param name="option">'N'</param>
        /// <param name="offOn">'1' - Enable; '0' - disable</param>
        public void config_Set_PrintDepartment(string option, string offOn) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(offOn);

            string r = CustomCommand(43 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Allow printing tax dds
        /// </summary>
        /// <param name="option">'T'</param>
        /// <param name="offOn">'1' - Enable; '0' - disable</param>
        public void config_Set_PrintTaxDDS(string option, string offOn) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(offOn);

            string r = CustomCommand(43 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Set cash drawer pulse
        /// </summary>
        /// <param name="option">'X'</param>
        /// <param name="offOn">'1' - Enable; '0' - disable</param>
        public void config_Set_CashDrawerPulse(string option, string offOn) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(offOn);

            string r = CustomCommand(43 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 61 - please check fiscal device documentation.
        /// <summary>Set date and time</summary>
        /// <param name="dateTime">Date and time in format: DD-MM-YY HH:MM[:SS]</param>
        public void config_Set_DateTime(string dateTime) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(dateTime);

            string r = CustomCommand(61 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 66 - please check fiscal device documentation.
        /// <summary>
        /// Set invoice range
        /// </summary>
        /// <param name="startValue">Start invoice value (up to 10 digits)</param>
        /// <param name="endValue">End invoice value (up to 10 digits)</param>
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

            string r = CustomCommand(66 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["valueStart"] = split[0];
            if(split.Length >= 2) 
                result["valueEnd"] = split[1];
            if(split.Length >= 3) 
                result["valueCurrent"] = split[2];
            return result;
        }

        // Command number(Dec): 85 - please check fiscal device documentation.
        /// <summary>
        /// Set additional payment name
        /// </summary>
        /// <param name="option">'N' - Payment 1;'C' - Payment 2;'D' - Payment 3;'I' - Payment 4;'J' - Payment 5;</param>
        /// <param name="additionalPaymentName">Name - up to 10 symbols</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>P(pass) or F(fail).</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_AdditionalPaymentName(string option, string additionalPaymentName) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(additionalPaymentName);

            string r = CustomCommand(85 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 87 - please check fiscal device documentation.
        /// <summary>
        /// Set department name
        /// </summary>
        /// <param name="departmentNumber">Department number -(1 ... 9)</param>
        /// <param name="taxGroup">Tax group, asociated with the department</param>
        /// <param name="textRow1">Description for department</param>
        public void config_Set_DepartmentName(string departmentNumber, string taxGroup, string textRow1) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(departmentNumber);
            inputString.Append(",");
            inputString.Append(taxGroup);
            inputString.Append(",");
            inputString.Append(textRow1);

            string r = CustomCommand(87 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 87 - please check fiscal device documentation.
        /// <summary>
        /// Set department name
        /// </summary>
        /// <param name="departmentNumber">Department number -(1 ... 9)</param>
        /// <param name="taxGroup">Tax group, asociated with the department</param>
        /// <param name="textRow1">Description for department</param>
        /// <param name="textRow2">Description for department - second row</param>
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

            string r = CustomCommand(87 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 101 - please check fiscal device documentation.
        /// <summary>
        /// Set operator's password
        /// </summary>
        /// <param name="operatorCode">Operator's code (1 ... 30)</param>
        /// <param name="oldPassword">Old password (1 ... 8)</param>
        /// <param name="newPassword">New password (1 ... 8)</param>
        public void config_Set_OperatorPassword(string operatorCode, string oldPassword, string newPassword) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorCode);
            inputString.Append(",");
            inputString.Append(oldPassword);
            inputString.Append(",");
            inputString.Append(newPassword);

            string r = CustomCommand(101 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 102 - please check fiscal device documentation.
        /// <summary>
        /// Set operator's name
        /// </summary>
        /// <param name="operatorCode">Operator's code (1 ... 30)</param>
        /// <param name="password">Password (1 ... 8)</param>
        /// <param name="operatorName">Operator's name (up to 10 symbols)</param>
        public void config_Set_OperatorName(string operatorCode, string password, string operatorName) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorCode);
            inputString.Append(",");
            inputString.Append(password);
            inputString.Append(",");
            inputString.Append(operatorName);

            string r = CustomCommand(102 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Get print options
        /// I[0...7] - Reading header and footer print option
        /// IA - Reading invoice setting
        /// IB - Reading barcode size
        /// ID - Reading print density
        /// IE - Reading EUR settings
        /// IL - Reading settings of logo print
        /// IN - Reading department print settings
        /// IT - DDS settings
        /// IX - Reading drawer pulse settings
        /// </summary>
        /// <param name="option">'I' + 0...7, A, B, D, E, L, N, T, X</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description>Depends on the input.</description>
        /// </item> 
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_PrintOption(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(43 , inputString.ToString());
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
        /// <param name="startDate">Start date in format DDMMYY /6 bytes/</param>
        /// <param name="endDate">End date in format DDMMYY /6 bytes/</param>
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

            string r = CustomCommand(50 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["aA"] = split[1];
            if(split.Length >= 3) 
                result["bB"] = split[2];
            if(split.Length >= 4) 
                result["cC"] = split[3];
            if(split.Length >= 5) 
                result["dD"] = split[4];
            if(split.Length >= 6) 
                result["eE"] = split[5];
            if(split.Length >= 7) 
                result["fF"] = split[6];
            if(split.Length >= 8) 
                result["gG"] = split[7];
            if(split.Length >= 9) 
                result["hH"] = split[8];
            if(split.Length >= 10) 
                result["dDMMYY"] = split[9];
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

            string r = CustomCommand(62 , inputString.ToString());
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

            string r = CustomCommand(62 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["day"] = split[0];
            if(split.Length >= 2) 
                result["month"] = split[1];
            if(split.Length >= 3) 
                result["year"] = split[2];
            if(split.Length >= 4) 
                result["hour"] = split[3];
            if(split.Length >= 5) 
                result["minute"] = split[4];
            if(split.Length >= 6) 
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
        /// </returns>
        public Dictionary<string, string> info_Get_LastFiscRecord() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(64 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["lastFRecordNumber"] = split[1];
            if(split.Length >= 3) 
                result["taxA"] = split[2];
            if(split.Length >= 4) 
                result["taxB"] = split[3];
            if(split.Length >= 5) 
                result["taxC"] = split[4];
            if(split.Length >= 6) 
                result["taxD"] = split[5];
            if(split.Length >= 7) 
                result["taxE"] = split[6];
            if(split.Length >= 8) 
                result["taxF"] = split[7];
            if(split.Length >= 9) 
                result["taxG"] = split[8];
            if(split.Length >= 10) 
                result["taxH"] = split[9];
            if(split.Length >= 11) 
                result["date"] = split[10];
            return result;
        }

        // Command number(Dec): 65 - please check fiscal device documentation.
        /// <summary>
        /// Get additional daily information - Turnover for tax groups
        /// </summary>
        /// <param name="option">'0' - Turnover for tax groups</param>
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
        public Dictionary<string, string> info_Get_AdditionalDailyInfo_04(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(65 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["sumA"] = split[0];
            if(split.Length >= 2) 
                result["sumB"] = split[1];
            if(split.Length >= 3) 
                result["sumC"] = split[2];
            if(split.Length >= 4) 
                result["sumD"] = split[3];
            if(split.Length >= 5) 
                result["sumE"] = split[4];
            if(split.Length >= 6) 
                result["sumF"] = split[5];
            if(split.Length >= 7) 
                result["sumG"] = split[6];
            if(split.Length >= 8) 
                result["sumH"] = split[7];
            return result;
        }

        // Command number(Dec): 65 - please check fiscal device documentation.
        /// <summary>
        /// Get additional daily information - Daily counters and sums (1/3 part)
        /// </summary>
        /// <param name="option">'2' - Daily counters and sums (1/3 part)</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>clientsCount</term>
        /// <description>Clients count (4 bytes)</description>
        /// </item>
        /// <item>
        /// <term>sumSold</term>
        /// <description>Sum of sold items (12 bytes with sign)</description>
        /// </item>
        /// <item>
        /// <term>surchargesSum</term>
        /// <description>Sum of surcharges (12 bytes with sign)</description>
        /// </item>
        /// <item>
        /// <term>surchargesCount</term>
        /// <description>Surcharges count (4 bytes)</description>
        /// </item>
        /// <item>
        /// <term>discountsSum</term>
        /// <description>Sum of discounts (12 bytes with sign)</description>
        /// </item>
        /// <item>
        /// <term>discountsCount</term>
        /// <description>Discounts count (4 bytes)</description>
        /// </item>
        /// <item>
        /// <term>voidsSum</term>
        /// <description>Sum of void operations (12 bytes with sign)</description>
        /// </item>
        /// <item>
        /// <term>voidsCount</term>
        /// <description>Void operations count (4 bytes)</description>
        /// </item>
        /// <item>
        /// <term>canceledSum</term>
        /// <description>Sum of canceled operations (12 bytes with sign)</description>
        /// </item>
        /// <item>
        /// <term>canceledCount</term>
        /// <description>Canceled operations count (4 bytes)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_AdditionalDailyInfo_05(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(65 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["clientsCount"] = split[0];
            if(split.Length >= 2) 
                result["sumSold"] = split[1];
            if(split.Length >= 3) 
                result["surchargesSum"] = split[2];
            if(split.Length >= 4) 
                result["surchargesCount"] = split[3];
            if(split.Length >= 5) 
                result["discountsSum"] = split[4];
            if(split.Length >= 6) 
                result["discountsCount"] = split[5];
            if(split.Length >= 7) 
                result["voidsSum"] = split[6];
            if(split.Length >= 8) 
                result["voidsCount"] = split[7];
            if(split.Length >= 9) 
                result["canceledSum"] = split[8];
            if(split.Length >= 10) 
                result["canceledCount"] = split[9];
            return result;
        }

        // Command number(Dec): 65 - please check fiscal device documentation.
        /// <summary>
        /// Get additional daily information - Daily counters and sums (2/3 part)
        /// </summary>
        /// <param name="option">'3' - Daily counters and sums (2/3 part)</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>foreignSum</term>
        /// <description>Sum paid with foreign currency (12 bytes with sign)</description>
        /// </item>
        /// <item>
        /// <term>localforeignSum</term>
        /// <description>Sum paid with local currency (12 bytes with sign)</description>
        /// </item>
        /// <item>
        /// <term>cashinSum</term>
        /// <description>Cash in sum (12 bytes with sign)</description>
        /// </item>
        /// <item>
        /// <term>cashinCount</term>
        /// <description>Cash in count (4 bytes)</description>
        /// </item>
        /// <item>
        /// <term>cashoutSum</term>
        /// <description>Cash out sum (12 bytes with sign)</description>
        /// </item>
        /// <item>
        /// <term>cashoutCount</term>
        /// <description>Cash out count (4 bytes)</description>
        /// </item>
        /// <item>
        /// <term>cashinForeignSum</term>
        /// <description>Cash in sum in foreign currency (12 bytes with sign)</description>
        /// </item>
        /// <item>
        /// <term>cashinForeignCount</term>
        /// <description>Cash in count in foreign currency (4 bytes)</description>
        /// </item>
        /// <item>
        /// <term>cashoutForeignSum</term>
        /// <description>Cash out sum in foreign currency (12 bytes with sign)</description>
        /// </item>
        /// <item>
        /// <term>cashoutForeignCount</term>
        /// <description>Cash out count in foreign currency (4 bytes)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_AdditionalDailyInfo_06(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(65 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["foreignSum"] = split[0];
            if(split.Length >= 2) 
                result["localforeignSum"] = split[1];
            if(split.Length >= 3) 
                result["cashinSum"] = split[2];
            if(split.Length >= 4) 
                result["cashinCount"] = split[3];
            if(split.Length >= 5) 
                result["cashoutSum"] = split[4];
            if(split.Length >= 6) 
                result["cashoutCount"] = split[5];
            if(split.Length >= 7) 
                result["cashinForeignSum"] = split[6];
            if(split.Length >= 8) 
                result["cashinForeignCount"] = split[7];
            if(split.Length >= 9) 
                result["cashoutForeignSum"] = split[8];
            if(split.Length >= 10) 
                result["cashoutForeignCount"] = split[9];
            return result;
        }

        // Command number(Dec): 65 - please check fiscal device documentation.
        /// <summary>
        /// Get additional daily information - Daily counters and sums (3/3 part)
        /// </summary>
        /// <param name="option">'4' - Daily counters and sums (3/3 part)</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>sumPaymentType_0</term>
        /// <description>Sum pay of payment type 0 (12 bytes with sign)</description>
        /// </item>
        /// <item>
        /// <term>sumPaymentType_1</term>
        /// <description>Sum pay of payment type 1 (12 bytes with sign)</description>
        /// </item>
        /// <item>
        /// <term>sumPaymentType_2</term>
        /// <description>Sum pay of payment type 2 (12 bytes with sign)</description>
        /// </item>
        /// <item>
        /// <term>sumPaymentType_3</term>
        /// <description>Sum pay of payment type 3 (12 bytes with sign)</description>
        /// </item>
        /// <item>
        /// <term>sumPaymentType_4</term>
        /// <description>Sum pay of payment type 4 (12 bytes with sign)</description>
        /// </item>
        /// <item>
        /// <term>sumPaymentType_5</term>
        /// <description>Sum pay of payment type 5 (12 bytes with sign)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_AdditionalDailyInfo_07(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(65 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["sumPaymentType_0"] = split[0];
            if(split.Length >= 2) 
                result["sumPaymentType_1"] = split[1];
            if(split.Length >= 3) 
                result["sumPaymentType_2"] = split[2];
            if(split.Length >= 4) 
                result["sumPaymentType_3"] = split[3];
            if(split.Length >= 5) 
                result["sumPaymentType_4"] = split[4];
            if(split.Length >= 6) 
                result["sumPaymentType_5"] = split[5];
            return result;
        }

        // Command number(Dec): 65 - please check fiscal device documentation.
        /// <summary>
        /// Get additional daily information - Storno DDS for tax groups
        /// </summary>
        /// <param name="option">'6' - Storno DDS for tax groups</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>reverseturnoverTaxA</term>
        /// <description>Storno DDS for tax group A</description>
        /// </item>
        /// <item>
        /// <term>reverseturnoverTaxB</term>
        /// <description>Storno DDS for tax group B</description>
        /// </item>
        /// <item>
        /// <term>reverseturnoverTaxC</term>
        /// <description>Storno DDS for tax group C</description>
        /// </item>
        /// <item>
        /// <term>reverseturnoverTaxD</term>
        /// <description>Storno DDS for tax group D</description>
        /// </item>
        /// <item>
        /// <term>reverseturnoverTaxE</term>
        /// <description>Storno DDS for tax group E</description>
        /// </item>
        /// <item>
        /// <term>reverseturnoverTaxF</term>
        /// <description>Storno DDS for tax group F</description>
        /// </item>
        /// <item>
        /// <term>reverseturnoverTaxG</term>
        /// <description>Storno DDS for tax group G</description>
        /// </item>
        /// <item>
        /// <term>reverseturnoverTaxH</term>
        /// <description>Storno DDS for tax group H</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_AdditionalDailyInfo_08(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(65 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["reverseturnoverTaxA"] = split[0];
            if(split.Length >= 2) 
                result["reverseturnoverTaxB"] = split[1];
            if(split.Length >= 3) 
                result["reverseturnoverTaxC"] = split[2];
            if(split.Length >= 4) 
                result["reverseturnoverTaxD"] = split[3];
            if(split.Length >= 5) 
                result["reverseturnoverTaxE"] = split[4];
            if(split.Length >= 6) 
                result["reverseturnoverTaxF"] = split[5];
            if(split.Length >= 7) 
                result["reverseturnoverTaxG"] = split[6];
            if(split.Length >= 8) 
                result["reverseturnoverTaxH"] = split[7];
            return result;
        }

        // Command number(Dec): 65 - please check fiscal device documentation.
        /// <summary>
        ///  Get additional daily information - Storno turnover for tax groups
        /// </summary>
        /// <param name="option">'5' - Storno turnover for tax groups</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>reversevatTaxA</term>
        /// <description>Storno turnover for tax group A</description>
        /// </item>
        /// <item>
        /// <term>reversevatTaxB</term>
        /// <description>Storno turnover for tax group B</description>
        /// </item>
        /// <item>
        /// <term>reversevatTaxC</term>
        /// <description>Storno turnover for tax group C</description>
        /// </item>
        /// <item>
        /// <term>reversevatTaxD</term>
        /// <description>Storno turnover for tax group D</description>
        /// </item>
        /// <item>
        /// <term>reversevatTaxE</term>
        /// <description>Storno turnover for tax group E</description>
        /// </item>
        /// <item>
        /// <term>reversevatTaxF</term>
        /// <description>Storno turnover for tax group F</description>
        /// </item>
        /// <item>
        /// <term>reversevatTaxG</term>
        /// <description>Storno turnover for tax group G</description>
        /// </item>
        /// <item>
        /// <term>reversevatTaxH</term>
        /// <description>Storno turnover for tax group H</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_AdditionalDailyInfo_09(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(65 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["reversevatTaxA"] = split[0];
            if(split.Length >= 2) 
                result["reversevatTaxB"] = split[1];
            if(split.Length >= 3) 
                result["reversevatTaxC"] = split[2];
            if(split.Length >= 4) 
                result["reversevatTaxD"] = split[3];
            if(split.Length >= 5) 
                result["reversevatTaxE"] = split[4];
            if(split.Length >= 6) 
                result["reversevatTaxF"] = split[5];
            if(split.Length >= 7) 
                result["reversevatTaxG"] = split[6];
            if(split.Length >= 8) 
                result["reversevatTaxH"] = split[7];
            return result;
        }

        // Command number(Dec): 65 - please check fiscal device documentation.
        /// <summary>
        /// Get additional daily information - Storno sums for payments
        /// </summary>
        /// <param name="option">'7' - Storno sums for payments</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>reversesumPaymentType_0</term>
        /// <description>Strono sum for payment type 0 (12 bytes with sign)</description>
        /// </item>
        /// <item>
        /// <term>reversesumPaymentType_1</term>
        /// <description>Strono sum for payment type 1 (12 bytes with sign)</description>
        /// </item>
        /// <item>
        /// <term>reversesumPaymentType_2</term>
        /// <description>Strono sum for payment type 2 (12 bytes with sign)</description>
        /// </item>
        /// <item>
        /// <term>reversesumPaymentType_3</term>
        /// <description>Strono sum for payment type 3 (12 bytes with sign)</description>
        /// </item>
        /// <item>
        /// <term>reversesumPaymentType_4</term>
        /// <description>Strono sum for payment type 4 (12 bytes with sign)</description>
        /// </item>
        /// <item>
        /// <term>reversesumPaymentType_5</term>
        /// <description>Strono sum for payment type 5 (12 bytes with sign)</description>
        /// </item>
        /// <item>
        /// <term>reversesumForeign</term>
        /// <description>Storno sum payment of foreign currency (12 bytes with sign)</description>
        /// </item>
        /// <item>
        /// <term>reversesumLocalForeign</term>
        /// <description>Storno sum payment of local currency (12 bytes with sign)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_AdditionalDailyInfo_10(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(65 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["reversesumPaymentType_0"] = split[0];
            if(split.Length >= 2) 
                result["reversesumPaymentType_1"] = split[1];
            if(split.Length >= 3) 
                result["reversesumPaymentType_2"] = split[2];
            if(split.Length >= 4) 
                result["reversesumPaymentType_3"] = split[3];
            if(split.Length >= 5) 
                result["reversesumPaymentType_4"] = split[4];
            if(split.Length >= 6) 
                result["reversesumPaymentType_5"] = split[5];
            if(split.Length >= 7) 
                result["reversesumForeign"] = split[6];
            if(split.Length >= 8) 
                result["reversesumLocalForeign"] = split[7];
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

            string r = CustomCommand(66 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["valueStart"] = split[0];
            if(split.Length >= 2) 
                result["valueEnd"] = split[1];
            if(split.Length >= 3) 
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

            string r = CustomCommand(68 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["countLogical"] = split[0];
            if(split.Length >= 2) 
                result["countTotal"] = split[1];
            return result;
        }

        // Command number(Dec): 68 - please check fiscal device documentation.
        /// <summary>
        /// Get free fiscal memory records
        /// </summary>
        /// <param name="option">'R' - Information for daily closures map.\n
        /// It can be use only if the fiscal device has worked before N18 - 2018 and it works now without fiscal memory change.</param>
        /// <returns>
        /// Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description>4 parameters, separated with commas.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_FreeFMRecords_01(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(68 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["fAnswer"] = split[0];
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

            string r = CustomCommand(70 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["cashSum"] = split[1];
            if(split.Length >= 3) 
                result["servIn"] = split[2];
            if(split.Length >= 4) 
                result["servOut"] = split[3];
            return result;
        }

        // Command number(Dec): 71 - please check fiscal device documentation.
        /// <summary>
        /// Print diaglnostic information
        /// </summary>
        /// <returns>
        /// Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass or 'F' - failed</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Print_Diagnostic_0() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(71 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 71 - please check fiscal device documentation.
        /// <summary>
        /// Print diaglnostic information
        /// </summary>
        /// <param name="option">'0' - printed receipt contains software date and version, firmware checksum and current date and time.</param>
        /// <returns>
        /// Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass or 'F' - failed</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Print_Diagnostic_1(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(71 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 71 - please check fiscal device documentation.
        /// <summary>
        /// Print diagnostic information
        /// </summary>
        /// <param name="option">'1' - GPRS test</param>
        /// <returns>
        /// Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass or 'F' - failed</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_PrintInfo_GPRS(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(71 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 71 - please check fiscal device documentation.
        /// <summary>
        /// Print diagnostic information
        /// </summary>
        /// <param name="option">'5' - print information for tax terminal</param>
        /// <returns>
        /// Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass or 'F' - failed</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_PrintInfo_TaxTerminal(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(71 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 71 - please check fiscal device documentation.
        /// <summary>
        /// Get diagnostic information
        /// </summary>
        /// <param name="option">'6' - get tax terminal information</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass or 'F' - failed</description>
        /// </item>
        /// <item>
        /// <term>synhronized</term>
        /// <description>Tax terminal state: '1' - synchronized; '0' - not synchronized</description>
        /// </item>
        /// <item>
        /// <term>minutes</term>
        /// <description>Minutes to next attempt for synchronization (0 - 120)</description>
        /// </item>
        /// <item>
        /// <term>napSellXD</term>
        /// <description>Date and time from last confirmed sell document from NAP.(format: DD-MM-YY HH:mm:ss)</description>
        /// </item>
        /// <item>
        /// <term>napSellChN</term>
        /// <description>Number of last confirmed sell document from NAP</description>
        /// </item>
        /// <item>
        /// <term>sellForSend</term>
        /// <description>Number of document for sending to NAP</description>
        /// </item>
        /// <item>
        /// <term>sellErrDocNumber</term>
        /// <description>Number of sell document, that has an error sending to NAP</description>
        /// </item>
        /// <item>
        /// <term>sellErrCnt</term>
        /// <description>Count for error documents</description>
        /// </item>
        /// <item>
        /// <term>sellErrCode</term>
        /// <description>Error code received from NAP server, while sending "sellErrDocNumber"</description>
        /// </item>
        /// <item>
        /// <term>zChN</term>
        /// <description>Number of last send to NAP Z report</description>
        /// </item>
        /// <item>
        /// <term>zForSend</term>
        /// <description>Count of Z reports in a queue for sending to NAP</description>
        /// </item>
        /// <item>
        /// <term>zErrDocNumber</term>
        /// <description>Number of Z report, that has an error sending to NAP</description>
        /// </item>
        /// <item>
        /// <term>zErrCnt</term>
        /// <description>Count of attempts to send Z report</description>
        /// </item>
        /// <item>
        /// <term>zErrCode</term>
        /// <description>Error code received from NAP server, while sending "zErrDocNumber"</description>
        /// </item>
        /// <item>
        /// <term>napLastSentDate</term>
        /// <description>Date and time of last connection with NAP</description>
        /// </item>
        /// <item>
        /// <term>napLastErr</term>
        /// <description>Status of last connection with NAP (0 - it was successful)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_TaxTerminalInfo(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(71 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["synhronized"] = split[1];
            if(split.Length >= 3) 
                result["minutes"] = split[2];
            if(split.Length >= 4) 
                result["napSellXD"] = split[3];
            if(split.Length >= 5) 
                result["napSellChN"] = split[4];
            if(split.Length >= 6) 
                result["sellForSend"] = split[5];
            if(split.Length >= 7) 
                result["sellErrDocNumber"] = split[6];
            if(split.Length >= 8) 
                result["sellErrCnt"] = split[7];
            if(split.Length >= 9) 
                result["sellErrCode"] = split[8];
            if(split.Length >= 10) 
                result["zChN"] = split[9];
            if(split.Length >= 11) 
                result["zForSend"] = split[10];
            if(split.Length >= 12) 
                result["zErrDocNumber"] = split[11];
            if(split.Length >= 13) 
                result["zErrCnt"] = split[12];
            if(split.Length >= 14) 
                result["zErrCode"] = split[13];
            if(split.Length >= 15) 
                result["napLastSentDate"] = split[14];
            if(split.Length >= 16) 
                result["napLastErr"] = split[15];
            return result;
        }

        // Command number(Dec): 71 - please check fiscal device documentation.
        /// <summary>
        /// Get modem information
        /// </summary>
        /// <param name="option">'7' - Get modem information</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass or 'F' - failed</description>
        /// </item>
        /// <item>
        /// <term>sIM</term>
        /// <description>Sim card status (0-1) 1 - ready for work</description>
        /// </item>
        /// <item>
        /// <term>iMSI</term>
        /// <description>IMSI number of SIM card</description>
        /// </item>
        /// <item>
        /// <term>isReg</term>
        /// <description>Registration status (0-1) 1 - The modem is registered.</description>
        /// </item>
        /// <item>
        /// <term>wpOperator</term>
        /// <description>Mobile operator name</description>
        /// </item>
        /// <item>
        /// <term>signal</term>
        /// <description>Level of signal in percents (1-100)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_ModemInfo(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(71 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["sIM"] = split[1];
            if(split.Length >= 3) 
                result["iMSI"] = split[2];
            if(split.Length >= 4) 
                result["isReg"] = split[3];
            if(split.Length >= 5) 
                result["wpOperator"] = split[4];
            if(split.Length >= 6) 
                result["signal"] = split[5];
            return result;
        }

        // Command number(Dec): 71 - please check fiscal device documentation.
        /// <summary>
        /// Get information for flash memory
        /// </summary>
        /// <param name="option">'8' - Information for flash memory</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>'P' - pass or 'F' - failed</description>
        /// </item>
        /// <item>
        /// <term>deviceID</term>
        /// <description>Chip ID</description>
        /// </item>
        /// <item>
        /// <term>volumeSize</term>
        /// <description>Memory size</description>
        /// </item>
        /// <item>
        /// <term>validBlocks</term>
        /// <description>Blocks count.</description>
        /// </item>
        /// <item>
        /// <term>blockSize</term>
        /// <description>Block size</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_FlashMemoryInfo(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(71 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["deviceID"] = split[1];
            if(split.Length >= 3) 
                result["volumeSize"] = split[2];
            if(split.Length >= 4) 
                result["validBlocks"] = split[3];
            if(split.Length >= 5) 
                result["blockSize"] = split[4];
            return result;
        }

        // Command number(Dec): 74 - please check fiscal device documentation.
        /// <summary>
        /// Get statuses
        /// </summary>
        /// <param name="option">'W' - First waits for every buffers to be printed; 'X' - Not waits fiscal device</param>
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

            string r = CustomCommand(74 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');
             
            Dictionary<string, string> result = new Dictionary<string, string>();

            result["statusBytes"] = split[0];
            return result;
        }

        // Command number(Dec): 76 - please check fiscal device documentation.
        /// <summary>
        /// Fiscal transaction status
        /// </summary>
        /// <param name="option">Blank or 'T' - The command will return information about current state of amount due from the customer.</param>
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

            string r = CustomCommand(76 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["open"] = split[0];
            if(split.Length >= 2) 
                result["items"] = split[1];
            if(split.Length >= 3) 
                result["amount"] = split[2];
            if(split.Length >= 4) 
                result["tender"] = split[3];
            return result;
        }

        // Command number(Dec): 79 - please check fiscal device documentation.
        /// <summary>
        /// Print short fiscal memory report by date and time range
        /// </summary>
        /// <param name="fromDate">Start date (DDMMYY)</param>
        /// <param name="toDate">End date (DDMMYY)</param>
        public void info_Print_Short_FMReportByDTRange(string fromDate, string toDate) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(fromDate);
            inputString.Append(",");
            inputString.Append(toDate);

            string r = CustomCommand(79 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 79 - please check fiscal device documentation.
        /// <summary>
        /// Print short fiscal memory report by month
        /// </summary>
        /// <param name="startValue">month (format: MMYY)</param>
        public void info_Print_Short_MonthlyFMReport(string startValue) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(startValue);

            string r = CustomCommand(79 , inputString.ToString());
            CheckResult();


        }

        // Command number(Dec): 79 - please check fiscal device documentation.
        /// <summary>
        /// Print short fiscal memory report by year
        /// </summary>
        /// <param name="startValue">year (format: YY)</param>
        public void info_Print_Short_YearlyFMReport(string startValue) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(startValue);

            string r = CustomCommand(79 , inputString.ToString());
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

            string r = CustomCommand(83 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["multiplier"] = split[0];
            if(split.Length >= 2) 
                result["decimals"] = split[1];
            if(split.Length >= 3) 
                result["currencyName"] = split[2];
            if(split.Length >= 4) 
                result["enabledMask"] = split[3];
            if(split.Length >= 5) 
                result["taxA"] = split[4];
            if(split.Length >= 6) 
                result["taxB"] = split[5];
            if(split.Length >= 7) 
                result["taxC"] = split[6];
            if(split.Length >= 8) 
                result["taxD"] = split[7];
            if(split.Length >= 9) 
                result["taxE"] = split[8];
            if(split.Length >= 10) 
                result["taxF"] = split[9];
            if(split.Length >= 11) 
                result["taxG"] = split[10];
            if(split.Length >= 12) 
                result["taxH"] = split[11];
            return result;
        }

        // Command number(Dec): 85 - please check fiscal device documentation.
        /// <summary>
        /// Get additional payment names
        /// </summary>
        /// <param name="option">'N' - Payment 1; 'C' - Payment 2; 'D' - Payment 3; 'I' - Payment 4; 'J' - Payment 5</param>
        /// <returns>
        /// Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>paymentName</term>
        /// <description>Payment name (up to 10 symbols)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_AdditionalPaymentNames(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(85 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["paymentName"] = split[0];
            return result;
        }

        // Command number(Dec): 86 - please check fiscal device documentation.
        public Dictionary<string, string> info_Get_FMRecord_LastDateTime(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(86 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["dateTime"] = split[0];
            return result;
        }

        // Command number(Dec): 86 - please check fiscal device documentation.
        public Dictionary<string, string> info_Get_FMRecord_LastDateTime_01(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(86 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["day"] = split[0];
            if(split.Length >= 2) 
                result["month"] = split[1];
            if(split.Length >= 3) 
                result["year"] = split[2];
            if(split.Length >= 4) 
                result["hour"] = split[3];
            if(split.Length >= 5) 
                result["minute"] = split[4];
            if(split.Length >= 6) 
                result["second"] = split[5];
            return result;
        }

        // Command number(Dec): 86 - please check fiscal device documentation.
        public Dictionary<string, string> info_Get_Registration_DT_Num(string option, string number) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(number);

            string r = CustomCommand(86 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["day"] = split[0];
            if(split.Length >= 2) 
                result["month"] = split[1];
            if(split.Length >= 3) 
                result["year"] = split[2];
            if(split.Length >= 4) 
                result["hour"] = split[3];
            if(split.Length >= 5) 
                result["minute"] = split[4];
            if(split.Length >= 6) 
                result["second"] = split[5];
            if(split.Length >= 7) 
                result["regNumber"] = split[6];
            return result;
        }

        // Command number(Dec): 86 - please check fiscal device documentation.
        public Dictionary<string, string> info_Get_DeRegistration_DT_Num(string option, string number) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(number);

            string r = CustomCommand(86 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["day"] = split[0];
            if(split.Length >= 2) 
                result["month"] = split[1];
            if(split.Length >= 3) 
                result["year"] = split[2];
            if(split.Length >= 4) 
                result["hour"] = split[3];
            if(split.Length >= 5) 
                result["minute"] = split[4];
            if(split.Length >= 6) 
                result["second"] = split[5];
            if(split.Length >= 7) 
                result["deregNumber"] = split[6];
            return result;
        }

        // Command number(Dec): 86 - please check fiscal device documentation.
        /// <summary>
        /// Get registration count
        /// </summary>
        /// <param name="option">'R'</param>
        /// Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>regCount</term>
        /// <description>Regestration count</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_Registration_Count(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(86 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["regCount"] = split[0];
            return result;
        }

        // Command number(Dec): 86 - please check fiscal device documentation.
        /// <summary>
        /// Get deregistration count
        /// </summary>
        /// <param name="option">'D'</param>
        /// Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>deregCount</term>
        /// <description>Deregestration count</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DeRegistration_Count(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(86 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["deregCount"] = split[0];
            return result;
        }

        // Command number(Dec): 88 - please check fiscal device documentation.
        /// <summary>
        /// Get department information
        /// </summary>
        /// <param name="departmentNumber">Department number (from 1 to 9)</param>
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

            string r = CustomCommand(88 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["taxGroup"] = split[1];
            if(split.Length >= 3) 
                result["recSales"] = split[2];
            if(split.Length >= 4) 
                result["recSum"] = split[3];
            if(split.Length >= 5) 
                result["totSales"] = split[4];
            if(split.Length >= 6) 
                result["totSum"] = split[5];
            if(split.Length >= 7) 
                result["line1"] = split[6];
            if(split.Length >= 8) 
                result["line2"] = split[7];
            return result;
        }

        // Command number(Dec): 88 - please check fiscal device documentation.
        /// <summary>
        /// Get department information
        /// </summary>
        /// <param name="departmentNumber">Department number (from 1 to 9)</param>
        /// <param name="stornoType">'1' - Storno from refund, '2' - Storno operator's error, '3' - tax base reduction</param>
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
        public Dictionary<string, string> info_Get_Department_StornoInfo(string departmentNumber, string stornoType) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(departmentNumber);
            inputString.Append(",");
            inputString.Append(stornoType);

            string r = CustomCommand(88 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["taxGroup"] = split[1];
            if(split.Length >= 3) 
                result["recSales"] = split[2];
            if(split.Length >= 4) 
                result["recSum"] = split[3];
            if(split.Length >= 5) 
                result["totSales"] = split[4];
            if(split.Length >= 6) 
                result["totSum"] = split[5];
            if(split.Length >= 7) 
                result["line1"] = split[6];
            if(split.Length >= 8) 
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

            inputString.Append("1");

            string r = CustomCommand(90 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["deviceName"] = split[0];
            if(split.Length >= 2) 
                result["firmware"] = split[1];
            if(split.Length >= 3) 
                result["checkSum"] = split[2];
            if(split.Length >= 4) 
                result["switches"] = split[3];
            if(split.Length >= 5) 
                result["serialNumber"] = split[4];
            if(split.Length >= 6) 
                result["fiscalMemoryNumber"] = split[5];
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
        public Dictionary<string, string> info_Get_TaxRates() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(97 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["taxA"] = split[0];
            if(split.Length >= 2) 
                result["taxB"] = split[1];
            if(split.Length >= 3) 
                result["taxC"] = split[2];
            if(split.Length >= 4) 
                result["taxD"] = split[3];
            if(split.Length >= 5) 
                result["taxE"] = split[4];
            if(split.Length >= 6) 
                result["taxF"] = split[5];
            if(split.Length >= 7) 
                result["taxG"] = split[6];
            if(split.Length >= 8) 
                result["taxH"] = split[7];
            return result;
        }

        // Command number(Dec): 99 - please check fiscal device documentation.
        /// <summary>
        /// Get EIK
        /// </summary>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>eikValue</term>
        /// <description>EIK as text</description>
        /// </item>
        /// <item>
        /// <term>eikName</term>
        /// <description>Description text before EIK.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_EIK() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(99 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["eikValue"] = split[0];
            if(split.Length >= 2) 
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
        /// <term>errorCode</term>
        /// <description>'P' - pass, 'F' - failed</description>
        /// </item>
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
        /// <item>
        /// <term>receiptType</term>
        /// <description>'0' - Fiscal; '1' - Storno operator's error; '2' - Storno refund; '3' - Storno tax base reduction</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_CurrentRecieptInfo() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(103 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["canVd"] = split[1];
            if(split.Length >= 3) 
                result["taxA"] = split[2];
            if(split.Length >= 4) 
                result["taxB"] = split[3];
            if(split.Length >= 5) 
                result["taxC"] = split[4];
            if(split.Length >= 6) 
                result["taxD"] = split[5];
            if(split.Length >= 7) 
                result["taxE"] = split[6];
            if(split.Length >= 8) 
                result["taxF"] = split[7];
            if(split.Length >= 9) 
                result["taxG"] = split[8];
            if(split.Length >= 10) 
                result["taxH"] = split[9];
            if(split.Length >= 11) 
                result["inv"] = split[10];
            if(split.Length >= 12) 
                result["invNum"] = split[11];
            if(split.Length >= 13) 
                result["receiptType"] = split[12];
            return result;
        }

        // Command number(Dec): 110 - please check fiscal device documentation.
        /// <summary>
        /// Get additional daily information
        /// </summary>
        /// <param name="option">'0' - form sales; '1' - from storno operations</param>
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
        /// <term>payment04</term>
        /// <description>Payed with NZOK</description>
        /// </item>
        /// <item>
        /// <term>payment05</term>
        /// <description>Payed with check</description>
        /// </item>
        /// <item>
        /// <term>payment06</term>
        /// <description>Payed with coupon</description>
        /// </item>
        /// <item>
        /// <term>resrvl1</term>
        /// <description>Always 0</description>
        /// </item>
        /// <item>
        /// <term>resrvl2</term>
        /// <description>Always 0</description>
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
        public Dictionary<string, string> info_Get_AdditionalDailyInfo_11(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(110 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["cash"] = split[0];
            if(split.Length >= 2) 
                result["credit"] = split[1];
            if(split.Length >= 3) 
                result["debit"] = split[2];
            if(split.Length >= 4) 
                result["payment04"] = split[3];
            if(split.Length >= 5) 
                result["payment05"] = split[4];
            if(split.Length >= 6) 
                result["payment06"] = split[5];
            if(split.Length >= 7) 
                result["resrvl1"] = split[6];
            if(split.Length >= 8) 
                result["resrvl2"] = split[7];
            if(split.Length >= 9) 
                result["closure"] = split[8];
            if(split.Length >= 10) 
                result["nextFReceiptNumber"] = split[9];
            return result;
        }

        // Command number(Dec): 112 - please check fiscal device documentation.
        /// <summary>
        /// Get operator's data
        /// </summary>
        /// <param name="wpOperator">Operator's number (1...30)</param>
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

            string r = CustomCommand(112 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["receiptsCount"] = split[0];
            if(split.Length >= 2) 
                result["salesCount"] = split[1];
            if(split.Length >= 3) 
                result["salesSum"] = split[2];
            if(split.Length >= 4) 
                result["discountsCount"] = split[3];
            if(split.Length >= 5) 
                result["discountsSum"] = split[4];
            if(split.Length >= 6) 
                result["surchargesCount"] = split[5];
            if(split.Length >= 7) 
                result["surchargesSum"] = split[6];
            if(split.Length >= 8) 
                result["voidsCount"] = split[7];
            if(split.Length >= 9) 
                result["voidsSum"] = split[8];
            if(split.Length >= 10) 
                result["operatorName"] = split[9];
            if(split.Length >= 11) 
                result["operatorPassword"] = split[10];
            return result;
        }

        // Command number(Dec): 112 - please check fiscal device documentation.
        /// <summary>
        /// Get operator's storno data
        /// </summary>
        /// <param name="wpOperator">Operator's number (1...30)</param>
        /// <param name="option">'1' - Storno operation information</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>reversalreceiptsCount</term>
        /// <description>Storno receipts count</description>
        /// </item>
        /// <item>
        /// <term>commonreversalCount</term>
        /// <description>Common reversal count</description>
        /// </item>
        /// <item>
        /// <term>commonreversalSum</term>
        /// <description>Common reversal sum</description>
        /// </item>
        /// <item>
        /// <term>strreturnCount</term>
        /// <description>Storno return count</description>
        /// </item>
        /// <item>
        /// <term>strreturnSum</term>
        /// <description>Storno return sum</description>
        /// </item>
        /// <item>
        /// <term>strerrorCount</term>
        /// <description>Storno "operator's error" count</description>
        /// </item>
        /// <item>
        /// <term>strerrorSum</term>
        /// <description>Storno "operator's error" sum</description>
        /// </item>
        /// <item>
        /// <term>strtaxbaseCount</term>
        /// <description>Storno "tax base reduction" count</description>
        /// </item>
        /// <item>
        /// <term>strtaxbaseSum</term>
        /// <description>Storno "tax base reduction" sum</description>
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
        public Dictionary<string, string> info_read_OperatorsInfo(string wpOperator, string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(wpOperator);
            inputString.Append(",");
            inputString.Append(option);

            string r = CustomCommand(112 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["reversalreceiptsCount"] = split[0];
            if(split.Length >= 2) 
                result["commonreversalCount"] = split[1];
            if(split.Length >= 3) 
                result["commonreversalSum"] = split[2];
            if(split.Length >= 4) 
                result["strreturnCount"] = split[3];
            if(split.Length >= 5) 
                result["strreturnSum"] = split[4];
            if(split.Length >= 6) 
                result["strerrorCount"] = split[5];
            if(split.Length >= 7) 
                result["strerrorSum"] = split[6];
            if(split.Length >= 8) 
                result["strtaxbaseCount"] = split[7];
            if(split.Length >= 9) 
                result["strtaxbaseSum"] = split[8];
            if(split.Length >= 10) 
                result["operatorName"] = split[9];
            if(split.Length >= 11) 
                result["operatorPassword"] = split[10];
            return result;
        }

        // Command number(Dec): 113 - please check fiscal device documentation.
        /// <summary>
        /// Get last document number
        /// </summary>
        /// <returns>
        /// Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>documentNumber</term>
        /// <description>Document number (7 digits)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_LastDocumentNumber() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(113 , inputString.ToString());
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
        /// <param name="closureNumber">Closure number</param>
        /// <param name="option">'0' - Information for active tax rates for given record</param>
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

            string r = CustomCommand(114 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["taxRecordNumber"] = split[1];
            if(split.Length >= 3) 
                result["decimalsCount"] = split[2];
            if(split.Length >= 4) 
                result["enabled"] = split[3];
            if(split.Length >= 5) 
                result["taxRateA"] = split[4];
            if(split.Length >= 6) 
                result["taxRateB"] = split[5];
            if(split.Length >= 7) 
                result["taxRateC"] = split[6];
            if(split.Length >= 8) 
                result["taxRateD"] = split[7];
            if(split.Length >= 9) 
                result["taxRateE"] = split[8];
            if(split.Length >= 10) 
                result["taxRateF"] = split[9];
            if(split.Length >= 11) 
                result["taxRateG"] = split[10];
            if(split.Length >= 12) 
                result["taxRateH"] = split[11];
            if(split.Length >= 13) 
                result["dateTime"] = split[12];
            return result;
        }

        // Command number(Dec): 114 - please check fiscal device documentation.
        /// <summary>
        /// Information about turnover for given record or period 
        /// </summary>
        /// <param name="closure1">Start closure number (1-1825)</param>
        /// <param name="option">'1' - Information about turnover for given record or period.</param>
        /// <param name="closure2">End closure number (1-1825)</param>
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

            string r = CustomCommand(114 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["closureNumber"] = split[1];
            if(split.Length >= 3) 
                result["receiptsCount"] = split[2];
            if(split.Length >= 4) 
                result["totTaxA"] = split[3];
            if(split.Length >= 5) 
                result["totTaxB"] = split[4];
            if(split.Length >= 6) 
                result["totTaxC"] = split[5];
            if(split.Length >= 7) 
                result["totTaxD"] = split[6];
            if(split.Length >= 8) 
                result["totTaxE"] = split[7];
            if(split.Length >= 9) 
                result["totTaxF"] = split[8];
            if(split.Length >= 10) 
                result["totTaxG"] = split[9];
            if(split.Length >= 11) 
                result["totTaxH"] = split[10];
            return result;
        }

        // Command number(Dec): 114 - please check fiscal device documentation.
        /// <summary>
        /// Information for NETO sums for given record or period
        /// </summary>
        /// <param name="closure1">Start closure number of fiscal memory (1-1825)</param>
        /// <param name="option">'2' - Information for NETO sums for given record or period</param>
        /// <param name="closure2">End closure number of fiscal memory (1-1825)</param>
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

            string r = CustomCommand(114 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["closureNumber"] = split[1];
            if(split.Length >= 3) 
                result["receiptsCount"] = split[2];
            if(split.Length >= 4) 
                result["nettoTaxA"] = split[3];
            if(split.Length >= 5) 
                result["nettoTaxB"] = split[4];
            if(split.Length >= 6) 
                result["nettoTaxC"] = split[5];
            if(split.Length >= 7) 
                result["nettoTaxD"] = split[6];
            if(split.Length >= 8) 
                result["nettoTaxE"] = split[7];
            if(split.Length >= 9) 
                result["nettoTaxF"] = split[8];
            if(split.Length >= 10) 
                result["nettoTaxG"] = split[9];
            if(split.Length >= 11) 
                result["nettoTaxH"] = split[10];
            return result;
        }

        // Command number(Dec): 114 - please check fiscal device documentation.
        /// <summary>
        /// Information for DDS (VAT) for given record or period
        /// </summary>
        /// <param name="closure1">Start closure number of fiscal memory (1-1825)</param>
        /// <param name="option">'3' - Information for DDS (VAT) for given record or period.</param>
        /// <param name="closure2">End closure number of fiscal memory (1-1825)</param>
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

            string r = CustomCommand(114 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["closureNumber"] = split[1];
            if(split.Length >= 3) 
                result["receiptsCount"] = split[2];
            if(split.Length >= 4) 
                result["vATTaxA"] = split[3];
            if(split.Length >= 5) 
                result["vATTaxB"] = split[4];
            if(split.Length >= 6) 
                result["vATTaxC"] = split[5];
            if(split.Length >= 7) 
                result["vATTaxD"] = split[6];
            if(split.Length >= 8) 
                result["vATTaxE"] = split[7];
            if(split.Length >= 9) 
                result["vATTaxF"] = split[8];
            if(split.Length >= 10) 
                result["vATTaxG"] = split[9];
            if(split.Length >= 11) 
                result["vATTaxH"] = split[10];
            return result;
        }

        // Command number(Dec): 114 - please check fiscal device documentation.
        /// <summary>
        /// Additional information for given record
        /// </summary>
        /// <param name="closure1">Closure number of fiscal memory (1-1825)</param>
        /// <param name="option"> '4' - Additional information for given record</param>
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

            string r = CustomCommand(114 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["closureNumber"] = split[1];
            if(split.Length >= 3) 
                result["taxRecordNumber"] = split[2];
            if(split.Length >= 4) 
                result["resetRecordNumber"] = split[3];
            if(split.Length >= 5) 
                result["kLENNumber"] = split[4];
            if(split.Length >= 6) 
                result["dateTime"] = split[5];
            return result;
        }

        // Command number(Dec): 114 - please check fiscal device documentation.
        /// <summary>
        /// Information for given number for DDS (VAT) set
        /// </summary>
        /// <param name="closure1">Closure number for given period (1 - 1825)</param>
        /// <param name="option"> '5' - Information for given number for DDS (VAT) set.</param>
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

            string r = CustomCommand(114 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["decimalsCount"] = split[1];
            if(split.Length >= 3) 
                result["enabled"] = split[2];
            if(split.Length >= 4) 
                result["taxRateA"] = split[3];
            if(split.Length >= 5) 
                result["taxRateB"] = split[4];
            if(split.Length >= 6) 
                result["taxRateC"] = split[5];
            if(split.Length >= 7) 
                result["taxRateD"] = split[6];
            if(split.Length >= 8) 
                result["taxRateE"] = split[7];
            if(split.Length >= 9) 
                result["taxRateF"] = split[8];
            if(split.Length >= 10) 
                result["taxRateG"] = split[9];
            if(split.Length >= 11) 
                result["taxRateH"] = split[10];
            if(split.Length >= 12) 
                result["dateTime"] = split[11];
            return result;
        }

        // Command number(Dec): 114 - please check fiscal device documentation.
        /// <summary>
        /// Information for turnover from storno (for given record or period)
        /// </summary>
        /// <param name="closure1">Start closure number (1-1825)</param>
        /// <param name="option">'7' - Information for turnover from storno (for given record or period)</param>
        /// <param name="closure2">End closure number (1-1825)</param>
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
        /// <term>closureCount</term>
        /// <description>Closure count for given period.</description>
        /// </item>
        /// <item>
        /// <term>totA</term>
        /// <description> Storno turnover for tax group A.</description>
        /// </item>
        /// <item>
        /// <term>totB</term>
        /// <description>Storno turnover for tax group B.</description>
        /// </item>
        /// <item>
        /// <term>totC</term>
        /// <description>Storno turnover for tax group C.</description>
        /// </item>
        /// <item>
        /// <term>totD</term>
        /// <description>Storno turnover for tax group D.</description>
        /// </item>
        /// <item>
        /// <term>totE</term>
        /// <description>Storno turnover for tax group E.</description>
        /// </item>
        /// <item>
        /// <term>totF</term>
        /// <description>Storno turnover for tax group F.</description>
        /// </item>
        /// <item>
        /// <term>totG</term>
        /// <description>Storno turnover for tax group G.</description>
        /// </item>
        /// <item>
        /// <term>totH</term>
        /// <description>Storno turnover for tax group H.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_ReversalTurnover(string closure1, string option, string closure2) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(closure1);
            inputString.Append(",");
            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(closure2);

            string r = CustomCommand(114 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["closureCount"] = split[1];
            if(split.Length >= 3) 
                result["totA"] = split[2];
            if(split.Length >= 4) 
                result["totB"] = split[3];
            if(split.Length >= 5) 
                result["totC"] = split[4];
            if(split.Length >= 6) 
                result["totD"] = split[5];
            if(split.Length >= 7) 
                result["totE"] = split[6];
            if(split.Length >= 8) 
                result["totF"] = split[7];
            if(split.Length >= 9) 
                result["totG"] = split[8];
            if(split.Length >= 10) 
                result["totH"] = split[9];
            return result;
        }

        // Command number(Dec): 114 - please check fiscal device documentation.
        /// <summary>
        /// Information for NETO sums from storno (for given record or period)
        /// </summary>
        /// <param name="closure1">Start closure number (1-1825)</param>
        /// <param name="option">'8' - Information for NETO sums from storno (for given record or period).</param>
        /// <param name="closure2">End closure number (1-1825)</param>
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
        /// <term>closureCount</term>
        /// <description>Closure count for given period.</description>
        /// </item>
        /// <item>
        /// <term>totA</term>
        /// <description> Netto storno turnover for tax group A.</description>
        /// </item>
        /// <item>
        /// <term>totB</term>
        /// <description>Netto storno turnover for tax group B.</description>
        /// </item>
        /// <item>
        /// <term>totC</term>
        /// <description>Netto storno turnover for tax group C.</description>
        /// </item>
        /// <item>
        /// <term>totD</term>
        /// <description>Netto storno turnover for tax group D.</description>
        /// </item>
        /// <item>
        /// <term>totE</term>
        /// <description>Netto storno turnover for tax group E.</description>
        /// </item>
        /// <item>
        /// <term>totF</term>
        /// <description>Netto storno turnover for tax group F.</description>
        /// </item>
        /// <item>
        /// <term>totG</term>
        /// <description>Netto storno turnover for tax group G.</description>
        /// </item>
        /// <item>
        /// <term>totH</term>
        /// <description>Netto storno turnover for tax group H.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_ReversalNetoSums(string closure1, string option, string closure2) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(closure1);
            inputString.Append(",");
            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(closure2);

            string r = CustomCommand(114 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["closureCount"] = split[1];
            if(split.Length >= 3) 
                result["totA"] = split[2];
            if(split.Length >= 4) 
                result["totB"] = split[3];
            if(split.Length >= 5) 
                result["totC"] = split[4];
            if(split.Length >= 6) 
                result["totD"] = split[5];
            if(split.Length >= 7) 
                result["totE"] = split[6];
            if(split.Length >= 8) 
                result["totF"] = split[7];
            if(split.Length >= 9) 
                result["totG"] = split[8];
            if(split.Length >= 10) 
                result["totH"] = split[9];
            return result;
        }

        // Command number(Dec): 114 - please check fiscal device documentation.
        /// <summary>
        /// Information for VAT charged from storno (for given record or period)
        /// </summary>
        /// <param name="closure1">Start closure number (1-1825)</param>
        /// <param name="option"> '9' - Information for VAT charged from storno (for given record or period)</param>
        /// <param name="closure2">End closure number (1-1825)</param>
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
        /// <term>closureCount</term>
        /// <description>Closure count for given period.</description>
        /// </item>
        /// <item>
        /// <term>totA</term>
        /// <description>Storno VAT (DDS) for tax group A.</description>
        /// </item>
        /// <item>
        /// <term>totB</term>
        /// <description>Storno VAT (DDS) for tax group B.</description>
        /// </item>
        /// <item>
        /// <term>totC</term>
        /// <description>Storno VAT (DDS) for tax group C.</description>
        /// </item>
        /// <item>
        /// <term>totD</term>
        /// <description>Storno VAT (DDS) for tax group D.</description>
        /// </item>
        /// <item>
        /// <term>totE</term>
        /// <description>Storno VAT (DDS) for tax group E.</description>
        /// </item>
        /// <item>
        /// <term>totF</term>
        /// <description>Storno VAT (DDS) for tax group F.</description>
        /// </item>
        /// <item>
        /// <term>totG</term>
        /// <description>Storno VAT (DDS) for tax group G.</description>
        /// </item>
        /// <item>
        /// <term>totH</term>
        /// <description>Storno VAT (DDS) for tax group H.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_ReversalVATSums(string closure1, string option, string closure2) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(closure1);
            inputString.Append(",");
            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(closure2);

            string r = CustomCommand(114 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["closureCount"] = split[1];
            if(split.Length >= 3) 
                result["totA"] = split[2];
            if(split.Length >= 4) 
                result["totB"] = split[3];
            if(split.Length >= 5) 
                result["totC"] = split[4];
            if(split.Length >= 6) 
                result["totD"] = split[5];
            if(split.Length >= 7) 
                result["totE"] = split[6];
            if(split.Length >= 8) 
                result["totF"] = split[7];
            if(split.Length >= 9) 
                result["totG"] = split[8];
            if(split.Length >= 10) 
                result["totH"] = split[9];
            return result;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Reading electronic journal (EJ) by date and time
        /// </summary>
        /// <param name="subcommand01">'R' - EJ read</param>
        /// <param name="documentType">'A' - document type (ALL documets)</param>
        /// <param name="fromDateTime">Start date and time (format: DDMMYY[hhmmss])</param>
        /// <param name="toDateTime">End date and time (format: DDMMYY[hhmmss])</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - Command is declined.
        /// </description>
        /// </item>
        /// <item>
        /// <term>fRow</term>
        /// <description>Text from first line from EJ reading.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_Set_DocsRange_ByDateTime(string subcommand01, string documentType, string fromDateTime, string toDateTime) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(subcommand01);
            inputString.Append(documentType);
            inputString.Append(fromDateTime);
            inputString.Append(",");
            inputString.Append(toDateTime);

            string r = CustomCommand(119 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(new[] { ',' },2);

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["fAnswer"] = split[0];
            if(split.Length >= 2) 
                result["fRow"] = split[1];
            return result;
        }

        // Command number(Dec): 119 - please check fiscal device documentation.
        /// <summary>
        /// Reading next line electronic journal (EJ) text
        /// </summary>
        /// <param name="subcommand01">'N' - Read next line of EJ</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - Command is declined.
        /// </description>
        /// </item>
        /// <item>
        /// <term>fRow</term>
        /// <description>Text line from EJ reading.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_Get_NextTextRow(string subcommand01) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(subcommand01);

            string r = CustomCommand(119 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(new[] { ',' },2);

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["fAnswer"] = split[0];
            if(split.Length >= 2) 
                result["fRow"] = split[1];
            return result;
        }

        // Command number(Dec): 124 - please check fiscal device documentation.
        /// <summary>
        /// Find range by date and time
        /// </summary>
        /// <param name="option">'D' - Search in EJ by  date and time</param>
        /// <param name="documentType">Document type: '0' - All documents; '1' - Fiscal receipts(all fiscal receipts, sells, storno, invoice, storno invoice,daily Z report); \n
        /// '2' - Daily Z report; '3' - Cash in/out; '4' - Daily X report; '5' - Non fiscal receipts; '6' - Invoices; '7' - Storno fiscal receipts; '8' - Storno invoices.</param>
        /// <param name="fromDateTime">Start date and time (format: DDMMYY[hhmmss])</param>
        /// <param name="toDateTime">End date and time (format: DDMMYY[hhmmss])</param>
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
        /// <term>firstDocumentNumber</term>
        /// <description>Number of first found document (1 - 9999999).</description>
        /// </item>
        /// <item>
        /// <term>lastDocumentNumber</term>
        /// <description>Number of last found document (1 - 9999999).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_FindRange_ByDateTime(string option, string documentType, string fromDateTime, string toDateTime) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(documentType);
            inputString.Append(",");
            inputString.Append(fromDateTime);
            inputString.Append(",");
            inputString.Append(toDateTime);

            string r = CustomCommand(124 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["firstDocumentNumber"] = split[1];
            if(split.Length >= 3) 
                result["lastDocumentNumber"] = split[2];
            return result;
        }

        // Command number(Dec): 124 - please check fiscal device documentation.
        /// <summary>
        /// Find range by Z reports
        /// </summary>
        /// <param name="option">'Z' - find range by Z reports</param>
        /// <param name="documentType">Document type: '0' - All documents; '1' - Fiscal receipts(all fiscal receipts, sells, storno, invoice, storno invoice,daily Z report)\n
        /// '2' - Daily Z report; '3' - Cash in/out; '4' - Daily X report; '5' - Non fiscal receipts; '6' - Invoices;'7' - Storno fiscal receipts.
        ///'8' - Storno invoices.</param>
        /// <param name="startNumber">Start number of Z reports (1 - 1825)</param>
        /// <param name="endNumber">End number of Z reports (1 - 1825)</param>
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
        /// <term>firstDocumentNumber</term>
        /// <description>Number of first found document (1 - 9999999).</description>
        /// </item>
        /// <item>
        /// <term>lastDocumentNumber</term>
        /// <description>Number of last found document (1 - 9999999).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_FindRange_ByZReports(string option, string documentType, string startNumber, string endNumber) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(documentType);
            inputString.Append(",");
            inputString.Append(startNumber);
            inputString.Append(",");
            inputString.Append(endNumber);

            string r = CustomCommand(124 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["firstDocumentNumber"] = split[1];
            if(split.Length >= 3) 
                result["lastDocumentNumber"] = split[2];
            return result;
        }

        // Command number(Dec): 124 - please check fiscal device documentation.
        /// <summary>
        /// Get information
        /// </summary>
        /// <param name="option">'I' - Get information for EJ</param>
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
        /// <term>isValid</term>
        /// <description>Is SD card contains a valid EJ.Possible values: (0 - No, 1 - yes)</description>
        /// </item>
        /// <item>
        /// <term>isCurrent</term>
        /// <description>If SD EJ is current (value 1) for device or not(value - 0).</description>
        /// </item>
        /// <item>
        /// <term>idNumber</term>
        /// <description>serial number of the device (8 symbols - 2 alphabets and 6 digits).</description>
        /// </item>
        /// <item>
        /// <term>klenNumber</term>
        /// <description>EJ number for rhe device (0...100).</description>
        /// </item>
        /// <item>
        /// <term>activationDT</term>
        /// <description>Activation date and time (format: DD.MM.YYYY hh:mm:ss).</description>
        /// </item>
        /// <item>
        /// <term>sdSerialNumber</term>
        /// <description>SD card serial number (8 symbols).</description>
        /// </item>
        /// <item>
        /// <term>firstZReportNumber</term>
        /// <description>First Z report in this EJ (1...1825)</description>
        /// </item>
        /// <item>
        /// <term>lastZReportNumber</term>
        /// <description>Last Z report in this EJ (1 - 1825).</description>
        /// </item>
        /// <item>
        /// <term>firstDocumentNumber</term>
        /// <description>First document number in EJ.</description>
        /// </item>
        /// <item>
        /// <term>lastDocumentNumber</term>
        /// <description>Last document number in EJ.</description>
        /// </item>
        /// <item>
        /// <term>sizeTotal</term>
        /// <description>EJ size (SD card) in MBytes.Number up to 4000</description>
        /// </item>
        /// <item>
        /// <term>sizeUsed</term>
        /// <description>Used bytes in EJ (SD card) in MBytes.Number up to 4000</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_Get_Info(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(124 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["isValid"] = split[1];
            if(split.Length >= 3) 
                result["isCurrent"] = split[2];
            if(split.Length >= 4) 
                result["idNumber"] = split[3];
            if(split.Length >= 5) 
                result["klenNumber"] = split[4];
            if(split.Length >= 6) 
                result["activationDT"] = split[5];
            if(split.Length >= 7) 
                result["sdSerialNumber"] = split[6];
            if(split.Length >= 8) 
                result["firstZReportNumber"] = split[7];
            if(split.Length >= 9) 
                result["lastZReportNumber"] = split[8];
            if(split.Length >= 10) 
                result["firstDocumentNumber"] = split[9];
            if(split.Length >= 11) 
                result["lastDocumentNumber"] = split[10];
            if(split.Length >= 12) 
                result["sizeTotal"] = split[11];
            if(split.Length >= 13) 
                result["sizeUsed"] = split[12];
            return result;
        }

        // Command number(Dec): 125 - please check fiscal device documentation.
        /// <summary>
        /// Prepare document for reading
        /// </summary>
        /// <param name="option">'0' - Prepare document for reading</param>
        /// <param name="inDocNumber">Number of document for reading</param>
        /// <param name="inDocType">Document type: '0' - All documents; '1' - Fiscal receipts(all fiscal receipts, sells, storno, invoice, storno invoice,daily Z report)\n
        /// '2' - Daily Z report; '3' - Cash in/out; '4' - Daily X report; '5' - Non fiscal receipts; '6' - Invoices;'7' - Storno fiscal receipts.
        ///'8' - Storno invoices.</param>
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
        /// <term>documentNumber</term>
        /// <description>Document number</description>
        /// </item>
        /// <item>
        /// <term>documentDate</term>
        /// <description>Document date and time.</description>
        /// </item>
        /// <item>
        /// <term>documentType</term>
        /// <description>Document type: '1' - All documents; '2' - Fiscal receipts(all fiscal receipts, sells, storno, invoice, storno invoice,daily Z report)\n
        /// '3' - Daily Z report; '4' - Cash in/out; '5' - Daily X report; '6' - Non fiscal receipts; '7' - Invoices;'8' - Storno fiscal receipts.
        ///'9' - Storno invoices.</description>
        /// </item>
        /// <item>
        /// <term>zreportNumber</term>
        /// <description>Z report number.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_Prepare_Document(string option, string inDocNumber, string inDocType) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(inDocNumber);
            inputString.Append(",");
            inputString.Append(inDocType);

            string r = CustomCommand(125 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["documentNumber"] = split[1];
            if(split.Length >= 3) 
                result["documentDate"] = split[2];
            if(split.Length >= 4) 
                result["documentType"] = split[3];
            if(split.Length >= 5) 
                result["zreportNumber"] = split[4];
            return result;
        }

        // Command number(Dec): 125 - please check fiscal device documentation.
        /// <summary>
        /// Prepare document in range
        /// </summary>
        /// <param name="option">'0' - Prepare document for reading</param>
        /// <param name="inFromDocNumber">First number of document for reading</param>
        /// <param name="inDocType">Document type: '0' - All documents; '1' - Fiscal receipts(all fiscal receipts, sells, storno, invoice, storno invoice,daily Z report)\n
        /// '2' - Daily Z report; '3' - Cash in/out; '4' - Daily X report; '5' - Non fiscal receipts; '6' - Invoices;'7' - Storno fiscal receipts.
        ///'8' - Storno invoices.</param>
        /// <param name="inToDocNumber">Last number of document for reading</param>
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
        /// <term>documentNumber</term>
        /// <description>Document number</description>
        /// </item>
        /// <item>
        /// <term>documentDate</term>
        /// <description>Document date and time.</description>
        /// </item>
        /// <item>
        /// <term>documentType</term>
        /// <description>Document type: '1' - All documents; '2' - Fiscal receipts(all fiscal receipts, sells, storno, invoice, storno invoice,daily Z report)\n
        /// '3' - Daily Z report; '4' - Cash in/out; '5' - Daily X report; '6' - Non fiscal receipts; '7' - Invoices;'8' - Storno fiscal receipts.
        ///'9' - Storno invoices.</description>
        /// </item>
        /// <item>
        /// <term>zreportNumber</term>
        /// <description>Z report number.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_Prepare_DocumentInRange(string option, string inFromDocNumber, string inDocType, string inToDocNumber) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(inFromDocNumber);
            inputString.Append(",");
            inputString.Append(inDocType);
            inputString.Append(",");
            inputString.Append(inToDocNumber);

            string r = CustomCommand(125 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["documentNumber"] = split[1];
            if(split.Length >= 3) 
                result["documentDate"] = split[2];
            if(split.Length >= 4) 
                result["documentType"] = split[3];
            if(split.Length >= 5) 
                result["zreportNumber"] = split[4];
            return result;
        }

        // Command number(Dec): 125 - please check fiscal device documentation.
        /// <summary>
        /// Get text row
        /// </summary>
        /// <param name="option">'1' - get text row</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - Command is declined.
        /// </description>
        /// </item>
        /// <item>
        /// <term>fTextRow</term>
        /// <description>Text row (42 ascii symbols)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_Get_TextRow(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(125 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(new[] { ',' },2);

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["fAnswer"] = split[0];
            if(split.Length >= 2) 
                result["fTextRow"] = split[1];
            return result;
        }

        // Command number(Dec): 125 - please check fiscal device documentation.
        /// <summary>
        /// Read next line structured information
        /// </summary>
        /// <param name="option">'2' - Read next line structured information.</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - Command is declined.
        /// </description>
        /// </item>
        /// <item>
        /// <term>fData</term>
        /// <description>One row binary data, base64 encoding(128 symbols)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_Get_Data(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(125 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(new[] { ',' },2);

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["fAnswer"] = split[0];
            if(split.Length >= 2) 
                result["fData"] = split[1];
            return result;
        }

        // Command number(Dec): 125 - please check fiscal device documentation.
        /// <summary>
        /// Reading next line text/structured information
        /// </summary>
        /// <param name="option">'4' - Reading next line text/structured information</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>fAnswer</term>
        /// <description> 'P' -  Command is executed correctly, 
        /// 'F' - Command is declined.
        /// </description>
        /// </item>
        /// <item>
        /// <term>fType</term>
        /// <description> Data type: '1' - Text line (data - 42 ascii symbols); '2' - Data line (data base64 128 symbols)</description>
        /// </item>
        /// <item>
        /// <term>fData</term>
        /// <description>One row data/text</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_Get_StructData(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(125 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(new[] { ',' },3);

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["fAnswer"] = split[0];
            if(split.Length >= 2) 
                result["fType"] = split[1];
            if(split.Length >= 3) 
                result["fData"] = split[2];
            return result;
        }

        public Dictionary<string, string> klen_Get_StructInfo_01(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(125 , inputString.ToString());
            CheckResult();

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["fAnswer"] = r;
            return result;
        }

       
        public Dictionary<string, string> klen_Get_StructInfo_02(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(125 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["datafield01"] = split[1];
            if(split.Length >= 3) 
                result["datafield02"] = split[2];
            if(split.Length >= 4) 
                result["datafield03"] = split[3];
            if(split.Length >= 5) 
                result["datafield04"] = split[4];
            if(split.Length >= 6) 
                result["datafield05"] = split[5];
            if(split.Length >= 7) 
                result["datafield06"] = split[6];
            if(split.Length >= 8) 
                result["datafield07"] = split[7];
            if(split.Length >= 9) 
                result["datafield08"] = split[8];
            if(split.Length >= 10) 
                result["datafield09"] = split[9];
            if(split.Length >= 11) 
                result["datafield10"] = split[10];
            if(split.Length >= 12) 
                result["datafield11"] = split[11];
            if(split.Length >= 13) 
                result["datafield12"] = split[12];
            if(split.Length >= 14) 
                result["datafield13"] = split[13];
            if(split.Length >= 15) 
                result["datafield14"] = split[14];
            if(split.Length >= 16) 
                result["datafield15"] = split[15];
            if(split.Length >= 17) 
                result["datafield16"] = split[16];
            if(split.Length >= 18) 
                result["datafield17"] = split[17];
            return result;
        }

        // Command number(Dec): 125 - please check fiscal device documentation.
        /// <summary>
        /// Print document
        /// </summary>
        /// <param name="option">'3' - print document</param>
        /// <param name="inDocNumber">Number of document for reading</param>
        /// <param name="inDocType">Document type: '0' - All documents; '1' - Fiscal receipts(all fiscal receipts, sells, storno, invoice, storno invoice,daily Z report)\n
        /// '2' - Daily Z report; '3' - Cash in/out; '4' - Daily X report; '5' - Non fiscal receipts; '6' - Invoices;'7' - Storno fiscal receipts.
        ///'8' - Storno invoices.</param>
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
        /// <term>documentNumber</term>
        /// <description>Document number</description>
        /// </item>
        /// <item>
        /// <term>documentDate</term>
        /// <description>Document date and time.</description>
        /// </item>
        /// <item>
        /// <term>documentType</term>
        /// <description>Document type: '1' - All documents; '2' - Fiscal receipts(all fiscal receipts, sells, storno, invoice, storno invoice,daily Z report)\n
        /// '3' - Daily Z report; '4' - Cash in/out; '5' - Daily X report; '6' - Non fiscal receipts; '7' - Invoices;'8' - Storno fiscal receipts.
        ///'9' - Storno invoices.</description>
        /// </item>
        /// <item>
        /// <term>zreportNumber</term>
        /// <description>Z report number.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_Print_Document(string option, string inDocNumber, string inDocType) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append(",");
            inputString.Append(inDocNumber);
            inputString.Append(",");
            inputString.Append(inDocType);

            string r = CustomCommand(125 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["documentNumber"] = split[1];
            if(split.Length >= 3) 
                result["documentDate"] = split[2];
            if(split.Length >= 4) 
                result["documentType"] = split[3];
            if(split.Length >= 5) 
                result["zreportNumber"] = split[4];
            return result;
        }

        // Command number(Dec): 80 - please check fiscal device documentation.
        /// <summary>
        /// Play sound
        /// </summary>
        /// <param name="hz">Frequency (100...5000)</param>
        /// <param name="mSec">Time in milliseconds (50...2000)</param>
        public void other_Sound_Signal(string hz, string mSec) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(hz);
            inputString.Append(",");
            inputString.Append(mSec);

            string r = CustomCommand(80 , inputString.ToString());
            CheckResult();


        }

        public Dictionary<string, string> service_Fiscalization(string serialNumber) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(serialNumber);

            string r = CustomCommand(72 , inputString.ToString());
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
        /// <param name="inpMultiplier">From 0 to 3. Currently deactivated and not used.</param>
        /// <param name="inpDecimals">1 byte with value between 0 and 2, shows number of digits after decimal point.</param>
        /// <param name="inpCurrencyName">Currency name (up to 3 bytes)</param>
        /// <param name="inpEnabledMask">8 bytes with value 0 or 1, shows if a tax group A,B ... or H is enabled or not</param>
        /// <param name="inpTaxA">Tax rate A value</param>
        /// <param name="inpTaxB">Tax rate B value</param>
        /// <param name="inpTaxC">Tax rate C value</param>
        /// <param name="inpTaxD">Tax rate D value</param>
        /// <param name="inpTaxE">Tax rate E value</param>
        /// <param name="inpTaxF">Tax rate F value</param>
        /// <param name="inpTaxG">Tax rate G value</param>
        /// <param name="inpTaxH">Tax rate H value</param>
        /// <returns>
        /// Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>outMultiplier</term>
        /// <description> From 0 to 3. Currently deactivated and not used.</description>
        /// </item>
        /// <item>
        /// <term>outDecimals</term>
        /// <description>1 byte with value between 0 and 2, shows number of digits after decimal point.</description>
        /// </item>
        /// <item>
        /// <term>outCurrencyName</term>
        /// <description>Currency name (up to 3 bytes)</description>
        /// </item>
        /// <item>
        /// <term>outEnabledMask</term>
        /// <description>8 bytes with value 0 or 1, shows if a tax group A,B ... or H is enabled or not.</description>
        /// </item>
        /// <item>
        /// <term>outTaxA</term>
        /// <description>Tax rate A value.</description>
        /// </item>
        /// <item>
        /// <term>outTaxB</term>
        /// <description>Tax rate B value.</description>
        /// </item>
        /// <item>
        /// <term>outTaxC</term>
        /// <description>Tax rate C value.</description>
        /// </item>
        /// <item>
        /// <term>outTaxD</term>
        /// <description>Tax rate D value.</description>
        /// </item>
        /// <item>
        /// <term>outTaxE</term>
        /// <description>Tax rate E value.</description>
        /// </item>
        /// <item>
        /// <term>outTaxF</term>
        /// <description>Tax rate F value.</description>
        /// </item>
        /// <item>
        /// <term>outTaxG</term>
        /// <description>Tax rate G value.</description>
        /// </item>
        /// <item>
        /// <term>outTaxH</term>
        /// <description>Tax rate H value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> service_Set_DecimalsAndTaxRates(string inpMultiplier, string inpDecimals, string inpCurrencyName, string inpEnabledMask, string inpTaxA, string inpTaxB, string inpTaxC, string inpTaxD, string inpTaxE, string inpTaxF, string inpTaxG, string inpTaxH) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(inpMultiplier);
            inputString.Append(",");
            inputString.Append(inpDecimals);
            inputString.Append(",");
            inputString.Append(inpCurrencyName);
            inputString.Append(",");
            inputString.Append(inpEnabledMask);
            inputString.Append(",");
            inputString.Append(inpTaxA);
            inputString.Append(",");
            inputString.Append(inpTaxB);
            inputString.Append(",");
            inputString.Append(inpTaxC);
            inputString.Append(",");
            inputString.Append(inpTaxD);
            inputString.Append(",");
            inputString.Append(inpTaxE);
            inputString.Append(",");
            inputString.Append(inpTaxF);
            inputString.Append(",");
            inputString.Append(inpTaxG);
            inputString.Append(",");
            inputString.Append(inpTaxH);

            string r = CustomCommand(83 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["outMultiplier"] = split[0];
            if(split.Length >= 2) 
                result["outDecimals"] = split[1];
            if(split.Length >= 3) 
                result["outCurrencyName"] = split[2];
            if(split.Length >= 4) 
                result["outEnabledMask"] = split[3];
            if(split.Length >= 5) 
                result["outTaxA"] = split[4];
            if(split.Length >= 6) 
                result["outTaxB"] = split[5];
            if(split.Length >= 7) 
                result["outTaxC"] = split[6];
            if(split.Length >= 8) 
                result["outTaxD"] = split[7];
            if(split.Length >= 9) 
                result["outTaxE"] = split[8];
            if(split.Length >= 10) 
                result["outTaxF"] = split[9];
            if(split.Length >= 11) 
                result["outTaxG"] = split[10];
            if(split.Length >= 12) 
                result["outTaxH"] = split[11];
            return result;
        }

        // Command number(Dec): 89 - please check fiscal device documentation.
        /// <summary>
        /// Test of fiscal memory
        /// </summary>
        /// <param name="option">'T' - for saving in fiscal memory</param>
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
        /// <term>freeRecords</term>
        /// <description>Number of records left</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> service_Set_ProductionTestArea(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);

            string r = CustomCommand(89 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["freeRecords"] = split[1];
            return result;
        }

        
        public Dictionary<string, string> service_Set_EIK(string eikValue, string eikName) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(eikValue);
            inputString.Append(",");
            inputString.Append(eikName);

            string r = CustomCommand(98 , inputString.ToString());
            CheckResult();

            string[] split = r.Split(',');

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }
        //AI generated source code  -end

        public bool ItIs_SummerDT(string dateTime)
        {
            DateTime dt;
            
            if (dateTime.Length == 10)
            {
                dt = DateTime.ParseExact(dateTime, "ddMMyyHHmm", null); // in format ddMMyyHHmm
            }
            dt = DateTime.ParseExact(dateTime, "ddMMyyHHmmss", null);  // in format ddMMyyHHmmss 
            return dt.IsDaylightSavingTime();

        }

        private string[] ReadDocumentLines(int documentNum, out string docType)
        {
            docType = "";
            if (!device_Connected)
                throw new Exception("Fiscal device not connected");

            List<string> lines = new List<string>();

            // command 125 - Information from EJ,reading by document number.                                
            var result = klen_Prepare_Document("0", documentNum.ToString(), userDocumentType.ToString());
            if (result["errorCode"] != "P")
                throw new Exception("Reading document failed with error: " + result["errorCode"]);
            if (documentNum.ToString() != result["documentNumber"])
                throw new Exception("Document number mismatch");
            documentDateTime = result["documentDate"];
            docType = result["documentType"];

            do
            {
                var rowReslt = klen_Get_TextRow("1"); // read line by line as text                               
                if (rowReslt["fAnswer"] == "F") break;


                string[] rowsData = rowReslt["fTextRow"].Split(new string[] { "\r\n" }, StringSplitOptions.None);
                string newLine = rowsData[0].Replace("^", " ");

                if (newLine.Contains("О б щ а   с у м а"))
                {
                    var tr = newLine.Replace("О б щ а   с у м а", "");
                    var trr = tr.Replace(" ", "");
                    if (trr.Contains("#")) trr = trr.Substring(0, trr.Length - 1);
                    if (trr != "") sum = trr;
                }
                if (newLine.Contains(SerialNumber + " "))
                {
                    var part = newLine.Replace(SerialNumber, "");
                    fMNumFromReceipt = part.Replace(" ", "");
                }
                lines.AddRange(rowsData);
            } while (true);

            return lines.ToArray();
        }

        private SizeF DrawReceiptOnGraphics(Graphics gr, string documentNumber, Font font, string[] lines,string docType, bool calculate)
        {
            string qrcodeText = "";
            string dt = "", date = "", time = "";
            var receiptSize = new SizeF(0, 0);
            Brush textBrush = new SolidBrush(Color.Black);
            bool barcodeFlag = false;
            var maxCharsPerLine = 0;

            if (docType != "" && docType != "2" && docType != "5")
            {
                string receiptNumber = documentNumber.PadLeft(7, '0');
                // example 14-05-20 10:29:10 
                string[] result = documentDateTime.Split(' ');
                date = result[0];
                time = result[1];
                string[] resultDateSplit = date.Split('-');
                string day = resultDateSplit[0];
                string month = resultDateSplit[1];
                string year = (2000 + int.Parse(resultDateSplit[2])).ToString();
                string newDate = year + "-" + month + "-" + day;
                string newDT = newDate + "*" + time;

                qrcodeText = FiscalMemoryNumber + "*" + receiptNumber + "*" + newDT + "*" + sum;
            }
            gr.Clear(Color.White);
            foreach (var line in lines)
            {
                Image pictureBarcode = null;
                Font boldFont = new Font(font.Name, 16, FontStyle.Bold);
                if (docType != "" && docType != "2" && docType != "5")
                {
                    var qrCodeWriter = new BarcodeWriter<Bitmap>
                    {
                        Format = BarcodeFormat.QR_CODE,
                        Options = new ZXing.Common.EncodingOptions
                        {
                            Height = 20,
                            Margin = 0,
                        }
                    };
                    pictureBarcode = qrCodeWriter.Write(qrcodeText);
                }
                Image bgMap = Image.FromFile(Directory.GetCurrentDirectory() + "\\Resources\\BGmapS.bmp");
                var fiscBonSize = gr.MeasureString(line.Trim(), boldFont);


                string newLine = line.Replace("^", " ");
                if (!calculate)
                {
                    if (line.Contains("^"))
                    {
                        newLine = newLine.Substring(1);
                        float charSize = ((float)receiptSize.Width / (float)maxCharsPerLine);
                        if (newLine.Contains("Ф И С К А Л Е Н   Б О Н"))
                        {
                            if (docType != "" && docType != "2" && docType != "5")
                            {
                                gr.DrawImage(pictureBarcode, new PointF((receiptSize.Width - pictureBarcode.Width) / 2, receiptSize.Height));
                                receiptSize.Height += pictureBarcode.Height + 20; //bit of a space
                            }
                            PointF mapPoint = new PointF((receiptSize.Width - (bgMap.Width + fiscBonSize.Width)) / 2, receiptSize.Height);
                            gr.DrawImage(bgMap, mapPoint);
                        }

                        for (int bi = 0; bi < newLine.Length; bi++)
                        {
                            if (newLine[bi] != ' ')
                                gr.DrawString(newLine.Substring(bi, 1), boldFont, textBrush, (float)bi * charSize, receiptSize.Height);
                        }


                    }
                    else
                    {
                        //
                        if (line.Length > maxCharsPerLine)
                            maxCharsPerLine = line.Length;
                        //
                        gr.DrawString(newLine, font, textBrush, 0f, receiptSize.Height);
                    }

                }
                var textSize = gr.MeasureString(newLine, font);
                receiptSize.Height += textSize.Height + 5; //bit of a space
                receiptSize.Width = Math.Max(receiptSize.Width, textSize.Width);
                if (calculate && newLine.Contains("Ф И С К А Л Е Н   Б О Н"))
                {
                    if (docType != "" && docType != "2" && docType != "5")
                    {
                        receiptSize.Height += pictureBarcode.Height + 20; //bit of a space
                    }
                    receiptSize.Height += bgMap.Height + 30; //bit of a space
                    //receiptSize.Width = Math.Max(receiptSize.Width, bgMap.Width + fiscBonSize.Width);
                    //receiptSize.Width = Math.Max(receiptSize.Width, pictureBarcode.Width);
                }

            }

            return receiptSize;
        }

        private Image DrawReceipt(string[] lines, string documentNumber,string docType)
        {
            Font font = new Font("Courier New", 12);

            //calculate the size first using 1x1 bitmap
            Image img = new Bitmap(1, 1);
            Graphics gr = Graphics.FromImage(img);
            SizeF imgSize = DrawReceiptOnGraphics(gr, documentNumber, font, lines,docType, true);

            //now that we have it, make real image and draw to it
            img = new Bitmap((int)imgSize.Width, (int)imgSize.Height);
            gr = Graphics.FromImage(img);
            DrawReceiptOnGraphics(gr, documentNumber, font, lines, docType, false);

            return img;
        }

        public Image ReadAndDrawReceipt(string documentNumber, string serialNum, string fiscMemNum)
        {
            string docType = "";
            var lines = ReadDocumentLines(int.Parse(documentNumber),out docType);
            return DrawReceipt(lines, documentNumber,docType);

        }

        public void ReadAndDrawReceiptToFile(string FileName, string documentNumber, string serialNum, string fiscMemNum)
        {
            try
            {
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
