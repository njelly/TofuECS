using System.Collections;
using System.Collections.Generic;

namespace Tofunaut.TofuECS
{
    public class ComponentIterator<TComponent>where TComponent : unmanaged
    {
        public TComponent Current => _buffer.GetAt(_currentIteratorIndex);
        public int CurrentEntity => _buffer.GetEntityAt(_currentIteratorIndex);
        
        private readonly ComponentBuffer<TComponent> _buffer;
        private int _currentIteratorIndex;

        internal ComponentIterator(ComponentBuffer<TComponent> buffer)
        {
            _buffer = buffer;
            _currentIteratorIndex = -1;
        }

        public bool Next()
        {
            _currentIteratorIndex++;
            while (_currentIteratorIndex < _buffer.Size &&
                   _buffer.GetEntityAt(_currentIteratorIndex) == Simulation.InvalidEntityId)
                _currentIteratorIndex++;

            return _currentIteratorIndex < _buffer.Size;
        }

        public void ModifyCurrent(ModifyDelegate<TComponent> modifyDelegate) =>
            _buffer.ModifyAt(_currentIteratorIndex, modifyDelegate);

        public void ModifyCurrentUnsafe(ModifyDelegateUnsafe<TComponent> modifyDelegateUnsafe) =>
            _buffer.ModifyAtUnsafe(_currentIteratorIndex, modifyDelegateUnsafe);
    }
}