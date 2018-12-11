using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightMessage.Common.Messages
{
    public class ParamTimeSpan : Param
    {
        public TimeSpan Value { get; private set; }

        internal ParamTimeSpan() { }

        public ParamTimeSpan(TimeSpan Value)
        {
            this.Value = Value;
        }

        internal override void WriteTo(Stream Stream)
        {
            Stream.WriteByte((int)ParamType.TimeSpan << 4);
            Stream.Write(BitConverter.GetBytes((Int32)Value.TotalMilliseconds), 0, 4);
        }

        internal override ParamContainer ReadFrom(Stream Stream, Byte FirstByte)
        {
            var Bytes = new byte[4];
            Stream.Read(Bytes, 0, Bytes.Length);
            Value = System.TimeSpan.FromMilliseconds(BitConverter.ToInt32(Bytes, 0));

            return null;
        }

        protected override string ToValueString()
        {
            return Value.ToString();
        }
    }
}
