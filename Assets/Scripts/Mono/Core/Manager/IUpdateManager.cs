namespace TaoTie
{
    public interface IUpdateManager
    {
        public void Update();
    }
    
    public interface ILateUpdateManager:IManager
    {
        public void LateUpdate();
    }
}