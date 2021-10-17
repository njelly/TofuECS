namespace Tofunaut.TofuECS
{
    public interface IEntityComponentIterator
    {
        void AddEntity(int entityId);
        void RemoveEntity(int entityId);
        void Reset(Frame f);
        void Reset();
        void CopyFrom(IEntityComponentIterator other);
    }
}