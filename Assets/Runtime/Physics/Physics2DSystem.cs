using Tofunaut.TofuECS.Math;

namespace Tofunaut.TofuECS.Physics
{
    internal unsafe class Physics2DSystem : ISystem
    {
        public void Initialize(Frame f) { }

        public void Process(Frame f)
        {
            var transform2dIterator = f.GetIterator<Transform2D>();
            while (transform2dIterator.NextUnsafe(out var entityId, out var transform2d))
            {
                if (!f.TryGetComponentUnsafe<DynamicBody2D>(entityId, out var dynamicBody2d))
                    continue;

                if (dynamicBody2d->ForcesNextIndex <= 0)
                    continue;

                // integrate the forces
                dynamicBody2d->Velocity += dynamicBody2d->SumForces() / new Fix64(dynamicBody2d->ForcesNextIndex) * f.DeltaTime;
                dynamicBody2d->ClearForces();
                
                // move the transform
                transform2d->Position += dynamicBody2d->Velocity * f.DeltaTime;
            }
        }

        public void Dispose(Frame f)
        {
            var dynamicBody2dIterator = f.GetIterator<DynamicBody2D>();
            while (dynamicBody2dIterator.NextUnsafe(out _, out var dynamicBody2D))
                dynamicBody2D->Dispose();
        }
    }
}