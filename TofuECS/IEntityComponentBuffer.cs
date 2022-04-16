using System;

namespace Tofunaut.TofuECS
{
    public interface IEntityComponentBuffer : IDisposable
    {
        event Action<int> ComponentAddedToEntity;
        event Action<int> ComponentRemovedFromEntity;
        bool HasEntityAssignment(int entityId);
        bool Remove(int entityId);
        void GetEntityAssignments(int[] entityAssignments);
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