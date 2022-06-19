using System;
using System.Text;
using System.Threading;

namespace TestSDK
{
    public enum FiscalPrinterProtocol
    {
        Legacy,
        Extended,
        AutoDetect
    }

    //public enum FiscalDeviceGroup
    //{
    //    DeviceGroup_A,
    //    DeviceGroup_B,
    //    DeviceGroup_C
    //}

    public class FiscalException: Exception
    {
        public int ErrorCode = 0;
        public FiscalException(int code, string message)
            :base(message)
        {
            ErrorCode = code;
        }
    }

   
    
    public abstract class FiscalPrinter
    {
        public FiscalPrinterProtocol fdProtocol;
        const int MAX_RETRIES = 3;
        const int MAX_DATA_SIZE = 218;

        // списък с грешките
        private Errors_FiscalDevice err = new Errors_FiscalDevice();

        public TLanguage language
        {
            get
            {
                return err.language;
            }
            set
            {
                err.language = value;
            }
        }


    private bool inDisconnect = false;
        //private bool deviceConnected = false;

        private int mPacketSeq = 0x20;

        private FiscalPrinterProtocol protocol = FiscalPrinterProtocol.Extended;
        private int codePage = 1250;
        private string serialNum = "";
        private string fiscMemNum = "";
        private string userDocType = "0";

        private FiscalComm comm = null;

        public byte[] StatusBytes = null;

       
        bool dev_Connected;
        public bool device_Connected
        {
            get
            {
                return this.dev_Connected;
            }
            set
            {
                this.dev_Connected = value;
            }
        }

        public string userDocumentType
        {
            get
            {
                return this.userDocType;
            }
            set
            {
                this.userDocType = value;
            }
        }

        public string SerialNumber
        {
            get
            {
                return serialNum;
            }
        }

        public string FiscalMemoryNumber
        {
            get
            {
                return fiscMemNum;
            }
        }

        public FiscalPrinter(FiscalComm comm, FiscalPrinterProtocol protocol, int codePage)
        {
            this.comm = comm;
            this.protocol = protocol;
            this.codePage = codePage;
        }

        public string deviceModel = "";
        //public FiscalDeviceGroup grp;


        //public void Calculate_DeviceModel()
        //{
        //    if (deviceModel == "FP-650" || deviceModel == "FP-2000" || deviceModel == "FP-800" || deviceModel == "SK1-21F" || deviceModel == "SK1-31F"//
        //        || deviceModel == "FP-700" || deviceModel == "FMP-10") grp = FiscalDeviceGroup.DeviceGroup_A;
        //    if(deviceModel == "DP-05" || deviceModel == "DP-15" || deviceModel == "DP-25" || deviceModel == "DP-35" || deviceModel == "DP-150"//
        //        || deviceModel == "WP-50") grp = FiscalDeviceGroup.DeviceGroup_B;
        //    if (deviceModel == "WP-50X" || deviceModel == "DP-25X" || deviceModel == "DP-150X" || deviceModel == "WP-500X" || deviceModel == "DP-05C"//
        //        || deviceModel == "FP-700X" || deviceModel == "FMP-55X" || deviceModel == "FMP-350X") grp = FiscalDeviceGroup.DeviceGroup_C;
        //}

        public abstract void Initialize_StatusBytes();
        public abstract void Set_AllsBytesBitsState();

        public string GetErrorMessage(string errCode)
        {
            if (language == TLanguage.English)
            {
                return err.Errors[errCode][0];
            }
            // if(language==TLanguage.Bulgarian)
            else
            {
                return err.Errors[errCode][1];
            }
        }
        public void Connect()
        {
            Utils.Log(@"FP: Connect: START");
            dev_Connected = false;

            while (inDisconnect)
            {
                Thread.Sleep(10);
            }
                comm.Connect();

            
            try
            {
                if (protocol == FiscalPrinterProtocol.AutoDetect)
                {
                    try
                    {
                        protocol = FiscalPrinterProtocol.Legacy;
                        GetStatus();
                    }
                    catch 
                    {
                        protocol = FiscalPrinterProtocol.Extended;
                        GetStatus();
                    }
                }
                 else   GetStatus();
            }catch
            {
                Utils.Log(@"FP: Connect: FAILED");
                Disconnect();
                dev_Connected = false;
                throw;
            }
            dev_Connected = true;
            fdProtocol = this.protocol;
            Utils.Log(@"FP: Connect: END");
            string answ = CustomCommand(90, "");
            string[] rows = answ.Split(new string[] { ",", "\r\n" }, StringSplitOptions.None);
            if (rows.Length != 0)
            {
                deviceModel = rows[0];
                serialNum = rows[4];
                fiscMemNum = rows[5];
            }

           // Calculate_DeviceModel();
            Initialize_StatusBytes();
        }

