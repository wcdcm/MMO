using Common.Network;
using GameServer.Network;
using Network;
using Proto;
using System.Net;
using System.Net.Sockets;

namespace GameServer
{
    internal class Server
    {
        static void Main(string[] args)
        {
            #region 启动网络服务
            NetService netService = new NetService();
            netService.Init(32510);
            netService.Start();
            #endregion

    
            MessageRouter.Instance.Start(8);

            //消息订阅测试：用户注册消息订阅
            MessageRouter.Instance.On<Proto.UserRegisterRequest>(OnUserRegisterRequest);

            Console.ReadKey();//等待输入。防止进程结束，因为一旦进程结束就无法继续进行监听了，所以需要让进程一直保持运行状态
        }

        //当消息分发器发现了UserRegisterRequest这个消息之后，就会回调OnUserRegisterRequest方法
        private static void OnUserRegisterRequest(NetConnection sender, UserRegisterRequest message)
        {
            Console.WriteLine("发现用户注册请求");
            Console.WriteLine("用户名：" + message.Username);
            Console.WriteLine("密码：" + message.Password);
            
        }
    }
}
