namespace Tofunaut.TofuECS
{
    internal interface IEntityComponentIterator
    {
        void AddEntity(Entity entity);
        void RemoveEntity(Entity entity);
    }
}