using System;
using NUnit.Framework;
using Tofunaut.TofuECS;

namespace TofuECS.Tests
{
    [TestFixture]
    public class BigTests
    { 
        private const int numCoordinates = 1000000;
        
        [Test]
        public void BigTest()
        {
            // This test simply creates a large number of entities and components, modifies them with a system, and confirms
            // the results.
            var s = new ECS(new Tests.DummyECSDatabase(), new Tests.TestLogService(), 1234, new ISystem[]
            {
                new CoordinateSystem(),
            });
            
            s.RegisterComponent<Coordinate>(numCoordinates);
            
            s.Initialize();

            const int numTicks = 10;
            for (var i = 0; i < numTicks; i++)
                s.Tick();

            var coordinateIterator = s.GetIterator<Coordinate>();
            while (coordinateIterator.Next(out _, out var coordinate))
            {
                Assert.IsTrue(coordinate.X == coordinate.StartX + numTicks);
                Assert.IsTrue(coordinate.Y == coordinate.StartY + numTicks);
            }
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
            public void Initialize(ECS ecs)
            {
                const int width = 1000;
                for (var i = 0; i < numCoordinates; i++)
                {
                    var x = i % width; 
                    var y = i / width;
                    try
                    {
                        var e = ecs.CreateEntity();
                        ecs.AssignComponent<Coordinate>(e);
                        var coordinate = ecs.GetUnsafe<Coordinate>(e);
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

            public void Process(ECS ecs)
            {
                var coordinateIterator = ecs.GetIterator<Coordinate>();
                while (coordinateIterator.NextUnsafe(out _, out var coordinate))
                {
                    coordinate->X++;
                    coordinate->Y++;
                }
            }
        }

    }
}