using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace OrderProcessing.Shared
{
    public static class Log
    {
        private static readonly object _lock = new object();

        public static void Info(string component, string message)
        {
            Print(component, message, null);
        }
        public static void Error(string component,string message)
        {
            Print(component, message, ConsoleColor.Red);
        }
        public static void Success(string component, string message)
        {
            Print(component, message, ConsoleColor.Green);
        }
        public static void Print(string component, string message, ConsoleColor? color)
        {
            lock (_lock)
            {
                var time = DateTime.Now.ToString(" HH:mm:ss.fff");
                var threadId = Environment.CurrentManagedThreadId;

                var previousColor = Console.ForegroundColor;
                try
                {
                    if (color.HasValue)
                    {
                        Console.ForegroundColor = color.Value;
                    }
                    Console.WriteLine($"[{time}] [Thread {threadId}] {component} -> {message}");

                }
                finally
                {
                    Console.ForegroundColor = previousColor;
                }
            }
        }


    }
}
