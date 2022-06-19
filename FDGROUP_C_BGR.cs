//
//  Fiscal Devices Group "C" - Bulgaria
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
    /// Fiscal Devices Group "C" - Bulgaria
    /// </summary>
    public class FDGROUP_C_BGR : FiscalPrinter
    {
        private string infoLastErrorText;
        public StatusBit[,] fstatusbytes = new StatusBit[8, 8];

        public FDGROUP_C_BGR(FiscalComm comm)
            : base(comm, FiscalPrinterProtocol.Extended, 1251)
        {

            for (int i = 0; i <= 7; i++)
            {

                for (int j = 0; j <= 7; j++)
                {
                    fstatusbytes[i, j] = new StatusBit();
                }
            }
        }

        uint sBytesCount = 8;
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
        public bool iSBit_Cover_IsOpen => fstatusbytes[0, 6].fCurrentState; // 0.6 = 1 Cover is open.
        public bool iSBit_Receipt_Nonfiscal => fstatusbytes[2, 5].fCurrentState; // 2.5 = 1 Nonfiscal receipt is open
        public bool iSBit_EJ_NearlyFull => fstatusbytes[2, 4].fCurrentState; // 2.4  = 1 EJ nearly full
        public bool iSBit_Receipt_Fiscal => fstatusbytes[2,3].fCurrentState; // 2.3 = 1 Fiscal receipt is open
        public bool iSBit_Near_PaperEnd => fstatusbytes[2,1].fCurrentState; // 2.1 = 1 Near paper end
        public bool iSBit_LessThan_60_Reports => fstatusbytes[4,3].fCurrentState; // 4.3 = 1 There is space for less then 60 reports in Fiscal memory
        public bool iSBit_Number_SFM_Set => fstatusbytes[4,2].fCurrentState; // 4.2 = 1 Serial number and number of FM are set
        public bool iSBit_Number_Tax_Set => fstatusbytes[4,1].fCurrentState; // 4.1 = 1 Tax number is set
        public bool iSBit_VAT_Set => fstatusbytes[5,4].fCurrentState; // 5.4 = 1 VAT are set at least once.
        public bool iSBit_Device_Fiscalized => fstatusbytes[5,3].fCurrentState; // 5.3 = 1 Device is fiscalized
        public bool iSBit_FM_formatted => fstatusbytes[5,1].fCurrentState; // 5.1 = 1 FM is formatted

        // Properties for current state of the device error status bits

        public bool eSBit_GeneralError_Sharp => fstatusbytes[0,5].fCurrentState; //0.5 = 1# General error - this is OR of all errors marked with #
        public bool eSBit_PrintingMechanism => fstatusbytes[0, 4].fCurrentState; // 0.4 = 1# Failure in printing mechanism.
        public bool eSBit_ClockIsNotSynchronized => fstatusbytes[0, 2].fCurrentState; // 0.2 = 1 The real time clock is not synchronize
        public bool eSBit_CommandCodeIsInvalid => fstatusbytes[0, 1].fCurrentState; //0.1 = 1# Command code is invalid.
        public bool eSBit_SyntaxError => fstatusbytes[0, 0].fCurrentState; //0.0 = 1# Syntax error.
        public bool eSBit_CommandNotPermitted => fstatusbytes[1,1].fCurrentState; //1.1 = 1# Command is not permitted.
        public bool eSBit_Overflow => fstatusbytes[1, 0].fCurrentState; // 1.0 = 1# Overflow during command execution.
        public bool eSBit_EJIsFull => fstatusbytes[2,2].fCurrentState; //2.2 = 1 EJ is full.
        public bool eSBit_EndOfPaper => fstatusbytes[2, 0].fCurrentState; // 2.0 = 1# End of paper.
        public bool eSBit_FM_NotFound => fstatusbytes[4,6].fCurrentState; // 4.6 Fiscal memory not found or damaged.
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

        public bool Get_SBit_State(int byteIndex, int bitIndex)
        {
           return fstatusbytes[byteIndex, bitIndex].fCurrentState;
        }

        public override void Set_AllsBytesBitsState()
        {
            for(int i=0;i<8;i++)
            {
                for(int j=0;j<8;j++)
                {
                    Set_sBytesState(i, j);
                }
            }
        }

        public bool Get_sBytesBitInUse(int byteIndex,int bitIndex)
        {
            return fstatusbytes[byteIndex, bitIndex].fInUse;
        }
        public void Set_sBytesBitInUse()
        {
            fstatusbytes[0, 0].fInUse = true;
            fstatusbytes[0, 1].fInUse = true;
            fstatusbytes[0, 2].fInUse = true;
            fstatusbytes[0, 4].fInUse = true;
            fstatusbytes[0, 5].fInUse = true;
            fstatusbytes[0, 6].fInUse = true;
            fstatusbytes[1, 0].fInUse = true;
            fstatusbytes[1, 1].fInUse = true;
            fstatusbytes[2, 0].fInUse = true;
            fstatusbytes[2, 1].fInUse = true;
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
            fstatusbytes[4, 6].fInUse = true;
            fstatusbytes[5, 1].fInUse = true;
            fstatusbytes[5, 3].fInUse = true;
            fstatusbytes[5, 4].fInUse = true;
        }

        public void Set_Sbit_ErrorChecking(int byteIndex,int bitIndex,bool IsError) // Да може клиентът да си промени дали един статус бит е грешка или не
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
            if(deviceModel == "FMP-350X" || deviceModel == "FMP-55X" || deviceModel == "FP-700X" || deviceModel == "WP-500X" || deviceModel == "WP-50X" || deviceModel == "DP-05C")
            {
                fstatusbytes[0, 6].fErrorForEndUser = true;
            }
            fstatusbytes[1, 0].fErrorForEndUser = true;
            fstatusbytes[1, 1].fErrorForEndUser = true;
            fstatusbytes[2, 0].fErrorForEndUser = true;
            fstatusbytes[2, 2].fErrorForEndUser = true;
            fstatusbytes[4, 0].fErrorForEndUser = true;
            fstatusbytes[4, 4].fErrorForEndUser = true;
            fstatusbytes[4, 5].fErrorForEndUser = true;
            fstatusbytes[4, 6].fErrorForEndUser = true;

        }

        public bool Get_SBit_ErrorChecking(int byteIndex, int bitIndex)
        {
            return fstatusbytes[byteIndex, bitIndex].fErrorForEndUser;
        }

        public string Get_sBit_Description(int byteIndex, int bitIndex)
        {
            return fstatusbytes[byteIndex, bitIndex].fTextDescription;
        }

        public void SetStatusBits_Descriptions()
        {
            fstatusbytes[0, 0].fTextDescription = GetErrorMessage("-24");
            fstatusbytes[0, 1].fTextDescription = GetErrorMessage("-25");
            fstatusbytes[0, 2].fTextDescription = GetErrorMessage("-26");
            fstatusbytes[0, 3].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[0, 4].fTextDescription = GetErrorMessage("-28");
            fstatusbytes[0, 5].fTextDescription = GetErrorMessage("-29");
            fstatusbytes[0, 6].fTextDescription = GetErrorMessage("-30");
            fstatusbytes[0, 7].fTextDescription = GetErrorMessage("-31");

            fstatusbytes[1, 0].fTextDescription = GetErrorMessage("-33");
            fstatusbytes[1, 1].fTextDescription = GetErrorMessage("-32");
            fstatusbytes[1, 2].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[1, 3].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[1, 4].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[1, 5].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[1, 6].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[1, 7].fTextDescription = GetErrorMessage("-31");

            fstatusbytes[2, 0].fTextDescription = GetErrorMessage("-39");
            fstatusbytes[2, 1].fTextDescription = GetErrorMessage("-38");
            fstatusbytes[2, 2].fTextDescription = GetErrorMessage("-37");
            fstatusbytes[2, 3].fTextDescription = GetErrorMessage("-36");
            fstatusbytes[2, 4].fTextDescription = GetErrorMessage("-35");
            fstatusbytes[2, 5].fTextDescription = GetErrorMessage("-34");
            fstatusbytes[2, 6].fTextDescription = GetErrorMessage("-27");
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
            fstatusbytes[4, 3].fTextDescription = GetErrorMessage("-43");
            fstatusbytes[4, 4].fTextDescription = GetErrorMessage("-42");
            fstatusbytes[4, 5].fTextDescription = GetErrorMessage("-41");
            fstatusbytes[4, 6].fTextDescription = GetErrorMessage("-40");
            fstatusbytes[4, 7].fTextDescription = GetErrorMessage("-31");

            fstatusbytes[5, 0].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[5, 1].fTextDescription = GetErrorMessage("-49");
            fstatusbytes[5, 2].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[5, 3].fTextDescription = GetErrorMessage("-48");
            fstatusbytes[5, 4].fTextDescription = GetErrorMessage("-47");
            fstatusbytes[5, 5].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[5, 6].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[5, 7].fTextDescription = GetErrorMessage("-31");

            fstatusbytes[6, 0].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[6, 1].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[6, 2].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[6, 3].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[6, 4].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[6, 5].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[6, 6].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[6, 7].fTextDescription = GetErrorMessage("-31");

            fstatusbytes[7, 0].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[7, 1].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[7, 2].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[7, 3].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[7, 4].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[7, 5].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[7, 6].fTextDescription = GetErrorMessage("-27");
            fstatusbytes[7, 7].fTextDescription = GetErrorMessage("-31");
        }

        private void CheckResult()
        {
            Calculate_StatusBits();
        }

        //AI generated source code  -start

        // Command number(Dec): 33 - please check fiscal device documentation.

        /// <summary>Clears the external display
        /// Note: The command is not used on FMP-350X and FMP-55X</summary>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> display_Clear() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(33 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 35 - please check fiscal device documentation.
        /// <summary>Displaying text on second line of the external display</summary>
        /// <param name="textData">text to display</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> display_Show_LowerLine(string textData) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\t");
            inputString.Append(textData);

            string r = CustomCommand(35 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 47 - please check fiscal device documentation.
        /// <summary>Displaying text on upper line of the external display</summary>
        /// <param name="textData">text to display</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> display_Show_UpperLine(string textData) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("\t");
            inputString.Append(textData);

            string r = CustomCommand(47 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 63 - please check fiscal device documentation.
        /// <summary>
        /// Show current date and time on the external display
        /// Note: The command is not used on FMP-350X and FMP-55X
        /// </summary>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>dateTime</term>
        /// <description>Date and time in format: "DD-MM-YY hh:mm:ss DST".</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> display_Show_DateTime() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(63 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["dateTime"] = split[1];
            return result;
        }

        // Command number(Dec): 38 - please check fiscal device documentation.
        /// <summary>Opening a non-fiscal receipt</summary>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>slipNumber</term>
        /// <description>Indicates current slip number (1...9999999)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_NonFiscal_Open() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(38 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["slipNumber"] = split[1];
            return result;
        }

        // Command number(Dec): 39 - please check fiscal device documentation.
        /// <summary>Closing a non-fiscal receipt</summary>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>slipNumber</term>
        /// <description>Indicates current slip number (1...9999999)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_NonFiscal_Close() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(39 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["slipNumber"] = split[1];
            return result;
        }

        // Command number(Dec): 42 - please check fiscal device documentation.
        /// <summary>Printing of a free non-fiscal text</summary>
        /// <param name="inputText">non-fiscal text</param>
        /// <param name="bold">1=print bold text; 0=normal text</param>
        /// <param name="italic">1=print italic text; 0=normal text</param>
        /// <param name="height">0=normal height, 1=double height, 2=half height; empty field = normal height text</param>
        /// <param name="underLine">flag 0 or 1, 1 = print underlined text; empty field = normal text</param>
        /// <param name="alignment">0, 1 or 2. 0=left alignment, 1=center, 2=right; empty field = left alignment</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_NonFiscal_Text(string inputText, string bold, string italic, string height, string underLine, string alignment) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(inputText);
            inputString.Append("\t");
            inputString.Append(bold);
            inputString.Append("\t");
            inputString.Append(italic);
            inputString.Append("\t");
            inputString.Append(height);
            inputString.Append("\t");
            inputString.Append(underLine);
            inputString.Append("\t");
            inputString.Append(alignment);
            inputString.Append("\t");

            string r = CustomCommand(42 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 42 - please check fiscal device documentation.
        public Dictionary<string, string> receipt_PNonFiscal_Text(string inputText, string bold, string italic, string height, string underLine, string alignment) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(inputText);
            inputString.Append("\t");
            inputString.Append(bold);
            inputString.Append("\t");
            inputString.Append(italic);
            inputString.Append("\t");
            inputString.Append(height);
            inputString.Append("\t");
            inputString.Append(underLine);
            inputString.Append("\t");
            inputString.Append(alignment);
            inputString.Append("\t");

            string r = CustomCommand(42 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 43 - please check fiscal device documentation.
        /// <summary>
        /// Opening of storno documents
        /// </summary>
        /// <param name="operatorNumber">Operator number from 1...30</param>
        /// <param name="operatorPassword">Operator password, ascii string of digits. Length from 1...8 \n Note: WP-500X, WP-50X, DP-25X, DP-150X, DP-05C: the default password for each operator is equal to the corresponding number (for 
        /// example, for Operator1 the password is "1") . FMP-350X, FMP-55X, FP-700X: the default password for each operator is “0000”</param>
        /// <param name="tillNumber">Number of point of sale from 1...99999</param>
        /// <param name="stornoType">Reason for storno</param>
        /// <list type="bullet">
        /// <item>
        /// <term>0: </term>
        /// <description>Reason "operator error"</description>
        /// </item>
        /// <item>
        /// <term>1: </term>
        /// <description>Reason "refund"</description>
        /// </item>
        /// <item>
        /// <term>2: </term>
        /// <description>Reason "tax base reduction";</description>
        /// </item>
        /// </list>
        /// <param name="stornoDocumentNumber">Number of the original document ( global 1...9999999 )</param>
        /// <param name="stornoDateTime">Date and time of the original document( format "DD-MM-YY hh:mm:ss DST" )</param>
        /// <param name="stornoFMNumber">Fiscal memory number of the device the issued the original document</param>
        /// <param name="invoice">If this parameter has value 'I' it opens an invoice storno/refund receipt</param>
        /// <param name="invoiceNumber">If Invoice is 'I' - Number of the invoice that this receipt is referred to; If Invoice is blank
        ///this parameter has to be blank too</param>
        /// <param name="stornoReason">If Invoice is 'I' - Reason for invoice storno/refund. If Invoice is blank this parameter has to be
        /// blank too</param>
        /// <param name="stornoUNP">Unique sale number (21 chars "LLDDDDDD-CCCC-DDDDDDD", L[A-Z], C[0-9A-Za-z],
        /// D[0 - 9] )</param>

        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0. </description>
        /// </item>
        /// <item>
        /// <term>slipNumber</term>
        /// <description>Indicates current slip number (1...9999999)</description>
        /// </item>
        /// </list>
        /// </returns>
        /// <seealso cref="receipt_Fiscal_Close()"/> to close storno receipt.
        public Dictionary<string, string> open_StornoReceipt(string operatorNumber, string operatorPassword, string tillNumber, string stornoType, string stornoDocumentNumber, string stornoDateTime, string stornoFMNumber, string invoice, string invoiceNumber, string stornoReason, string stornoUNP) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append("\t");
            inputString.Append(operatorPassword);
            inputString.Append("\t");
            inputString.Append(tillNumber);
            inputString.Append("\t");
            inputString.Append(stornoType);
            inputString.Append("\t");
            inputString.Append(stornoDocumentNumber);
            inputString.Append("\t");
            inputString.Append(stornoDateTime);
            inputString.Append("\t");
            inputString.Append(stornoFMNumber);
            inputString.Append("\t");
            inputString.Append(invoice);
            inputString.Append("\t");
            inputString.Append(invoiceNumber);
            inputString.Append("\t");
            inputString.Append(stornoReason);
            inputString.Append("\t");
            inputString.Append(stornoUNP);
            inputString.Append("\t");

            string r = CustomCommand(43 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["slipNumber"] = split[1];
            return result;
        }

        public void open_FiscalReceipt(string operatorNumber, string operatorPassword, string uNP, string tillNumber, string invoice)
        {
           
            if (Y(uNP) && Y(invoice)) receipt_Invoice_Open(operatorNumber, operatorPassword, uNP, tillNumber, invoice);
            if (N(uNP) && Y(invoice)) receipt_FiscalOpen_C02(operatorNumber, operatorPassword, tillNumber, invoice);
            if (Y(uNP) && N(invoice)) receipt_Fiscal_Open(operatorNumber, operatorPassword, uNP,tillNumber, invoice);
            if (N(uNP) && N(invoice)) receipt_FiscalOpen_C01(operatorNumber, operatorPassword, tillNumber, invoice);
        }

        // Command number(Dec): 48 - please check fiscal device documentation.
        /// <summary>
        /// Open fiscal receipt
        /// </summary>
        /// <param name="operatorNumber">Operator number from 1...30</param>
        /// <param name="operatorPassword">Operator password, ascii string of digits. Length from 1...8 \n
        /// Note: WP-500X, WP-50X, DP-25X, DP-150X, DP-05C:, the default password for each operator is equal to the corresponding number (for
        /// example, for Operator1 the password is "1") . FMP-350X, FMP-55X, FP-700X: the default password for each operator is “0000”</param>
        /// <param name="tillNumber">Number of point of sale from 1...99999</param>
        /// <param name="invoice">If this parameter has value 'I' it opens an invoice receipt. If left blank it opens fiscal receipt</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>slipNumber</term>
        /// <description>Indicates current slip number (1...9999999)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_FiscalOpen_C01(string operatorNumber, string operatorPassword, string tillNumber, string invoice) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append("\t");
            inputString.Append(operatorPassword);
            inputString.Append("\t");
            inputString.Append(tillNumber);
            inputString.Append("\t");
            inputString.Append(invoice);
            inputString.Append("\t");

            string r = CustomCommand(48 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["slipNumber"] = split[1];
            return result;
        }

        // Command number(Dec): 48 - please check fiscal device documentation.
        /// <summary>
        ///  Open fiscal receipt
        /// </summary>
        /// <param name="operatorNumber">Operator number from 1...30</param>
        /// <param name="operatorPassword">Operator password, ascii string of digits. Length from 1...8 \n
        /// Note: WP-500X, WP-50X, DP-25X, DP-150X, DP-05C:, the default password for each operator is equal to the corresponding number (for
        /// example, for Operator1 the password is "1") . FMP-350X, FMP-55X, FP-700X: the default password for each operator is “0000”</param>
        /// <param name="uNP">Unique sale number (21 chars "LLDDDDDD-CCCC-DDDDDDD", L[A-Z], C[0-9fA-Za-z],
        /// D[0 - 9] )</param>
        /// <param name="tillNumber">Number of point of sale from 1...99999</param>
        /// <param name="invoice">If this parameter has value 'I' it opens an invoice receipt. If left blank it opens fiscal receipt</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>slipNumber</term>
        /// <description>Indicates current slip number (1...9999999)</description>
        /// </item>
        /// </list>
        /// </returns>
        /// <seealso cref="receipt_Fiscal_Close()"/> to close storno receipt.
        public Dictionary<string, string> receipt_FiscalOpen_C03(string operatorNumber, string operatorPassword, string uNP, string tillNumber, string invoice) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append("\t");
            inputString.Append(operatorPassword);
            inputString.Append("\t");
            inputString.Append(uNP);
            inputString.Append("\t");
            inputString.Append(tillNumber);
            inputString.Append("\t");
            inputString.Append(invoice);
            inputString.Append("\t");

            string r = CustomCommand(48 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["slipNumber"] = split[1];
            return result;
        }

        // Command number(Dec): 48 - please check fiscal device documentation.
        /// <summary>
        ///  Open fiscal receipt
        /// </summary>
        /// <param name="operatorNumber">Operator number from 1...30</param>
        /// <param name="operatorPassword">Operator password, ascii string of digits. Length from 1...8 \n
        /// Note: WP-500X, WP-50X, DP-25X, DP-150X, DP-05C:, the default password for each operator is equal to the corresponding number (for
        /// example, for Operator1 the password is "1") . FMP-350X, FMP-55X, FP-700X: the default password for each operator is “0000”</param>
        /// <param name="uNP">Unique sale number (21 chars "LLDDDDDD-CCCC-DDDDDDD", L[A-Z], C[0-9fA-Za-z],
        /// D[0 - 9] )</param>
        /// <param name="tillNumber">Number of point of sale from 1...99999</param>
        /// <param name="invoice">If this parameter has value 'I' it opens an invoice receipt. If left blank it opens fiscal receipt</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>slipNumber</term>
        /// <description>Indicates current slip number (1...9999999)</description>
        /// </item>
        /// </list>
        /// </returns>
        /// <seealso cref="receipt_Fiscal_Close()"/> to close storno receipt.
        public Dictionary<string, string> receipt_Fiscal_Open(string operatorNumber, string operatorPassword, string uNP, string tillNumber, string invoice) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append("\t");
            inputString.Append(operatorPassword);
            inputString.Append("\t");
            inputString.Append(uNP);
            inputString.Append("\t");
            inputString.Append(tillNumber);
            inputString.Append("\t");
            inputString.Append(invoice);
            inputString.Append("\t");

            string r = CustomCommand(48 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["slipNumber"] = split[1];
            return result;
        }

        // Command number(Dec): 56 - please check fiscal device documentation.
        /// <summary>
        /// Close fiscal receipt
        /// </summary>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>slipNumber</term>
        /// <description>Indicates current slip number (1...9999999)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Fiscal_Close() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(56 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["slipNumber"] = split[1];
            return result;
        }

        // Command number(Dec): 48 - please check fiscal device documentation.
        /// <summary>
        ///  Open fiscal receipt
        /// </summary>
        /// <param name="operatorNumber">Operator number from 1...30</param>
        /// <param name="operatorPassword">Operator password, ascii string of digits. Length from 1...8 \n
        /// Note: WP-500X, WP-50X, DP-25X, DP-150X, DP-05C:, the default password for each operator is equal to the corresponding number (for
        /// example, for Operator1 the password is "1") . FMP-350X, FMP-55X, FP-700X: the default password for each operator is “0000”</param>
        /// <param name="tillNumber">Number of point of sale from 1...99999</param>
        /// <param name="invoice">If this parameter has value 'I' it opens an invoice receipt. If left blank it opens fiscal receipt</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>slipNumber</term>
        /// <description>Indicates current slip number (1...9999999)</description>
        /// </item>
        /// </list>
        /// </returns>
        /// <seealso cref="receipt_Fiscal_Close()"/> to close storno receipt.
        public Dictionary<string, string> receipt_FiscalOpen_C02(string operatorNumber, string operatorPassword, string tillNumber, string invoice) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append("\t");
            inputString.Append(operatorPassword);
            inputString.Append("\t");
            inputString.Append(tillNumber);
            inputString.Append("\t");
            inputString.Append(invoice);
            inputString.Append("\t");

            string r = CustomCommand(48 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["slipNumber"] = split[1];
            return result;
        }

        // Command number(Dec): 48 - please check fiscal device documentation.
        public Dictionary<string, string> receipt_FiscalOpen_C04(string operatorNumber, string operatorPassword, string uNP, string tillNumber, string invoice) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append("\t");
            inputString.Append(operatorPassword);
            inputString.Append("\t");
            inputString.Append(uNP);
            inputString.Append("\t");
            inputString.Append(tillNumber);
            inputString.Append("\t");
            inputString.Append(invoice);
            inputString.Append("\t");

            string r = CustomCommand(48 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["slipNumber"] = split[1];
            return result;
        }

        // Command number(Dec): 48 - please check fiscal device documentation.
        public Dictionary<string, string> receipt_Invoice_Open(string operatorNumber, string operatorPassword, string uNP, string tillNumber, string invoice) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorNumber);
            inputString.Append("\t");
            inputString.Append(operatorPassword);
            inputString.Append("\t");
            inputString.Append(uNP);
            inputString.Append("\t");
            inputString.Append(tillNumber);
            inputString.Append("\t");
            inputString.Append(invoice);
            inputString.Append("\t");

            string r = CustomCommand(48 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["slipNumber"] = split[1];
            return result;
        }

        // Command number(Dec): 57 - please check fiscal device documentation.
        /// <summary>
        /// Enter and print invoice data
        /// </summary>
        /// <param name="sellerName">Name of the client; 36 symbols max; if left blank prints empty space for hand-writing</param>
        /// <param name="receiverName">Name of the receiver; 36 symbols max; if left blank prints empty space for hand-writing</param>
        /// <param name="clientName">Name of the buyer; 36 symbols max; if left blank prints empty space for hand-writing</param>
        /// <param name="address1">First line of the address; 36 symbols max; if left blank prints empty space for hand-writing</param>
        /// <param name="address2">Second line of the address; 36 symbols max; if left blank prints empty space for handwriting</param>
        /// <param name="eIKType">Type of client's tax number. 0-BULSTAT; 1-EGN; 2-LNCH; 3-service number</param>
        /// <param name="eIK">Client's tax number. ascii string of digits 8...13 Optional parameters</param>
        /// <param name="taxNo">VAT number of the client. 10...14 symbols</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_PrintClientInfo_15(string sellerName, string receiverName, string clientName, string address1, string address2, string eIKType, string eIK, string taxNo) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(sellerName);
            inputString.Append("\t");
            inputString.Append(receiverName);
            inputString.Append("\t");
            inputString.Append(clientName);
            inputString.Append("\t");
            inputString.Append(address1);
            inputString.Append("\t");
            inputString.Append(address2);
            inputString.Append("\t");
            inputString.Append(eIKType);
            inputString.Append("\t");
            inputString.Append(eIK);
            inputString.Append("\t");
            inputString.Append(taxNo);
            inputString.Append("\t");

            string r = CustomCommand(57 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        public void execute_Sale(string textRow1, string department, string taxGroup, string singlePrice, string quantity, string measure, string discountType, string discountValue)
        {
            if(String.IsNullOrEmpty(measure)) receipt_Sale(textRow1, taxGroup, singlePrice, quantity, discountType, discountValue, department);
            else receipt_Sale_Un(textRow1, taxGroup, singlePrice, quantity, discountType, discountValue, department, measure);

        }

            // Command number(Dec): 49 - please check fiscal device documentation.
            /// <summary>
            /// Registration of sale
            /// </summary>
            /// <param name="textRow1">Name of product, up to 72 characters not empty string</param>
            /// <param name="taxGroup">Tax code 1...8</param>
            /// <param name="singlePrice">Product price, with sign '-' at void operations. Format: 2 decimals; up to *9999999.99</param>
            /// <param name="quantity">Quantity of the product ( default: 1.000 ); Format: 3 decimals; up to *999999.999</param>
            /// <param name="discountType">type of discount.\n
            /// '0' or empty - no discount;\n
            /// '1' - surcharge by percentage;\n
            /// '2' - discount by percentage;\n
            /// '3' - surcharge by sum;\n
            /// '4' - discount by sum; If DiscountType is non zero, DiscountValue have to contain value.The
            ///format must be a value with two decimals.</param>
            /// <param name="discountValue">value of discount.\n
            ///a number from 0.01 to 9999999.99 for sum operations\n
            ///a number from 0.01 to 99.99 for percentage operations</param>
            /// <param name="department">Number of the department 0..99; If '0' - Without department</param>
            /// <returns>Dictionary with keys:
            /// <list type="table">
            /// <item>
            /// <term>errorCode</term>
            /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
            /// </item>
            /// <item>
            /// <term>slipNumber</term>
            /// <description>Indicates current slip number (1...9999999)</description>
            /// </item>
            /// </list>
            /// </returns>
            public Dictionary<string, string> receipt_Sale(string textRow1, string taxGroup, string singlePrice, string quantity, string discountType, string discountValue, string department) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("\t");
            inputString.Append(quantity);
            inputString.Append("\t");
            inputString.Append(discountType);
            inputString.Append("\t");
            inputString.Append(discountValue);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");

            string r = CustomCommand(49 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["slipNumber"] = split[1];
            return result;
        }

        // Command number(Dec): 49 - please check fiscal device documentation.
        /// <summary>
        /// Registration of sale
        /// </summary>
        /// <param name="textRow1">Name of product, up to 72 characters not empty string</param>
        /// <param name="taxGroup">Tax code 1...8</param>
        /// <param name="singlePrice">Product price, with sign '-' at void operations. Format: 2 decimals; up to *9999999.99</param>
        /// <param name="quantity">Quantity of the product ( default: 1.000 ); Format: 3 decimals; up to *999999.999</param>
        /// <param name="discountType">type of discount.\n
        /// '0' or empty - no discount;\n
        /// '1' - surcharge by percentage;\n
        ///'2' - discount by percentage;\n
        ///'3' - surcharge by sum;\n
        /// '4' - discount by sum; If DiscountType is non zero, DiscountValue have to contain value.The
        ///format must be a value with two decimals.</param>
        /// <param name="discountValue">value of discount.\n
        ///a number from 0.01 to 9999999.99 for sum operations\n
        ///a number from 0.01 to 99.99 for percentage operations</param>
        /// <param name="department">Number of the department 0..99; If '0' - Without department</param>
        /// <param name="measure">Unit name, up to 6 characters not empty string</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>slipNumber</term>
        /// <description>Indicates current slip number (1...9999999)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Sale_Un(string textRow1, string taxGroup, string singlePrice, string quantity, string discountType, string discountValue, string department, string measure) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(textRow1);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("\t");
            inputString.Append(quantity);
            inputString.Append("\t");
            inputString.Append(discountType);
            inputString.Append("\t");
            inputString.Append(discountValue);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(measure);
            inputString.Append("\t");

            string r = CustomCommand(49 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["slipNumber"] = split[1];
            return result;
        }

        // Command number(Dec): 58 - please check fiscal device documentation.
        /// <summary>
        /// Registering the sale of a programmed item
        /// </summary>
        /// <param name="targetPLU">The code of the item. from 1 to MAX_PLU. MAX_PLU: ECR-100000, Printer-3000</param>
        /// <param name="quantity">Quantity of the product ( default: 1.000 ); Format: 3 decimals; up to *999999.999</param>
        /// <param name="discountType">type of discount.\n
        /// '0' or empty - no discount;'1' - surcharge by percentage;\n
        /// '2' - discount by percentage;'3' - surcharge by sum;'4' - discount by sum;</param>
        /// <param name="discountValue">Value of discount\n
        /// - a number from 0.01 to 9999999.99 for sum operations\n
        /// - a number from 0.01 to 100.00 for percentage operations</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>slipNumber</term>
        /// <description>Indicates current slip number (1...9999999)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_PLU_Sale(string targetPLU, string quantity, string discountType, string discountValue) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(targetPLU);
            inputString.Append("\t");
            inputString.Append(quantity);
            inputString.Append("\t");
            inputString.Append(discountType);
            inputString.Append("\t");
            inputString.Append(discountValue);
            inputString.Append("\t");

            string r = CustomCommand(58 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["slipNumber"] = split[1];
            return result;
        }

        // Command number(Dec): 51 - please check fiscal device documentation.
        /// <summary>
        /// Subtotal
        /// </summary>
        /// <param name="toPrint">Print out\n
        /// '0' - default, no print out\n
        /// '1' - the sum of the subtotal will be printed out</param>
        /// <param name="toDisplay">Show the subtotal on the client display\n
        /// '0' - No display\n
        /// '1' - The sum of the subtotal will appear on the display</param>
        /// <param name="discountType">Type of discount\n
        /// '0' or empty - no discount
        /// '1' - surcharge by percentage
        /// '2' - discount by percentage
        /// '3' - surcharge by sum
        /// '4' - discount by sum</param>
        /// <param name="discountValue">Value of discount\n
        /// a number from 0.01 to 21474836.47 for sum operations\n
        /// a number from 0.01 to 99.99 for percentage operations\n</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>slipNumber</term>
        /// <description>Indicates current slip number (1...9999999)</description>
        /// </item>
        /// <item>
        /// <term>subtotal</term>
        /// <description>Subtotal of the receipt (0.00...9999999.99 or 0...999999999 depending dec point position)</description>
        /// </item>
        /// <item>
        /// <term>taxA</term>
        /// <description>Value of Tax group A (0.00...99.99 taxable,100.00=disabled)</description>
        /// </item>
        /// <item>
        /// <term>taxB</term>
        /// <description>Value of Tax group B (0.00...99.99 taxable,100.00=disabled)</description>
        /// </item>
        /// <item>
        /// <term>taxC</term>
        /// <description>Value of Tax group C (0.00...99.99 taxable,100.00=disabled)</description>
        /// </item>
        /// <item>
        /// <term>taxD</term>
        /// <description>Value of Tax group D (0.00...99.99 taxable,100.00=disabled)</description>
        /// </item>
        /// <item>
        /// <term>taxE</term>
        /// <description>Value of Tax group E (0.00...99.99 taxable,100.00=disabled)</description>
        /// </item>
        /// <item>
        /// <term>taxF</term>
        /// <description>Value of Tax group F (0.00...99.99 taxable,100.00=disabled)</description>
        /// </item>
        /// <item>
        /// <term>taxG</term>
        /// <description>Value of Tax group G (0.00...99.99 taxable,100.00=disabled)</description>
        /// </item>
        /// <item>
        /// <term>taxH</term>
        /// <description>Value of Tax group H (0.00...99.99 taxable,100.00=disabled)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Subtotal(string toPrint, string toDisplay, string discountType, string discountValue) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(toPrint);
            inputString.Append("\t");
            inputString.Append(toDisplay);
            inputString.Append("\t");
            inputString.Append(discountType);
            inputString.Append("\t");
            inputString.Append(discountValue);
            inputString.Append("\t");

            string r = CustomCommand(51 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["slipNumber"] = split[1];
            if(split.Length >= 3) 
                result["subtotal"] = split[2];
            if(split.Length >= 4) 
                result["taxA"] = split[3];
            if(split.Length >= 5) 
                result["taxB"] = split[4];
            if(split.Length >= 6) 
                result["taxC"] = split[5];
            if(split.Length >= 7) 
                result["taxD"] = split[6];
            if(split.Length >= 8) 
                result["taxE"] = split[7];
            if(split.Length >= 9) 
                result["taxF"] = split[8];
            if(split.Length >= 10) 
                result["taxG"] = split[9];
            if(split.Length >= 11) 
                result["taxH"] = split[10];
            return result;
        }

        public void execute_Total(string paidMode, string inputAmount, string pinpadPaidMode, string currencyType)
        {
           
            if (Y(currencyType)&& Y(paidMode) && Y(inputAmount)) receipt_Total_Currency(paidMode, inputAmount,currencyType);
            if (Y(paidMode) && Y(inputAmount) && N(currencyType)) receipt_Total(paidMode, inputAmount,pinpadPaidMode);

        }

        // Command number(Dec): 53 - please check fiscal device documentation.
        /// <summary>
        /// Payments and calculation of the total sum (TOTAL)
        /// </summary>
        /// <param name="paidMode">Type of payment\n
        /// '0' - cash , '1' - credit card, '2' - debit card, '3' - other pay #3, '4' - other pay #4, '5' - other pay #5</param>
        /// <param name="inputAmount">Amount to pay (0.00...9999999.99 or 0...999999999 depending dec point position)</param>
        /// <param name="pinpadPaidMode">Optional parameter. Type of card payment. Only for payment with debit card\n
        /// '1' - with money;  '12'- with points from loyal scheme</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>answerfield01</term>
        /// <description>Status - Indicates an error\n
        /// 'D' - The command passed, return when the paid sum is less than the sum of the receipt. The
        ///residual sum due for payment is returned to Amount\n
        ///'R' - The command passed, return when the paid sum is greater than the sum of the receipt. A
        /// message “CHANGE” will be printed out and the change will be returned to Amount</description>
        /// </item>
        /// /// <item>
        /// <term>answerfield02</term>
        /// <description>The sum tendered ( 0.00...9999999.99 or 0...999999999 depending dec point position</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Total(string paidMode, string inputAmount, string pinpadPaidMode) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(paidMode);
            inputString.Append("\t");
            inputString.Append(inputAmount);
            inputString.Append("\t");
            inputString.Append(pinpadPaidMode);
            inputString.Append("\t");

            string r = CustomCommand(53 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0 && error != -111560) // error for pinpads 
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["answerfield01"] = split[1];
            if(split.Length >= 3) 
                result["answerfield02"] = split[2];
            return result;
        }

        // Command number(Dec): 53 - please check fiscal device documentation.
        /// <summary>
        /// Payments and calculation of the total sum (TOTAL)
        /// </summary>
        /// <param name="paidMode">Type of payment - '6' - Foreign currency</param>
        /// <param name="inputAmount">Amount to pay ( 0.00...9999999.99 or 0...999999999 depending dec point position)</param>
        /// <param name="currencyType">Type of change\n
        /// '0' - current currency; '1' - foreign currency</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>answerfield01</term>
        /// <description>Amount to pay (0.00...9999999.99 or 0...999999999 depending dec point position)</description>
        /// </item>
        /// /// <item>
        /// <term>answerfield02</term>
        /// <description>Type of change\n
        /// '0' - current currency; '1' - foreign currency</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Total_Currency(string paidMode, string inputAmount, string currencyType) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(paidMode);
            inputString.Append("\t");
            inputString.Append(inputAmount);
            inputString.Append("\t");
            inputString.Append(currencyType);
            inputString.Append("\t");

            string r = CustomCommand(53 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["answerfield01"] = split[1];
            if(split.Length >= 3) 
                result["answerfield02"] = split[2];
            return result;
        }

        //Command 55 Pinpad commands
        /// <summary>
        /// If Pinpad is configured for Borica, this is a VOID operation command.
        /// </summary>
        /// <param name="payType">Type of payment: 7 - Return with money, 13 - Return with points from loyal scheme</param>
        /// <param name="inputAmount">The amount of the transaction</param>
        /// <param name="rrn">RRN of the transaction(12 digits max)</param>
        /// <param name="authCode">Authorization code (AC) of the transaction(6 digits max)</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Pinpad_Borica_Void(string payType, string inputAmount, string rrn, string authCode)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("1");
            inputString.Append("\t");
            inputString.Append(payType);
            inputString.Append("\t");
            inputString.Append(inputAmount);
            inputString.Append("\t");
            inputString.Append(rrn);
            inputString.Append("\t");
            inputString.Append(authCode);
            inputString.Append("\t");

            string r = CustomCommand(55, inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: " + split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            return result;
        }

        //Command 55 Pinpad commands
        /// <summary>
        /// If pinpad is configured for UBB, this is a VOID operation command.
        /// </summary>
        /// <param name="payType">Type of payment: 16 - Return with AC number, 17 - Return with receipt number</param>
        /// <param name="inputAmount">The amount of the transaction</param>
        /// <param name="specNumber">depent on parameter "payType". If payType = "16" - specNumber must be authorization code of the transaction,If payType = "17" - specNumber must be receipt number of the transaction)</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Pinpad_UBB_Void(string payType, string inputAmount, string specNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("1");
            inputString.Append("\t");
            inputString.Append(payType);
            inputString.Append("\t");
            inputString.Append(inputAmount);
            inputString.Append("\t");
            inputString.Append(specNumber);
            inputString.Append("\t");

            string r = CustomCommand(55, inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: " + split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            return result;
        }

        //Command 55 Pinpad commands
        /// <summary>
        /// If pinpad is configured for DSK bank, this is a VOID operation command (Return with money).
        /// </summary>
        /// <param name="payType">Type of payment: 16 - Return with money</param>
        /// <param name="inputAmount">The amount of the transaction</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Pinpad_DSK_Void(string payType, string inputAmount)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("1");
            inputString.Append("\t");
            inputString.Append(payType);
            inputString.Append("\t");
            inputString.Append(inputAmount);
            inputString.Append("\t");

            string r = CustomCommand(55, inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: " + split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            return result;
        }


        /// <summary>
        /// If pinpad is configured for DSK bank, this is a VOID operation command (for last document).
        /// </summary>
        /// <param name="payType">Type of payment: 17 - Void last document</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Pinpad_DSK_VoidLastDoc(string payType)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("1");
            inputString.Append("\t");
            inputString.Append(payType);
            inputString.Append("\t");

            string r = CustomCommand(55, inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: " + split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            return result;
        }


        /// <summary>
        /// Copy of last document
        /// </summary>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Pinpad_CopyOfLastDocument()
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("2");
            inputString.Append("\t");

            string r = CustomCommand(55, inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: " + split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            return result;
        }

        /// <summary>
        /// Copy of document by type
        /// </summary>
        /// <param name="type">1 - RRN, 2 - AC, 3 - Number of the transaction</param>
        /// <param name="specNumber">depends on Type( RRN - 12 digits max, AC - 6 digits max, Number - 6 digits max )</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Pinpad_CopyDocumentByType(string type, string specNumber)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("3");
            inputString.Append("\t");
            inputString.Append(type);
            inputString.Append("\t");
            inputString.Append(specNumber);
            inputString.Append("\t");

            string r = CustomCommand(55, inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: " + split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            return result;
        }

        /// <summary>
        /// Copy of all document
        /// </summary>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Pinpad_CopyOfAllDocument()
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("4");
            inputString.Append("\t");

            string r = CustomCommand(55, inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: " + split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            return result;
        }

        /// <summary>
        /// End of day from pinpad
        /// </summary>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_Pinpad_EndOfDay()
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("5");
            inputString.Append("\t");

            string r = CustomCommand(55, inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: " + split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            return result;
        }

        /// <summary>
        /// report from pinpad
        /// </summary>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_Pinpad_Report()
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("6");
            inputString.Append("\t");

            string r = CustomCommand(55, inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: " + split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            return result;
        }

        /// <summary>
        /// Full report from pinpad
        /// </summary>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_Pinpad_FullReport()
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("7");
            inputString.Append("\t");

            string r = CustomCommand(55, inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: " + split[0]);
                }
            }
            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command 55 Pinpad  - set Date and time
        /// <summary>
        /// Enter date and time for Pinpad
        /// </summary>
        /// <param name="dateTime">Date and time in format: "DD-MM-YY hh:mm:ss DST (Text "DST" if exist time is Summer time)</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Pinpad_SetDateTime(string dateTime)
            {
                StringBuilder inputString = new StringBuilder();

                inputString.Append("8");
                inputString.Append("\t");
                inputString.Append(dateTime);
                inputString.Append("\t");

            string r = CustomCommand(55, inputString.ToString());
                CheckResult();

                string[] split = r.Split('\t');

                if (split.Length >= 1)
                {
                    var error = int.Parse(split[0]);
                    if (error != 0)
                    {
                        throw new FiscalException(error, "Operation failed with error code: " + split[0]);
                    }
                }

                Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            return result;
        }

        /// <summary>
        /// Check connection with pinpad
        /// </summary>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Pinpad_CheckConnection()
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("9");
            inputString.Append("\t");

            string r = CustomCommand(55, inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: " + split[0]);
                }
            }
            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        /// <summary>
        /// Check connection with server
        /// </summary>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Pinpad_CheckConnectionWithServer()
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("10");
            inputString.Append("\t");

            string r = CustomCommand(55, inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: " + split[0]);
                }
            }
            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }


        /// <summary>
        /// Check pinpad loyalty balance
        /// </summary>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Pinpad_LoyaltyBalance()
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("11");
            inputString.Append("\t");

            string r = CustomCommand(55, inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: " + split[0]);
                }
            }
            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        /// <summary>
        /// Get pinpad update
        /// </summary>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Pinpad_GetUpdate()
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("12");
            inputString.Append("\t");

            string r = CustomCommand(55, inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: " + split[0]);
                }
            }
            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        /// <summary>
        /// Used when command 53( paying with pinpad ) and command 55 ( option 14 ) returns error along with sum and last digits of card number
        /// </summary>
        /// <param name="operation">Operation for execution ('1' - print receipt; '2' - void transaction from pinpad)</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Pinpad_Option13(string operation)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("13");
            inputString.Append("\t");
            inputString.Append(operation);
            inputString.Append("\t");

            string r = CustomCommand(55, inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: " + split[0]);
                }
            }
            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        /// <summary>
        /// Make sale from pinpad, without fiscal receipt
        /// </summary>
        /// <param name="inputAmount">Amount for sale</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Pinpad_SaleWithoutFiscalReceipt(string inputAmount)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("14");
            inputString.Append("\t");
            inputString.Append(inputAmount);
            inputString.Append("\t");

            string r = CustomCommand(55, inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0 && error != -111560)
                {
                    throw new FiscalException(error, "Operation failed with error code: " + split[0]);
                }
            }
            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            if (split.Length > 2)
            {
                result["dataField1"] = split[1];
                result["dataField2"] = split[2];
                if (split.Length > 3)
                {
                    result["dataField3"] = split[3];
                    result["dataField4"] = split[4];
                    result["dataField5"] = split[5];
                    result["dataField6"] = split[6];
                    result["dataField7"] = split[7];
                    result["dataField8"] = split[8];
                    result["dataField9"] = split[9];
                    result["dataField10"] = split[10];
                    result["dataField11"] = split[11];
                    result["dataField12"] = split[12];
                    result["dataField13"] = split[13];
                }
            }
            return result;
        }

        /// <summary>
        /// Print receipt for pinpad after succesfull transaction. Must be executed after command
        /// 53 (when paying with pinpad) and after command 56 (when paying with pinpad);
        /// </summary>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Pinpad_PrintReceipt()
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append("15");
            inputString.Append("\t");

            string r = CustomCommand(55, inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: " + split[0]);
                }
            }
            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 44 - please check fiscal device documentation.
        /// <summary>
        /// Paper feed
        /// </summary>
        /// <param name="linesCount">Number of lines to feed from 1 to 99</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Paper_Feed(string linesCount) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(linesCount);
            inputString.Append("\t");

            string r = CustomCommand(44 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 46 - please check fiscal device documentation.
        /// <summary>
        /// Paper cutting
        /// </summary>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Paper_Cut() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(46 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 54 - please check fiscal device documentation.
        /// <summary>
        /// Printing of a free fiscal text
        /// </summary>
        /// <param name="inputText">Text - text of 0...XX symbols, XX = PrintColumns-2</param>
        /// <param name="bold">Bold - flag 0 or 1, 1 = print bold text; empty field = normal text</param>
        /// <param name="italic">Italic - flag 0 or 1, 1 = print italic text; empty field = normal text</param>
        /// <param name="doubleHeight">DoubleH - flag 0 or 1, 1 = print double height text; empty field = normal text</param>
        /// <param name="underLine">Underline - flag 0 or 1, 1 = print underlined text; empty field = normal text</param>
        /// <param name="alignment">alignment - 0, 1 or 2. 0=left alignment, 1=center, 2=right; empty field = left alignment</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Fiscal_Text(string inputText, string bold, string italic, string doubleHeight, string underLine, string alignment) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(inputText);
            inputString.Append("\t");
            inputString.Append(bold);
            inputString.Append("\t");
            inputString.Append(italic);
            inputString.Append("\t");
            inputString.Append(doubleHeight);
            inputString.Append("\t");
            inputString.Append(underLine);
            inputString.Append("\t");
            inputString.Append(alignment);
            inputString.Append("\t");

            string r = CustomCommand(54 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 54 - please check fiscal device documentation.
        public Dictionary<string, string> receipt_PFiscal_Text(string inputText, string bold, string italic, string height, string underLine, string alignment) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(inputText);
            inputString.Append("\t");
            inputString.Append(bold);
            inputString.Append("\t");
            inputString.Append(italic);
            inputString.Append("\t");
            inputString.Append(height);
            inputString.Append("\t");
            inputString.Append(underLine);
            inputString.Append("\t");
            inputString.Append(alignment);
            inputString.Append("\t");

            string r = CustomCommand(54 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 60 - please check fiscal device documentation.
        /// <summary>
        /// Cancel fiscal receipt
        /// </summary>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Fiscal_Cancel() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(60 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 70 - please check fiscal device documentation.
        /// <summary>
        /// Cash in and Cash out operations
        /// </summary>
        /// <param name="amountType">Type of operation\n
        /// '0' - cash in; '1' - cash out; '2' - cash in (foreign currency); '3' - cash out (foreign currency)</param>
        /// <param name="amount">the sum ( 0.00...9999999.99 or 0...999999999 depending dec point position ); If Amount=0,
        /// the only Answer is returned, and receipt does not print</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>cashSum</term>
        /// <description>cash in safe sum ( 0.00...9999999.99 or 0...999999999 depending dec point position)</description>
        /// </item>
        /// <item>
        /// <term>servIn</term>
        /// <description>total sum of cash in operations ( 0.00...9999999.99 or 0...999999999 depending dec point position</description>
        /// </item>
        /// <item>
        /// <term>servOut</term>
        /// <description>total sum of cash out operations ( 0.00...9999999.99 or 0...999999999 depending dec point  position</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_CashIn_CashOut(string amountType, string amount) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(amountType);
            inputString.Append("\t");
            inputString.Append(amount);
            inputString.Append("\t");

            string r = CustomCommand(70 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

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
        /// Printing of barcode - QR code
        /// </summary>
        /// <param name="barcodeType">'4' - QR code.Data must contain symbols with ASCII codes between 32 and 127. Data length is between 3 and 279 symbols</param>
        /// <param name="barcodeData">Data of the barcode; Length of Data depents on the type of the barcode</param>
        /// <param name="qRCodeSize">Dots multiplier ( 3...10 ) for QR barcodes and PDF417 barcodes. Default: 4</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Print_Barcode_QR(string barcodeType, string barcodeData, string qRCodeSize) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(barcodeType);
            inputString.Append("\t");
            inputString.Append(barcodeData);
            inputString.Append("\t");
            inputString.Append(qRCodeSize);
            inputString.Append("\t");

            string r = CustomCommand(84 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 84 - please check fiscal device documentation.
        /// <summary>
        /// Printing of barcode
        /// </summary>
        /// <param name="barcodeType">'1' - EAN8 barcode. Data must contain only 8 digits;
        /// '2' - EAN13 barcode.Data must contain only 13 digits;\n
        /// '3' - Code128 barcode.Data must contain symbols with ASCII codes between 32 and 127. Data length is between 3 and 31 symbols;\n
        /// '5' - Interleave 2of5 barcode.Data must contain only digits, from 3 to 22 chars;\n
        /// '6' - PDF417 truncated Data must contain symbols with ASCII codes between 32 and 127. Data length is between 3 and 400 symbols;
        /// '7' - PDF417 normal Data must contain symbols with ASCII codes between 32 and 127. Data length is between 3 and 400 symbols</param>
        /// <param name="barcodeData">Data of the barcode; Length of Data depents on the type of the barcode</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Print_Barcode(string barcodeType, string barcodeData)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(barcodeType);
            inputString.Append("\t");
            inputString.Append(barcodeData);
            inputString.Append("\t");

            string r = CustomCommand(84, inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: " + split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 92 - please check fiscal device documentation.
        /// <summary>
        /// Printing of separating line
        /// </summary>
        /// <param name="lineType">Type of the separating line\n
        /// '1' - Separating line with the symbol '-'; '2' - Separating line with the symbols '-' and ' '\n
        /// '3' - Separating line with the symbol '='; '4' - Print fixed text "НЕ СЕ ДЪЛЖИ ПЛАЩАНЕ"</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Separating_Line(string lineType) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(lineType);
            inputString.Append("\t");

            string r = CustomCommand(92 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 103 - please check fiscal device documentation.
        public Dictionary<string, string> receipt_Current_Info() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(103 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["taxA"] = split[1];
            if(split.Length >= 3) 
                result["taxB"] = split[2];
            if(split.Length >= 4) 
                result["taxC"] = split[3];
            if(split.Length >= 5) 
                result["taxD"] = split[4];
            if(split.Length >= 6) 
                result["taxE"] = split[5];
            if(split.Length >= 7) 
                result["taxF"] = split[6];
            if(split.Length >= 8) 
                result["taxG"] = split[7];
            if(split.Length >= 9) 
                result["taxH"] = split[8];
            if(split.Length >= 10) 
                result["inv"] = split[9];
            if(split.Length >= 11) 
                result["invNmb"] = split[10];
            if(split.Length >= 12) 
                result["fStorno"] = split[11];
            return result;
        }

        // Command number(Dec): 106 - please check fiscal device documentation.
        /// <summary>
        /// Drawer opening
        /// Note: only for FP-705
        /// </summary>
        /// <param name="mSec">mSec - The length of the impulse in milliseconds. ( 0...65535 )</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Drawer_KickOut(string mSec) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(mSec);
            inputString.Append("\t");

            string r = CustomCommand(106 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 109 - please check fiscal device documentation.
        /// <summary>
        /// Print duplicate copy of receipt
        /// </summary>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Print_Duplicate() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(109 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 122 - please check fiscal device documentation.
        /// <summary>
        /// Printing of a free vertical fiscal text
        /// </summary>
        /// <param name="inputText">text of 0...128 symbols
        /// Double-byte control codes: (0Bh 42h ) - Bolds all symbols; (0Bh 62h ) - Stops the bolding of the symbols.\n
        /// (0Bh 55h ) - Overlines all symbols; (0Bh 75h ) - Stops the overlining of the symbols\n
        ///(0Bh 4Fh ) - Underlines all symbols; (0Bh 6Fh ) - Stops the underlining of the symbols\n
        ///Box drawing symbols: (82h) - up right; (84h) - up left; (91h) - down right; (92h) - down left; (93h) - up right + down right\n
        ///(94h) - up left + down left; (95h) - up left + down right; (96h) - down left + down right; (97h) - up right + down left + up left + down right</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_PVerticalFiscal_Text(string inputText) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(inputText);
            inputString.Append("\t");

            string r = CustomCommand(122 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 127 - please check fiscal device documentation.
        /// <summary>
        /// Stamp operations - print
        /// </summary>
        /// <param name="option">Type of operation: '0' - Print stamp</param>
        /// <param name="stampName">Name of stamp as filename in format 8.3</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> receipt_Print_Stamp(string option, string stampName) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(stampName);
            inputString.Append("\t");

            string r = CustomCommand(127 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 69 - please check fiscal device documentation.
        /// <summary>
        /// Daily X or Z reports
        /// </summary>
        /// <param name="option">'X' - X report;'Z' - Z report</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>nRep</term>
        /// <description>Number of Z-report (1...3650).</description>
        /// </item>
        /// <item>
        /// <term>totalsumA</term>
        /// <description>Total sum accumulated by TAX group A - sell operations ( 0.00...9999999.99 or 0...999999999 depending dec point position).</description>
        /// </item>
        /// <item>
        /// <term>totalsumB</term>
        /// <description>Total sum accumulated by TAX group B - sell operations ( 0.00...9999999.99 or 0...999999999 depending dec point position).</description>
        /// </item>
        /// <item>
        /// <term>totalsumC</term>
        /// <description>Total sum accumulated by TAX group C - sell operations ( 0.00...9999999.99 or 0...999999999 depending dec point position).</description>
        /// </item>
        /// <item>
        /// <term>totalsumD</term>
        /// <description>Total sum accumulated by TAX group D - sell operations ( 0.00...9999999.99 or 0...999999999 depending dec point position).</description>
        /// </item>
        /// <item>
        /// <term>totalsumE</term>
        /// <description>Total sum accumulated by TAX group E - sell operations ( 0.00...9999999.99 or 0...999999999 depending dec point position).</description>
        /// </item>
        /// <item>
        /// <term>totalsumF</term>
        /// <description>Total sum accumulated by TAX group F - sell operations ( 0.00...9999999.99 or 0...999999999 depending dec point position).</description>
        /// </item>
        /// <item>
        /// <term>totalsumG</term>
        /// <description>Total sum accumulated by TAX group G - sell operations ( 0.00...9999999.99 or 0...999999999 depending dec point position).</description>
        /// </item>
        /// <item>
        /// <term>totalsumH</term>
        /// <description>Total sum accumulated by TAX group H - sell operations ( 0.00...9999999.99 or 0...999999999 depending dec point position).</description>
        /// </item>
        /// <item>
        /// <term>stornosumA</term>
        /// <description>Total sum accumulated by TAX group A - storno operations ( 0.00...9999999.99 or 0...999999999 depending dec point position).</description>
        /// </item>
        ///  <item>
        /// <term>stornosumB</term>
        /// <description>Total sum accumulated by TAX group B - storno operations ( 0.00...9999999.99 or 0...999999999 depending dec point position).</description>
        /// </item>
        /// <item>
        /// <term>stornosumC</term>
        /// <description>Total sum accumulated by TAX group C - storno operations ( 0.00...9999999.99 or 0...999999999 depending dec point position).</description>
        /// </item>
        ///  <item>
        /// <term>stornosumD</term>
        /// <description>Total sum accumulated by TAX group D - storno operations ( 0.00...9999999.99 or 0...999999999 depending dec point position).</description>
        /// </item>
        ///  <item>
        /// <term>stornosumE</term>
        /// <description>Total sum accumulated by TAX group E - storno operations ( 0.00...9999999.99 or 0...999999999 depending dec point position).</description>
        /// </item>
        ///  <item>
        /// <term>stornosumF</term>
        /// <description>Total sum accumulated by TAX group F - storno operations ( 0.00...9999999.99 or 0...999999999 depending dec point position).</description>
        /// </item>
        ///  <item>
        /// <term>stornosumG</term>
        /// <description>Total sum accumulated by TAX group G - storno operations ( 0.00...9999999.99 or 0...999999999 depending dec point position).</description>
        /// </item>
        ///  <item>
        /// <term>stornosumH</term>
        /// <description>Total sum accumulated by TAX group H - storno operations ( 0.00...9999999.99 or 0...999999999 depending dec point position).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_DailyClosure_01(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");

            string r = CustomCommand(69 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["nRep"] = split[1];
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
            if(split.Length >= 11) 
                result["stornosumA"] = split[10];
            if(split.Length >= 12) 
                result["stornosumB"] = split[11];
            if(split.Length >= 13) 
                result["stornosumC"] = split[12];
            if(split.Length >= 14) 
                result["stornosumD"] = split[13];
            if(split.Length >= 15) 
                result["stornosumE"] = split[14];
            if(split.Length >= 16) 
                result["stornosumF"] = split[15];
            if(split.Length >= 17) 
                result["stornosumG"] = split[16];
            if(split.Length >= 18) 
                result["stornosumH"] = split[17];
            return result;
        }

        /// <summary>
        /// Reports - Departments and Item groups
        /// </summary>
        /// <param name="option">'D' - Departments report; 'G' - Item group report</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_DailyClosure_02(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");

            string r = CustomCommand(69 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 87 - please check fiscal device documentation.
        /// <summary>
        /// Get item groups information
        /// </summary>
        /// <param name="itemGroup">Number of item group; If ItemGroup is empty - item group report</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>totSales</term>
        /// <description>Number of sales for this item group for day</description>
        /// </item>
        /// <item>
        /// <term>totSum</term>
        /// <description>Accumulated sum for this item group for day</description>
        /// </item>
        /// <item>
        /// <term>groupName</term>
        /// <description>Name of item group</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_Item_Groups(string itemGroup) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(itemGroup);
            inputString.Append("\t");

            string r = CustomCommand(87 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 88 - please check fiscal device documentation.
        /// <summary>
        /// Get department information
        /// </summary>
        /// <param name="departmentNumber">Number of department (1...99); If Department is empty - department report</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_Department_Info(string departmentNumber) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(departmentNumber);
            inputString.Append("\t");

            string r = CustomCommand(88 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 94 - please check fiscal device documentation.
        /// <summary>
        /// Fiscal memory report by date
        /// </summary>
        /// <param name="option">Type: 1 - detailed</param>
        /// <param name="fromDate">Start date. Default: Date of fiscalization (format DD-MM-YY)</param>
        /// <param name="toDate">End date. Default: Current date (format DD-MM-YY)</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_FMByDateRange(string option, string fromDate, string toDate) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(fromDate);
            inputString.Append("\t");
            inputString.Append(toDate);
            inputString.Append("\t");

            string r = CustomCommand(94 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 94 - please check fiscal device documentation.
        /// <summary>
        /// Fiscal memory report by date
        /// </summary>
        /// <param name="option">Type: 0 - short</param>
        /// <param name="fromDate">Start date. Default: Date of fiscalization (format DD-MM-YY)</param>
        /// <param name="toDate">End date. Default: Current date (format DD-MM-YY)</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_FMByDateRange_Short(string option, string fromDate, string toDate) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(fromDate);
            inputString.Append("\t");
            inputString.Append(toDate);
            inputString.Append("\t");

            string r = CustomCommand(94 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 95 - please check fiscal device documentation.
        /// <summary>
        /// Fiscal memory report by number of Z-report
        /// </summary>
        /// <param name="reportType">Type: 1 - detailed</param>
        /// <param name="startNumber">First Z-report in the period.Default: 1</param>
        /// <param name="endNumber">Last Z-report in the period. Default: Number of last Z-report</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_FMByNumRange(string reportType, string startNumber, string endNumber) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(reportType);
            inputString.Append("\t");
            inputString.Append(startNumber);
            inputString.Append("\t");
            inputString.Append(endNumber);
            inputString.Append("\t");

            string r = CustomCommand(95 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 95 - please check fiscal device documentation.
        /// <summary>
        /// Fiscal memory report by number of Z-report
        /// </summary>
        /// <param name="reportType">Type: 0 - short</param>
        /// <param name="startNumber">First Z-report in the period.Default: 1</param>
        /// <param name="endNumber">Last Z-report in the period. Default: Number of last Z-report</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_FMByNumRange_Short(string reportType, string startNumber, string endNumber) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(reportType);
            inputString.Append("\t");
            inputString.Append(startNumber);
            inputString.Append("\t");
            inputString.Append(endNumber);
            inputString.Append("\t");

            string r = CustomCommand(95 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 105 - please check fiscal device documentation.
        /// <summary>
        /// Report operators
        /// </summary>
        /// <param name="operatorscodeStart">First operator. Default: 1 (1...30)</param>
        /// <param name="operatorscodeEnd">Last operator. Default: Maximum operator number (1...30)</param>
        /// <param name="toClear">Clear registers for operators. Default: 0\n
        /// '0' - Does not clear registers for operators; '1' - Clear registers for operators</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_Operators(string operatorscodeStart, string operatorscodeEnd, string toClear) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorscodeStart);
            inputString.Append("\t");
            inputString.Append(operatorscodeEnd);
            inputString.Append("\t");
            inputString.Append(toClear);
            inputString.Append("\t");

            string r = CustomCommand(105 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 111 - please check fiscal device documentation.
        /// <summary>
        /// PLU report
        /// </summary>
        /// <param name="option">Type - Type of report\n
        /// '0' - PLU turnovers; '1' - PLU turnovers with clearing; '2' - PLU parameters; '3' - PLU stock</param>
        /// <param name="startPLU">First PLU in the report (1...3000). Default: 1</param>
        /// <param name="endPLU">Last PLU in the report (1...3000). Default: Maximum PLU in the FPr</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> report_Items(string option, string startPLU, string endPLU) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(startPLU);
            inputString.Append("\t");
            inputString.Append(endPLU);
            inputString.Append("\t");

            string r = CustomCommand(111 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Returns data about the last found item with sales on it
        /// </summary>
        /// <param name="option">'I' - Items information</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>total</term>
        /// <description>Total count of the programmable items (For ECRs: 100000; For FPs: 3000).</description>
        /// </item>
        /// <item>
        /// <term>prog</term>
        /// <description>Total count of the programmed items (For ECRs 0...100000; For FPs 0...3000).</description>
        /// </item>
        /// <item>
        /// <term>len</term>
        /// <description>Maximum length of item name (72).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Get_ItemsInformation(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");

            string r = CustomCommand(107 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

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
        /// Item programming
        /// </summary>
        /// <param name="option">'P' - Item programming</param>
        /// <param name="targetPLU">PLU - Item number ( For ECRs 1...100000; For FPs 1...3000 );</param>
        /// <param name="taxGroup">VAT group (letter 'A'...'H' or cyrillic 'А'...'З')</param>
        /// <param name="department">Department ( 0...99 )</param>
        /// <param name="group">Stock group (1...99)</param>
        /// <param name="priceType">Price type ('0' - fixed price, '1' - free price, '2' - max price)</param>
        /// <param name="singlePrice">Price ( 0.00...9999999.99 or 0...999999999 depending dec point position)</param>
        /// <param name="addQty">A byte with value 'A' (optional)</param>
        /// <param name="quantity">Stock quantity ( 0.001...99999.999)</param>
        /// <param name="barcode01">Barcode 1 (up to 13 digits)</param>
        /// <param name="barcode02">Barcode 2 (up to 13 digits)</param>
        /// <param name="barcode03">Barcode 3 (up to 13 digits)</param>
        /// <param name="barcode04">Barcode 4 (up to 13 digits)</param>
        /// <param name="itemName">Item name (up to 72 symbols)</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Set_Item(string option, string targetPLU, string taxGroup, string department, string group, string priceType, string singlePrice, string addQty, string quantity, string barcode01, string barcode02, string barcode03, string barcode04, string itemName) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(targetPLU);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(group);
            inputString.Append("\t");
            inputString.Append(priceType);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("\t");
            inputString.Append(addQty);
            inputString.Append("\t");
            inputString.Append(quantity);
            inputString.Append("\t");
            inputString.Append(barcode01);
            inputString.Append("\t");
            inputString.Append(barcode02);
            inputString.Append("\t");
            inputString.Append(barcode03);
            inputString.Append("\t");
            inputString.Append(barcode04);
            inputString.Append("\t");
            inputString.Append(itemName);
            inputString.Append("\t");

            string r = CustomCommand(107 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Item programming
        /// </summary>
        /// <param name="option">'P' - Item programming</param>
        /// <param name="targetPLU">PLU - Item number ( For ECRs 1...100000; For FPs 1...3000 );</param>
        /// <param name="taxGroup">VAT group (letter 'A'...'H' or cyrillic 'А'...'З')</param>
        /// <param name="department">Department ( 0...99 )</param>
        /// <param name="group">Stock group (1...99)</param>
        /// <param name="priceType">Price type ('0' - fixed price, '1' - free price, '2' - max price)</param>
        /// <param name="singlePrice">Price ( 0.00...9999999.99 or 0...999999999 depending dec point position)</param>
        /// <param name="addQty">A byte with value 'A' (optional)</param>
        /// <param name="quantity">Stock quantity ( 0.001...99999.999)</param>
        /// <param name="barcode01">Barcode 1 (up to 13 digits)</param>
        /// <param name="barcode02">Barcode 2 (up to 13 digits)</param>
        /// <param name="barcode03">Barcode 3 (up to 13 digits)</param>
        /// <param name="barcode04">Barcode 4 (up to 13 digits)</param>
        /// <param name="itemName">Item name (up to 72 symbols)</param>
        /// <param name="measurementUnit">Measurement unit 0 - 19</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Set_ItemUn(string option, string targetPLU, string taxGroup, string department, string group, string priceType, string singlePrice, string addQty, string quantity, string barcode01, string barcode02, string barcode03, string barcode04, string itemName, string measurementUnit) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(targetPLU);
            inputString.Append("\t");
            inputString.Append(taxGroup);
            inputString.Append("\t");
            inputString.Append(department);
            inputString.Append("\t");
            inputString.Append(group);
            inputString.Append("\t");
            inputString.Append(priceType);
            inputString.Append("\t");
            inputString.Append(singlePrice);
            inputString.Append("\t");
            inputString.Append(addQty);
            inputString.Append("\t");
            inputString.Append(quantity);
            inputString.Append("\t");
            inputString.Append(barcode01);
            inputString.Append("\t");
            inputString.Append(barcode02);
            inputString.Append("\t");
            inputString.Append(barcode03);
            inputString.Append("\t");
            inputString.Append(barcode04);
            inputString.Append("\t");
            inputString.Append(itemName);
            inputString.Append("\t");
            inputString.Append(measurementUnit);
            inputString.Append("\t");

            string r = CustomCommand(107 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Change of the available quantity for item
        /// </summary>
        /// <param name="option">'A' - Change of the available quantity for item</param>
        /// <param name="targetPLU">Item number ( For ECRs 1...100000; For FPs 1...3000)</param>
        /// <param name="quantity">Stock quantity ( 0.001...99999.999)</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Set_ItemQuantity(string option, string targetPLU, string quantity) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(targetPLU);
            inputString.Append("\t");
            inputString.Append(quantity);
            inputString.Append("\t");

            string r = CustomCommand(107 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Delete item
        /// </summary>
        /// <param name="option">'D' - Item deleting</param>
        /// <param name="pluStart">First item to delete (For ECRs 1...100000; For FPs 1...3000). </param>
        /// <param name="pluEnd">Last item to delete (For ECRs 1...100000; For FPs 1...3000). Default: firstPLU</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Delete_Item(string option, string pluStart, string pluEnd) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(pluStart);
            inputString.Append("\t");
            inputString.Append(pluEnd);
            inputString.Append("\t");

            string r = CustomCommand(107 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Delete item
        /// </summary>
        /// <param name="option">'D' - Item deleting</param>
        /// <param name="pluStart">First item to delete (For ECRs 1...100000; For FPs 1...3000). </param>
        /// <param name="pluEnd">Last item to delete (For ECRs 1...100000; For FPs 1...3000). Default: firstPLU</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Delete_ItemsInRange(string option, string pluStart, string pluEnd) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(pluStart);
            inputString.Append("\t");
            inputString.Append(pluEnd);
            inputString.Append("\t");

            string r = CustomCommand(107 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// 
        /// </summary>
        /// <param name="option"> 'D' - Deleting item</param>
        /// <param name="deleteAll">'A'- all items will be deleted(lastPLU must be empty)</param>
        /// <param name="pluEnd">Must be empty</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Delete_All_Items(string option, string deleteAll, string pluEnd) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(deleteAll);
            inputString.Append("\t");
            inputString.Append(pluEnd);
            inputString.Append("\t");

            string r = CustomCommand(107 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Reading item data
        /// </summary>
        /// <param name="option">'R' - Reading item data</param>
        /// <param name="targetPLU">Item number (For ECRs 1...100000; For FPs 1...3000)</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>PLU</term>
        /// <description>Item number (For ECRs 1...100000; For FPs 1...3000).</description>
        /// </item>
        /// <item>
        /// <term>taxGroup</term>
        /// <description>VAT group (letter 'A'...'H' or cyrillic 'А'...'З').</description>
        /// </item>
        /// <item>
        /// <term>department</term>
        /// <description>Department (0...99).</description>
        /// </item>
        /// <item>
        /// <term>group</term>
        /// <description>Stock group (1...99).</description>
        /// </item>
        /// <item>
        /// <term>priceType</term>
        /// <description>Price type ('0' - fixed price, '1' - free price, '2' - max price).</description>
        /// </item>
        /// <item>
        /// <term>singlePrice</term>
        /// <description>Price ( 0.00...9999999.99 or 0...999999999 depending dec point position).</description>
        /// </item>
        /// <item>
        /// <term>total</term>
        /// <description>Total count of the programmable items ( For ECRs: 100000; For FPs: 3000).</description>
        /// </item>
        /// <item>
        /// <term>sold</term>
        /// <description>Sold out quantity ( 0.001...99999.999).</description>
        /// </item>
        /// <item>
        /// <term>available</term>
        /// <description>Current quantity ( 0.001...99999.999).</description>
        /// </item>
        /// <item>
        /// <term>barcode01</term>
        /// <description>Barcode 1 (up to 13 digits).</description>
        /// </item>
        /// <item>
        /// <term>barcode02</term>
        /// <description>Barcode 2 (up to 13 digits).</description>
        /// </item>
        /// <item>
        /// <term>barcode03</term>
        /// <description>Barcode 3 (up to 13 digits).</description>
        /// </item>
        /// <item>
        /// <term>barcode04</term>
        /// <description>Barcode 4 (up to 13 digits).</description>
        /// </item>
        /// <item>
        /// <term>itemName</term>
        /// <description>Item name (up to 72 symbols).</description>
        /// </item>
        /// <item>
        /// <term>measurementUnit</term>
        /// <description>Measurement unit 0 - 19.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Get_Item(string option, string targetPLU) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(targetPLU);
            inputString.Append("\t");

            string r = CustomCommand(107 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["PLU"] = split[1];
            if(split.Length >= 3) 
                result["taxGroup"] = split[2];
            if(split.Length >= 4) 
                result["department"] = split[3];
            if(split.Length >= 5) 
                result["group"] = split[4];
            if(split.Length >= 6) 
                result["priceType"] = split[5];
            if(split.Length >= 7) 
                result["singlePrice"] = split[6];
            if(split.Length >= 8) 
                result["total"] = split[7];
            if(split.Length >= 9) 
                result["sold"] = split[8];
            if(split.Length >= 10) 
                result["available"] = split[9];
            if(split.Length >= 11) 
                result["barcode01"] = split[10];
            if(split.Length >= 12) 
                result["barcode02"] = split[11];
            if(split.Length >= 13) 
                result["barcode03"] = split[12];
            if(split.Length >= 14) 
                result["barcode04"] = split[13];
            if(split.Length >= 15) 
                result["itemName"] = split[14];
            if(split.Length >= 16) 
                result["measurementUnit"] = split[15];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Returns data about the first found programmed item
        /// </summary>
        /// <param name="option">'F' - Returns data about the first found programmed item</param>
        /// <param name="targetPLU">Item number (For ECRs 1...100000; For FPs 1...3000). Default: 1</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>PLU</term>
        /// <description>Item number (For ECRs 1...100000; For FPs 1...3000).</description>
        /// </item>
        /// <item>
        /// <term>taxGroup</term>
        /// <description>VAT group (letter 'A'...'H' or cyrillic 'А'...'З').</description>
        /// </item>
        /// <item>
        /// <term>department</term>
        /// <description>Department (0...99).</description>
        /// </item>
        /// <item>
        /// <term>group</term>
        /// <description>Stock group (1...99).</description>
        /// </item>
        /// <item>
        /// <term>priceType</term>
        /// <description>Price type ('0' - fixed price, '1' - free price, '2' - max price).</description>
        /// </item>
        /// <item>
        /// <term>singlePrice</term>
        /// <description>Price ( 0.00...9999999.99 or 0...999999999 depending dec point position).</description>
        /// </item>
        /// <item>
        /// <term>total</term>
        /// <description>Total count of the programmable items ( For ECRs: 100000; For FPs: 3000).</description>
        /// </item>
        /// <item>
        /// <term>sold</term>
        /// <description>Sold out quantity ( 0.001...99999.999).</description>
        /// </item>
        /// <item>
        /// <term>available</term>
        /// <description>Current quantity ( 0.001...99999.999).</description>
        /// </item>
        /// <item>
        /// <term>barcode01</term>
        /// <description>Barcode 1 (up to 13 digits).</description>
        /// </item>
        /// <item>
        /// <term>barcode02</term>
        /// <description>Barcode 2 (up to 13 digits).</description>
        /// </item>
        /// <item>
        /// <term>barcode03</term>
        /// <description>Barcode 3 (up to 13 digits).</description>
        /// </item>
        /// <item>
        /// <term>barcode04</term>
        /// <description>Barcode 4 (up to 13 digits).</description>
        /// </item>
        /// <item>
        /// <term>itemName</term>
        /// <description>Item name (up to 72 symbols).</description>
        /// </item>
        /// <item>
        /// <term>measurementUnit</term>
        /// <description>Measurement unit 0 - 19.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Get_FirstFoundItem(string option, string targetPLU) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(targetPLU);
            inputString.Append("\t");

            string r = CustomCommand(107 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["PLU"] = split[1];
            if(split.Length >= 3) 
                result["taxGroup"] = split[2];
            if(split.Length >= 4) 
                result["department"] = split[3];
            if(split.Length >= 5) 
                result["group"] = split[4];
            if(split.Length >= 6) 
                result["priceType"] = split[5];
            if(split.Length >= 7) 
                result["singlePrice"] = split[6];
            if(split.Length >= 8) 
                result["total"] = split[7];
            if(split.Length >= 9) 
                result["sold"] = split[8];
            if(split.Length >= 10) 
                result["available"] = split[9];
            if(split.Length >= 11) 
                result["barcode01"] = split[10];
            if(split.Length >= 12) 
                result["barcode02"] = split[11];
            if(split.Length >= 13) 
                result["barcode03"] = split[12];
            if(split.Length >= 14) 
                result["barcode04"] = split[13];
            if(split.Length >= 15) 
                result["itemName"] = split[14];
            if(split.Length >= 16) 
                result["measurementUnit"] = split[15];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Returns data about the last found programmed item
        /// </summary>
        /// <param name="option">'L' - Returns data about the last found programmed item</param>
        /// <param name="targetPLU">Item number ( For ECRs 1...100000; For FPs 1...3000 ). Default: For ECRs 100000; For FPs 3000</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>PLU</term>
        /// <description>Item number (For ECRs 1...100000; For FPs 1...3000).</description>
        /// </item>
        /// <item>
        /// <term>taxGroup</term>
        /// <description>VAT group (letter 'A'...'H' or cyrillic 'А'...'З').</description>
        /// </item>
        /// <item>
        /// <term>department</term>
        /// <description>Department (0...99).</description>
        /// </item>
        /// <item>
        /// <term>group</term>
        /// <description>Stock group (1...99).</description>
        /// </item>
        /// <item>
        /// <term>priceType</term>
        /// <description>Price type ('0' - fixed price, '1' - free price, '2' - max price).</description>
        /// </item>
        /// <item>
        /// <term>singlePrice</term>
        /// <description>Price ( 0.00...9999999.99 or 0...999999999 depending dec point position).</description>
        /// </item>
        /// <item>
        /// <term>total</term>
        /// <description>Total count of the programmable items ( For ECRs: 100000; For FPs: 3000).</description>
        /// </item>
        /// <item>
        /// <term>sold</term>
        /// <description>Sold out quantity ( 0.001...99999.999).</description>
        /// </item>
        /// <item>
        /// <term>available</term>
        /// <description>Current quantity ( 0.001...99999.999).</description>
        /// </item>
        /// <item>
        /// <term>barcode01</term>
        /// <description>Barcode 1 (up to 13 digits).</description>
        /// </item>
        /// <item>
        /// <term>barcode02</term>
        /// <description>Barcode 2 (up to 13 digits).</description>
        /// </item>
        /// <item>
        /// <term>barcode03</term>
        /// <description>Barcode 3 (up to 13 digits).</description>
        /// </item>
        /// <item>
        /// <term>barcode04</term>
        /// <description>Barcode 4 (up to 13 digits).</description>
        /// </item>
        /// <item>
        /// <term>itemName</term>
        /// <description>Item name (up to 72 symbols).</description>
        /// </item>
        /// <item>
        /// <term>measurementUnit</term>
        /// <description>Measurement unit 0 - 19.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Get_LastFoundItem(string option, string targetPLU) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(targetPLU);
            inputString.Append("\t");

            string r = CustomCommand(107 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["PLU"] = split[1];
            if(split.Length >= 3) 
                result["taxGroup"] = split[2];
            if(split.Length >= 4) 
                result["department"] = split[3];
            if(split.Length >= 5) 
                result["group"] = split[4];
            if(split.Length >= 6) 
                result["priceType"] = split[5];
            if(split.Length >= 7) 
                result["singlePrice"] = split[6];
            if(split.Length >= 8) 
                result["total"] = split[7];
            if(split.Length >= 9) 
                result["sold"] = split[8];
            if(split.Length >= 10) 
                result["available"] = split[9];
            if(split.Length >= 11) 
                result["barcode01"] = split[10];
            if(split.Length >= 12) 
                result["barcode02"] = split[11];
            if(split.Length >= 13) 
                result["barcode03"] = split[12];
            if(split.Length >= 14) 
                result["barcode04"] = split[13];
            if(split.Length >= 15) 
                result["itemName"] = split[14];
            if(split.Length >= 16) 
                result["measurementUnit"] = split[15];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation. // N
        /// <summary>
        /// 'N' - Returns data for the next found programmed item
        /// </summary>
        /// <param name="option">'N' - Returns data for the next found programmed item</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>PLU</term>
        /// <description>Item number (For ECRs 1...100000; For FPs 1...3000).</description>
        /// </item>
        /// <item>
        /// <term>taxGroup</term>
        /// <description>VAT group (letter 'A'...'H' or cyrillic 'А'...'З').</description>
        /// </item>
        /// <item>
        /// <term>department</term>
        /// <description>Department (0...99).</description>
        /// </item>
        /// <item>
        /// <term>group</term>
        /// <description>Stock group (1...99).</description>
        /// </item>
        /// <item>
        /// <term>priceType</term>
        /// <description>Price type ('0' - fixed price, '1' - free price, '2' - max price).</description>
        /// </item>
        /// <item>
        /// <term>singlePrice</term>
        /// <description>Price ( 0.00...9999999.99 or 0...999999999 depending dec point position).</description>
        /// </item>
        /// <item>
        /// <term>total</term>
        /// <description>Total count of the programmable items ( For ECRs: 100000; For FPs: 3000).</description>
        /// </item>
        /// <item>
        /// <term>sold</term>
        /// <description>Sold out quantity ( 0.001...99999.999).</description>
        /// </item>
        /// <item>
        /// <term>available</term>
        /// <description>Current quantity ( 0.001...99999.999).</description>
        /// </item>
        /// <item>
        /// <term>barcode01</term>
        /// <description>Barcode 1 (up to 13 digits).</description>
        /// </item>
        /// <item>
        /// <term>barcode02</term>
        /// <description>Barcode 2 (up to 13 digits).</description>
        /// </item>
        /// <item>
        /// <term>barcode03</term>
        /// <description>Barcode 3 (up to 13 digits).</description>
        /// </item>
        /// <item>
        /// <term>barcode04</term>
        /// <description>Barcode 4 (up to 13 digits).</description>
        /// </item>
        /// <item>
        /// <term>itemName</term>
        /// <description>Item name (up to 72 symbols).</description>
        /// </item>
        /// <item>
        /// <term>measurementUnit</term>
        /// <description>Measurement unit 0 - 19.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Get_NextItem(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");

            string r = CustomCommand(107 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["PLU"] = split[1];
            if(split.Length >= 3) 
                result["taxGroup"] = split[2];
            if(split.Length >= 4) 
                result["department"] = split[3];
            if(split.Length >= 5) 
                result["group"] = split[4];
            if(split.Length >= 6) 
                result["priceType"] = split[5];
            if(split.Length >= 7) 
                result["singlePrice"] = split[6];
            if(split.Length >= 8) 
                result["total"] = split[7];
            if(split.Length >= 9) 
                result["sold"] = split[8];
            if(split.Length >= 10) 
                result["available"] = split[9];
            if(split.Length >= 11) 
                result["barcode01"] = split[10];
            if(split.Length >= 12) 
                result["barcode02"] = split[11];
            if(split.Length >= 13) 
                result["barcode03"] = split[12];
            if(split.Length >= 14) 
                result["barcode04"] = split[13];
            if(split.Length >= 15) 
                result["itemName"] = split[14];
            if(split.Length >= 16) 
                result["measurementUnit"] = split[15];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Returns data about the first found item with sales on it
        /// </summary>
        /// <param name="option">'f' - Returns data about the first found item with sales on it</param>
        /// <param name="targetPLU">Item number ( For ECRs 1...100000; For FPs 1...3000 ). Default: 1</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>PLU</term>
        /// <description>Item number (For ECRs 1...100000; For FPs 1...3000).</description>
        /// </item>
        /// <item>
        /// <term>taxGroup</term>
        /// <description>VAT group (letter 'A'...'H' or cyrillic 'А'...'З').</description>
        /// </item>
        /// <item>
        /// <term>department</term>
        /// <description>Department (0...99).</description>
        /// </item>
        /// <item>
        /// <term>group</term>
        /// <description>Stock group (1...99).</description>
        /// </item>
        /// <item>
        /// <term>priceType</term>
        /// <description>Price type ('0' - fixed price, '1' - free price, '2' - max price).</description>
        /// </item>
        /// <item>
        /// <term>singlePrice</term>
        /// <description>Price ( 0.00...9999999.99 or 0...999999999 depending dec point position).</description>
        /// </item>
        /// <item>
        /// <term>total</term>
        /// <description>Total count of the programmable items ( For ECRs: 100000; For FPs: 3000).</description>
        /// </item>
        /// <item>
        /// <term>sold</term>
        /// <description>Sold out quantity ( 0.001...99999.999).</description>
        /// </item>
        /// <item>
        /// <term>available</term>
        /// <description>Current quantity ( 0.001...99999.999).</description>
        /// </item>
        /// <item>
        /// <term>barcode01</term>
        /// <description>Barcode 1 (up to 13 digits).</description>
        /// </item>
        /// <item>
        /// <term>barcode02</term>
        /// <description>Barcode 2 (up to 13 digits).</description>
        /// </item>
        /// <item>
        /// <term>barcode03</term>
        /// <description>Barcode 3 (up to 13 digits).</description>
        /// </item>
        /// <item>
        /// <term>barcode04</term>
        /// <description>Barcode 4 (up to 13 digits).</description>
        /// </item>
        /// <item>
        /// <term>itemName</term>
        /// <description>Item name (up to 72 symbols).</description>
        /// </item>
        /// <item>
        /// <term>measurementUnit</term>
        /// <description>Measurement unit 0 - 19.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Get_FirstSoldItem(string option, string targetPLU) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(targetPLU);
            inputString.Append("\t");

            string r = CustomCommand(107 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["PLU"] = split[1];
            if(split.Length >= 3) 
                result["taxGroup"] = split[2];
            if(split.Length >= 4) 
                result["department"] = split[3];
            if(split.Length >= 5) 
                result["group"] = split[4];
            if(split.Length >= 6) 
                result["priceType"] = split[5];
            if(split.Length >= 7) 
                result["singlePrice"] = split[6];
            if(split.Length >= 8) 
                result["total"] = split[7];
            if(split.Length >= 9) 
                result["sold"] = split[8];
            if(split.Length >= 10) 
                result["available"] = split[9];
            if(split.Length >= 11) 
                result["barcode01"] = split[10];
            if(split.Length >= 12) 
                result["barcode02"] = split[11];
            if(split.Length >= 13) 
                result["barcode03"] = split[12];
            if(split.Length >= 14) 
                result["barcode04"] = split[13];
            if(split.Length >= 15) 
                result["itemName"] = split[14];
            if(split.Length >= 16) 
                result["measurementUnit"] = split[15];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Returns data about the last found item with sales on it
        /// </summary>
        /// <param name="option">'l' - Returns data about the last found item with sales on it</param>
        /// <param name="targetPLU">Item number ( For ECRs 1...100000; For FPs 1...3000 ). Default: For ECRs 100000; For FPs 3000</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>PLU</term>
        /// <description>Item number (For ECRs 1...100000; For FPs 1...3000).</description>
        /// </item>
        /// <item>
        /// <term>taxGroup</term>
        /// <description>VAT group (letter 'A'...'H' or cyrillic 'А'...'З').</description>
        /// </item>
        /// <item>
        /// <term>department</term>
        /// <description>Department (0...99).</description>
        /// </item>
        /// <item>
        /// <term>group</term>
        /// <description>Stock group (1...99).</description>
        /// </item>
        /// <item>
        /// <term>priceType</term>
        /// <description>Price type ('0' - fixed price, '1' - free price, '2' - max price).</description>
        /// </item>
        /// <item>
        /// <term>singlePrice</term>
        /// <description>Price ( 0.00...9999999.99 or 0...999999999 depending dec point position).</description>
        /// </item>
        /// <item>
        /// <term>total</term>
        /// <description>Total count of the programmable items ( For ECRs: 100000; For FPs: 3000).</description>
        /// </item>
        /// <item>
        /// <term>sold</term>
        /// <description>Sold out quantity ( 0.001...99999.999).</description>
        /// </item>
        /// <item>
        /// <term>available</term>
        /// <description>Current quantity ( 0.001...99999.999).</description>
        /// </item>
        /// <item>
        /// <term>barcode01</term>
        /// <description>Barcode 1 (up to 13 digits).</description>
        /// </item>
        /// <item>
        /// <term>barcode02</term>
        /// <description>Barcode 2 (up to 13 digits).</description>
        /// </item>
        /// <item>
        /// <term>barcode03</term>
        /// <description>Barcode 3 (up to 13 digits).</description>
        /// </item>
        /// <item>
        /// <term>barcode04</term>
        /// <description>Barcode 4 (up to 13 digits).</description>
        /// </item>
        /// <item>
        /// <term>itemName</term>
        /// <description>Item name (up to 72 symbols).</description>
        /// </item>
        /// <item>
        /// <term>measurementUnit</term>
        /// <description>Measurement unit 0 - 19.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Get_LastSoldItem(string option, string targetPLU) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(targetPLU);
            inputString.Append("\t");

            string r = CustomCommand(107 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["PLU"] = split[1];
            if(split.Length >= 3) 
                result["taxGroup"] = split[2];
            if(split.Length >= 4) 
                result["department"] = split[3];
            if(split.Length >= 5) 
                result["group"] = split[4];
            if(split.Length >= 6) 
                result["priceType"] = split[5];
            if(split.Length >= 7) 
                result["singlePrice"] = split[6];
            if(split.Length >= 8) 
                result["total"] = split[7];
            if(split.Length >= 9) 
                result["sold"] = split[8];
            if(split.Length >= 10) 
                result["available"] = split[9];
            if(split.Length >= 11) 
                result["barcode01"] = split[10];
            if(split.Length >= 12) 
                result["barcode02"] = split[11];
            if(split.Length >= 13) 
                result["barcode03"] = split[12];
            if(split.Length >= 14) 
                result["barcode04"] = split[13];
            if(split.Length >= 15) 
                result["itemName"] = split[14];
            if(split.Length >= 16) 
                result["measurementUnit"] = split[15];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Returns data for the next found item with sales on it
        /// </summary>
        /// <param name="option">'n' - Returns data for the next found item with sales on it</param>
        /// <param name="targetPLU">Item number ( For ECRs 1...100000; For FPs 1...3000 ). Default: For ECRs 100000; For FPs 3000</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>PLU</term>
        /// <description>Item number (For ECRs 1...100000; For FPs 1...3000).</description>
        /// </item>
        /// <item>
        /// <term>taxGroup</term>
        /// <description>VAT group (letter 'A'...'H' or cyrillic 'А'...'З').</description>
        /// </item>
        /// <item>
        /// <term>department</term>
        /// <description>Department (0...99).</description>
        /// </item>
        /// <item>
        /// <term>group</term>
        /// <description>Stock group (1...99).</description>
        /// </item>
        /// <item>
        /// <term>priceType</term>
        /// <description>Price type ('0' - fixed price, '1' - free price, '2' - max price).</description>
        /// </item>
        /// <item>
        /// <term>singlePrice</term>
        /// <description>Price ( 0.00...9999999.99 or 0...999999999 depending dec point position).</description>
        /// </item>
        /// <item>
        /// <term>total</term>
        /// <description>Total count of the programmable items ( For ECRs: 100000; For FPs: 3000).</description>
        /// </item>
        /// <item>
        /// <term>sold</term>
        /// <description>Sold out quantity ( 0.001...99999.999).</description>
        /// </item>
        /// <item>
        /// <term>available</term>
        /// <description>Current quantity ( 0.001...99999.999).</description>
        /// </item>
        /// <item>
        /// <term>barcode01</term>
        /// <description>Barcode 1 (up to 13 digits).</description>
        /// </item>
        /// <item>
        /// <term>barcode02</term>
        /// <description>Barcode 2 (up to 13 digits).</description>
        /// </item>
        /// <item>
        /// <term>barcode03</term>
        /// <description>Barcode 3 (up to 13 digits).</description>
        /// </item>
        /// <item>
        /// <term>barcode04</term>
        /// <description>Barcode 4 (up to 13 digits).</description>
        /// </item>
        /// <item>
        /// <term>itemName</term>
        /// <description>Item name (up to 72 symbols).</description>
        /// </item>
        /// <item>
        /// <term>measurementUnit</term>
        /// <description>Measurement unit 0 - 19.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Get_NextSoldItem(string option, string targetPLU) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(targetPLU);
            inputString.Append("\t");

            string r = CustomCommand(107 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["PLU"] = split[1];
            if(split.Length >= 3) 
                result["taxGroup"] = split[2];
            if(split.Length >= 4) 
                result["department"] = split[3];
            if(split.Length >= 5) 
                result["group"] = split[4];
            if(split.Length >= 6) 
                result["priceType"] = split[5];
            if(split.Length >= 7) 
                result["singlePrice"] = split[6];
            if(split.Length >= 8) 
                result["total"] = split[7];
            if(split.Length >= 9) 
                result["sold"] = split[8];
            if(split.Length >= 10) 
                result["available"] = split[9];
            if(split.Length >= 11) 
                result["barcode01"] = split[10];
            if(split.Length >= 12) 
                result["barcode02"] = split[11];
            if(split.Length >= 13) 
                result["barcode03"] = split[12];
            if(split.Length >= 14) 
                result["barcode04"] = split[13];
            if(split.Length >= 15) 
                result["itemName"] = split[14];
            if(split.Length >= 16) 
                result["measurementUnit"] = split[15];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        ///  Find the first not programmed item
        /// </summary>
        /// <param name="option">'X' - Find the first not programmed item</param>
        /// <param name="targetPLU">Item number (For ECRs 1...100000; For FPs 1...3000). Default: 1</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>PLU</term>
        /// <description>Item number (For ECRs 1...100000; For FPs 1...3000).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Get_FirstNotProgrammedItem(string option, string targetPLU) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(targetPLU);
            inputString.Append("\t");

            string r = CustomCommand(107 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["PLU"] = split[1];
            return result;
        }

        // Command number(Dec): 107 - please check fiscal device documentation.
        /// <summary>
        /// Find the last not programmed item
        /// </summary>
        /// <param name="option">'x' - Find the last not programmed item</param>
        /// <param name="targetPLU">Item number ( For ECRs 1...100000; For FPs 1...3000 ). Default: For ECRs 100000; For FPs 3000</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>PLU</term>
        /// <description>Item number (For ECRs 1...100000; For FPs 1...3000).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> items_Get_LastNotProgrammedItem(string option, string targetPLU) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(targetPLU);
            inputString.Append("\t");

            string r = CustomCommand(107 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["PLU"] = split[1];
            return result;
        }

        // Command number(Dec): 140 - please check fiscal device documentation.
        /// <summary>
        /// Defining and reading clients - Clients programming
        /// </summary>
        /// <param name="option">'P' - Clients programming</param>
        /// <param name="clientNumber">Client number, index of record (1...1000)</param>
        /// <param name="clientName">Client's name (up to 36 chars)</param>
        /// <param name="typeTAXN">Тype of TAXN: '0' - BULSTAT; '1' - EGN; '2' - LNCH; '3' - service number</param>
        /// <param name="taxNumber">Client's tax number (9...13 chars);</param>
        /// <param name="recieverName">Reciever's name (up to 36 chars)</param>
        /// <param name="vatNumber">VAT number of the client (up to 14 chars)</param>
        /// <param name="address01">Client's address - line 1 (up to 36 chars)</param>
        /// <param name="address02">Client's address - line 2 (up to 36 chars)</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> clients_Set_ClientData(string option, string clientNumber, string clientName, string typeTAXN, string taxNumber, string recieverName, string vatNumber, string address01, string address02) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(clientNumber);
            inputString.Append("\t");
            inputString.Append(clientName);
            inputString.Append("\t");
            inputString.Append(typeTAXN);
            inputString.Append("\t");
            inputString.Append(taxNumber);
            inputString.Append("\t");
            inputString.Append(recieverName);
            inputString.Append("\t");
            inputString.Append(vatNumber);
            inputString.Append("\t");
            inputString.Append(address01);
            inputString.Append("\t");
            inputString.Append(address02);
            inputString.Append("\t");

            string r = CustomCommand(140 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 140 - please check fiscal device documentation.
        /// <summary>
        /// Defining and reading clients - Client deleting
        /// </summary>
        /// <param name="option">'D' - Client deleting</param>
        /// <param name="firstClientNumber">First client to delete (1...1000); </param>
        /// <param name="lastClientNumber">last client to delete (1...1000). Caqn be blank</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> clients_Del_ClientData(string option, string firstClientNumber, string lastClientNumber) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(firstClientNumber);
            inputString.Append("\t");
            inputString.Append(lastClientNumber);
            inputString.Append("\t");

            string r = CustomCommand(140 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 140 - please check fiscal device documentation.
        /// <summary>
        /// Defining and reading clients - Client deleting
        /// </summary>
        /// <param name="option">'D' - Client deleting</param>
        /// <param name="firstClientNumber">parameter has value 'A', all clients will be deleted</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> clients_Del_AllClientData(string option, string firstClientNumber) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(firstClientNumber);
            inputString.Append("\t");

            string r = CustomCommand(140 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 140 - please check fiscal device documentation.
        /// <summary>
        ///  Reading client data
        /// </summary>
        /// <param name="option">'R' - Reading client data</param>
        /// <param name="clientNumber">Client number (1...1000)</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>clientNumber</term>
        /// <description>Client number, index of record (1...1000).</description>
        /// </item>
        /// <item>
        /// <term>clientName</term>
        /// <description>Client's name (up to 36 chars).</description>
        /// </item>
        /// <item>
        /// <term>typeTAXNumver</term>
        /// <description>Тype of TAXN: '0' - BULSTAT; '1' - EGN; '2' - LNCH; '3' - service number.</description>
        /// </item>
        /// <item>
        /// <term>tAXNumber</term>
        /// <description>Client's tax number (9...13 chars).</description>
        /// </item>
        /// <item>
        /// <term>recieverName</term>
        /// <description>Reciever's name (up to 36 chars).</description>
        /// </item>
        /// <item>
        /// <term>vATNumber</term>
        /// <description>VAT number of the client (up to 14 chars).</description>
        /// </item>
        /// <item>
        /// <term>address01</term>
        /// <description>Client's address - line 1 (up to 36 chars).</description>
        /// </item>
        /// <item>
        /// <term>address02</term>
        /// <description>Client's address - line 2 (up to 36 chars).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> clients_Get_ClientData(string option, string clientNumber) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(clientNumber);
            inputString.Append("\t");

            string r = CustomCommand(140 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["clientNumber"] = split[1];
            if(split.Length >= 3) 
                result["clientName"] = split[2];
            if(split.Length >= 4) 
                result["typeTAXNumver"] = split[3];
            if(split.Length >= 5) 
                result["tAXNumber"] = split[4];
            if(split.Length >= 6) 
                result["recieverName"] = split[5];
            if(split.Length >= 7) 
                result["vATNumber"] = split[6];
            if(split.Length >= 8) 
                result["address01"] = split[7];
            if(split.Length >= 9) 
                result["address02"] = split[8];
            return result;
        }

        // Command number(Dec): 140 - please check fiscal device documentation.
        /// <summary>
        ///  Returns data about the first found programmed client
        /// </summary>
        /// <param name="option">'F' - Returns data about the first found programmed client</param>
        /// <param name="clientNumber">Client number (0...1000)</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>clientNumber</term>
        /// <description>Client number, index of record (1...1000).</description>
        /// </item>
        /// <item>
        /// <term>clientName</term>
        /// <description>Client's name (up to 36 chars).</description>
        /// </item>
        /// <item>
        /// <term>typeTAXNumver</term>
        /// <description>Тype of TAXN: '0' - BULSTAT; '1' - EGN; '2' - LNCH; '3' - service number.</description>
        /// </item>
        /// <item>
        /// <term>tAXNumber</term>
        /// <description>Client's tax number (9...13 chars).</description>
        /// </item>
        /// <item>
        /// <term>recieverName</term>
        /// <description>Reciever's name (up to 36 chars).</description>
        /// </item>
        /// <item>
        /// <term>vATNumber</term>
        /// <description>VAT number of the client (up to 14 chars).</description>
        /// </item>
        /// <item>
        /// <term>address01</term>
        /// <description>Client's address - line 1 (up to 36 chars).</description>
        /// </item>
        /// <item>
        /// <term>address02</term>
        /// <description>Client's address - line 2 (up to 36 chars).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> clients_Get_FirstProgrammedClient(string option, string clientNumber) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(clientNumber);
            inputString.Append("\t");

            string r = CustomCommand(140 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["clientNumber"] = split[1];
            if(split.Length >= 3) 
                result["clientName"] = split[2];
            if(split.Length >= 4) 
                result["typeTAXNumver"] = split[3];
            if(split.Length >= 5) 
                result["tAXNumber"] = split[4];
            if(split.Length >= 6) 
                result["recieverName"] = split[5];
            if(split.Length >= 7) 
                result["vATNumber"] = split[6];
            if(split.Length >= 8) 
                result["address01"] = split[7];
            if(split.Length >= 9) 
                result["address02"] = split[8];
            return result;
        }

        // Command number(Dec): 140 - please check fiscal device documentation.
        /// <summary>
        ///  Returns data about the last found programmed client
        /// </summary>
        /// <param name="option">'L' - Returns data about the last found programmed client</param>
        /// <param name="clientNumber">Client number (1...1000)</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>clientNumber</term>
        /// <description>Client number, index of record (1...1000).</description>
        /// </item>
        /// <item>
        /// <term>clientName</term>
        /// <description>Client's name (up to 36 chars).</description>
        /// </item>
        /// <item>
        /// <term>typeTAXNumver</term>
        /// <description>Тype of TAXN: '0' - BULSTAT; '1' - EGN; '2' - LNCH; '3' - service number.</description>
        /// </item>
        /// <item>
        /// <term>tAXNumber</term>
        /// <description>Client's tax number (9...13 chars).</description>
        /// </item>
        /// <item>
        /// <term>recieverName</term>
        /// <description>Reciever's name (up to 36 chars).</description>
        /// </item>
        /// <item>
        /// <term>vATNumber</term>
        /// <description>VAT number of the client (up to 14 chars).</description>
        /// </item>
        /// <item>
        /// <term>address01</term>
        /// <description>Client's address - line 1 (up to 36 chars).</description>
        /// </item>
        /// <item>
        /// <term>address02</term>
        /// <description>Client's address - line 2 (up to 36 chars).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> clients_Get_LastProgrammedClient(string option, string clientNumber) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(clientNumber);
            inputString.Append("\t");

            string r = CustomCommand(140 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["clientNumber"] = split[1];
            if(split.Length >= 3) 
                result["clientName"] = split[2];
            if(split.Length >= 4) 
                result["typeTAXNumver"] = split[3];
            if(split.Length >= 5) 
                result["tAXNumber"] = split[4];
            if(split.Length >= 6) 
                result["recieverName"] = split[5];
            if(split.Length >= 7) 
                result["vATNumber"] = split[6];
            if(split.Length >= 8) 
                result["address01"] = split[7];
            if(split.Length >= 9) 
                result["address02"] = split[8];
            return result;
        }

        // Command number(Dec): 140 - please check fiscal device documentation.
        /// <summary>
        ///  Returns data for the next found programmed client
        ///  Note: "clients_Get_LastProgrammedClient" or "clients_Get_FirstProgrammedClient" must be executed first. This determines whether to get next('F') or previous('L') client.
        /// </summary>
        /// <param name="option">'N' - Returns data for the next found programmed client</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>clientNumber</term>
        /// <description>Client number, index of record (1...1000).</description>
        /// </item>
        /// <item>
        /// <term>clientName</term>
        /// <description>Client's name (up to 36 chars).</description>
        /// </item>
        /// <item>
        /// <term>typeTAXNumver</term>
        /// <description>Тype of TAXN: '0' - BULSTAT; '1' - EGN; '2' - LNCH; '3' - service number.</description>
        /// </item>
        /// <item>
        /// <term>tAXNumber</term>
        /// <description>Client's tax number (9...13 chars).</description>
        /// </item>
        /// <item>
        /// <term>recieverName</term>
        /// <description>Reciever's name (up to 36 chars).</description>
        /// </item>
        /// <item>
        /// <term>vATNumber</term>
        /// <description>VAT number of the client (up to 14 chars).</description>
        /// </item>
        /// <item>
        /// <term>address01</term>
        /// <description>Client's address - line 1 (up to 36 chars).</description>
        /// </item>
        /// <item>
        /// <term>address02</term>
        /// <description>Client's address - line 2 (up to 36 chars).</description>
        /// </item>
        /// </list>
        /// </returns>
        ///  <seealso cref="clients_Get_FirstProgrammedClient()"/> to get next client.
        ///  <seealso cref="clients_Get_LastProgrammedClient()"/> to get previous client.
        public Dictionary<string, string> clients_Get_NextProgrammedClient(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");

            string r = CustomCommand(140 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["clientNumber"] = split[1];
            if(split.Length >= 3) 
                result["clientName"] = split[2];
            if(split.Length >= 4) 
                result["typeTAXNumver"] = split[3];
            if(split.Length >= 5) 
                result["tAXNumber"] = split[4];
            if(split.Length >= 6) 
                result["recieverName"] = split[5];
            if(split.Length >= 7) 
                result["vATNumber"] = split[6];
            if(split.Length >= 8) 
                result["address01"] = split[7];
            if(split.Length >= 9) 
                result["address02"] = split[8];
            return result;
        }

        // Command number(Dec): 140 - please check fiscal device documentation.
        /// <summary>
        ///  Find a client by tax number
        /// </summary>
        /// <param name="option">'T' - Find a client by tax number</param>
        /// <param name="taxNumber">Client's tax number (9...13 chars)</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>clientNumber</term>
        /// <description>Client number, index of record (1...1000).</description>
        /// </item>
        /// <item>
        /// <term>clientName</term>
        /// <description>Client's name (up to 36 chars).</description>
        /// </item>
        /// <item>
        /// <term>typeTAXNumver</term>
        /// <description>Тype of TAXN: '0' - BULSTAT; '1' - EGN; '2' - LNCH; '3' - service number.</description>
        /// </item>
        /// <item>
        /// <term>tAXNumber</term>
        /// <description>Client's tax number (9...13 chars).</description>
        /// </item>
        /// <item>
        /// <term>recieverName</term>
        /// <description>Reciever's name (up to 36 chars).</description>
        /// </item>
        /// <item>
        /// <term>vATNumber</term>
        /// <description>VAT number of the client (up to 14 chars).</description>
        /// </item>
        /// <item>
        /// <term>address01</term>
        /// <description>Client's address - line 1 (up to 36 chars).</description>
        /// </item>
        /// <item>
        /// <term>address02</term>
        /// <description>Client's address - line 2 (up to 36 chars).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> clients_Get_ClientByTaxNumber(string option, string taxNumber) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(taxNumber);
            inputString.Append("\t");

            string r = CustomCommand(140 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["clientNumber"] = split[1];
            if(split.Length >= 3) 
                result["clientName"] = split[2];
            if(split.Length >= 4) 
                result["typeTAXNumver"] = split[3];
            if(split.Length >= 5) 
                result["tAXNumber"] = split[4];
            if(split.Length >= 6) 
                result["recieverName"] = split[5];
            if(split.Length >= 7) 
                result["vATNumber"] = split[6];
            if(split.Length >= 8) 
                result["address01"] = split[7];
            if(split.Length >= 9) 
                result["address02"] = split[8];
            return result;
        }

        // Command number(Dec): 140 - please check fiscal device documentation.
        /// <summary>
        /// Find the first not programmed client
        /// </summary>
        /// <param name="option">'X' - Find the first not programmed client</param>
        /// <param name="clientNumber">Client number (0...1000).</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>clientNumber</term>
        /// <description>Client number (1...1000).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> clients_Get_FirstNotProgrammed(string option, string clientNumber) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(clientNumber);
            inputString.Append("\t");

            string r = CustomCommand(140 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["clientNumber"] = split[1];
            return result;
        }

        // Command number(Dec): 140 - please check fiscal device documentation.
        /// <summary>
        /// Find the last not programmed client
        /// </summary>
        /// <param name="option">'x' - Find the last not programmed client</param>
        /// <param name="clientNumber">Client number (1...1000).</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>clientNumber</term>
        /// <description>Client number (1...1000).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> clients_Get_LastNotProgrammed(string option, string clientNumber) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(clientNumber);
            inputString.Append("\t");

            string r = CustomCommand(140 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["clientNumber"] = split[1];
            return result;
        }

        // Command number(Dec): 61 - please check fiscal device documentation.
        /// <summary>
        /// Set date and time
        /// </summary>
        /// <param name="dateTime">Date and time in format: "DD-MM-YY hh:mm:ss DST (DD - day; MM - month; YY - year; hh - hour; mm - minute; ss - seconds; DST - Text "DST" if exist time is Summer time)</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_DateTime(string dateTime) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(dateTime);
            inputString.Append("\t");

            string r = CustomCommand(61 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 66 - please check fiscal device documentation.
        /// <summary>
        /// Set invoice interval\n
        /// If the current invoice counter have reached the end of the interval
        /// </summary>
        /// <param name="startValue">The starting number of the interval. Max 10 digits (1...9999999999)</param>
        /// <param name="endValue">The ending number of the interval. Max 10 digits (1...9999999999)</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>valueStart</term>
        /// <description>The current starting value of the interval (1...9999999999).</description>
        /// </item>
        /// <item>
        /// <term>valueEnd</term>
        /// <description>The current ending value of the interval (1...9999999999).</description>
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
            inputString.Append("\t");
            inputString.Append(endValue);
            inputString.Append("\t");

            string r = CustomCommand(66 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["valueStart"] = split[1];
            if(split.Length >= 3) 
                result["valueEnd"] = split[2];
            if(split.Length >= 4) 
                result["valueCurrent"] = split[3];
            return result;
        }

        // Command number(Dec): 66 - please check fiscal device documentation.
        /// <summary>
        /// Set invoice interval\n
        /// If the current invoice counter didn't reached the end of the interval
        /// </summary>
        /// <param name="endValue">The ending number of the interval. Max 10 digits (1...9999999999).</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>valueStart</term>
        /// <description>The current starting value of the interval (1...9999999999).</description>
        /// </item>
        /// <item>
        /// <term>valueEnd</term>
        /// <description>The current ending value of the interval (1...9999999999).</description>
        /// </item>
        /// <item>
        /// <term>valueCurrent</term>
        /// <description>The current invoice receipt number (1...9999999999).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_InvoiceRange_01(string endValue) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(endValue);
            inputString.Append("\t");

            string r = CustomCommand(66 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["valueStart"] = split[1];
            if(split.Length >= 3) 
                result["valueEnd"] = split[2];
            if(split.Length >= 4) 
                result["valueCurrent"] = split[3];
            return result;
        }

        // Command number(Dec): 101 - please check fiscal device documentation.
        /// <summary>
        /// Set operator password
        /// </summary>
        /// <param name="operatorCode">Operator number from 1...30</param>
        /// <param name="oldPassword">Operator old password or administrator (oper29 & oper30) password. Can be blank if service jumper is on.</param>
        /// <param name="newPassword">Operator password, ascii string of digits. Lenght from 1...8</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_OperatorPassword(string operatorCode, string oldPassword, string newPassword) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(operatorCode);
            inputString.Append("\t");
            inputString.Append(oldPassword);
            inputString.Append("\t");
            inputString.Append(newPassword);
            inputString.Append("\t");

            string r = CustomCommand(101 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 127 - please check fiscal device documentation.
        /// <summary>
        /// Stamp operations - set stamp name
        /// </summary>
        /// <param name="option">'1' - Rename loaded stamp with command 203</param>
        /// <param name="stampName">Name of stamp as filename in format 8.3</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_StampName(string option, string stampName) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(stampName);
            inputString.Append("\t");

            string r = CustomCommand(127 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 202 - please check fiscal device documentation.
        /// <summary>
        /// Customer graphic logo loading 
        /// </summary>
        /// <param name="paramValue">START - Praparation for data loading; POWEROFF - Shutting down the device; RESTART - Device restarting</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Start_LogoLoading(string paramValue) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(paramValue);
            inputString.Append("\t");

            string r = CustomCommand(202 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 202 - please check fiscal device documentation.
        /// <summary>
        ///  base64 coded data of the graphic logo
        /// </summary>
        /// <param name="dataValue">YmFzZTY0ZGF0YQ== - base64 coded data of the grahpic logo</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>checkSum</term>
        /// <description>Sum of decoded base64 data.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Logo_Loading(string dataValue) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(dataValue);
            inputString.Append("\t");

            string r = CustomCommand(202 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["checkSum"] = split[1];
            return result;
        }

        // Command number(Dec): 202 - please check fiscal device documentation.
        /// <summary>
        /// End of data
        /// </summary>
        /// <param name="paramValue">STOP - End of data</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>checkSum</term>
        /// <description>Sum of decoded base64 data.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Stop_LogoLoading(string paramValue) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(paramValue);
            inputString.Append("\t");

            string r = CustomCommand(202 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 203 - please check fiscal device documentation.
        /// <summary>
        /// Stamp image loading 
        /// </summary>
        /// <param name="paramValue">START - Praparation for data loading</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Start_StampLoading(string paramValue) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(paramValue);
            inputString.Append("\t");

            string r = CustomCommand(203 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 203 - please check fiscal device documentation.
        /// <summary>
        ///  base64 coded data of the graphic logo
        /// </summary>
        /// <param name="dataValue">YmFzZTY0ZGF0YQ== - base64 coded data of the graphic logo</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>checkSum</term>
        /// <description>Sum of decoded base64 data.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Stamp_Loading(string dataValue) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(dataValue);
            inputString.Append("\t");

            string r = CustomCommand(203 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["checkSum"] = split[1];
            return result;
        }

        // Command number(Dec): 203 - please check fiscal device documentation.
        /// <summary>
        ///  End of data
        /// </summary>
        /// <param name="paramValue">STOP - End of data</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>checkSum</term>
        /// <description>Sum of decoded base64 data.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Stop_StampLoading(string paramValue) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(paramValue);
            inputString.Append("\t");

            string r = CustomCommand(203 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Baud rate of COM port for communication with PC (from 0 to 9)
        /// </summary>
        /// <param name="variableName">FpComBaudRate</param>
        /// <param name="index">leave blank or "0"</param>
        /// <param name="value">The value of baud rate to be set</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_FpComBaudRate(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Permission/rejection of the automatic cutting of paper after each
        /// receipt. ( 1 - permitted, 0 - rejected ) (FP-700X only)
        /// </summary>
        /// <param name="variableName">AutoPaperCutting</param>
        /// <param name="index">leave blank or 0</param>
        /// <param name="value">1 - permitted, 0 - rejected</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_AutoPaperCutting(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Partial=0/Full=1 cutting of paper (FP-700X only)
        /// </summary>
        /// <param name="variableName"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_PaperCuttingType(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Set barcode height from '1' (7mm) to '10' (70mm)
        /// </summary>
        /// <param name="variableName">BarCodeHeight</param>
        /// <param name="index">leave blank or 0</param>
        /// <param name="value"> - '1' (7mm) to '10' (70mm)</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_BarCodeHeight(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Set Enable/Disable printing of the barcode data
        /// </summary>
        /// <param name="variableName">BarcodeName</param>
        /// <param name="index">leave blank or 0</param>
        /// <param name="value">BarcodeName - Enable/Disable printing of the barcode data</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_BarcodeName(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Set Com port baud rate 
        /// </summary>
        /// <param name="variableName">ComPortBaudRate</param>
        /// <param name="index">Number of COM port is determined by "Index"</param>
        /// <param name="value">Baud rate of COM port that has peripheral device assigned.(from 0 to 999999)</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_ComPortBaudRate(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Set com port protocol - Protocol for communication with peripheral device assigned COM
        /// port. (from 0 to 9 ), if device is scale; .
        /// </summary>
        /// <param name="variableName">ComPortProtocol</param>
        /// <param name="index">Number of COM port </param>
        /// <param name="value">(from 0 to 9 )</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_ComPortProtocol(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Set PC interface type. 
        /// </summary>
        /// <param name="variableName">MainInterfaceType</param>
        /// <param name="index">leave blank or 0</param>
        /// <param name="value">0-auto select, 1-RS232, 2-BLUETOOTH, 3-USB, 4-LAN</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_MainInterfaceType(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Set time out between fiscal printer commands before start auto  print(in milliseconds ). 
        /// </summary>
        /// <param name="variableName">TimeOutBeforePrintFlush</param>
        /// <param name="index"></param>
        /// <param name="value">1...999999999</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_TimeOutBeforePrintFlush(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// FPr works with battery on main supply 
        /// </summary>
        /// <param name="variableName">WorkBatteryIncluded</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - enable; 0 - disable</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_WorkBatteryIncluded(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        ///  Set line spacing - Decrease the space between text lines. Greater values = less line spacing
        /// </summary>
        /// <param name="variableName">Dec2xLineSpacing</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">0...5 - default 0</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_Dec2xLineSpacing(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Printer font type. 
        /// </summary>
        /// <param name="variableName">PrintFontType</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">0: default, coarser with a small line spacing, 1: smaller, with greater spacing between rows</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_PrintFontType(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Number of blank lines for proper paper cutting
        /// </summary>
        /// <param name="variableName">FooterEmptyLines</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">Number of blank lines for proper paper cutting</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_FooterEmptyLines(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        ///  Minimum number of lines from the header after printing the footer
        /// </summary>
        /// <param name="variableName">HeaderMinLines</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">Minimum number of lines from the header after printing the footer</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_HeaderMinLines(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        ///  Print the logo after rows to push the paper. 
        /// </summary>
        /// <param name="variableName">LogoPrintAfterFooter</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1: yes, 0: no. default:0</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_LogoPrintAfterFooter(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        ///  Handling of near paper end. 
        /// </summary>
        /// <param name="variableName">EnableNearPaperEnd</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">0: No handling, 1: handling (default)</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_EnableNearPaperEnd(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        ///  Synchronize date/time from the NRA server (0 - sync, 1 - does not sync)
        /// </summary>
        /// <param name="variableName">DateFromNAPServDisable</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">0 - sync, 1 - does not sync</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_DateFromNAPServDisable(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// AutoPowerOff - Minutes to automatically turn off ECR if it is idle. (0 - disable; from 1  minute to 15 minutes)
        /// </summary>
        /// <param name="variableName">AutoPowerOff</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">0 - disable; from 1  minute to 15 minutes</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_AutoPowerOff(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Minutes to automatically turn off Backlight of the display if FPr is idle
        /// </summary>
        /// <param name="variableName">BkLight_AutoOff</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">0 - disable; from 1 minute to 5 minutes</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_BkLight_AutoOff(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Number of COM port for communication with pinpad
        /// </summary>
        /// <param name="variableName">PinpadComPort</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1-COM1, 2- COM2, 4-Bluetooth</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_PinpadComPort(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        public Dictionary<string, string> config_Set_PinpadShortRec(string variableName, string index, string value)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255, inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: " + split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Baud rate of COM port that has pinpad device assigned
        /// </summary>
        /// <param name="variableName">PinpadComBaudRate</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">from 0 to 9</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_PinpadComBaudRate(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Type of pinpad
        /// </summary>
        /// <param name="variableName">PinpadType</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - BORICA; 2 - UBB; 3 - DSK></param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_PinpadType(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Type of connection between cash register and bank server
        /// </summary>
        /// <param name="variableName">PinpadConnectionType</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">0-GPRS, 1-LAN</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_PinpadConnectionType(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Copies of the receipt from pinpad
        /// </summary>
        /// <param name="variableName">PinpadReceiptCopies</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">0 - 3</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_PinpadReceiptCopies(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Where to print pinpad receipt
        /// </summary>
        /// <param name="variableName">PinpadReceiptInfo</param>
        /// <param name="index"></param>
        /// <param name="value">1 - in fiscal receipt; 0 - separate from fiscal receipt</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_PinpadReceiptInfo(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Function of PY2 key in registration. Works only with configuration with BORICA
        /// </summary>
        /// <param name="variableName">PinpadPaymentMenu</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - menu for payment with pinpad(card and loyalty scheme); 0 - payment with card with pinpad</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_PinpadPaymentMenu(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Function of PY4 key. Works only with configuration with BORICA
        /// </summary>
        /// <param name="variableName">PinpadLoyaltyPayment</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - payment with pinpad with loyalty scheme; 0 - payment PY4</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_PinpadLoyaltyPayment(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Turn on / off bluetooth module (Only )
        /// </summary>
        /// <param name="variableName">BthEnable</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - on, 0 - off</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_BthEnable(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Тurn on / off bluetooth device discoverability
        /// </summary>
        /// <param name="variableName">BthDiscoverability</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - discoverable; 0 - non-discoverable</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_BthDiscoverability(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Bluetooth pairing
        /// </summary>
        /// <param name="variableName">BthPairing</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">0-unsecure, 1-reset and save, 2-reset</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_BthPairing(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Pin code for bluetooth pairing
        /// </summary>
        /// <param name="variableName">BthPinCode</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">Four digits</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_BthPinCode(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Firmware version of bluetooth module
        /// </summary>
        /// <param name="variableName">BthVersion</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">firmware version of bluetooth module</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_BthVersion(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Bluetooth device address
        /// </summary>
        /// <param name="variableName">BthAddress</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">bluetooth device address</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_BthAddress(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Logical number in the workplace
        /// </summary>
        /// <param name="variableName">EcrLogNumber</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">from 1 to 9999</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_EcrLogNumber(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Type of the receipt
        /// </summary>
        /// <param name="variableName">EcrExtendedReceipt</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - extended, 0 - simplified</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_EcrExtendedReceipt(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Work with constituents
        /// </summary>
        /// <param name="variableName">EcrDoveriteli</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1-enable( in one receipt only one constituent ), 0 - disable</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_EcrDoveriteli(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Work without passwords 
        /// </summary>
        /// <param name="variableName">EcrWithoutPasswords</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - enable; 0 - disable</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_EcrWithoutPasswords(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Require password after each receipt
        /// </summary>
        /// <param name="variableName">EcrAskForPassword</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - enable; 0 - disable</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_EcrAskForPassword(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Require password for void operations
        /// </summary>
        /// <param name="variableName">EcrAskForVoidPassword</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - enable; 0 - disable</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_EcrAskForVoidPassword(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// When making Z-report, automatically make "Operator report"
        /// </summary>
        /// <param name="variableName">EcrConnectedOperReport</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - enable; 0 - disable</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_EcrConnectedOperReport(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// When making Z-report, automatically make "Report by Departments"
        /// </summary>
        /// <param name="variableName">EcrConnectedDeptReport</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - enable; 0 - disable</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_EcrConnectedDeptReport(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// When making Z-report, automatically make "Report by PLU with turnovers"
        /// </summary>
        /// <param name="variableName">EcrConnectedPluSalesReport</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - enable; 0 - disable</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_EcrConnectedPluSalesReport(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// When making Z-report, automatically make "Group report"
        /// </summary>
        /// <param name="variableName">EcrConnectedGroupsReport</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - enable; 0 - disable</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_EcrConnectedGroupsReport(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// When making Z-report, automatically make "Ecr report"
        /// </summary>
        /// <param name="variableName">EcrConnectedCashReport</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - enable; 0 - disable</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_EcrConnectedCashReport(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// When making Z-report, automatically make "Ecr report"
        /// </summary>
        /// <param name="variableName">EcrConnectedCashReport</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - enable; 0 - disable</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_EcrUserPeriodReports(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// When making Z-report, automatically clear PLU turnover
        /// </summary>
        /// <param name="variableName">EcrPluDailyClearing</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - enable; 0 - disable</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_EcrPluDailyClearing(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Open drawer on every total
        /// </summary>
        /// <param name="variableName">EcrSafeOpening</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - enable; 0 - disable</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_EcrSafeOpening(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// If second number of the weight barcode match any of the symbols in this string, barcode will be interpreted as normal barcode
        /// </summary>
        /// <param name="variableName">EcrScaleBarMask</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">Text up to 10 symbols.</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_EcrScaleBarMask(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Count of used barcodes for each programmed article
        /// </summary>
        /// <param name="variableName">EcrNumberBarcode</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1...4</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_EcrNumberBarcode(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Time to clear display after last receipt in miliseconds
        /// </summary>
        /// <param name="variableName">RegModeOnIdle</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - 2 147 483 647</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_RegModeOnIdle(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// For ECR's only. The receipt is printed after last payment
        /// </summary>
        /// <param name="variableName">FlushAtEndOnly</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1- enable, 0 - disable</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_FlushAtEndOnly(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// For ECR's only. Minutes before midnight, when ECR starts showing warning for Z report
        /// </summary>
        /// <param name="variableName">EcrMidnightWarning</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">Minutes</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_EcrMidnightWarning(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// For ECR's only. The operator must press STL key before payment
        /// </summary>
        /// <param name="variableName">EcrMandatorySubtotal</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1: yes, 0: no. default: 0</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_EcrMandatorySubtotal(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// For ECR's only; Name of the seller
        /// </summary>
        /// <param name="variableName">Seller</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">36 symbols max</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_Seller(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// For ECR's only; Flag for a monthly report suggesting
        /// </summary>
        /// <param name="variableName">AutoMonthReport</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1: yes, 0: no, default: 1</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_AutoMonthReport(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// For ECR's only; Warning for unsent documents from XX hours. The value must be set in hours before device will be blocked
        /// </summary>
        /// <param name="variableName">EcrUnsentWarning</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">0: no, 1: yes</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_EcrUnsentWarning(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Local currency name
        /// </summary>
        /// <param name="variableName">CurrNameLocal</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">up to 3 chars</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_CurrNameLocal(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Foreign currency name
        /// </summary>
        /// <param name="variableName">CurrNameForeign</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">up to 3 chars</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_CurrNameForeign(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Exchange rate 
        /// </summary>
        /// <param name="variableName">ExchangeRate</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">from 1 to 999999999, decimal point is before last five digits</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_ExchangeRate(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Unit name
        /// </summary>
        /// <param name="variableName">Unit_name</param>
        /// <param name="index">Index 0 is for line 1...Index 19 is for line 20</param>
        /// <param name="value">Text up to 6 chars</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_Unit_name(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Text up to XX symbols for header
        /// </summary>
        /// <param name="variableName">Header</param>
        /// <param name="index">Index 0 is for line 1, Index 9 is for line 10</param>
        /// <param name="value">
        /// <list type="table">
        /// <item>
        /// <term>for FP-700X</term>
        /// <description>42, 48 or 64 columns</description>
        /// </item>
        /// <item>
        /// <term>for FMP-350X</term>
        /// <description>32 columns</description>
        /// </item>
        /// <item>
        /// <term>for FMP-55X</term>
        /// <description>42, 48 or 64 columns</description>
        /// </item>
        /// <item>
        /// <term>for DP-25X, DP-150X, WP-500X, WP-25X, WP-50X</term>
        /// <description>42 columns</description>
        /// </item>
        /// </list>
        /// </param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_Header(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Text up to XX symbols for footer
        /// </summary>
        /// <param name="variableName">Footer</param>
        /// <param name="index">Index 0 is for line 1, Index 9 is for line 10</param>
        /// <param name="value">
        /// <list type="table">
        /// <item>
        /// <term>for FP-700X</term>
        /// <description>42, 48 or 64 columns</description>
        /// </item>
        /// <item>
        /// <term>for FMP-350X</term>
        /// <description>42, 48 or 64 columns</description>
        /// </item>
        /// <item>
        /// <term>for FMP-55X</term>
        /// <description>32 columns</description>
        /// </item>
        /// <item>
        /// <term>for DP-25X, DP-150X, WP-500X, WP-25X, WP-50X</term>
        /// <description>42 columns</description>
        /// </item>
        /// </list>
        /// </param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_Footer(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Name of operator
        /// </summary>
        /// <param name="variableName">OperName</param>
        /// <param name="index">Number of operator (from 1 to 30)</param>
        /// <param name="value">Text up to 32 symbols</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_OperName(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Password of operator; Require Service jumper!
        /// </summary>
        /// <param name="variableName">OperPasw</param>
        /// <param name="index">Number of operator (from 1 to 30)</param>
        /// <param name="value">Text up to 8 symbols</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_OperPasw(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Name of payment. 
        /// </summary>
        /// <param name="variableName">PayName</param>
        /// <param name="index">Number of payment</param>
        /// <param name="value">Text up to 16 symbols</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_PayName(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Forbid the payment
        /// </summary>
        /// <param name="variableName">Payment_forbidden</param>
        /// <param name="index">Number of payment</param>
        /// <param name="value">1- forbidden, 0 - not forbidden</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_Payment_forbidden(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Number of PLU assigned to shortcut key (Only for ECRs)
        /// </summary>
        /// <param name="variableName">DPxx_PluCode</param>
        /// <param name="index">Number of key</param>
        /// <param name="value">0 - Key is disabled; from 1 to 99999 for assigning PLU</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_DPxx_PluCode(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Value for value surcharge
        /// </summary>
        /// <param name="variableName">KeyNDB_value</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">Value is in cents (from 0 to 999999999)</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_KeyNDB_value(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Percentage for percentage surcharge. Value is in hundredths (0.01) of a percent.
        /// </summary>
        /// <param name="variableName">KeyNDB_percentage</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">from 0 to 9999</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_KeyNDB_percentage(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Value for value discount. Value is in cents
        /// </summary>
        /// <param name="variableName">KeyOTS_value</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">from 0 to 999999999</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_KeyOTS_value(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Percentage for percentage discount; Value is in hundredths (0.01) of a percent
        /// </summary>
        /// <param name="variableName">KeyOTS_percentage</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">from 0 to 9999</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_KeyOTS_percentage(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Forbid the surcharge key
        /// </summary>
        /// <param name="variableName">KeyNDB_forbidden</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1- forbidden, 0 - not forbidden</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_KeyNDB_forbidden(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Forbid the discount key
        /// </summary>
        /// <param name="variableName">KeyOTS_forbidden</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1- forbidden, 0 - not forbidden</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_KeyOTS_forbidden(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Password of the Service man. Require service jumper
        /// </summary>
        /// <param name="variableName">ServPasw</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">Text up to 8 symbols</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_ServPasw(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Message that will be printed when "ServDate" is reached, up to 64 symbols
        /// </summary>
        /// <param name="variableName">ServMessage</param>
        /// <param name="index">Message line number</param>
        /// <param name="value">up to 64 symbols</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_ServMessage(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Service date
        /// </summary>
        /// <param name="variableName">ServiceDate</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">date (Format: DD-MM-YY HH:MM:SS)</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_ServiceDate(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Contrast of Printing
        /// </summary>
        /// <param name="variableName">PrnQuality</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">from 0 to 20</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_PrnQuality(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Number of printer columns
        /// </summary>
        /// <param name="variableName">PrintColumns</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">
        /// <list type="table">
        /// <item>
        /// <term>for FP-700X</term>
        /// <description>42, 48 or 64 columns</description>
        /// </item>
        /// <item>
        /// <term>for FMP-350X</term>
        /// <description>32 columns</description>
        /// </item>
        /// <item>
        /// <term>for FMP-55X</term>
        /// <description>42, 48 or 64 columns</description>
        /// </item>
        /// <item>
        /// <term>for DP-25X, DP-150X, WP-500X, WP-25X, WP-50X</term>
        /// <description>42 columns</description>
        /// </item>
        /// </list>
        /// </param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_PrintColumns(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Print empty line after TOTAL line in fiscal receipts
        /// </summary>
        /// <param name="variableName">EmptyLineAfterTotal</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - enable, 0 -disable</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_EmptyLineAfterTotal(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Print TOTAL line in fiscal receipts with double height
        /// </summary>
        /// <param name="variableName">DblHeigh_totalinreg</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - enable, 0 -disable</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_DblHeigh_totalinreg(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Bold print of the payment names in fiscal receipt
        /// </summary>
        /// <param name="variableName">Bold_payments</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - enable, 0 -disable</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_Bold_payments(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Print dublicate receipt
        /// </summary>
        /// <param name="variableName">DublReceipts</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - enable, 0 -disable</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_DublReceipts(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Number of internal receipts
        /// </summary>
        /// <param name="variableName">IntUseReceipts</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">from 0 to 9</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_IntUseReceipts(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Print PLU barcode in the receipt
        /// </summary>
        /// <param name="variableName">BarcodePrint</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - enable, 0 -disable</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_BarcodePrint(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Print the logo in the receipt
        /// </summary>
        /// <param name="variableName">LogoPrint</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - enable, 0 -disable</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_LogoPrint(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Print the department name at the beginning of the receipt
        /// </summary>
        /// <param name="variableName">DoveritelPrint</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - enable, 0 - disable</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_DoveriteliPrint(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Print total sum in foreign currency
        /// </summary>
        /// <param name="variableName">ForeignPrint</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - enable, 0 -disable, 2 - print exchange rate</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_ForeignPrint(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Print VAT rates in the receipt
        /// </summary>
        /// <param name="variableName">VatPrintEnable</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - enable, 0 -disable</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_VatPrintEnable(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        
        public Dictionary<string, string> config_Set_CondensedPrint(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Disable Z report generating from the keyboard
        /// </summary>
        /// <param name="variableName">DsblKeyZreport</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - disabled, 0 - enabled</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_DsblKeyZreport(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Disable X report generating from the keyboard
        /// </summary>
        /// <param name="variableName">DsblKeyXreport</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - disabled, 0 - enabled</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_DsblKeyXreport(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Disable diagnostic info
        /// </summary>
        /// <param name="variableName">DsblKeyDiagnostics</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - disabled, 0 - enabled</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_DsblKeyDiagnostics(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Disable fiscal memory reports
        /// </summary>
        /// <param name="variableName">DsblKeyFmReports</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - disabled, 0 - enabled</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_DsblKeyFmReports(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Disable electronic journal menu
        /// </summary>
        /// <param name="variableName">DsblKeyJournal</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - disabled, 0 - enabled</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_DsblKeyJournal(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Disable changing the date and time
        /// </summary>
        /// <param name="variableName">DsblKeyDateTime</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - disabled, 0 - enabled</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_DsblKeyDateTime(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Disable manualy closing of the receipt
        /// </summary>
        /// <param name="variableName">DsblKeyCloseReceipt</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - disabled, 0 - enabled</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_DsblKeyCloseReceipt(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Disable manualy cancellation of the receipt
        /// </summary>
        /// <param name="variableName">DsblKeyCancelReceipt</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - disabled, 0 - enabled</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_DsblKeyCancelReceipt(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Model of the modem
        /// </summary>
        /// <param name="variableName">ModemModel</param>
        /// <param name="index"></param>
        /// <param name="value">0 - Quectel M72, 1 - Quectel UC20, 2 - Quectel M66, 3- Quectel UG96</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_ModemModel(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// PIN code of SIM card
        /// </summary>
        /// <param name="variableName">SimPin</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">Text up to 16 symbols</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_SimPin(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// MAC address of the LAN controller
        /// </summary>
        /// <param name="variableName">LanMAC</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">up to 12 chars</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_LanMAC(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Enable use of DHCP
        /// </summary>
        /// <param name="variableName">DHCPenable</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - enable, 0 -disable</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_DHCPenable(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// IP address when DHCP is disabled
        /// </summary>
        /// <param name="variableName">LAN_IP</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">up to 15 chars</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_LAN_IP(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Net mask when DHCP is disabled
        /// </summary>
        /// <param name="variableName">LAN_NetMask</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">up to 15 chars</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_LAN_NetMask(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Default gateway when DHCP is disabled
        /// </summary>
        /// <param name="variableName">LAN_Gateway</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">up to 15 chars</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_LAN_Gateway(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Primary DNS when DHCP is disabled
        /// </summary>
        /// <param name="variableName">LAN_PriDNS</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">up to 15 chars</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_LAN_PriDNS(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Second DNS when DHCP is disabled
        /// </summary>
        /// <param name="variableName">LAN_SecDNS</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">up to 15 chars</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_LAN_SecDNS(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// The number of listening port for PC connection (only for devices with LAN)
        /// </summary>
        /// <param name="variableName">LANport_fpCommands</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">default: 4999 </param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_LANport_fpCommands(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        ///Name of item group
        /// </summary>
        /// <param name="variableName">ItemGroups_name</param>
        /// <param name="index">Number of item group</param>
        /// <param name="value">Text up to 32 symbols</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_ItemGroups_name(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Programmed price of department
        /// </summary>
        /// <param name="variableName">Dept_price</param>
        /// <param name="index">Number of department</param>
        /// <param name="value">from 0 to 999999999</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_Dept_price(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Name of department
        /// </summary>
        /// <param name="variableName">Dept_name</param>
        /// <param name="index">Number of department</param>
        /// <param name="value">Text up to 72 symbols</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_Dept_name(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Flag that tells if the entered tovaritelnica has to be checked with DHL's algorithm(only in DP-05C)
        /// </summary>
        /// <param name="variableName">DHL_Algo</param>
        /// <param name="index">Leave blak or 0</param>
        /// <param name="value">1 - yes, 0 - no</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_DHL_Algo(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Flag that tells if the entered EIK number has to be valid
        /// </summary>
        /// <param name="variableName">EIK_validation</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - yes, 0 - no</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_EIK_validation(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Flag that tells if the entered EGN number has to be valid
        /// </summary>
        /// <param name="variableName">EGN_validation</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">1 - yes, 0 - no</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_EGN_validation(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Description of the bonus
        /// </summary>
        /// <param name="variableName">Bonuses</param>
        /// <param name="index">Number of bonus</param>
        /// <param name="value">Text up to 40 symbols</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_Bonuses(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Free text lines describing reason for reduced VAT
        /// </summary>
        /// <param name="variableName">TextReducedVAT</param>
        /// <param name="index">Number of line</param>
        /// <param name="value">Text up to 42 symbols</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_TextReducedVAT(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// TAX number label
        /// </summary>
        /// <param name="variableName">TAXlabel</param>
        /// <param name="index">Leave blank or 0</param>
        /// <param name="value">up to 10 chars</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> config_Set_TAXlabel(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 50 - please check fiscal device documentation.
        /// <summary>
        /// Return the active VAT rates
        /// </summary>
        /// <returns>
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>nZreport</term>
        /// <description>Number of first Z report</description>
        /// </item>
        ///  <item>
        /// <term>taxA</term>
        /// <description>Value of Tax group A (0.00...99.99 taxable,100.00=disabled)</description>
        /// </item>
        /// <item>
        /// <term>taxB</term>
        /// <description>Value of Tax group B (0.00...99.99 taxable,100.00=disabled)</description>
        /// </item>
        /// <item>
        /// <term>taxC</term>
        /// <description>Value of Tax group C (0.00...99.99 taxable,100.00=disabled)</description>
        /// </item>
        /// <item>
        /// <term>taxD</term>
        /// <description>Value of Tax group D (0.00...99.99 taxable,100.00=disabled)</description>
        /// </item>
        /// <item>
        /// <term>taxE</term>
        /// <description>Value of Tax group E (0.00...99.99 taxable,100.00=disabled)</description>
        /// </item>
        /// <item>
        /// <term>taxF</term>
        /// <description>Value of Tax group F (0.00...99.99 taxable,100.00=disabled)</description>
        /// </item>
        /// <item>
        /// <term>taxG</term>
        /// <description>Value of Tax group G (0.00...99.99 taxable,100.00=disabled)</description>
        /// </item>
        /// <item>
        /// <term>taxH</term>
        /// <description>Value of Tax group H (0.00...99.99 taxable,100.00=disabled)</description>
        /// </item>
        /// <item>
        /// <term>entDate</term>
        /// <description>Date of entry ( format DD-MM-YY )</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_TaxRatesByPeriod() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(50 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["nZreport"] = split[1];
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
                result["entDate"] = split[10];
            return result;
        }

        // Command number(Dec): 62 - please check fiscal device documentation.
        /// <summary>
        /// Read date and time
        /// </summary>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>dateTime</term>
        /// <description>Date and time in format: "DD-MM-YY hh:mm:ss DST".</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DateTime() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(62 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["dateTime"] = split[1];
            return result;
        }

        // Command number(Dec): 66 - please check fiscal device documentation.
        /// <summary>
        /// Get invoice interval
        /// </summary>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>valueStart</term>
        /// <description>The current starting value of the interval (1...9999999999).</description>
        /// </item>
        /// <item>
        /// <term>valueEnd</term>
        /// <description>The current ending value of the interval (1...9999999999).</description>
        /// </item>
        /// <item>
        /// <term>valueCurrent</term>
        /// <description>The current invoice receipt number (1...9999999999).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_InvoiceRange() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(66 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["valueStart"] = split[1];
            if(split.Length >= 3) 
                result["valueEnd"] = split[2];
            if(split.Length >= 4) 
                result["valueCurrent"] = split[3];
            return result;
        }

        // Command number(Dec): 62 - please check fiscal device documentation.
        /// <summary>
        /// Get Date and time
        /// </summary>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>day</term>
        /// <description>Day - format "DD".</description>
        /// </item>
        /// <item>
        /// <term>month</term>
        /// <description>Month - format "MM".</description>
        /// </item>
        /// <item>
        /// <term>year</term>
        /// <description>Year - format "YY".</description>
        /// </item>
        /// <item>
        /// <term>hour</term>
        /// <description>Hour - format "hh".</description>
        /// </item>
        /// <item>
        /// <term>minute</term>
        /// <description>Minutes - format "mm".</description>
        /// </item>
        /// <item>
        /// <term>second</term>
        /// <description>Seconds - format "ss".</description>
        /// </item>
        /// <item>
        /// <term>dST</term>
        /// <description>Text "DST" if exist time is Summer time.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DateTime_01() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(62 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["day"] = split[1];
            if(split.Length >= 3) 
                result["month"] = split[2];
            if(split.Length >= 4) 
                result["year"] = split[3];
            if(split.Length >= 5) 
                result["hour"] = split[4];
            if(split.Length >= 6) 
                result["minute"] = split[5];
            if(split.Length >= 7) 
                result["second"] = split[6];
            if(split.Length >= 8) 
                result["dST"] = split[7];
            return result;
        }

        // Command number(Dec): 64 - please check fiscal device documentation.
        /// <summary>
        /// Information on the last fiscal entry
        /// </summary>
        /// <param name="dataType">Type of returned data\n
        /// 0 - Turnover on TAX group; 1 - Amount on TAX group; 2 - Storno turnover on TAX group; 3 - Storno amount on TAX group</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>lastFRecordNumber</term>
        /// <description>Indicates current slip number (1...9999999)</description>
        /// </item>
        /// <item>
        /// <term>taxA</term>
        /// <description>Depend on Type. A is the letter of TAX group ( 0.00...9999999.99 or 0...999999999 depending
        /// dec point position</description>
        /// </item>
        /// <item>
        /// <term>taxB</term>
        /// <description>Depend on Type. B is the letter of TAX group ( 0.00...9999999.99 or 0...999999999 depending
        /// dec point position</description>
        /// </item>
        /// <item>
        /// <term>taxC</term>
        /// <description>Depend on Type. C is the letter of TAX group ( 0.00...9999999.99 or 0...999999999 depending
        /// dec point position</description>
        /// </item>
        /// <item>
        /// <term>taxD</term>
        /// <description>Depend on Type. D is the letter of TAX group ( 0.00...9999999.99 or 0...999999999 depending
        /// dec point position</description>
        /// </item>
        /// <item>
        /// <term>taxE</term>
        /// <description>Depend on Type. E is the letter of TAX group ( 0.00...9999999.99 or 0...999999999 depending
        /// dec point position</description>
        /// </item>
        /// <item>
        /// <term>taxF</term>
        /// <description>Depend on Type. F is the letter of TAX group ( 0.00...9999999.99 or 0...999999999 depending
        /// dec point position</description>
        /// </item>
        /// <item>
        /// <term>taxJ</term>
        /// <description>Depend on Type. J is the letter of TAX group ( 0.00...9999999.99 or 0...999999999 depending
        /// dec point position</description>
        /// </item>
        /// <item>
        /// <term>taxH</term>
        /// <description>Depend on Type. H is the letter of TAX group ( 0.00...9999999.99 or 0...999999999 depending
        /// dec point position</description>
        /// </item>
        /// <item>
        /// <term>date</term>
        /// <description>Date of fiscal record in format DD-MM-YY</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_LastFiscRecord(string dataType) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(dataType);
            inputString.Append("\t");

            string r = CustomCommand(64 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

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
        /// Information on daily taxation
        /// </summary>
        /// <param name="dataType">Type of returned data\n
        /// 0 - Turnover on TAX group; 1 - Amount on TAX group\n
        /// 2 - Storno turnover on TAX group; 3 - Storno amount on TAX group</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>nRep</term>
        /// <description>Number of report (1...3650)</description>
        /// </item>
        /// <item>
        /// <term>sumA</term>
        /// <description>Depend on Type. A is the letter of TAX group ( 0.00...9999999.99 or 0...999999999 depending
        /// dec point position</description>
        /// </item>
        /// <item>
        /// <term>sumB</term>
        /// <description>Depend on Type. B is the letter of TAX group ( 0.00...9999999.99 or 0...999999999 depending
        /// dec point position</description>
        /// </item>
        /// <item>
        /// <term>sumC</term>
        /// <description>Depend on Type. C is the letter of TAX group ( 0.00...9999999.99 or 0...999999999 depending
        /// dec point position</description>
        /// </item>
        /// <item>
        /// <term>sumD</term>
        /// <description>Depend on Type. D is the letter of TAX group ( 0.00...9999999.99 or 0...999999999 depending
        /// dec point position</description>
        /// </item>
        /// <item>
        /// <term>sumE</term>
        /// <description>Depend on Type. E is the letter of TAX group ( 0.00...9999999.99 or 0...999999999 depending
        /// dec point position</description>
        /// </item>
        /// <item>
        /// <term>sumF</term>
        /// <description>Depend on Type. F is the letter of TAX group ( 0.00...9999999.99 or 0...999999999 depending
        /// dec point position</description>
        /// </item>
        /// <item>
        /// <term>sumJ</term>
        /// <description>Depend on Type. J is the letter of TAX group ( 0.00...9999999.99 or 0...999999999 depending
        /// dec point position</description>
        /// </item>
        /// <item>
        /// <term>sumH</term>
        /// <description>Depend on Type. H is the letter of TAX group ( 0.00...9999999.99 or 0...999999999 depending
        /// dec point position</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_AdditionalDailyInfo(string dataType) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(dataType);
            inputString.Append("\t");

            string r = CustomCommand(65 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["nRep"] = split[1];
            if(split.Length >= 3) 
                result["sumA"] = split[2];
            if(split.Length >= 4) 
                result["sumB"] = split[3];
            if(split.Length >= 5) 
                result["sumC"] = split[4];
            if(split.Length >= 6) 
                result["sumD"] = split[5];
            if(split.Length >= 7) 
                result["sumE"] = split[6];
            if(split.Length >= 8) 
                result["sumF"] = split[7];
            if(split.Length >= 9) 
                result["sumG"] = split[8];
            if(split.Length >= 10) 
                result["sumH"] = split[9];
            return result;
        }

        // Command number(Dec): 68 - please check fiscal device documentation.
        /// <summary>
        /// Number of remaining entries for Z-reports in FM
        /// </summary>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>reportsLeft</term>
        /// <description>The number of remaining entries for Z-reports in FM (1...1825 or 3650)..</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_FreeFMRecords() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(68 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["reportsLeft"] = split[1];
            return result;
        }

        // Command number(Dec): 70 - please check fiscal device documentation.
        /// <summary>
        /// Cash in and Cash out operations
        /// </summary>
        /// <param name="amountType">Type of operation\n
        /// '0' - cash in; '1' - cash out; '2' - cash in (foreign currency); '3' - cash out (foreign currency)</param>
        /// <param name="amount">the sum ( 0.00...9999999.99 or 0...999999999 depending dec point position ); If Amount=0,
        /// the only Answer is returned, and receipt does not print</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>cashSum</term>
        /// <description>cash in safe sum ( 0.00...9999999.99 or 0...999999999 depending dec point position)</description>
        /// </item>
        /// <item>
        /// <term>servIn</term>
        /// <description>total sum of cash in operations ( 0.00...9999999.99 or 0...999999999 depending dec point position</description>
        /// </item>
        /// <item>
        /// <term>servOut</term>
        /// <description>total sum of cash out operations ( 0.00...9999999.99 or 0...999999999 depending dec point  position</description>
        /// </item>
        /// </list>
        /// </returns>

        public Dictionary<string, string> info_Get_CashIn_CashOut(string amountType, string amount) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(amountType);
            inputString.Append("\t");
            inputString.Append(amount);
            inputString.Append("\t");

            string r = CustomCommand(70 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

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
        /// General information, modem test
        /// </summary>
        /// <param name="infoType">Type of the information printed.\n
        /// '0' - General diagnostic information about the device; '1' - test of the modem with connection to the NRA server;
        ///'2' - general information about the connection with NRA server</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Print_Diagnostic_0(string infoType) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(infoType);
            inputString.Append("\t");

            string r = CustomCommand(71 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 71 - please check fiscal device documentation.
        /// <summary>
        /// General information, modem test
        /// </summary>
        /// <param name="infoType">Type of the information printed.\n
        /// '0' - General diagnostic information about the device; '1' - test of the modem with connection to the NRA server;
        ///'2' - general information about the connection with NRA server</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Print_Diagnostic_X(string infoType) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(infoType);
            inputString.Append("\t");

            string r = CustomCommand(71 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 71 - please check fiscal device documentation.
        /// <summary>
        /// General information, modem test
        /// </summary>
        /// <param name="infoType">'3' - print information about the connection with NRA server; '4' - test of the LAN interface if present;
        ///'6' - test of the SD card performance;'9' - setup of the Ble module( if present );
        ///'10' - test of the modem without PPP connection;</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>lastDate</term>
        /// <description>Last connection to the server</description>
        /// </item>
        ///  <item>
        /// <term>nextDate</term>
        /// <description>Next connection to the server</description>
        /// </item>
        /// <item>
        /// <term>zrep</term>
        /// <description>Last send Z report</description>
        /// </item>
        /// <item>
        /// <term>zErrZnum</term>
        /// <description>Number of Z report with error</description>
        /// </item>
        /// <item>
        /// <term>zErrCnt</term>
        /// <description>Sum of all errors for Z reports</description>
        /// </item>
        /// <item>
        /// <term>zErrNum</term>
        /// <description>Error number from the server</description>
        /// </item>
        /// <item>
        /// <term>sellErrnDoc</term>
        /// <description>Number of sell document with error</description>
        /// </item>
        /// <item>
        /// <term>sellErrCnt</term>
        /// <description>Sum of all errors for sell documents</description>
        /// </item>
        /// <item>
        /// <term>sellErrStatus</term>
        /// <description>Error number from the server</description>
        /// </item>
        /// <item>
        /// <term>sellNumber</term>
        /// <description>Last received document number from the server</description>
        /// </item>
        /// <item>
        /// <term>sellDate</term>
        /// <description>The date and time of last received document from the server</description>
        /// </item>
        /// <item>
        /// <term>lastErr</term>
        /// <description>Last error from the server</description>
        /// </item>
        /// <item>
        /// <term>remMinutes</term>
        /// <description>Remaining minutes until next GetDeviceInfo request</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_TaxTerminalInfo(string infoType) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(infoType);
            inputString.Append("\t");

            string r = CustomCommand(71 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["lastDate"] = split[1];
            if(split.Length >= 3) 
                result["nextDate"] = split[2];
            if(split.Length >= 4) 
                result["zrep"] = split[3];
            if(split.Length >= 5) 
                result["zErrZnum"] = split[4];
            if(split.Length >= 6) 
                result["zErrCnt"] = split[5];
            if(split.Length >= 7) 
                result["zErrNum"] = split[6];
            if(split.Length >= 8) 
                result["sellErrnDoc"] = split[7];
            if(split.Length >= 9) 
                result["sellErrCnt"] = split[8];
            if(split.Length >= 10) 
                result["sellErrStatus"] = split[9];
            if(split.Length >= 11) 
                result["sellNumber"] = split[10];
            if(split.Length >= 12) 
                result["sellDate"] = split[11];
            if(split.Length >= 13) 
                result["lastErr"] = split[12];
            if(split.Length >= 14) 
                result["remMinutes"] = split[13];
            return result;
        }

        // Command number(Dec): 74 - please check fiscal device documentation.
        /// <summary>
        /// Reading the Status
        /// </summary>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>statusBytes</term>
        /// <description>Status Bytes ( See the description of the status bytes ).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_StatusBytes() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(74 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["statusBytes"] = split[1];
            return result;
        }

        // Command number(Dec): 76 - please check fiscal device documentation.
        /// <summary>
        /// Status of the fiscal transaction
        /// </summary>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>isOpen</term>
        /// <description>0 - Receipt is closed;1 - Normal receipt is open;
        /// 2 - Storno receipt is open.Reason "mistake by operator";3 - Storno receipt is open.Reason "refund";
        /// 4 - Storno receipt is open.Reason "tax base reduction";5 - standard non-fiscal receipt is open;.</description>
        /// </item>
        /// <item>
        /// <term>number</term>
        /// <description>The number of the current or the last receipt (1...9999999)</description>
        /// </item>
        /// <item>
        /// <term>items</term>
        /// <description>number of sales registered on the current or the last fiscal receipt (0...9999999)</description>
        /// </item>
        /// <item>
        /// <term>amount</term>
        /// <description>The sum from the current or the last fiscal receipt (0.00...9999999.99 or 0...999999999 depending dec point position)</description>
        /// </item>
        /// <item>
        /// <term>payed</term>
        /// <description>The sum payed for the current or the last receipt ( 0.00...9999999.99 or 0...999999999) depending dec point position</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_FTransactionStatus() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(76 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["isOpen"] = split[1];
            if(split.Length >= 3) 
                result["number"] = split[2];
            if(split.Length >= 4) 
                result["items"] = split[3];
            if(split.Length >= 5) 
                result["amount"] = split[4];
            if(split.Length >= 6) 
                result["payed"] = split[5];
            return result;
        }

        // Command number(Dec): 86 - please check fiscal device documentation.
        /// <summary>
        /// Date of the last fiscal record
        /// </summary>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_FMRecord_LastDateTime() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(86 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["dateTime"] = split[1];
            return result;
        }

        // Command number(Dec): 87 - please check fiscal device documentation.
        /// <summary>
        /// Get item groups information
        /// </summary>
        /// <param name="itemGroup">Number of item group; If ItemGroup is empty - item group report</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>totSales</term>
        /// <description>Number of sales for this item group for day</description>
        /// </item>
        /// <item>
        /// <term>totSum</term>
        /// <description>Accumulated sum for this item group for day</description>
        /// </item>
        /// <item>
        /// <term>groupName</term>
        /// <description>Name of item group</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_ItemGroup(string itemGroup) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(itemGroup);
            inputString.Append("\t");

            string r = CustomCommand(87 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["totSales"] = split[1];
            if(split.Length >= 3) 
                result["totSum"] = split[2];
            if(split.Length >= 4) 
                result["groupName"] = split[3];
            return result;
        }

        // Command number(Dec): 88 - please check fiscal device documentation.
        /// <summary>
        /// Get department information
        /// </summary>
        /// <param name="departmentNumber">Number of department (1...99); If Department is empty - department report</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>taxGr</term>
        /// <description>Tax group of department</description>
        /// </item>
        /// <item>
        /// <term>price</term>
        /// <description>Price of department.</description>
        /// </item>
        /// <item>
        /// <term>totSales</term>
        /// <description>Number of sales for this department for day.</description>
        /// </item>
        /// <item>
        /// <term>totSum</term>
        /// <description>Accumulated sum for this department for day.</description>
        /// </item>
        /// <item>
        /// <term>stTotSales</term>
        /// <description>Number of storno operations for this department for day.</description>
        /// </item>
        /// <item>
        /// <term>stTotSum</term>
        /// <description>Accumulated sum from storno operations for this department for day.</description>
        /// </item>
        /// <item>
        /// <term>departmentName</term>
        /// <description>Name of the department.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DepartmentInfo(string departmentNumber) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(departmentNumber);
            inputString.Append("\t");

            string r = CustomCommand(88 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["taxGr"] = split[1];
            if(split.Length >= 3) 
                result["price"] = split[2];
            if(split.Length >= 4) 
                result["totSales"] = split[3];
            if(split.Length >= 5) 
                result["totSum"] = split[4];
            if(split.Length >= 6) 
                result["stTotSales"] = split[5];
            if(split.Length >= 7) 
                result["stTotSum"] = split[6];
            if(split.Length >= 8) 
                result["departmentName"] = split[7];
            return result;
        }

        // Command number(Dec): 90 - please check fiscal device documentation.
        /// <summary>
        /// Diagnostic information
        /// </summary>
        /// <param name="calcCRC">none - Diagnostic information without firmware checksum OR '1' - Diagnostic information with firmware checksum</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>deviceName</term>
        /// <description>Device name (up to 32 symbols)</description>
        /// </item>
        /// <item>
        /// <term>firmwareRevision</term>
        /// <description>Firmware version. 6 symbols.</description>
        /// </item>
        /// <item>
        /// <term>firmwareDate</term>
        /// <description>Firmware date DDMMMYY. 7 symbols.</description>
        /// </item>
        /// <item>
        /// <term>firmwareTime</term>
        /// <description>Firmware time hhmm. 4 symbols.</description>
        /// </item>
        /// <item>
        /// <term>checkSum</term>
        /// <description>Firmware checksum. 4 symbols.</description>
        /// </item>
        /// <item>
        /// <term>switches</term>
        /// <description>Switch from Sw1 to Sw8. 8 symbols (not used at this device, always 00000000).</description>
        /// </item>
        /// <item>
        /// <term>serialNumber</term>
        /// <description>Serial Number ( Two letters and six digits: XX123456).</description>
        /// </item>
        /// <item>
        /// <term>fiscalMemoryNumber</term>
        /// <description>Fiscal memory number (8 digits).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DiagnosticInfo(string calcCRC) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(calcCRC);
            inputString.Append("\t");

            string r = CustomCommand(90 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["deviceName"] = split[1];
            if(split.Length >= 3) 
                result["firmwareRevision"] = split[2];
            if(split.Length >= 4) 
                result["firmwareDate"] = split[3];
            if(split.Length >= 5) 
                result["firmwareTime"] = split[4];
            if(split.Length >= 6) 
                result["checkSum"] = split[5];
            if(split.Length >= 7) 
                result["switches"] = split[6];
            if(split.Length >= 8) 
                result["serialNumber"] = split[7];
            if(split.Length >= 9) 
                result["fiscalMemoryNumber"] = split[8];
            return result;
        }

        // Command number(Dec): 99 - please check fiscal device documentation.
        /// <summary>
        /// Reading the programmed TAX number
        /// </summary>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>eikValue</term>
        /// <description>TAX number (max 13 characters)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_EIKValue() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(99 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["eikValue"] = split[1];
            return result;
        }

        // Command number(Dec): 100 - please check fiscal device documentation.
        /// <summary>
        /// Reading an error
        /// </summary>
        /// <param name="targetCode">Code of the error(negative number)</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>codeValue</term>
        /// <description>Code of the error, to be explained</description>
        /// </item>
        /// <item>
        /// <term>codeDescription</term>
        /// <description>Explanation of the error in Code</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_ErrorDescription(string targetCode) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(targetCode);
            inputString.Append("\t");

            string r = CustomCommand(100 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["codeValue"] = split[1];
            if(split.Length >= 3) 
                result["codeDescription"] = split[2];
            return result;
        }

        // Command number(Dec): 103 - please check fiscal device documentation.
        /// <summary>
        /// Information for the current receipt
        /// </summary>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>canVd</term>
        /// <description>Code of the error, to be explained</description>
        /// </item>
        /// <item>
        /// <term>taxA</term>
        /// <description>The current accumulated sum on VAT A ( 0.00...9999999.99 or 0...999999999 depending dec point position )</description>
        /// </item>
        /// <item>
        /// <term>taxB</term>
        /// <description>The current accumulated sum on VAT B ( 0.00...9999999.99 or 0...999999999 depending dec point position )</description>
        /// </item>
        /// <item>
        /// <term>taxC</term>
        /// <description>The current accumulated sum on VAT C ( 0.00...9999999.99 or 0...999999999 depending dec point position )</description>
        /// </item>
        /// <item>
        /// <term>taxD</term>
        /// <description>The current accumulated sum on VAT D ( 0.00...9999999.99 or 0...999999999 depending dec point position )</description>
        /// </item>
        /// <item>
        /// <term>taxE</term>
        /// <description>The current accumulated sum on VAT E ( 0.00...9999999.99 or 0...999999999 depending dec point position )</description>
        /// </item>
        /// <item>
        /// <term>taxF</term>
        /// <description>The current accumulated sum on VAT F ( 0.00...9999999.99 or 0...999999999 depending dec point position )</description>
        /// </item>
        /// <item>
        /// <term>taxG</term>
        /// <description>The current accumulated sum on VAT G ( 0.00...9999999.99 or 0...999999999 depending dec point position )</description>
        /// </item>
        /// <item>
        /// <term>taxH</term>
        /// <description>The current accumulated sum on VAT H ( 0.00...9999999.99 or 0...999999999 depending dec point position )</description>
        /// </item>
        /// <item>
        /// <term>inv</term>
        /// <description>'1' if it is expanded receipt; '0' if it is simplified receipt</description>
        /// </item>
        /// <item>
        /// <term>invNum</term>
        /// <description>Number of the next invoice (up to 10 digits)</description>
        /// </item>
        /// <item>
        /// <term>recieptType</term>
        /// <description>'1' if a storno receipt is open; '0' if it is normal receipt</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_CurrentRecieptInfo() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(103 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

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
                result["recieptType"] = split[12];
            return result;
        }

        // Command number(Dec): 110 - please check fiscal device documentation.
        /// <summary>
        /// Additional daily information - Payments (sell operations);
        /// </summary>
        /// <param name="option">Type of information. '0' - Payments (sell operations)</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>pay1</term>
        /// <description>Value payed by payment 1 (0.00...9999999.99 or 0...999999999 depending dec point position)</description>
        /// </item>
        /// <item>
        /// <term>pay2</term>
        /// <description>Value payed by payment 2 (0.00...9999999.99 or 0...999999999 depending dec point position)</description>
        /// </item>
        /// <item>
        /// <term>pay3</term>
        /// <description>Value payed by payment 3 (0.00...9999999.99 or 0...999999999 depending dec point position)</description>
        /// </item>
        /// <item>
        /// <term>pay4</term>
        /// <description>Value payed by payment 4 (0.00...9999999.99 or 0...999999999 depending dec point position)</description>
        /// </item>
        /// <item>
        /// <term>pay5</term>
        /// <description>Value payed by payment 5 (0.00...9999999.99 or 0...999999999 depending dec point position)</description>
        /// </item>
        /// <item>
        /// <term>pay6</term>
        /// <description>Value payed by payment 6 (0.00...9999999.99 or 0...999999999 depending dec point position)</description>
        /// </item>
        /// <item>
        /// <term>foreignPay</term>
        /// <description>Value payed by foreign currency (0.00...9999999.99 or 0...999999999 depending dec point position)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_AdditionalDailyInfo_C00(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");

            string r = CustomCommand(110 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["pay1"] = split[1];
            if(split.Length >= 3) 
                result["pay2"] = split[2];
            if(split.Length >= 4) 
                result["pay3"] = split[3];
            if(split.Length >= 5) 
                result["pay4"] = split[4];
            if(split.Length >= 6) 
                result["pay5"] = split[5];
            if(split.Length >= 7) 
                result["pay6"] = split[6];
            if(split.Length >= 8) 
                result["foreignPay"] = split[7];
            return result;
        }

        // Command number(Dec): 110 - please check fiscal device documentation.
        /// <summary>
        /// Additional daily information - payments (storno operations);
        /// </summary>
        /// <param name="option">'1' - Payments (storno operations)</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>pay1</term>
        /// <description>Value payed by payment 1 (0.00...9999999.99 or 0...999999999 depending dec point position)</description>
        /// </item>
        /// <item>
        /// <term>pay2</term>
        /// <description>Value payed by payment 2 (0.00...9999999.99 or 0...999999999 depending dec point position)</description>
        /// </item>
        /// /// <item>
        /// <term>pay3</term>
        /// <description>Value payed by payment 3 (0.00...9999999.99 or 0...999999999 depending dec point position)</description>
        /// </item>
        /// /// <item>
        /// <term>pay4</term>
        /// <description>Value payed by payment 4 (0.00...9999999.99 or 0...999999999 depending dec point position)</description>
        /// </item>
        /// /// <item>
        /// <term>pay5</term>
        /// <description>Value payed by payment 5 (0.00...9999999.99 or 0...999999999 depending dec point position)</description>
        /// </item>
        /// /// <item>
        /// <term>pay6</term>
        /// <description>Value payed by payment 6 (0.00...9999999.99 or 0...999999999 depending dec point position)</description>
        /// </item>
        /// <item>
        /// <term>foreignPay</term>
        /// <description>Value payed by foreign currency (0.00...9999999.99 or 0...999999999 depending dec point position)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_AdditionalDailyInfo_C01(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");

            string r = CustomCommand(110 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["pay1"] = split[1];
            if(split.Length >= 3) 
                result["pay2"] = split[2];
            if(split.Length >= 4) 
                result["pay3"] = split[3];
            if(split.Length >= 5) 
                result["pay4"] = split[4];
            if(split.Length >= 6) 
                result["pay5"] = split[5];
            if(split.Length >= 7) 
                result["pay6"] = split[6];
            if(split.Length >= 8) 
                result["foreignPay"] = split[7];
            return result;
        }

        // Command number(Dec): 110 - please check fiscal device documentation.
        /// <summary>
        ///Additional daily information - nNumber and sum of sells
        /// </summary>
        /// <param name="option">'2' - number and sum of sells</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>clients</term>
        /// <description>Number of clients (integer number - 0,1,2, ....)</description>
        /// </item>
        /// <item>
        /// <term>sums</term>
        /// <description>Sum of the sells (0.00...9999999.99)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_AdditionalDailyInfo_C02(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");

            string r = CustomCommand(110 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["clients"] = split[1];
            if(split.Length >= 3) 
                result["sums"] = split[2];
            return result;
        }

        // Command number(Dec): 110 - please check fiscal device documentation.
        /// <summary>
        /// Number and sum of discounts and surcharges
        /// </summary>
        /// <param name="option">'3' - number and sum of discounts and surcharges</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>surcharges</term>
        /// <description>Number of surcharges</description>
        /// </item>
        /// <item>
        /// <term>surchargesSum</term>
        /// <description>Sum of surcharges</description>
        /// </item>
        /// <item>
        /// <term>discounts</term>
        /// <description>Number of discounts</description>
        /// </item>
        /// <item>
        /// <term>discountsSum</term>
        /// <description>Sum of discounts</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_AdditionalDailyInfo_C03(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");

            string r = CustomCommand(110 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["surcharges"] = split[1];
            if(split.Length >= 3) 
                result["surchargesSum"] = split[2];
            if(split.Length >= 4) 
                result["discounts"] = split[3];
            if(split.Length >= 5) 
                result["discountsSum"] = split[4];
            return result;
        }

        // Command number(Dec): 110 - please check fiscal device documentation.
        /// <summary>
        /// Additional information - number and sum of corrections and annulled receipts
        /// </summary>
        /// <param name="option">'4' - number and sum of corrections and annulled receipts</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>corrections</term>
        /// <description>Number of corrections ( integer number - 0,1,2, .... )</description>
        /// </item>
        /// <item>
        /// <term>correctionsSum</term>
        /// <description>Sum of corrections ( 0.00...9999999.99)</description>
        /// </item>
        /// <item>
        /// <term>annulledReceipts</term>
        /// <description>Number of annulled ( integer number - 0,1,2, .... );</description>
        /// </item>
        /// <item>
        /// <term>annulledreceiptsSum</term>
        /// <description>Sum of annulled ( 0.00...9999999.99 )</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_AdditionalDailyInfo_C04(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");

            string r = CustomCommand(110 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["corrections"] = split[1];
            if(split.Length >= 3) 
                result["correctionsSum"] = split[2];
            if(split.Length >= 4) 
                result["annulledReceipts"] = split[3];
            if(split.Length >= 5) 
                result["annulledreceiptsSum"] = split[4];
            return result;
        }

        // Command number(Dec): 110 - please check fiscal device documentation.
        /// <summary>
        /// Additional information - Number and sum of cash in and cash out operations
        /// </summary>
        /// <param name="option">'5' - number and sum of cash in and cash out operations</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>cashIn</term>
        /// <description>Number of cash in operations ( integer number - 0,1,2, .... )</description>
        /// </item>
        /// <item>
        /// <term>cashinSum</term>
        /// <description>Sum of cash in operations ( 0.00...9999999.99)</description>
        /// </item>
        /// <item>
        /// <term>cashOut</term>
        /// <description>Number of cash out operations ( integer number - 0,1,2, .... );</description>
        /// </item>
        /// <item>
        /// <term>cashoutSum</term>
        /// <description>Sum of cash out operations ( 0.00...9999999.99 )</description>
        /// </item>
        /// <item>
        /// <term>currencyCashIn</term>
        /// <description>Number of cash in operations in alternative currency ( integer number - 0,1,2, .... );</description>
        /// </item>
        /// <item>
        /// <term>currencycashinSum</term>
        /// <description>Sum of cash in operations in alternative currency ( 0.00...9999999.99 );</description>
        /// </item>
        /// <item>
        /// <term>currencyCashOut</term>
        /// <description>Number of cash out operations in alternative currency (integer number - 0,1,2, ....);</description>
        /// </item>
        /// <item>
        /// <term>currencycashoutSum</term>
        /// <description>Sum of cash out operations in alternative currency (0.00...9999999.99)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_AdditionalDailyInfo_C05(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");

            string r = CustomCommand(110 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["cashIn"] = split[1];
            if(split.Length >= 3) 
                result["cashinSum"] = split[2];
            if(split.Length >= 4) 
                result["cashOut"] = split[3];
            if(split.Length >= 5) 
                result["cashoutSum"] = split[4];
            if(split.Length >= 6) 
                result["currencyCashIn"] = split[5];
            if(split.Length >= 7) 
                result["currencycashinSum"] = split[6];
            if(split.Length >= 8) 
                result["currencyCashOut"] = split[7];
            if(split.Length >= 9) 
                result["currencycashoutSum"] = split[8];
            return result;
        }

        // Command number(Dec): 112 - please check fiscal device documentation.
        /// <summary>
        /// Information for operator
        /// </summary>
        /// <param name="wpOperator">Number of operator (1...30)</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>freceiptsCount</term>
        /// <description>Number of fiscal receipts, issued by the operator (0...65535)</description>
        /// </item>
        /// <item>
        /// <term>salesSum</term>
        /// <description>Total accumulated sum (0.00...9999999.99 or 0...999999999 depending dec point position)</description>
        /// </item>
        /// <item>
        /// <term>discountsCount</term>
        /// <description>Number of discounts (0...65535)</description>
        /// </item>
        /// <item>
        /// <term>discountsSum</term>
        /// <description>Total accumulated sum of discounts with sign (0.00...9999999.99 or 0...999999999 depending dec point position)</description>
        /// </item>
        /// <item>
        /// <term>surchargesCount</term>
        /// <description>Number of surcharges (0...65535);</description>
        /// </item>
        /// <item>
        /// <term>surchargesSum</term>
        /// <description>Total accumulated sum of surcharges with sign( 0.00...9999999.99 or 0...999999999 depending dec point position)</description>
        /// </item>
        /// <item>
        /// <term>voidsCount</term>
        /// <description>Number of corrections (0...65535)</description>
        /// </item>
        /// <item>
        /// <term>voidsSum</term>
        /// <description>Total accumulated sum of corrections with sign( 0.00...9999999.99 or 0...999999999 depending dec point position)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_OperatorsData(string wpOperator) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(wpOperator);
            inputString.Append("\t");

            string r = CustomCommand(112 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["freceiptsCount"] = split[1];
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
            return result;
        }

        // Command number(Dec): 123 - please check fiscal device documentation.
        /// <summary>
        /// Device information
        /// </summary>
        /// <param name="option">Type of information to return: '1' - Serial numbers, Header and Tax numbers</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>serialNumber</term>
        /// <description>Indicates current slip number (1...9999999)</description>
        /// </item>
        /// <item>
        /// <term>fiscalMemoryNumber</term>
        /// <description>Subtotal of the receipt (0.00...9999999.99 or 0...999999999 depending dec point position)</description>
        /// </item>
        ///  <item>
        /// <term>headerline1</term>
        /// <description>Value of Tax group A (0.00...99.99 taxable,100.00=disabled)</description>
        /// </item>
        /// <item>
        /// <term>headerline2</term>
        /// <description>Value of Tax group B (0.00...99.99 taxable,100.00=disabled)</description>
        /// </item>
        /// <item>
        /// <term>tAXnumber</term>
        /// <description>Value of Tax group C (0.00...99.99 taxable,100.00=disabled)</description>
        /// </item>
        /// <item>
        /// <term>headerline3</term>
        /// <description>Value of Tax group D (0.00...99.99 taxable,100.00=disabled)</description>
        /// </item>
        /// <item>
        /// <term>headerline4</term>
        /// <description>Value of Tax group E (0.00...99.99 taxable,100.00=disabled)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DeviceInfo_01(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");

            string r = CustomCommand(123 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["serialNumber"] = split[1];
            if(split.Length >= 3) 
                result["fiscalMemoryNumber"] = split[2];
            if(split.Length >= 4) 
                result["headerline1"] = split[3];
            if(split.Length >= 5) 
                result["headerline2"] = split[4];
            if(split.Length >= 6) 
                result["tAXnumber"] = split[5];
            if(split.Length >= 7) 
                result["headerline3"] = split[6];
            if(split.Length >= 8) 
                result["headerline4"] = split[7];
            return result;
        }

        // Command number(Dec): 123 - please check fiscal device documentation.
        /// <summary>
        /// Device information - battery and GSM signal status
        /// </summary>
        /// <param name="option">'2' - Battery and GSM signal status</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>mainBattery</term>
        /// <description>Main Battery level in mV</description>
        /// </item>
        /// <item>
        /// <term>ramBattery</term>
        /// <description>Ram Battery level in mV</description>
        /// </item>
        ///  <item>
        /// <term>signal</term>
        /// <description>GSM Signal level in percentage</description>
        /// </item>
        /// <item>
        /// <term>networkStatus</term>
        /// <description>GSM network status. 1-registered, 0-unregistered</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DeviceInfo_02(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");

            string r = CustomCommand(123 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["mainBattery"] = split[1];
            if(split.Length >= 3) 
                result["ramBattery"] = split[2];
            if(split.Length >= 4) 
                result["signal"] = split[3];
            if(split.Length >= 5) 
                result["networkStatus"] = split[4];
            return result;
        }

        // Command number(Dec): 123 - please check fiscal device documentation.
        /// <summary>
        /// Device information - last fiscal receipt
        /// </summary>
        /// <param name="option">'3' - Last fiscal receipt</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>lastfiscalreceiptNumber</term>
        /// <description>Number of last sales receipt in current Z report (1...9999)</description>
        /// </item>
        /// <item>
        /// <term>datetimeLastFiscalReceipt</term>
        /// <description>Date and time of last sales receipt (format "DD-MM-YYYY hh:mm:ss")</description>
        /// </item>
        /// <item>
        /// <term>lastzreportNumber</term>
        /// <description>Number of last Z-report (1..????)</description>
        /// </item>
        /// <item>
        /// <term>datetimeLastZReport</term>
        /// <description>Date of last of Z-report (format "DD-MM-YYYY hh:mm:ss")</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DeviceInfo_03(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");

            string r = CustomCommand(123 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["lastfiscalreceiptNumber"] = split[1];
            if(split.Length >= 3) 
                result["datetimeLastFiscalReceipt"] = split[2];
            if(split.Length >= 4) 
                result["lastzreportNumber"] = split[3];
            if(split.Length >= 5) 
                result["datetimeLastZReport"] = split[4];
            return result;
        }

        // Command number(Dec): 123 - please check fiscal device documentation.
        /// <summary>
        /// Full EJ verify
        /// </summary>
        /// <param name="option">'4' - Full EJ verify</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DeviceInfo_04(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");

            string r = CustomCommand(123 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 123 - please check fiscal device documentation.
        /// <summary>
        /// Device information - Battery level
        /// </summary>
        /// <param name="option">'5' - Battery level</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>mainBattery</term>
        /// <description>Main Battery level in mV.</description>
        /// </item>
        /// <item>
        /// <term>chargeLevel</term>
        /// <description>Battery charge percentage.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DeviceInfo_05(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");

            string r = CustomCommand(123 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["mainBattery"] = split[1];
            if(split.Length >= 3) 
                result["chargeLevel"] = split[2];
            return result;
        }

        // Command number(Dec): 135 - please check fiscal device documentation.
        /// <summary>
        ///  Modem information
        /// </summary>
        /// <param name="option">Type of information to return : 's' - Read the IMEI of the modem</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>iMEI</term>
        /// <description>IMEI number of the modem.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_Modem_IMEI(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");

            string r = CustomCommand(135 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["iMEI"] = split[1];
            return result;
        }

        // Command number(Dec): 135 - please check fiscal device documentation.
        /// <summary>
        /// Modem information 
        /// </summary>
        /// <param name="option">Type of information to return: 'i' - Read the IMSI of the SIM card</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>iMSI</term>
        /// <description>IMSI number of the SIM card.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_IMSI(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");

            string r = CustomCommand(135 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["iMSI"] = split[1];
            return result;
        }

        // Command number(Dec): 135 - please check fiscal device documentation.
        /// <summary>
        /// Modem information
        /// </summary>
        /// <param name="option">'M' - Modem status. Returns the last state of the modem</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>signalLevel</term>
        /// <description>GSM Signal level in percentage 0...100.</description>
        /// </item>
        /// <item>
        /// <term>iMEI</term>
        /// <description>IMEI number of the modem.</description>
        /// </item>
        /// <item>
        /// <term>iMSI</term>
        /// <description>IMSI number of the SIM card.</description>
        /// </item>
        /// <item>
        /// <term>mobileOperator</term>
        /// <description>Mobile operator name.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_ModemStatus(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");

            string r = CustomCommand(135 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["signalLevel"] = split[1];
            if(split.Length >= 3) 
                result["iMEI"] = split[2];
            if(split.Length >= 4) 
                result["iMSI"] = split[3];
            if(split.Length >= 5) 
                result["mobileOperator"] = split[4];
            return result;
        }

        // Command number(Dec): 140 - please check fiscal device documentation.
        /// <summary>
        /// Defining and reading clients - Clients information
        /// </summary>
        /// <param name="option">'I' - Clients information</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>countTotal</term>
        /// <description>Total count of the programmable clients (1000).</description>
        /// </item>
        /// <item>
        /// <term>countProgrammed</term>
        /// <description>Total count of the programmed clients (0...1000).</description>
        /// </item>
        /// <item>
        /// <term>nameLength</term>
        /// <description>Maximum length of client name (36).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_ClientsInfo(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");

            string r = CustomCommand(140 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["countTotal"] = split[1];
            if(split.Length >= 3) 
                result["countProgrammed"] = split[2];
            if(split.Length >= 4) 
                result["nameLength"] = split[3];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Baud rate of COM port for communication with PC
        /// </summary>
        /// <param name="variableName">FpComBaudRate</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value for COM baud rate.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_FpComBaudRate(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Permission/rejection of the automatic cutting of paper after each receipt
        /// </summary>
        /// <param name="variableName">AutoPaperCutting</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value - ( 1 - permitted, 0 - rejected ).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_AutoPaperCutting(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Partial=0/Full=1 cutting of paper (FP-700X only)
        /// </summary>
        /// <param name="variableName">PaperCuttingType</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (Partial=0/Full=1).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_PaperCuttingType(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Barcode height
        /// </summary>
        /// <param name="variableName">BarCodeHeight</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value - '1' (7mm) to '10' (70mm).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_BarCodeHeight(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Enable/Disable printing of the barcode data
        /// </summary>
        /// <param name="variableName">BarcodeName</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value for barcode name.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_BarcodeName(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Baud rate of COM port that has peripheral device assigned
        /// </summary>
        /// <param name="variableName">ComPortBaudRate</param>
        /// <param name="index">Number of com port</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value </description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_ComPortBaudRate(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Protocol for communication with peripheral device assigned COM port</summary>
        /// <param name="variableName">ComPortProtocol</param>
        /// <param name="index">Number of COM port</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value for COM port protocol.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_ComPortProtocol(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// PC interface type
        /// </summary>
        /// <param name="variableName">MainInterfaceType</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value for pc interface type.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_MainInterfaceType(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Time out between fiscal printer commands before start auto  print(in milliseconds)
        /// </summary>
        /// <param name="variableName">TimeOutBeforePrintFlush-</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value for pc interface type.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_TimeOutBeforePrintFlush(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// FPr works with battery on main supply
        /// </summary>
        /// <param name="variableName">WorkBatteryIncluded</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1- enable, 0 - disable).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_WorkBatteryIncluded(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Decrease the space between text lines
        /// </summary>
        /// <param name="variableName">Dec2xLineSpacing</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (0...5 Greater values = less line spacing).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_Dec2xLineSpacing(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Printer font type
        /// </summary>
        /// <param name="variableName">PrintFontType</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_PrintFontType(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Number of blank lines for proper paper cutting
        /// </summary>
        /// <param name="variableName">FooterEmptyLines</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_FooterEmptyLines(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Minimum number of lines from the header after printing the footer
        /// </summary>
        /// <param name="variableName">HeaderMinLines</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_HeaderMinLines(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Print the logo after rows to push the paper
        /// </summary>
        /// <param name="variableName">LogoPrintAfterFooter</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1: yes, 0: no).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_LogoPrintAfterFooter(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Handling of near paper end
        /// </summary>
        /// <param name="variableName">EnableNearPaperEnd</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_EnableNearPaperEnd(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Synchronize date/time from the NRA server
        /// </summary>
        /// <param name="variableName">DateFromNAPServDisable</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (0 - sync, 1 - does not sync).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DateFromNAPServDisable(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Minutes to automatically turn off ECR if it is idle
        /// </summary>
        /// <param name="variableName">AutoPowerOff</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (0 - disable; from 1 minute to 240 minutes).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_AutoPowerOff(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Minutes to automatically turn off Backlight of the display if FPr is idle
        /// </summary>
        /// <param name="variableName">BkLight_AutoOff</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (0 - disable; from 1 minute to 5 minutes).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_BkLight_AutoOff(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Number of COM port for communication with pinpad
        /// </summary>
        /// <param name="variableName">PinpadComPort</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1-COM1, 2- COM2, 4-Bluetooth).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_PinpadComPort(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Baud rate of COM port that has pinpad device assigned
        /// </summary>
        /// <param name="variableName">PinpadComBaudRate</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value(0...9).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_PinpadComBaudRate(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Type of pinpad
        /// </summary>
        /// <param name="variableName">PinpadType</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value(1 - BORICA; 2 - UBB; 3 - DSK).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_PinpadType(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Type of connection between cash register and bank server
        /// </summary>
        /// <param name="variableName">PinpadConnectionType</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (0-GPRS, 1-LAN).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_PinpadConnectionType(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Copies of the receipt from pinpad
        /// </summary>
        /// <param name="variableName">PinpadReceiptCopies</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (0...3).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_PinpadReceiptCopies(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Where to print pinpad receipt
        /// </summary>
        /// <param name="variableName">PinpadReceiptInfo</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - in fiscal receipt; 0 - separate from fiscal receipt).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_PinpadReceiptInfo(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Function of PY2 key in registration (Works only with configuration with BORICA)
        /// </summary>
        /// <param name="variableName">PinpadPaymentMenu</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - menu for payment with pinpad(card and loyalty scheme);\n 0 - payment with card with pinpad).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_PinpadPaymentMenu(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Function of PY4 key (Works only with configuration with BORICA)
        /// </summary>
        /// <param name="variableName">PinpadLoyaltyPayment</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - payment with pinpad with loyalty scheme; 0 - payment PY4).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_PinpadLoyaltyPayment(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        public Dictionary<string, string> info_Get_PinpadShortRec(string variableName, string index, string value)
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255, inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: " + split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (split.Length >= 1)
                result["errorCode"] = split[0];
            if (split.Length >= 2)
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Turn on / off bluetooth module
        /// </summary>
        /// <param name="variableName">BthEnable</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.(0 - off, 1 - on)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_BthEnable(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Turn on / off bluetooth device discoverability
        /// </summary>
        /// <param name="variableName">BthDiscoverability</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.(1 - discoverable; 0 - non-discoverable)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_BthDiscoverability(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Bluetooth pairing
        /// </summary>
        /// <param name="variableName">BthPairing</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (0-unsecure, 1-reset and save, 2-reset).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_BthPairing(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Pin code for bluetooth pairing
        /// </summary>
        /// <param name="variableName">BthPinCode</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value for pin code.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_BthPinCode(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Firmware version of bluetooth module
        /// </summary>
        /// <param name="variableName">BthVersion</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value for firmware version of bluetooth module.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_BthVersion(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Bluetooth device address
        /// </summary>
        /// <param name="variableName">BthAddress</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value of bluetooth device address.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_BthAddress(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Logical number in the workplace
        /// </summary>
        /// <param name="variableName">EcrLogNumber</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (from 1 to 9999).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_EcrLogNumber(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Type of the receipt
        /// </summary>
        /// <param name="variableName">EcrExtendedReceipt</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - extended, 0 - simplified).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_EcrExtendedReceipt(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Work with constituents
        /// </summary>
        /// <param name="variableName">EcrDoveriteli</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1-enable( in one receipt only one constituent ), 0 - disable).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_EcrDoveriteli(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Work without passwords
        /// </summary>
        /// <param name="variableName">EcrWithoutPasswords</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - enable; 0 - disable).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_EcrWithoutPasswords(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Require password after each receipt
        /// </summary>
        /// <param name="variableName">EcrAskForPassword</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - enable; 0 - disable).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_EcrAskForPassword(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Require password for void operations
        /// </summary>
        /// <param name="variableName">EcrAskForVoidPassword</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - enable; 0 - disable).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_EcrAskForVoidPassword(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// When making Z-report, automatically make "Operator report"
        /// </summary>
        /// <param name="variableName">EcrConnectedOperReport</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - enable; 0 - disable).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_EcrConnectedOperReport(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// When making Z-report, automatically make "Report by Departments"
        /// </summary>
        /// <param name="variableName">EcrConnectedDeptReport</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - enable; 0 - disable).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_EcrConnectedDeptReport(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// When making Z-report, automatically make "Report by PLU with turnovers"
        /// </summary>
        /// <param name="variableName">EcrConnectedPluSalesReport</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - enable; 0 - disable).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_EcrConnectedPluSalesReport(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// When making Z-report, automatically make "Group report"
        /// </summary>
        /// <param name="variableName">EcrConnectedGroupsReport</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - enable; 0 - disable).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_EcrConnectedGroupsReport(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// When making Z-report, automatically make "Ecr report"
        /// </summary>
        /// <param name="variableName">EcrConnectedCashReport</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - enable; 0 - disable).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_EcrConnectedCashReport(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Periodic reports
        /// </summary>
        /// <param name="variableName">EcrUserPeriodReports</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - enable; 0 - disable).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_EcrUserPeriodReports(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// When making Z-report, automatically clear PLU turnover
        /// </summary>
        /// <param name="variableName">EcrPluDailyClearing</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - enable; 0 - disable).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_EcrPluDailyClearing(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Open drawer on every total
        /// </summary>
        /// <param name="variableName">EcrSafeOpening</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - enable; 0 - disable).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_EcrSafeOpening(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Text up to 10 symbols. If second number of the weight barcode\n
        /// match any of the symbols in this string, barcode will be interpreted as normal barcode
        /// </summary>
        /// <param name="variableName">EcrScaleBarMask</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_EcrScaleBarMask(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Count of used barcodes for each programmed article
        /// </summary>
        /// <param name="variableName">EcrNumberBarcode</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1...4).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_EcrNumberBarcode(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Time to clear display after last receipt in miliseconds
        /// </summary>
        /// <param name="variableName">RegModeOnIdle</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - 2 147 483 647).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_RegModeOnIdle(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// For ECR's only. The receipt is printed after last payment
        /// </summary>
        /// <param name="variableName">FlushAtEndOnly</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_FlushAtEndOnly(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// For ECR's only. Minutes before midnight, when ECR starts showing warning for Z report.
        /// </summary>
        /// <param name="variableName">EcrMidnightWarning</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_EcrMidnightWarning(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// For ECR's only. The operator must press STL key before payment
        /// </summary>
        /// <param name="variableName">EcrMandatorySubtotal</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1: yes, 0: no).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_EcrMandatorySubtotal(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// For ECR's only; Name of the seller
        /// </summary>
        /// <param name="variableName">Seller</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (36 symbols max).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_Seller(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// For ECR's only; Flag for a monthly report suggesting
        /// </summary>
        /// <param name="variableName">AutoMonthReport</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1: yes, 0: no).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_AutoMonthReport(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// For ECR's only; Warning for unsent documents from XX hours.
        // The value must be set in hours before device will be blocked
        /// </summary>
        /// <param name="variableName">EcrUnsentWarning</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_EcrUnsentWarning(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Local currency name
        /// </summary>
        /// <param name="variableName">CurrNameLocal</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (up to 3 chars).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_CurrNameLocal(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Foreign currency name
        /// </summary>
        /// <param name="variableName">CurrNameForeign</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (up to 3 chars).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_CurrNameForeign(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Exchange rate 
        /// </summary>
        /// <param name="variableName">ExchangeRate</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (from 1 to 999999999, decimal point is before last five digits).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_ExchangeRate(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// 
        /// </summary>
        /// <param name="variableName">Unit_name</param>
        /// <param name="index">The line. Index 0 is for line 1...Index 19 is for line 20</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (Text up to 6 chars).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_Unit_name(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// 
        /// </summary>
        /// <param name="variableName">Header</param>
        /// <param name="index">Index 0 is for line 1, Index 9 is for line 10</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.
        /// <list type="table">
        /// <item>
        /// <term>for FP-700X</term>
        /// <description>42, 48 or 64 columns</description>
        /// </item>
        /// <item>
        /// <term>for FMP-350X</term>
        /// <description>32 columns</description>
        /// </item>
        /// <item>
        /// <term>for FMP-55X</term>
        /// <description>42, 48 or 64 columns</description>
        /// </item>
        /// <item>
        /// <term>for DP-25X, DP-150X, WP-500X, WP-25X, WP-50X</term>
        /// <description>42 columns</description>
        /// </item> 
        /// </description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_Header(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Text up to XX symbols
        /// </summary>
        /// <param name="variableName">Footer</param>
        /// <param name="index">Index 0 is for line 1, Index 9 is for line 10</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.
        /// <list type = "table" >
        /// <item>
        /// <term>for FP-700X</term>
        /// <description>42, 48 or 64 columns</description>
        /// </item>
        /// <item>
        /// <term>for FMP-350X</term>
        /// <description>42, 48 or 64 columns</description>
        /// </item>
        /// <item>
        /// <term>for FMP-55X</term>
        /// <description>32 columns</description>
        /// </item>
        /// <item>
        /// <term>for DP-25X, DP-150X, WP-500X, WP-25X, WP-50X</term>
        /// <description>42 columns</description>
        /// </item> 
        /// </description>
        /// </item>
        /// </list>
        /// </description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_Footer(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Name of operator
        /// </summary>
        /// <param name="variableName">OperName</param>
        /// <param name="index">Number of operator</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (text up to 32 symbols).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_OperName(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Password of operator (Require Service jumper)
        /// </summary>
        /// <param name="variableName">OperPasw</param>
        /// <param name="index">Number of operator</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (Text up to 8 symbols).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_OperPasw(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Name of payment
        /// </summary>
        /// <param name="variableName">PayName</param>
        /// <param name="index">Number of payment</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (Text up to 16 symbols).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_PayName(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Forbid the payment
        /// </summary>
        /// <param name="variableName">Payment_forbidden</param>
        /// <param name="index">Number of payment</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1- forbidden, 0 - not forbidden).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_Payment_forbidden(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Number of PLU assigned to shortcut key
        /// </summary>
        /// <param name="variableName">DPxx_PluCode</param>
        /// <param name="index">Number of key</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (0 - Key is disabled; from 1  to 99999 for assigning PLU).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DPxx_PluCode(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Value for value surcharge; Value is in cents
        /// </summary>
        /// <param name="variableName">KeyNDB_value</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (from 0 to 999999999).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_KeyNDB_value(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Percentage for percentage surcharge; Value is in hundredths (0.01) of a percent
        /// </summary>
        /// <param name="variableName">KeyNDB_percentage</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (from 0 to 9999).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_KeyNDB_percentage(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Value for value discount; Value is in cents
        /// </summary>
        /// <param name="variableName">KeyOTS_value</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (from 0 to 999999999).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_KeyOTS_value(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Percentage for percentage discount; Value is in hundredths (0.01)
        /// of a percent.
        /// </summary>
        /// <param name="variableName">KeyOTS_percentage</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (from 0 to 9999).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_KeyOTS_percentage(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Forbid the surcharge key
        /// </summary>
        /// <param name="variableName">KeyNDB_forbidden</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1- forbidden, 0 - not forbidden).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_KeyNDB_forbidden(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Forbid the discount key
        /// </summary>
        /// <param name="variableName">KeyOTS_forbidden</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1- forbidden, 0 - not forbidden).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_KeyOTS_forbidden(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Password of the Service man.(Require Service jumper)
        /// </summary>
        /// <param name="variableName">ServPasw</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (Text up to 8 symbols).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_ServPasw(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Message that will be printed when "ServDate" is reached, up to 64 symbols
        /// </summary>
        /// <param name="variableName">ServMessage</param>
        /// <param name="index">Message line</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_ServMessage(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Service date( Format: DD-MM-YY HH:MM:SS).
        /// </summary>
        /// <param name="variableName">ServiceDate</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_ServiceDate(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Contrast of Printing.
        /// </summary>
        /// <param name="variableName">PrnQuality</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (from 0 to 20).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_PrnQuality(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Number of printer columns.
        /// </summary>
        /// <param name="variableName">PrintColumns</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.
        /// <list type="table">
        /// <item>
        /// <term>for FP-700X</term>
        /// <description>42, 48 or 64 columns</description>
        /// </item>
        /// <item>
        /// <term>for FMP-350X</term>
        /// <description>42, 48 or 64 columns</description>
        /// </item>
        /// <item>
        /// <term>for FMP-55X</term>
        /// <description>32 columns</description>
        /// </item>
        /// <item>
        /// <term>for DP-25X, DP-150X, WP-500X, WP-25X, WP-50X</term>
        /// <description>42 columns</description>
        /// </item>
        /// </list>
        /// </description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_PrintColumns(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Print empty line after TOTAL line in fiscal receipts
        /// </summary>
        /// <param name="variableName">EmptyLineAfterTotal</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - enable, 0 -disable).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_EmptyLineAfterTotal(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Print TOTAL line in fiscal receipts with double height
        /// </summary>
        /// <param name="variableName">DblHeigh_totalinreg</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - enable, 0 -disable).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DblHeigh_totalinreg(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Bold print of the payment names in fiscal receipt.
        /// </summary>
        /// <param name="variableName">Bold_payments</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - enable, 0 -disable).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_Bold_payments(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Print receipt dublicate.
        /// </summary>
        /// <param name="variableName">DublReceipts</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - enable, 0 -disable).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DublReceipts(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Number of internal receipts
        /// </summary>
        /// <param name="variableName">IntUseReceipts</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (from 0 to 9).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_IntUseReceipts(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Print PLU barcode in the receipt.
        /// </summary>
        /// <param name="variableName">BarcodePrint</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - enable, 0 -disable).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_BarcodePrint(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Print the logo in the receipt.
        /// </summary>
        /// <param name="variableName">LogoPrint</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - enable, 0 -disable).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_LogoPrint(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Print the department name at the beginning of the receipt
        /// </summary>
        /// <param name="variableName">DoveritelPrint</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - enable, 0 -disable).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DoveriteliPrint(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Print total sum in foreign currency
        /// </summary>
        /// <param name="variableName">ForeignPrint</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - enable, 0 -disable, 2 - print exchange rate).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_ForeignPrint(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Print VAT rates in the receipt
        /// </summary>
        /// <param name="variableName">VatPrintEnable</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - enable, 0 -disable).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_VatPrintEnable(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        //public Dictionary<string, string> info_Get_CondensedPrint(string variableName, string index, string value) 
        //{
        //    StringBuilder inputString = new StringBuilder();

        //    inputString.Append(variableName);
        //    inputString.Append("\t");
        //    inputString.Append(index);
        //    inputString.Append("\t");
        //    inputString.Append(value);
        //    inputString.Append("\t");

        //    string r = CustomCommand(255 , inputString.ToString());
        //    CheckResult();

        //    string[] split = r.Split('\t');

        //    if (split.Length >= 1)
        //    {
        //        var error = int.Parse(split[0]);
        //        if (error != 0)
        //        {
        //            throw new FiscalException(error, "Operation failed with error code: "+split[0]);
        //        }
        //    }

        //    Dictionary<string, string> result = new Dictionary<string, string>();

        //    if(split.Length >= 1) 
        //        result["errorCode"] = split[0];
        //    if(split.Length >= 2) 
        //        result["variableValue"] = split[1];
        //    return result;
        //}

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Disable Z report generating from the keyboard.
        /// </summary>
        /// <param name="variableName">DsblKeyZreport</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - disabled, 0 - enabled).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DsblKeyZreport(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Disable X report generating from the keyboard
        /// </summary>
        /// <param name="variableName">DsblKeyXreport</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - disabled, 0 - enabled).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DsblKeyXreport(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Disable diagnostic info.
        /// </summary>
        /// <param name="variableName">DsblKeyDiagnostics</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - disabled, 0 - enabled).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DsblKeyDiagnostics(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Disable fiscal memory reports.
        /// </summary>
        /// <param name="variableName">DsblKeyFmReports</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - disabled, 0 - enabled).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DsblKeyFmReports(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Disable electronic journal menu.
        /// </summary>
        /// <param name="variableName">DsblKeyJournal</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - disabled, 0 - enabled).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DsblKeyJournal(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Disable changing the date and time
        /// </summary>
        /// <param name="variableName">DsblKeyDateTime</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - disabled, 0 - enabled).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DsblKeyDateTime(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Disable manualy closing of the receipt
        /// </summary>
        /// <param name="variableName">DsblKeyCloseReceipt</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - disabled, 0 - enabled).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DsblKeyCloseReceipt(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Disable manualy cancellation of the receipt
        /// </summary>
        /// <param name="variableName">DsblKeyCancelReceipt</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - disabled, 0 - enabled).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DsblKeyCancelReceipt(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Model of the modem
        /// </summary>
        /// <param name="variableName">ModemModel</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (0 - Quectel M72, 1 - Quectel UC20, 2 - Quectel
        /// M66, 3- Quectel UG96).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_ModemModel(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// PIN code of SIM card.
        /// </summary>
        /// <param name="variableName">SimPin</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (Text up to 16 symbols).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_SimPin(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// MAC address of the LAN controller
        /// </summary>
        /// <param name="variableName">LanMAC</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (up to 12 chars).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_LanMAC(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Enable use of DHCP
        /// </summary>
        /// <param name="variableName">DHCPenable</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - enable, 0 -disable).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DHCPenable(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// IP address when DHCP is disabled
        /// </summary>
        /// <param name="variableName">LAN_IP</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (up to 15 chars).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_LAN_IP(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Net mask when DHCP is disabled
        /// </summary>
        /// <param name="variableName">LAN_NetMask</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (up to 15 chars).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_LAN_NetMask(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Default gateway when DHCP is disabled
        /// </summary>
        /// <param name="variableName">LAN_Gateway</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (up to 15 chars).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_LAN_Gateway(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Primary DNS when DHCP is disabled
        /// </summary>
        /// <param name="variableName">LAN_PriDNS</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (up to 15 chars).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_LAN_PriDNS(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Second DNS when DHCP is disabled
        /// </summary>
        /// <param name="variableName">LAN_SecDNS</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (up to 15 chars).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_LAN_SecDNS(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// The number of listening port for PC connection.(only for devices with LAN)
        /// </summary>
        /// <param name="variableName">LANport_fpCommands</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_LANport_fpCommands(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Name of item group.
        /// </summary>
        /// <param name="variableName">ItemGroups_name</param>
        /// <param name="index">Number of item group</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (Text up to 32 symbols).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_ItemGroups_name(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Programmed price of department( from 0 to 999999999 )
        /// </summary>
        /// <param name="variableName">Dept_price</param>
        /// <param name="index">Number of department</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_Dept_price(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Name of department
        /// </summary>
        /// <param name="variableName">Dept_name</param>
        /// <param name="index">Number of department</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (Text up to 72 symbols).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_Dept_name(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Flag that tells if the entered tovaritelnica has to be checked with DHL's algorithm
        /// </summary>
        /// <param name="variableName">DHL_Algo</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DHL_Algo(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Flag that tells if the entered EIK number has to be valid
        /// </summary>
        /// <param name="variableName">EIK_validation</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_EIK_validation(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Flag that tells if the entered EGN number has to be valid;
        /// </summary>
        /// <param name="variableName">EGN_validation</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_EGN_validation(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Description of the bonus.
        /// </summary>
        /// <param name="variableName">Bonuses</param>
        /// <param name="index">Number of bonus</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (Text up to 40 symbols).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_Bonuses(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Free text lines describing reason for reduced VAT.
        /// </summary>
        /// <param name="variableName">TextReducedVAT</param>
        /// <param name="index">Number of line</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (Text up to 42 symbols).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_TextReducedVAT(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// TAX number label
        /// </summary>
        /// <param name="variableName">TAXlabel</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (up to 10 chars).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_TAXlabel(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Number of current Z-report.
        /// </summary>
        /// <param name="variableName">nZreport</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_nZreport(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Number of current memory failure
        /// </summary>
        /// <param name="variableName">nReset</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_nReset(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Number of current VAT change
        /// </summary>
        /// <param name="variableName">nVatChanges</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_nVatChanges(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Number of current SN changes
        /// </summary>
        /// <param name="variableName">nIDnumberChanges</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (0 - not programmed; 1 - programmed).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_nIDnumberChanges(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Number of current FM number changes
        /// </summary>
        /// <param name="variableName">nFMnumberChanges</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (0 - not programmed; 1 - programmed).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_nFMnumberChanges(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Number of current TAX number changes
        /// </summary>
        /// <param name="variableName">nTAXnumberChanges</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (0 - not programmed; 1 - programmed).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_nTAXnumberChanges(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Current value of VAT.
        /// </summary>
        /// <param name="variableName">valVat</param>
        /// <param name="index">Number of VAT</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_valVat(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// ID of the fiscal memory.
        /// </summary>
        /// <param name="variableName">FMDeviceID</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value .</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_FMDeviceID(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Serial number of the ECR.
        /// </summary>
        /// <param name="variableName">IDnumber</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_IDnumber(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Number of FM.
        /// </summary>
        /// <param name="variableName">FMnumber</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_FMnumber(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Get TAX number.
        /// </summary>
        /// <param name="variableName">TAXnumber</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current tax number.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_TAXnumber(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Date and time for writting block in FM
        /// </summary>
        /// <param name="variableName">FmWriteDateTime</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value .</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_FmWriteDateTime(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Last valid date (written on FM or EJ).
        /// </summary>
        /// <param name="variableName">LastValiddate</param>
        /// <param name="index"></param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_LastValiddate(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Get last printed unique sale number 
        /// </summary>
        /// <param name="variableName">UNP</param>
        /// <param name="index">Leave blank.</param>
        /// <param name="value">Leave blank.</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value in format ((21 chars "LLDDDDDD-CCCC-DDDDDDD",
        /// L[A - Z], C[0 - 9A - Za - z], D[0 - 9])).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_UNP(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Get last printed unique sale number in strono document
        /// </summary>
        /// <param name="variableName">StornoUNP</param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value in format ((21 chars "LLDDDDDD-CCCC-DDDDDDD",
        /// L[A - Z], C[0 - 9A - Za - z], D[0 - 9])).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_StornoUNP(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Flag that shows if FPr is fiscalized.
        /// </summary>
        /// <param name="variableName">Fiscalized</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (format: ( 1 - fiscalized; 0 - not fiscalized).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_Fiscalized(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Shows if fiscal receipt is issued after last Z-report.
        /// </summary>
        /// <param name="variableName">DFR_needed</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (format: (1 - Z-report is needed; 0 - Z-report is not needed ).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DFR_needed(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Number of symbols after decimal point
        /// </summary>
        /// <param name="variableName">DecimalPoint</param>
        /// <param name="index">Leave blank</param>
        /// <param name="value">Leave blank</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DecimalPoint(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Global number of receipts
        /// </summary>
        /// <param name="variableName">nBon</param>
        /// <param name="index">Leave blank.</param>
        /// <param name="value">Leave blank.</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_nBon(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Global number of fiscal receipts.
        /// </summary>
        /// <param name="variableName">nFBon</param>
        /// <param name="index">Leave blank.</param>
        /// <param name="value">Leave blank.</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_nFBon(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Number of invoices.
        /// </summary>
        /// <param name="variableName">nInvoice</param>
        /// <param name="index">Leave blank.</param>
        /// <param name="value">Leave blank.</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_nInvoice(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }


        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Start of the invoice range.
        /// </summary>
        /// <param name="variableName">InvoiceRangeBeg</param>
        /// <param name="index">Leave blank.</param>
        /// <param name="value">Leave blank.</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (from 0 to 9999999999).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_InvoiceRangeBeg(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// End of the invoice range.
        /// </summary>
        /// <param name="variableName">InvoiceRangeEnd</param>
        /// <param name="index">Leave blank.</param>
        /// <param name="value">Leave blank.</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (from 0 to 9999999999).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_InvoiceRangeEnd(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Number of fiscal receipts for the day.
        /// </summary>
        /// <param name="variableName">nFBonDailyCount</param>
        /// <param name="index">Leave blank.</param>
        /// <param name="value">Leave blank.</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_nFBonDailyCount(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Last number of fiscal receipt.
        /// </summary>
        /// <param name="variableName">nLastFiscalDoc</param>
        /// <param name="index">Leave blank.</param>
        /// <param name="value">Leave blank.</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_nLastFiscalDoc(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Number of current operator
        /// </summary>
        /// <param name="variableName">CurrClerk</param>
        /// <param name="index">Leave blank.</param>
        /// <param name="value">Leave blank.</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_CurrClerk(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Get new EJ
        /// </summary>
        /// <param name="variableName">EJNewJurnal</param>
        /// <param name="index">Leave blank.</param>
        /// <param name="value">Leave blank.</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_EJNewJurnal(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Number of current EJ.
        /// </summary>
        /// <param name="variableName">EJNumber</param>
        /// <param name="index">Leave blank.</param>
        /// <param name="value">Leave blank.</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_EJNumber(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// Date/time of last connection to the server.
        /// </summary>
        /// <param name="variableName">DateLastSucceededSent</param>
        /// <param name="index">Leave blank.</param>
        /// <param name="value">Leave blank.</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_DateLastSucceededSent(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        /// <summary>
        /// ECR is registered on the NRA server.
        /// </summary>
        /// <param name="variableName">NapRegistered</param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>variableValue</term>
        /// <description>Current value (1 - registered; 0 -not registered).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> info_Get_NapRegistered(string variableName, string index, string value) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(variableName);
            inputString.Append("\t");
            inputString.Append(index);
            inputString.Append("\t");
            inputString.Append(value);
            inputString.Append("\t");

            string r = CustomCommand(255 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["variableValue"] = split[1];
            return result;
        }

        // Command number(Dec): 255 - please check fiscal device documentation.
        //public Dictionary<string, string> info_Get_DeregOnSever(string variableName, string index, string value) 
        //{
        //    StringBuilder inputString = new StringBuilder();

        //    inputString.Append(variableName);
        //    inputString.Append("\t");
        //    inputString.Append(index);
        //    inputString.Append("\t");
        //    inputString.Append(value);
        //    inputString.Append("\t");

        //    string r = CustomCommand(255 , inputString.ToString());
        //    CheckResult();

        //    string[] split = r.Split('\t');

        //    if (split.Length >= 1)
        //    {
        //        var error = int.Parse(split[0]);
        //        if (error != 0)
        //        {
        //            throw new FiscalException(error, "Operation failed with error code: "+split[0]);
        //        }
        //    }

        //    Dictionary<string, string> result = new Dictionary<string, string>();

        //    if(split.Length >= 1) 
        //        result["errorCode"] = split[0];
        //    if(split.Length >= 2) 
        //        result["variableValue"] = split[1];
        //    return result;
        //}

        // Command number(Dec): 124 - please check fiscal device documentation.
        /// <summary>
        /// Search receipt number by period
        /// </summary>
        /// <param name="fromDateTime">Start date and time for searching ( format "DD-MM-YY hh:mm:ss DST" ). Default: Date and time of first document</param>
        /// <param name="toDateTime">End date and time for searching ( format "DD-MM-YY hh:mm:ss DST" ). Default: Date and time of last document</param>
        /// <param name="inDocType">Type of document: '0' - all types; '1' - fiscal receipts; '2' - daily Z reports; '3' - invoice receipts\n
        /// '4' - non fiscal receipts; '5' - paidout receipts; '6' - fiscal receipts - storno; '7' - invoice receipts - storno;
        /// '8' - cancelled receipts (all voided); '9' - daily X reports; '10' - fiscal receipts, invoice receipts, fiscal receipts - storno and invoice receipts - storno</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>startDateTime</term>
        /// <description>Start date for searching, see DateTime format described at the beginning of the document</description>
        /// </item>
        /// <item>
        /// <term>endDateTime</term>
        /// <description>End date for searching, see DateTime format described at the beginning of the document</description>
        /// </item>
        /// <item>
        /// <term>firstDocumentNumber</term>
        /// <description>First document in the period. For DocType = '2' (1...3650), else (1...99999999)</description>
        /// </item>
        /// <item>
        /// <term>lastDocumentNumber</term>
        /// <description>Last document in the period. For DocType = '2' (1...3650), else (1...99999999)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_Documents_InRange(string fromDateTime, string toDateTime, string inDocType) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(fromDateTime);
            inputString.Append("\t");
            inputString.Append(toDateTime);
            inputString.Append("\t");
            inputString.Append(inDocType);
            inputString.Append("\t");

            string r = CustomCommand(124 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["startDateTime"] = split[1];
            if(split.Length >= 3) 
                result["endDateTime"] = split[2];
            if(split.Length >= 4) 
                result["firstDocumentNumber"] = split[3];
            if(split.Length >= 5) 
                result["lastDocumentNumber"] = split[4];
            return result;
        }

        // Command number(Dec): 125 - please check fiscal device documentation.
        /// <summary>
        /// Information from EJ - Set document to read
        /// </summary>
        /// <param name="option">'0' - Set document to read</param>
        /// <param name="docNum">Number of document (1...9999999).</param>
        /// <param name="recType">Document type: '0' - all types; '1' - fiscal receipts; '2' - daily Z reports; '3' - invoice receipts\n
        /// '4' - nonfiscal receipts; '5' - paidout receipts; '6' - fiscal receipts - storno; '7' - invoice receipts - storno;  '8' - cancelled receipts(all voided);
        /// '9' - daily X reports; '10' - fiscal receipts, invoice receipts, fiscal receipts - storno and invoice receipts - storno;</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>docNumber</term>
        /// <description>Number of document (global 1...9999999)</description>
        /// </item>
        /// <item>
        /// <term>recNumber</term>
        /// <description>Number of document (depending "Type")</description>
        /// </item>
        /// <item>
        /// <term>dateTime</term>
        /// <description>Date of document, see DateTime format described at the beginning of the document</description>
        /// </item>
        /// <item>
        /// <term>docType</term>
        /// <description>Type of document: '0' - all types; '1' - fiscal receipts; '2' - daily Z reports; '3' - invoice receipts\n
        /// '4' - non fiscal receipts; '5' - paidout receipts; '6' - fiscal receipts - storno; '7' - invoice receipts - storno
        /// '8' - cancelled receipts ( all voided); '9' - daily X reports</description>
        /// </item>
        /// <item>
        /// <term>znumber</term>
        /// <description>Number of Z report (1...3650)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_SetDocument_ToRead(string option, string docNum, string recType) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(docNum);
            inputString.Append("\t");
            inputString.Append(recType);
            inputString.Append("\t");

            string r = CustomCommand(125 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["docNumber"] = split[1];
            if(split.Length >= 3) 
                result["recNumber"] = split[2];
            if(split.Length >= 4) 
                result["dateTime"] = split[3];
            if(split.Length >= 5) 
                result["docType"] = split[4];
            if(split.Length >= 6) 
                result["znumber"] = split[5];
            return result;
        }

        // Command number(Dec): 125 - please check fiscal device documentation.
        /// <summary>
        /// Information of EJ - Read one line as text.
        /// </summary>
        /// <param name="option">'1' - Read one line as text.Must be called multiple times to read the whole document</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>textData</term>
        /// <description>Document text (up to 64 chars)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_GetLine_AsText(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");

            string r = CustomCommand(125 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["textData"] = split[1];
            return result;
        }

        // Command number(Dec): 125 - please check fiscal device documentation.
        /// <summary>
        /// Information from EJ - Read as data. Must be called multiple times to read the whole document
        /// </summary>
        /// <param name="option">'2' - Read as data. Must be called multiple times to read the whole document</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>base64Data</term>
        /// <description>Document data, structured information in base64 format. Detailed information in other document</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_GetLine_AsData(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");

            string r = CustomCommand(125 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["base64Data"] = split[1];
            return result;
        }

        // Command number(Dec): 125 - please check fiscal device documentation.
        /// <summary>
        /// Information from EJ - Print document
        /// </summary>
        /// <param name="option">'3' - Print document</param>
        /// <param name="docNum">Number of document (1...9999999)</param>
        /// <param name="recType">Document type: '0' - all types; '1' - fiscal receipts; '2' - daily Z reports; '3' - invoice receipts\n
        /// '4' - nonfiscal receipts; '5' - paidout receipts; '6' - fiscal receipts - storno; '7' - invoice receipts - storno;  '8' - cancelled receipts(all voided);
        /// '9' - daily X reports; '10' - fiscal receipts, invoice receipts, fiscal receipts - storno and invoice receipts - storno;</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_Print_Document(string option, string docNum, string recType) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(docNum);
            inputString.Append("\t");
            inputString.Append(recType);
            inputString.Append("\t");

            string r = CustomCommand(125 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 125 - please check fiscal device documentation.
        /// <summary>
        /// Read CSV data
        /// </summary>
        /// <param name="option">Type of information; '9' - Set document to read</param>
        /// <param name="firstDoc">First document in the period (1...99999999). Number received in response to command 124</param>
        /// <param name="lastDoc">Last document in the period. (1...99999999). Number received in response to command 124</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>docNumber</term>
        /// <description>Number of document (global 1...9999999)</description>
        /// </item>
        /// <item>
        /// <term>recNumber</term>
        /// <description>Number of document (depending "Type")</description>
        /// </item>
        /// <item>
        /// <term>dateTime</term>
        /// <description>Date of document, see DateTime format described at the beginning of the document</description>
        /// </item>
        /// <item>
        /// <term>docType</term>
        /// <description>Type of document: '0' - all types; '1' - fiscal receipts; '2' - daily Z reports; '3' - invoice receipts\n
        /// '4' - non fiscal receipts; '5' - paidout receipts; '6' - fiscal receipts - storno; '7' - invoice receipts - storno
        /// '8' - cancelled receipts ( all voided); '9' - daily X reports</description>
        /// </item>
        /// <item>
        /// <term>znumber</term>
        /// <description>Number of Z report (1...3650)</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_SetCSVDocument_ToRead(string option, string firstDoc, string lastDoc) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");
            inputString.Append(firstDoc);
            inputString.Append("\t");
            inputString.Append(lastDoc);
            inputString.Append("\t");

            string r = CustomCommand(125 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["docNumber"] = split[1];
            if(split.Length >= 3) 
                result["recNumber"] = split[2];
            if(split.Length >= 4) 
                result["dateTime"] = split[3];
            if(split.Length >= 5) 
                result["docType"] = split[4];
            if(split.Length >= 6) 
                result["znumber"] = split[5];
            return result;
        }

        // Command number(Dec): 125 - please check fiscal device documentation.
        /// <summary>
        /// Read as data. Must be called multiple times to read the whole document
        /// </summary>
        /// <param name="option">'8' - Read as data. Must be called multiple times to read the whole document</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>column01</term>
        /// <description>Fiscal serial number</description>
        /// </item>
        /// <item>
        /// <term>column02</term>
        /// <description>Fiscal receipt type: Fiscal receipt, extended fiscal receipt, STORNO fiscal receipt or extended STORNO fiscal receipt</description>
        /// </item>
        /// <item>
        /// <term>column03</term>
        /// <description>Number of fiscal receipt</description>
        /// </item>
        /// <item>
        /// <term>column04</term>
        /// <description>UNP - unique sale number (if the device is fiscal printer)</description>
        /// </item>
        /// <item>
        /// <term>column05</term>
        /// <description>Product/service name</description>
        /// </item>
        /// <item>
        /// <term>column06</term>
        /// <description>Product/service price</description>
        /// </item>
        /// <item>
        /// <term>column07</term>
        /// <description>Product/service quantity</description>
        /// </item>
        /// <item>
        /// <term>column08</term>
        /// <description>Product/service total</description>
        /// </item>
        /// <item>
        /// <term>column09</term>
        /// <description>Total sum of fiscal receipt/ STORNO fr or extended ones</description>
        /// </item>
        /// <item>
        /// <term>column10</term>
        /// <description>Invoice number</description>
        /// </item>
        /// <item>
        /// <term>column11</term>
        /// <description>Customer EIK (if the receipt is extended)</description>
        /// </item>
        /// <item>
        /// <term>column12</term>
        /// <description>STORNO receipt number</description>
        /// </item>
        /// <item>
        /// <term>column13</term>
        /// <description>STORNO invoice number</description>
        /// </item>
        /// <item>
        /// <term>column14</term>
        /// <description>Reason for STORNO</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> klen_Get_CSVData_AsText(string option) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(option);
            inputString.Append("\t");

            string r = CustomCommand(125 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["column01"] = split[1];
            if(split.Length >= 3) 
                result["column02"] = split[2];
            if(split.Length >= 4) 
                result["column03"] = split[3];
            if(split.Length >= 5) 
                result["column04"] = split[4];
            if(split.Length >= 6) 
                result["column05"] = split[5];
            if(split.Length >= 7) 
                result["column06"] = split[6];
            if(split.Length >= 8) 
                result["column07"] = split[7];
            if(split.Length >= 9) 
                result["column08"] = split[8];
            if(split.Length >= 10) 
                result["column09"] = split[9];
            if(split.Length >= 11) 
                result["column10"] = split[10];
            if(split.Length >= 12) 
                result["column11"] = split[11];
            if(split.Length >= 13) 
                result["column12"] = split[12];
            if(split.Length >= 14) 
                result["column13"] = split[13];
            if(split.Length >= 15) 
                result["column14"] = split[14];
            return result;
        }

        // Command number(Dec): 45 - please check fiscal device documentation.
        /// <summary>
        /// Check for mode connection with PC
        /// </summary>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> other_Check_Connection() 
        {
            // Command without input data.
            StringBuilder inputString = new StringBuilder();

            string r = CustomCommand(45 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 80 - please check fiscal device documentation.
        /// <summary>
        /// Play sound
        /// </summary>
        /// <param name="hz">Frequency (0...65535)</param>
        /// <param name="mSec">Time in milliseconds (0...65535)</param>
        /// <returns>Dictionary with key:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> other_Sound_Signal(string hz, string mSec) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(hz);
            inputString.Append("\t");
            inputString.Append(mSec);
            inputString.Append("\t");

            string r = CustomCommand(80 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 202 - please check fiscal device documentation.
        public Dictionary<string, string> other_Power_Off(string paramValue) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(paramValue);
            inputString.Append("\t");

            string r = CustomCommand(202 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 202 - please check fiscal device documentation.
        public Dictionary<string, string> other_Restart(string paramValue) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(paramValue);
            inputString.Append("\t");

            string r = CustomCommand(202 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            result["errorCode"] = split[0];
            return result;
        }

        // Command number(Dec): 83 - please check fiscal device documentation.
        /// <summary>
        /// Programming of VAT rates
        /// </summary>
        /// <param name="taxA">Value of VAT rate A;0.00...99.99 - enabled;100.00 - disabled</param>
        /// <param name="taxB">Value of VAT rate B;0.00...99.99 - enabled;100.00 - disabled</param>
        /// <param name="taxC">Value of VAT rate C;0.00...99.99 - enabled;100.00 - disabled</param>
        /// <param name="taxD">Value of VAT rate D;0.00...99.99 - enabled;100.00 - disabled</param>
        /// <param name="taxE">Value of VAT rate E;0.00...99.99 - enabled;100.00 - disabled</param>
        /// <param name="taxF">Value of VAT rate F;0.00...99.99 - enabled;100.00 - disabled</param>
        /// <param name="taxG">Value of VAT rate G;0.00...99.99 - enabled;100.00 - disabled</param>
        /// <param name="taxH">Value of VAT rate H;0.00...99.99 - enabled;100.00 - disabled</param>
        /// <param name="decimalPoint">value: 0 or 2( if decimal_point = 0 - work with integer prices. If decimal_point = 2 - work with fract prices</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>remainingChanges</term>
        /// <description>Number of remaining changes (1...30).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> service_Set_DecimalsAndTaxRates(string taxA, string taxB, string taxC, string taxD, string taxE, string taxF, string taxG, string taxH, string decimalPoint) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(taxA);
            inputString.Append("\t");
            inputString.Append(taxB);
            inputString.Append("\t");
            inputString.Append(taxC);
            inputString.Append("\t");
            inputString.Append(taxD);
            inputString.Append("\t");
            inputString.Append(taxE);
            inputString.Append("\t");
            inputString.Append(taxF);
            inputString.Append("\t");
            inputString.Append(taxG);
            inputString.Append("\t");
            inputString.Append(taxH);
            inputString.Append("\t");
            inputString.Append(decimalPoint);
            inputString.Append("\t");

            string r = CustomCommand(83 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["remainingChanges"] = split[1];
            return result;
        }

        // Command number(Dec): 89 - please check fiscal device documentation.
        /// <summary>
        /// Test of Fiscal Memory
        /// </summary>
        /// <param name="testType">Write test: '0' - Read test; '1' - Write and read test</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>records</term>
        /// <description>Number of records left (0...16)..</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> service_Test_FiscalMemory(string testType) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(testType);
            inputString.Append("\t");

            string r = CustomCommand(89 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["records"] = split[1];
            return result;
        }

        // Command number(Dec): 91 - please check fiscal device documentation.
        /// <summary>
        /// Programming of Serial number and FM number
        /// </summary>
        /// <param name="serialNumber">Serial Number ( Two letters and six digits: XX123456).</param>
        /// <param name="fMNumber">Fiscal Memory Number (Eight digits).</param>
        /// <returns>Dictionary with keys:
        /// <list type="table">
        /// <item>
        /// <term>errorCode</term>
        /// <description>Indicates an error code. If command passed, ErrorCode is 0.</description>
        /// </item>
        /// <item>
        /// <term>country</term>
        /// <description>Name of the country (up to 32 symbols).</description>
        /// </item>
        /// </list>
        /// </returns>
        public Dictionary<string, string> service_Set_SerialNumber(string serialNumber, string fMNumber) 
        {
            StringBuilder inputString = new StringBuilder();

            inputString.Append(serialNumber);
            inputString.Append("\t");
            inputString.Append(fMNumber);
            inputString.Append("\t");

            string r = CustomCommand(91 , inputString.ToString());
            CheckResult();

            string[] split = r.Split('\t');

            if (split.Length >= 1)
            {
                var error = int.Parse(split[0]);
                if (error != 0)
                {
                    throw new FiscalException(error, "Operation failed with error code: "+split[0]);
                }
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            if(split.Length >= 1) 
                result["errorCode"] = split[0];
            if(split.Length >= 2) 
                result["country"] = split[1];
            return result;
        }

        //AI generated source code  -end

        public bool ItIs_SummerDT(string dateTime)
        {
            // in format dd-MM-yy HH:mm:ss 
            DateTime dt = DateTime.ParseExact(dateTime, "dd-MM-yy HH:mm:ss", null);
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

            int sumF = 0;

            for (int i = 6; i >= 0; i--)
            {
                int digit = data[i] - 0x30;
                if ((i & 0x01) == 1)
                    sumF += digit;
                else
                    sumF += digit * 3;
            }
            int mod = sumF % 10;
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

            int sumF = 0;

            for (int i = 11; i >= 0; i--)
            {
                int digit = data[i] - 0x30;
                if ((i & 0x01) == 1)
                    sumF += digit;
                else
                    sumF += digit * 3;
            }
            int mod = sumF % 10;
            return mod == 0 ? 0 : 10 - mod;
        }

        private string[] ReadDocumentLines(int documentNum, out string docType)
        {
            docType = "";
            if (!device_Connected)
                throw new Exception("Fiscal device not connected");

            List<string> lines = new List<string>();

            try
            {
                // command 125 - Information from EJ,reading by document number.                                
                var result = klen_SetDocument_ToRead("0", documentNum.ToString(), userDocumentType.ToString());
                if (result["errorCode"] != "0")
                    throw new Exception("Reading document failed with error: " + result["errorCode"]);
                if (documentNum.ToString() != result["docNumber"])
                    throw new Exception("Document number mismatch");
                documentDateTime = result["dateTime"];
                docType = result["docType"];


                do
                {
                    var rowReslt = klen_GetLine_AsText("1"); // read line by line as text                               

                    if (rowReslt["errorCode"] == "0")
                    {

                        string[] rowsData = rowReslt["textData"].Split(new string[] { "\r\n" }, StringSplitOptions.None);
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
                    }
                } while (true);
            }
            catch (FiscalException ex)
            {
                if (ex.ErrorCode != -100003)
                    throw;

            }

            return lines.ToArray();
        }

        private SizeF DrawReceiptOnGraphics(Graphics gr, string documentNumber, Font font, string[] lines, bool calculate,string docType)
        {
            string qrcodeText = "";
            string dt = "", date = "", time = "";
            var receiptSize = new SizeF(0, 0);
            Brush textBrush = new SolidBrush(Color.Black);
            bool barcodeFlag = false;
            var maxCharsPerLine = 0;

            if (docType != "" && docType != "2" && docType != "9")
            {
                string receiptNumber = documentNumber.PadLeft(7, '0');
                receiptNumber = documentNumber.PadLeft(7, '0');
                // 14-05-20 10:29:10 DST
                if (documentDateTime.Contains("DST")) dt = documentDateTime.Replace("DST", "");
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
                if (barcodeFlag)
                {
                    // There is a chance that a checksum of two types of barcode can match. Keep that in mind!
                    // Unfortunately there is no other way, rather than this, that we can say which is the type of barcode.
                    string barcodeText = line.Replace(" ", "");
                    if (barcodeText.Length == 0)
                    {
                        barcodeFlag = false;
                        continue;
                    }
                    if (barcodeText.Length == 8)
                    {
                        if (Checksum_ean8(barcodeText) == barcodeText[7])
                        {
                            var barcodeWriter = new BarcodeWriter<Bitmap>
                            {
                                Format = BarcodeFormat.EAN_8,
                                Options = new ZXing.Common.EncodingOptions
                                {
                                    Height = 20,
                                    Margin = 0
                                }
                            };
                            pictureBarcode = barcodeWriter.Write(barcodeText);
                        }

                        else
                        {
                            var barcodeWriter = new BarcodeWriter<Bitmap>
                            {
                                Format = BarcodeFormat.CODE_128,
                                Options = new ZXing.Common.EncodingOptions
                                {
                                    Height = 30,
                                    Margin = 0
                                }
                            };
                            pictureBarcode = barcodeWriter.Write(barcodeText);
                        }
                    }

                    else if (barcodeText.Length == 13)
                    {
                        if (Checksum_ean13(barcodeText) == barcodeText[12])
                        {
                            var barcodeWriter = new BarcodeWriter<Bitmap>
                            {
                                Format = BarcodeFormat.EAN_13,
                                Options = new ZXing.Common.EncodingOptions
                                {
                                    Height = 30,
                                    Margin = 0
                                }
                            };
                            pictureBarcode = barcodeWriter.Write(barcodeText);
                        }
                        else
                        {
                            var barcodeWriter = new BarcodeWriter<Bitmap>
                            {
                                Format = BarcodeFormat.CODE_128,
                                Options = new ZXing.Common.EncodingOptions
                                {
                                    Height = 30,
                                    Margin = 0
                                }
                            };
                            pictureBarcode = barcodeWriter.Write(barcodeText);
                        }

                    }
                    else
                    {
                        var barcodeWriter = new BarcodeWriter<Bitmap>
                        {
                            Format = BarcodeFormat.CODE_128,
                            Options = new ZXing.Common.EncodingOptions
                            {
                                Height = 30,
                                Margin = 0
                            }
                        };
                        pictureBarcode = barcodeWriter.Write(barcodeText);
                    }


                    if (!calculate)
                        gr.DrawImage(pictureBarcode, new PointF((receiptSize.Width - pictureBarcode.Width) / 2, receiptSize.Height));
                    receiptSize.Height += pictureBarcode.Height + 30; //bit of a space
                    receiptSize.Width = Math.Max(receiptSize.Width, pictureBarcode.Width);


                    barcodeFlag = false;
                }
                else
                {
                    if (line.Contains("BARCODE"))
                        barcodeFlag = true;
                    else if (line.Contains("ФИСКАЛЕН БОН")) // трябва да се провери
                    {
                        Font boldFont = new Font(font.Name, 16, FontStyle.Bold);
                        if (docType != "" && docType != "2" && docType != "9")
                        {
                            var barcodeWriter = new BarcodeWriter<Bitmap>
                            {
                                Format = BarcodeFormat.QR_CODE,
                                Options = new ZXing.Common.EncodingOptions
                                {
                                    Height = 30,
                                    Margin = 0
                                }
                            };
                            pictureBarcode = barcodeWriter.Write(qrcodeText);
                        }
                        Image bgMap = Image.FromFile(Directory.GetCurrentDirectory() + "\\Resources\\BGmapS.bmp");
                        var textSize = gr.MeasureString(line.Trim(), boldFont);
                        if (!calculate)
                        {

                            if (docType != "" && docType != "2" && docType != "9")
                            {
                                gr.DrawImage(pictureBarcode, new PointF((receiptSize.Width - pictureBarcode.Width) / 2, receiptSize.Height));
                                receiptSize.Height += pictureBarcode.Height + 20; //bit of a space
                                receiptSize.Width = Math.Max(receiptSize.Width, pictureBarcode.Width);
                            }
                            PointF mapPoint = new PointF((receiptSize.Width - (bgMap.Width + textSize.Width)) / 2, receiptSize.Height);
                            gr.DrawImage(bgMap, mapPoint);
                            gr.DrawString(line.Trim(), boldFont, textBrush, mapPoint.X + bgMap.Width + 10, receiptSize.Height);
                            receiptSize.Height += bgMap.Height + 30; //bit of a space
                            receiptSize.Width = Math.Max(receiptSize.Width, bgMap.Width + textSize.Width);
                        }
                        else
                        {
                            if (docType != "" && docType != "2" && docType != "9")
                            {
                                receiptSize.Height += pictureBarcode.Height + 20; //bit of a space
                                receiptSize.Width = Math.Max(receiptSize.Width, pictureBarcode.Width);
                            }
                            receiptSize.Height += bgMap.Height + 30; //bit of a space
                            receiptSize.Width = Math.Max(receiptSize.Width, bgMap.Width + textSize.Width);
                        }
                    }
                    else
                    {
                        string newLine = line.Replace("^", " ");
                        if (!calculate)
                        {
                            if (line.Contains("^"))
                            {
                                
                                Font boldFont = new Font(font.Name, 16, FontStyle.Bold);
                                newLine = newLine.Substring(1);
                                float charSize = ((float)receiptSize.Width / (float)maxCharsPerLine);
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
                    }
                }
            }

            return receiptSize;
        }

        private Image DrawReceipt(string[] lines, string documentNumber, string docType)
        {
            Font font = new Font("Courier New", 12);

            //calculate the size first using 1x1 bitmap
            Image img = new Bitmap(1, 1);
            Graphics gr = Graphics.FromImage(img);
            SizeF imgSize = DrawReceiptOnGraphics(gr, documentNumber, font, lines, true,docType);

            //now that we have it, make real image and draw to it
            img = new Bitmap((int)imgSize.Width, (int)imgSize.Height);
            gr = Graphics.FromImage(img);
            DrawReceiptOnGraphics(gr, documentNumber, font, lines, false,docType);

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
