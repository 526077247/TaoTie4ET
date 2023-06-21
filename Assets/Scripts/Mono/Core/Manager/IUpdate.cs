namespace TaoTie
{
    public interface IUpdate
    {
        public void Update();
    }
    
    public interface ILateUpdateManager:IManager
    {
        public void LateUpdate();
    }
}