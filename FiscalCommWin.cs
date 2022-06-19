using System;
using System.IO.Ports;
using System.Threading;

namespace TestSDK
{
    public class FiscalCommWin : FiscalComm
    {
        private SerialPort com = null;
        private string port;
        private int baudRate;

        public FiscalCommWin(string port, int baudRate)
            : base()
        {
            this.port = port;
            this.baudRate = baudRate;
        }

        public override void Connect()
        {
            Close();
            com = new SerialPort(port, baudRate, Parity.None);
            com.Open();
        }

        public override void Close()
        {
            if (com != null)
                com.Close();
            com = null;
        }

        private byte[] readBuf = new byte[1024];

        public override byte[] Read(int maxLength, int timeout)
        {

            int read = 0;
            int len = 0;
            byte[] r = new byte[maxLength];
            com.ReadTimeout = timeout;

            while (read < maxLength)
            {
                int toRead = maxLength - read;
                len = com.Read(readBuf, 0, toRead);
                Array.Copy(readBuf, 0, r, read, len);
                read += len;
            }
        
            return r;
        }

    public override void Write(byte[] data, uint len, int timeout)
        {
            com.WriteTimeout = timeout;
            com.Write(data, 0, (int)len);
        }

        public override void ClearReceive()
        {
            com.ReadTimeout = 50;

            while (true)
            {
                try
                {
                    com.ReadByte();
                    continue;
                }catch
                {
                    break;
                }
            }
        }
    }
}
