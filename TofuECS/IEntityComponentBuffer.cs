using System;

namespace Tofunaut.TofuECS
{
    internal interface IEntityComponentBuffer : IDisposable
    {
        event EventHandler<EntityEventArgs> OnComponentAdded;
        event EventHandler<EntityEventArgs> OnComponentRemoved;
        bool HasEntityAssignment(int entityId);
        bool Remove(int entityId);
    }

    public class EntityEventArgs : EventArgs
    {
        public readonly int Entity;

        public EntityEventArgs(int entity)
        {
            Entity = entity;
        }
    }
}