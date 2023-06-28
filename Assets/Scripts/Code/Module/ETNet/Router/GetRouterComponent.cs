using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
namespace TaoTie
{
    /// <summary>
    /// 初始获取路由组件
    /// </summary>
    public class GetRouterComponent :IManager<long,long>,IUpdate
    {
        public int ChangeTimes;
        public Socket socket;
        public readonly byte[] cache = new byte[8192];
        public EndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, 0);
        public ETCancellationToken CancellationToken;
        public ETTask<string> Tcs;
        public bool IsChangingRouter;

        public void Init()
        {
            
        }

        public void Init(long gateid, long channelid)
        {
            this.ipEndPoint = new IPEndPoint(IPAddress.Any, 0);
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            // 作为客户端不需要修改发送跟接收缓冲区大小
            this.socket.Bind(new IPEndPoint(IPAddress.Any, 0));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                const uint IOC_IN = 0x80000000;
                const uint IOC_VENDOR = 0x18000000;
                uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
                this.socket.IOControl((int)SIO_UDP_CONNRESET, new[] { Convert.ToByte(false) }, null);
            }
            this.ChangeTimes = 3;
            SynAsync(gateid, channelid).Coroutine();
        }

        public void Update()
        {
            Recv();
        }

        public void Destroy()
        {
            this.CancellationToken?.Cancel();
            this.CancellationToken = null;
            this.ChangeTimes = 0;
            this.socket.Dispose();
            this.socket = null;
            this.ipEndPoint = null;
            this.Tcs = null;
        }

        /// <summary>
        /// 应从cdn获取
        /// </summary>
        /// <returns></returns>
        static async ETTask<string[]> GetRouterList()
        {
            return await HttpManager.Instance.HttpGetResult<string[]>(ServerConfigManager.Instance.GetCurConfig().RouterListUrl+ "/router.list");
        }
        private async ETTask SynAsync(long gateid, long channelid)
        {
            this.CancellationToken = new ETCancellationToken();
            this.Tcs = ETTask<string>.Create();
            //value是对应gate的scene.
            var insid = new InstanceIdStruct(gateid);
            uint localConn = (uint)((ulong)channelid & uint.MaxValue);
            var routerlist = await GetRouterList();
            if (routerlist == null)
            {
                var tcs = this.Tcs;
                this.Tcs = null;
                tcs?.SetResult("");
                Log.Error("从cdn获取路由失败");
                return;
            }
            Log.Debug("路由数量:" + routerlist.Length.ToString());
            Log.Debug("gateid:" + insid.Value.ToString());
            byte[] buffer = this.cache;
            buffer.WriteTo(0, KcpProtocalType.RouterSYN);
            buffer.WriteTo(1, localConn);
            buffer.WriteTo(5, insid.Value);
            for (int i = 0; i < this.ChangeTimes; i++)
            {
                string router = routerlist.RandomArray();
                Log.Debug("router:" + router);
                this.socket.SendTo(buffer, 0, 9, SocketFlags.None, NetworkHelper.ToIPEndPoint(router));
                var returnbool = await TimerManager.Instance.WaitAsync(300, this.CancellationToken);
                if (returnbool == false)
                {
                    Log.Debug("提前取消了.可能连接上了");
                    return;
                }
            }
            await TimerManager.Instance.WaitAsync(1300, this.CancellationToken);
            var tcss = this.Tcs;
            this.Tcs = null;
            tcss?.SetResult("");
            Log.Debug("三次失败.获取路由失败");
        }
        
        public void Recv()
        {
            if (this.socket == null)
            {
                return;
            }

            while (this.socket != null && this.socket.Available > 0)
            {
                int messageLength = this.socket.ReceiveFrom(this.cache, ref this.ipEndPoint);

                // 长度小于1，不是正常的消息
                if (messageLength < 1)
                {
                    continue;
                }
                byte flag = this.cache[0];
                try
                {
                    switch (flag)
                    {
                        case KcpProtocalType.RouterACK:
                            Log.Debug("RouterACK:"+ this.ipEndPoint.ToString());
                            this.Tcs?.SetResult(this.ipEndPoint.ToString());
                            this.Tcs = null;
                            this.CancellationToken?.Cancel();
                            break;
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"kservice error: {flag}\n{e}");
                }
            }
        }
    }

}
