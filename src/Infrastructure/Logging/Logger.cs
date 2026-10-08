using System;
using Domain.Services;

namespace Infrastructure.Logging;

public class Logger : IAppLogger
{
    public void Log(string message)
    {
        Console.WriteLine($"[LOG] {DateTime.Now} - {message}");
    }
}
