using System;
using System.Collections;
using System.Collections.Generic;
using UnsafeCollections.Collections.Native;
using UnsafeCollections.Collections.Unsafe;

namespace Tofunaut.TofuECS
{
    /// <summary>
    /// A buffer containing the state information for the ECS.
    /// </summary>
    /// <typeparam name="TComponent">An unmanaged component type.</typeparam>
    public unsafe class ComponentBuffer<TComponent> : IComponentBuffer where TComponent : unmanaged
    {
        public event EventHandler<EntityEventArgs> OnComponentAdded;
        public event EventHandler<EntityEventArgs> OnComponentRemoved;
        public int Size => UnsafeArray.GetLength(_arr);

        private readonly UnsafeArray* _arr;
        private readonly int[] _entityAssignments;
        private readonly Queue<int> _freeIndexes;
        private readonly Dictionary<int, int> _entityToIndex;

        internal ComponentBuffer(int size)
        {
            _arr = UnsafeArray.Allocate<TComponent>(size);
            _entityAssignments = new int[size];
            _freeIndexes = new Queue<int>(size);
            _entityToIndex = new Dictionary<int, int>();

            for (var i = size - 1; i >= 0; i--)
            {
                _entityAssignments[i] = Simulation.InvalidEntityId;
                _freeIndexes.Enqueue(i);
            }
        }

        /// <summary>
        /// Get the component associated with the entity id.
        /// </summary>
        /// <param name="entityId">A unique entity identifier.</param>
        /// <param name="component">The component associated with the entity.</param>
        /// <returns>Returns false if no component is associated with the entity.</returns>
        public bool Get(int entityId, out TComponent component)
        {
            if (_entityToIndex.TryGetValue(entityId, out var componentIndex))
            {
                component = UnsafeArray.Get<TComponent>(_arr, componentIndex);
                return true;
            }

            component = default;
            return false;
        }

        public bool GetUnsafe(int entityId, out TComponent* component)
        {
            if (_entityToIndex.TryGetValue(entityId, out var componentIndex))
            {
                component = UnsafeArray.GetPtr<TComponent>(_arr, componentIndex);
                return true;
            }

            component = null;
            return false;
        }

        /// <summary>
        /// Associates a component with an entity id. If the entity is already associated with this component type, the
        /// component will be overwritten.
        /// </summary>
        /// <param name="entityId">A unique entity identifier.</param>
        /// <param name="component">The component to associate with the entity.</param>
        /// <exception cref="BufferFullException{TComponent}">Will be thrown if all components are in use.</exception>
        public void Set(int entityId, in TComponent component)
        {
            if (_entityToIndex.TryGetValue(entityId, out var componentIndex))
            {
                UnsafeArray.Set(_arr, componentIndex, component);
                return;
            }
            
            if (_freeIndexes.Count <= 0)
                throw new BufferFullException<TComponent>();
            
            componentIndex = _freeIndexes.Dequeue();
            _entityToIndex.Add(entityId, componentIndex);
            _entityAssignments[componentIndex] = entityId;
            UnsafeArray.Set(_arr, componentIndex, component);
            OnComponentAdded?.Invoke(this, new EntityEventArgs(entityId));
        }

        /// <summary>
        /// Associates a component with an entity id using the default value of <typeparam name="TComponent"></typeparam>.
        /// </summary>
        /// <param name="entityId">A unique entity identifier.</param>
        /// <exception cref="BufferFullException{TComponent}">Will be thrown if all components are in use.</exception>
        public void Set(int entityId)
        {
            if (_entityToIndex.TryGetValue(entityId, out var componentIndex))
            {
                UnsafeArray.Set(_arr, componentIndex, new TComponent());
                return;
            }
            
            if (_freeIndexes.Count <= 0)
                throw new BufferFullException<TComponent>();
            
            componentIndex = _freeIndexes.Dequeue();
            _entityToIndex.Add(entityId, componentIndex);
            _entityAssignments[componentIndex] = entityId;
            UnsafeArray.Set(_arr, componentIndex, new TComponent());
            OnComponentAdded?.Invoke(this, new EntityEventArgs(entityId));
        }

        /// <summary>
        /// Sets a value directly at the index of the buffer.
        /// </summary>
        public void SetAt(int bufferIndex, in TComponent component) => UnsafeArray.Set(_arr, bufferIndex, component);

