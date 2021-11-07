using System.Collections.Generic;
using System.Linq;

namespace Tofunaut.TofuECS
{
    public class EntityComponentIterator<TComponent> : IEntityComponentIterator where TComponent : unmanaged
    {
        public int Count => _entityIds.Count;
        private readonly List<int> _entityIds;
        private readonly List<int> _entitiesToRemove;
        private int _currentIndex;
        internal ComponentBuffer<TComponent> buffer;
        private Frame _currentFrame;

        internal EntityComponentIterator()
        {
            _entityIds = new List<int>();
            _entitiesToRemove = new List<int>();
        }

        internal EntityComponentIterator(EntityComponentIterator<TComponent> copyFrom)
        {
            _entityIds = copyFrom._entityIds;
            _entitiesToRemove = copyFrom._entitiesToRemove;
        }

        public void Reset(Frame f)
        {
            _currentFrame = f;
            Reset();
        }
        
        public void Reset()
        {
            _currentIndex = 0;
            foreach (var entity in _entitiesToRemove)
                _entityIds.Remove(entity);
            
            _entitiesToRemove.Clear();
        }

        public void AddEntity(int entityId) => _entityIds.Add(entityId);
        public void RemoveEntity(int entityId) => _entitiesToRemove.Add(entityId);

        public bool Next(out int entityId, out TComponent component)
        {
            if (_currentIndex == _entityIds.Count)
            {
                entityId = -1;
                component = default;
                return false;
            }

            entityId = _entityIds[_currentIndex];
            var entity = _currentFrame.GetEntity(entityId);
            entity.TryGetComponentIndex(typeof(TComponent), out var index);
            component = buffer.Get(index);
            _currentIndex++;
            return true;
        }
        
        public unsafe bool NextUnsafe(out int entityId, out TComponent* component)
        {
            if (_currentIndex == _entityIds.Count)
            {
                entityId = -1;
                component = default;
                return false;
            }

            entityId = _entityIds[_currentIndex];
            var entity = _currentFrame.GetEntity(entityId);
            entity.TryGetComponentIndex(typeof(TComponent), out var index);
            component = buffer.GetUnsafe(index);
            _currentIndex++;
            return true;
        }

        public void Recycle(IEntityComponentIterator other)
        {
            var otherIterator = (EntityComponentIterator<TComponent>)other;
            otherIterator.Reset();
            Reset();
            _entityIds.Clear();
            for(var i = 0; i < otherIterator._entityIds.Count; i++)
                _entityIds.Add(otherIterator._entityIds[i]);
        }
    }
}