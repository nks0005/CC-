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
    public class StateObject
    {
        public Socket workSocket = null;
        public const int BufferSize = 1024;
        public byte[] buffer = new byte[BufferSize];
        public StringBuilder sb = new StringBuilder();
    }

    public class Logins
    {
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        public Socket g_listener = null;
        public Socket db_socket = null;

        // 자신의 로컬 주소를 반환(string)
        public string LocalIPAddress()
        {
            IPHostEntry host;
            string localIP = "";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    return localIP;
                }
            }
            return "127.0.0.1";
        }

        public void Start()
        {
            byte[] bytes = new byte[1024];

            IPEndPoint dbEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);
            db_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                db_socket.Connect(dbEndPoint);
            }
            catch (ObjectDisposedException se)
            {
                Console.WriteLine("DB 서버가 닫혔습니다." + se.Message.ToString());
                Console.ReadKey();
            }
            Console.WriteLine("데이터베이스 서버와 연결이 되었습니다." + dbEndPoint.ToString());

            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(LocalIPAddress()), 7777);
            Console.WriteLine("서버 아이피가 할당 되었습니다." + localEndPoint.ToString());
            g_listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                g_listener.Bind(localEndPoint);
                g_listener.Listen(100);

                allDone.Reset();

                Console.WriteLine("로그인 요청을 받을 준비가 되었습니다...");
                g_listener.BeginAccept(new AsyncCallback(AcceptCallback), g_listener);

            }
            catch (SocketException se)
            {
                Console.WriteLine("SocketException 발생! " + se.Message.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception 발생! " + ex.Message.ToString());
            }

            allDone.WaitOne(); 
            Console.WriteLine("종료 되었습니다.");
        }

        public void AcceptCallback(IAsyncResult ar)
        {
            // 메인 스레드가 계속 진행되기 위해 신호를 줌
            allDone.Set();

            // 소켓 요청을 핸들할 소켓을 얻어옴
            Socket listener = ar.AsyncState as Socket;
            Socket handler = listener.EndAccept(ar);

            // State Object 클래스를 만듬
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReadCallback), state);
        }

        public void ReadCallback(IAsyncResult ar)
        {
            string content = string.Empty;

            StateObject state = ar.AsyncState as StateObject;
            Socket handler = state.workSocket;

            int bytesRead = handler.EndReceive(ar);

            // 데이터를 받음 
            if (bytesRead > 0)
            {
                state.sb.Clear();
                state.sb.Append(Encoding.UTF8.GetString(state.buffer, 0, bytesRead));
                Console.WriteLine("받은 데이터 값 : " + state.sb.ToString());
                if ((state.sb[0] == 0) || (state.sb[0] == 1)) // Login or Create
                {
                    DB_Call(handler, state.sb.ToString());
                }
                else // 알수없는 이상한 종류의 데이터 ( 무시 )
                {

                }
            }
        }

        public void DB_Call(Socket handler, string data)
        {
            byte[] byteData = Encoding.UTF8.GetBytes(data);

            db_socket.Send(byteData);
            byte[] recvData = new byte[1024];
            db_socket.Receive(recvData);
            string recvString = Encoding.UTF8.GetString(recvData);

            Client_Send(handler, recvString);
        }

        public void Client_Send(Socket handler, string data)
        {
            byte[] byteData = Encoding.UTF8.GetBytes(data);

            handler.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(SendCallback), handler);
        }

        public void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket handler = ar.AsyncState as Socket;

                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Send {0} bytes to client : {1} ", bytesSent, handler.RemoteEndPoint.ToString());

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            catch (SocketException se)
            {
                Console.WriteLine("SendCallback [SocketException] Error " + se.Message.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendCallback [Exception] Error " + ex.Message.ToString());
            }
        }
    }
}