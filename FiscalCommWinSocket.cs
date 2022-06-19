using System;
using System.IO.Ports;
using System.Net.Sockets;
using System.Threading;

namespace TestSDK
{
    public class FiscalCommWinSocket : FiscalComm
    {
        System.Net.Sockets.TcpClient clientSocket = new System.Net.Sockets.TcpClient();
        private string ipAddress;
        private int port;
        NetworkStream netStream;

        public FiscalCommWinSocket(string ip_Address, int tcp_Port)
            : base()
        {
            this.ipAddress = ip_Address;
            this.port = tcp_Port;
        }

        public override void Connect()
        {
            Close();
            clientSocket.Connect(ipAddress, port);
            //needed for device to wake up
            Thread.Sleep(200);

        }

        public override void Close()
        {
            if (clientSocket.Connected)
            {
                netStream.Close();
                clientSocket.Close();
                Thread.Sleep(500);
            }
        }

        private byte[] readBuf = new byte[1024];

        public override byte[] Read(int maxLength, int timeout)
        {
            int read = 0;
            int len = 0;
            byte[] r = new byte[maxLength];
            clientSocket.ReceiveTimeout = timeout;

             netStream = clientSocket.GetStream();

            if (netStream.CanRead)
            {
                while (read < maxLength)
                {
                    int toRead = maxLength - read;
                    len = netStream.Read(readBuf, 0, toRead);
                    Array.Copy(readBuf, 0, r, read, len);
                    read += len;
                }
               
            }
            return r;
           
        }

    public override void Write(byte[] data, uint len, int timeout)
        {
            netStream = clientSocket.GetStream();
            if (netStream.CanWrite)
            {
                clientSocket.SendTimeout = timeout;
                netStream.Write(data, 0, (int)len);
            }
           
        }

        public override void ClearReceive()
        {
           //
        }


    }
}
