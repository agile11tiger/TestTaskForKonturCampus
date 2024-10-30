using System;
using System.Collections.Generic;
using System.IO;

namespace KizhiPart2
{
    using Function = List<string>;

    public class Interpreter
    {
        private TextWriter _writer;

        private State _state = State.WaitCmd;

        private readonly Dictionary<string, long> _variables = new Dictionary<string, long>();
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

        private class FunctionPosition
        {
            // Position inside function
            public int pos;
            public Function function;

            public FunctionPosition(int pos = 0, Function function = null)
            {
                this.pos = pos;
                this.function = function;
            }
        }

        public Interpreter(TextWriter writer)
        {
            _writer = writer;
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
                            break;
                        case "run":
                            RunProgram();
                            _state = State.WaitCmd;
                            break;
                        default:
                            throw new Exception("Ожидалось \"set code\" или \"run\", но получено \"" + command + "\"");
                    }
                    break;
                case State.ReadCode:
                    Parse(command);
                    _state = State.ReadEndCode;
                    break;
                case State.ReadEndCode:
                    if (command != "end set code")
                    {
                        throw new Exception("Ожидалось \"end set code\", но получено " + command + "\"");
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
            var mainFunction = new Function();
            _functions[mainFunctionName] = mainFunction;
            for (var i = 0; i < fullProgramLines.Length; i++)
            {
                var line = RemoveNotUsedSymbols(fullProgramLines[i]);
                var lex = line.Split();

                switch (lex[0])
                {
                    case "def":
                        if (_functions.ContainsKey(lex[1]))
                        {
                            throw new Exception("Переопределение функции " + line);
                        }
                        var function = new Function();
                        _functions[lex[1]] = function;

                        var r = i + 1;
                        while (r < fullProgramLines.Length && IsStartWithSpaces(fullProgramLines[r]))
                        {
                            function.Add(RemoveNotUsedSymbols(fullProgramLines[r]));
                            r++;
                        }

                        i = r - 1;
                        break;
                    default:
                        mainFunction.Add(line);
                        break;
                }

            }

        }

        private void RunProgram()
        {
            _currentReadingPosition = new FunctionPosition(0, _functions[mainFunctionName]);

            while (true)
            {
                var line = _currentReadingPosition.function[_currentReadingPosition.pos];
                var lex = line.Split();

                switch (lex[0])
                {
                    case "set":
                        ExecuteSet(lex[1], Convert.ToInt64(lex[2]));
                        _currentReadingPosition.pos++;
                        break;
                    case "sub":
                        ExecuteSub(lex[1], Convert.ToInt64(lex[2]));
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
                }

                // Returning from function
                while (_currentReadingPosition.pos == _currentReadingPosition.function.Count)
                {
                    // End of main function
                    if (_stack.Count == 0)
                    {
                        Cleanup();
                        return;
                    }
                    _currentReadingPosition = _stack.Pop();
                    _currentReadingPosition.pos++;
                }
            }
        }

        private void Cleanup()
        {
            _variables.Clear();
            _functions.Clear();
            _stack.Clear();
            _currentReadingPosition = new FunctionPosition();
        }

        private void ExecuteSet(string varName, long val)
        {
            _variables[varName] = val;
        }

        private void ExecuteSub(string varName, long val)
        {
            if (_variables.ContainsKey(varName))
            {
                _variables[varName] -= val;
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
                _writer.WriteLine(_variables[varName]);
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
        var i = new KizhiPart2.Interpreter(Console.Out);

        var pr =
@"call test
print a
def test
    set a 5";

        string[] lines =
        {
            "set code",
            pr,
            "end set code",
            "run"
        };

        foreach (var line in lines)
        {
            i.ExecuteLine(line);
        }

    }
}