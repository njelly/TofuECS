namespace Tofunaut.TofuECS
{
    public interface IEntityComponentIterator
    {
        void AddEntity(Entity entity);
        void RemoveEntity(Entity entity);
        void Reset();
        void CopyFrom(IEntityComponentIterator other);
    }
}