using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightMessage.Common.Util
{
    public struct SingleEnumerable<T> : IEnumerable<T>
    {
        public struct SingleEnumerator : IEnumerator<T>
        {
            private readonly SingleEnumerable<T> Owner;
            private bool CanMove;

            public SingleEnumerator(ref SingleEnumerable<T> parent)
            {
                Owner = parent;
                CanMove = true;
            }

            public T Current => Owner.Value;

            object IEnumerator.Current => Current;


            public void Dispose() { }

            public bool MoveNext()
            {
                if (!CanMove)
                    return false;

                CanMove = false;
                return true;
            }

            public void Reset()
            {
                CanMove = true;
            }
        }


        readonly T Value;


        public SingleEnumerable(T value)
        {
            Value = value;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new SingleEnumerator(ref this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new SingleEnumerator(ref this);
        }
    }
}
