using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tofunaut.TofuECS;

namespace TofuECS.Tests
{
    [TestFixture]
    public class BigTests
    { 
        private const int numCoordinates = 1000000;
        private const int numBigStructs = 1;
        
        [Test]
        public void LotsOfEntitiesAndComponentsTest()
        {
            // This test simply creates a large number of entities and components, modifies them with a system, and confirms
            // the results.
            var s = new Simulation(new ECSDatabase(), new Tests.TestLogService(), 1234, new ISystem[]
            {
                new CoordinateSystem(),
            });

            s.RegisterComponent<Coordinate>(numCoordinates);
            
            s.Initialize();

            const int numTicks = 10;
            while(s.CurrentTick < numTicks)
                s.Tick();

            var coordinateIterator = s.Buffer<Coordinate>().GetIterator();
            while (coordinateIterator.Next())
            {
                Assert.IsTrue(coordinateIterator.Current.X == coordinateIterator.Current.StartX + numTicks);
                Assert.IsTrue(coordinateIterator.Current.Y == coordinateIterator.Current.StartY + numTicks);
            }
        }

        [Test]
        public unsafe void VeryLargeAndVeryManyComponentsTest()
        {
            var s = new Simulation(new ECSDatabase(), new Tests.TestLogService(), 1234, new ISystem[]
            {
                new ManyBigStructsSystem(),
            });
            
            s.RegisterComponent<BigStruct>(numCoordinates);
            s.Initialize();
            s.Tick();

            var iterator = s.Buffer<BigStruct>().GetIterator();
            while (iterator.Next()) 
            {
                iterator.ModifyCurrentUnsafe(component =>
                {
                    Assert.True(component->SomeState[0]);
                    //Assert.True(component.SomeState[BigStruct.MaxArraySize - 1]);
                });
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
            public void Initialize(Simulation s)
            {
                const int width = 1000;
                for (var i = 0; i < numCoordinates; i++)
                {
                    var x = i % width; 
                    var y = i / width;
                    try
                    {
                        var e = s.CreateEntity();
                        var coordinateBuffer = s.Buffer<Coordinate>();
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

            public void Process(Simulation s)
            {
                var coordinateIterator = s.Buffer<Coordinate>().GetIterator();
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

        private class ManyBigStructsSystem : ISystem
        {
            public void Initialize(Simulation s)
            {
                var buffer = s.Buffer<BigStruct>();
                for (var i = 0; i < numBigStructs; i++)
                {
                    var e = s.CreateEntity();
                    buffer.Set(e);
                }
            }

            public unsafe void Process(Simulation s)
            {
                var iterator = s.Buffer<BigStruct>().GetIterator();
                while (iterator.Next())
                {
                    iterator.ModifyCurrentUnsafe(bigStruct =>
                    {
                        bigStruct->SomeState[0] = true;
                        //bigStruct->SomeState[BigStruct.MaxArraySize - 1] = true;
                    });
                }
            }
        }

        public unsafe struct BigStruct
        {
            public const int MaxArraySize = 1028040; // literally the largest possible bool array size
            public fixed bool SomeState[MaxArraySize];
        }
    }
}