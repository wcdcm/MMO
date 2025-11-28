using System;
using System.Net.Sockets;
using System.Text;

namespace Network
{
    /// <summary>
    /// 基于长度字段(LengthField)的解码器。
    /// 这是Socket异步接收器，可以对接收的数据粘包与拆包
    /// 事件委托：
    ///     -- DataReceived 数据包接收完成事件，参数为接收的数据包
    ///     -- Disconnected 接收异常事件
    /// </summary>
    public class LengthFieldDecoder
    {

        private Socket mSocket;

        /// <summary>
        /// 长度字段位置下标
        /// </summary>
        private int lengthFieldOffset = 8;  

        /// <summary>
        /// 长度字段本身长度，只支持1、2、4、8
        /// </summary>
        private int lengthFieldLength = 4;

        /// <summary>
        /// 偏移位，长度字段和消息内容之间距离几个字节。
        /// 负数代表向前偏移，body实际长度要减去这个绝对值
        /// </summary>
        private int lengthAdjustment = 0;

        /// <summary>
        /// 完整获取一个数据包之后，需要舍弃前面的几个字节
        /// </summary>
        private int initialBytesToStrip = 0;

        private byte[] mBuffer;   //接收数据的缓存空间
        private int mOffect;    //读取位置

        /// <summary>
        ///	一次性接收数据的最大字节，默认1MB
        /// </summary>
        private int maxSize = 1024 * 1024;

        //成功收到消息的委托事件
        public delegate void OnReceived(byte[] data);
        public event OnReceived DataReceived;

        //连接失败的委托事件
        public delegate void OnDisconnectedEventHandler(Socket soc);
        public event OnDisconnectedEventHandler Disconnected;


        public LengthFieldDecoder(Socket socket, int lengthFieldOffset, int lengthFieldLength)
        {
            mSocket = socket;
            this.lengthFieldOffset = lengthFieldOffset;
            this.lengthFieldLength = lengthFieldLength;
            mBuffer = new byte[maxSize];
        }

        public LengthFieldDecoder(Socket socket, int maxBufferLength, int lengthFieldOffset, int lengthFieldLength,
            int lengthAdjustment, int initialBytesToStrip)
        {
            mSocket = socket;
            maxSize = maxBufferLength;
            this.lengthFieldOffset = lengthFieldOffset;
            this.lengthFieldLength = lengthFieldLength;
            this.lengthAdjustment = lengthAdjustment;
            this.initialBytesToStrip = initialBytesToStrip;
            mBuffer = new byte[maxSize];
        }


        public void Start()
        {
            BeginAsyncReceive();
        }

        public void BeginAsyncReceive()
        {
            //Debug.Log("开始接收");
            mSocket.BeginReceive(mBuffer, mOffect, maxSize - mOffect, SocketFlags.None, new AsyncCallback(Receive), null);
        }

        public void Receive(IAsyncResult result)
        {
            try{
                int len = mSocket.EndReceive(result);
                // 0代表连接失败
                if (len == 0)
                {
                    _disconnected();
                    return;
                }

                //headLen+bodyLen=totalLen
                int headLen = lengthFieldOffset + lengthFieldLength;
                int adj = lengthAdjustment; //body偏移量

                //size是待处理的数据长度，mOffect每次都从0开始，
                //循环开始之前mOffect代表上次剩余长度
                int size = len;
                if (mOffect > 0)
                {
                    size += mOffect;
                    mOffect = 0;
                }
                //循环解析
                while (true)
                {
                    int remain = size - mOffect;//剩余未处理的长度
                    //Debug.Log("剩余未处理的长度：" + remain);
                    //Debug.Log("remain=" + remain);
                    //如果未处理的数据超出限制
                    if (remain > maxSize)
                    {
                        throw new IndexOutOfRangeException("数据超出限制");
                        //mOffect = 0;
                        //BeginAsyncReceive();
                        //return;
                    }
                    if (remain < headLen)
                    {
                        //接收的数据不够一个完整的包，继续接收
                        Array.Copy(mBuffer, mOffect, mBuffer, 0, remain);
                        mOffect = remain;
                        BeginAsyncReceive();
                        return;
                    }

                    //获取包长度
                    int bodyLen = BitConverter.ToInt32(mBuffer, mOffect + lengthFieldOffset);
                    if (remain < headLen + adj + bodyLen)
                    {
                        //接收的数据不够一个完整的包，继续接收
                        Array.Copy(mBuffer, mOffect, mBuffer, 0, remain);
                        mOffect = remain;
                        BeginAsyncReceive();
                        return;
                    }

                    //body的读取位置
                    int bodyStart = mOffect + Math.Max(headLen, headLen + adj);
                    //body的真实长度
                    int bodyCount = Math.Min(bodyLen, bodyLen + adj);
                    //Debug.Log("bodyStart=" + bodyStart + ", bodyCount=" + bodyCount+ ", remain=" + remain);

                    //获取包体
                    int total = headLen + adj + bodyLen; //数据包总长度
                    int count = total - initialBytesToStrip;
                    byte[] data = new byte[count];
                    Array.Copy(mBuffer, mOffect + initialBytesToStrip, data, 0, count);
                    mOffect += total;

                    //完成一个数据包
                    DataReceived?.Invoke(data);
                    //Debug.Log("完成一个数据包");
                }

            }
            catch (SocketException)
            {
                _disconnected();
            }
            catch (ObjectDisposedException)
            {
                _disconnected();
            }
            
        }

        private void _disconnected()
        {
            Disconnected?.Invoke(mSocket);
        }
    }
}
