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

                var sumForces = FixVector2.Zero;
                for (var i = 0; i < dynamicBody2d->ForcesNextIndex; i++)
                    sumForces += dynamicBody2d->Forces[i];

                dynamicBody2d->Velocity += sumForces / new Fix64(dynamicBody2d->ForcesNextIndex) * f.DeltaTime;
            }
        }

        public void Dispose(Frame f) { }
    }
}