using Tofunaut.TofuECS.Math;
using Tofunaut.TofuECS.Physics;

namespace Tofunaut.TofuECS.Samples.Physics2DDemo.ECS
{
    public unsafe class BallSystem : ISystem
    {
        public void Initialize(Frame f)
        {
            CreateBall(f, FixVector2.Zero);
        }

        public void Process(Frame f) { }

        public void Dispose(Frame f) { }

        private static void CreateBall(Frame f, FixVector2 position)
        {
            var ballEntity = f.CreateEntity();
            f.AddComponent<Transform2D>(ballEntity);
            f.AddComponent<DynamicBody2D>(ballEntity);
            
            var ballData = ((IPhysics2DSimulationConfig)f.Config).BallData;
            var dynamicBody2d = f.GetComponentUnsafe<DynamicBody2D>(ballEntity);
            dynamicBody2d->Collider = new Collider
            {
                ShapeType = ShapeType.Circle,
                CircleRadius = ballData.Radius,
                IsTrigger = false,
            };
            dynamicBody2d->Mass = ballData.Mass;

            f.GetComponentUnsafe<Transform2D>(ballEntity)->Position = position;
        }
    }
}