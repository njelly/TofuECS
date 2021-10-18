namespace Tofunaut.TofuECS.Physics
{
    internal unsafe class PhysicsSystem : ISystem
    {
        public void Initialize(Frame f) { }

        public void Process(Frame f)
        {
            var transform2dIterator = f.GetIterator<Transform2D>();
            while (transform2dIterator.NextUnsafe(out var entityId, out var transform2D))
            {
                if (!f.TryGetComponent<DynamicBody2D>(entityId, out var dynamicBody2D))
                    continue;
                
                
            }
        }

        public void Dispose(Frame f) { }
    }
}