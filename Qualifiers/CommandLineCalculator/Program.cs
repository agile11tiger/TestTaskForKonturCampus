using CommandLineCalculator.Tests;
using System;

namespace CommandLineCalculator
{
    public static class Program
    {
        public static void Main()
        {
            var console = new TextUserConsole(Console.In, Console.Out);
            var storage = new FileStorage("db");
            var interpreter = new StatefulInterpreter();
            //var storage = new MemoryStorage();
            //var interpreter = new StatelessInterpreter();
            interpreter.Run(console, storage);
        }
    }
}