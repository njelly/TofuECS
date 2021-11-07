namespace Tofunaut.TofuECS.Samples.ConwaysGameOfLife.ECS
{
    public interface ICOGLSimulationConfig : ISimulationConfig
    {
        int BoardConfigId { get; }
    }
}