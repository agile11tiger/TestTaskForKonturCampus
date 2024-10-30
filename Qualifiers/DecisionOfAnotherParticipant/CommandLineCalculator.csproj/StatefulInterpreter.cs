using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace CommandLineCalculator
{
    public sealed class StatefulInterpreter : Interpreter
    {
        private static CultureInfo Culture => CultureInfo.InvariantCulture;

        class StatefulUserConsole : UserConsole
        {
            private UserConsole _userConsole;
            private Storage _storage;

            // State point of app
            private State _state;

            // User input from state point to saved state of app
            private StateLoadingInfo _stateLoadingInfo;

            [Serializable]
            private class State
            {
                public readonly LinkedList<string> userInput = new LinkedList<string>();
                public int countOutput = 0;
                public long x = 420;
            }

            private class StateLoadingInfo
            {
                public readonly LinkedList<string> userInput = new LinkedList<string>();
                public int countOutput = 0;
            }

            public StatefulUserConsole(UserConsole userConsole, Storage storage)
            {
                _userConsole = userConsole;
                _storage = storage;
                LoadState();
            }

            public override string ReadLine()
            {
                if (_stateLoadingInfo.userInput.Any())
                {
                    var loadedInputLine = _stateLoadingInfo.userInput.First.Value;
                    _stateLoadingInfo.userInput.RemoveFirst();
                    return loadedInputLine;
                }
                var inputLine = _userConsole.ReadLine();
                _state.userInput.AddLast(inputLine);
                SaveState();
                return inputLine;
            }

            public override void WriteLine(string content)
            {
                if (_stateLoadingInfo.countOutput > 0)
                {
                    _stateLoadingInfo.countOutput--;
                    return;
                }
                _userConsole.WriteLine(content);
                _state.countOutput++;
                if (_state.countOutput > 10000)
                {
                    throw new StackOverflowException("Over 10000");
                }
                SaveState();
            }

            private static byte[] Serialize<T>(T obj)
            {
                var formatter = new BinaryFormatter();
                var stream = new MemoryStream();
                formatter.Serialize(stream, obj);
                return stream.ToArray();
            }

            private static T Deserialize<T>(byte[] data)
            {
                var formatter = new BinaryFormatter();
                var stream = new MemoryStream(data);
                return (T)formatter.Deserialize(stream);
            }

            private void LoadState()
            {
                _stateLoadingInfo = new StateLoadingInfo();
                var data = _storage.Read();
                if (data.Length == 0)
                {
                    _state = new State();
                    return;
                }
                _state = Deserialize<State>(data);
                foreach (var text in _state.userInput)
                {
                    _stateLoadingInfo.userInput.AddLast(text);
                }
                _stateLoadingInfo.countOutput = _state.countOutput;
            }

            private void SaveState()
            {
                _storage.Write(Serialize(_state));
            }

            private bool IsLoadingState
            {
                get { return _stateLoadingInfo.countOutput != 0 || _stateLoadingInfo.userInput.Any(); }
            }

            public long GetStatePoint()
            {
                return _state.x;
            }

            public void SetNewStatePoint(long x)
            {
                if (IsLoadingState)
                {
                    return;
                }
                _state.x = x;
                _state.userInput.Clear();
                _state.countOutput = 0;
                SaveState();
            }

            public void ClearState()
            {
                _state = new State();
                SaveState();
            }
        }

        public override void Run(UserConsole statelessUserConsole, Storage storage)
        {
            var userConsole = new StatefulUserConsole(statelessUserConsole, storage);
            long x = userConsole.GetStatePoint();

            while (true)
            {
                var input = userConsole.ReadLine();
                switch (input.Trim())
                {
                    case "exit":
                        userConsole.ClearState();
                        return;
                    case "add":
                        Add(userConsole);
                        break;
                    case "median":
                        Median(userConsole);
                        break;
                    case "help":
                        Help(userConsole);
                        break;
                    case "rand":
                        x = Random(userConsole, x);
                        break;
                    default:
                        userConsole.WriteLine("Такой команды нет, используйте help для списка команд");
                        break;
                }
                userConsole.SetNewStatePoint(x);
            }
        }

        private long Random(UserConsole console, long x)
        {
            const int a = 16807;
            const int m = 2147483647;

            var count = ReadNumber(console);
            for (var i = 0; i < count; i++)
            {
                console.WriteLine(x.ToString(Culture));
                x = a * x % m;
            }

            return x;
        }

        private void Add(UserConsole console)
        {
            var a = ReadNumber(console);
            var b = ReadNumber(console);
            console.WriteLine((a + b).ToString(Culture));
        }

        private void Median(UserConsole console)
        {
            var count = ReadNumber(console);
            var numbers = new List<int>();
            for (var i = 0; i < count; i++)
            {
                numbers.Add(ReadNumber(console));
            }

            var result = CalculateMedian(numbers);
            console.WriteLine(result.ToString(Culture));
        }

        private double CalculateMedian(List<int> numbers)
        {
            numbers.Sort();
            var count = numbers.Count;
            if (count == 0)
                return 0;

            if (count % 2 == 1)
                return numbers[count / 2];

            return (numbers[count / 2 - 1] + numbers[count / 2]) / 2.0;
        }

        private static void Help(UserConsole console)
        {
            const string exitMessage = "Чтобы выйти из режима помощи введите end";
            const string commands = "Доступные команды: add, median, rand";

            console.WriteLine("Укажите команду, для которой хотите посмотреть помощь");
            console.WriteLine(commands);
            console.WriteLine(exitMessage);
            while (true)
            {
                var command = console.ReadLine();
                switch (command.Trim())
                {
                    case "end":
                        return;
                    case "add":
                        console.WriteLine("Вычисляет сумму двух чисел");
                        console.WriteLine(exitMessage);
                        break;
                    case "median":
                        console.WriteLine("Вычисляет медиану списка чисел");
                        console.WriteLine(exitMessage);
                        break;
                    case "rand":
                        console.WriteLine("Генерирует список случайных чисел");
                        console.WriteLine(exitMessage);
                        break;
                    default:
                        console.WriteLine("Такой команды нет");
                        console.WriteLine(commands);
                        console.WriteLine(exitMessage);
                        break;
                }
            }
        }

        private int ReadNumber(UserConsole console)
        {
            return int.Parse(console.ReadLine().Trim(), Culture);
        }
    }
}