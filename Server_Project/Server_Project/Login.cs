using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Server_Project
{
    enum Mode { Login =0x01, Create = 0x02, Find = 0x02, C_Find = 0x03, D_Create = 0x00, D_C_Create = 0x01, Error = 0x05 }

    public class Login
    {
        static Socket Login_Socket
        {
            get;set;
        }
        static Socket Client_Socket
        {
            get;set;
        }

        static private byte[] SendHeader(byte Mode, string ID, string PWD)
        {
            StringBuilder m_MakeHeader_String = new  StringBuilder();
            m_MakeHeader_String.Append(Mode);
            m_MakeHeader_String.Append(ID);
            m_MakeHeader_String.Append(PWD);

            byte[] m_MakeHeader_Byte = Encoding.ASCII.GetBytes(m_MakeHeader_String.ToString());

            return m_MakeHeader_Byte;
        }

        static private byte[] SendDB(byte[] Message)
        {
            IPEndPoint DBIP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);
            Socket DB_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            DB_Socket.Connect(DBIP);
            DB_Socket.Send(Message);

            byte[] recvBuffer = new byte[] { };
            DB_Socket.Receive(recvBuffer);

            return recvBuffer;
        }

        public void Start()
        {
            byte[] RecvBuffer = new byte[1024];

            Console.Write("현재 IP값 할당 : ");
            string strIP = Console.ReadLine();
            int strPort = 7777;
            IPEndPoint IP = new IPEndPoint(IPAddress.Parse(strIP), strPort);


            Login_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Login_Socket.Bind(IP);
            Login_Socket.Listen(100);

            while (true)
            {
                Client_Socket = Login_Socket.Accept();
                int length = Client_Socket.Receive(RecvBuffer);
                
                string RecvBuffer_String = Encoding.UTF8.GetString(RecvBuffer);

                Console.WriteLine("받은 길이 : {0} \n받은 데이터 값 : {1}", length, RecvBuffer_String);
                Console.WriteLine("받은 데이터 원본 값 : {0}", RecvBuffer[0]);

                if (RecvBuffer[0] == 1) // Login
                {
                    byte[] RecvBuffer_DB = SendDB(RecvBuffer);
                }

                else if (RecvBuffer[0] == 2) // Create
                {
                    byte[] RecvBuffer_DB = SendDB(RecvBuffer);
                }
                else // 알수 없는 종류의 요청
                {

                }
                Client_Socket.Close();
            }
            Login_Socket.Close();
        }
    }
}
