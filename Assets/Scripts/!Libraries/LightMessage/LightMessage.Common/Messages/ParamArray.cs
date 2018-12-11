using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightMessage.Common.Messages
{
    public class ParamArray : ParamContainer
    {
        public IReadOnlyList<Param> Params => ReadOnlyParams;

        int NumParamsForRead;


        internal ParamArray() { }
        public ParamArray(params Param[] Params) : base(Params) { }
        public ParamArray(IEnumerable<Param> Params) : base(Params) { }

        protected override void FinalizeParams() { }

        protected override bool HaveEnoughParams()
        {
            return ReadOnlyParams.Count == NumParamsForRead;
        }

        internal override void WriteTo(Stream Stream)
        {
            IntegerHelper.EncodeVarUInt(Stream, (ulong)ReadOnlyParams.Count, 3, ((int)ParamType.Array << 4));
            foreach (var Param in ReadOnlyParams)
                Param.WriteTo(Stream);
        }

        internal override ParamContainer ReadFrom(Stream Stream, byte FirstByte)
        {
            NumParamsForRead = (int)IntegerHelper.DecodeVarUInt(Stream, FirstByte, 3);
            return NumParamsForRead == 0 ? null : this;
        }

        protected override string ToValueString()
        {
            StringBuilder Result = new StringBuilder("[");
            bool bFirst = true;

            foreach (var Param in Params)
            {
                if (bFirst)
                    bFirst = false;
                else
                    Result.Append(',');
                Result.Append(Param.ToString());
            }

            Result.Append(']');

            return Result.ToString();
        }
    }
}
