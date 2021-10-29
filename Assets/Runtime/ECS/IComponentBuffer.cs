namespace Tofunaut.TofuECS
{
    internal interface IComponentBuffer
    {
        int NumInUse { get; }
        int Request();
        void Release(int index);
        void Recycle(IComponentBuffer other);
    }
}