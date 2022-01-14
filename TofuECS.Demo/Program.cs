using TofuECS.Demo.Demos;

namespace TofuECS.Demo;

internal class Program
{
    public static void Main(string[] args)
    {
        if (args.Length != 0)
        {
            switch (args[0])
            {
                case "simple":
                    new SimpleDemo().Run();
                    break;
                case "multi":
                    new MultipleComponentSystemDemo().Run();
                    break;
                default:
                    new SimpleDemo().Run();
                    break;
            }
        }
    }
}