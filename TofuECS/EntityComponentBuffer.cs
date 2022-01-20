using System;
using System.Collections.Generic;
using UnsafeCollections.Collections.Unsafe;

namespace Tofunaut.TofuECS
{
    /// <summary>
    /// A buffer containing the state information for the ECS.
    /// </summary>
    /// <typeparam name="TComponent">An unmanaged component type.</typeparam>
    public unsafe class EntityComponentBuffer<TComponent> : IEntityComponentBuffer where TComponent : unmanaged
    {
        public event EventHandler<EntityEventArgs> OnComponentAdded;
        public event EventHandler<EntityEventArgs> OnComponentRemoved;
        
        public int Size { get; }
        
        /// <summary>
        /// The number of components that have been assigned to entities.
        /// </summary>
        public int NumAssignedComponents => Size - _freeIndexes.Count;

        private readonly UnsafeArray* _arr;
        private readonly int[] _entityAssignments;
        private readonly Queue<int> _freeIndexes;
        private readonly Dictionary<int, int> _entityToIndex;

        internal EntityComponentBuffer(int size)
        {
            Size = size;
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
        /// <param name="component">The value of the component associated with the entity.</param>
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

        /// <summary>
        /// Get a pointer to the component associated with the entity id.
        /// </summary>
        /// <param name="entityId">A unique entity identifier.</param>
        /// <param name="component">A pointer to the component associated with the entity.</param>
        /// <returns>Returns false if no component is associated with the entity.</returns>
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
        
        /// <summary>
        /// Is the entity assigned to some component in the buffer.
        /// </summary>
        /// <param name="entityId">A unique entity identifier.</param>
        /// <returns></returns>
        public bool HasEntityAssignment(int entityId) => _entityToIndex.ContainsKey(entityId);

        /// <summary>
        /// Iterate over the buffer without creating garbage.
        /// </summary>
        /// <param name="i">The iterator to use.</param>
        /// <param name="entityId">The next valid entity assignment.</param>
        /// <param name="component">The next valid component value.</param>
        /// <returns>Returns true as long as there exists another valid entity-component assignment.</returns>
        public bool Next(ref int i, out int entityId, out TComponent component)
        {
            while (i < Size && _entityAssignments[i] == Simulation.InvalidEntityId)
                i++;

            if (i >= Size)
            {
                entityId = Simulation.InvalidEntityId;
                component = default;
                return false;
            }

            entityId = _entityAssignments[i];
            component = UnsafeArray.Get<TComponent>(_arr, i);
            i++;
            return true;
        }
        
        /// <summary>
        /// Iterate over the buffer without creating garbage (unsafe).
        /// </summary>
        /// <param name="i">The iterator to use.</param>
        /// <param name="entityId">The next valid entity assignment.</param>
        /// <param name="component">A pointer to the next valid component.</param>
        /// <returns>Returns true as long as there exists another valid entity-component assignment.</returns>
        public bool NextUnsafe(ref int i, out int entityId, out TComponent* component)
        {
            while (i < Size && _entityAssignments[i] == Simulation.InvalidEntityId)
                i++;

            if (i >= Size)
            {
                entityId = Simulation.InvalidEntityId;
                component = default;
                return false;
            }

            entityId = _entityAssignments[i];
            component = UnsafeArray.GetPtr<TComponent>(_arr, i);
            i++;
            return true;
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