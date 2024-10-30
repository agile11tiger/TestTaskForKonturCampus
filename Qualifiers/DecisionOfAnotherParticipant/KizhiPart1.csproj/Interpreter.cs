using System;
using System.Collections.Generic;
using System.IO;

namespace KizhiPart1
{
    public class Interpreter
    {
        private TextWriter _writer;

        private readonly Dictionary<string, long> _variables = new Dictionary<string, long>();

        private const string errorUndefinedMessage = "Переменная отсутствует в памяти";

        public Interpreter(TextWriter writer)
        {
            _writer = writer;
        }

        public void ExecuteLine(string command)
        {
            var lex = command.Split();
            {
                switch (lex[0])
                {
                    case "set":
                        ExecuteSet(lex[1], Convert.ToInt64(lex[2]));
                        break;
                    case "sub":
                        ExecuteSub(lex[1], Convert.ToInt64(lex[2]));
                        break;
                    case "print":
                        ExecutePrint(lex[1]);
                        break;
                    case "rem":
                        ExecuteRem(lex[1]);
                        break;
                }
            }
        }

        private void ExecuteSet(string varName, long val)
        {
            _variables[varName] = val;
        }

        void ExecuteSub(string varName, long val)
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