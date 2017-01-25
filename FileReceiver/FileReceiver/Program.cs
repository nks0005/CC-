using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FUP;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.IO;

namespace FileReceiver
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("사용법 : {0} <Directory>", Process.GetCurrentProcess().ProcessName);
            uint msgId = 0;
            string dir = Console.ReadLine();

            if (Directory.Exists(dir) == false)
                Directory.CreateDirectory(dir);

            const int bindPort = 5425;
            TcpListener server = null;
            try
            {
                IPEndPoint localAddress = new IPEndPoint(0, bindPort); // IP 주소를 0으로 입력하면 127.0.0.1 뿐 아니라 OS에 할당되어 있는 어떤 주소로도 서버에 접속이 가능합니다.

                server = new TcpListener(localAddress);
                server.Start();

                Console.WriteLine("파일 업로드 서버 시작...");

                while (true)
                {
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("클라이언트 접속 : {0} ", ((IPEndPoint)client.Client.RemoteEndPoint).ToString());

                    NetworkStream stream = client.GetStream();

                    Message reqMsg = MessageUtil.Receive(stream); // 클라이언트가 보내온 파일 전송 요청 메시지를 수신합니다.

                    if (reqMsg.Header.MSGTYPE != CONSTANTS.REQ_FILE_SEND)
                    {
                        stream.Close();
                        client.Close();
                        continue;
                    }

                    BodyRequest reqBody = (BodyRequest)reqMsg.Body;

                    Console.WriteLine("파일 업로드 요청이 왔습니다. 수락하시겠습니까? yes/no");
                    string answer = Console.ReadLine();

                    Message rspMsg = new Message();
                    rspMsg.Body = new BodyResponse()
                    {
                        MSGID = rspMsg.Header.MSGID,
                        RESPONSE = CONSTANTS.ACCEPTED
                    };
                    rspMsg.Header = new Header()
                    {
                        MSGID = msgId++,
                        MSGTYPE = CONSTANTS.REP_FILE_SEND,
                        BODYLEN = (uint)rspMsg.Body.GetSize(),
                        FRAGMENTED = CONSTANTS.NOT_FRAGMENTED,
                        LASTMSG = CONSTANTS.LASTMSG,
                        SEQ = 0
                    };

                    if (answer != "yes")
                    {
                        rspMsg.Body = new BodyResponse() // 사용자가 "yes"가 아닌 답을 입력하면 클라이언트에게 "거부" 응답을 보냅니다.
                        {
                            MSGID = reqMsg.Header.MSGID,
                            RESPONSE = CONSTANTS.DENIED
                        };
                        MessageUtil.Send(stream, rspMsg);
                        stream.Close();
                        client.Close();

                        continue;
                    }
                    else
                        MessageUtil.Send(stream, rspMsg);

                    Console.WriteLine("파일 전송을 시작합니다...");

                    long fileSize = reqBody.FILESIZE;
                    string fileName = Encoding.Default.GetString(reqBody.FILENAME);
                    FileStream file = new FileStream(dir + "\\" + fileName, FileMode.Create);

                    uint? dataMsgId = null;
                    ushort prevSeq = 0;
                    while ((reqMsg = MessageUtil.Receive(stream)) != null)
                    {
                        Console.Write("#");
                        if (reqMsg.Header.MSGTYPE != CONSTANTS.FILE_SEND_DATA)
                            break;

                        if (dataMsgId == null)
                            dataMsgId = reqMsg.Header.MSGID;
                        else
                        {
                            if (dataMsgId != reqMsg.Header.MSGID)
                                break;
                        }

                        if (prevSeq++ != reqMsg.Header.SEQ) // 메시지 순서가 어긋나면 전송을 중단합니다.
                        {
                            Console.WriteLine("{0} , {1}", prevSeq, reqMsg.Header.SEQ);
                            break;
                        }

                        file.Write(reqMsg.Body.GetBytes(), 0, reqMsg.Body.GetSize());

                        if (reqMsg.Header.FRAGMENTED == CONSTANTS.NOT_FRAGMENTED)
                            break;
                        if (reqMsg.Header.LASTMSG == CONSTANTS.LASTMSG)
                            break;
                    }

                    long recvFileSize = file.Length;
                    file.Close();

                    Console.WriteLine();
                    Console.WriteLine("수신 파일 크기 : {0} bytes", recvFileSize);

                    Message rstMsg = new Message();
                    rstMsg.Body = new BodyResult()
                    {
                        MSGID = reqMsg.Header.MSGID,
                        RESULT = CONSTANTS.SUCCESS
                    };
                    rstMsg.Header = new Header()
                    {
                        MSGID = msgId++,
                        MSGTYPE = CONSTANTS.FILE_SEND_RES,
                        BODYLEN = (uint)rstMsg.Body.GetSize(),
                        FRAGMENTED = CONSTANTS.NOT_FRAGMENTED,
                        LASTMSG = CONSTANTS.LASTMSG,
                        SEQ = 0
                    };

                    if (fileSize == recvFileSize)
                        MessageUtil.Send(stream, rstMsg);
                    else
                    {
                        rstMsg.Body = new BodyResult()
                        {
                            MSGID = reqMsg.Header.MSGID,
                            RESULT = CONSTANTS.FAIL
                        };

                        MessageUtil.Send(stream, rstMsg);
                    }
                    Console.WriteLine("파일 전송을 마쳤습니다.");

                    stream.Close();
                    client.Close();
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                server.Stop();
            }

            Console.WriteLine("서버를 종료합니다.");
        }
    }
}