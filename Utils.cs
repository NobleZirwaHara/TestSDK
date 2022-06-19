using System;
using System.Text;

namespace TestSDK
{
    public class Utils
    {
        public static void Log(string text)
        {
            System.Diagnostics.Debug.WriteLine(text);
        }
        public static void LogData(string title, byte[] data, int offset, int length)
        {
            const string HEX = "0123456789ABCDEF";
            StringBuilder sb = new StringBuilder(length * 2);

            for (int i = 0; i < length;i++)
            {
                sb.Append(HEX[(data[offset + i] >> 4) & 0x0f]);
                sb.Append(HEX[(data[offset + i] >> 0) & 0x0f]);
            }

            System.Diagnostics.Debug.WriteLine(title + "(" + length.ToString("D2") + "): " + sb.ToString());
        }
    }
}
