using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace TaoTie
{
    public readonly struct RpcInfo
    {
        public readonly IRequest Request;
        public readonly ETTask<IResponse> Tcs;

        public RpcInfo(IRequest request)
        {
            this.Request = request;
            this.Tcs = ETTask<IResponse>.Create(true);
        }
    }

    
    public sealed class Session: IManager<AService,long>,IDisposable
    {
        public long GateId;
        public AService AService;
        public long Id;
        public bool IsDisposed;
        public static int RpcId
        {
            get;
            set;
        }

        public readonly Dictionary<int, RpcInfo> requestCallbacks = new Dictionary<int, RpcInfo>();
        
        public long LastRecvTime
        {
            get;
            set;
        }

        public long LastSendTime
        {
            get;
            set;
        }

        public int Error
        {
            get;
            set;
        }

        public IPEndPoint RemoteAddress
        {
            get;
            set;
        }

        public void Init(AService aService,long id)
        {
            IsDisposed = false;
            Id = id;
            this.AService = aService;
            long timeNow = TimeHelper.ClientNow();
            this.LastRecvTime = timeNow;
            this.LastSendTime = timeNow;

            this.requestCallbacks.Clear();
            
            Log.Info($"session create: id: {this.Id} {timeNow} ");
        }

        public void Destroy()
        {
            if(IsDisposed) return;
            IsDisposed = true;
            
            ManagerProvider.RemoveManager<PingComponent>(Id.ToString());
            ManagerProvider.RemoveManager<SwitchRouterComponent>(Id.ToString());
            ManagerProvider.RemoveManager<SessionIdleCheckerComponent>(Id.ToString());
            ManagerProvider.RemoveManager<SessionAcceptTimeoutComponent>(Id.ToString());
            
            this.AService.RemoveChannel(this.Id);
            
            foreach (RpcInfo responseCallback in this.requestCallbacks.Values.ToArray())
            {
                responseCallback.Tcs.SetException(new RpcException(this.Error, $"session dispose: {this.Id} {this.RemoteAddress}"));
            }

            Log.Info($"session dispose: {this.RemoteAddress} id: {this.Id} ErrorCode: {this.Error}, please see ErrorCode.cs! {TimeHelper.ClientNow()}");
            
            this.requestCallbacks.Clear();
        }

        public void Dispose()
        {
            ManagerProvider.RemoveManager<Session>(this.Id.ToString());
        }

        public void OnRead(ushort opcode, IResponse response)
        {
            OpcodeHelper.LogMsg(0, opcode, response);
            
            if (!this.requestCallbacks.TryGetValue(response.RpcId, out var action))
            {
                return;
            }

            this.requestCallbacks.Remove(response.RpcId);
            if (ErrorCore.IsRpcNeedThrowException(response.Error))
            {
                action.Tcs.SetException(new Exception($"Rpc error, request: {action.Request} response: {response}"));
                return;
            }
            action.Tcs.SetResult(response);
        }
        
        public async ETTask<IResponse> Call(IRequest request, ETCancellationToken cancellationToken)
        {
            int rpcId = ++Session.RpcId;
            RpcInfo rpcInfo = new RpcInfo(request);
            this.requestCallbacks[rpcId] = rpcInfo;
            request.RpcId = rpcId;

            this.Send(request);
            
            void CancelAction()
            {
                if (!this.requestCallbacks.TryGetValue(rpcId, out RpcInfo action))
                {
                    return;
                }

                this.requestCallbacks.Remove(rpcId);
                Type responseType = OpcodeTypeComponent.Instance.GetResponseType(action.Request.GetType());
                IResponse response = (IResponse) Activator.CreateInstance(responseType);
                response.Error = ErrorCore.ERR_Cancel;
                action.Tcs.SetResult(response);
            }

            IResponse ret;
            try
            {
                cancellationToken?.Add(CancelAction);
                ret = await rpcInfo.Tcs;
            }
            finally
            {
                cancellationToken?.Remove(CancelAction);
            }
            return ret;
        }

        public async ETTask<IResponse> Call(IRequest request)
        {
            int rpcId = ++Session.RpcId;
            RpcInfo rpcInfo = new RpcInfo(request);
            this.requestCallbacks[rpcId] = rpcInfo;
            request.RpcId = rpcId;
            this.Send(request);
            return await rpcInfo.Tcs;
        }

        public void Reply(IResponse message)
        {
            this.Send(0, message);
        }

        public void Send(IMessage message)
        {
            this.Send(0, message);
        }
        
        public void Send(long actorId, IMessage message)
        {
            (ushort opcode, MemoryStream stream) = MessageSerializeHelper.MessageToStream(message);
            OpcodeHelper.LogMsg(0, opcode, message);
            this.Send(actorId, stream);
        }
        
        public void Send(long actorId, MemoryStream memoryStream)
        {
            this.LastSendTime = TimeHelper.ClientNow();
            this.AService.SendStream(this.Id, actorId, memoryStream);
        }
    }
}