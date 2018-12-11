using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightMessage.Common.Messages
{
    public abstract class ParamContainer : Param
    {
        List<Param> _Params;
        protected IReadOnlyList<Param> ReadOnlyParams => _Params.AsReadOnly();


        protected abstract void FinalizeParams();
        protected abstract bool HaveEnoughParams();


        protected ParamContainer()
        {
            _Params = new List<Param>();
        }

        protected ParamContainer(IEnumerable<Param> Params)
        {
            _Params = new List<Param>(Params);
        }

        internal bool AddParamForRead(Param Param)
        {
            _Params.Add(Param);

            if (HaveEnoughParams())
            {
                FinalizeParams();
                return true;
            }
            else
                return false;
        }
    }
}
