namespace Tofunaut.TofuECS
{
    public interface ISystem
    {
        void Initialize(ECS ecs);
        void Process(ECS ecs);
    }
}