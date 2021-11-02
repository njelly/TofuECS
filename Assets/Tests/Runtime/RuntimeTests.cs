using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Tofunaut.TofuECS;
using Tofunaut.TofuECS.Math;
using Tofunaut.TofuECS.Unity;
using Tofunaut.TofuECS.Utilities;
using UnityEngine;

public class RuntimeTests
{
    [Test]
    public void RuntimeTestsSimplePasses()
    {
        var seed = (ulong)1993;
        var sim = new Simulation(new DummySimulationConfig(seed), new UnityLogService(),
            new DummyInputProvider(),
            new ISystem[]
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

        // roll back and ensure the value is the same as at the start of frame 1
        sim.RollbackTo(1);
        Assert.IsTrue(sim.CurrentFrame.GetComponent<TestComponent>(entityIdA).Value == 1);
        
        // destroy entity and ensure it is no longer valid
        sim.Tick();
        sim.CurrentFrame.DestroyEntity(entityIdA);
        Assert.IsTrue(!sim.CurrentFrame.IsValid(entityIdA));
        
        // the entity should now exist again
        sim.RollbackTo(1);
        Assert.IsTrue(sim.CurrentFrame.IsValid(entityIdA));
        
        // verify rollback is working for RNG
        sim.Tick();
        var rollbackToFrame = sim.CurrentFrame.Number;
        var randNum = sim.CurrentFrame.RNG.NextUInt64();
        for (var i = 0; i < 10; i++)
        {
            sim.Tick();
            sim.CurrentFrame.RNG.NextUInt64();
        }
        sim.RollbackTo(rollbackToFrame);
        Assert.IsTrue(randNum == sim.CurrentFrame.RNG.NextUInt64());
    }

    [Test]
    public void MathTests()
    { 
        // FixVector2
        
        var a = new FixVector2(Fix64.One, Fix64.One);
        var b = new FixVector2(new Fix64(2), new Fix64(2));
        
        Assert.IsTrue(a + b == new FixVector2(new Fix64(3), new Fix64(3)));
        Assert.IsTrue(a - b == new FixVector2(new Fix64(-1), new Fix64(-1)));
        Assert.IsTrue(a * new Fix64(2) == b);
        Assert.IsTrue(a / new Fix64(2) == new FixVector2(new Fix64(1) / new Fix64(2), new Fix64(1) / new Fix64(2)));
        
        Assert.IsTrue(a.Magnitude == Fix64.Sqrt(new Fix64(2)));
        Assert.IsTrue(a.SqrMagnitude == new Fix64(2));
        Assert.IsTrue(a.ManhattanDistance == new Fix64(2));
        
        Assert.IsTrue(b.Magnitude == Fix64.Sqrt(new Fix64(8)));
        Assert.IsTrue(b.SqrMagnitude == new Fix64(8));
        Assert.IsTrue(b.ManhattanDistance == new Fix64(4));
        
        Assert.IsTrue(FixVector2.Right.Rotate(Fix64.PiOver2) == FixVector2.Up);
        Assert.IsTrue(FixVector2.Right.Rotate(Fix64.Pi) == FixVector2.Left);
        Assert.IsTrue(FixVector2.Right.Rotate(Fix64.Pi + Fix64.PiOver2) == FixVector2.Down);
        Assert.IsTrue(FixVector2.Right.Rotate(Fix64.PiTimes2) == FixVector2.Right);
        
        
        
        // FixAABB
        
        var aAABB = new FixAABB(FixVector2.Zero, Fix64.One, Fix64.One);
        var bAABB = new FixAABB(new FixVector2(new Fix64(1) / new Fix64(2), new Fix64(1) / new Fix64(2)), Fix64.One,
            Fix64.One);
        var cAABB = new FixAABB(new FixVector2(new Fix64(3) / new Fix64(2), new Fix64(3) / new Fix64(2)), Fix64.One,
            Fix64.One);
        
        Assert.IsTrue(aAABB.Intersects(bAABB));
        Assert.IsTrue(bAABB.Intersects(aAABB));
        Assert.IsFalse(aAABB.Intersects(cAABB));



        // RNG
        
        var r = new XorShiftRandom((ulong)DateTime.Now.Ticks);
        const int numIter = 100;
        var max = new Fix64(numIter);
        for (var i = numIter/-2; i < numIter/2; i++)
        {
            var min = new Fix64(i);
            var n = r.NextFix64ZeroOne() * (max - min) + min;
            Assert.IsTrue(n >= min && n < max);
        }

        for (var i = 0; i < numIter; i++)
        {
            var n = r.NextFix64ZeroOne();
            Assert.IsTrue(n >= Fix64.Zero && n < Fix64.One);
        }
    }

    [Test]
    public unsafe void UtilitiesTests()
    {
        // unmanaged quick sort (used in physics sim)
        const int length = 10;
        var intArray = stackalloc int[length];
        for (var i = 0; i < length; i++)
            intArray[i] = length - 1 - i;
        
        UnmanagedQuickSort.Sort(intArray, length, (a, b) => a < b);
        
        for(var i = 0; i < length - 1; i++)
            Assert.IsTrue(intArray[i] < intArray[i + 1]);
    }

    private class DummySimulationConfig : ISimulationConfig
    {
        public int MaxRollback => 60;
        public TData GetECSData<TData>(int id) where TData : unmanaged
        {
            return default;
        }

        public SimulationMode SimulationMode => SimulationMode.Offline;
        public int NumInputs => 1;
        public ulong Seed { get; }
        public int TicksPerSecond => 30;
        public PhysicsMode PhysicsMode => PhysicsMode.None;

        public TAsset GetAsset<TAsset>(int id)
        {
            return default;
        }

        public DummySimulationConfig(ulong seed)
        {
            Seed = seed;
        }
    }

    private class DummyInputProvider : InputProvider
    {
        public override Tofunaut.TofuECS.Input Poll(int index)
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
