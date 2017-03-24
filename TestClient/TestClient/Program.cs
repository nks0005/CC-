using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;


namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            IPEndPoint IP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7777);
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            socket.Connect(IP);

            byte option = 0;
            Console.WriteLine("로그인(L), 회원가입(S)");
            string What =  Console.ReadLine();
            if (What == "L")
                option = 1;
            else if (What == "S")
                option = 2;


            string bufz = Console.ReadLine();
            byte standard = 45;
            string bufzz = Console.ReadLine();


            byte[] strings1 = Encoding.Default.GetBytes(bufz.ToString());
            byte[] strings2 = Encoding.Default.GetBytes(bufzz.ToString());
            byte[] buf = new byte[strings1.Length + strings2.Length + 2];

            buf[0] = option;
            int i = 1;
            for (int k = 0; k < strings1.Length; i++, k++)
            {
                buf[i] = strings1[k];
                Console.Write("{0} ", strings1[k]);
            }
            Console.WriteLine();
            buf[i] = standard;
            i++;
            for (int k = 0; k < strings2.Length; i++, k++)
            {
                buf[i] = strings2[k];
                Console.Write("{0} ", strings2[k]);
            }
            Console.WriteLine();

            for (int z = 0; i < buf.Length; z++)
                Console.Write("{0} ", buf[z]);
            Console.WriteLine();

            socket.Send(buf);

            byte[] buffer = new byte[2048];
            int length = socket.Receive(buffer, 0, 2048, SocketFlags.None);

            byte[] recvMessage = new byte[length];
            for (int v = 0; v < length; v++)
                recvMessage[v] = buffer[v];

            byte Type = recvMessage[0];
            if (Type == 1)
            {
                Console.WriteLine("{0} ", Type);
                Console.Write("로그인 성공 : ");
                for (int k = 1; recvMessage[k] != (byte)0; k++)
                {
                    Console.Write("{0} ", recvMessage[k]);
                }
            }
            else if (Type == 2)
                Console.WriteLine("로그인 실패");

            else if (Type == 3)
                Console.WriteLine("회원가입 성공");

            else if (Type == 4)
                Console.WriteLine("회원가입 실패");


            Console.WriteLine();
            socket.Close();

        }
    }
}
