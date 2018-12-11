using LightMessage.Common.Messages;
using LightMessage.Common.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LightMessage.Common.ProtocolMessages
{
#if !UNITY
    [Orleans.Concurrency.Immutable]
#endif
    public abstract class MessageBase : Message
    {
        public static async new Task<MessageBase> ReadFrom(Stream Stream, int MaxMessageSize, int MaxLengthSectionBytes, CancellationToken CancellationToken)
        {
            var Msg = await Message.ReadFrom(Stream, MaxMessageSize, MaxLengthSectionBytes, CancellationToken);
            if (Msg == null)
                return null;

            return ReadFromMessage(Msg);
        }

        public static new MessageBase ReadFrom(ArraySegment<byte> Array)
        {
            return ReadFromMessage(Message.ReadFrom(Array));
        }

        static MessageBase ReadFromMessage(Message Message)
        {
            var TypeParam = Message.Params[0] as ParamUInt;

            if (TypeParam == null)
                return default(MessageBase);

            switch (TypeParam.Value)
            {
                case (int)MessageType.KeepAlive:
                    return KeepAliveMessage.ReadFrom(Message);

                case (int)MessageType.Invocation:
                    return InvocationMessage.ReadFrom(Message);

                case (int)MessageType.Ack:
                    return AckMessage.ReadFrom(Message);

                case (int)MessageType.Invocation_SuccessReply:
                    return InvocationSuccessReplyMessage.ReadFrom(Message);

                case (int)MessageType.Invocation_FailureReply:
                    return InvocationFailureReplyMessage.ReadFrom(Message);

                case (int)MessageType.RealTimeReliableRpc:
                    return RealTimeReliableRpcMessage.ReadFrom(Message);

                case (int)MessageType.RealTimeUnreliableRpc:
                    return RealTimeUnreliableRpcMessage.ReadFrom(Message);

                case (int)MessageType.Custom:
                    return CustomMessage.ReadFrom(Message);

                case (int)MessageType.AuthRequest:
                    return AuthRequestMessage.ReadFrom(Message);

                case (int)MessageType.AuthResponse:
                    return AuthResponseMessage.ReadFrom(Message);

                case (int)MessageType.AuthRejoin:
                    return AuthRejoinMessage.ReadFrom(Message);

                case (int)MessageType.AuthFailure:
                    return AuthFailureMessage.ReadFrom(Message);

                case (int)MessageType.Ready:
                    return ReadyMessage.ReadFrom(Message);

                case (int)MessageType.CleanDisconnect:
                    return CleanDisconnectMessage.ReadFrom(Message);
            }

            return default(MessageBase);
        }


        protected abstract IEnumerable<Param> GetSerializationParams();
        public abstract MessageType GetMessageType();


        protected MessageBase() { }

        protected MessageBase(IEnumerable<Param> Params) : base(Params) { }

        public override ArraySegment<byte> Serialize()
        {
            return SerializeInternal(
                new SingleEnumerable<Param>(Param.UInt((ulong)GetMessageType()))
                .Concat(GetSerializationParams())
                .Concat(_Params));
        }

        public override string ToString()
        {
            StringBuilder Result = new StringBuilder($"{GetMessageType()}Message[");
            bool bFirst = true;

            foreach (var Param in GetSerializationParams().Concat(_Params))
            {
                if (bFirst)
                    bFirst = false;
                else
                    Result.Append(',');
                Result.Append(Param.ToString());
            }

            Result.Append(']');

            return Result.ToString();
        }
    }
}
