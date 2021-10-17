using System;
using System.Collections.Generic;

namespace Tofunaut.TofuECS
{
    internal class EntityBuffer
    {
        private Entity[] _entities;
        private int _entityIdCounter;
        private readonly Queue<int> _freeIndexes;
        private readonly Dictionary<int, int> _idToIndex;

        public EntityBuffer()
        {
            _entities = new Entity[1];
            _entityIdCounter = 0;
            _freeIndexes = new Queue<int>();
            _idToIndex = new Dictionary<int, int>();

            for (var i = 0; i < _entities.Length; i++)
            {
                _entities[i] = new Entity();
                _freeIndexes.Enqueue(i);
            }
        }

        public int Request()
        {
            int UseNextFreeIndex()
            {
                var freeIndex = _freeIndexes.Dequeue();
                _entities[freeIndex].Recycle(_entityIdCounter++);
                _idToIndex[_entities[freeIndex].Id] = freeIndex;
                return freeIndex;
            }

            if (_freeIndexes.Count > 0)
                return UseNextFreeIndex();
            
            // expand the buffer - there are no free indexes
            var prevLength = _entities.Length;
            var newBuffer = new Entity[prevLength * 2];
            Array.Copy(_entities, newBuffer, _entities.Length);

            for (var i = newBuffer.Length - _entities.Length; i < newBuffer.Length; i++)
            {
                newBuffer[i] = new Entity();
                _freeIndexes.Enqueue(i);
            }
            
            _entities = newBuffer;

            return UseNextFreeIndex();
        }
        
        public void Release(Entity entity) => _freeIndexes.Enqueue(_idToIndex[entity.Id]);

        public Entity Get(int id) => _entities[_idToIndex[id]];

        public bool TryGet(int id, out Entity entity)
        {
            entity = default;
            if (!_idToIndex.TryGetValue(id, out var index))
                return false;

            entity = _entities[index];
            return true;
        }

        public void CopyFrom(EntityBuffer other)
        {
            _entityIdCounter = other._entityIdCounter;
            
            if (other._entities.Length != _entities.Length)
                Array.Resize(ref _entities, other._entities.Length);
            
            Array.Copy(other._entities, _entities, other._entities.Length);
            
            _freeIndexes.Clear();
            foreach (var freeIndex in other._freeIndexes)
                _freeIndexes.Enqueue(freeIndex);
            
            _idToIndex.Clear();
            foreach(var kvp in other._idToIndex)
                _idToIndex.Add(kvp.Key, kvp.Value);
        }
    }
}