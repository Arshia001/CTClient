using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightMessage.Common.Messages
{
    public static class Crc16
    {
        const ushort polynomial = 0xA001;
        static readonly ushort[] Table = new ushort[256];

        public static ushort ComputeChecksum(byte[] Bytes, int Offset, int Length)
        {
            ushort Result = 0;
            for (int i = Offset; i < Offset + Length; ++i)
            {
                byte Index = (byte)(Result ^ Bytes[i]);
                Result = (ushort)((Result >> 8) ^ Table[Index]);
            }
            return Result;
        }

        static Crc16()
        {
            ushort Value;
            ushort Temp;
            for (ushort i = 0; i < Table.Length; ++i)
            {
                Value = 0;
                Temp = i;
                for (byte j = 0; j < 8; ++j)
                {
                    if (((Value ^ Temp) & 0x0001) != 0)
                    {
                        Value = (ushort)((Value >> 1) ^ polynomial);
                    }
                    else
                    {
                        Value >>= 1;
                    }
                    Temp >>= 1;
                }
                Table[i] = Value;
            }
        }
    }
}
