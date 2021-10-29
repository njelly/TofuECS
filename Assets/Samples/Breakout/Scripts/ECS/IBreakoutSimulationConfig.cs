using Tofunaut.TofuECS.Math;

namespace Tofunaut.TofuECS.Samples.Breakout.ECS
{
    public interface IBreakoutSimulationConfig : ISimulationConfig
    {
        public FixVector2 BoardMin { get; }
        public FixVector2 BoardMax { get; }
        public Fix64 PaddleWidth { get; }
        public Fix64 BallRadius { get; }
        public Fix64 BallSpeed { get; }
        public FixVector2 BrickExtents { get; }
        public int NumLives { get; }
        public PlayerConfig PlayerConfig { get; }
        public int PaddleViewId { get; }
    }
}