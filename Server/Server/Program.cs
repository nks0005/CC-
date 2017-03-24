using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{

    public class SynchronousSocketListener
    {
        public static void StartListening()
        {
            // Data buffer for incoming data.
            byte[] bytes = new byte[124];

            // Establish the local endpoint for the socket.
            // DNS.GetHostName returns the name of the host running the application.
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

            // Create a TCP/IP socket
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);

                // Start listening from connections.
                while (true)
                {
                    int i = 0;
                    Console.WriteLine("Waiting for a connection...");
                    // Program is suspended while wating for an incoming connection.
                    Socket handler = listener.Accept();
                    while (true)
                    {
                        i++;
                        Console.WriteLine(i + "연결이 완료되었습니다. : " + handler.SocketType.ToString());
                        bytes = new byte[1024];
                        int bytesRec = handler.Receive(bytes);
                        handler.Send(bytes);
                        Console.WriteLine("받은 메시지 : " + Encoding.ASCII.GetString(bytes, 0, bytesRec));
                    }   
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            SynchronousSocketListener.StartListening();
            return;
        }
    }
}