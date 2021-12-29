namespace Tofunaut.TofuECS
{
    public interface IECSDatabase
    {
        TData Get<TData>(int id) where TData : unmanaged;
    }
}