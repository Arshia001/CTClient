using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightMessage.Common.Messages
{
    public class ParamNull : Param
    {
        public ParamNull() { }

        internal override void WriteTo(Stream Stream)
        {
            Stream.WriteByte((byte)((int)ParamType.Null << 4));
        }

        internal override ParamContainer ReadFrom(Stream Stream, Byte FirstByte)
        {
            return null;
        }

        protected override string ToValueString()
        {
            return "null";
        }
    }
}
