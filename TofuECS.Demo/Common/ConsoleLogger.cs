using System;
using Tofunaut.TofuECS;

namespace TofuECS.Demo.Common;

internal class ConsoleLogger : ILogService
{
    public void Debug(string s)
    {
        Console.WriteLine($"[DEBUG]: {s}");
    }

    public void Info(string s)
    {
        Console.WriteLine($"[INFO]: {s}");
    }

    public void Warn(string s)
    {
        var oldColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("[WARN]: ");
        Console.ForegroundColor = oldColor;
        Console.WriteLine(s);
    }

    public void Error(string s)
    {
        var oldColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("[ERROR]: ");
        Console.ForegroundColor = oldColor;
        Console.WriteLine(s);
    }

    public void Exception(Exception e)
    {
        Console.WriteLine($"[EXCEPTION]: {e}");
    }
}