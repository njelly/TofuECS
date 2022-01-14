using System;
using System.Collections.Generic;
using System.Linq;

namespace Tofunaut.TofuECS
{
    public class ComponentQuery
    {
        private readonly IComponentBuffer[] _buffers;
        private readonly Simulation _simulation;
        private readonly Dictionary<Type, ComponentQuery> _typeToChildren;
        private readonly HashSet<int> _entities;
        private readonly ComponentQuery _parent;

        internal ComponentQuery(Simulation simulation, IComponentBuffer buffer)
        {
            _simulation = simulation;
            _buffers = new [] { buffer };
            _typeToChildren = new Dictionary<Type, ComponentQuery>();

            buffer.OnComponentAdded += OnComponentAdded;
            buffer.OnComponentRemoved += OnComponentRemoved;

            _parent = null;
            
            _entities = new HashSet<int>(buffer.GetEntities());
        }

        private ComponentQuery(ComponentQuery parent, IComponentBuffer newBuffer)
        {
            _simulation = parent._simulation;
            _typeToChildren = new Dictionary<Type, ComponentQuery>();
            _buffers = new IComponentBuffer[parent._buffers.Length + 1];
            Array.Copy(parent._buffers, _buffers, parent._buffers.Length);
            _buffers[_buffers.Length - 1] = newBuffer;
            _entities = new HashSet<int>(parent._entities.Intersect(newBuffer.GetEntities()));

            _parent = parent;

            newBuffer.OnComponentAdded += OnComponentAdded;
            newBuffer.OnComponentRemoved += OnComponentRemoved;
        }

        private void OnComponentAdded(object sender, EntityEventArgs args)
        {
            if (_buffers.Where(x => x != sender).Any(x => !x.HasEntityAssignment(args.Entity)))
                return;

            _entities.Add(args.Entity);
            
            foreach (var kvp in _typeToChildren)
                kvp.Value.OnComponentAdded(sender, args);
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