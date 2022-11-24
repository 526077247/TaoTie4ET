namespace TaoTie
{
    public interface IUpdateManager:IManager
    {
        public void Update();
    }
    
    public interface ILateUpdateManager:IManager
    {
        public void LateUpdate();
    }
}