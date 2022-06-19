using System;
namespace TestSDK
{
    public abstract class FiscalComm
    {
        const int DEFTIMEOUT = 5000;

        public byte ReadByte()
        {
            return ReadByte(DEFTIMEOUT);
        }

        public byte ReadByte(int timeout)
        {
            byte[] r = Read(1, timeout);
            return r[0];
        }

        public byte[] Read(int maxLength)
        {
            return Read(maxLength, DEFTIMEOUT);
        }

        public abstract void Connect();
        public abstract void Close();

        public abstract byte[] Read(int maxLength, int timeout);
        public void Write(byte[] data, uint len)
        {
            Write(data, len, 1000);
        }
        public abstract void Write(byte[] data, uint len, int timeout);

        public abstract void ClearReceive();
    }
}
