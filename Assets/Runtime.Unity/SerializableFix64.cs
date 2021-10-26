using System;
using Tofunaut.TofuECS.Math;

namespace Tofunaut.TofuECS.Unity
{
    [Serializable]
    public class SerializableFix64
    {
        public long RawValue = Fix64.Zero.RawValue;
        public Fix64 Value => Fix64.FromRaw(RawValue);
    }
}