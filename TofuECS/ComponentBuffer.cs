using System;
using System.Collections.Generic;

namespace Tofunaut.TofuECS
{
    public delegate void ModifyDelegate<TComponent>(ref TComponent component) where TComponent : unmanaged;
    public unsafe delegate void ModifyDelegateUnsafe<TComponent>(TComponent* component) where TComponent : unmanaged;
    public unsafe delegate void ModifyWithIteratorDelegateUnsafe<TComponent>(
        ComponentBuffer<TComponent>.Iterator i, TComponent* buffer) where TComponent : unmanaged;
    
    /// <summary>
    /// A buffer containing the state information for the ECS.
    /// </summary>
    /// <typeparam name="TComponent">An unmanaged component type.</typeparam>
    public class ComponentBuffer<TComponent> : IComponentBuffer where TComponent : unmanaged
    {
        public event EventHandler<EntityEventArgs> OnComponentAdded;
        public event EventHandler<EntityEventArgs> OnComponentRemoved; 
        public int Size => _buffer.Length;

        private readonly TComponent[] _buffer;
        private readonly int[] _entityAssignments;
        private readonly Queue<int> _freeIndexes;
        private readonly Dictionary<int, int> _entityToIndex;

        internal ComponentBuffer(int size)
        {
            _buffer = new TComponent[size];
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
                component = _buffer[componentIndex];
                return true;
            }

            component = default;
            return false;
        }

        /// <summary>
        /// Get the component associated with the entity id and modify it in a safe context.
        /// </summary>
        /// <param name="entityId">A unique entity identifier.</param>
        /// <param name="modifyDelegate">A delegate that can directly modify the component by passing it with the ref keyword.</param>
        /// <returns>Returns false if no component is associated with the entity.</returns>
        public bool GetAndModify(int entityId, ModifyDelegate<TComponent> modifyDelegate)
        {
            if (!_entityToIndex.TryGetValue(entityId, out var componentIndex)) 
                return false;
            
            modifyDelegate.Invoke(ref _buffer[componentIndex]);
            return true;
        }

        /// <summary>
        /// Get the component associated with the entity id and modify it in an unsafe context.
        /// </summary>
        /// <param name="entityId">A unique entity identifier.</param>
        /// <param name="modifyDelegateUnsafe">A delegate that can directly modify the component via a pointer.</param>
        /// <returns>Returns false if no component is associated with the entity.</returns>
        public unsafe bool GetAndModifyUnsafe(int entityId, ModifyDelegateUnsafe<TComponent> modifyDelegateUnsafe)
        {
            if (!_entityToIndex.TryGetValue(entityId, out var componentIndex)) 
                return false;

            fixed (TComponent* ptr = &_buffer[componentIndex])
                modifyDelegateUnsafe.Invoke(ptr);

            return true;
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
                _buffer[componentIndex] = component;
                return;
            }
            
            if (_freeIndexes.Count <= 0)
                throw new BufferFullException<TComponent>();
            
            componentIndex = _freeIndexes.Dequeue();
            _entityToIndex.Add(entityId, componentIndex);
            _entityAssignments[componentIndex] = entityId;
            _buffer[componentIndex] = component;
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
                _buffer[componentIndex] = new TComponent();
                return;
            }
            
            if (_freeIndexes.Count <= 0)
                throw new BufferFullException<TComponent>();
            
            componentIndex = _freeIndexes.Dequeue();
            _entityToIndex.Add(entityId, componentIndex);
            _entityAssignments[componentIndex] = entityId;
            _buffer[componentIndex] = new TComponent();
            OnComponentAdded?.Invoke(this, new EntityEventArgs(entityId));
        }

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
            Array.Copy(state, _buffer, Math.Min(state.Length, _buffer.Length));
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

        internal void GetState(out TComponent[] state, out int[] entityAssignments)
        {
            state = new TComponent[_buffer.Length];
            entityAssignments = new int[_entityAssignments.Length];
            Array.Copy(_buffer, state, _buffer.Length);
            Array.Copy(_entityAssignments, entityAssignments, _entityAssignments.Length);
        }

        internal void GetFirst(out TComponent component) => component = _buffer[0];
        internal void ModifyFirst(ModifyDelegate<TComponent> modifyDelegate) => modifyDelegate(ref _buffer[0]);
        internal unsafe void ModifyFirstUnsafe(ModifyDelegateUnsafe<TComponent> modifyDelegateUnsafe)
        {
            fixed (TComponent* ptr = &_buffer[0])
                modifyDelegateUnsafe(ptr);
        }

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
        /// Use an iterator to modify the buffer directly via a fixed pointer. This is the fastest way of iterating
        /// through a component buffer.
        /// </summary>
        /// <param name="modifyDelegate">A delegate that provides an iterator and access to the raw buffer data.</param>
        public unsafe void ModifyUnsafe(ModifyWithIteratorDelegateUnsafe<TComponent> modifyDelegate)
        {
            var iterator = new Iterator(this);
            fixed (TComponent* ptr = _buffer)
                modifyDelegate.Invoke(iterator, ptr);
        }
        
        /// <summary>
        /// Allows iterating safely over a pointer to the buffer data directly.
        /// </summary>
        public class Iterator
        {
            /// <summary>
            /// The current index for accessing data in the buffer. NOT the entity.
            /// </summary>
            public int Current { get; private set; }
            
            /// <summary>
            /// The Entity assigned to the component at the current index.
            /// </summary>
            public int Entity => _buffer._entityAssignments[Current];

            private readonly ComponentBuffer<TComponent> _buffer;

            internal Iterator(ComponentBuffer<TComponent> buffer)
            {
                _buffer = buffer;
                Current = -1;
            }

            /// <summary>
            /// Returns true as long as there are more components assigned to entities in the buffer.
            /// </summary>
            public bool Next()
            {
                Current++;
                while (Current < _buffer._entityAssignments.Length &&
                       _buffer._entityAssignments[Current] == Simulation.InvalidEntityId)
                    Current++;

                return Current < _buffer._entityAssignments.Length;
            }
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