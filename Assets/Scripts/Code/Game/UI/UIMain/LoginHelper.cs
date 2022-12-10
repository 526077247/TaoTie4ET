using System;


namespace TaoTie
{
    public static class LoginHelper
    {
        [Timer(TimerType.LoginTimeOut)]
        public class LoginTimeOut: ATimer<ETCancellationToken>
        {
            public override void Run(ETCancellationToken cancel)
            {
                try
                {
                    cancel.Cancel();
                    Log.Info("Login Time Out");
                }
                catch (Exception e)
                {
                    Log.Error($"move timer error: LoginTimeOut\n{e}");
                }
            }
        }
        public static async ETTask<bool> Login(string address, string account, string password)
        {
            try
            {
                // 创建一个ETModel层的Session
                R2C_Login r2CLogin;
                Session session = null;
                long timerId = 0;
                try
                {
                    session = ManagerProvider.GetManager<NetKcpComponent>().Create(NetworkHelper.ToIPEndPoint(address));
                    ETCancellationToken cancel = new ETCancellationToken();
                    timerId = TimerManager.Instance.NewOnceTimer(TimeInfo.Instance.ClientNow()+10000,TimerType.LoginTimeOut, cancel);
                    r2CLogin = (R2C_Login) await session.Call(new C2R_Login() { Account = account, Password = password },cancel);
                }
                finally
                {
                    session?.Dispose();
                }
                TimerManager.Instance.Remove(ref timerId);
                long channelId = RandomHelper.RandInt64();
                var routercomponent = ManagerProvider.RegisterManager<GetRouterComponent, long, long>(r2CLogin.GateId, channelId);
                string routerAddress = await routercomponent.Tcs;
                if (routerAddress == "")
                {
                    ManagerProvider.RemoveManager<GetRouterComponent>();
                    throw new Exception("routerAddress 失败");
                }
                Log.Debug("routerAddress 获取成功:" + routerAddress);
                ManagerProvider.RemoveManager<GetRouterComponent>();
                // 创建一个gate Session,并且保存到SessionComponent中
                Session gateSession = ManagerProvider.GetManager<NetKcpComponent>().Create(channelId,r2CLogin.GateId, NetworkHelper.ToIPEndPoint(routerAddress));
                ManagerProvider.RegisterManager<PingComponent, Session>(gateSession, gateSession.Id.ToString());

                SessionComponent.Instance.Session = gateSession;
				
                G2C_LoginGate g2CLoginGate = (G2C_LoginGate)await gateSession.Call(
                    new C2G_LoginGate() { Key = r2CLogin.Key, GateId = r2CLogin.GateId});

                Log.Debug("登陆gate成功!");
                
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            return false;
        } 
    }
}