        /// <summary>
        /// Removes an association between the entity and a component in the buffer, and allows a new entity to be
        /// associated with that component.
        /// </summary>
        /// <param name="entityId">A unique entity identifier.</param>
        /// <returns>Returns false if the entity was never associated with a component.</returns>
        public bool Remove(int entityId)
        {
            if(!_entityToIndex.TryGetValue(entityId, out var componentIndex))
                return false;
            
            _freeIndexes.Enqueue(componentIndex);
            _entityAssignments[componentIndex] = Simulation.InvalidEntityId;
            _entityToIndex.Remove(entityId);
            OnComponentRemoved?.Invoke(this, new EntityEventArgs(entityId));
            return true;
        }
        
        internal void SetState(TComponent[] state, int[] entityAssignments)
        {
            fixed (TComponent* statePtr = state)
                _arr->CopyFrom<TComponent>(statePtr, 0, Math.Min(state.Length, Size));
            
            Array.Copy(entityAssignments, _entityAssignments,
                Math.Min(entityAssignments.Length, _entityAssignments.Length));
            
            _freeIndexes.Clear();
            _entityToIndex.Clear();

            for (var i = _entityAssignments.Length - 1; i >= 0; i--)
            {
                if (_entityAssignments[i] == Simulation.InvalidEntityId)
                    _freeIndexes.Enqueue(i);
                else
                    _entityToIndex.Add(_entityAssignments[i], i);
            }
        }

        /// <summary>
        /// Copies the state of the buffer to the given array (use cached array to avoid extra gc alloc).
        /// </summary>
        public void GetState(TComponent[] state)
        {
            fixed (TComponent* statePtr = state)
                _arr->CopyTo<TComponent>(statePtr, 0);
        }

        /// <summary>
        /// Copies the array of entity assignments to the given array (use cached array ot avoid extra gc alloc).
        /// </summary>
        /// <param name="entityAssignments"></param>
        public void GetEntityAssignments(int[] entityAssignments) =>
            Array.Copy(_entityAssignments, entityAssignments, _entityAssignments.Length);
        
        public TComponent GetAt(int bufferIndex) => UnsafeArray.Get<TComponent>(_arr, bufferIndex);
        public TComponent* GetAtUnsafe(int bufferIndex) => UnsafeArray.GetPtr<TComponent>(_arr, bufferIndex);

        /// <summary>
        /// Access all the entities assigned to the buffer.
        /// </summary>
        public IEnumerable<int> GetEntities() => _entityToIndex.Keys;
        
        /// <summary>
        /// Is the entity assigned to some component in the buffer.
        /// </summary>
        /// <param name="entityId">A unique entity identifier.</param>
        /// <returns></returns>
        public bool HasEntityAssignment(int entityId) => _entityToIndex.ContainsKey(entityId);

        /// <summary>
        /// Creates a new instance of Iterator that allows safe access to all assigned components in the buffer.
        /// </summary>
        public Iterator GetIterator() => new Iterator(this);
        
        /// <summary>
        /// Allows iterating safely over a pointer to the buffer data directly.
        /// </summary>
        public class Iterator
        {
            /// <summary>
            /// The Entity assigned to the component at the current index.
            /// </summary>
            public int Entity => _buffer._entityAssignments[_currentIndex];
            
            /// <summary>
            /// A pointer to the current component.
            /// </summary>
            public TComponent* CurrentUnsafe => UnsafeArray.GetPtr<TComponent>(_buffer._arr, _currentIndex);
            
            /// <summary>
            /// The value of the current component.
            /// </summary>
            public TComponent Current => UnsafeArray.Get<TComponent>(_buffer._arr, _currentIndex);

            private readonly ComponentBuffer<TComponent> _buffer;
            private int _currentIndex;

            internal Iterator(ComponentBuffer<TComponent> buffer)
            {
                _buffer = buffer;
                _currentIndex = -1;
            }

            /// <summary>
            /// Returns true as long as there are more components assigned to entities in the buffer.
            /// </summary>
            public bool Next()
            {
                _currentIndex++;
                while (_currentIndex < _buffer._entityAssignments.Length && _buffer._entityAssignments[_currentIndex] == Simulation.InvalidEntityId)
                    _currentIndex++;

                return _currentIndex < _buffer._entityAssignments.Length;
            }
        }

        public void Dispose()
        {
            UnsafeArray.Free(_arr);
        }
    }

    /// <summary>
    /// Thrown when the buffer is full and no more entities can be assigned.
    /// </summary>
    public class BufferFullException<TComponent> : Exception where TComponent : unmanaged
    {
        public override string Message =>
            $"Unable to complete the operation, the buffer of type {typeof(TComponent)} is full.";
    }
}