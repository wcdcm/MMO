using Network;
using Proto;
using System.Net.Sockets;

namespace Common.Network
{
    #region 网络连接对象
    /// <summary>
    /// <para>网络连接对象 </para>
    /// <para>主要职责：</para>
    /// <para>发送消息、接收消息（回调）、</para>
    /// <para>解析消息、关闭连接、</para>
    /// <para>断开连接时通知对方（回调）。</para>
    /// </summary>
    #endregion
    public class NetConnection
    {
        #region 收到网络连接的委托
        /// <summary>
        /// 收到网络连接的委托
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="data"></param>
        #endregion
        public delegate void DataReceivedCallback(NetConnection sender, byte[] data);

        #region 断开网络连接的委托
        /// <summary>
        /// 断开网络连接的委托
        /// </summary>
        /// <param name="sender"></param>
        #endregion
        public delegate void DisConnectedCallback(NetConnection sender);

        public Socket socket;

        // 存储由外部传入的回调方法
        private DataReceivedCallback dataReceivedCallback;
        private DisConnectedCallback disConnectedCallback;

        //提供构造函数供外部注册回调
        public NetConnection(Socket socket,DataReceivedCallback cb1, DisConnectedCallback cb2)
        {
            this.socket = socket;
            this.dataReceivedCallback = cb1;
            this.disConnectedCallback = cb2;

            //创建解码器，负责解析网络消息
            LengthFieldDecoder lfd = new LengthFieldDecoder(socket, 64 * 1024, 0, 4, 0, 4);//定义读取数据(message)的方式
            
            //消息接收事件订阅
            lfd.DataReceived += OnDataReceived;

            //断开连接事件订阅
            lfd.Disconnected += (Socket socket)=> disConnectedCallback?.Invoke(this);

            //启动消息解码器
            lfd.Start();
        }

        #region 接收到消息时的处理方法
        /// <summary>
        /// 接收到消息时的处理方法
        /// </summary>
        /// <param name="buffer"></param>
        #endregion
        private void OnDataReceived(byte[] buffer)
        {
            //触发接收消息的回调方法
            dataReceivedCallback?.Invoke(this,buffer);
        }

        #region 关闭连接
        /// <summary>
        /// 关闭连接
        /// </summary>
        #endregion
        public void Close()
        {
            try { socket.Shutdown(SocketShutdown.Both); } catch { }
            socket.Close();
            socket = null;
        }
    }
}
