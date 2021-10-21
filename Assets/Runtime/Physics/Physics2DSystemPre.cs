using Tofunaut.TofuECS.Math;

namespace Tofunaut.TofuECS.Physics
{
    public unsafe class Physics2DSystemPre : ISystem
    {
        public void Initialize(Frame f) { }

        public void Process(Frame f)
        {
            var dynamicBody2dIterator = f.GetIterator<DynamicBody2D>();
            while (dynamicBody2dIterator.NextUnsafe(out var entityId, out var dynamicBody2d))
            {
                dynamicBody2d->ForcesNextIndex = 0;
                for (var i = 0; i < dynamicBody2d->ForcesLength; i++)
                    dynamicBody2d->Forces[i] = FixVector2.Zero;
            }
        }

        public void Dispose(Frame f) { }
    }
}