using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace KizhiPart1
{
    public class Interpreter
    {
        public Interpreter(TextWriter writer)
        {
            Writer = writer;
            commandValidator = new InterpreterCommandValidator();
            commandCreator = new InterpreterCommandСreator();
            Variables = new Dictionary<string, int>();
        }

        private readonly IInterpreterCommandValidator commandValidator;
        private readonly IInterpreterCommandСreator commandCreator;
        public TextWriter Writer { get; }
        public IDictionary<string, int> Variables { get; }

        public void ExecuteLine(string strCommand)
        {
            if (!commandValidator.IsValid(strCommand))
                throw new FormatException($"{ExceptionTexts.CommandNotCorrect}: {strCommand}");

            var parameters = strCommand.Split(' ');
            var command = commandCreator.Create(parameters);

            if (!command.TryExecute(this))
                Writer.WriteLine("Переменная отсутствует в памяти");
        }
    }

    public static class CommandNames
    {
        public const string Set = "set";
        public const string Sub = "sub";
        public const string Print = "print";
        public const string Rem = "rem";
    }

    public static class ExceptionTexts
    {
        public const string CommandNotCorrect = "Введенная вами команда некорректна";
        public const string CommandNotFound = "Данной команды не существует";
        public const string ResultCanNotBeNegative = "Результат не может быть отрицательным";
    }

    public static class IntExtensions
    {
        public static bool IsNegative(this int number) => number < 0;
    }

    public interface IInterpreterCommandValidator
    {
        bool IsValid(string command);
    }

    public interface IInterpreterCommandСreator
    {
        IInterpreterCommand Create(string[] parameters);
    }

    public interface IInterpreterCommand
    {
        bool TryExecute(Interpreter interpreter);
    }

    public class InterpreterCommandValidator : IInterpreterCommandValidator
    {
        private readonly Regex regexCommand = new Regex(
            @"\A((set [a-zA-Z]+ \d+)" +
            @"|(sub [a-zA-Z]+ \d+)" +
            @"|(print [a-zA-Z]+)" +
            @"|(rem [a-zA-Z]+))\z");

        public bool IsValid(string command)
        {
            return regexCommand.IsMatch(command);
        }
    }

    public class InterpreterCommandСreator : IInterpreterCommandСreator
    {
        public IInterpreterCommand Create(string[] parameters)
        {
            var commandName = parameters[0];

            switch (commandName)
            {
                case CommandNames.Set:
                    return new SetCommand(parameters[1], parameters[2]);
                case CommandNames.Sub:
                    return new SubCommand(parameters[1], parameters[2]);
                case CommandNames.Print:
                    return new PrintCommand(parameters[1]);
                case CommandNames.Rem:
                    return new RemCommand(parameters[1]);
                default:
                    throw new ArgumentException($"{ExceptionTexts.CommandNotFound}. Имя команды: {commandName}");
            }
        }
    }

    public class SetCommand : IInterpreterCommand
    {
        public SetCommand(string variableName, string variableValue)
        {
            this.variableName = variableName;
            this.variableValue = int.Parse(variableValue);
        }

        private readonly string variableName;
        private readonly int variableValue;

        public bool TryExecute(Interpreter interpreter)
        {
            interpreter.Variables[variableName] = variableValue;
            return true;
        }
    }

    public class SubCommand : IInterpreterCommand
    {
        public SubCommand(string variableName, string variableValue)
        {
            this.variableName = variableName;
            this.variableValue = int.Parse(variableValue);
        }

        private readonly string variableName;
        private readonly int variableValue;

        public bool TryExecute(Interpreter interpreter)
        {
            if (interpreter.Variables.ContainsKey(variableName))
            {
                var result = interpreter.Variables[variableName] - variableValue;

                if (result.IsNegative())
                    throw new ArgumentException($"{ExceptionTexts.ResultCanNotBeNegative}: " +
                        $"{interpreter.Variables[variableName]} - {variableValue} = {result}");

                interpreter.Variables[variableName] = result;
                return true;
            }

            return false;
        }
    }

    public class PrintCommand : IInterpreterCommand
    {
        public PrintCommand(string variableName)
        {
            this.variableName = variableName;
        }

        private readonly string variableName;

        public bool TryExecute(Interpreter interpreter)
        {
            if (interpreter.Variables.ContainsKey(variableName))
            {
                interpreter.Writer.WriteLine(interpreter.Variables[variableName]);
                return true;
            }

            return false;
        }
    }

    public class RemCommand : IInterpreterCommand
    {
        public RemCommand(string variableName)
        {
            this.variableName = variableName;
        }

        private readonly string variableName;

        public bool TryExecute(Interpreter interpreter)
        {
            if (interpreter.Variables.ContainsKey(variableName))
            {
                interpreter.Variables.Remove(variableName);
                return true;
            }

            return false;
        }
    }
}