using System;
using System.Collections.Generic;

namespace Tofunaut.TofuECS
{
    public class EntityComponentQuery
    {
        /// <summary>
        /// Returns the HashSet of Entity IDs in the Query as an IReadOnlyCollection.
        /// </summary>
        public IReadOnlyCollection<int> Entities => _entities;

        private readonly IEntityComponentBuffer _buffer;
        private readonly Simulation _simulation;
        private readonly Dictionary<Type, EntityComponentQuery> _typeToChildren;
        private readonly HashSet<int> _entities;
        private readonly EntityComponentQuery _parent;

        internal EntityComponentQuery(Simulation simulation, IEntityComponentBuffer buffer, HashSet<int> entities)
        {
            _simulation = simulation;
            _buffer = buffer;
            _typeToChildren = new Dictionary<Type, EntityComponentQuery>();
            _parent = null;
            _entities = entities;

            buffer.OnComponentAdded += OnComponentAdded;
            buffer.OnComponentRemoved += OnComponentRemoved;
        }

        private EntityComponentQuery(EntityComponentQuery parent, IEntityComponentBuffer buffer)
        {
            _simulation = parent._simulation;
            _buffer = buffer;
            _typeToChildren = new Dictionary<Type, EntityComponentQuery>();
            _parent = parent;

            _entities = new HashSet<int>();
            foreach(var entity in parent._entities)
                if (buffer.HasEntityAssignment(entity))
                    _entities.Add(entity);

            buffer.OnComponentAdded += OnComponentAdded;
            buffer.OnComponentRemoved += OnComponentRemoved;
        }

        private void OnComponentAdded(object sender, EntityEventArgs args)
        {
            var p = _parent;
            while (p != null)
            {
                if (!p._buffer.HasEntityAssignment(args.Entity))
                    return;
                
                p = p._parent;
            }
            
            _entities.Add(args.Entity);
            
            foreach (var kvp in _typeToChildren)
                kvp.Value.OnComponentAddedInternal(args.Entity);
        }

        private void OnComponentAddedInternal(int entity)
        {
            if (!_buffer.HasEntityAssignment(entity))
                return;
            
            _entities.Add(entity);
            
            foreach (var kvp in _typeToChildren)
                kvp.Value.OnComponentAddedInternal(entity);
        }

        private void OnComponentRemoved(object sender, EntityEventArgs args)
        {
            _entities.Remove(args.Entity);

            foreach (var kvp in _typeToChildren)
                kvp.Value.OnComponentRemoved(sender, args);
        }

        /// <summary>
        /// Access a more specific query for entities that share each component.
        /// </summary>
        public EntityComponentQuery And<TComponent>() where TComponent : unmanaged
        {
            if (_typeToChildren.TryGetValue(typeof(TComponent), out var componentQuery)) 
                return componentQuery;
            
            componentQuery = new EntityComponentQuery(this, _simulation.Buffer<TComponent>());
            _typeToChildren.Add(typeof(TComponent), componentQuery);
            return componentQuery;
        }
    }
}