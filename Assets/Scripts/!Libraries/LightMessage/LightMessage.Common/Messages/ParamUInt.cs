using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightMessage.Common.Messages
{
    public class ParamUInt : Param
    {
        public UInt64 Value { get; private set; }

        internal ParamUInt() { }

        public ParamUInt(ulong Value)
        {
            this.Value = Value;
        }

        internal override void WriteTo(Stream Stream)
        {
            IntegerHelper.EncodeVarUInt(Stream, Value, 3, ((int)ParamType.UInt << 4));
        }

        internal override ParamContainer ReadFrom(Stream Stream, Byte FirstByte)
        {
            Value = IntegerHelper.DecodeVarUInt(Stream, FirstByte, 3);

            return null;
        }

        protected override string ToValueString()
        {
            return Value.ToString() + "U";
        }
    }
}
