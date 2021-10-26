using System;
using Tofunaut.TofuECS.Math;

namespace Tofunaut.TofuECS.Unity
{
    [Serializable]
    public class SerializableFixVector2
    {
        public long RawValueX = Fix64.Zero.RawValue;
        public long RawValueY = Fix64.Zero.RawValue;
        public FixVector2 Value => new FixVector2(Fix64.FromRaw(RawValueX), Fix64.FromRaw(RawValueY));
    }
}