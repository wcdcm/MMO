using Common;
using Google.Protobuf;

namespace Common.Network
{
    #region 消息单元
    /// <summary>
    /// 消息单元；包含消息发送者和消息本身
    /// </summary>
    #endregion
    public class MessageUnit
    {
        public NetConnection sender;

        //所有不同的消息都有一个共同的父类，叫IMessage
        public Google.Protobuf.IMessage message;
    }

    #region 消息分发器
    /// <summary>
    /// 消息分发器
    /// </summary>
    #endregion
    public class MessageRouter:Singleton<MessageRouter>
    {

        #region 消息队列
        /// <summary>
        /// 消息队列。存储所有 客户端/服务端 发来的消息
        /// </summary>
        #endregion
        private Queue<MessageUnit> messageQueue = new Queue<MessageUnit>();

        #region 消息处理器
        /// <summary>
        /// 消息处理器（消息频道）
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="sender">发送者</param>
        /// <param name="message">消息本体</param>
        #endregion
        public delegate void MessageHandler<T>(NetConnection sender, T message);

        #region 消息频道字典
        /// <summary>
        /// 消息频道字典。
        /// string是频道名字；Delegate是一个委托类型，是所有消息频道的父类型
        /// </summary>
        #endregion
        private Dictionary<string, Delegate> delegateMap = new Dictionary<string, Delegate>();

        #region 订阅频道
        /// <summary>
        /// 订阅频道
        /// </summary>
        /// <typeparam name="T">消息类型，必须是IMessage的子类型</typeparam>
        /// <param name="handler"></param>
        #endregion
        public void On<T>(MessageHandler<T> handler) where T : Google.Protobuf.IMessage
        {
            string msgType = typeof(T).Name;//获取传入的消息类型的名字
            
            //如果字典中不存在这么一个key，就创建它暂且将它赋值为null
            if (!delegateMap.ContainsKey(msgType)) 
            {
                delegateMap[msgType] = null;
            }

            //委托是可以组合的，通过+号形成一个委托链，当触发委托时，所有的注册者都会被触发
            delegateMap[msgType] = (MessageHandler<T>)delegateMap[msgType] + handler;

            Console.WriteLine(msgType +":" + delegateMap[msgType].GetInvocationList().Length);
        }

        #region 退订频道
        /// <summary>
        /// 退订频道
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        #endregion
        public void Off<T>(MessageHandler<T> handler) where T : Google.Protobuf.IMessage
        {
            string key = typeof(T).Name;//获取传入的消息类型的名字

            //如果字典中不存在这么一个key，就创建它暂且将它赋值为null
            if (!delegateMap.ContainsKey(key))
            {
                delegateMap[key] = null;
            }

            //委托是可以组合的，通过+号形成一个委托链，当触发委托时，所有的注册者都会被触发
            delegateMap[key] = (MessageHandler<T>)delegateMap[key] - handler;
        }

        #region 添加新的消息到队列中
        /// <summary>
        /// 添加新的消息到队列中
        /// </summary>
        /// <param name="sender">消息发送者</param>
        /// <param name="message">消息对象</param>
        #endregion
        public void AddMessage(NetConnection sender,Google.Protobuf.IMessage message)
        {
            var unit = new MessageUnit() { sender = sender, message = message };
            messageQueue.Enqueue(unit);
            Console.WriteLine("当前messageQueue的长度："+ messageQueue.Count);
        }
    }
}
