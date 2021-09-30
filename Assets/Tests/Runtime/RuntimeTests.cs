using NUnit.Framework;
using Tofunaut.TofuECS;
using UnityEngine;

public class RuntimeTests
{
    // A Test behaves as an ordinary method
    [Test]
    public void RuntimeTestsSimplePasses()
    {
        var sim = new Simulation();
        sim.RegisterComponent<TestComponent>();
        sim.RegisterSystem(new TestSystem());

        var entityA = sim.CreateEntity();
        sim.AddComponent<TestComponent>(entityA);

        for(var i = 0; i < 100; i++)
        {
            var e = sim.CreateEntity();
            sim.AddComponent<TestComponent>(e);
        }

        Debug.Log(sim.GetComponent<TestComponent>(entityA).Value);
        Assert.IsTrue(sim.GetComponent<TestComponent>(entityA).Value == 0);

        sim.Tick();

        Debug.Log(sim.GetComponent<TestComponent>(entityA).Value);
        Assert.IsTrue(sim.GetComponent<TestComponent>(entityA).Value == 1);

        sim.Tick();
        sim.Tick();

        Debug.Log(sim.GetComponent<TestComponent>(entityA).Value);
        Assert.IsTrue(sim.GetComponent<TestComponent>(entityA).Value == 3);

        sim.DestroyEntity(entityA);

        sim.Tick();
    }

    private struct TestComponent
    {
        public int Value;
    }

    private unsafe class TestSystem : ISystem
    {
        public void Process(Simulation simulation)
        {
            var iter = simulation.GetIterator<TestComponent>();
            while (iter.NextUnsafe(out var entity, out var testComponent))
                testComponent->Value++;
        }
    }

    //// A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    //// `yield return null;` to skip a frame.
    //[UnityTest]
    //public IEnumerator RuntimeTestsWithEnumeratorPasses()
    //{
    //    // Use the Assert class to test conditions.
    //    // Use yield to skip a frame.
    //    yield return null;
    //}
}
