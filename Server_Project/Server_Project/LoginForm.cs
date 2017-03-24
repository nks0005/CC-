using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace Server_Project
{
    public class Login_Data_Form
    {
        public byte[] Data = new byte[1024];
        public int Data_Length;

        public int Data_Can_Length = 1024;
        public Socket clientsocket { get; set; }
        public byte type { get; set; }
        
        public StringBuilder ID = new StringBuilder();
        public StringBuilder PWD = new StringBuilder();
        public StringBuilder Hash = new StringBuilder();
        public string Login_Data {
            get
            {
                return (this.ID.ToString() + "-" + this.PWD.ToString());
            }
        } // type + ID + PWD
        public byte[] Login_Data_Byte
        {
            get
            {
                return Encoding.UTF8.GetBytes(Login_Data);
            }
        }
        public string Success_Login_Data
        {
            get
            {
                return (this.type + this.ID.ToString() + this.Hash.ToString());
            }
        }// type + ID + Hash
    }

    class LoginForm
    {
        static Socket ServerSocket { get; set; }
        static Socket GameSocket { get; set; }
        static Socket DbSocket { get; set; }

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
            Console.WriteLine("자신의 IP 값 : " + LocalIPAddress());
            IPEndPoint Server_IP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7777);
            IPEndPoint DB_IP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);
            IPEndPoint GameServer_IP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9999);

            ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            DbSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
           // GameSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                Console.WriteLine("DB 서버에 접속합니다." + DB_IP.ToString());
                DbSocket.Connect(DB_IP);
            }
            catch (Exception ex)
            {
                Console.WriteLine("DB 서버에 접속중 예외가 발생하였습니다." + ex.Message.ToString());
                Console.ReadKey();
                 
            }
            Console.WriteLine("DB 서버에 접속완료되었습니다.");

            try
            {
                Console.WriteLine("서버를 생성중입니다..." + LocalIPAddress());
                ServerSocket.Bind(Server_IP);
                ServerSocket.Listen(1000);
            }
            catch(Exception ex)
            {
                Console.WriteLine("서버 생성 중 예외가 발생하였습니다." + ex.Message.ToString());
            }
            Console.WriteLine("서버를 생성하였습니다.");

            List<Thread> List_Thread = new List<Thread>();

            Thread T_Login_Recv = new Thread(new ThreadStart(Recv_Login_Data));
            Thread T_Login_Send = new Thread(new ThreadStart(Send_Login_Data));

            Thread T_DB_Recv = new Thread(new ThreadStart(Recv_DB));
            Thread T_DB_Send = new Thread(new ThreadStart(Send_DB));

            
            List_Thread.Add(T_Login_Recv);
            List_Thread.Add(T_Login_Send);

            List_Thread.Add(T_DB_Recv);
            List_Thread.Add(T_DB_Send);
            
            foreach(var list_thread in List_Thread)
            {
                list_thread.Start();
            }

            Console.WriteLine("종료 하시려면 'Exit'를 입력하세요");
            string Exit = "";
            while(Exit != "Exit")
            {
                Exit = Console.ReadLine();
            }
            Thread_Stop = true;

            foreach(var list_thread in List_Thread)
            {
                list_thread.Interrupt();
                list_thread.Abort();
            }

            ServerSocket.Close();
            DbSocket.Close();

            Console.WriteLine("종료하였습니다.");
        }

        static volatile Queue<Login_Data_Form> Recv_Login_Queue = new Queue<Login_Data_Form>();
        static volatile Queue<Login_Data_Form> Send_Login_Queue = new Queue<Login_Data_Form>();

        static volatile Queue<Login_Data_Form> Send_DB_Queue = new Queue<Login_Data_Form>();
        static volatile Queue<Login_Data_Form> Recv_DB_Queue = new Queue<Login_Data_Form>();

        static volatile bool Thread_Stop = false;

        // 클라이언트로 부터 로그인 종류 + 아이디 + 비밀번호를 얻습니다.
        public void Recv_Login_Data()  // -> Send_DB_Queue
        {
            Recv_Login_Queue.Clear();
            while (Thread_Stop == false)
            {
                try
                {
                    Thread.Sleep(100);
                    Login_Data_Form LDF = new Login_Data_Form();
                    LDF.clientsocket = ServerSocket.Accept();
                    int Length = LDF.clientsocket.Receive(LDF.Data, 0, LDF.Data_Can_Length, SocketFlags.None);
                    LDF.Data_Length = Length;

                    LDF.type = LDF.Data[0]; // 1 값은 49

                    string message = Encoding.UTF8.GetString(LDF.Data);
                    int k = 1;
                    for (int i = 1; message[i] != '-'; i++, k++)
                        LDF.ID.Append(message[i]);
                    k++;
                    for (int i = k; message[i] != '\0'; i++)
                        LDF.PWD.Append(message[i]);
                    
                    Console.WriteLine("로그인이 되었습니다.");
                    Console.WriteLine(LDF.ID.ToString() + " " + LDF.PWD.ToString());
                    Send_DB_Queue.Enqueue(LDF);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Recv_Login_Data() Error" + ex.Message.ToString());
                }
            }
        }

        // 클라이언트와 게임서버에게 아이디와 아이디에 맞는 암호화된 Hash 정보를 넘깁니다.
        public void Send_Login_Data()
        {
            Send_Login_Queue.Clear();
            while (Thread_Stop == false)
            {
                Thread.Sleep(100);
                if (Send_Login_Queue.Count > 0)
                {
                    try
                    {
                        Login_Data_Form LDF = new Login_Data_Form();
                        byte[] SendMessage = Encoding.UTF8.GetBytes(LDF.Success_Login_Data.ToString());

                        // 클라이언트와 게임서버에게 보내야함
                        //                GameSocket.Send(SendMessage, 0, SendMessage.Length, SocketFlags.None);

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Send_Login_Data() Error" + ex.Message.ToString());
                    }
                }
            }
        }
        
        // DB으로부터 정보를 받습니다.
        public void Recv_DB() // -> Send_Login_Queue
        {
            Recv_DB_Queue.Clear();
            while (Thread_Stop == false)
            {
                try
                {
                    Thread.Sleep(100);
                    Login_Data_Form LDF = new Login_Data_Form();
                    int Length = DbSocket.Receive(LDF.Data, 0, LDF.Data_Can_Length, SocketFlags.None);
                    LDF.Data_Length = Length;
                   
                    int k = 1;
                    for (int i = 1; LDF.Data[i] != 32; i++, k++)
                    {
                        LDF.ID.Append(LDF.Data[i]);
                    }

                    for (int i = k; LDF.Data[i] != 32; i++)
                    {
                        LDF.Hash.Append(LDF.Data[i]);
                    }

                    Console.WriteLine("데이터 베이스로부터 {0}의 해시값인 {1}을 받았습니다.", LDF.ID.ToString(), LDF.Hash.ToString());

                    Send_Login_Queue.Enqueue(LDF);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Recv_DB() Error" + ex.Message.ToString());
                }
            }
        }

        // DB에게 정보를 넘깁니다.
        public void Send_DB()
        {
            Send_DB_Queue.Clear();
            while (Thread_Stop == false)
            {
                Thread.Sleep(100);
                if (Send_DB_Queue.Count > 0)
                {
                    try
                    {
                        Login_Data_Form LDF = Send_DB_Queue.Dequeue();
                        Console.WriteLine(LDF.Login_Data.ToString());
                        byte[] SendMessage = new byte[1024];
                        SendMessage[0] = LDF.type;
                        for (int i = 0; i < LDF.Login_Data_Byte.Length; i++)
                            SendMessage[i + 1] = LDF.Login_Data_Byte[i];
                        
                        DbSocket.Send(SendMessage, 0, SendMessage.Length, SocketFlags.None);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Send_DB() Error" + ex.Message.ToString());
                    }
                }
            }
        }
    }
}
