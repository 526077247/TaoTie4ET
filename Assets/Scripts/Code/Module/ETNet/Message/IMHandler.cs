using System;

namespace TaoTie
{
    public interface IMHandler
    {
        void Handle(Session session, object message);
        Type GetMessageType();

        Type GetResponseType();
    }
}