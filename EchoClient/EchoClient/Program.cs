using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace EchoClient
{
    class Program
    {
        static void Main(string[] args)
        {
            string bindIp = "127.0.0.1";
            int bindPort = 5426;

            string serverIp = "127.0.0.1";
            const int serverPort = 5425;

            string message = null;
            try
            {
                IPEndPoint clientAddress = new IPEndPoint(IPAddress.Parse(bindIp), bindPort);
                IPEndPoint serverAddress = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);

                Console.WriteLine("클라이언트 : {0}, 서버 : {1}", clientAddress.ToString(), serverAddress.ToString());

                TcpClient client = new TcpClient(clientAddress);

                client.Connect(serverAddress);

                NetworkStream stream = null;
                do
                {
                    Console.WriteLine("보낼 메시지 : ");
                    message = Console.ReadLine();

                    byte[] data = System.Text.Encoding.Default.GetBytes(message);

                    stream = client.GetStream();

                    stream.Write(data, 0, data.Length);

                    Console.WriteLine("송신 : {0}", message);

                    data = new byte[256];

                    string responseData = "";

                    int bytes = stream.Read(data, 0, data.Length);
                    responseData = Encoding.Default.GetString(data, 0, bytes);
                    Console.WriteLine("수신 : {0}", responseData);
                } while (message != string.Empty);

                stream.Close();
                client.Close();
            }
            catch (SocketException se)
            {
                Console.WriteLine(se);
            }

            Console.WriteLine("클라이언트를 종료합니다.");
        }
    }
}
