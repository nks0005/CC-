using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            IPEndPoint ip = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 11000);

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Connect(ip);
                for (int i = 0; i < 5; i++)
                {
                    string Smsg = Console.ReadLine();

                    byte[] msg = Encoding.ASCII.GetBytes(Smsg);
                    socket.Send(msg);
                }
                byte[] rmsg = new byte[1024];
                int length = socket.Receive(rmsg);
                Console.WriteLine(Encoding.ASCII.GetString(rmsg, 0, length));
            }
            catch (SocketException se)
            {
                Console.WriteLine(se);
            }
            finally
            {
                socket.Close();
            }
        }
    }
}