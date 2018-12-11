using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightMessage.Common.Messages
{
    public class ParamString : Param
    {
        public string Value { get; private set; }

        internal ParamString() { }

        public ParamString(string Value)
        {
            this.Value = Value ?? "";
        }

        internal override void WriteTo(Stream Stream)
        {
            var Bytes = Encoding.UTF8.GetBytes(Value);
            IntegerHelper.EncodeVarUInt(Stream, (ulong)Bytes.Length, 3, ((int)ParamType.String << 4));
            Stream.Write(Bytes, 0, Bytes.Length);
        }

        internal override ParamContainer ReadFrom(Stream Stream, Byte FirstByte)
        {
            ulong NumBytes = IntegerHelper.DecodeVarUInt(Stream, FirstByte, 3);

            if (NumBytes == 0)
                Value = "";
            else
            {
                var Bytes = new byte[NumBytes];
                Stream.Read(Bytes, 0, Bytes.Length);
                Value = Encoding.UTF8.GetString(Bytes);
            }

            return null;
        }

        protected override string ToValueString()
        {
            return $"\"{Value}\"";
        }
    }
}
