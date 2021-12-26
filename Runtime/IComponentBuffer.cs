namespace Tofunaut.TofuECS
{
    internal interface IComponentBuffer
    {
        int Request();
        void Release(int index);
        void Recycle(IComponentBuffer other);
        void Dispose();
    }
}