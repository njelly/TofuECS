using System;
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
                s.RegisterComponent<SomeValueComponent>(1);
                s.RegisterSingletonComponent(new XorShiftRandom(1234));

                s.Initialize();

                var entity = s.CreateEntity();
                s.Buffer<SomeValueComponent>().Set(entity);

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
                s.Buffer<SomeValueComponent>().Get(entity, out var someValueComponent);
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
            
                Assert.True(s.Buffer<SomeValueComponent>().Get(entity, out someValueComponent));
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
                const int numTicks = 10;
                const int numComponents = 10;

                s.RegisterComponent<SomeValueComponent>(numComponents);
                s.Initialize();

                var buffer = s.Buffer<SomeValueComponent>();
                for(var i = 0; i < numComponents; i++)
                    buffer.Set(s.CreateEntity());

                for (var i = 0; i < numTicks; i++)
                    s.Tick();

                var j = 0;
                while(buffer.Next(ref j, out _, out var someValueComponent))
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

                void validateEntitiesHaveComponents(EntityComponentQuery query)
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
        
        [Test]
        public void AnonymousBufferTest()
        {
            const int numCoordinates = 1000000;
            
            // This test simply creates a large number of components in an anonymous buffer, modifies them with a system, and confirms
            // the results.
            using (var s = new Simulation(new TestLogService(), new ISystem[] { new CoordinateSystem() }))
            {
                var index = s.RegisterAnonymousComponent<Coordinate>(numCoordinates);
                s.RegisterSingletonComponent(new CoordinateBufferData
                {
                    Index = index,
                });
                s.Initialize();

                const int numTicks = 10;
                while(s.CurrentTick < numTicks)
                    s.Tick();

                var buffer = s.AnonymousBuffer<Coordinate>(index);
                var i = 0;
                while(buffer.Next(ref i, out var c))
                    Assert.True(c.X == c.StartX + numTicks && c.Y == c.StartY + numTicks);
            }
        }

        [Test]
        public void DestroyEntityTest()
        {
            using (var s = new Simulation(new TestLogService(), new ISystem[] {new SomeValueSystem()}))
            {
                s.RegisterSingletonComponent(new XorShiftRandom(1234)); // necessary for SomeValueSystem
                s.RegisterComponent<SomeValueComponent>(1);
                s.Initialize();

                var e = s.CreateEntity();
                s.Buffer<SomeValueComponent>().Set(e);
                s.Tick();
                
                var someValueComponent = s.Buffer<SomeValueComponent>().Get(e);
                unsafe
                {
                    s.Buffer<SomeValueComponent>().GetUnsafe(e);
                }
                
                Assert.IsTrue(someValueComponent.IncrementingValue == 1);
                
                s.Destroy(e);
                
                Assert.Throws<EntityNotAssignedException<SomeValueComponent>>(() =>
                {
                    s.Buffer<SomeValueComponent>().Get(e);
                });
                
                Assert.Throws<EntityNotAssignedException<SomeValueComponent>>(() =>
                {
                    unsafe
                    {
                        s.Buffer<SomeValueComponent>().GetUnsafe(e);
                    }
                });

                e = s.CreateEntity();
                s.Buffer<SomeValueComponent>().Set(e);
                
                s.Tick();
                
                someValueComponent = s.Buffer<SomeValueComponent>().Get(e);
                unsafe
                {
                    s.Buffer<SomeValueComponent>().GetUnsafe(e);
                }
                Assert.IsTrue(someValueComponent.IncrementingValue == 1);
            } 
        }

        #region Test Components
        
        private struct Coordinate
        {
            public int StartX;
            public int StartY;
            public int X;
            public int Y;
        }

        private struct CoordinateBufferData
        {
            public int Index;
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
        
        #endregion Test Components
        
        #region Test Systems
        
        private class SomeValueSystem : ISystem
        {
            public void Initialize(Simulation s) { }

            public unsafe void Process(Simulation s)
            {
                var buffer = s.Buffer<SomeValueComponent>();
                var r = s.GetSingletonComponentUnsafe<XorShiftRandom>();
                var i = 0;
                while (buffer.NextUnsafe(ref i, out _, out var someValueComponent))
                {
                    someValueComponent->IncrementingValue++;
                    someValueComponent->RandomValue = r->NextInt32();
                }
            }
        }

        private class SystemEventTestSystem : ISystem, ISystemEventListener<IncrementValueSystemEvent>
        {
            public void Initialize(Simulation s) { }

            public void Process(Simulation s)
            {
                var buffer = s.Buffer<SomeValueComponent>();
                var i = 0;
                while(buffer.Next(ref i, out var entityId, out _))
                    s.SystemEvent(new IncrementValueSystemEvent
                    {
                        EntityId = entityId,
                    });
            }
            
            public unsafe void OnSystemEvent(Simulation s, in IncrementValueSystemEvent data)
            {
                if (!s.Buffer<SomeValueComponent>().GetUnsafe(data.EntityId, out var someValueComponent))
                    return;

                someValueComponent->EventIncrementingValue++;
            }
        }

        private class CoordinateSystem : ISystem
        {
            public unsafe void Initialize(Simulation s)
            {
                const int width = 1000;
                var index = s.GetSingletonComponent<CoordinateBufferData>().Index;
                var coordinates = s.AnonymousBuffer<Coordinate>(index);
                var i = 0;
                while (coordinates.NextUnsafe(ref i, out var coordinate))
                {
                    coordinate->X = i % width;
                    coordinate->Y = i / width;
                    coordinate->StartX = coordinate->X;
                    coordinate->StartY = coordinate->Y;
                }
            }

            public unsafe void Process(Simulation s)
            {
                var index = s.GetSingletonComponent<CoordinateBufferData>().Index;
                var coordinates = s.AnonymousBuffer<Coordinate>(index);
                var i = 0;
                while (coordinates.NextUnsafe(ref i, out var coordinate))
                {
                    coordinate->X++;
                    coordinate->Y++;
                }
            }
        }
        
        #endregion Test Systems

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