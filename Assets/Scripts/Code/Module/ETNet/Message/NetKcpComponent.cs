using System;
using System.IO;
using System.Net;

namespace TaoTie
{

    public class NetKcpComponent: IManager<int>,IManager<IPEndPoint, int>
    {
        public AService Service;
        
        public int SessionStreamDispatcherType { get; set; }

        public void Init(int sessionStreamDispatcherType)
        {
            this.SessionStreamDispatcherType = sessionStreamDispatcherType;
            
            this.Service = new KService(NetThreadComponent.Instance.ThreadSynchronizationContext, ServiceType.Outer);
            this.Service.ErrorCallback += (channelId, error) => this.OnError(channelId, error);
            this.Service.ReadCallback += (channelId, Memory) => this.OnRead(channelId, Memory);

            NetThreadComponent.Instance.Add(this.Service);
        }

        public void Init(IPEndPoint address, int sessionStreamDispatcherType)
        {
            this.SessionStreamDispatcherType = sessionStreamDispatcherType;
            
            this.Service = new KService(NetThreadComponent.Instance.ThreadSynchronizationContext, address, ServiceType.Outer);
            this.Service.ErrorCallback += (channelId, error) => this.OnError(channelId, error);
            this.Service.ReadCallback += (channelId, Memory) => this.OnRead(channelId, Memory);
            this.Service.AcceptCallback += (channelId, IPAddress) => this.OnAccept(channelId, IPAddress);

            NetThreadComponent.Instance.Add(this.Service);
        }

        public void Destroy()
        {
            NetThreadComponent.Instance.Remove(this.Service);
            this.Service.Destroy();
        }
        
        public void OnRead(long channelId, MemoryStream memoryStream)
        {
            Session session = this.GetChild(channelId);
            if (session == null)
            {
                return;
            }

            session.LastRecvTime = TimeHelper.ClientNow();
            SessionStreamDispatcher.Instance.Dispatch(this.SessionStreamDispatcherType, session, memoryStream);
        }

        public void OnError(long channelId, int error)
        {
            Session session = this.GetChild(channelId);
            if (session == null)
            {
                return;
            }

            session.Error = error;
            session.Dispose();
        }

        // 这个channelId是由CreateAcceptChannelId生成的
        public void OnAccept(long channelId, IPEndPoint ipEndPoint)
        {
            Session session = this.AddChildWithId(channelId, this.Service);
            session.RemoteAddress = ipEndPoint;
           
            // 挂上这个组件，5秒就会删除session，所以客户端验证完成要删除这个组件。该组件的作用就是防止外挂一直连接不发消息也不进行权限验证
            ManagerProvider.RegisterManager<SessionAcceptTimeoutComponent, Session>(session);
            // 客户端连接，2秒检查一次recv消息，10秒没有消息则断开
            ManagerProvider.RegisterManager<SessionIdleCheckerComponent, Session, int>(session,
                NetThreadComponent.checkInteral);
        }

        public Session Get(long id)
        {
            Session session = this.GetChild(id);
            return session;
        }

        public Session Create(IPEndPoint realIPEndPoint)
        {
            long channelId = RandomHelper.RandInt64();
            Session session = this.AddChildWithId(channelId, this.Service);
            session.RemoteAddress = realIPEndPoint;
            ManagerProvider.RegisterManager<SessionIdleCheckerComponent, Session, int>(session,
                NetThreadComponent.checkInteral);
            
            this.Service.GetOrCreate(session.Id, realIPEndPoint);

            return session;
        }

        /// <summary>
        /// 路由用.需要提前生成localconn
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="realIPEndPoint"></param>
        /// <returns></returns>
        public Session Create(long channelId, IPEndPoint realIPEndPoint)
        {
            Session session = this.AddChildWithId(channelId, this.Service);
            session.RemoteAddress = realIPEndPoint;
            ManagerProvider.RegisterManager<SessionIdleCheckerComponent, Session, int>(session,
                NetThreadComponent.checkInteral);
            this.Service.GetOrCreate(session.Id, realIPEndPoint);
            return session;
        }


        public Session GetChild(long id)
        {
            return ManagerProvider.GetManager<Session>(id.ToString());
        }

        public Session AddChildWithId(long id, AService service)
        {
            return ManagerProvider.RegisterManager<Session,AService,long>(service,id,id.ToString());
        }
    }
}