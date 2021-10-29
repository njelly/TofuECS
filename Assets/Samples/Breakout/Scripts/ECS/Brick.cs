using Tofunaut.TofuECS.Math;

namespace Tofunaut.TofuECS.Samples.Breakout.ECS
{
    public struct Brick
    {
        public FixVector2 Extents;
        

        public void Initialize(Frame f)
        {
            Extents = ((IBreakoutSimulationConfig)f.Config).BrickExtents;
        }
    }
}