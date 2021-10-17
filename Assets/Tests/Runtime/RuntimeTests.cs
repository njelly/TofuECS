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

        // ensure we can register a component
        sim.RegisterComponent<TestComponent>();
        
        // negative entity values should never be valid
        Assert.IsTrue(!sim.CurrentFrame.IsValid(-1));

        // ensure that we can create an entity (and store it's id in a var), and add a component to it
        var entityIdA = sim.CurrentFrame.CreateEntity();
        sim.CurrentFrame.AddComponent<TestComponent>(entityIdA);
        
        // an entity that was just created should always be valid
        Assert.IsTrue(sim.CurrentFrame.IsValid(entityIdA));

        // create a bunch of entities and add components to them
        for(var i = 0; i < 100; i++)
        {
            var e = sim.CurrentFrame.CreateEntity();
            sim.CurrentFrame.AddComponent<TestComponent>(e);
            
            // an entity that was just created should always be valid
            Assert.IsTrue(sim.CurrentFrame.IsValid(entityIdA));
        }

        // ensure that we can retrieve the component and that the value of the component is correct
        Assert.IsTrue(sim.CurrentFrame.GetComponent<TestComponent>(entityIdA).Value == 0);

        // ensure Tick() works, and that we are now on frame 1
        sim.Tick();
        Assert.IsTrue(sim.CurrentFrame.Number == 1);

        // TestSystem should have incremented the value by 1
        Assert.IsTrue(sim.CurrentFrame.GetComponent<TestComponent>(entityIdA).Value == 1);

        // Tick twice and ensure the value is incremented by 2 (to 3)
        sim.Tick();
        sim.Tick();
        Assert.IsTrue(sim.CurrentFrame.GetComponent<TestComponent>(entityIdA).Value == 3);

        // roll back and ensure the value is the same as on frame 1
        sim.RollbackTo(1);
        Assert.IsTrue(sim.CurrentFrame.GetComponent<TestComponent>(entityIdA).Value == 1);
        
        // destroy entity and ensure it is no longer valid
        sim.Tick();
        sim.CurrentFrame.DestroyEntity(entityIdA);
        Assert.IsTrue(!sim.CurrentFrame.IsValid(entityIdA));
        
        // the entity should now exist again
        sim.RollbackTo(1);
        Assert.IsTrue(sim.CurrentFrame.IsValid(entityIdA));
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
        public void Initialize(Frame f) { }
        
        public void Dispose(Frame f) { }

        public void Process(Frame f)
        {
            var iter = f.GetIterator<TestComponent>();
            while (iter.NextUnsafe(out var entity, out var testComponent))
                testComponent->Value++;
        }
    }
}
