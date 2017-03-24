using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace GameServer_Version_1
{
    enum _NETWORKSTATE { YES = 0x00, NO = 0x01 }

    class SERVER_SOCKET
    {
        List<Thread> Socket_List = new List<Thread>();
        private volatile bool _StopThread = false;

        public SERVER_SOCKET()
        {
            Start();
        }

        public void Start()
        {
            string Strip = "127.0.0.1";
            const int port = 5656;
            IPEndPoint ipendpoint = new IPEndPoint(IPAddress.Parse(Strip), port);
            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(ipendpoint);
            server.Listen(100);
            Thread Check_thread = new Thread(Check);
            Check_thread.Start();
            while(_StopThread == false)
            {
                server.Accept();
                Thread temp_thread = new Thread(() => Accept_Thread(server));
                temp_thread.Start();
                Socket_List.Add(temp_thread);
            }

            foreach(Thread thread in Socket_List)
            {
                thread.Join();
            }
            Check_thread.Join();
            server.Close();
        }

        void Check()
        {
            Console.ReadKey();
            _StopThread = true;
            Console.WriteLine(_StopThread);
        }
        
        void Accept_Thread (Socket Accept_Socket)
        {
            while (_StopThread == false)
            { 
                byte[] RecvBytes = new byte[1024];
                int Recv_Datalength = Accept_Socket.Receive(RecvBytes, 0, Accept_Socket.Available, SocketFlags.None);
                if (Recv_Datalength > 0)
                {
                    string RecvString = Encoding.UTF8.GetString(RecvBytes, 0, Recv_Datalength);
                    Console.WriteLine("{0} : 받은 데이터 : {1}", Accept_Socket.RemoteEndPoint.ToString(), RecvString);
                }
            }
            Console.WriteLine("{0} 종료", Accept_Socket.RemoteEndPoint.ToString());
            Accept_Socket.Close();
        }
    }



    class Program
    {
        static void Main(string[] args)
        {
            SERVER_SOCKET SS = new SERVER_SOCKET();
        }
    }
}
