using System;

namespace TaoTie
{
    public class SessionIdleCheckerComponent: IManager<Session,int>
    {
        public long RepeatedTimer;
        public Session Session;
        [Timer(TimerType.SessionIdleChecker)]
        public class SessionIdleChecker: ATimer<SessionIdleCheckerComponent>
        {
            public override void Run(SessionIdleCheckerComponent self)
            {
                try
                {
                    self.Check();
                }
                catch (Exception e)
                {
                    Log.Error($"move timer error: {self.Session.Id}\n{e}");
                }
            }
        }
        
        public void Init(Session session,int checkInteral)
        {
            Session = session;
            this.RepeatedTimer = TimerManager.Instance.NewRepeatedTimer(checkInteral, TimerType.SessionIdleChecker, this);
        }

        public void Destroy()
        {
            TimerManager.Instance.Remove(ref this.RepeatedTimer);
        }
        
        public void Check()
        {
            long timeNow = TimeHelper.ClientNow();
            if (timeNow - Session.LastRecvTime > 6 * 1000)
            {
                var comp = ManagerProvider.GetManager<SwitchRouterComponent>(this.Session.Id.ToString());
                if (comp == null)
                {
                    ManagerProvider.RegisterManager<SwitchRouterComponent,Session>(this.Session,this.Session.Id.ToString());
                }
            }
        }
    }
}