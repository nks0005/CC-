using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace DB
{
    enum Type { Login = 49, SignUp = 50 }

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
        public string Login_Data
        {
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


    public class DataList
    {
       public string ID;
       public string PWD;
       public string HASH;
    }

    class DB_Login
    {
        static Socket Server_socket { get; set; }
        static Socket DB_Socket { get; set; }
        public void Start()
        {
            try
            {
                IPEndPoint IP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);
                DB_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                DB_Socket.Bind(IP);
                DB_Socket.Listen(100);


                List<Thread> Thread_List = new List<Thread>();
                Thread T_Receive = new Thread(new ThreadStart(Receive));
                Thread T_Send = new Thread(new ThreadStart(Send));
                Thread T_Processing = new Thread(new ThreadStart(Processing));

                Thread_List.Add(T_Receive);
                Thread_List.Add(T_Send);
                Thread_List.Add(T_Processing);

                foreach (var list_thread in Thread_List)
                {
                    list_thread.Start();
                }

                Console.WriteLine("종료 하시려면 'Exit'를 입력하세요");
                string Exit = "";
                while (Exit != "Exit")
                {
                    Exit = Console.ReadLine();
                }
                Thread_Stop = true;

                foreach (var list_thread in Thread_List)
                {
                    list_thread.Interrupt();
                    list_thread.Abort();
                }

                DB_Socket.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("에러발생" + ex.Message.ToString());
            }
        }

        static volatile bool Thread_Stop = false;
        static volatile Queue<Login_Data_Form> Processing_Queue = new Queue<Login_Data_Form>();
        static volatile Queue<Login_Data_Form> Send_Queue = new Queue<Login_Data_Form>();


        void Receive() // -> Processing_Queue
        {
            Server_socket = DB_Socket.Accept();
            Console.WriteLine("서버와 동기화 완료");

            while (Thread_Stop == false)
            {
                try
                {
                    Login_Data_Form LDF = new Login_Data_Form();
                    int Length = Server_socket.Receive(LDF.Data, 0, LDF.Data_Can_Length, SocketFlags.None);
                    LDF.Data_Length = Length;
                    Console.WriteLine("데이터를 받았습니다.");
                    Processing_Queue.Enqueue(LDF);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message.ToString());
                    Console.ReadKey();
                    Thread_Stop = true;
                }
            }
        }

        void Send()
        {
            
            while (Thread_Stop == false)
            {
                Thread.Sleep(100);
                if (Send_Queue.Count > 0)
                {
                    Login_Data_Form LDF = Send_Queue.Dequeue();

                    byte[] SendMessage = new byte[1024];
                    SendMessage[0] = LDF.type;
                    for (int i = 0; i < LDF.Login_Data_Byte.Length; i++)
                        SendMessage[i + 1] = LDF.Login_Data_Byte[i];

                    Server_socket.Send(SendMessage, 0, SendMessage.Length, SocketFlags.None);
                }
            }
        }



        void Processing() // -> Send_Queue
        {
            while (Thread_Stop == false)
            {
                try
                {
                    Thread.Sleep(100);
                    if (Processing_Queue.Count > 0)
                    {
                        Login_Data_Form LDF = Processing_Queue.Dequeue();
                        LDF.type = LDF.Data[0];

                        string message = Encoding.UTF8.GetString(LDF.Data);
                        int k = 1;
                        for (int i = 1; message[i] != '-'; i++, k++)
                            LDF.ID.Append(message[i]);
                        k++;
                        for (int i = k; message[i] != '\0'; i++)
                            LDF.PWD.Append(message[i]);

                        Console.WriteLine("아이디 : {0}, 비밀번호 : {1}, Type : {2}", LDF.ID.ToString(), LDF.PWD.ToString(),LDF.type);
                        
                        
                        // Type : 49 = 로그인, Type : 50 = 신규 가입
                        if(LDF.type == 49) // 성공시 49, 실패시 50
                        {
                            DataList DL = new DataList();
                            DL.ID = LDF.ID.ToString();
                            DL.PWD = LDF.PWD.ToString();

                            Console.WriteLine("파일 읽는중...");
                            StreamReader sr = new StreamReader("data.txt");
                            while (sr.Peek() >= 0)
                            {
                                Console.WriteLine(sr.ReadLine());
                            }
                            sr.Close();

                        }
                        else if(LDF.type == 50) // 성공시 51, 실패시 52
                        {
                            DataList DL = new DataList();
                            DL.ID = LDF.ID.ToString();
                            DL.PWD = LDF.PWD.ToString();
                            DL.HASH = DL.PWD + "AAA";

                            StreamWriter sw = new StreamWriter("data.txt");
                            sw.WriteLine(DL.ID + " " + DL.PWD +" "+ DL.HASH);
                            sw.Close();

                            Console.WriteLine("저장 완료.");
                        }
                        else // 이상한 Type 
                        {
                            Console.WriteLine("이상한 Type의 정보가 들어왔습니다. ");
                            LDF.type = 48;
                        }


                        
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message.ToString());
                }
            }
        }
    }
}