using System.Runtime.InteropServices;
using TofuECS.Demo.Common;
using Tofunaut.TofuECS;

namespace TofuECS.Demo.Demos;

// The simplest of demos
public class SimpleDemo : IDemo
{
    public void Run()
    {
        // Create simulation and register system(s)
        var simulation = new Simulation(
            new ConsoleLogger(),
            new ISystem[] {new PrintCoordinateSystem()}
        );

        // Register components
        simulation.RegisterComponent<CoordinateComponent>(Marshal.SizeOf<CoordinateComponent>());

        // Initialize
        simulation.Initialize();

        // Create entity
        var entity = simulation.CreateEntity();

        // Associate a component with an entity
        simulation.Buffer<CoordinateComponent>().Set(entity, new CoordinateComponent(10, 1));

        // Run all systems once
        simulation.Tick();
    }
}

// A system must implement ISystem
internal class PrintCoordinateSystem : ISystem
{
    // Run arbitrary code once on simulation initialization
    public void Initialize(Simulation s)
    {
    }

    // Code that runs on every tick
    public void Process(Simulation s)
    {
        // Get an iterator to iterate over entities
        var iterator = s.Buffer<CoordinateComponent>().GetIterator();
        while (iterator.Next())
        {
            // Actually get the component out of the buffer
            iterator.Get(out var transform);
            s.Log.Info($"Entity {iterator.Entity} with CoordinateComponent (X, Y): {transform.X}, {transform.Y}");
        }
    }
}