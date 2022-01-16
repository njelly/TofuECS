﻿using System;
using System.Collections;
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
            using (var s = new Simulation(new TestLogService(), new ISystem[]
                {
                    new SomeValueSystem(),
                }))
            {
                s.RegisterSingletonComponent<SomeValueComponent>();
                s.RegisterSingletonComponent(new XorShiftRandom(1234));

                s.Initialize();

                // just tick for a while, doesn't really matter
                const int numTicks = 10;
                for(var i = 0; i < numTicks; i++)
                    s.Tick();

                // get the current state of the sim
                var rollbackTickNumber = s.CurrentTick;
                var xorShiftState = s.GetSingletonComponent<XorShiftRandom>();
                var someValueComponentBuffer = s.Buffer<SomeValueComponent>();
                var someValueComponentState = new SomeValueComponent[someValueComponentBuffer.Size];
                var someValueComponentAssignments = new int[someValueComponentBuffer.Size];
                someValueComponentBuffer.GetState(someValueComponentState);
                someValueComponentBuffer.GetEntityAssignments(someValueComponentAssignments);
            
                // tick once
                s.Tick();

                // this is the value we'll be verifying
                var someValueComponent = s.GetSingletonComponent<SomeValueComponent>();
                var randValue = someValueComponent.RandomValue;

                // now just keep ticking into the future, it shouldn't really matter how many times
                for(var i = 0; i < numTicks; i++)
                    s.Tick();
            
                // ROLLBACK!
                s.SetSingletonComponent(xorShiftState);
                s.SetState(someValueComponentState, someValueComponentAssignments, rollbackTickNumber);
            
                // Our sim ought to be deterministic, so we Tick once just like we did after we got the state, and all our
                // values should be the same.
                s.Tick();
            
                someValueComponent = s.GetSingletonComponent<SomeValueComponent>();
                Assert.True(someValueComponent.RandomValue == randValue);
            }
        }
        
        [Test]
        public void AddRemoveComponentTest()
        {
            using (var s = new Simulation(new TestLogService(), new ISystem[]
                   {
                       new SomeValueSystem(),
                   }))
            {
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
        }

        [Test]
        public void SystemEventTests()
        {
            using (var s = new Simulation(new TestLogService(), new ISystem[]
                   {
                       new SystemEventTestSystem(),
                   }))
            {
                s.RegisterSingletonComponent<SomeValueComponent>();
                s.Initialize();

                const int numTicks = 10;
                for (var i = 0; i < numTicks; i++)
                    s.Tick();

                var someValueComponent = s.GetSingletonComponent<SomeValueComponent>();
                Assert.IsTrue(someValueComponent.EventIncrementingValue == numTicks);
            }
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

        [Test]
        public void QueryTest()
        {
            using (var s = new Simulation(new TestLogService(), new ISystem[] { }))
            {
                s.RegisterComponent<TestComponentA>(2);
                s.RegisterComponent<TestComponentB>(2);
                
                s.Initialize();

                var entityA = s.CreateEntity();
                var entityB = s.CreateEntity();
                
                s.Buffer<TestComponentA>().Set(entityA);
                s.Buffer<TestComponentB>().Set(entityA);
                s.Buffer<TestComponentA>().Set(entityB);
                s.Buffer<TestComponentB>().Set(entityB);

                void validateEntitiesHaveComponents(ComponentQuery query)
                {
                    foreach (var entity in query.Entities)
                    {
                        Assert.True(s.Buffer<TestComponentA>().Get(entity, out _));
                        Assert.True(s.Buffer<TestComponentB>().Get(entity, out _));
                    }
                }

                void validate()
                {
                    var aFirst = s.Query<TestComponentA>().And<TestComponentB>();
                    var bFirst = s.Query<TestComponentB>().And<TestComponentA>();
                    
                    validateEntitiesHaveComponents(aFirst);
                    validateEntitiesHaveComponents(bFirst);
                    
                    Assert.True(aFirst.Entities.Count == bFirst.Entities.Count);
                }

                var aAndB = s.Query<TestComponentA>().And<TestComponentB>();
                
                // remove the entities from the buffers
                Assert.True(aAndB.Entities.Count == 2);
                validate();
                s.Buffer<TestComponentA>().Remove(entityA);
                Assert.True(aAndB.Entities.Count == 1);
                validate();
                s.Buffer<TestComponentB>().Remove(entityB);
                Assert.True(aAndB.Entities.Count == 0);
                validate();
                
                
                // add them back
                s.Buffer<TestComponentB>().Set(entityB);
                Assert.True(aAndB.Entities.Count == 1);
                validate();
                s.Buffer<TestComponentA>().Set(entityA);
                Assert.True(aAndB.Entities.Count == 2);
                validate();
            }
        }

        private struct SomeValueComponent
        {
            public int IncrementingValue;
            public int RandomValue;
            public int EventIncrementingValue;
        }

        private struct TestComponentA
        {
        }

        private struct TestComponentB
        {
        }
        
        private class SomeValueSystem : ISystem
        {
            public void Initialize(Simulation s) { }

            public unsafe void Process(Simulation s)
            {
                var i = s.Buffer<SomeValueComponent>().GetIterator();
                var r = s.GetSingletonComponentUnsafe<XorShiftRandom>();
                while (i.Next())
                {
                    i.CurrentUnsafe->IncrementingValue++;
                    i.CurrentUnsafe->RandomValue = r->NextInt32();
                }
            }
        }

        private class SystemEventTestSystem : ISystem, ISystemEventListener<IncrementValueSystemEvent>
        {
            public void Initialize(Simulation s) { }

            public void Process(Simulation s)
            {
                var entities = s.Buffer<SomeValueComponent>().GetEntities();
                foreach (var e in entities)
                {
                    s.SystemEvent(new IncrementValueSystemEvent
                    {
                        EntityId = e,
                    });
                }
            }
            
            public unsafe void OnSystemEvent(Simulation s, in IncrementValueSystemEvent data)
            {
                if (!s.Buffer<SomeValueComponent>().GetUnsafe(data.EntityId, out var someValueComponent))
                    return;

                someValueComponent->EventIncrementingValue++;
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