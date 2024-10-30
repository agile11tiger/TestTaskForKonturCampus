using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KizhiPart3
{
    public class Debugger
    {
        private TextWriter _writer;

        private State _state = State.WaitCmd;

        private bool _isFirstRun = true;
        private readonly HashSet<int> _breakPoints = new HashSet<int>();
        private readonly Dictionary<string, VariableInfo> _variables = new Dictionary<string, VariableInfo>();
        private readonly Dictionary<string, Function> _functions = new Dictionary<string, Function>();
        private readonly Stack<FunctionPosition> _stack = new Stack<FunctionPosition>();
        private FunctionPosition _currentReadingPosition;

        private const string errorUndefinedMessage = "Переменная отсутствует в памяти";
        private const string mainFunctionName = "#";

        private enum State
        {
            WaitCmd,
            ReadCode,
            ReadEndCode
        };

        private enum ProgramState
        {
            BreakPoint,
            StepEnd,
            Exit
        }

        private enum RunMode
        {
            FirstBreakPoint,
            Step,
            StepOver
        }

        private struct ProgramLine
        {
            public ProgramLine(string line, int lineNumber)
            {
                lex = RemoveNotUsedSymbols(line).Split();
                sourceLineNumber = lineNumber;
            }
            public readonly string[] lex;
            public readonly int sourceLineNumber;
        }

        private class Function
        {
            public Function(string name)
            {
                this.name = name;
            }
            public readonly string name;
            // Line number in source file
            public readonly List<ProgramLine> lines = new List<ProgramLine>();
        }

        private struct FunctionPosition
        {
            public FunctionPosition(int pos, Function function)
            {
                this.pos = pos;
                this.function = function;
            }
            // Position inside function
            public int pos;
            public Function function;

            public int SourceLine
            {
                get { return function.lines[pos].sourceLineNumber; }
            }
        }

        private struct VariableInfo
        {
            public VariableInfo(long val, FunctionPosition position)
            {
                this.position = position;
                this.val = val;
            }
            public long val;
            public FunctionPosition position;
        }

        public Debugger(TextWriter writer)
        {
            _writer = writer;
        }

        private void OutputAllMemory()
        {
            foreach (var pair in _variables)
            {
                _writer.WriteLine(pair.Key + " " + pair.Value.val + " " + pair.Value.position.SourceLine);
            }
        }

        private void OutputStackTrace()
        {
            var functionName = _currentReadingPosition.function.name;
            foreach (var position in _stack)
            {
                _writer.WriteLine(position.SourceLine + " " + functionName);
                functionName = position.function.name;
            }
        }

        private void ClearProgramState()
        {
            _isFirstRun = true;
            _variables.Clear();
            _stack.Clear();
        }

        private void ClearProgram()
        {
            ClearProgramState();
            _functions.Clear();
            _breakPoints.Clear();
        }

        public void ExecuteLine(string command)
        {
            switch (_state)
            {
                case State.WaitCmd:
                    switch (command)
                    {
                        case "set code":
                            _state = State.ReadCode;
                            ClearProgram();
                            break;
                        case "run":
                            RunProgram(!_isFirstRun);
                            break;
                        case "print mem":
                            OutputAllMemory();
                            break;
                        case "step":
                            RunProgram(true, RunMode.Step);
                            break;
                        case "step over":
                            RunProgram(true, RunMode.StepOver);
                            break;
                        case "print trace":
                            OutputStackTrace();
                            break;
                        default:
                            var lex = command?.Split();
                            if (lex != null && lex.Length == 3 && lex[0] == "add" && lex[1] == "break")
                            {
                                _breakPoints.Add(Convert.ToInt32(lex[2]));
                            }
                            else
                            {
                                throw new Exception("Нераспознаная комманда \"" + command + "\"");
                            }
                            break;
                    }
                    break;
                case State.ReadCode:
                    Parse(command);
                    _state = State.ReadEndCode;
                    break;
                case State.ReadEndCode:
                    if (command != "end set code")
                    {
                        throw new Exception("Ожидалось \"end set code\", но получено \"" + command + "\"");
                    }
                    _state = State.WaitCmd;
                    break;
            }
        }

        private static bool IsStartWithSpaces(string s)
        {
            return s[0] == ' ' || s[0] == '\t';
        }

        private static string RemoveNotUsedSymbols(string s)
        {
            return s.TrimStart(' ', '\t').Trim('\r');
        }

        private void Parse(string programText)
        {
            var fullProgramLines = programText.Split('\n');

            _functions.Clear();
            var mainFunction = new Function(mainFunctionName);
            _functions[mainFunctionName] = mainFunction;
            for (var i = 0; i < fullProgramLines.Length; i++)
            {
                var line = fullProgramLines[i];
                var lex = line.Split();
                switch (lex[0])
                {
                    case "def":
                        var functionName = lex[1];
                        if (_functions.ContainsKey(functionName))
                        {
                            throw new Exception("Переопределение функции " + line);
                        }
                        var function = new Function(functionName);
                        _functions[functionName] = function;

                        //  Add line with 'def'
                        function.lines.Add(new ProgramLine(fullProgramLines[i], i));
                        var r = i + 1;
                        while (r < fullProgramLines.Length && IsStartWithSpaces(fullProgramLines[r]))
                        {
                            function.lines.Add(new ProgramLine(fullProgramLines[r], r));
                            r++;
                        }

                        i = r - 1;
                        break;
                    default:
                        mainFunction.lines.Add(new ProgramLine(line, i));
                        break;
                }
            }
        }

        private void RunProgram(bool isSkipCurrentLineBreakpoint = false, RunMode mode = RunMode.FirstBreakPoint)
        {
            var runResult = RunProgramDebugger(isSkipCurrentLineBreakpoint, mode);
            _isFirstRun = false;
            if (runResult == ProgramState.Exit)
            {
                ClearProgramState();
            }
        }

        private ProgramState RunProgramDebugger(bool isSkipCurrentLineBreakpoint, RunMode mode)
        {
            if (_isFirstRun)
            {
                _currentReadingPosition = new FunctionPosition(0, _functions[mainFunctionName]);
            }

            var stepOverStackLevel = _stack.Count;

            while (true)
            {
                if (!isSkipCurrentLineBreakpoint)
                {
                    if (_breakPoints.Contains(_currentReadingPosition.SourceLine))
                    {
                        return ProgramState.BreakPoint;
                    }
                }
                isSkipCurrentLineBreakpoint = false;

                var line = _currentReadingPosition.function.lines[_currentReadingPosition.pos];
                var lex = line.lex;

                switch (lex[0])
                {
                    case "def":
                        _currentReadingPosition.pos++;
                        break;
                    case "set":
                        ExecuteSet(lex[1], Convert.ToInt64(lex[2]), _currentReadingPosition);
                        _currentReadingPosition.pos++;
                        break;
                    case "sub":
                        ExecuteSub(lex[1], Convert.ToInt64(lex[2]), _currentReadingPosition);
                        _currentReadingPosition.pos++;
                        break;
                    case "print":
                        ExecutePrint(lex[1]);
                        _currentReadingPosition.pos++;
                        break;
                    case "rem":
                        ExecuteRem(lex[1]);
                        _currentReadingPosition.pos++;
                        break;
                    case "call":
                        _stack.Push(_currentReadingPosition);
                        _currentReadingPosition = new FunctionPosition(0, _functions[lex[1]]);
                        break;
                    default:
                        throw new Exception("Нераспознаное выражение в программе \"" + lex[0] + "\"");
                }

                // Returning from function
                while (_currentReadingPosition.pos == _currentReadingPosition.function.lines.Count)
                {
                    // End of main function
                    if (!_stack.Any())
                    {
                        return ProgramState.Exit;
                    }
                    _currentReadingPosition = _stack.Pop();
                    _currentReadingPosition.pos++;
                }

                // Debug stop program
                if (mode == RunMode.Step || mode == RunMode.StepOver && _stack.Count <= stepOverStackLevel)
                {
                    return ProgramState.StepEnd;
                }
            }
        }

        private void ExecuteSet(string varName, long val, FunctionPosition funcPos)
        {
            _variables[varName] = new VariableInfo(val, funcPos);
        }

        private void ExecuteSub(string varName, long val, FunctionPosition funcPos)
        {
            if (_variables.ContainsKey(varName))
            {
                _variables[varName] = new VariableInfo(_variables[varName].val - val, funcPos);
            }
            else
            {
                _writer.WriteLine(errorUndefinedMessage);
            }
        }

        private void ExecutePrint(string varName)
        {
            if (_variables.ContainsKey(varName))
            {
                _writer.WriteLine(_variables[varName].val);
            }
            else
            {
                _writer.WriteLine(errorUndefinedMessage);
            }
        }

        private void ExecuteRem(string varName)
        {
            if (_variables.ContainsKey(varName))
            {
                _variables.Remove(varName);
            }
            else
            {
                _writer.WriteLine(errorUndefinedMessage);
            }
        }
    }
}

public class Program
{
    public static void Main()
    {
        var i = new KizhiPart3.Debugger(Console.Out);
        var pr =
@"def test
    sub set 1
    print a
def test1
    sub set 1
    call test
    print a
set set 1000
set set 1000
call test1";

        string[] lines =
        {
            "set code",
            pr,
            "end set code",
            "add break 0",
            "run",
            "print mem",
            "print trace",
            "step",
            "step",
            "step",
            "print mem",
            "print trace",
            "step",
            "print mem",
            "print trace"
        };

        foreach (var line in lines)
        {
            i.ExecuteLine(line);
        }
    }
}