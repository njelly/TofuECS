using System;
using System.Collections.Generic;
using Tofunaut.TofuECS.Interfaces;

namespace Tofunaut.TofuECS
{
    public unsafe class Filter<TComponent> where TComponent : unmanaged, IComponent
    {
        private readonly Frame _f;
        private int[] _entities;
        private int _currentIndex;
        
        internal Filter(Frame frame, int[] entities)
        {
            _f = frame;
            _entities = entities;
            _currentIndex = 0;
        }
        
        public bool Next(out int entity, out TComponent* component)
        {
            entity = _entities[_currentIndex++];
            component = null;
            
            return _currentIndex < _entities.Length && _f.TryGetComponent(entity, out component);
        }
    }
}