using NUnit.Framework;
using Tofunaut.TofuECS;
using UnityEngine;

public class RuntimeTests
{
    // A Test behaves as an ordinary method
    [Test]
    public void RuntimeTestsSimplePasses()
    {
        var sim = new Simulation(new DummySimulationConfig(),
            new DummyInputProvider(),
            new[]
            {
                new TestSystem(),
            });

        sim.RegisterComponent<TestComponent>();

        var entityIdA = sim.CurrentFrame.CreateEntity();
        sim.CurrentFrame.AddComponent<TestComponent>(entityIdA);

        for(var i = 0; i < 100; i++)
        {
            var e = sim.CurrentFrame.CreateEntity();
            sim.CurrentFrame.AddComponent<TestComponent>(e);
        }

        Assert.IsTrue(sim.CurrentFrame.GetComponent<TestComponent>(entityIdA).Value == 0);

        sim.Tick();

        Assert.IsTrue(sim.CurrentFrame.GetComponent<TestComponent>(entityIdA).Value == 1);

        sim.Tick();
        sim.Tick();

        Assert.IsTrue(sim.CurrentFrame.GetComponent<TestComponent>(entityIdA).Value == 3);

        sim.RollbackTo(1);

        Assert.IsTrue(sim.CurrentFrame.GetComponent<TestComponent>(entityIdA).Value == 1);

        sim.Tick();

        Assert.IsTrue(sim.CurrentFrame.GetComponent<TestComponent>(entityIdA).Value == 2);
        
        sim.CurrentFrame.DestroyEntity(entityIdA);
        
        Assert.IsTrue(!sim.CurrentFrame.IsValid(entityIdA));

        sim.Tick();
        
        Assert.IsTrue(!sim.CurrentFrame.IsValid(entityIdA));

        var entityB = sim.CurrentFrame.CreateEntity();
        
        Assert.IsTrue(sim.CurrentFrame.IsValid(entityB));
    }

    private class DummySimulationConfig : ISimulationConfig
    {
        public int MaxRollback => 60;

        public SimulationMode Mode => SimulationMode.Offline;

        public int NumInputs => 1;

        public TAsset GetAsset<TAsset>(int id)
        {
            return default;
        }
    }

    private class DummyInputProvider : InputProvider
    {
        public override Tofunaut.TofuECS.Input GetInput(int index)
        {
            return new DummyInput();
        }
    }

    private class DummyInput : Tofunaut.TofuECS.Input
    {

    }

    private struct TestComponent
    {
        public int Value;
    }

    private unsafe class TestSystem : ISystem
    {
        public void Process(Frame f)
        {
            var iter = f.GetIterator<TestComponent>();
            while (iter.NextUnsafe(out var entity, out var testComponent))
                testComponent->Value++;
        }
    }
}
