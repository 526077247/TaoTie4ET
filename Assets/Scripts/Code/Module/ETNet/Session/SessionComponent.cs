namespace TaoTie
{
    public class SessionComponent: IManager
    {
        public static SessionComponent Instance;
        public Session Session { get; set; }
        public void Init()
        {
            Instance = this;
        }

        public void Destroy()
        {
            Instance = null;
            Session.Dispose();
        }
    }
}