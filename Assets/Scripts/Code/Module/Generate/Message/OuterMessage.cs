using System;
using ProtoBuf;


namespace TaoTie
{
    /// <summary>
    /// 下面代码需要自己写生成器
    /// </summary>
    public static partial class OuterOpcode
    {
        public const ushort C2G_Ping = 10018;
        public const ushort G2C_Ping = 10019;
        public const ushort C2R_Login = 10023;
        public const ushort R2C_Login = 10024;
        public const ushort C2G_LoginGate = 10025;
        public const ushort G2C_LoginGate = 10026;
    }


    [ResponseType(nameof(R2C_Login))]
    [Message(OuterOpcode.C2R_Login)]
    [ProtoContract]
    public partial class C2R_Login : Object, IRequest
    {
        [ProtoMember(90)] public int RpcId { get; set; }

        [ProtoMember(1)] public string Account { get; set; }

        [ProtoMember(2)] public string Password { get; set; }

    }

    [Message(OuterOpcode.R2C_Login)]
    [ProtoContract]
    public partial class R2C_Login : Object, IResponse
    {
        [ProtoMember(90)] public int RpcId { get; set; }

        [ProtoMember(91)] public int Error { get; set; }

        [ProtoMember(92)] public string Message { get; set; }

        [ProtoMember(1)] public string Address { get; set; }

        [ProtoMember(2)] public long Key { get; set; }

        [ProtoMember(3)] public long GateId { get; set; }

    }
    [ResponseType(nameof(G2C_Ping))]
    [Message(OuterOpcode.C2G_Ping)]
    [ProtoContract]
    public partial class C2G_Ping: Object, IRequest
    {
        [ProtoMember(90)]
        public int RpcId { get; set; }

    }

    [Message(OuterOpcode.G2C_Ping)]
    [ProtoContract]
    public partial class G2C_Ping: Object, IResponse
    {
        [ProtoMember(90)]
        public int RpcId { get; set; }

        [ProtoMember(91)]
        public int Error { get; set; }

        [ProtoMember(92)]
        public string Message { get; set; }

        [ProtoMember(1)]
        public long Time { get; set; }

    }
    
    [ResponseType(nameof(G2C_LoginGate))]
    [Message(OuterOpcode.C2G_LoginGate)]
    [ProtoContract]
    public partial class C2G_LoginGate: Object, IRequest
    {
        [ProtoMember(90)]
        public int RpcId { get; set; }

        [ProtoMember(1)]
        public long Key { get; set; }

        [ProtoMember(2)]
        public long GateId { get; set; }

    }

    [Message(OuterOpcode.G2C_LoginGate)]
    [ProtoContract]
    public partial class G2C_LoginGate: Object, IResponse
    {
        [ProtoMember(90)]
        public int RpcId { get; set; }

        [ProtoMember(91)]
        public int Error { get; set; }

        [ProtoMember(92)]
        public string Message { get; set; }

        [ProtoMember(1)]
        public long PlayerId { get; set; }

    }
}