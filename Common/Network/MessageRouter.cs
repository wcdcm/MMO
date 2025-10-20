using Common;
using Google.Protobuf;

namespace Common.Network
{
    #region 消息单元
    /// <summary>
    /// 消息单元；包含消息发送者和消息本身内容
    /// </summary>
    #endregion
    public class MessageUnit
    {
        public NetConnection sender;

        //所有不同的消息都有一个共同的父类，叫IMessage
        public Proto.Package message;
    }

    #region 消息分发器
    /// <summary>
    /// 消息分发器
    /// </summary>
    #endregion
    public class MessageRouter : Singleton<MessageRouter>
    {
        private int threadCount = 1;//开辟的线程总数
        private int workThreadCount = 0;//正在工作的线程数
        private bool Running = false;//MessageRouter运行状态

        #region 协调多线程间的通信
        /// <summary>
        /// <para>用于协调多线程间的通信.</para>
        /// <para>作用：</para>
        /// <para>休眠线程(threadEvent.WaitOne())</para>
        /// <para>唤醒线程(thread.Set())</para>
        /// </summary>
        #endregion
        AutoResetEvent threadEvent = new AutoResetEvent(true);

        #region 消息队列
        /// <summary>
        /// 消息队列。存储所有 客户端/服务端 发来的消息
        /// </summary>
        #endregion
        private Queue<MessageUnit> messageQueue = new Queue<MessageUnit>();

        #region 消息频道
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

            Console.WriteLine("消息类型：" + msgType + "、" +"委托链长度：" + delegateMap[msgType].GetInvocationList().Length);
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
        /// <param name="message">消息对象，一定是Package类型的</param>
        #endregion
        public void AddMessage(NetConnection sender, Proto.Package message)
        {
            //将消息和发送者绑定，作为一个消息单元填入消息队列
            var unit = new MessageUnit() { sender = sender, message = message };
            messageQueue.Enqueue(unit);

            //唤醒某一个休眠的线程，让它进入工作状态处理消息
            threadEvent.Set();

            Console.WriteLine("当前messageQueue的长度：" + messageQueue.Count);
        }

        #region 开启多线程（消费者模型）：
        public void Start(int threadCount)
        {
            Running = true;

            //限制线程的范围1 - 200：
            this.threadCount = Math.Min(threadCount, 200);
            this.threadCount = Math.Max(threadCount, 1);

            //创建线程池
            for (int i = 0; i < this.threadCount; i++)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(MessageWork));
            }

            //等待线程全部创建完成
            while (workThreadCount < this.threadCount)
            {
                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// 关闭消息分发器
        /// </summary>
        public void Stop()
        {
            Running = false;
            messageQueue.Clear();//清空消息队列

            #region 等待所有工人下线...
            //因为是多线程运行，
            //当关闭的一瞬间如果还有线程没处理完数据，即workThreadCount > 0时，
            //等待一定的时间，等该线程执行完毕
            #endregion
            while (workThreadCount > 0)
            {
                #region 唤醒线程...
                //保证没有一个线程处于休眠状态，保证代码的正常执行
                #endregion
                threadEvent.Set();
            }
            Thread.Sleep(100);
        }

        #region 每个线程独有的消息处理方法...
        /// <summary>
        /// <para>每个线程独有的消息处理方法</para>
        /// <para>只要有线程在运行，就会执行MessageWork中的代码</para>
        /// </summary>
        /// <param name="state"></param>
        #endregion
        private void MessageWork(object? state)
        {

            Console.WriteLine("work thread start");

            try
            {
                //让workThreadCount自增1（线程安全）
                Interlocked.Increment(ref workThreadCount);

                //不停地执行任务，直到分发器停止工作（Running == false）
                while (Running)
                {
                    if (messageQueue.Count == 0)
                    {
                        #region 线程休眠等待...
                        //线程休眠等待，代码会卡在这一句不再执行。
                        //可以使用AutoResetEvent类，通过Set()唤醒（这段唤醒代码是可以从别处执行的，这是.Net内部的处理）
                        //被唤醒后，从这一句继续往下执行
                        #endregion
                        threadEvent.WaitOne();

                        #region 为什么要用continue...
                        //唤醒后先进入下一次循环，
                        //先判断是否Running，如果没有在Running直接进入finally。
                        //如果在Running再判断一次messageQueue长度是否为0，
                        //如果为0 就继续等待。
                        //----------------------------------------------------
                        //为什么要这样做：
                        //因为唤醒需要一定时间，
                        //无法保证这段时间内MessageQueue里的消息是否被别的进程消费了
                        //也无法保证这个唤醒的来源到底是 添加消息的AddMessage方法 还是 要关闭转发器的Stop方法
                        //所以要再进行一次Running判断和messageCount判断
                        #endregion
                        continue;
                        
                    }
                    //从消息队列中取出一个消息单元：
                    MessageUnit pack = messageQueue.Dequeue();
                }

            }
            catch { }

            finally
            {
                Interlocked.Decrement(ref workThreadCount);
            }

            // 线程结束打印日志
            Console.WriteLine("work thread end");
        }
        #endregion
    }
}
