using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;

namespace SpyGamePlus_Server.Classes
{
    static class NetLib
    {
        public static void SendDataToNet(Socket socket, string text)
        {
            if (socket == null) return;
            byte[] Data = Encoding.Unicode.GetBytes(text);
            socket.Send(Data);
        }
        public static void SendDataToNet(Socket socket, string text, ManualResetEvent SendDone)
        {
            if (socket == null) return;
            SendDone.Reset();
            byte[] Data = Encoding.Unicode.GetBytes(text);
            socket.Send(Data);
            SendDone.Set();
        }
        public static string ReadDataFromNet(Socket socket)
        {
            byte[] Data = new byte[256];
            int count = socket.Receive(Data);
            return Encoding.Unicode.GetString(Data, 0, count);
        }
        public static string ReadDataFromNet(Socket socket, ManualResetEvent ReadDone)
        {
            ReadDone.Reset();
            byte[] Data = new byte[256];
            int count = socket.Receive(Data);
            ReadDone.Set();
            return Encoding.Unicode.GetString(Data, 0, count);
        }
    }
}
