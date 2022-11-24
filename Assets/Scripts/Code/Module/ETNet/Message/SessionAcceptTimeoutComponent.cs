using System;

namespace TaoTie
{
    public class SessionAcceptTimeoutComponent: IManager<Session>
    {
        public long Timer;
        public Session Session;
        [Timer(TimerType.SessionAcceptTimeout)]
        public class SessionAcceptTimeout: ATimer<SessionAcceptTimeoutComponent>
        {
            public override void Run(SessionAcceptTimeoutComponent self)
            {
                try
                {
                    self.Session.Dispose();
                }
                catch (Exception e)
                {
                    Log.Error($"move timer error: {self.Session.Id}\n{e}");
                }
            }
        }

        public void Init(Session session)
        {
            Session = session;
            this.Timer = TimerManager.Instance.NewOnceTimer(TimeHelper.ServerNow() + 5000, TimerType.SessionAcceptTimeout, this);
        }

        public void Destroy()
        {
            TimerManager.Instance.Remove(ref this.Timer);
        }
    }
}