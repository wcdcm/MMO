using System.Net;
using System.Net.Sockets;

namespace GameServer.Network
{
    public class TcpSocketListener
    {
        private Socket serverSocket;//创建服务器的Socket对象

        private IPEndPoint endPoint;//定义一个服务端的IP和端口（Port）

        public EventHandler<Socket> SocketConnectedEvent;//客户端接入事件

        public EventHandler<Socket> SocketDisconnectedEvent;//客户端断开事件

        public TcpSocketListener(string host, int port)
        {
            //初始化一个IP端点，一个IP端点由IP地址和端口组成
            endPoint = new IPEndPoint(IPAddress.Parse(host), port);
        }

        public void Start()
        {
            lock (this) //保证同一时间只能有一个进程
            {
                if (!isRunning)//保证当服务器没有启动时，再启动服务器，防止重复启动 重复绑定端口
                {
                    #region 初始化Socket对象。
                    //Param1：指定网络地址的类型，AddressFamily.InterNetwork 表示使用 IPv4 地址 进行通信。
                    //Param2：指定Socket的通信类型，Stream表示面向连接的字节流类型
                    //Param3：指定Socket的协议类型，这里是TCP通信协议
                    #endregion
                    serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    //Socket对象绑定服务器的IP和端口
                    serverSocket.Bind(endPoint);

                    //Socket对象进行监听该服务器端口
                    serverSocket.Listen();
                    Console.WriteLine("开始监听端口:" + endPoint.Port);

                    #region 异步接收客户端连接请求
                    // 常见的同步接收是 ： serverSocket.Accept()。这是一个同步方法，这就意味着它会阻塞主线程，在没有接收到请求之前会一直处于等待状态，阻塞后面的逻辑。
                    // 1. 创建异步操作事件参数对象，这是一个容器，用于处理连接对象的Socket
                    // 2. 绑定接受完成后的回调方法OnAccept，一旦有客户端连入就用OnAccept对客户端进行处理
                    // 3. 启动异步接收，异步接收需要一个用于处理连接的参数对象，这个对象就是args，args会存储连接的对象的Socket信息
                    #endregion
                    SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                    args.Completed += OnAccept                            ;
                    serverSocket.AcceptAsync(args);
                }
            }
        }

        /// <summary>
        /// 处理客户端连进来的逻辑
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// 
        #region e和args的关系
        //此处的e和args是同一个对象，当Completed事件委托触发时，args会作为参数传进OnComplete方法
        #endregion
        private void OnAccept(object? sender, SocketAsyncEventArgs e)
        {
            //如果客户端成功连入
            if (e.SocketError == SocketError.Success)
            {
                //为客户端创建一个Socket，e.AcceptSocket里存储的就是当前接入的对象的Socket信息
                Socket client = e.AcceptSocket;

                if (client != null)
                {
                    #region 触发客户端连接事件：(其他逻辑就可以订阅这个事件)
                    //Param1 ; 事件的Sender -> 这个服务器进程
                    //Param2 ; 对应的Socket -> 刚刚创建的客户端Socket
                    #endregion
                    SocketConnectedEvent?.Invoke(this, client);
                }

                #region 继续接收下一位
                //将e.AcceptSocket设置为空，在下一位到来时将e.AcceptSocket设置为下一位客户端的Socket
                #endregion
                e.AcceptSocket = null;
                serverSocket.AcceptAsync(e);
            }
        }

        private bool isRunning
        {
            get { return serverSocket != null; }
        }

        //关闭服务器
        public void Stop()
        {
            if (serverSocket == null)
                return;

            serverSocket.Close();
            serverSocket = null;
        }
    }
}
