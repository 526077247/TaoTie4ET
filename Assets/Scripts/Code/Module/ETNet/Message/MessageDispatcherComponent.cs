using System;
using System.Collections.Generic;


namespace TaoTie
{
    /// <summary>
    /// 消息分发组件
    /// </summary>
    public class MessageDispatcherComponent: IManager
    {
        public static MessageDispatcherComponent Instance
        {
            get;
            private set;
        }

        public readonly Dictionary<ushort, List<IMHandler>> Handlers = new Dictionary<ushort, List<IMHandler>>();

        public void Init()
        {
            MessageDispatcherComponent.Instance = this;
            this.Load();
        }

        public void Destroy()
        {
            MessageDispatcherComponent.Instance = null;
            this.Handlers.Clear();
        }
        
        public void Load()
        {
            this.Handlers.Clear();

            List<Type> types = AttributeManager.Instance.GetTypes(TypeInfo<MessageHandlerAttribute>.Type);

            foreach (Type type in types)
            {
                IMHandler iMHandler = Activator.CreateInstance(type) as IMHandler;
                if (iMHandler == null)
                {
                    Log.Error($"message handle {type.Name} 需要继承 IMHandler");
                    continue;
                }

                Type messageType = iMHandler.GetMessageType();
                ushort opcode = OpcodeTypeComponent.Instance.GetOpcode(messageType);
                if (opcode == 0)
                {
                    Log.Error($"消息opcode为0: {messageType.Name}");
                    continue;
                }

                this.RegisterHandler(opcode, iMHandler);
            }
        }

        public void RegisterHandler(ushort opcode, IMHandler handler)
        {
            if (!this.Handlers.ContainsKey(opcode))
            {
                this.Handlers.Add(opcode, new List<IMHandler>());
            }

            this.Handlers[opcode].Add(handler);
        }

        public void Handle(Session session, ushort opcode, object message)
        {
            List<IMHandler> actions;
            if (!this.Handlers.TryGetValue(opcode, out actions))
            {
                Log.Error($"消息没有处理: {opcode} {message}");
                return;
            }

            foreach (IMHandler ev in actions)
            {
                try
                {
                    ev.Handle(session, message);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }
    }
}