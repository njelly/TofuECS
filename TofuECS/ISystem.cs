namespace Tofunaut.TofuECS
{
    public interface ISystem
    {
        void Initialize(Simulation s);
        void Process(Simulation s);
    }
}