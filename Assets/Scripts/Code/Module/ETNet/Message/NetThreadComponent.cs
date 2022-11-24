using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace TaoTie
{

    public class NetThreadComponent: ILateUpdateManager
    {
        public static NetThreadComponent Instance { get; private set; }
        
        public const int checkInteral = 2000;
        public const int recvMaxIdleTime = 60000;
        public const int sendMaxIdleTime = 60000;

        public ThreadSynchronizationContext ThreadSynchronizationContext;
        
        public HashSet<AService> Services = new HashSet<AService>();

        public void Init()
        {
            NetThreadComponent.Instance = this;
            
            this.ThreadSynchronizationContext = ThreadSynchronizationContext.Instance;
        }

        public void Destroy()
        {
            NetThreadComponent.Instance = null;
            this.Stop();
        }

        public void LateUpdate()
        {
            foreach (var service in this.Services)
            {
                service.Update();
            }
        }
        
        public void Stop()
        {
        }

        public void Add(AService kService)
        {
            // 这里要去下一帧添加，避免foreach错误
            this.ThreadSynchronizationContext.PostNext(() =>
            {
                if (kService.IsDispose())
                {
                    return;
                }
                this.Services.Add(kService);
            });
        }
        
        public void Remove(AService kService)
        {
            // 这里要去下一帧删除，避免foreach错误
            this.ThreadSynchronizationContext.PostNext(() =>
            {
                if (kService.IsDispose())
                {
                    return;
                }
                this.Services.Remove(kService);
            });
        }

    }
}