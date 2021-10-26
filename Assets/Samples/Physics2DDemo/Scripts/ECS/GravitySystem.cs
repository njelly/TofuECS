using Tofunaut.TofuECS.Physics;

namespace Tofunaut.TofuECS.Samples.Physics2DDemo.ECS
{
    public unsafe class GravitySystem : ISystem
    {
        public void Initialize(Frame f) { }

        public void Process(Frame f)
        {
            var dynamicBody2DIterator = f.GetIterator<DynamicBody2D>();
            while (dynamicBody2DIterator.NextUnsafe(out _, out var dynamicBody2D))
                dynamicBody2D->AddForce(((IPhysics2DSimulationConfig)f.Config).Gravity);
        }

        public void Dispose(Frame f) { }
    }
}