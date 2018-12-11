using LightMessage.Common.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LightMessage.Common.Util
{
    public static class Extensions
    {
        public static void DontCare(this Task T)
        {
            if (T.IsCompleted)
            {
                var ignored = T.Exception;
            }
            else
            {
                T.ContinueWith(
                    t => { var ignored = t.Exception; },
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }
        }

        public static Task Send(this Message Message, TcpClient TcpClient)
        {
            return Message.Send(TcpClient, CancellationToken.None);
        }

        public static Task Send(this Message Message, TcpClient TcpClient, CancellationToken CancellationToken)
        {
            var MessageData = Message.Serialize();
            var NetworkStream = TcpClient.GetStream();
            return NetworkStream.WriteAsync(MessageData.Array, MessageData.Offset, MessageData.Count, CancellationToken);
        }

        public static Task Send(this Message message, UdpClient udpClient)
        {
            var MessageData = message.Serialize();
            var bytes = MessageData.ToArray(); //?? performance -> Span<T> / UdpClient subclass with ArraySegment support
            return udpClient.SendAsync(bytes, bytes.Length);
        }

        public static Task Send(this Message message, UdpClient udpClient, IPEndPoint remoteEP)
        {
            var MessageData = message.Serialize();
            var bytes = MessageData.ToArray(); //?? performance
            return udpClient.SendAsync(bytes, bytes.Length, remoteEP);
        }
    }
}
