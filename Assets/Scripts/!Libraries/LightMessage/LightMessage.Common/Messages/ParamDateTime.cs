using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightMessage.Common.Messages
{
    public class ParamDateTime : Param
    {
        public DateTime Value { get; private set; }

        internal ParamDateTime() { }

        public ParamDateTime(DateTime Value)
        {
            this.Value = Value;
        }

        internal override void WriteTo(Stream Stream)
        {
            Stream.WriteByte((int)ParamType.DateTime << 4);
            Stream.Write(BitConverter.GetBytes((Int64)Value.Ticks), 0, 8);
        }

        internal override ParamContainer ReadFrom(Stream Stream, Byte FirstByte)
        {
            var Bytes = new byte[8];
            Stream.Read(Bytes, 0, Bytes.Length);
            Value = new DateTime(BitConverter.ToInt64(Bytes, 0));

            return null;
        }

        protected override string ToValueString()
        {
            return Value.ToUniversalTime().ToString();
        }
    }
}
