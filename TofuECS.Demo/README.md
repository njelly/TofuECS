# Demos

Currently there are 2 demos

1. The simplest "Hello World" showcasing how to register systems and components
2. A demo showcasing how to get multiple components from an entity inside a system

To a run a demo either adjust the `commandLineArgs` property in the `launchSettings.json` file or run it from the terminal with one of the following arguments:

1. The simple demo is run when the command line argument equals `simple`
2. The demo showcasing multiple components is run when the command line argument equals `multi`

If no command line argument is passed, no demo is ran. If a command line argument is passed that's unknown (i.e not `simple` or `multi`) then the simple demo is ran