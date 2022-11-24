using System;

namespace TaoTie
{
    public class PingComponent: IManager<Session>
    {

        public C2G_Ping C2G_Ping = new C2G_Ping();

        public long Ping; //延迟值

        private Session Session;

        public void Destroy()
        {
            this.Ping = default;
            Session = null;
        }

        public void Init(Session p1)
        {
            Session = p1;
            PingAsync().Coroutine();
        }
        
        private async ETTask PingAsync()
        {
            Session session = Session;

            while (true)
            {
                if (session.IsDisposed)
                {
                    return;
                }

                long time1 = TimeHelper.ClientNow();
                try
                {
                    G2C_Ping response = await session.Call(this.C2G_Ping) as G2C_Ping;

                    if (session.IsDisposed)
                    {
                        return;
                    }

                    long time2 = TimeHelper.ClientNow();
                    this.Ping = time2 - time1;
                    
                    TimeInfo.Instance.ServerMinusClientTime = response.Time + (time2 - time1) / 2 - time2;

                    await TimerManager.Instance.WaitAsync(2000);
                }
                catch (RpcException e)
                {
                    // session断开导致ping rpc报错，记录一下即可，不需要打成error
                    Log.Info($"ping error: {this.Session.Id} {e.Error}");
                    return;
                }
                catch (Exception e)
                {
                    Log.Error($"ping error: \n{e}");
                }
            }
        }
    }
}