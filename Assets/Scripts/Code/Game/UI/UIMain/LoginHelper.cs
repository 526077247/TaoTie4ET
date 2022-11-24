using System;


namespace TaoTie
{
    public static class LoginHelper
    {
        public static async ETTask Login(string address, string account, string password,Action onError=null)
        {
            try
            {
                // 创建一个ETModel层的Session
                R2C_Login r2CLogin;
                Session session = null;
                try
                {
                    session = ManagerProvider.GetManager<NetKcpComponent>().Create(NetworkHelper.ToIPEndPoint(address));
                    {
                        r2CLogin = (R2C_Login) await session.Call(new C2R_Login() { Account = account, Password = password });
                    }
                }
                finally
                {
                    session?.Dispose();
                }

                long channelId = RandomHelper.RandInt64();
                var routercomponent =ManagerProvider.RegisterManager<GetRouterComponent, long, long>(r2CLogin.GateId, channelId);
                string routerAddress = await routercomponent.Tcs;
                if (routerAddress == "")
                {
                    ManagerProvider.RemoveManager<GetRouterComponent>();
                    throw new Exception("routerAddress 失败");
                }
                Log.Debug("routerAddress 获取成功:" + routerAddress);
                ManagerProvider.RemoveManager<GetRouterComponent>();
                // 创建一个gate Session,并且保存到SessionComponent中
                Session gateSession = ManagerProvider.GetManager<NetKcpComponent>().Create(channelId, NetworkHelper.ToIPEndPoint(routerAddress));
                ManagerProvider.RegisterManager<RouterDataComponent>(gateSession.Id.ToString()).Gateid = r2CLogin.GateId;
                ManagerProvider.RegisterManager<PingComponent, Session>(gateSession, gateSession.Id.ToString());

                ManagerProvider.RegisterManager<SessionComponent>().Session = gateSession;
				
                G2C_LoginGate g2CLoginGate = (G2C_LoginGate)await gateSession.Call(
                    new C2G_LoginGate() { Key = r2CLogin.Key, GateId = r2CLogin.GateId});

                Log.Debug("登陆gate成功!");
                
            }
            catch (Exception e)
            {
                onError?.Invoke();
                Log.Error(e);
            }
        } 
    }
}