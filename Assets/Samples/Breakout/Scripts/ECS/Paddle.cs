using Tofunaut.TofuECS.Math;

namespace Tofunaut.TofuECS.Samples.Breakout.ECS
{
    public struct Paddle
    {
        public bool BallIsStuck;
        public int PlayerEntityId;
        public Fix64 HalfWidth;

        public void Initialize(Frame f, int playerEntityId)
        {
            BallIsStuck = true;
            PlayerEntityId = playerEntityId;
            HalfWidth = ((IBreakoutSimulationConfig)f.Config).PaddleWidth;
        }
    }
}