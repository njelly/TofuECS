![tofuecs_logo](https://user-images.githubusercontent.com/8916588/139094266-3e2db942-4842-4f0d-b1da-8e694ee3578c.png)

This is entity component system (ECS) framework written in C# that can be easily added to a Unity project as a managed plugin (dll) -- although I suppose there's no reason you couldn't use it for some other non-Unity purpose. Licensed under MIT.

This repo contains a solution with three projects: TofuECS, TofuECS.Utilities, and TofuECS.Tests. TofuECS is the main dll you will want to include in your project, while TofuECS.Utilities contains some other useful classes that aren't strictly necessary. TofuECS.Tests should not be included, but showcases how to start and run a simulation and how you can use this library.

ECS frameworks are fun to code in and offer performance benefits against the typical GameObject/MonoBehaviour Unity workflow, all while presenting a clear separation of logic from views (i.e., your GameObjects, Meshes, Sprites, etc.). I have a lot of experience working with other frameworks, and I wanted to write my own while attempting to match their performance and ease of use.

In development. Vegan friendly.
