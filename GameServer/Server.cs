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

            MessageRouter.Instance.On<Vector3>(fff);
            MessageRouter.Instance.On<Vector3>(fff);
            MessageRouter.Instance.On<Vector3>(fff);

            Console.ReadKey();//等待输入。防止进程结束，因为一旦进程结束就无法继续进行监听了，所以需要让进程一直保持运行状态
        }
        
        static void fff(NetConnection handler,Vector3 message)
        {

        }
    }
}
