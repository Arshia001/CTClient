using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LightMessage.Common.Messages
{
    public class IntegerHelper
    {
        public static void DecodeVarUInt(byte CurrentByte, ref ulong Value, ref int Shift, out bool Continued)
        {
            Value |= (CurrentByte & 0x7fu) << Shift;
            Shift += 7;
            Continued = (CurrentByte & 0x80u) != 0;
        }

        public static async Task<UInt64> DecodeVarUIntAsync(Stream Stream, byte FirstByte, byte FromBit = 7)
        {
            UInt64 Result;
            byte[] Buffer = new byte[1];

            byte Current = FirstByte;

            Current = (byte)(Current << (7 - FromBit));

            if ((Current & 0x80u) == 0)
                return (ulong)(Current >> (7 - FromBit));

            int Shift = FromBit;
            Result = ((Current & 0x7fu) >> (7 - FromBit));

            do
            {
                await Stream.ReadAsync(Buffer, 0, 1);
                Current = Buffer[0];
                Result |= (Current & 0x7fu) << Shift;
                Shift += 7;
            } while ((Current & 0x80u) != 0);

            return Result;
        }

        public static async Task EncodeVarUIntAsync(Stream Stream, UInt64 Value, byte FromBit = 7, byte RestOfFirstByte = 0)
        {
            byte[] Buffer = new byte[1];

            if (Value < 1LU << FromBit)
            {
                Buffer[0] = (byte)(RestOfFirstByte | Value);
                await Stream.WriteAsync(Buffer, 0, 1);
                return;
            }

            UInt64 Temp = Value << (7 - FromBit);

            Buffer[0] = (byte)(RestOfFirstByte | ((Temp & 0x7fu) >> (7 - FromBit) | 1LU << FromBit));
            await Stream.WriteAsync(Buffer, 0, 1);
            Value >>= FromBit;

            do
            {
                Buffer[0] = (byte)(Value | 0x80u);
                await Stream.WriteAsync(Buffer, 0, 1);
                Value >>= 7;
            } while (Value > 0x80u);

            Buffer[0] = (byte)Value;
            await Stream.WriteAsync(Buffer, 0, 1);
        }

        public static UInt64 DecodeVarUInt(Stream Stream, byte FirstByte, byte FromBit = 7)
        {
            UInt64 Result;

            byte Current = FirstByte;

            Current = (byte)(Current << (7 - FromBit));

            if ((Current & 0x80u) == 0)
                return (ulong)(Current >> (7 - FromBit));

            int Shift = FromBit;
            Result = ((Current & 0x7fu) >> (7 - FromBit));

            do
            {
                Current = (byte)Stream.ReadByte();
                Result |= (Current & 0x7fu) << Shift;
                Shift += 7;
            } while ((Current & 0x80u) != 0);

            return Result;
        }

        public static void EncodeVarUInt(Stream Stream, UInt64 Value, byte FromBit = 7, byte RestOfFirstByte = 0)
        {
            if (Value < 1LU << FromBit)
            {
                Stream.WriteByte((byte)(RestOfFirstByte | Value));
                return;
            }

            UInt64 Temp = Value << (7 - FromBit);

            Stream.WriteByte((byte)(RestOfFirstByte | ((Temp & 0x7fu) >> (7 - FromBit) | 1LU << FromBit)));
            Value >>= FromBit;

            while (Value >= 0x80u)
            {
                Stream.WriteByte((byte)(Value | 0x80u));
                Value >>= 7;
            }

            Stream.WriteByte((byte)Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt64 EncodeZigzag(Int64 Value)
        {
            return (UInt64)((Value << 1) ^ (Value >> (sizeof(Int64) * 8 - 1)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int64 DecodeZigzag(UInt64 Value)
        {
            return (Int64)((Value >> 1) ^ (UInt64)(-(Int64)(Value & 1)));
        }
    }
}
