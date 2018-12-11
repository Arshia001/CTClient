using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightMessage.Common.Messages
{
    public class ParamBinary : Param
    {
        public ArraySegment<byte> Value { get; private set; }

        internal ParamBinary() { }

        public ParamBinary(ArraySegment<byte> Value)
        {
            this.Value = Value;
        }

        internal override void WriteTo(Stream Stream)
        {
            IntegerHelper.EncodeVarUInt(Stream, (ulong)Value.Count, 3, ((int)ParamType.Binary << 4));
            Stream.Write(Value.Array, Value.Offset, Value.Count);
        }

        internal override ParamContainer ReadFrom(Stream Stream, Byte FirstByte)
        {
            ulong NumBytes = IntegerHelper.DecodeVarUInt(Stream, FirstByte, 3);

            if (NumBytes == 0)
                Value = new ArraySegment<byte>(new byte[0]);
            else
            {
                var Bytes = new byte[NumBytes];
                Stream.Read(Bytes, 0, Bytes.Length);
                Value = new ArraySegment<byte>(Bytes);
            }

            return null;
        }

        protected override string ToValueString()
        {
            return "0x" + BitConverter.ToString(Value.Array, Value.Offset, Value.Count).Replace("-", "");
        }
    }
}
