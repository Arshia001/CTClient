using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightMessage.Common.Messages
{
    public class ParamInt : Param
    {
        public Int64 Value { get; private set; }

        internal ParamInt() { }

        public ParamInt(long Value)
        {
            this.Value = Value;
        }

        internal override void WriteTo(Stream Stream)
        {
            IntegerHelper.EncodeVarUInt(Stream, IntegerHelper.EncodeZigzag(Value), 3, ((int)ParamType.Int << 4));
        }

        internal override ParamContainer ReadFrom(Stream Stream, Byte FirstByte)
        {
            Value = IntegerHelper.DecodeZigzag(IntegerHelper.DecodeVarUInt(Stream, FirstByte, 3));

            return null;
        }

        protected override string ToValueString()
        {
            return Value.ToString();
        }
    }
}
