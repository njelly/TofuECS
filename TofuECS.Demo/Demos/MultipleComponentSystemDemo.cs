using System;
using System.Numerics;
using System.Runtime.InteropServices;
using TofuECS.Demo.Common;
using Tofunaut.TofuECS;

namespace TofuECS.Demo.Demos;

public class MultipleComponentSystemDemo : IDemo
{
    public void Run()
    {
        // Create simulation and register system(s)
        var simulation = new Simulation(
            new ConsoleLogger(),
            new ISystem[] {new PrintCoordinateAndSpeedSystem()}
        );

        // Register components
        simulation.RegisterComponent<CoordinateComponent>(Marshal.SizeOf<CoordinateComponent>());
        simulation.RegisterComponent<SpeedComponent>(Marshal.SizeOf<SpeedComponent>());

        // Initialize
        simulation.Initialize();

        // Create entity
        var entity = simulation.CreateEntity();

        // Associate a component with an entity
        simulation.Buffer<CoordinateComponent>().Set(entity, new CoordinateComponent(10, 1));
        simulation.Buffer<SpeedComponent>().Set(entity, new SpeedComponent(3, Vector2.UnitX));

        // Run all systems once
        simulation.Tick();
    }
}

internal class PrintCoordinateAndSpeedSystem : ISystem
{
    public void Initialize(Simulation s) { }

    public void Process(Simulation s)
    {
        var iterator = s.Buffer<CoordinateComponent>().GetIterator();
        var speedBuffer = s.Buffer<SpeedComponent>();

        while (iterator.Next())
        {
            iterator.Get(out var coords);
            speedBuffer.Get(iterator.Entity, out var speed);
            
            s.Log.Info($"Entity {iterator.Entity}:" +
                        $"{Environment.NewLine}\t- ({coords.X}, {coords.Y})" +
                        $"{Environment.NewLine}\t- ({speed.Velocity}, {speed.Direction})"
            );
        }
    }
}