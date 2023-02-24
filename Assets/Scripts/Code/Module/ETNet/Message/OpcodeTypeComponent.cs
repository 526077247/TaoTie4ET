using System;
using System.Collections.Generic;

namespace TaoTie
{
   
    public class OpcodeTypeComponent: IManager
    {
        public static OpcodeTypeComponent Instance { get; private set; }
        
        public HashSet<ushort> outrActorMessage = new HashSet<ushort>();
        
        public readonly Dictionary<ushort, Type> opcodeTypes = new Dictionary<ushort, Type>();
        public readonly Dictionary<Type, ushort> typeOpcodes = new Dictionary<Type, ushort>();
        
        public readonly Dictionary<Type, Type> requestResponse = new Dictionary<Type, Type>();

        public void Init()
        {
            OpcodeTypeComponent.Instance = this;
                
                this.opcodeTypes.Clear();
                this.typeOpcodes.Clear();
                this.requestResponse.Clear();

                List<Type> types = AttributeManager.Instance.GetTypes(TypeInfo<MessageAttribute>.Type);
                foreach (Type type in types)
                {
                    object[] attrs = type.GetCustomAttributes(TypeInfo<MessageAttribute>.Type, false);
                    if (attrs.Length == 0)
                    {
                        continue;
                    }

                    MessageAttribute messageAttribute = attrs[0] as MessageAttribute;
                    if (messageAttribute == null)
                    {
                        continue;
                    }
                

                    this.opcodeTypes.Add(messageAttribute.Opcode, type);
                    this.typeOpcodes.Add(type, messageAttribute.Opcode);

                    if (OpcodeHelper.IsOuterMessage(messageAttribute.Opcode) && TypeInfo<IActorMessage>.Type.IsAssignableFrom(type))
                    {
                        this.outrActorMessage.Add(messageAttribute.Opcode);
                    }
                
                    // 检查request response
                    if (TypeInfo<IRequest>.Type.IsAssignableFrom(type))
                    {
                        if (TypeInfo<IActorLocationMessage>.Type.IsAssignableFrom(type))
                        {
                            this.requestResponse.Add(type, TypeInfo<ActorResponse>.Type);
                            continue;
                        }
                    
                        attrs = type.GetCustomAttributes(TypeInfo<ResponseTypeAttribute>.Type, false);
                        if (attrs.Length == 0)
                        {
                            Log.Error($"not found responseType: {type}");
                            continue;
                        }

                        ResponseTypeAttribute responseTypeAttribute = attrs[0] as ResponseTypeAttribute;
                        this.requestResponse.Add(type, AssemblyManager.Instance.GetType($"TaoTie.{responseTypeAttribute.Type}"));
                    }
                }
        }

        public void Destroy()
        {
            OpcodeTypeComponent.Instance = null;
        }
        
        public bool IsOutrActorMessage(ushort opcode)
        {
            return this.outrActorMessage.Contains(opcode);
        }

        public ushort GetOpcode(Type type)
        {
            return this.typeOpcodes[type];
        }

        public Type GetType(ushort opcode)
        {
            return this.opcodeTypes[opcode];
        }

        public Type GetResponseType(Type request)
        {
            if (!this.requestResponse.TryGetValue(request, out Type response))
            {
                throw new Exception($"not found response type, request type: {request.GetType().Name}");
            }
            return response;
        }
    }
}