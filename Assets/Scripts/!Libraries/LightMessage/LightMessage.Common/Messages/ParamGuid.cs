using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightMessage.Common.Messages
{
    public class ParamGuid : Param
    {
        public Guid Value { get; private set; }

        internal ParamGuid() { }

        public ParamGuid(Guid Value)
        {
            this.Value = Value;
        }

        internal override void WriteTo(Stream Stream)
        {
            Stream.WriteByte((int)ParamType.Guid << 4);
            Stream.Write(Value.ToByteArray(), 0, 16);
        }

        internal override ParamContainer ReadFrom(Stream Stream, Byte FirstByte)
        {
            var Bytes = new byte[16];
            Stream.Read(Bytes, 0, Bytes.Length);
            Value = new Guid(Bytes);

            return null;
        }

        protected override string ToValueString()
        {
            return Value.ToString();
        }
    }
}
