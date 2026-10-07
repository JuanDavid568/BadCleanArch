using System;

namespace Infrastructure.Logging;

public static class Logger
{
    public static bool Enabled = true;

    public static void Log(string message)
    {
        if (!Enabled) return;
        Console.WriteLine("[LOG] " + DateTime.Now + " - " + message);
    }

    public static void Try(Action a)
    {
        try { a(); } catch { }
    }
}
