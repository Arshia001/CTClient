using LightMessage.Common.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightMessage.Common.ProtocolMessages
{
#if !UNITY
    [Orleans.Concurrency.Immutable]
#endif
    public class AuthResponseMessage : AuthMessageBase
    {
        public static AuthResponseMessage ReadFrom(Message Message)
        {
            var SessionId = (Message.Params[1] as ParamGuid)?.Value;
            var KeepAliveTime = (Message.Params[2] as ParamTimeSpan)?.Value;
            var KeepAliveTimeout = (Message.Params[3] as ParamTimeSpan)?.Value;
            var ReconnectTimeout = (Message.Params[4] as ParamTimeSpan)?.Value;
            var RequestAckTimeout = (Message.Params[5] as ParamTimeSpan)?.Value;

            if (SessionId.HasValue && KeepAliveTime.HasValue && KeepAliveTimeout.HasValue && ReconnectTimeout.HasValue && RequestAckTimeout.HasValue)
                return new AuthResponseMessage(SessionId.Value, KeepAliveTime.Value, KeepAliveTimeout.Value, ReconnectTimeout.Value, RequestAckTimeout.Value);

            return null;
        }


        public Guid SessionId { get; }
        public TimeSpan KeepAliveTime { get; }
        public TimeSpan KeepAliveTimeout { get; }
        public TimeSpan ReconnectTimeout { get; }
        public TimeSpan RequestAckTimeout { get; }


        public AuthResponseMessage(Guid SessionId, TimeSpan KeepAliveTime, TimeSpan KeepAliveTimeout, TimeSpan ReconnectTimeout, TimeSpan RequestAckTimeout)
        {
            this.SessionId = SessionId;
            this.KeepAliveTime = KeepAliveTime;
            this.KeepAliveTimeout = KeepAliveTimeout;
            this.ReconnectTimeout = ReconnectTimeout;
            this.RequestAckTimeout = RequestAckTimeout;
        }

        public override MessageType GetMessageType()
        {
            return MessageType.AuthResponse;
        }

        protected override IEnumerable<Param> GetSerializationParams()
        {
            return new Param[] { Param.Guid(SessionId), Param.TimeSpan(KeepAliveTime), Param.TimeSpan(KeepAliveTimeout), Param.TimeSpan(ReconnectTimeout), Param.TimeSpan(RequestAckTimeout) };
        }
    }
}
