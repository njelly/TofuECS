using FixMath.NET;
using NUnit.Framework;
using Tofunaut.TofuECS.Interfaces;

namespace Tofunaut.TofuECS.Tests
{
    public static class BasicTests
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

            for (var i = 0; i < config.MaxComponents; i++)
            {
                var counterEntity = world.Predicted.Create();
                world.Predicted.AddComponent<Counter>(counterEntity);
                Assert.IsTrue(world.Predicted.TryGetComponent(counterEntity, out Counter _));
                Assert.IsFalse(world.Predicted.TryGetComponent(counterEntity, out DummyComponent _));
            }
            
            Assert.IsTrue(world.Predicted.number == 0);
            world.Update();
            Assert.IsTrue(world.Predicted.number == 1);
            
            var counterFilter = world.Predicted.Filter<Counter>();
            Assert.IsTrue(counterFilter.Count == config.MaxComponents);
            while(counterFilter.Next(out var e, out var counter))
                Assert.IsTrue(counter.SomeNum == 1);
        }

        private class Counter : IComponent
        {
            public int SomeNum;
        }

        private class DummyComponent : IComponent { }

        private class CounterSystem : System
        {
            public override void Update(Frame f)
            {
                var filter = f.Filter<Counter>();
                while(filter.Next(out var e, out var counter))
                    UpdateCounter(f, e, counter);
            }

            private static void UpdateCounter(Frame f, int e, Counter counter)
            {
                counter.SomeNum++;
            }
        }
    }
}