using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace KizhiPart3
{
    public class Debugger
    {
        public Debugger(TextWriter writer)
        {
            Writer = writer;
            commandValidator = new DebuggerCommandValidator();
            Interpreter = new Interpreter(writer);
            commands = new Dictionary<string, IDebuggerCommand>();
            BreakPoints = new Dictionary<int, IBreakPoint>();
            StackTrace = new Stack<CallCommand>();
            CallCommandLineNumbers = new HashSet<int>();
            InitCommands();
        }

        private void InitCommands()
        {
            commands.Add(DebuggerCommandNames.AddBreakPoint, new AddBreakPointCommand());
            commands.Add(DebuggerCommandNames.StepOver, new StepOverCommand());
            commands.Add(DebuggerCommandNames.Step, new StepCommand());
            commands.Add(DebuggerCommandNames.PrintMem, new PrintMemCommand());
            commands.Add(DebuggerCommandNames.PrintTrace, new PrintTraceCommand());
            commands.Add(DebuggerCommandNames.Run, new RunDebuggerCommand());
        }

        private readonly IDictionary<string, IDebuggerCommand> commands;
        private readonly IDebuggerCommandValidator commandValidator;
        public TextWriter Writer { get; }
        public Interpreter Interpreter { get; }
        public IDictionary<int, IBreakPoint> BreakPoints { get; }
        public Stack<CallCommand> StackTrace { get; }
        public HashSet<int> CallCommandLineNumbers { get; }

        public void ExecuteLine(string strCommand)
        {
            if (commandValidator.IsValid(strCommand))
            {
                GetCommandParameters(strCommand, out string[] parameters);
                var commandName = parameters[0];

                if (commandName == DebuggerCommandNames.Run && Interpreter.TempCommands.Count == 0)
                    Interpreter.TempCommands.AddLast(Interpreter.Commands);

                commands[commandName].Execute(this, parameters);
            }
            else
                Interpreter.ExecuteLine(strCommand);
        }

        public IInterpreterCommand ExecuteNextCommand(bool isBreakpointDisabled = false)
        {
            var command = Interpreter.TempCommands.First();
            PushToStackTrace(command);

            if (BreakPoints.ContainsKey(command.LineNumber) && !isBreakpointDisabled)
                return null;

            Interpreter.TempCommands.RemoveFirst();
            command.Execute(Interpreter);

            PopFromStackTrace(command.LineNumber + 1);
            return command;
        }

        public void ClearMemory()
        {
            Interpreter.ClearMemory();
            BreakPoints.Clear();
            StackTrace.Clear();
            CallCommandLineNumbers.Clear();
        }

        private void GetCommandParameters(string strCommand, out string[] parameters)
        {
            var list = new List<string>();

            if (strCommand.StartsWith(DebuggerCommandNames.AddBreakPoint))
            {
                list.Add(DebuggerCommandNames.AddBreakPoint);
                list.Add(strCommand.Remove(0, DebuggerCommandNames.AddBreakPoint.Length));
            }
            else
                list.Add(strCommand);

            parameters = list.ToArray();
        }

        private void PushToStackTrace(IInterpreterCommand command)
        {
            if (command is CallCommand callCommand)
            {
                StackTrace.Push(callCommand);
                CallCommandLineNumbers.Add(callCommand.LineNumber);
            }
        }

        private void PopFromStackTrace(int lineNumber)
        {
            if (CallCommandLineNumbers.Contains(lineNumber))
            {
                StackTrace.Pop();
                CallCommandLineNumbers.Remove(lineNumber);
            }
        }
    }

    public static class DebuggerCommandNames
    {
        public const string AddBreakPoint = "add break";
        public const string StepOver = "step over";
        public const string Step = "step";
        public const string PrintMem = "print mem";
        public const string PrintTrace = "print trace";
        public const string Run = "run";
    }

    public interface IDebuggerCommandValidator
    {
        bool IsValid(string command);
    }

    public interface IDebuggerCommand
    {
        string Name { get; }
        void Execute(Debugger debugger, params string[] parameters);
    }

    public interface IBreakPoint
    {
        int LineNumber { get; }
    }

    public class DebuggerCommandValidator : IDebuggerCommandValidator
    {
        private readonly Regex regexCommand = new Regex(
            @"\A((add break \d+)" +
            @"|(step over)" +
            @"|(step)" +
            @"|(print mem)" +
            @"|(print trace)" +
            @"|(run))\z");

        public bool IsValid(string command)
        {
            return regexCommand.IsMatch(command);
        }
    }

    public class BreakPoint : IBreakPoint, IComparable<BreakPoint>
    {
        public BreakPoint(string lineNumber)
        {
            LineNumber = int.Parse(lineNumber);
        }

        public int LineNumber { get; }

        public int CompareTo(BreakPoint other)
        {
            return LineNumber.CompareTo(other.LineNumber);
        }
    }

    public class AddBreakPointCommand : IDebuggerCommand
    {
        public string Name { get; } = DebuggerCommandNames.AddBreakPoint;

        public void Execute(Debugger debugger, string[] parameters)
        {
            var lineNumber = parameters[1];
            var breakPoint = new BreakPoint(lineNumber);
            debugger.BreakPoints.Add(breakPoint.LineNumber, breakPoint);
        }
    }

    public class StepOverCommand : IDebuggerCommand
    {
        public string Name { get; } = DebuggerCommandNames.StepOver;

        public void Execute(Debugger debugger, string[] parameters)
        {
            var interpreter = debugger.Interpreter;

            if (debugger.ExecuteNextCommand(true) is CallCommand callCommand)
            {
                var amountCommandsAddedByCallCommand = interpreter.DefCommands[callCommand.FuncName].FuncBody.Count;
                var amountCommandsBefore = interpreter.TempCommands.Count - amountCommandsAddedByCallCommand;
                var difference = -1;

                while (difference != 0)
                {
                    if (debugger.ExecuteNextCommand(true) == null)
                        return;

                    var amountCommandsAfter = interpreter.TempCommands.Count;
                    difference = amountCommandsAfter - amountCommandsBefore;
                }
            }
        }
    }

    public class StepCommand : IDebuggerCommand
    {
        public string Name { get; } = DebuggerCommandNames.Step;

        public void Execute(Debugger debugger, string[] parameters)
        {
            debugger.ExecuteNextCommand(true);
        }
    }

    public class PrintMemCommand : IDebuggerCommand
    {
        public string Name { get; } = DebuggerCommandNames.PrintMem;

        public void Execute(Debugger debugger, string[] parameters)
        {
            foreach (var variable in debugger.Interpreter.Variables.Values)
                debugger.Writer.WriteLine(variable.ToString());
        }
    }

    public class PrintTraceCommand : IDebuggerCommand
    {
        public string Name { get; } = DebuggerCommandNames.PrintTrace;

        public void Execute(Debugger debugger, string[] parameters)
        {
            foreach (var calledCommand in debugger.StackTrace)
                debugger.Writer.WriteLine(calledCommand.ToString());
        }
    }

    public class RunDebuggerCommand : IDebuggerCommand
    {
        public string Name { get; } = DebuggerCommandNames.Run;

        public void Execute(Debugger debugger, string[] parameters)
        {
            var interpreter = debugger.Interpreter;
            debugger.ExecuteNextCommand(true);

            for (var i = 0; i < interpreter.TempCommands.Count;)
            {
                if (debugger.ExecuteNextCommand() == null)
                    return;
            }

            debugger.ClearMemory();
        }
    }

    public class Interpreter
    {
        public Interpreter(TextWriter writer)
        {
            Writer = writer;
            CommandValidator = new InterpreterCommandValidator();
            CommandHandler = new InterpreterCommandHandler(this);
            CommandCreator = new InterpreterCommandСreator(this);
            Variables = new Dictionary<string, Variable>();
            DefCommands = new Dictionary<string, DefCommand>();
            Commands = new LinkedList<IInterpreterCommand>();
            TempCommands = new LinkedList<IInterpreterCommand>();
        }

        public TextWriter Writer { get; }
        public IInterpreterCommandValidator CommandValidator { get; }
        public IInterpreterCommandHandler CommandHandler { get; }
        public IInterpreterCommandСreator CommandCreator { get; }
        public IDictionary<string, Variable> Variables { get; }
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

        public bool TryValidateCommand(string command, int currentLineNumber)
        {
            if (!CommandValidator.IsValid(command))
                throw new FormatException(
                    $"{ExceptionTexts.CommandNotCorrect}. Команда: {command}. Номер строки: {currentLineNumber}");

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
        IInterpreterCommand Create(string[] parameters, int commandLineNumber);
        DefCommand CreateDef(Queue<string> funcBody, ref int currentLineNumber);
    }

    public interface IInterpreterCommand
    {
        int LineNumber { get; }
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
        private int currentLineNumber;

        public void Process(string strCommand)
        {
            interpreter.TryValidateCommand(strCommand, currentLineNumber);
            interpreter.GetCommandParameters(strCommand, out string[] parameters);
            var command = interpreter.CommandCreator.Create(parameters, currentLineNumber);

            if (TryRun(command))
                return;

            interpreter.Commands.AddLast(command);
            currentLineNumber++;
        }

        public void ProcessDef(string strDefCommand)
        {
            GetFuncBody(strDefCommand, out Queue<string> funcBody);
            var currentFuncLineNumber = currentLineNumber;
            currentLineNumber += funcBody.Count;
            interpreter.CommandCreator.CreateDef(funcBody, ref currentFuncLineNumber);
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

        public IInterpreterCommand Create(string[] parameters, int commandLineNumber)
        {
            var commandName = parameters[0];

            switch (commandName)
            {
                case InterpreterCommandNames.Set:
                    return new SetCommand(parameters[1], parameters[2], commandLineNumber);
                case InterpreterCommandNames.Sub:
                    return new SubCommand(parameters[1], parameters[2], commandLineNumber);
                case InterpreterCommandNames.Print:
                    return new PrintCommand(parameters[1], commandLineNumber);
                case InterpreterCommandNames.Rem:
                    return new RemCommand(parameters[1], commandLineNumber);
                case InterpreterCommandNames.Call:
                    return new CallCommand(parameters[1], commandLineNumber);
                case InterpreterCommandNames.Run:
                    return new RunCommand(commandLineNumber);
                default:
                    throw new ArgumentException(
                        $"{ExceptionTexts.CommandNotFound}. Имя команды: {commandName}. Номер строки: {commandLineNumber}");
            }
        }

        public DefCommand CreateDef(Queue<string> funcBody, ref int currentLineNumber)
        {
            var defCommand = CreateDefCommand(funcBody, ref currentLineNumber);
            closingBracketsAmount = 0;
            return defCommand;
        }

        private DefCommand CreateDefCommand(Queue<string> funcBody, ref int currentLineNumber)
        {
            var funcName = funcBody.Dequeue().Split(' ')[1].TrimEnd('}');
            var defCommand = new DefCommand(funcName, currentLineNumber);
            currentLineNumber++;

            while (funcBody.Count > 0)
            {
                if (closingBracketsAmount > 0)
                {
                    closingBracketsAmount--;
                    break;
                }

                var command = ParseNextCommand(funcBody, ref currentLineNumber);
                defCommand.FuncBody.Add(command);
            }

            interpreter.DefCommands[defCommand.FuncName] = defCommand;
            return defCommand;
        }

        private IInterpreterCommand ParseNextCommand(Queue<string> funcBody, ref int currentLineNumber)
        {
            var strCommand = funcBody.Peek().TrimEnd("}", ref closingBracketsAmount);
            IInterpreterCommand command;

            if (interpreter.CommandValidator.IsValidDefWithBrackets(strCommand))
                command = CreateDefCommand(funcBody, ref currentLineNumber);
            else
            {
                interpreter.TryValidateCommand(strCommand, currentLineNumber);
                interpreter.GetCommandParameters(strCommand, out string[] parameters);
                command = interpreter.CommandCreator.Create(parameters, currentLineNumber);
                funcBody.Dequeue();
                currentLineNumber++;
            }

            return command;
        }
    }

    public struct Variable
    {
        public Variable(string name, int value, int lastChangeLineNumber)
        {
            Name = name;
            Value = value;
            LastChangeLineNumber = lastChangeLineNumber;
        }

        public string Name { get; }
        public int Value { get; }
        public int LastChangeLineNumber { get; }

        public override string ToString()
        {
            return $"{Name} {Value} {LastChangeLineNumber}";
        }
    }

    public class SetCommand : IInterpreterCommand
    {
        public SetCommand(string variableName, string variableValue, int lineNumber)
        {
            variable = new Variable(variableName, int.Parse(variableValue), lineNumber);
            LineNumber = lineNumber;
        }

        private readonly Variable variable;
        public int LineNumber { get; }

        public void Execute(Interpreter interpreter)
        {
            interpreter.Variables[variable.Name] = variable;
        }
    }

    public class SubCommand : IInterpreterCommand
    {
        public SubCommand(string variableName, string variableValue, int lineNumber)
        {
            this.variableName = variableName;
            this.variableValue = int.Parse(variableValue);
            LineNumber = lineNumber;
        }

        private readonly string variableName;
        private readonly int variableValue;
        public int LineNumber { get; }

        public void Execute(Interpreter interpreter)
        {
            if (!interpreter.Variables.ContainsKey(variableName))
                interpreter.Writer.WriteLine(ExceptionTexts.VariableNotFound);
            else
            {
                var result = interpreter.Variables[variableName].Value - variableValue;

                if (result.IsNegative())
                    throw new ArgumentException($"{ExceptionTexts.ResultCanNotBeNegative}: " +
                        $"{interpreter.Variables[variableName].Value} - {variableValue} = {result}");

                var variable = new Variable(variableName, result, LineNumber);
                interpreter.Variables[variableName] = variable;
            }
        }
    }

    public class PrintCommand : IInterpreterCommand
    {
        public PrintCommand(string variableName, int lineNumber)
        {
            this.variableName = variableName;
            LineNumber = lineNumber;
        }

        private readonly string variableName;
        public int LineNumber { get; }

        public void Execute(Interpreter interpreter)
        {
            if (!interpreter.Variables.ContainsKey(variableName))
                interpreter.Writer.WriteLine(ExceptionTexts.VariableNotFound);
            else
                interpreter.Writer.WriteLine(interpreter.Variables[variableName].Value);
        }
    }

    public class RemCommand : IInterpreterCommand
    {
        public RemCommand(string variableName, int lineNumber)
        {
            this.variableName = variableName;
            LineNumber = lineNumber;
        }

        private readonly string variableName;
        public int LineNumber { get; }

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
        public DefCommand(string funcName, int lineNumber)
        {
            FuncName = funcName;
            LineNumber = lineNumber;
            FuncBody = new List<IInterpreterCommand>();
        }

        public string FuncName { get; }
        public int LineNumber { get; }
        public List<IInterpreterCommand> FuncBody { get; }

        public void Execute(Interpreter interpreter)
        {
            if (!interpreter.DefCommands.ContainsKey(FuncName))
                interpreter.DefCommands[FuncName] = this;
        }
    }

    public class CallCommand : IInterpreterCommand
    {
        public CallCommand(string funcName, int lineNumber)
        {
            FuncName = funcName;
            LineNumber = lineNumber;
        }

        public string FuncName { get; }
        public int LineNumber { get; }

        public void Execute(Interpreter interpreter)
        {
            if (!interpreter.DefCommands.ContainsKey(FuncName))
                throw new ArgumentException(
                    $"{ExceptionTexts.FuncNotFound}. Имя функции: {FuncName}. Номер строки: {LineNumber}");

            var commands = interpreter.DefCommands[FuncName].FuncBody;
            commands.Reverse();
            interpreter.TempCommands.AddFirst(commands);
        }

        public override string ToString()
        {
            return $"{LineNumber} {FuncName}";
        }
    }

    public class RunCommand : IInterpreterCommand
    {
        public RunCommand(int lineNumber)
        {
            LineNumber = lineNumber;
        }

        public int LineNumber { get; }

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