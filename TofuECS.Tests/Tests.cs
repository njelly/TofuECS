﻿using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tofunaut.TofuECS;
using Tofunaut.TofuECS.Utilities;

namespace TofuECS.Tests
{
    [TestFixture]
    public class Tests
    {
        [Test]
        public void RollbackTest()
        {
            var s = new Simulation(new TestLogService(), new ISystem[]
            {
                new SomeValueSystem(),
            });
            
            s.RegisterSingletonComponent<SomeValueComponent>();
            s.RegisterSingletonComponent(new XorShiftRandom(1234));

            s.Initialize();

            // just tick for a while, doesn't really matter
            const int numTicks = 10;
            for(var i = 0; i < numTicks; i++)
                s.Tick();

            // get the current state of the sim
            var rollbackTickNumber = s.CurrentTick;
            s.GetState<SomeValueComponent>(out var someValueState, out var someValueAssignments);
            s.GetState<XorShiftRandom>(out var xorShiftRandomState, out var xorShiftAssignments);
            
            // tick once
            s.Tick();

            // this is the value we'll be verifying
            var randValue = 0;
            if (s.GetSingletonComponent<SomeValueComponent>(out var someValueComponent))
                randValue = someValueComponent.RandomValue;

            // now just keep ticking into the future, it shouldn't really matter how many times
            for(var i = 0; i < numTicks; i++)
                s.Tick();
            
            // ROLLBACK!
            s.SetState(someValueState, someValueAssignments, rollbackTickNumber);
            s.SetState(xorShiftRandomState, xorShiftAssignments, rollbackTickNumber);
            
            // Our sim ought to be deterministic, so we Tick once just like we did after we got the state, and all our
            // values should be the same.
            s.Tick();
            
            Assert.True(s.GetSingletonComponent(out someValueComponent));
            Assert.True(someValueComponent.RandomValue == randValue);
        }
        
        [Test]
        public void AddRemoveComponentTest()
        {
            var s = new Simulation(new TestLogService(), new ISystem[]
            {
                new SomeValueSystem(),
            });
            
            s.RegisterSingletonComponent<XorShiftRandom>();
            s.RegisterComponent<SomeValueComponent>(16);

            s.Initialize();

            var e = s.CreateEntity();
            
            s.Buffer<SomeValueComponent>().Set(e);
            
            s.Tick();
            
            Assert.True(s.Buffer<SomeValueComponent>().Get(e, out var someValueComponent) && someValueComponent.IncrementingValue == 1);
            Assert.True(s.Buffer<SomeValueComponent>().Remove(e));
            Assert.False(s.Buffer<SomeValueComponent>().Get(e, out someValueComponent));
            
            s.Tick();
            
            s.Buffer<SomeValueComponent>().Set(e);
            
            s.Tick();
            
            Assert.True(s.Buffer<SomeValueComponent>().Get(e, out someValueComponent) && someValueComponent.IncrementingValue == 1);
        }

        [Test]
        public void SystemEventTests()
        {
            var ecs = new Simulation(new TestLogService(), new ISystem[]
            {
                new SystemEventTestSystem(),
            });
            
            ecs.RegisterSingletonComponent<SomeValueComponent>();
            ecs.Initialize();

            const int numTicks = 10;
            for (var i = 0; i < numTicks; i++)
                ecs.Tick();

            Assert.IsTrue(ecs.GetSingletonComponent(out SomeValueComponent someValueComponent) &&
                          someValueComponent.EventIncrementingValue == numTicks);
        }

        [Test]
        public void ArrayQuickSortTest()
        {
            var arr = new []
            {
                10,
                139,
                -49,
                193545,
                1,
                -9393,
                123,
                124,
                9
            };

            var prevLength = arr.Length;

            ArrayQuickSort.Sort(arr, (a, b) => a.CompareTo(b));

            Assert.True(prevLength == arr.Length);
            
            for(var i = 1; i < arr.Length; i++)
                Assert.IsTrue(arr[i] > arr[i - 1]);
        }

        private struct SomeValueComponent
        {
            public int IncrementingValue;
            public int RandomValue;
            public int EventIncrementingValue;
        }
        
        private class SomeValueSystem : ISystem
        {
            public void Initialize(Simulation s) { }

            public void Process(Simulation s)
            {
                var someValueIterator = s.Buffer<SomeValueComponent>().GetIterator();
                while (someValueIterator.Next())
                {
                    someValueIterator.Modify((ref SomeValueComponent component) =>
                    {
                        component.IncrementingValue++;
                        var randValue = 0;
                        // THIS IS NOT VALID: s.ModifySingletonComponent((ref XorShiftRandom r) => component.RandomValue = r.NextInt32())
                        s.ModifySingletonComponent((ref XorShiftRandom random) =>
                        {
                            randValue = random.NextInt32();
                        });
                        component.RandomValue = randValue;
                    });
                }
            }
        }

        private class SystemEventTestSystem : ISystem, ISystemEventListener<IncrementValueSystemEvent>
        {
            public void Initialize(Simulation s) { }

            public void Process(Simulation s)
            {
                var someValueIterator = s.Buffer<SomeValueComponent>().GetIterator();
                while (someValueIterator.Next())
                {
                    s.SystemEvent(new IncrementValueSystemEvent
                    {
                        EntityId = someValueIterator.Entity,
                    });
                }
            }
            
            public void OnSystemEvent(Simulation simulation, in IncrementValueSystemEvent data)
            {
                simulation.Buffer<SomeValueComponent>().GetAndModify(data.EntityId,
                    (ref SomeValueComponent someValueComponent) => { someValueComponent.EventIncrementingValue++; });
            }
        }

        public class TestLogService : ILogService
        {
            public void Debug(string s) => Console.WriteLine($"[DEBUG] {s}");
            public void Info(string s) => Console.WriteLine($"[INFO] {s}");
            public void Warn(string s) => Console.WriteLine($"[WARN] {s}");
            public void Error(string s) => Console.WriteLine($"[ERROR] {s}");
            public void Exception(Exception e) => throw e;
        }

        private struct IncrementValueSystemEvent
        {
            public int EntityId;
        }
    }
}