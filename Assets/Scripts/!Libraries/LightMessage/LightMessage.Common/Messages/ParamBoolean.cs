using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightMessage.Common.Messages
{
    public class ParamBoolean : Param
    {
        public bool Value { get; private set; }

        internal ParamBoolean() { }

        public ParamBoolean(bool Value)
        {
            this.Value = Value;
        }

        internal override void WriteTo(Stream Stream)
        {
            Stream.WriteByte((byte)(((int)ParamType.Boolean << 4) | (Value ? 0x01 : 0)));
        }

        internal override ParamContainer ReadFrom(Stream Stream, Byte FirstByte)
        {
            Value = (FirstByte & 0x01) != 0;

            return null;
        }

        protected override string ToValueString()
        {
            return Value.ToString();
        }
    }
}
