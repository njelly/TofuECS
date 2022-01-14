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

        internal ComponentQuery(Simulation simulation, IComponentBuffer[] buffers)
        {
            _simulation = simulation;
            _buffers = buffers;
            _typeToChildren = new Dictionary<Type, ComponentQuery>();

            foreach (var buffer in _buffers)
            {
                buffer.OnComponentAdded += OnComponentAdded;
                buffer.OnComponentRemoved += OnComponentRemoved;
            }
            
            var entities = _buffers[0].GetEntities();
            for (var i = 1; i < _buffers.Length; i++)
                entities = entities.Intersect(_buffers[i].GetEntities());

            _entities = new HashSet<int>(entities);
        }

        private void OnComponentAdded(object sender, EntityEventArgs args)
        {
            if (_buffers.Where(x => x != sender).Any(x => !x.HasEntityAssignment(args.Entity)))
                return;

            _entities.Add(args.Entity);
        }
        
        private void OnComponentRemoved(object sender, EntityEventArgs args) => _entities.Remove(args.Entity);

        /// <summary>
        /// Access a more specific query for entities that share each component.
        /// </summary>
        public ComponentQuery And<TComponent>() where TComponent : unmanaged
        {
            if (_typeToChildren.TryGetValue(typeof(TComponent), out var componentQuery)) 
                return componentQuery;
            
            var buffers = new IComponentBuffer[_buffers.Length + 1];
            Array.Copy(_buffers, buffers, _buffers.Length);
            buffers[_buffers.Length] = _simulation.Buffer<TComponent>();
            componentQuery = new ComponentQuery(_simulation, buffers);
            _typeToChildren.Add(typeof(TComponent), componentQuery);
            return componentQuery;
        }

        /// <summary>
        /// Create an enumerator for all the entities that share components with the desired types.
        /// </summary>
        public IEnumerator<int> GetEnumerator() => _entities.GetEnumerator();
    }
}