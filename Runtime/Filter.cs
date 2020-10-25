using System;
using System.Collections.Generic;
using Tofunaut.TofuECS.Interfaces;

namespace Tofunaut.TofuECS
{
    public class Filter<TComponent> where TComponent : class, IComponent, new()
    {
        public int Count => _entities.Length;
        
        private readonly Frame _f;
        private readonly int[] _entities;
        private int _currentIndex;
        
        internal Filter(Frame frame, int[] entities)
        {
            _f = frame;
            _entities = entities;
            _currentIndex = 0;
        }
        
        public bool Next(out int entity, out TComponent component)
        {
            entity = 0;
            component = null;

            if (_currentIndex >= _entities.Length)
                return false;

            entity = _entities[_currentIndex];

            if (!_f.TryGetComponent(_entities[_currentIndex], out component)) 
                return false;
            
            _currentIndex++;
            return true;

        }
    }
}