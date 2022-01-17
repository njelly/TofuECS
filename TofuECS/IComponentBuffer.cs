using System;

namespace Tofunaut.TofuECS
{
    internal interface IComponentBuffer : IDisposable
    {
        event EventHandler<EntityEventArgs> OnComponentAdded;
        event EventHandler<EntityEventArgs> OnComponentRemoved;
        bool HasEntityAssignment(int entityId);
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