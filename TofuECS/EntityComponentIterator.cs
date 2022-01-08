using System;

namespace Tofunaut.TofuECS
{
    public class EntityComponentIterator<TComponent> where TComponent : unmanaged
    {
        private readonly ComponentBuffer<TComponent> _rootBuffer;
        private ComponentBuffer<TComponent> _currentBuffer;

        internal EntityComponentIterator(ComponentBuffer<TComponent> rootBuffer)
        {
            _rootBuffer = rootBuffer;
            Reset();
        }

        public void Reset()
        {
            _currentBuffer = _rootBuffer;
            _currentBuffer.ResetIterator();
        }

        public bool Next(out int entityId, out TComponent component)
        {
            while (_currentBuffer.Next(out entityId, out component))
            {
                if (entityId != ECS.InvalidEntityId)
                    return true;
            }

            _currentBuffer = _currentBuffer.NextBuffer;
            return _currentBuffer != null && Next(out entityId, out component);
        }

        public unsafe bool NextUnsafe(out int entityId, out TComponent* component)
        {
            while (_currentBuffer.NextUnsafe(out entityId, out component))
            {
                if (entityId != ECS.InvalidEntityId)
                    return true;
            }

            _currentBuffer = _currentBuffer.NextBuffer;
            return _currentBuffer != null && NextUnsafe(out entityId, out component);
        }
    }
}