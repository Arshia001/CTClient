using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightMessage.Common.Messages
{
    public class ParamFloat : Param
    {
        public Single Value { get; private set; }

        internal ParamFloat() { }

        public ParamFloat(float Value)
        {
            this.Value = Value;
        }

        internal override void WriteTo(Stream Stream)
        {
            Stream.WriteByte(((int)ParamType.Float << 4));
            Stream.Write(BitConverter.GetBytes(Value), 0, 4);
        }

        internal override ParamContainer ReadFrom(Stream Stream, Byte FirstByte)
        {
            byte[] Buffer = new byte[4];
            Stream.Read(Buffer, 0, 4);
            Value = BitConverter.ToSingle(Buffer, 0);

            return null;
        }

        protected override string ToValueString()
        {
            return Value.ToString() + "F";
        }
    }
}
