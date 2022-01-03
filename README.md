![tofuecs_logo](https://user-images.githubusercontent.com/8916588/139094266-3e2db942-4842-4f0d-b1da-8e694ee3578c.png)

This is an entity component system (ECS) framework written in C# that can be easily added to a Unity project as a managed plugin (dll) — although I suppose there's no reason you couldn't use it for some other non-Unity purpose. Licensed under MIT.

This repo contains a solution with three projects: TofuECS, TofuECS.Utilities, and TofuECS.Tests. TofuECS is the main dll you will want to include in your project, while TofuECS.Utilities contains some other useful classes that aren't strictly necessary. TofuECS.Tests should not be included, but showcases how to start and run a simulation and how you can use this framework.

ECS frameworks are fun to code in and offer performance benefits against the typical GameObject/MonoBehaviour Unity workflow, all while presenting a clear separation of logic from views (for example: your GameObjects, Meshes, Sprites, etc.). I have a lot of experience working with other frameworks, and I wanted to write my own while attempting to match their performance and ease of use.

## Components
Components contain all data about the state of the simulation. For TofuECS, all components must be unmanaged (see [MS docs for the Unmanaged type constraint](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-7.3/blittable)), i.e, structs with only fields of types `int`, `bool`, etc. The type `UnmanagedArray<T>` is included in `TofuECS.Utilities` to help out with arrayed data. This requires some creativity on the part of the developer to inject or read event data in Unity, as it must be converted to some unmanaged struct.

## Entities
Entities are pointers to components. References to entities are passed around and store on components as integers (the actual `Entity` class is internal and not accessible to the user). Entities can point to multiple components and can be created and destroyed over the lifetime of the simulation.

## Events
There are two type of events: *external* and *system* events. Both must be unmanaged types, just like components. External events can be raised via `Frame.RaiseExternalEvent<TEvent>()` and will be invoked **after** the frame is processed. System events can be raised via `Frame.RaiseSystemEvent<TEvent>()`, are used to communicate between instances of `ISystem`, and will be invoked immediately, like any function call. Instances of `ISystem` must implement `ISystemEventListener<TEvent>` in order to respond to the system event.

## Frames
Frames allow the user to access data about the state of the ECS. With an instance of `Frame` you can access the components of an entity and perform logic on that component. You can also raise *external* and *system* events so data can be processed somewhere else in your code base. The current frame is passed as a parameter during the initialization, processing, and disposing of systems. There is a fixed number of frames as specified by the `ISimulationConfig` instance during the construction of the simulation.

## Simulation
The simulation contains all frames and systems. It must be initialized before calling `Tick()` to ensure all systems have been initialized. To visualize the simulation (instantiating a prefab in Unity when an entity is created, for example), the user can subscribe to external events via a reference to the simulation.

## Systems
Systems are ***stateless*** classes that implement the `ISystem` interface. Systems are registered at the creation of the simulation and are processed once each time the simulation "ticks" (i.e., when `Simulation.Tick()` is called). This is where all logic for your simulation will occur. Typically, you'll want to get an `EntityComponentIterator<T>` from the current `Frame` and do your logic on each entity-component pair.

Systems are initialized at the start of the simulation (when `Simulation.Initialize()` is called), are processed each tick, and disposed at the end of the simulation (when Simulation.Shutdown() is called).

It is extremely important to remember the term ***stateless***. While there's nothing stopping you from adding fields (state) to an implementation of `ISystem`, doing so will likely lead to inconsistent results when re-simulating frames during a rollback or replay. Remember to store all data in components.



*TofuECS is in development! Vegan friendly.*
