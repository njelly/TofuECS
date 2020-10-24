using System;
using FixMath.NET;

namespace Tofunaut.TofuECS
{
    [Serializable]
    public struct WorldConfig
    {
        public Fix64 DeltaTime;
        public int MaxComponents;
        public int MaxEntities;
    }
}