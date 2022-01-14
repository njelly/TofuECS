using System;
using System.Collections.Generic;

namespace Tofunaut.TofuECS
{
    internal interface IComponentBuffer
    {
        event EventHandler<EntityEventArgs> OnComponentAdded;
        event EventHandler<EntityEventArgs> OnComponentRemoved;
        IEnumerable<int> GetEntities();
        bool HasEntityAssignment(int entity);
    }

    public class EntityEventArgs : EventArgs
    {
        public readonly int Entity;

        internal EntityEventArgs(int entity)
        {
            Entity = entity;
        }
    }
}