        public void Disconnect()
        {
            if (inDisconnect)
                return;

            inDisconnect = true;


            Utils.Log(@"FP: Disconnect: START");

            comm.Close();
            Utils.Log(@"FP: Disconnect: END");

            inDisconnect = false;
            dev_Connected = false;
        }

        static int[] cp_1251 = {
    0x0402, 0x80, 0x0403, 0x81, 0x201A, 0x82, 0x0453, 0x83, 0x201E, 0x84, 0x2026, 0x85, 0x2020, 0x86, 0x2021, 0x87, 0x20AC, 0x88, 0x2030, 0x89, 0x0409, 0x8A,
    0x2039, 0x8B, 0x040A, 0x8C, 0x040C, 0x8D, 0x040B, 0x8E, 0x040F, 0x8F, 0x0452, 0x90, 0x2018, 0x91, 0x2019, 0x92, 0x201C, 0x93, 0x201D, 0x94, 0x2022, 0x95, 0x2013, 0x96, 0x2014, 0x97,
    0x2122, 0x99, 0x0459, 0x9A, 0x203A, 0x9B, 0x045A, 0x9C, 0x045C, 0x9D, 0x045B, 0x9E, 0x045F, 0x9F, 0x00A0, 0xA0, 0x040E, 0xA1, 0x045E, 0xA2, 0x0408, 0xA3, 0x00A4, 0xA4, 0x0490, 0xA5,
    0x00A6, 0xA6, 0x00A7, 0xA7, 0x0401, 0xA8, 0x00A9, 0xA9, 0x0404, 0xAA, 0x00AB, 0xAB, 0x00AC, 0xAC, 0x00AD, 0xAD, 0x00AE, 0xAE, 0x0407, 0xAF, 0x00B0, 0xB0, 0x00B1, 0xB1, 0x0406, 0xB2,
    0x0456, 0xB3, 0x0491, 0xB4, 0x00B5, 0xB5, 0x00B6, 0xB6, 0x00B7, 0xB7, 0x0451, 0xB8, 0x2116, 0xB9, 0x0454, 0xBA, 0x00BB, 0xBB, 0x0458, 0xBC, 0x0405, 0xBD, 0x0455, 0xBE, 0x0457, 0xBF,
    0x0410, 0xC0, 0x0411, 0xC1, 0x0412, 0xC2, 0x0413, 0xC3, 0x0414, 0xC4, 0x0415, 0xC5, 0x0416, 0xC6, 0x0417, 0xC7, 0x0418, 0xC8, 0x0419, 0xC9, 0x041A, 0xCA, 0x041B, 0xCB, 0x041C, 0xCC,
    0x041D, 0xCD, 0x041E, 0xCE, 0x041F, 0xCF, 0x0420, 0xD0, 0x0421, 0xD1, 0x0422, 0xD2, 0x0423, 0xD3, 0x0424, 0xD4, 0x0425, 0xD5, 0x0426, 0xD6, 0x0427, 0xD7, 0x0428, 0xD8, 0x0429, 0xD9,
    0x042A, 0xDA, 0x042B, 0xDB, 0x042C, 0xDC, 0x042D, 0xDD, 0x042E, 0xDE, 0x042F, 0xDF, 0x0430, 0xE0, 0x0431, 0xE1, 0x0432, 0xE2, 0x0433, 0xE3, 0x0434, 0xE4, 0x0435, 0xE5, 0x0436, 0xE6,
    0x0437, 0xE7, 0x0438, 0xE8, 0x0439, 0xE9, 0x043A, 0xEA, 0x043B, 0xEB, 0x043C, 0xEC, 0x043D, 0xED, 0x043E, 0xEE, 0x043F, 0xEF, 0x0440, 0xF0, 0x0441, 0xF1, 0x0442, 0xF2, 0x0443, 0xF3,
    0x0444, 0xF4, 0x0445, 0xF5, 0x0446, 0xF6, 0x0447, 0xF7, 0x0448, 0xF8, 0x0449, 0xF9, 0x044A, 0xFA, 0x044B, 0xFB, 0x044C, 0xFC, 0x044D, 0xFD, 0x044E, 0xFE, 0x044F, 0xFF, };

