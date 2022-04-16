using System;
using System.Collections.Generic;

namespace Tofunaut.TofuECS
{
    /// <summary>
    /// A buffer containing the state information for the ECS.
    /// </summary>
    /// <typeparam name="TComponent">An unmanaged component type.</typeparam>
    public abstract unsafe class EntityComponentBuffer<TComponent> : IEntityComponentBuffer where TComponent : unmanaged
    {
        public event Action<int> ComponentAddedToEntity;
        public event Action<int> ComponentRemovedFromEntity;

        public abstract int Size { get; }

        protected readonly List<int> _entityAssignments;
        protected readonly Queue<int> _freeIndexes;
        protected readonly Dictionary<int, int> _entityToIndex;

        internal EntityComponentBuffer(int size)
        {
            _entityAssignments = new List<int>(size);
            _freeIndexes = new Queue<int>(size);
            _entityToIndex = new Dictionary<int, int>(size);

            for (var i = size - 1; i >= 0; i--)
            {
                _entityAssignments.Add(Simulation.InvalidEntityId);
                _freeIndexes.Enqueue(i);
            }
        }

        /// <summary>
        /// Is the entity assigned to some component in the buffer.
        /// </summary>
        /// <param name="entityId">A unique entity identifier.</param>
        /// <returns></returns>
        public bool HasEntityAssignment(int entityId) => _entityToIndex.ContainsKey(entityId);

        public bool Remove(int entityId)
        {
            if(!_entityToIndex.TryGetValue(entityId, out var componentIndex))
                return false;
            
            _freeIndexes.Enqueue(componentIndex);
            _entityAssignments[componentIndex] = Simulation.InvalidEntityId;
            _entityToIndex.Remove(entityId);
            ComponentRemovedFromEntity?.Invoke(entityId);
            return true;
        }

        /// <summary>
        /// Copies the array of entity assignments to the given array (use cached array ot avoid extra gc alloc).
        /// </summary>
        /// <param name="entityAssignments"></param>
        public void GetEntityAssignments(int[] entityAssignments) => _entityAssignments.CopyTo(entityAssignments);
        

        /// <summary>
        /// Get the component associated with the entity id.
        /// </summary>
        /// <param name="entityId">A unique entity identifier.</param>
        /// <param name="component">The value of the component associated with the entity.</param>
        /// <returns>Returns false if no component is associated with the entity.</returns>
        public abstract bool Get(int entityId, out TComponent component);
        

        /// <summary>
        /// Gets the component assigned to the entity.
        /// </summary>
        /// <param name="entityId"></param>
        /// <returns></returns>
        /// <exception cref="EntityNotAssignedException{TComponent}">Thrown if the assignment does not exist.</exception>
        public abstract TComponent Get(int entityId);

        /// <summary>
        /// Get a pointer to the component associated with the entity id.
        /// </summary>
        /// <param name="entityId">A unique entity identifier.</param>
        /// <param name="component">A pointer to the component associated with the entity.</param>
        /// <returns>Returns false if no component is associated with the entity.</returns>
        public abstract bool GetUnsafe(int entityId, out TComponent* component);
        
        
        /// <summary>
        /// Gets a pointer to the component assigned to the entity.
        /// </summary>
        /// <param name="entityId"></param>
        /// <returns></returns>
        /// <exception cref="EntityNotAssignedException{TComponent}">Thrown if the assignment does not exist.</exception>
        public abstract TComponent* GetUnsafe(int entityId);
        
        /// <summary>
        /// Associates a component with an entity id. If the entity is already associated with this component type, the
        /// component will be overwritten.
        /// </summary>
        /// <param name="entityId">A unique entity identifier.</param>
        /// <param name="component">The component to associate with the entity.</param>
        /// <exception cref="BufferFullException{TComponent}">Will be thrown if all components are in use.</exception>
        public abstract void Set(int entityId, in TComponent component);

        /// <summary>
        /// Associates a component with an entity id using the default value of <typeparam name="TComponent"></typeparam>.
        /// If the entity is already associated with this component type, the component will be overwritten.
        /// </summary>
        /// <param name="entityId">A unique entity identifier.</param>
        /// <exception cref="BufferFullException{TComponent}">Will be thrown if all components are in use.</exception>
        public virtual void Set(int entityId)
        {
            ComponentAddedToEntity?.Invoke(entityId);
        }
        
        internal abstract void SetState(TComponent[] state, int[] entityAssignments);

        /// <summary>
        /// Copies the state of the buffer to the given array (use cached array to avoid gc alloc).
        /// </summary>
        public abstract void GetState(TComponent[] state);

        /// <summary>
        /// Iterate over the buffer without creating garbage.
        /// </summary>
        /// <param name="i">The iterator to use.</param>
        /// <param name="entityId">The next valid entity assignment.</param>
        /// <param name="component">The next valid component value.</param>
        /// <returns>Returns true as long as there exists another valid entity-component assignment.</returns>
        public abstract bool Next(ref int i, out int entityId, out TComponent component);
        
        /// <summary>
        /// Iterate over the buffer without creating garbage (unsafe).
        /// </summary>
        /// <param name="i">The iterator to use.</param>
        /// <param name="entityId">The next valid entity assignment.</param>
        /// <param name="component">A pointer to the next valid component.</param>
        /// <returns>Returns true as long as there exists another valid entity-component assignment.</returns>
        public abstract bool NextUnsafe(ref int i, out int entityId, out TComponent* component);
        
        public abstract void Dispose();
    }

    /// <summary>
    /// Thrown when the buffer is full and no more entities can be assigned.
    /// </summary>
    public class BufferFullException<TComponent> : Exception where TComponent : unmanaged
    {
        public override string Message =>
            $"Unable to complete the operation, the buffer of type {typeof(TComponent)} is full.";
    }

    public class EntityNotAssignedException<TComponent> : Exception where TComponent : unmanaged
    {
        public override string Message =>
            $"The component {typeof(TComponent)} is not assigned to the entity {_entityId}";
        private readonly int _entityId;
        public EntityNotAssignedException(int entityId)
        {
            _entityId = entityId;
        }
    }
}