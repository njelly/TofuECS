using System;
using NUnit.Framework;
using Tofunaut.TofuECS;

namespace TofuECS.Tests
{
    [TestFixture]
    public class BigTests
    {
        [Test]
        public void BigTest()
        {
            // This test simply creates a large number of entities and components, modifies them with a system, and confirms
            // the results.
            var s = new Simulation(new BigTestSimulationConfig(), new Tests.DummyECSDatabase(),
                new Tests.TestLogService(), new ISystem[]
                {
                    new CoordinateSystem(),
                });
            
            s.RegisterComponent<Coordinate>();
            
            s.Initialize();

            const int numTicks = 100;
            for (var i = 0; i < numTicks; i++)
                s.Tick();

            var coordinateIterator = s.CurrentFrame.GetIterator<Coordinate>();
            while (coordinateIterator.Next(out _, out var coordinate))
            {
                Assert.IsTrue(coordinate.X == coordinate.StartX + numTicks);
                Assert.IsTrue(coordinate.Y == coordinate.StartY + numTicks);
            }
            
            s.Shutdown();
        }

        private struct Coordinate
        {
            public int StartX;
            public int StartY;
            public int X;
            public int Y;
        }

        private unsafe class CoordinateSystem : ISystem
        {
            public void Initialize(Frame f)
            {
                // one million entities will be created
                const int width = 1000;
                const int height = 1000;
                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        var e = f.CreateEntity();
                        try
                        {
                            var coordinate = f.GetOrAddComponentUnsafe<Coordinate>(e);
                            coordinate->StartX = x;
                            coordinate->StartY = y;
                            coordinate->X = x;
                            coordinate->Y = y;
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine(exception);
                            throw;
                        }
                    }
                }
            }

            public void Process(Frame f)
            {
                var coordinateIterator = f.GetIterator<Coordinate>();
                while (coordinateIterator.NextUnsafe(out _, out var coordinate))
                {
                    coordinate->X++;
                    coordinate->Y++;
                }
            }

            public void Dispose(Frame f) { }
        }
        
        private class BigTestSimulationConfig : ISimulationConfig
        {
            public int FramesInMemory => 2;
            public int NumInputs => 1;
            public ulong Seed => 987654321;
        }

    }
}