        private byte[] ToAnsi(string str)
        {
            if (str == null)
                return null;

            byte[] data = new byte[str.Length];
            for (int s = 0; s < str.Length; s++)
            {
                var c = str[s];
                data[s] = (byte)c;
                if (c < 0x80)
                    continue;

                if (codePage == 1251)
                {
                    for (int i = 0; i < cp_1251.Length; i += 2)
                        if (cp_1251[i] == c)
                        {
                            data[s] = (byte)cp_1251[i + 1];
                            break;
                        }
                }
                else
                {
                    data[s] = (byte)c;
                }
            }

            return data;
        }

        string ToUnicode(byte[] data)
        {
            StringBuilder sb = new StringBuilder(data.Length);

            for (int s = 0; s < data.Length; s++)
            {
                char c = (char)(data[s] & 0xff);
                if (c < 0x80)
                {
                    sb.Append(c);
                    continue;
                }

                if (codePage == 1251)
                {
                    for (int i = 0; i < cp_1251.Length; i += 2)
                        if (cp_1251[i + 1] == c)
                        {
                            sb.Append((char)cp_1251[i]);
                            break;
                        }
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }


        void WritePacket(int cmd, string arguments)
        {
            uint len = (uint)(arguments != null ? arguments.Length : 0);
            byte[] data = ToAnsi(arguments);

            for (int retry = 0; retry < MAX_RETRIES; retry++)
            {
                byte[] buf = new byte[MAX_DATA_SIZE];
                uint offs = 0;
                int crc = 0;

                if (len > MAX_DATA_SIZE)
                {
                    throw new ArgumentException("Lenght of the packet exceeds the limits!");
                }

                // Set control symbol
                buf[offs++] = 0x01;
                // Set data length
                if (protocol == FiscalPrinterProtocol.Legacy)
                {
                    buf[offs++] = (byte)(0x24 + len);
                }
                if (protocol == FiscalPrinterProtocol.Extended)
                {
                    len += 0x2A;
                    buf[offs++] = (byte)(((len >> 12) & 0xf) + 0x30);
                    buf[offs++] = (byte)(((len >> 8) & 0xf) + 0x30);
                    buf[offs++] = (byte)(((len >> 4) & 0xf) + 0x30);
                    buf[offs++] = (byte)(((len >> 0) & 0xf) + 0x30);
                    len -= 0x2A;

                    //            NSString *lenString=[NSString stringWithFormat:@"%04d",len];
                    //            for(int i=0;i<lenString.length;i++)
                    //                buf[offs++] = (uint8_t) [lenString characterAtIndex:i];
                }
                // Set packet sequence
                buf[offs++] = (byte)mPacketSeq;
                // Set command
                if (protocol == FiscalPrinterProtocol.Legacy)
                {
                    buf[offs++] = (byte)cmd;
                }
                if (protocol == FiscalPrinterProtocol.Extended)
                {
                    buf[offs++] = (byte)(((cmd >> 12) & 0xf) + 0x30);
                    buf[offs++] = (byte)(((cmd >> 8) & 0xf) + 0x30);
                    buf[offs++] = (byte)(((cmd >> 4) & 0xf) + 0x30);
                    buf[offs++] = (byte)(((cmd >> 0) & 0xf) + 0x30);
                    //            NSString *cmdString=[NSString stringWithFormat:@"%04d",cmd];
                    //            for(int i=0;i<cmdString.length;i++)
                    //                buf[offs++] = (uint8_t) [cmdString characterAtIndex:i];
                }
                // Set data
                if (len > 0)
                    Array.Copy(data, 0, buf, offs, len);
                //[self toAnsi:data data:&buf[offs]];
                offs += len;
                // Set control symbol
                buf[offs++] = 0x05;
                // Calculate checksum
                for (int i = 1; i < offs; i++)
                {
                    crc += buf[i] & 0xff;
                }
                // Set checksum
                buf[offs++] = (byte)(((crc >> 12) & 0xf) + 0x30);
                buf[offs++] = (byte)(((crc >> 8) & 0xf) + 0x30);
                buf[offs++] = (byte)(((crc >> 4) & 0xf) + 0x30);
                buf[offs++] = (byte)(((crc >> 0) & 0xf) + 0x30);
                // Set control symbol
                buf[offs++] = 0x03;

                comm.Write(buf, offs);

                // Wait to finish pending command.
                byte b;
                do
                {
                    b = comm.ReadByte();
                } while (b == 0x16);

                if (b == 0x15)
                    continue;
                if (b != 0x01)
                    Utils.Log(@"Invalid data received, expected 0x01, got 0x" + b.ToString("X2"));
                //            ERROR(DT_ETIMEOUT,@"Invalid data received, expected 0x01, got 0x%02x",buf[0]);
                return;
            }
            throw new Exception("Invalid packet checksum!");
        }


        private string ReadPacket()
        {
            byte[] buf = new byte[MAX_DATA_SIZE];
            int b = 0;
            int len = 0;
            int crc = 0;

            // Read data length
            if (protocol == FiscalPrinterProtocol.Legacy)
            {
                b = comm.ReadByte();
                crc += b;
                len = b - 0x2B;
            }
            if (protocol == FiscalPrinterProtocol.Extended)
            {
                b = comm.ReadByte();
                crc += b;
                len |= (b - 0x30) << 12;
                b = comm.ReadByte();
                crc += b;
                len |= (b - 0x30) << 8;
                b = comm.ReadByte();
                crc += b;
                len |= (b - 0x30) << 4;
                b = comm.ReadByte();
                crc += b;
                len |= (b - 0x30);
                len -= 0x33;
            }

            // Sequence
            b = comm.ReadByte();
            crc += b;

            // Command
            if (protocol == FiscalPrinterProtocol.Legacy)
            {
                b = comm.ReadByte();
                crc += b;
            }
            if (protocol == FiscalPrinterProtocol.Extended)
            {
                b = comm.ReadByte();
                crc += b;
                b = comm.ReadByte();
                crc += b;
                b = comm.ReadByte();
                crc += b;
                b = comm.ReadByte();
                crc += b;
            }

            var payload = comm.Read(len);
            for (int i = 0; i < len; i++)
                crc += payload[i] & 0xff;


            b = comm.ReadByte();
            if (b != 0x04)
                throw new Exception("Invalid data received, expected 0x04, got 0x" + b.ToString("X2"));
            crc += b;

            int msbLen = 6;
            if (protocol == FiscalPrinterProtocol.Extended)
            {
                msbLen = 8;
            }

            StatusBytes = comm.Read(msbLen);
            for (int i = 0; i < msbLen; i++)
                crc += StatusBytes[i] & 0xff;

            Set_AllsBytesBitsState();

            b = comm.ReadByte();
            if (b != 0x05)
                throw new Exception("Invalid data received, expected 0x05, got 0x" + b.ToString("X2"));
            crc += b;


            b = comm.ReadByte();
            crc -= (b - 0x30) << 12;
            b = comm.ReadByte();
            crc -= (b - 0x30) << 8;
            b = comm.ReadByte();
            crc -= (b - 0x30) << 4;
            b = comm.ReadByte();
            crc -= (b - 0x30);
            if (crc != 0)
                throw new Exception("Invalid CRC!");


            b = comm.ReadByte();
            if (b != 0x03)
                throw new Exception("Invalid data received, expected 0x03, got 0x" + b.ToString("X2"));

            if (len < 0)
                throw new Exception("Invalid data received, length is " + len);

            return ToUnicode(payload);
        }

        public string CustomCommand(int cmd, string arguments)
        {
            comm.ClearReceive();


            mPacketSeq++;
            if (mPacketSeq > 0x7F)
                mPacketSeq = 0x20;

            Utils.Log(">> FP(" + cmd + "): " + arguments);

            WritePacket(cmd, arguments);
            var r = ReadPacket();
            Utils.Log("<< FP(" + cmd + "): " + r);

            //if ((mSB[0] & (1 << 5)) > 0)
            //    ERROR(DT_EINVALID_CMD,@"Invalid command!");

            return r;
        }

        public bool ItIs_SummerDT_Now()
        {
            return DateTime.Now.IsDaylightSavingTime();
        }

        public byte[] GetStatus()
        {
            CustomCommand(74, null);
            return StatusBytes;
        }

        // private Errors_FiscalDevice err = new Errors_FiscalDevice();

        public Func<string, bool> Y_0 = (param) =>
        {
            return
!string.IsNullOrEmpty(param) && !param.Equals("0");
        };
        public Func<string, bool> N_0 = (param) =>
        {
            return
string.IsNullOrEmpty(param) || param.Equals("0");
        };

        public Func<string, bool> Y = (param) =>
        {
            return !string.IsNullOrEmpty(param);
        };

        public Func<string, bool> N = (param) =>
        {
            return
string.IsNullOrEmpty(param);
        };


    }
}
