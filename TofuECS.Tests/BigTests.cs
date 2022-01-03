using System;
using System.Collections.Generic;
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
            var s = new Simulation(new BigTestSimulationConfig(), new Tests.DummyECSDatabase(),
                new Tests.TestLogService(), new ISystem[]
                {
                    new CoordinateSystem(),
                });
            
            s.RegisterComponent<Coordinate>();
            
            s.Initialize();
            
            s.Shutdown();
        }
        

        private struct Coordinate
        {
            public int X;
            public int Y;
        }

        private unsafe class CoordinateSystem : ISystem
        {
            public void Initialize(Frame f)
            {
                const int width = 20;
                const int height = 20;
                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        var e = f.CreateEntity();
                        try
                        {
                            var coordinate = f.GetOrAddComponentUnsafe<Coordinate>(e);
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
                //var coordinateIterator = f.GetIterator<Coordinate>();
            }

            public void Dispose(Frame f) { }
        }
        
        public class BigTestSimulationConfig : ISimulationConfig
        {
            public int FramesInMemory => 2;
            public int NumInputs => 1;
            public ulong Seed => 987654321;
        }

    }
}