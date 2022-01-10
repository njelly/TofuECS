using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tofunaut.TofuECS;
using Tofunaut.TofuECS.Utilities;

namespace TofuECS.Tests
{
    [TestFixture]
    public class Tests
    {
        /*
        [Test]
        public void RollbackTests()
        {
            var s = new Simulation(new RollbackTestSimulationConfig(), new DummyECSDatabase(),
                new TestLogService(), new ISystem[]
                {
                    new SomeValueSystem(),
                });
            
            s.RegisterComponent<SomeValueComponent>();
            s.Initialize();

            var entityA = s.CurrentFrame.CreateEntity();
            s.CurrentFrame.AddComponent<SomeValueComponent>(entityA);

            var randomAt30 = 0;
            var randomAt15 = 0;
            for (var i = 0; i < 30; i++)
            {
                s.Tick();

                switch (s.CurrentFrame.Number)
                {
                    case 30:
                        randomAt30 = s.CurrentFrame.GetComponent<SomeValueComponent>(entityA).RandomValue;
                        break;
                    case 15:
                        randomAt15 = s.CurrentFrame.GetComponent<SomeValueComponent>(entityA).RandomValue;
                        break;
                }Ωz
            }
            
            Assert.IsTrue(s.CurrentFrame.GetComponent<SomeValueComponent>(entityA).IncrementingValue == 30);
            s.RollbackTo(15);
            Assert.IsTrue(s.CurrentFrame.GetComponent<SomeValueComponent>(entityA).IncrementingValue == 15);
            s.RollbackTo(2);
            Assert.IsTrue(s.CurrentFrame.GetComponent<SomeValueComponent>(entityA).IncrementingValue == 2);
            
            // The simulation can "rollback" into the future if the frames have already been processed, although I'm not
            // sure why this would be desirable...
            s.RollbackTo(29);
            Assert.IsTrue(s.CurrentFrame.GetComponent<SomeValueComponent>(entityA).IncrementingValue == 29);
            // ...although frame 30 has not been processed, so we cannot "rollback" to it or any frames beyond it.
            Assert.Catch<InvalidRollbackException>(() =>
            {
                s.RollbackTo(30);
            });
            
            // frame 1 is too far back to rollback to, now that we've processed 29 frames
            // ISimulationConfig.FramesInMemory - 2 is the max number of frames we can rollback to from the highest processed frame
            Assert.Catch<InvalidRollbackException>(() =>
            {
                s.RollbackTo(1);
            });
            // frame -1 is obviously invalid
            Assert.Catch<InvalidRollbackException>(() =>
            {
                s.RollbackTo(-1);
            });
            
            // make sure RNG is the same when the sim is played back
            for (var i = 0; i < 30; i++)
            {
                s.Tick();

                switch (s.CurrentFrame.Number)
                {
                    case 30:
                        Assert.IsTrue(s.CurrentFrame.GetComponent<SomeValueComponent>(entityA).RandomValue == randomAt30);
                        break;
                    case 15:
                        Assert.IsTrue(s.CurrentFrame.GetComponent<SomeValueComponent>(entityA).RandomValue == randomAt15);
                        break;
                }
            }
            
            s.Shutdown();
        }
        */

        [Test]
        public void AddRemoveComponentTest()
        {
            var ecs = new Simulation(new ECSDatabase(), new TestLogService(), 1234, new ISystem[]
            {
                new AddRemoveComponentExternalEventSystem(),
            });
            
            ecs.RegisterComponent<SomeValueComponent>(16);

            var testValue = 0;
            void onTestExternalEvent(TestExternalEvent data)
            {
                testValue++;
            }

            ecs.Subscribe<TestExternalEvent>(onTestExternalEvent);

            ecs.Initialize();

            var e = ecs.CreateEntity();
            
            ecs.Buffer<SomeValueComponent>().Set(e);
            
            ecs.Tick();
            
            Assert.True(testValue == 1);
            
            Assert.True(ecs.Buffer<SomeValueComponent>().Remove(e));
            
            ecs.Tick();
            
            Assert.True(testValue == 1);
            
            ecs.Buffer<SomeValueComponent>().Set(e);
            
            ecs.Tick();
            
            Assert.True(testValue == 2);
        }

        [Test]
        public void SystemEventTests()
        {
            var ecs = new Simulation(new ECSDatabase(), new TestLogService(), 1234, new ISystem[]
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
        public void ExternalEventTests()
        {
            var ecs = new Simulation(new ECSDatabase(), new TestLogService(), 1234, new ISystem[]
            {
                new ExternalEventTestSystem(),
            });
            
            ecs.Initialize();

            var testValue = 0;
            void onTestExternalEvent(TestExternalEvent data)
            {
                testValue++;
            }
            
            ecs.Subscribe<TestExternalEvent>(onTestExternalEvent);

            const int numTicks = 10;
            
            for (var i = 0; i < numTicks; i++)
                ecs.Tick();
            
            ecs.Unsubscribe<TestExternalEvent>(onTestExternalEvent);
            
            for (var i = 0; i < numTicks; i++)
                ecs.Tick();
            
            Assert.IsTrue(testValue == numTicks);
        }

        [Test]
        public void ArrayQuickSortTests()
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

            ArrayQuickSort.Sort(arr, (a, b) => a.CompareTo(b));

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
                    someValueIterator.ModifyCurrent((ref SomeValueComponent component) =>
                    {
                        component.IncrementingValue++;
                        component.RandomValue = s.RNG.NextInt32();
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
                    s.RaiseSystemEvent(new IncrementValueSystemEvent
                    {
                        EntityId = someValueIterator.CurrentEntity,
                    });
                }
            }
            
            public void OnSystemEvent(Simulation simulation, IncrementValueSystemEvent data)
            {
                simulation.Buffer<SomeValueComponent>().GetAndModify(data.EntityId,
                    (ref SomeValueComponent someValueComponent) => { someValueComponent.EventIncrementingValue++; });
            }
        }

        private class ExternalEventTestSystem : ISystem
        {
            public void Initialize(Simulation s) { }

            public void Process(Simulation s)
            {
                s.QueueExternalEvent(new TestExternalEvent());
            }
        }

        private class AddRemoveComponentExternalEventSystem : ISystem
        {
            public void Initialize(Simulation s) { }

            public void Process(Simulation s)
            {
                var iterator = s.Buffer<SomeValueComponent>().GetIterator();
                while(iterator.Next())
                    s.QueueExternalEvent(new TestExternalEvent());
            }
        }

        public class TestLogService : ILogService
        {
            public void Info(string s)
            {
                Console.WriteLine($"[INFO] {s}");
            }

            public void Warn(string s)
            {
                Console.WriteLine($"[WARN] {s}");
            }

            public void Error(string s)
            {
                Console.WriteLine($"[ERROR] {s}");
            }

            public void Exception(Exception e)
            {
                Console.WriteLine($"[EXCEPTION] {e.Message}");
            }
        }

        private struct IncrementValueSystemEvent
        {
            public int EntityId;
        }
        
        private struct TestExternalEvent { }
    }
}