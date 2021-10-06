using System.Collections.Generic;

namespace Tofunaut.TofuECS
{
    public class EntityComponentIterator<TComponent> : IEntityComponentIterator where TComponent : unmanaged
    {
        private readonly List<Entity> _entities;
        private readonly List<Entity> _entitiesToRemove;
        private int _currentIndex;
        internal ComponentBuffer<TComponent> buffer;

        internal EntityComponentIterator()
        {
            _entities = new List<Entity>();
            _entitiesToRemove = new List<Entity>();
        }

        public void Reset()
        { 
            _currentIndex = 0;
            foreach (var entity in _entitiesToRemove)
                _entities.Remove(entity);
            
            _entitiesToRemove.Clear();
        }

        public void AddEntity(Entity entity) => _entities.Add(entity);
        public void RemoveEntity(Entity entity) => _entitiesToRemove.Add(entity);

        public bool Next(out Entity entity, out TComponent component)
        {
            if (_currentIndex == _entities.Count)
            {
                entity = default;
                component = default;
                return false;
            }

            entity = _entities[_currentIndex];
            entity.TryGetComponentIndex(typeof(TComponent), out var index);

            unsafe
            {
                component = buffer.Get(index);
            }
            _currentIndex++;
            return true;
        }
        
        public unsafe bool NextUnsafe(out Entity entity, out TComponent* component)
        {
            if (_currentIndex == _entities.Count)
            {
                entity = default;
                component = default;
                return false;
            }

            entity = _entities[_currentIndex];
            entity.TryGetComponentIndex(typeof(TComponent), out var index);
            component = buffer.GetUnsafe(index);
            _currentIndex++;
            return true;
        }

        public void CopyFrom(IEntityComponentIterator other)
        {
            var otherIterator = (EntityComponentIterator<TComponent>)other;
            otherIterator.Reset();
            Reset();
            _entities.Clear();
            foreach (var otherEntity in otherIterator._entities)
                _entities.Add(otherEntity);
        }
    }
}