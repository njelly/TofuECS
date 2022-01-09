﻿using System;
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
            var s = new ECS(new ECSDatabase(), new Tests.TestLogService(), 1234, new ISystem[]
            {
                new CoordinateSystem(),
            });
            
            s.RegisterComponent<Coordinate>(numCoordinates);
            
            s.Initialize();

            const int numTicks = 10;
            for (var i = 0; i < numTicks; i++)
                s.Tick();

            var coordinateIterator = s.Buffer<Coordinate>().GetIterator();
            while (coordinateIterator.Next())
            {
                Assert.IsTrue(coordinateIterator.Current.X == coordinateIterator.Current.StartX + numTicks);
                Assert.IsTrue(coordinateIterator.Current.Y == coordinateIterator.Current.StartY + numTicks);
            }
        }

        private struct Coordinate
        {
            public int StartX;
            public int StartY;
            public int X;
            public int Y;
        }

        private class CoordinateSystem : ISystem
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
                        var coordinateBuffer = ecs.Buffer<Coordinate>();
                        coordinateBuffer.Set(e, new Coordinate
                        {
                            StartX = x,
                            StartY = y,
                            X = x,
                            Y = y,
                        });
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
                var coordinateIterator = ecs.Buffer<Coordinate>().GetIterator();
                while (coordinateIterator.Next())
                {
                    coordinateIterator.ModifyCurrent((ref Coordinate coordinate) =>
                    {
                        coordinate.X++;
                        coordinate.Y++;
                    });
                }
            }
        }

    }
}