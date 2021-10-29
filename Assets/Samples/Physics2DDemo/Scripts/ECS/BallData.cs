using Tofunaut.TofuECS.Math;

namespace Tofunaut.TofuECS.Samples.Physics2DDemo.ECS
{
    public unsafe struct BallData
    {
        public Fix64 Mass;
        public Fix64 Radius;
        public int* PrefabOptions;
        public int PrefabLength;

        public int GetRandomBallViewId(Frame f)
        {
            var roll = f.RNG.NextFix64(Fix64.Zero, new Fix64(PrefabLength));
            var i = 0;
            for(; i < PrefabLength; i++)
                if (i + 1 > (int)roll)
                    break;

            return PrefabOptions[i];
        }
    }
}