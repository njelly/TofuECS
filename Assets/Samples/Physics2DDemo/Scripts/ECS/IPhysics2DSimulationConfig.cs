using Tofunaut.TofuECS.Math;

namespace Tofunaut.TofuECS.Samples.Physics2DDemo.ECS
{
    public interface IPhysics2DSimulationConfig : ISimulationConfig
    {
        BallData BallData { get; }
        FixVector2 Gravity { get; }
    }
}