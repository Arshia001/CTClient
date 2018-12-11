using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightMessage.Common.Messages
{
    public abstract class Param
    {
        public static Param Int(long? Value)
        {
            return Value.HasValue ? new ParamInt(Value.Value) : new ParamNull() as Param;
        }

        public static Param UInt(ulong? Value)
        {
            return Value.HasValue ? new ParamUInt(Value.Value) : new ParamNull() as Param;
        }

        public static Param Float(float? Value)
        {
            return Value.HasValue ? new ParamFloat(Value.Value) : new ParamNull() as Param;
        }

        public static Param Double(double? Value)
        {
            return Value.HasValue ? new ParamDouble(Value.Value) : new ParamNull() as Param;
        }

        public static Param Boolean(bool? Value)
        {
            return Value.HasValue ? new ParamBoolean(Value.Value) : new ParamNull() as Param;
        }

        public static Param String(string Value)
        {
            return Value != null ? new ParamString(Value) : new ParamNull() as Param;
        }

        public static Param Null()
        {
            return new ParamNull();
        }

        public static Param Binary(ArraySegment<byte> Value)
        {
            return Value.Array != null ? new ParamBinary(Value) : new ParamNull() as Param;
        }

        public static Param Guid(Guid? Value)
        {
            return Value.HasValue ? new ParamGuid(Value.Value) : new ParamNull() as Param;
        }

        public static Param DateTime(DateTime? Value)
        {
            return Value.HasValue ? new ParamDateTime(Value.Value) : new ParamNull() as Param;
        }

        public static Param TimeSpan(TimeSpan? Value)
        {
            return Value.HasValue ? new ParamTimeSpan(Value.Value) : new ParamNull() as Param;
        }

        public static Param Array(IEnumerable<Param> Params)
        {
            return Params == null ? new ParamNull() as Param : new ParamArray(Params);
        }

        public static Param Array(params Param[] Params)
        {
            return Params == null ? new ParamNull() as Param : new ParamArray(Params);
        }


        public long? AsInt => (this as ParamInt)?.Value ?? (long?)(this as ParamUInt)?.Value;

        public ulong? AsUInt => (this as ParamUInt)?.Value ?? (ulong?)(this as ParamInt)?.Value;

        public float? AsFloat => (this as ParamFloat)?.Value ?? (float?)(this as ParamDouble)?.Value;

        public double? AsDouble => (this as ParamDouble)?.Value ?? (this as ParamFloat)?.Value;

        public bool? AsBoolean => (this as ParamBoolean)?.Value;

        public bool IsNull => this is ParamNull;

        public string AsString => (this as ParamString)?.Value;

        public ArraySegment<byte>? AsBinary => (this as ParamBinary)?.Value;

        public Guid? AsGuid => (this as ParamGuid)?.Value;

        public DateTime? AsDateTime => (this as ParamDateTime)?.Value;

        public TimeSpan? AsTimeSpan => (this as ParamTimeSpan)?.Value;

        public IReadOnlyList<Param> AsArray => (this as ParamArray)?.Params;


        internal abstract void WriteTo(Stream Stream);
        internal abstract ParamContainer ReadFrom(Stream Stream, byte FirstByte);
        protected abstract string ToValueString();

        protected Param() { }

        public override string ToString()
        {
            return ToValueString();
        }
    }
}
