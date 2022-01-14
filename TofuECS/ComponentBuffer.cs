using System;
using System.Collections.Generic;

namespace Tofunaut.TofuECS
{
    public delegate void ModifyDelegate<TComponent>(ref TComponent component) where TComponent : unmanaged;
    public unsafe delegate void ModifyDelegateUnsafe<TComponent>(TComponent* component) where TComponent : unmanaged;
    
    /// <summary>
    /// A buffer containing the state information for the ECS.
    /// </summary>
    /// <typeparam name="TComponent">An unmanaged component type.</typeparam>
    public class ComponentBuffer<TComponent> where TComponent : unmanaged
    {
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
            return true;
        }

        /// <summary>
        /// Creates a new iterator to loop through the buffer.
        /// </summary>
        /// <returns>An instance of ComponentIterator.</returns>
        public ComponentIterator<TComponent> GetIterator() => new ComponentIterator<TComponent>(this);
        
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

        internal bool GetFirst(out TComponent component)
        {
            for (var i = 0; i < _buffer.Length; i++)
            {
                if (_entityAssignments[i] == Simulation.InvalidEntityId) 
                    continue;
                
                component = _buffer[i];
                return true;
            }

            component = default;
            return false;
        }
        
        internal void ModifyFirst(ModifyDelegate<TComponent> modifyDelegate) => modifyDelegate(ref _buffer[0]);

        internal unsafe void ModifyFirstUnsafe(ModifyDelegateUnsafe<TComponent> modifyDelegateUnsafe)
        {
            fixed (TComponent* ptr = &_buffer[0])
                modifyDelegateUnsafe(ptr);
        }

        internal int GetEntityAt(int index) => _entityAssignments[index];
        internal void GetAt(int index, out TComponent component) => component = _buffer[index];
        internal void ModifyAt(int index, ModifyDelegate<TComponent> modifyDelegate) =>
            modifyDelegate.Invoke(ref _buffer[index]);
        internal unsafe void ModifyAtUnsafe(int index, ModifyDelegateUnsafe<TComponent> modifyDelegateUnsafe)
        {
            fixed (TComponent* ptr = &_buffer[index])
                modifyDelegateUnsafe(ptr);
        }
    }

    public class BufferFullException<TComponent> : Exception where TComponent : unmanaged
    {
        public override string Message =>
            $"Unable to complete the operation, the buffer of type {typeof(TComponent)} is full.";
    }
}