using System;
using System.Collections.Generic;

namespace Tofunaut.TofuECS
{
    public class ComponentQuery
    {
        private readonly IComponentBuffer _buffer;
        private readonly Simulation _simulation;
        private readonly Dictionary<Type, ComponentQuery> _typeToChildren;
        private readonly HashSet<int> _entities;
        private readonly ComponentQuery _parent;

        internal ComponentQuery(Simulation simulation, IComponentBuffer buffer)
        {
            _simulation = simulation;
            _buffer = buffer;
            _typeToChildren = new Dictionary<Type, ComponentQuery>();
            _parent = null;
            
            _entities = new HashSet<int>(buffer.GetEntities());

            buffer.OnComponentAdded += OnComponentAdded;
            buffer.OnComponentRemoved += OnComponentRemoved;
        }

        private ComponentQuery(ComponentQuery parent, IComponentBuffer buffer)
        {
            _simulation = parent._simulation;
            _buffer = buffer;
            _typeToChildren = new Dictionary<Type, ComponentQuery>();
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
        public ComponentQuery And<TComponent>() where TComponent : unmanaged
        {
            if (_typeToChildren.TryGetValue(typeof(TComponent), out var componentQuery)) 
                return componentQuery;
            
            componentQuery = new ComponentQuery(this, _simulation.Buffer<TComponent>());
            _typeToChildren.Add(typeof(TComponent), componentQuery);
            return componentQuery;
        }

        /// <summary>
        /// Create an enumerator for all the entities that share components with the desired types.
        /// </summary>
        public IEnumerator<int> GetEnumerator() => _entities.GetEnumerator();
    }
}