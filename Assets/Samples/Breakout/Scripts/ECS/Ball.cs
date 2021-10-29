using Tofunaut.TofuECS.Math;

namespace Tofunaut.TofuECS.Samples.Breakout.ECS
{
    public struct Ball
    {
        public Fix64 Radius;
        public int PlayerEntityId;
        public FixVector2 Velocity;

        public void Initialize(Frame f, int playerEntityId)
        {
            Radius = ((IBreakoutSimulationConfig)f.Config).BallRadius;
            PlayerEntityId = playerEntityId;
        }
    }
}