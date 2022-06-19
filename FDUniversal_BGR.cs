using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TestSDK
{
    public enum FiscalDeviceGroup
    {
        DeviceGroup_A,
        DeviceGroup_B,
        DeviceGroup_C
    }

    public class FiscalPrinterDetect : FiscalPrinter
    {
        public FiscalPrinterDetect(FiscalComm comm) : base(comm, FiscalPrinterProtocol.AutoDetect, 1251)
        {

        }
        public override void Initialize_StatusBytes()
        {
            //
        }

        public override void Set_AllsBytesBitsState()
        {
            //
        }

    }
    public class FDUniversal_BGR
    {
        private FDGROUP_A_BGR fiscal_A;
        private FDGROUP_B_BGR fiscal_B;
        private FDGROUP_C_BGR fiscal_C;
        private FiscalPrinterDetect fd;
        private FiscalComm comm;

        public FiscalPrinterProtocol fiscalDeviceProtocol;

        public FDUniversal_BGR(FiscalComm comm)
        {
            this.comm = comm;
            fd = new FiscalPrinterDetect(comm);
        }

        public void Calculate_DeviceGroup()
        {
            if (deviceModel == "FP-650" || deviceModel == "FP-2000" || deviceModel == "FP-800" || deviceModel == "SK1-21F" || deviceModel == "SK1-31F"//
                || deviceModel == "FP-700" || deviceModel == "FMP-10") grp = FiscalDeviceGroup.DeviceGroup_A;
            if (deviceModel == "DP-05" || deviceModel == "DP-15" || deviceModel == "DP-25" || deviceModel == "DP-35" || deviceModel == "DP-150"//
                || deviceModel == "WP-50") grp = FiscalDeviceGroup.DeviceGroup_B;
            if (deviceModel == "WP-50X" || deviceModel == "DP-25X" || deviceModel == "DP-150X" || deviceModel == "WP-500X" || deviceModel == "DP-05C"//
                || deviceModel == "FP-700X" || deviceModel == "FMP-55X" || deviceModel == "FMP-350X") grp = FiscalDeviceGroup.DeviceGroup_C;
        }

        public FiscalDeviceGroup grp;

        public FiscalDeviceGroup device_Group
        {
            get
            {
                return this.grp;

            }
            set
            {
                Calculate_DeviceGroup();
            }
        }
        public bool device_Connected
        {
            get
            {
                return Get_DeviceConnected();
            }
            set
            {
                fd.device_Connected = value;
            }
        }

        public TLanguage language
        {
            get
            {
                return fd.language;
            }
            set
            {
                fd.language = value;
            }
        }

        public string deviceModel = "";

        public bool Get_DeviceConnected()
        {
            if (fiscal_A != null && fiscal_A.device_Connected) return true;
            if (fiscal_B != null && fiscal_B.device_Connected) return true;
            if (fiscal_C != null && fiscal_C.device_Connected) return true;
            return false;
        }

        public bool Get_SBit_State(int byteIndex, int bitIndex)
        {
            if (fiscal_A != null) return fiscal_A.Get_SBit_State(byteIndex, bitIndex);
            else if (fiscal_B != null) return fiscal_B.Get_SBit_State(byteIndex, bitIndex);
            else return fiscal_C.Get_SBit_State(byteIndex, bitIndex);
        }

        public string Get_SBit_Description(int byteIndex, int bitIndex)
        {
            if (fiscal_A != null) return fiscal_A.Get_SBit_Description(byteIndex, bitIndex);
            else if (fiscal_B != null) return fiscal_B.Get_SBit_Description(byteIndex, bitIndex);
            else return fiscal_C.Get_sBit_Description(byteIndex, bitIndex);
        }

        public bool Get_SBit_ErrorChecking(int byteIndex, int bitIndex)
        {
            if (fiscal_A != null) return fiscal_A.Get_SBit_ErrorChecking(byteIndex, bitIndex);
            else if (fiscal_B != null) return fiscal_B.Get_SBit_ErrorChecking(byteIndex, bitIndex);
            else return fiscal_C.Get_SBit_ErrorChecking(byteIndex, bitIndex);
        }

        public string Get_DiagnosticInfo()
        {

            if (fiscal_A != null)
            {
                var result = fiscal_A.info_Get_DiagnosticInfo();
                return result["serialNumber"];
            }
            else if (fiscal_B != null)
            {
                var result = fiscal_B.info_Get_DiagnosticInfo();
                return result["serialNumber"];
            }
            else
            {
                var result = fiscal_C.info_Get_DiagnosticInfo("");
                return result["serialNumber"];
            }
        }

        public string GetErrorMessage(string errCode)
        {
            return fd.GetErrorMessage(errCode);
        }

        public void Initialize_StatusBytes()
        {
            //fd.Initialize_StatusBytes();
            if (fiscal_A != null) fiscal_A.Initialize_StatusBytes();
            if (fiscal_B != null) fiscal_B.Initialize_StatusBytes();
            if (fiscal_C != null) fiscal_C.Initialize_StatusBytes();
        }

        public void SetStatusBits_Descriptions()
        {
            if (fiscal_A != null) fiscal_A.SetStatusBits_Descriptions();
            if (fiscal_B != null) fiscal_B.SetStatusBits_Descriptions();
            if (fiscal_C != null) fiscal_C.SetStatusBits_Descriptions();
        }

        public void Disconnect()
        {
            SetPropertiesToBlank();
            if (fiscal_A != null && fiscal_A.device_Connected) fiscal_A.Disconnect();
            if (fiscal_B != null && fiscal_B.device_Connected) fiscal_B.Disconnect();
            if (fiscal_C != null && fiscal_C.device_Connected) fiscal_C.Disconnect();
        }

        public void Connect()
        {

            fd.Connect();
            deviceModel = fd.deviceModel;
            fiscalDeviceProtocol = fd.fdProtocol;
            if (fiscalDeviceProtocol == FiscalPrinterProtocol.Extended)
            {
                fiscal_C = new FDGROUP_C_BGR(comm);
                if (fd.device_Connected) fd.Disconnect();
                fiscal_C.Connect();
                SetPropertiesToBlank();
            }
            else
            {
                if (fd.deviceModel == "FP-650" || fd.deviceModel == "FP-2000" || fd.deviceModel == "FP-800" || fd.deviceModel == "SK1-21F" || fd.deviceModel == "SK1-31F"//
               || fd.deviceModel == "FP-700" || fd.deviceModel == "FMP-10")
                {
                    fiscal_A = new FDGROUP_A_BGR(comm);
                    if (fd.device_Connected) fd.Disconnect();
                    fiscal_A.Connect();
                    SetPropertiesToBlank();
                }

                if (fd.deviceModel == "DP-05" || fd.deviceModel == "DP-15" || fd.deviceModel == "DP-25" || fd.deviceModel == "DP-35" || fd.deviceModel == "DP-150"//
                || fd.deviceModel == "WP-50")
                {
                    fiscal_B = new FDGROUP_B_BGR(comm);
                    if (fd.device_Connected) fd.Disconnect();
                    fiscal_B.Connect();
                    SetPropertiesToBlank();
                }
            }
        }


        public int opNum = 1, tillNum = 1, stornoDocNum = 0;
        public long invNum = 0;
        public string stornoT = "", stornoDT = "", stornoFMNum = "", stornoUNP_ = "", inv = "", stornoRsn = "", unp = "", opPass = "", paidM = "", amountS = "";
        public bool eSBitGeneralType1, eSBitGeneralType2, eSBitCommandSyntaxError, eSBitCommandInvalidCode, eSBitCommandOverflow;
        public bool eSBitFullEJournal, eSBitFullFiscalMemory, eSBitOutOfPaper, eSBitCommandNotPermited, eSBitWriteError, iSBitOpenedReceiptFiscal, iSBitOpenedReceiptNonFiscal;
        public bool iSBitNearlyFullEJournal, iSBitNearlyFullFiscalMemory, iSBitSettedSerialNumber, iSBitSettedTaxNumber, iSBitSettedTaxRates, iSBitFormattedFiscalMemory;
        public bool iSBitDeviceIsFiscalized, canOpenStorno;
        public string txtRow1 = "", txtRow2 = "", taxGr = "", singlePr = "", quan = "", perc = "", abss = "", meas = "", depart = "", discType = "", discValue = "";
        public string ppMode = "", currencyT = "", unt = "", eik = "", eikType = "", seller = "", receiver = "", client = "", taxN = "", address1 = "", address2 = "", amountT = "";
        public string dt = "";

        public bool CanOpenStorno
        {
            get
            {
                return Check_Can_OpenStorno();
            }
            set
            {
                this.canOpenStorno = value;
            }
        }

        public void SetPropertiesToBlank()
        {

            operatorPassword = "";
            stornoFMNumber = "";
            stornoDateTime = "";
            stornoUNP = "";
            UNP = "";
            textRow1 = "";
            textRow2 = "";
            taxGroup = "";
            singlePrice = "";
            quantity = "";
            percent = "";
            abs = "";
            measure = "";
            if(fiscal_C != null) department = "0";
            else department = "";
            discountType = "";
            discountValue = "";
            paidM = "";
            amountS = "";
            ppMode = "";
            currencyT = "";
            unt = "";
            eik = "";
            eikType = "";
            seller = "";
            receiver = "";
            client = "";
            taxN = "";
            address1 = "";
            address2 = "";
            amountT = "";
            dt = "";
        }

        public string department
        {
            get
            {
                return this.depart;
            }
            set
            {
                this.depart = value;
            }
        }

        public string dateTime
        {
            get
            {
                return this.dt;
            }
            set
            {
                this.dt = value;
            }
        }

        public string measure
        {
            get
            {
                return this.meas;
            }
            set
            {
                this.meas = value;
            }
        }

        public string percent
        {
            get
            {
                return this.perc;
            }
            set
            {
                this.perc = value;
            }
        }

        public string abs
        {
            get
            {
                return this.abss;
            }
            set
            {
                this.abss = value;
            }
        }

        public string textRow1
        {
            get
            {
                return this.txtRow1;
            }
            set
            {
                this.txtRow1 = value;
            }
        }

        public string textRow2
        {
            get
            {
                return this.txtRow2;
            }
            set
            {
                this.txtRow2 = value;
            }
        }

        public string taxGroup
        {
            get
            {
                return this.taxGr;
            }
            set
            {
                this.taxGr = value;
            }
        }

        public string singlePrice
        {
            get
            {
                return this.singlePr;
            }
            set
            {
                this.singlePr = value;
            }
        }

        public string quantity
        {
            get
            {
                return this.quan;
            }
            set
            {
                this.quan = value;
            }
        }

        public string discountType
        {
            get
            {
                return this.discType;
            }
            set
            {
                this.discType = value;
            }
        }

        public string discountValue
        {
            get
            {
                return this.discValue;
            }
            set
            {
                this.discValue = value;
            }
        }

        public int operatorNumber
        {
            get
            {
                return this.opNum;
            }
            set
            {
                this.opNum = value;
            }
        }

        public string operatorPassword
        {
            get
            {
                return this.opPass;
            }
            set
            { if (value == "")
                {
                    if (device_Group == FiscalDeviceGroup.DeviceGroup_A) opPass = "0000";
                    else opPass = "1";
                }
                else this.opPass = value;
            }
        }

        public int tillNumber
        {
            get
            {
                return this.tillNum;
            }
            set
            {
                this.tillNum = value;
            }
        }

        public string stornoType
        {
            get
            {
                return this.stornoT;
            }
            set
            {
                this.stornoT = value;
            }
        }

        public int stornoDocumentNumber
        {
            get
            {
                return this.stornoDocNum;
            }
            set
            {
                this.stornoDocNum = value;
            }
        }

        public string stornoDateTime
        {
            get
            {
                return this.stornoDT;
            }
            set
            {
                this.stornoDT = value;
            }
        }

        public string stornoFMNumber
        {
            get
            {
                return this.stornoFMNum;
            }
            set
            {
                this.stornoFMNum = value;
            }
        }

        public string stornoUNP
        {
            get
            {
                return this.stornoUNP_;
            }
            set
            {
                this.stornoUNP_ = value;
            }
        }

        public string invoice
        {
            get
            {
                return this.inv;
            }
            set
            {
                this.inv = value;
            }
        }

        public long invoiceNumber
        {
            get
            {
                return this.invNum;
            }
            set
            {
                this.invNum = value;
            }
        }

        public string stornoReason
        {
            get
            {
                return this.stornoRsn;
            }
            set

            {
                this.stornoRsn = value;
            }
        }

        public string UNP
        {
            get
            {
                return this.unp;
            }
            set
            {
                this.unp = value;
            }
        }

        public string paidMode
        {
            get
            {
                return paidM;
            }
            set
            {
                this.paidM = value;
            }
        }

        public string amount
        {
            get
            {
                return amountS;
            }
            set
            {
                this.amountS = value;
            }
        }

        public string ppPaidMode
        {
            get
            {
                return ppMode;
            }
            set
            {
                this.ppMode = value;
            }
        }

        public string currencyType
        {
            get
            {
                return currencyT;
            }
            set
            {
                this.currencyT = value;
            }
        }

        public string unit
        {
            get
            {
                return unt;
            }
            set
            {
                this.unt = value;
            }
        }

        public string eIK
        {
            get
            {
                return eik;
            }
            set
            {
                this.eik = value;
            }
        }

        public string eIK_Type
        {
            get
            {
                return eikType;
            }
            set
            {
                this.eikType = value;
            }
        }

        public string Seller
        {
            get
            {
                return seller;
            }
            set
            {
                this.seller = value;
            }
        }

        public string Receiver
        {
            get
            {
                return receiver;
            }
            set
            {
                this.receiver = value;
            }
        }

        public string Client
        {
            get
            {
                return client;
            }
            set
            {
                this.client = value;
            }
        }

        public string TaxNumber
        {
            get
            {
                return taxN;
            }
            set
            {
                this.taxN = value;
            }
        }

        public string Address1
        {
            get
            {
                return address1;
            }
            set
            {
                this.address1 = value;
            }
        }

        public string Address2
        {
            get
            {
                return address2;
            }
            set
            {
                this.address2 = value;
            }
        }

        public string AmountType
        {
            get
            {
                return amountT;
            }

            set
            {
                this.amountT = value;
            }
        }

        public bool Get_eSBit_GeneralType_1()
        {
            if (fiscal_A != null) return fiscal_A.eSBit_GeneralError_Sharp;
            if (fiscal_B != null) return fiscal_B.eSBit_GeneralError_Sharp;
            if (fiscal_C != null) return fiscal_C.eSBit_GeneralError_Sharp;
            return false;
        }

        public bool Get_eSBit_GeneralType_2()
        {
            if (fiscal_A != null) return fiscal_A.eSBit_GeneralError_Star;
            if (fiscal_B != null) return fiscal_B.eSBit_GeneralError_Star;
            if (fiscal_C != null) return fiscal_C.eSBit_GeneralError_Star;
            return false;
        }

        public bool Get_eSBit_Command_SyntaxError()
        {
            if (fiscal_A != null) return fiscal_A.eSBit_SyntaxError;
            if (fiscal_B != null) return fiscal_B.eSBit_SyntaxError;
            if (fiscal_C != null) return fiscal_C.eSBit_SyntaxError;
            return false;
        }

        public bool Get_eSBit_Command_InvalidCode()
        {
            if (fiscal_A != null) return fiscal_A.eSBit_CommandCodeIsInvalid;
            if (fiscal_B != null) return fiscal_B.eSBit_CommandCodeIsInvalid;
            if (fiscal_C != null) return fiscal_C.eSBit_CommandCodeIsInvalid;
            return false;
        }

        public bool Get_eSBit_Command_NotPermited()
        {
            if (fiscal_A != null) return fiscal_A.eSBit_CommandNotPermitted;
            if (fiscal_B != null) return fiscal_B.eSBit_CommandNotPermitted;
            if (fiscal_C != null) return fiscal_C.eSBit_CommandNotPermitted;
            return false;
        }

        public bool Get_eSBit_Command_Overflow()
        {
            if (fiscal_A != null) return fiscal_A.eSBit_Overflow;
            if (fiscal_B != null) return fiscal_B.eSBit_Overflow;
            if (fiscal_C != null) return fiscal_C.eSBit_Overflow;
            return false;
        }

        public bool Get_eSBit_Full_EJournal()
        {
            if (fiscal_A != null) return fiscal_A.eSBit_EJIsFull;
            if (fiscal_B != null) return fiscal_B.eSBit_EJIsFull;
            if (fiscal_C != null) return fiscal_C.eSBit_EJIsFull;
            return false;
        }

        public bool Get_eSBit_Full_FiscalMemory()
        {
            if (fiscal_A != null) return fiscal_A.eSBit_FM_Full;
            if (fiscal_B != null) return fiscal_B.eSBit_FM_Full;
            if (fiscal_C != null) return fiscal_C.eSBit_FM_Full;
            return false;
        }

        public bool Get_eSBit_OutOf_Paper()
        {
            if (fiscal_A != null) return fiscal_A.eSBit_EndOfPaper;
            if (fiscal_B != null) return fiscal_B.eSBit_EndOfPaper;
            if (fiscal_C != null) return fiscal_C.eSBit_EndOfPaper;
            return false;
        }


        public bool Get_eSBit_WriteError()
        {
            if (fiscal_A != null) return fiscal_A.eSBit_FM_NotAccess;
            if (fiscal_B != null) return fiscal_B.eSBit_FM_NotAccess;
            if (fiscal_C != null) return fiscal_C.eSBit_FM_NotAccess;
            return false;
        }

        public bool Get_iSBit_OpenedReceipt_Fiscal()
        {
            if (fiscal_A != null) return fiscal_A.iSBit_Receipt_Fiscal;
            if (fiscal_B != null) return fiscal_B.iSBit_Receipt_Fiscal;
            if (fiscal_C != null) return fiscal_C.iSBit_Receipt_Fiscal;
            return false;
        }

        public bool Get_iSBit_OpenedReceipt_NonFiscal()
        {
            if (fiscal_A != null) return fiscal_A.iSBit_Receipt_Nonfiscal;
            if (fiscal_B != null) return fiscal_B.iSBit_Receipt_Nonfiscal;
            if (fiscal_C != null) return fiscal_C.iSBit_Receipt_Nonfiscal;
            return false;
        }

        public bool Get_iSBit_NearlyFull_EJournal()
        {
            if (fiscal_A != null) return fiscal_A.iSBit_EJ_NearlyFull;
            if (fiscal_B != null) return fiscal_B.iSBit_EJ_NearlyFull;
            if (fiscal_C != null) return fiscal_C.iSBit_EJ_NearlyFull;
            return false;
        }

        public bool Get_iSBit_NearlyFull_FiscalMemory()
        {
            if (fiscal_A != null) return fiscal_A.iSBit_LessThan_50_Reports;
            if (fiscal_B != null) return fiscal_B.iSBit_LessThan_50_Reports;
            if (fiscal_C != null) return fiscal_C.iSBit_LessThan_60_Reports;
            return false;
        }

        public bool Get_iSBit_Setted_SerialNumber()
        {
            if (fiscal_A != null) return fiscal_A.iSBit_Number_SFM_Set;
            if (fiscal_B != null) return fiscal_B.iSBit_Number_SFM_Set;
            if (fiscal_C != null) return fiscal_C.iSBit_Number_SFM_Set;
            return false;
        }

        public bool Get_iSBit_Setted_TaxNumber()
        {
            if (fiscal_A != null) return fiscal_A.iSBit_Number_Tax_Set;
            if (fiscal_B != null) return fiscal_B.iSBit_Number_Tax_Set;
            if (fiscal_C != null) return fiscal_C.iSBit_Number_Tax_Set;
            return false;
        }

        public bool Get_iSBit_Setted_TaxRates()
        {
            if (fiscal_A != null) return fiscal_A.iSBit_VAT_Set;
            if (fiscal_B != null) return fiscal_B.iSBit_VAT_Set;
            if (fiscal_C != null) return fiscal_C.iSBit_VAT_Set;
            return false;
        }


        public bool Get_iSBit_Formatted_FiscalMemory()
        {
            if (fiscal_A != null) return fiscal_A.iSBit_FM_formatted;
            if (fiscal_B != null) return fiscal_B.iSBit_FM_formatted;
            if (fiscal_C != null) return fiscal_C.iSBit_FM_formatted;
            return false;
        }

        public bool Get_iSBit_DeviceIsFiscalized()
        {
            if (fiscal_A != null) return fiscal_A.iSBit_Device_Fiscalized;
            if (fiscal_B != null) return fiscal_B.iSBit_Device_Fiscalized;
            if (fiscal_C != null) return fiscal_C.iSBit_Device_Fiscalized;
            return false;
        }

        public bool eSBit_GeneralType_1
        {
            get
            {
                return Get_eSBit_GeneralType_1();
            }
            set
            {
                this.eSBitGeneralType1 = value;
            }

        }

        public bool eSBit_GeneralType_2
        {
            get
            {
                return Get_eSBit_GeneralType_2();
            }
            set
            {
                this.eSBitGeneralType2 = value;
            }

        }

        public bool eSBit_Command_SyntaxError
        {
            get
            {
                return Get_eSBit_Command_SyntaxError();
            }
            set
            {
                this.eSBitCommandSyntaxError = value;
            }

        }

        public bool eSBit_Command_InvalidCode
        {
            get
            {
                return Get_eSBit_Command_InvalidCode();
            }
            set
            {
                this.eSBitCommandInvalidCode = value;
            }

        }

        public bool eSBit_Command_NotPermited
        {
            get
            {
                return Get_eSBit_Command_NotPermited();
            }
            set
            {
                this.eSBitCommandNotPermited = value;
            }

        }

        public bool eSBit_Command_Overflow
        {
            get
            {
                return Get_eSBit_Command_Overflow();
            }
            set
            {
                this.eSBitCommandOverflow = value;
            }

        }

        public bool eSBit_Full_EJournal
        {
            get
            {
                return Get_eSBit_Full_EJournal();
            }
            set
            {
                this.eSBitFullEJournal = value;
            }

        }

        public bool eSBit_Full_FiscalMemory
        {
            get
            {
                return Get_eSBit_Full_FiscalMemory();
            }
            set
            {
                this.eSBitFullFiscalMemory = value;
            }
        }

        public bool eSBit_OutOf_Paper
        {
            get
            {
                return Get_eSBit_OutOf_Paper();
            }
            set
            {
                this.eSBitOutOfPaper = value;
            }
        }

        public bool eSBit_WriteError
        {
            get
            {
                return Get_eSBit_WriteError();
            }
            set
            {
                this.eSBitWriteError = value;
            }
        }

        public bool iSBit_OpenedReceipt_Fiscal
        {
            get
            {
                return Get_iSBit_OpenedReceipt_Fiscal();
            }
            set
            {
                this.iSBitOpenedReceiptFiscal = value;
            }
        }

        public bool iSBit_OpenedReceipt_NonFiscal
        {
            get
            {
                return Get_iSBit_OpenedReceipt_NonFiscal();
            }
            set
            {
                this.iSBitOpenedReceiptNonFiscal = value;
            }
        }

        public bool iSBit_NearlyFull_EJournal
        {
            get
            {
                return Get_iSBit_NearlyFull_EJournal();
            }
            set
            {
                this.iSBitNearlyFullEJournal = value;
            }
        }

        public bool iSBit_NearlyFull_FiscalMemory
        {
            get
            {
                return Get_iSBit_NearlyFull_FiscalMemory();
            }
            set
            {
                this.iSBitNearlyFullFiscalMemory = value;
            }
        }

        public bool iSBit_Setted_SerialNumber
        {
            get
            {
                return Get_iSBit_Setted_SerialNumber();
            }
            set
            {
                this.iSBitSettedSerialNumber = value;
            }
        }

        public bool iSBit_Setted_TaxNumber
        {
            get
            {
                return Get_iSBit_Setted_TaxNumber();
            }
            set
            {
                this.iSBitSettedTaxNumber = value;
            }
        }

        public bool iSBit_Setted_TaxRates
        {
            get
            {
                return Get_iSBit_Setted_TaxRates();
            }
            set
            {
                this.iSBitSettedTaxRates = value;
            }
        }

        public bool iSBit_Formatted_FiscalMemory
        {
            get
            {
                return Get_iSBit_Formatted_FiscalMemory();
            }
            set
            {
                this.iSBitFormattedFiscalMemory = value;
            }
        }

        public bool iSBit_DeviceIsFiscalized
        {
            get
            {
                return Get_iSBit_DeviceIsFiscalized();
            }
            set
            {
                this.iSBitDeviceIsFiscalized = value;
            }
        }

        private bool ParamBetweenRanges(int a, int number)
        {
            return (a <= number);
        }

        public bool IsItValidDateTime(string dt)
        {
            DateTime dateT;
            string[] formats = { "ddMMyyHHmmss", "ddMMyyHHmm" };
            return DateTime.TryParseExact(dt, formats, null, System.Globalization.DateTimeStyles.None, out dateT);
        }

        public bool IsItValidDateTime_A(string dt)
        {
            DateTime dateT;
            return DateTime.TryParseExact(dt, "ddMMyyHHmmss", null, System.Globalization.DateTimeStyles.None, out dateT);
        }

        public bool Check_Can_OpenStorno()
        {
            bool tmpVar = false;
            bool isOpCode_Valid;
            if (fiscal_A != null)
            {
                if (operatorNumber.ToString().Length == 2) isOpCode_Valid = Regex.IsMatch(operatorNumber.ToString(), @"[1][0-6]");
                else isOpCode_Valid = Regex.IsMatch(operatorNumber.ToString(), @"[1-9]");
                tmpVar = (operatorNumber.ToString() != "") && (operatorNumber.ToString().Length < 3) && isOpCode_Valid && ParamBetweenRanges(int.Parse(operatorNumber.ToString()), 30);
                tmpVar = tmpVar && (operatorPassword != "") && (operatorPassword.Length > 3) && (operatorPassword.Length < 9);
                tmpVar = tmpVar && (tillNumber.ToString() != "") && (tillNumber.ToString().Length < 6);
                tmpVar = tmpVar && (stornoDocumentNumber > 0) && (stornoDocumentNumber <= 9999999);
                tmpVar = tmpVar && (!fiscal_A.iSBit_Receipt_Fiscal);
                tmpVar = tmpVar && (!fiscal_A.iSBit_Receipt_Nonfiscal);
                if (stornoDateTime != "" || stornoFMNumber != "" || stornoUNP != "") tmpVar = tmpVar && stornoFMNumber != "" && stornoUNP != "" && stornoDateTime != "" && stornoDateTime.Length == 12 && IsItValidDateTime_A(stornoDateTime)//
                          && (stornoFMNumber.Length == 8) && Regex.IsMatch(stornoUNP, @"[A-Z][A-Z][0-9][0-9][0-9][0-9][0-9][0-9]\-[a-zA-Z0-9][a-zA-Z0-9][a-zA-Z0-9][a-zA-Z0-9]\-[0-9][0-9][0-9][0-9][0-9][0-9][0-9]") && Regex.IsMatch(stornoFMNumber, @"[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]");
                if (invoice == "I") tmpVar = tmpVar && (invoiceNumber != 0) && (invoiceNumber <= 9999999999);
                if (stornoReason != "") tmpVar = tmpVar && stornoReason.Length < 31;
                if (UNP != "") tmpVar = tmpVar && Regex.IsMatch(UNP, @"[A-Z][A-Z][0-9][0-9][0-9][0-9][0-9][0-9]\-[a-zA-Z0-9][a-zA-Z0-9][a-zA-Z0-9][a-zA-Z0-9]\-[0-9][0-9][0-9][0-9][0-9][0-9][0-9]");
            }

            if (fiscal_B != null)
            {
                if (operatorNumber.ToString().Length == 2) isOpCode_Valid = Regex.IsMatch(operatorNumber.ToString(), @"[1][0-6]");
                else isOpCode_Valid = Regex.IsMatch(operatorNumber.ToString(), @"[1-9]");
                tmpVar = (operatorNumber.ToString() != "") && (operatorNumber.ToString().Length < 3) && isOpCode_Valid;
                tmpVar = tmpVar && ((operatorPassword != "") && (operatorPassword.Length < 9));
                tmpVar = tmpVar && ((tillNumber.ToString() != "") && (tillNumber.ToString().Length < 6));
                tmpVar = tmpVar && (stornoDocumentNumber > 0) && (stornoDocumentNumber <= 9999999);
                tmpVar = tmpVar && (stornoDateTime != "") && stornoDateTime.Length > 9 && stornoDateTime.Length < 13 && IsItValidDateTime(stornoDateTime);
                tmpVar = tmpVar && ((stornoFMNumber != "") && (stornoFMNumber.Length == 8) && Regex.IsMatch(stornoUNP, @"[A-Z][A-Z][0-9][0-9][0-9][0-9][0-9][0-9]\-[a-zA-Z0-9][a-zA-Z0-9][a-zA-Z0-9][a-zA-Z0-9]\-[0-9][0-9][0-9][0-9][0-9][0-9][0-9]")) && Regex.IsMatch(stornoFMNumber, @"[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]");
                tmpVar = tmpVar && (!fiscal_B.iSBit_Receipt_Fiscal);
                tmpVar = tmpVar && (!fiscal_B.iSBit_Receipt_Nonfiscal);
                if (stornoUNP != "") tmpVar = tmpVar && Regex.IsMatch(stornoUNP, @"[A-Z][A-Z][0-9][0-9][0-9][0-9][0-9][0-9]\-[a-zA-Z0-9][a-zA-Z0-9][a-zA-Z0-9][a-zA-Z0-9]\-[0-9][0-9][0-9][0-9][0-9][0-9][0-9]");
                if (invoiceNumber != 0) tmpVar = tmpVar && (invoiceNumber <= 9999999999);
                if (stornoReason != "") tmpVar = tmpVar && stornoReason.Length < 43;
            }

            if (fiscal_C != null)
            {
                if (operatorNumber.ToString().Length == 2) isOpCode_Valid = Regex.IsMatch(operatorNumber.ToString(), @"[1-3][0-9]");
                else isOpCode_Valid = Regex.IsMatch(operatorNumber.ToString(), @"[1-9]");
                tmpVar = (operatorNumber.ToString() != "") && (operatorNumber.ToString().Length < 3) && isOpCode_Valid && ParamBetweenRanges(int.Parse(operatorNumber.ToString()), 30);
                tmpVar = tmpVar && (operatorPassword != "") && (operatorPassword.Length < 9);
                tmpVar = tmpVar && (tillNumber.ToString() != "") && (tillNumber.ToString().Length < 6);
                tmpVar = tmpVar && (stornoDocumentNumber > 0) && (stornoDocumentNumber <= 9999999);
                tmpVar = tmpVar && (stornoDateTime != "") && (stornoDateTime.Length < 22) && (stornoDateTime.Length > 16) && IsItValidDateTime(stornoDateTime);
                tmpVar = tmpVar && (stornoFMNumber != "") && (stornoFMNumber.Length == 8) && Regex.IsMatch(stornoFMNumber, @"[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]");
                tmpVar = tmpVar && (!fiscal_C.iSBit_Receipt_Fiscal);
                tmpVar = tmpVar && (!fiscal_C.iSBit_Receipt_Nonfiscal);
                if (stornoUNP != "") tmpVar = tmpVar && Regex.IsMatch(stornoUNP, @"[A-Z][A-Z][0-9][0-9][0-9][0-9][0-9][0-9]\-[a-zA-Z0-9][a-zA-Z0-9][a-zA-Z0-9][a-zA-Z0-9]\-[0-9][0-9][0-9][0-9][0-9][0-9][0-9]");
                if (invoiceNumber != 0) tmpVar = tmpVar && (invoiceNumber <= 9999999999);
                if (stornoReason != "") tmpVar = tmpVar && stornoReason.Length < 43;
            }
            return tmpVar;
        }

        public void Get_CashIn_CashOut(ref string errorParam, ref string cashSum, ref string cashIn, ref string cashOut)
        {
            if (fiscal_A != null)
            {
                var answer = fiscal_A.info_Get_CashIn_CashOut(); // Get If there is enough service money to make storno
                errorParam = answer["errorCode"];
                cashSum = answer["cashSum"];
                cashIn = answer["servIn"];
                cashOut = answer["servOut"];
            }

            if (fiscal_B != null)
            {
                var answer = fiscal_B.info_Get_CashIn_CashOut(); // Get If there is enough service money to make storno
                errorParam = answer["errorCode"];
                cashSum = answer["cashSum"];
                cashIn = answer["servIn"];
                cashOut = answer["servOut"];
            }

            if (fiscal_C != null)
            {
                var answer = fiscal_C.info_Get_CashIn_CashOut(" ", " "); // Get If there is enough service money to make storno
                errorParam = answer["errorCode"];
                cashSum = answer["cashSum"];
                cashIn = answer["servIn"];
                cashOut = answer["servOut"];
            }
        }

        public void Get_InvoiceRange(ref string startVal, ref string endVal, ref string currentVal)
        {
            try
            {
                if (fiscal_A != null)
                {
                    var answer = fiscal_A.info_Get_InvoiceRange();
                    if (answer != null && answer["valueStart"] != "" && answer["valueEnd"] != "" && answer["valueCurrent"] != "")
                    {
                        startVal = answer["valueStart"];
                        endVal = answer["valueEnd"];
                        currentVal = answer["valueCurrent"];
                    }
                }
                if (fiscal_B != null)
                {
                    var answ = fiscal_B.info_Get_InvoiceRange();
                    if (answ != null && answ["valueStart"] != "" && answ["valueEnd"] != "" && answ["valueCurrent"] != "")
                    {
                        startVal = answ["valueStart"];
                        endVal = answ["valueEnd"];
                        currentVal = answ["valueCurrent"];
                    }
                }
                if (fiscal_C != null)
                {
                    var answr = fiscal_C.info_Get_InvoiceRange();
                    if (answr != null && answr["valueStart"] != "" && answr["valueEnd"] != "" && answr["valueCurrent"] != "")
                    {
                        startVal = answr["valueStart"];
                        endVal = answr["valueEnd"];
                        currentVal = answr["valueCurrent"];
                    }
                }
            }
            catch (Exception s)
            {
                throw s;
            }
        }

        public void Set_InvoiceRange(string startInterval, string endInterval)
        {
            try
            {
                if (fiscal_A != null)
                {
                    fiscal_A.config_Set_InvoiceRange(startInterval, endInterval);
                }
                if (fiscal_B != null)
                {
                    fiscal_B.config_Set_InvoiceRange(startInterval, endInterval);
                }
                if (fiscal_C != null)
                {
                    fiscal_C.config_Set_InvoiceRange(startInterval, endInterval);
                }
            }
            catch (Exception s)
            {
                throw s;
            }
        }


        public void Cancel_FiscalReceipt()
        {
            try
            {
                if (fiscal_A != null)
                {
                    fiscal_A.receipt_Fiscal_Cancel();
                }
                if (fiscal_B != null)
                {
                    fiscal_B.receipt_Fiscal_Cancel();
                }
                if (fiscal_C != null)
                {
                    fiscal_C.receipt_Fiscal_Cancel();
                }
            }
            catch (Exception s)
            {
                throw s;
            }
        }

        public void execute_Total()
        {
            if (fiscal_A != null) fiscal_A.execute_Total(textRow1, textRow2, paidMode, amount);
            if (fiscal_B != null) fiscal_B.execute_Total(textRow1, textRow2, paidMode, amount);
            if (fiscal_C != null) fiscal_C.execute_Total(paidMode, amount, ppPaidMode, currencyType);
        }

        public void execute_Sale()
        {
            if (fiscal_A != null) fiscal_A.execute_Sale(textRow1, textRow2, department, taxGroup, singlePrice, quantity, measure, percent, abs);
            if (fiscal_B != null) fiscal_B.execute_Sale(textRow1, textRow2, department, taxGroup, singlePrice, quantity, measure, percent, abs);
            if (fiscal_C != null) fiscal_C.execute_Sale(textRow1, department, taxGroup, singlePrice, quantity, measure, discountType, discountValue);
        }

        public void Open_Fiscal()
        {
            if (fiscal_A != null) fiscal_A.open_FiscalReceipt(operatorNumber.ToString(), operatorPassword.ToString(), UNP, tillNumber.ToString(), invoice);
            if (fiscal_B != null) fiscal_B.open_FiscalReceipt(operatorNumber.ToString(), operatorPassword.ToString(), UNP, tillNumber.ToString(), invoice);
            if (fiscal_C != null) fiscal_C.open_FiscalReceipt(operatorNumber.ToString(), operatorPassword.ToString(), UNP, tillNumber.ToString(), invoice);
        }

        public void Print_Z_Report()
        {
            if (fiscal_A != null) fiscal_A.report_DailyClosure_01("0");
            if (fiscal_B != null) fiscal_B.report_DailyClosure_01("0");
            if (fiscal_C != null) fiscal_C.report_DailyClosure_01("Z");
        }

        public void Print_X_Report()
        {
            if (fiscal_A != null) fiscal_A.report_DailyClosure_01("2");
            if (fiscal_B != null) fiscal_B.report_DailyClosure_01("2");
            if (fiscal_C != null) fiscal_C.report_DailyClosure_01("X");
        }

        public void Set_CashInOut()
        {
            if (fiscal_A != null) fiscal_A.receipt_CashIn_CashOut(amount);
            if (fiscal_B != null) fiscal_B.receipt_CashIn_CashOut(amount);
            if (fiscal_C != null) fiscal_C.receipt_CashIn_CashOut(AmountType,amount);
        }

        public void Close_Fiscal()
        {
            if (fiscal_A != null) fiscal_A.receipt_Fiscal_Close();
            if (fiscal_B != null) fiscal_B.receipt_Fiscal_Close();
            if (fiscal_C != null) fiscal_C.receipt_Fiscal_Close();
        }

        public void Print_ClientInfo()
        {
            if (fiscal_A != null) fiscal_A.receipt_PrintClientInfo(eIK,eIK_Type,Seller,Receiver,Client,TaxNumber,Address1,Address2);
            if (fiscal_B != null) fiscal_B.receipt_PrintClientInfo(eIK, eIK_Type, Seller, Receiver, Client, TaxNumber, Address1, Address2);
            if (fiscal_C != null) fiscal_C.receipt_PrintClientInfo_15( Seller, Receiver, Client, Address1, Address2, eIK_Type,eIK,TaxNumber); 
        }

        public void Get_CurrentRecieptInfo(ref string param1,ref string param2, ref string param3, ref string param4, ref string param5, ref string param6//
            , ref string param7, ref string param8, ref string param9, ref string param10, ref string param11, ref string param12,ref string param13)
        {
            if (fiscal_A != null)
            {
                var result = fiscal_A.receipt_Current_Info();
                param1 = result["canVd"];
                param2 = result["taxA"];
                param3 = result["taxB"];
                param4 = result["taxC"];
                param5 = result["taxD"];
                param6 = result["taxE"];
                param7 = result["taxF"];
                param8 = result["taxG"];
                param9 = result["taxH"];
                param10 = result["inv"];
                param11 = result["invNumber"];
             }

            if (fiscal_B != null)
            {
                var result = fiscal_B.receipt_Current_Info();
                param1 = result["errorCode"];
                param2 = result["canVd"];
                param3 =result["taxA"];
                param4 =result["taxB"];
                param5 =result["taxC"];
                param6 =result["taxD"];
                param7 =result["taxE"];
                param8 =result["taxF"];
                param9 =result["taxG"];
                param10 =result["taxH"];
                param11 = result["inv"];
                param12 = result["invNumber"];
                if(result.Count > 12) param13 = result["fReceiptType"];
            }

            if (fiscal_C != null)
            {
                var result = fiscal_C.receipt_Current_Info();
                param1 = result["errorCode"];
                param2 = result["taxA"];
                param3 = result["taxB"];
                param4 = result["taxC"];
                param5 = result["taxD"];
                param6 = result["taxE"];
                param7 = result["taxF"];
                param8 = result["taxG"];
                param9 = result["taxH"];
                param10 = result["inv"];
                param11 = result["invNmb"];
                if (result.Values.Count > 11) param12 = result["fStorno"];
            }
        }

        public void Set_DateTime()
        {
            if (fiscal_A != null) fiscal_A.config_Set_DateTime(dateTime);
            if(fiscal_B != null) fiscal_B.config_Set_DateTime(dateTime);
            if (fiscal_C != null)
            {
                DateTime thisTime = DateTime.Now;
                bool isDaylight = TimeZoneInfo.Local.IsDaylightSavingTime(thisTime);
                if (isDaylight) dateTime += " DST";
                fiscal_C.config_Set_DateTime(dateTime);
            }
        } 

        public Dictionary<string, string> Get_DateTime()
        {
            if (fiscal_A != null) return fiscal_A.info_Get_DateTime();
            else if (fiscal_B != null) return fiscal_B.info_Get_DateTime();
            else return fiscal_C.info_Get_DateTime();
        }

        public void Open_Storno(ref bool groupA_isItFullStorno)
        {
            if (fiscal_A != null)
                fiscal_A.open_StornoReceipt(operatorNumber.ToString(), operatorPassword.ToString(), tillNumber.ToString(), invoice, invoiceNumber.ToString(), UNP, stornoType, stornoDocumentNumber.ToString(), stornoUNP,//
                   stornoDateTime, stornoFMNumber, stornoReason, ref groupA_isItFullStorno);
            if (fiscal_B != null) fiscal_B.open_StornoReceipt(operatorNumber.ToString(), operatorPassword.ToString(), stornoUNP, tillNumber.ToString(),  stornoType, stornoDocumentNumber.ToString(), //
                 stornoDateTime, stornoFMNumber, invoice, invoiceNumber.ToString(), stornoReason);
            if (fiscal_C != null) fiscal_C.open_StornoReceipt(operatorNumber.ToString(), operatorPassword.ToString(), stornoUNP, tillNumber.ToString(), stornoType, stornoDocumentNumber.ToString(), //
                 stornoDateTime, stornoFMNumber, invoice, invoiceNumber.ToString(), stornoReason);
        }

        
    }
}
