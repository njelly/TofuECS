using FixMath.NET;
using NUnit.Framework;
using Tofunaut.TofuECS.Interfaces;

namespace Tofunaut.TofuECS.Tests
{
    public static unsafe class BasicTests
    {
        [Test]
        public static void WorldCreation()
        {
            var config = new WorldConfig
            {
                DeltaTime = Fix64.One / (Fix64) 60,
                MaxComponents = 128,
            };

            var systems = new System[]
            {
                new CounterSystem(),
            };

            var world = new World(config, systems);
            world.RegisterComponent<Counter>();
            world.RegisterComponent<DummyComponent>();
            
            var counterEntity = world.Predicted.Create();
            world.Predicted.AddComponent<Counter>(counterEntity);
            Assert.IsTrue(world.Predicted.TryGetComponent(counterEntity, out Counter* counter));
            Assert.IsFalse(world.Predicted.TryGetComponent(counterEntity, out DummyComponent* _));
            
            counter->SomeNum += 1;
            
            world.Predicted.TryGetComponent(counterEntity, out Counter* theSameCounter);
            Assert.IsTrue(theSameCounter->SomeNum == counter->SomeNum);
            
            Assert.IsTrue(world.Predicted.number == 0);
            world.Update();
            Assert.IsTrue(world.Predicted.number == 1);

            var counterFilter = world.Predicted.Filter<Counter>();
            Assert.IsTrue(counterFilter.Count == 1);
        }

        private struct Counter : IComponent
        {
            public int SomeNum;
        }

        private struct DummyComponent : IComponent { }

        private class CounterSystem : System
        {
            public override void Update(Frame f)
            {
                var filter = f.Filter<Counter>();
                while(filter.Next(out var e, out var counter))
                    UpdateCounter(f, e, counter);
            }

            private static void UpdateCounter(Frame f, int e, Counter* counter)
            {
                counter->SomeNum = counter->SomeNum + 1;
            }
        }
    }
}