using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightMessage.Common.Messages
{
    public class ParamDouble : Param
    {
        public Double Value { get; private set; }

        internal ParamDouble() { }

        public ParamDouble(double Value)
        {
            this.Value = Value;
        }

        internal override void WriteTo(Stream Stream)
        {
            Stream.WriteByte(((int)ParamType.Double << 4));
            Stream.Write(BitConverter.GetBytes(Value), 0, 8);
        }

        internal override ParamContainer ReadFrom(Stream Stream, Byte FirstByte)
        {
            byte[] Buffer = new byte[8];
            Stream.Read(Buffer, 0, 8);
            Value = BitConverter.ToDouble(Buffer, 0);

            return null;
        }

        protected override string ToValueString()
        {
            return Value.ToString() + "D";
        }
    }
}
