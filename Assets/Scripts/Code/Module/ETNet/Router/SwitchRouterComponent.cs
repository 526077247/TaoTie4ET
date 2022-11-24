using System;
using System.Net;
using System.Net.Sockets;
using System.Linq;
namespace TaoTie
{
    /// <summary>
    /// 在session上挂载的保存路由信息的组件.切换路由用
    /// </summary>
    public class RouterDataComponent : IManager
    {
        public long Gateid;
        public void Init()
        {
            
        }

        public void Destroy()
        {
            
        }
    }
    /// <summary>
    /// 切换路由组件
    /// </summary>
    public class SwitchRouterComponent : IManager<Session>
    {
        public Session Session;
        public void Init(Session session)
        {
            Session = session;
            ChangeRouter().Coroutine();
        }

        public void Destroy()
        {
            
        }

        public async ETTask ChangeRouter()
        {
            ManagerProvider.RemoveManager<SessionIdleCheckerComponent>();
            var gateid = ManagerProvider.GetManager<RouterDataComponent>(Session.Id.ToString()).Gateid;

            var routercomponent = ManagerProvider.RegisterManager<GetRouterComponent, long, long>(gateid, Session.Id,Session.Id.ToString());
            string routerAddress = await routercomponent.Tcs;
            ManagerProvider.RemoveManager<GetRouterComponent>(Session.Id.ToString());
            if (routerAddress == "")
            {
                Session.Dispose();
                return;
            }
            (Session.AService as KService).ChangeAddress(Session.Id, NetworkHelper.ToIPEndPoint(routerAddress));
            Session.LastRecvTime = TimeHelper.ClientNow();
            ManagerProvider.RegisterManager<SessionIdleCheckerComponent,TaoTie.Session, int>(Session,NetThreadComponent.checkInteral,Session.Id.ToString());
            ManagerProvider.RemoveManager<SwitchRouterComponent>();
        }
    }

}
