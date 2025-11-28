using Google.Protobuf;
using Network;
using Proto;
using System;
using System.IO;
using System.Net.Sockets;

namespace Summer.Network
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
        #region 接收网络数据的委托
        /// <summary>
        /// 接收网络数据的委托
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
        public NetConnection(Socket socket, DataReceivedCallback cb1, DisConnectedCallback cb2)
        {
            this.socket = socket;
            this.dataReceivedCallback = cb1;
            this.disConnectedCallback = cb2;

            #region 消息解码
            //创建解码器，负责解析网络消息
            LengthFieldDecoder lfd = new LengthFieldDecoder(socket, 64 * 1024, 0, 4, 0, 4);//定义读取数据(message)的方式
            //消息接收事件订阅
            lfd.DataReceived += DataReceivedHandler;
            //断开连接事件订阅
            lfd.Disconnected += (Socket socket) => disConnectedCallback?.Invoke(this);
            //启动消息解码器
            lfd.Start();
            #endregion
        }

        #region 接收到消息的处理方法
        /// <summary>
        /// 接收到消息时的处理方法
        /// </summary>
        /// <param name="buffer"></param>
        #endregion
        private void DataReceivedHandler(byte[] buffer)
        {
            //触发接收消息的回调方法
            dataReceivedCallback?.Invoke(this, buffer);
        }

        #region 快捷发送网络数据包
        private Proto.Package _package = null;
        public Proto.Request Request
        {
            get
            {
                if (_package == null) 
                {
                    _package = new Proto.Package();
                }
                if(_package.Request == null) 
                {
                    _package.Request = new Proto.Request();
                }
                return _package.Request;
            }
        }
        public Proto.Response Response
        {
            get
            {
                if (_package == null)
                {
                    _package = new Proto.Package();
                }
                if (_package.Response == null)
                {
                    _package.Response = new Proto.Response();
                }
                return _package.Response;
            }
        }

        public void Send()
        {
            if (_package != null)
            {
                Send(_package);
                _package = null;
            }
        }
        #endregion

        #region 发送网络数据包（异步）

        /// <summary>
        /// 发送数据包
        /// </summary>
        /// <param name="package"></param>
        public void Send(Proto.Package package) 
        {
            byte[] data = null;
            //将package写到内存流中去
            //MemoryStream本质上是内存中的字节流容器
            //加了using，用完之后会自动关闭该流。
            using (MemoryStream ms = new MemoryStream()) 
            {

                package.WriteTo(ms);

                #region 对消息进行编码
                data = new byte[4 +  ms.Length];
                //上面这段代码只是定义了data数组的结构，现在需要为该数组填充内容
                //给前四个字节填充数据：
                Buffer.BlockCopy(BitConverter.GetBytes(ms.Length), 0, data, 0, 4);
                //给后面填充数据
                Buffer.BlockCopy(ms.GetBuffer(),0,data,4,(int)ms.Length);
                #endregion
            }
            Send(data,0,data.Length);
        }

        /// <summary>
        /// 发送字节流(异步)
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public void Send(byte[] buffer,int offset,int count)
        {
            //加锁，保证多线程情况下同一时刻只能有一个线程访问Send方法，其他的都处于等待队列中
            lock(this)
            {
                if (socket.Connected)
                {
                    //虽然该方法是异步发送，但放到缓冲区的时机也是有先后顺序的
                    socket.BeginSend(buffer, offset, count, SocketFlags.None, new AsyncCallback(SendCallback), socket);
                }
            }
        }

        /// <summary>
        /// 发送消息完成后触发该回调
        /// </summary>
        /// <param name="ar"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void SendCallback(IAsyncResult ar)
        {
            //获取发送字节数
            int len = socket.EndSend(ar);
        }
        #endregion

        #region 关闭连接
        /// <summary>
        /// 关闭连接
        /// </summary>
        public void Close()
        {
            try { socket.Shutdown(SocketShutdown.Both); } catch { }
            socket.Close();
            socket = null;
        }
        #endregion
    }
}
