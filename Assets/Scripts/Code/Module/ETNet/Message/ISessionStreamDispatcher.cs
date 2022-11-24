using System.IO;

namespace TaoTie
{
    public interface ISessionStreamDispatcher
    {
        void Dispatch(Session session, MemoryStream stream);
    }
}