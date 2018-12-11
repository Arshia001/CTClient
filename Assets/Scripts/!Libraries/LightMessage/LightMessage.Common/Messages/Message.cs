using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LightMessage.Common.Messages
{
    public class Message
    {
        static async Task Read(Stream Stream, byte[] Buffer, int Start, int Count, CancellationToken CancellationToken)
        {
            int Read = 0;

            while (Read < Count)
                Read += await Stream.ReadAsync(Buffer, Start + Read, Count - Read, CancellationToken);
        }

        public static async Task<Message> ReadFrom(Stream Stream, int MaxMessageSize, int MaxLengthSectionBytes, CancellationToken CancellationToken)
        {
            var MessageLength = 0LU;

            try
            {
                bool Continued = true;
                int Shift = 0;
                var TempBuf = new byte[1];
                for (int i = 0; i < MaxLengthSectionBytes && Continued; ++i)
                {
                    await Stream.ReadAsync(TempBuf, 0, 1);
                    IntegerHelper.DecodeVarUInt(TempBuf[0], ref MessageLength, ref Shift, out Continued);
                }

                if (Continued)
                    throw new NotSupportedException("Encoded message length exceeds maximum number of bytes allowed");
            }
            catch
            {
                return null;
            }

            if ((int)MessageLength > MaxMessageSize)
                throw new NotSupportedException("Message is too long");


            var Buffer = new byte[MessageLength];
            await Read(Stream, Buffer, 0, (int)MessageLength, CancellationToken);

            return ReadFromInternal(new ArraySegment<byte>(Buffer));
        }

        public static Message ReadFrom(ArraySegment<byte> Array)
        {
            var MessageLength = 0LU;
            int NumLengthSectionBytes;

            {
                bool Continued = true;
                int Shift = 0;
                for (NumLengthSectionBytes = 0; NumLengthSectionBytes < Array.Count && Continued; ++NumLengthSectionBytes)
                {
                    IntegerHelper.DecodeVarUInt(Array.Array[Array.Offset + NumLengthSectionBytes], ref MessageLength, ref Shift, out Continued);
                }
            }

            if ((int)MessageLength != Array.Count - NumLengthSectionBytes)
                throw new NotSupportedException("Array length does not match the length encoded in the message");

            return ReadFromInternal(new ArraySegment<byte>(Array.Array, Array.Offset + NumLengthSectionBytes, Array.Count - NumLengthSectionBytes));
        }

        static Message ReadFromInternal(ArraySegment<byte> Array)
        {
            int MessageLength = Array.Count;

            ushort Crc = Crc16.ComputeChecksum(Array.Array, Array.Offset, MessageLength - 2);
            if (Crc != BitConverter.ToUInt16(Array.Array, Array.Offset + MessageLength - 2))
                throw new NotSupportedException("Invalid checksum");


            var Result = new Message();
            var Containers = new Stack<ParamContainer>();
            Param Param = null;
            var MessageStream = new MemoryStream(Array.Array, Array.Offset, MessageLength - 2);

            while (MessageStream.Position < MessageStream.Length)
            {
                byte IdByte = (byte)MessageStream.ReadByte();

                switch (IdByte >> 4)
                {
                    case (int)ParamType.Int:
                        Param = new ParamInt();
                        break;
                    case (int)ParamType.UInt:
                        Param = new ParamUInt();
                        break;
                    case (int)ParamType.Float:
                        Param = new ParamFloat();
                        break;
                    case (int)ParamType.Double:
                        Param = new ParamDouble();
                        break;
                    case (int)ParamType.Boolean:
                        Param = new ParamBoolean();
                        break;
                    case (int)ParamType.Null:
                        Param = new ParamNull();
                        break;
                    case (int)ParamType.String:
                        Param = new ParamString();
                        break;
                    case (int)ParamType.Binary:
                        Param = new ParamBinary();
                        break;
                    case (int)ParamType.Guid:
                        Param = new ParamGuid();
                        break;
                    case (int)ParamType.DateTime:
                        Param = new ParamDateTime();
                        break;
                    case (int)ParamType.TimeSpan:
                        Param = new ParamTimeSpan();
                        break;
                    case (int)ParamType.Array:
                        Param = new ParamArray();
                        break;
                }

                var NewContainer = Param.ReadFrom(MessageStream, IdByte);
                if (Containers.Count == 0)
                    Result._Params.Add(Param);
                else
                {
                    if (Containers.Peek().AddParamForRead(Param))
                        Containers.Pop();
                }

                if (NewContainer != null)
                    Containers.Push(NewContainer);
            }

            return Result;
        }

        protected static ArraySegment<byte> SerializeInternal(IEnumerable<Param> Params)
        {
            var MessageStream = new MemoryStream();

            MessageStream.Seek(4, SeekOrigin.Begin);

            foreach (var Param in Params)
                Param.WriteTo(MessageStream);

            ushort Crc = Crc16.ComputeChecksum(MessageStream.GetBuffer(), 4, (int)MessageStream.Length - 4);
            MessageStream.Write(BitConverter.GetBytes(Crc), 0, 2);

            var MessageLength = MessageStream.Length - 4;
            var MessageLengthStream = new MemoryStream();
            IntegerHelper.EncodeVarUInt(MessageLengthStream, (ulong)MessageLength);

            var NumLengthSectionBytes = (int)MessageLengthStream.Length;
            MessageStream.Seek(4 - NumLengthSectionBytes, SeekOrigin.Begin);
            MessageStream.Write(MessageLengthStream.GetBuffer(), 0, NumLengthSectionBytes);

            return new ArraySegment<byte>(MessageStream.GetBuffer(), 4 - NumLengthSectionBytes, (int)MessageStream.Length - 4 + NumLengthSectionBytes);
        }


        public IReadOnlyList<Param> Params => _Params;

        protected List<Param> _Params = new List<Param>();


        public Message(params Param[] Params) : this(Params.AsEnumerable()) { }

        public Message(IEnumerable<Param> Params)
        {
            _Params = new List<Param>(Params);
        }

        public virtual ArraySegment<byte> Serialize()
        {
            return SerializeInternal(_Params);
        }

        public override string ToString()
        {
            StringBuilder Result = new StringBuilder($"Message[");
            bool bFirst = true;

            foreach (var Param in Params)
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

        public long? AsInt(int Index)
        {
            return Params.Count > Index ? (Params[Index] as ParamInt)?.Value : null;
        }

        public ulong? AsUInt(int Index)
        {
            return Params.Count > Index ? (Params[Index] as ParamUInt)?.Value : null;
        }

        public float? AsFloat(int Index)
        {
            return Params.Count > Index ? (Params[Index] as ParamFloat)?.Value : null;
        }

        public double? AsDouble(int Index)
        {
            return Params.Count > Index ? (Params[Index] as ParamDouble)?.Value : null;
        }

        public bool? AsBoolean(int Index)
        {
            return Params.Count > Index ? (Params[Index] as ParamBoolean)?.Value : null;
        }

        public bool IsNull(int Index)
        {
            return Params.Count > Index ? (Params[Index] is ParamNull) : true;
        }

        public string AsString(int Index)
        {
            return Params.Count > Index ? (Params[Index] as ParamString)?.Value : null;
        }

        public ArraySegment<byte>? AsBinary(int Index)
        {
            return Params.Count > Index ? (Params[Index] as ParamBinary)?.Value : null;
        }

        public Guid? AsGuid(int Index)
        {
            return Params.Count > Index ? (Params[Index] as ParamGuid)?.Value : null;
        }

        public DateTime? AsDateTime(int Index)
        {
            return Params.Count > Index ? (Params[Index] as ParamDateTime)?.Value : null;
        }

        public TimeSpan? AsTimeSpan(int Index)
        {
            return Params.Count > Index ? (Params[Index] as ParamTimeSpan)?.Value : null;
        }

        public IReadOnlyList<Param> AsArray(int Index)
        {
            return Params.Count > Index ? (Params[Index] as ParamArray)?.Params : null;
        }
    }
}
