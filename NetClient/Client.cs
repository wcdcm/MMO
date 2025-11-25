using Common.Network;
using Google.Protobuf;
using Proto;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TestClient
{
    internal class Client
    {
        static void Main(string[] args)
        {
            string host = "127.0.0.1";//服务器IP
            int port = 32510;//服务器端口

            IPAddress ip = IPAddress.Parse(host);//将IP字符串转换为IPAddress类型
            IPEndPoint ipe = new IPEndPoint(ip, port);//创建一个IP 端点，里面封装了IP地址和端口号

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//创建Socket对象
            socket.Connect(ipe);
            Console.WriteLine("成功连接到服务器");

            #region Test
            ////发消息：按照 消息头 + 消息体 的格式来发送
            //string text = "黄河之水天上来";
            //byte[] body = Encoding.UTF8.GetBytes(text);
            //SendMessage(socket, body);

            //string text2 = "奔流到海不复回";
            //byte[] buffer2 = Encoding.UTF8.GetBytes(text2);
            //SendMessage(socket, buffer2);
            ////Console.ReadKey();

            //while (true)
            //{
            //    Console.WriteLine("请输入想发送的内容：");
            //    string txt = Console.ReadLine();
            //    SendMessage(socket, Encoding.UTF8.GetBytes(txt));
            //}
            #endregion

            #region Test2
            //Vector3 v = new Vector3() { X = 100,Y = 200,Z = 300};
            //byte[] data = v.ToByteArray();
            //SendMessage(socket, data);
            //Console.ReadKey();
            #endregion

            #region 用户注册测试
            Proto.Package package = new Package();
            package.Request = new Proto.Request();
            package.Request.UserRegisterRequest = new UserRegisterRequest();
            package.Request.UserRegisterRequest.Username = "who";
            package.Request.UserRegisterRequest.Password = "who124553";

            #region 发送消息（旧）
            ////发送消息（旧）：
            ////基于内存的数据流，
            ////用于临时存储二进制数据,作为 Protobuf 序列化数据的 “临时缓冲区”，
            ////后续序列化后的二进制内容会写入该流。
            //MemoryStream rawOutput = new MemoryStream();
            //CodedOutputStream output = new CodedOutputStream(rawOutput);
            //package.WriteTo(output);
            //output.Flush();
            //SendMessage(socket, rawOutput.ToArray());
            //Console.ReadKey();
            #endregion

            //网络连接对象：
            //客户端也需要收发消息，所以也需要创建一个网络连接对象
            NetConnection conn = new NetConnection(socket,
                                  new NetConnection.DataReceivedCallback(OnDataReceived),
                                  new NetConnection.DisConnectedCallback(OnDisConnected));
            #region 另一种写法
            /*
             NetConnection conn = new NetConnection(socket,
                                  OnDataReceived,
                                  OnDisConnected);
             */
            #endregion

            conn.Send(package);
            conn.Send(package);
            conn.Send(package);
            Console.ReadKey();
            #endregion
        }

        private static void OnDisConnected(NetConnection sender)
        {
            
        }

        private static void OnDataReceived(NetConnection sender, byte[] data)
        {
            
        }


        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="body"></param>
        public static void SendMessage(Socket socket, byte[] body)
        {
            int head = body.Length;//定义一个int类型的头部，存储body对应的字节数组的长度
            byte[] headByte = BitConverter.GetBytes(head);//将head转化成字节流
            socket.Send(headByte);//发送头部
            socket.Send(body);//发送消息体（内容）
        }
    }
}
