using System.Net;
using System.Net.Sockets;
using Common.Network;
using Proto;

namespace GameServer.Network
{
    /// <summary>
    /// 网络服务
    /// </summary>
    public class NetService
    {
        public NetService() { }

        #region 定义网络监听器
        /// <summary>
        /// 定义网络监听器(监听网络端口)
        /// </summary>
        #endregion
        public TcpSocketListener listener = null;

        #region 初始化网络监听器
        /// <summary>
        /// 初始化网络监听器，需输入服务端端口号
        /// </summary>
        /// <param name="port">端口号</param>
        #endregion
        public void Init(int port)
        {
            listener = new TcpSocketListener("0.0.0.0",port);
            listener.SocketConnectedEvent += OnClientConnected;//订阅客户端接入事件
            
        }
        public void Start()
        {
            listener.Start();
        }

        #region 客户端连进来时的处理
        /// <summary>
        /// 有客户端连进来时，就会触发该方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="socket"></param>
        #endregion
        private void OnClientConnected(object? sender, Socket socket)
        {
            #region Debug
            var ipe = socket.RemoteEndPoint as IPEndPoint;
            Console.WriteLine("有客户端接入，IP是：" + ipe.Address + "、" + "对方端口是：" + ipe.Port);
            #endregion

            //当有客户端接入时，创建一个网络连接
                new NetConnection(socket,
                                  new NetConnection.DataReceivedCallback(OnDataReceived),
                                  new NetConnection.DisConnectedCallback(OnDisConnected));
        }

        private void OnDataReceived(NetConnection sender, byte[] data)
        {
            Vector3 v = Vector3.Parser.ParseFrom(data);
            Console.WriteLine(v.ToString());

            //将收到的消息放入消息队列中
            MessageRouter.Instance.AddMessage(sender, v);
        }

        private void OnDisConnected(NetConnection sender)
        {
            Console.WriteLine("连接断开！");
        }
    }
}
