using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace KizhiPart2
{
    /* 
        1) Есть возможность добавления пустых вложенных функций:
        var code = "" +
            "def cat\n" +
            "    def{def dog}\n" +
            "    call dog\n" +
            "call cat"
            Этот код отработает без ошибок.
          ------------------------------------------------------------------
        2) А также возможность добавления n-ого количества вложенных функций:
        var code = "" +
            "set cat 5\n" +
            "set dog 10\n" +
            "set wolf 15\n" +
            "set tiger 21\n" +
            "def cat\n" +
            "    print cat\n" +
            "    def{def dog\n" +
            "    print dog\n" +
            "    def{def wolf\n" +
            "    print wolf\n" +
            "    def{def tiger\n" +
            "    print tiger}}\n" +
            "    call wolf\n" +
            "    print dog}\n" +
            "    call dog\n" +
            "    print cat\n" +
            "call cat";
            Этот код напечатает: "5 10 15 10 5". (\r\n заменены пробелом)
    */

    public class Interpreter
    {
        public Interpreter(TextWriter writer)
        {
            Writer = writer;
            CommandValidator = new InterpreterCommandValidator();
            CommandHandler = new InterpreterCommandHandler(this);
            CommandCreator = new InterpreterCommandСreator(this);
            Variables = new Dictionary<string, int>();
            DefCommands = new Dictionary<string, DefCommand>();
            Commands = new LinkedList<IInterpreterCommand>();
            TempCommands = new LinkedList<IInterpreterCommand>();
        }

        public TextWriter Writer { get; }
        public IInterpreterCommandValidator CommandValidator { get; }
        public IInterpreterCommandHandler CommandHandler { get; }
        public IInterpreterCommandСreator CommandCreator { get; }
        public IDictionary<string, int> Variables { get; }
        public IDictionary<string, DefCommand> DefCommands { get; }
        public LinkedList<IInterpreterCommand> Commands { get; }
        public LinkedList<IInterpreterCommand> TempCommands { get; }

        public void ExecuteLine(string code)
        {
            if (code.StartsWith(InterpreterCommandNames.SetCode)
             || code.StartsWith(InterpreterCommandNames.EndSetCode)
             || string.IsNullOrWhiteSpace(code))
                return;

            foreach (var strCommand in GetStrCommands(code))
            {
                if (CommandValidator.IsValidDef(strCommand))
                    CommandHandler.ProcessDef(strCommand);
                else
                    CommandHandler.Process(strCommand);
            }
        }

        public string[] GetStrCommands(string code)
        {
            return code.Replace("\n    ", "    ").Split('\n');
        }

        public bool TryValidateCommand(string command)
        {
            if (!CommandValidator.IsValid(command))
                throw new FormatException($"{ExceptionTexts.CommandNotCorrect}: {command}");

            return true;
        }

        public void GetCommandParameters(string strCommand, out string[] parameters)
        {
            parameters = strCommand.Split(' ');
        }

        public void ClearMemory()
        {
            Variables.Clear();
        }
    }

    public static class InterpreterCommandNames
    {
        public const string SetCode = "set code";
        public const string Set = "set";
        public const string Sub = "sub";
        public const string Print = "print";
        public const string Rem = "rem";
        public const string Def = "def";
        public const string Call = "call";
        public const string EndSetCode = "end set code";
        public const string Run = "run";
    }

    public static class ExceptionTexts
    {
        public const string CommandNotCorrect = "Введенная вами команда некорректна";
        public const string CommandNotFound = "Данной команды не существует";
        public const string VariableNotFound = "Переменная отсутствует в памяти";
        public const string FuncNotFound = "Функция отсутствует в памяти";
        public const string ResultCanNotBeNegative = "Результат не может быть отрицательным";
    }

    public static class IntExtensions
    {
        public static bool IsNegative(this int number) => number < 0;
    }

    public static class LinkedListExtensions
    {
        /// <summary>
        /// Удаляет и возвращает узел, находящийся в начале <see cref="LinkedList{T}"/>.
        /// </summary>
        public static LinkedListNode<T> Dequeue<T>(this LinkedList<T> linkedList)
        {
            var firstNode = linkedList.First;
            linkedList.RemoveFirst();
            return firstNode;
        }

        /// <summary>
        /// Добавляет все элементы в начало <see cref="LinkedList{T}"/>.
        /// </summary>
        public static void AddFirst<T>(this LinkedList<T> linkedList, IEnumerable<T> collection)
        {
            foreach (var item in collection)
            {
                linkedList.AddFirst(item);
            }
        }

        /// <summary>
        /// Добавляет все элементы в конец <see cref="LinkedList{T}"/>.
        /// </summary>
        public static void AddLast<T>(this LinkedList<T> linkedList, IEnumerable<T> collection)
        {
            foreach (var item in collection)
            {
                linkedList.AddLast(item);
            }
        }
    }

    public static class StringExtensions
    {
        /// <summary>
        /// Удаляет все конечные вхождения строки из текущего объекта <see cref="string"/>
        /// </summary>
        public static string TrimEnd(this string str, string value, ref int amount)
        {
            if (str.EndsWith(value))
            {
                amount = 1 + str.LastIndexOf(value) - str.IndexOf(value);
                str = str.Remove(str.Length - amount, amount);
            }

            return str;
        }
    }

    public interface IInterpreterCommandValidator
    {
        bool IsValid(string command);
        bool IsValidDef(string command);
        bool IsValidDefWithBrackets(string command);
    }

    public interface IInterpreterCommandHandler
    {
        void Process(string strCommand);
        void ProcessDef(string strDefCommand);
    }

    public interface IInterpreterCommandСreator
    {
        IInterpreterCommand Create(string[] parameters);
        DefCommand CreateDef(Queue<string> funcBody);
    }

    public interface IInterpreterCommand
    {
        void Execute(Interpreter interpreter);
    }

    public class InterpreterCommandValidator : IInterpreterCommandValidator
    {
        private readonly Regex regexCommand = new Regex(
            @"\A(" +
            @"(set [a-zA-Z]+ \d+)" +
            @"|(sub [a-zA-Z]+ \d+)" +
            @"|(print [a-zA-Z]+)" +
            @"|(rem [a-zA-Z]+)" +
            @"|(set code)" +
            @"|(end set code)" +
            @"|(call [a-zA-Z]+)" +
            @"|(run)" +
            @")\z");
        private readonly Regex regexDefCommand = new Regex(@"\A(def [a-zA-z]+(\s(\w|\s|{|})*)?)\z");
        private readonly Regex regexDefCommandWithBrackets = new Regex(@"\A(def{def [a-zA-z]+)\z");

        public bool IsValid(string command)
        {
            return regexCommand.IsMatch(command);
        }

        public bool IsValidDef(string command)
        {
            return regexDefCommand.IsMatch(command);
        }

        public bool IsValidDefWithBrackets(string command)
        {
            return regexDefCommandWithBrackets.IsMatch(command);
        }
    }

    public class InterpreterCommandHandler : IInterpreterCommandHandler
    {
        public InterpreterCommandHandler(Interpreter interpreter)
        {
            this.interpreter = interpreter;
        }

        private readonly Interpreter interpreter;

        public void Process(string strCommand)
        {
            interpreter.TryValidateCommand(strCommand);
            interpreter.GetCommandParameters(strCommand, out string[] parameters);
            var command = interpreter.CommandCreator.Create(parameters);

            if (TryRun(command))
                return;
            
            interpreter.Commands.AddLast(command);
        }

        public void ProcessDef(string strDefCommand)
        {
            GetFuncBody(strDefCommand, out Queue<string> funcBody);
            interpreter.CommandCreator.CreateDef(funcBody);
        }

        private bool TryRun(IInterpreterCommand command)
        {
            if (command is RunCommand)
            {
                interpreter.TempCommands.Clear();
                interpreter.TempCommands.AddLast(interpreter.Commands);
                command.Execute(interpreter);
                return true;
            }

            return false;
        }

        private void GetFuncBody(string strCommand, out Queue<string> funcBody)
        {
            var separators = new string[] { "    " };
            var strFuncBody = strCommand.Split(separators, StringSplitOptions.None);
            funcBody = new Queue<string>(strFuncBody);
        }
    }

    public class InterpreterCommandСreator : IInterpreterCommandСreator
    {
        public InterpreterCommandСreator(Interpreter interpreter)
        {
            this.interpreter = interpreter;
        }

        private readonly Interpreter interpreter;
        private int closingBracketsAmount;

        public IInterpreterCommand Create(string[] parameters)
        {
            var commandName = parameters[0];

            switch (commandName)
            {
                case InterpreterCommandNames.Set:
                    return new SetCommand(parameters[1], parameters[2]);
                case InterpreterCommandNames.Sub:
                    return new SubCommand(parameters[1], parameters[2]);
                case InterpreterCommandNames.Print:
                    return new PrintCommand(parameters[1]);
                case InterpreterCommandNames.Rem:
                    return new RemCommand(parameters[1]);
                case InterpreterCommandNames.Call:
                    return new CallCommand(parameters[1]);
                case InterpreterCommandNames.Run:
                    return new RunCommand();
                default:
                    throw new ArgumentException($"{ExceptionTexts.CommandNotFound}. Имя команды: {commandName}");
            }
        }

        public DefCommand CreateDef(Queue<string> funcBody)
        {
            var defCommand = CreateDefCommand(funcBody);
            closingBracketsAmount = 0;
            return defCommand;
        }

        private DefCommand CreateDefCommand(Queue<string> funcBody)
        {
            var funcName = funcBody.Dequeue().Split(' ')[1].TrimEnd('}');
            var defCommand = new DefCommand(funcName);

            while (funcBody.Count > 0)
            {
                if (closingBracketsAmount > 0)
                {
                    closingBracketsAmount--;
                    break;
                }

                var command = ParseNextCommand(funcBody);
                defCommand.FuncBody.Add(command);
            }

            interpreter.DefCommands[defCommand.FuncName] = defCommand;
            return defCommand;
        }

        private IInterpreterCommand ParseNextCommand(Queue<string> funcBody)
        {
            var strCommand = funcBody.Peek().TrimEnd("}", ref closingBracketsAmount);
            IInterpreterCommand command;

            if (interpreter.CommandValidator.IsValidDefWithBrackets(strCommand))
                command = CreateDefCommand(funcBody);
            else
            {
                interpreter.TryValidateCommand(strCommand);
                interpreter.GetCommandParameters(strCommand, out string[] parameters);
                command = Create(parameters);
                funcBody.Dequeue();
            }

            return command;
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

        public void Execute(Interpreter interpreter)
        {
            interpreter.Variables[variableName] = variableValue;
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

        public void Execute(Interpreter interpreter)
        {
            if (!interpreter.Variables.ContainsKey(variableName))
                interpreter.Writer.WriteLine(ExceptionTexts.VariableNotFound);
            else
            {
                var result = interpreter.Variables[variableName] - variableValue;

                if (result.IsNegative())
                    throw new ArgumentException($"{ExceptionTexts.ResultCanNotBeNegative}: " +
                        $"{interpreter.Variables[variableName]} - {variableValue} = {result}");

                interpreter.Variables[variableName] = result;
            }
        }
    }

    public class PrintCommand : IInterpreterCommand
    {
        public PrintCommand(string variableName)
        {
            this.variableName = variableName;
        }

        private readonly string variableName;

        public void Execute(Interpreter interpreter)
        {
            if (!interpreter.Variables.ContainsKey(variableName))
                interpreter.Writer.WriteLine(ExceptionTexts.VariableNotFound);
            else
                interpreter.Writer.WriteLine(interpreter.Variables[variableName]);
        }
    }

    public class RemCommand : IInterpreterCommand
    {
        public RemCommand(string variableName)
        {
            this.variableName = variableName;
        }

        private readonly string variableName;

        public void Execute(Interpreter interpreter)
        {
            if (!interpreter.Variables.ContainsKey(variableName))
                interpreter.Writer.WriteLine(ExceptionTexts.VariableNotFound);
            else
                interpreter.Variables.Remove(variableName);
        }
    }

    public class DefCommand : IInterpreterCommand
    {
        public DefCommand(string funcName)
        {
            FuncName = funcName;
            FuncBody = new List<IInterpreterCommand>();
        }

        public string FuncName { get; }
        public List<IInterpreterCommand> FuncBody { get; }

        public void Execute(Interpreter interpreter)
        {
            if (!interpreter.DefCommands.ContainsKey(FuncName))
                interpreter.DefCommands[FuncName] = this;
        }
    }

    public class CallCommand : IInterpreterCommand
    {
        public CallCommand(string funcName)
        {
            this.funcName = funcName;
        }

        private readonly string funcName;

        public void Execute(Interpreter interpreter)
        {
            if (!interpreter.DefCommands.ContainsKey(funcName))
                throw new ArgumentException($"{ExceptionTexts.FuncNotFound}. Имя функции: {funcName}");

            var commands = interpreter.DefCommands[funcName].FuncBody;
            commands.Reverse();
            interpreter.TempCommands.AddFirst(commands);
        }
    }

    public class RunCommand : IInterpreterCommand
    {
        public void Execute(Interpreter interpreter)
        {
            for (var i = 0; i < interpreter.TempCommands.Count;)
            {
                var command = interpreter.TempCommands.Dequeue().Value;
                command.Execute(interpreter);
            }

            interpreter.ClearMemory();
        }
    }
}