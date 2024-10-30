//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.IO;
//using System.Runtime.Serialization.Formatters.Binary;
//using System.Text;

//namespace CommandLineCalculator
//{
//    public sealed class StatefulInterpreter : Interpreter
//    {
//        public override void Run(UserConsole userConsole, Storage storage)
//        {
//            var state = new State(storage);
//            state.Load();

//            var input = state.CurrentCommandData == null
//                ? userConsole.ReadLine()
//                : state.CurrentCommandData.Name;

//            while (true)
//            {
//                switch (input)
//                {
//                    case CommandNames.Add:
//                        new AddCommand(state).Run(userConsole);
//                        break;
//                    case CommandNames.Median:
//                        new MedianCommand(state).Run(userConsole);
//                        break;
//                    case CommandNames.Rand:
//                        new RandomCommand(state).Run(userConsole);
//                        break;
//                    case CommandNames.Help:
//                        new HelpCommand(state).Run(userConsole);
//                        break;
//                    case CommandNames.Exit:
//                        storage.Write(Array.Empty<byte>());
//                        return;
//                    default:
//                        userConsole.WriteLine($"Такой команды нет, используйте {CommandNames.Help} для списка команд");
//                        break;
//                }

//                state.CurrentCommandData = null;
//                input = userConsole.ReadLine();
//            }
//        }
//    }

//    public static class CommandNames
//    {
//        public const string Add = "add";
//        public const string Median = "median";
//        public const string Rand = "rand";
//        public const string Help = "help";
//        public const string End = "end";
//        public const string None = "none";
//        public const string Exit = "exit";
//    }

//    public static class UserConsoleExtensions
//    {
//        public static int ReadNumber(this UserConsole console)
//        {
//            var input = console.ReadLine();
//            return int.Parse(input);
//        }
//    }

//    public interface IStatefulCommand<T>
//    {
//        State State { get; }
//        T CommandData { get; }
//        void Run(UserConsole console);
//    }

//    public class State
//    {
//        public State(Storage storage)
//        {
//            this.storage = storage;
//            serializer = new BinaryFormatter();
//        }

//        private readonly Storage storage;
//        private readonly BinaryFormatter serializer;
//        public long X { get; set; } = 420;
//        public CommandData CurrentCommandData { get; set; }

//        public void Save(bool isIncreaseActionsCounter = true)
//        {
//            if (isIncreaseActionsCounter)
//                CurrentCommandData.LastActionNumber++;

//            using (var buffer = new MemoryStream())
//            {
//                var bytes = BitConverter.GetBytes(X);
//                buffer.Write(bytes, 0, 8);
//                serializer.Serialize(buffer, CurrentCommandData);
//                storage.Write(buffer.ToArray());
//            }
//        }

//        public void Load()
//        {
//            using (var buffer = new MemoryStream(storage.Read()))
//            {
//                if (buffer.Length >= 8)
//                {
//                    var bytes = new byte[8];
//                    buffer.Read(bytes, 0, 8);
//                    X = BitConverter.ToInt64(bytes, 0);
//                }

//                if (buffer.Length > 8)
//                    CurrentCommandData = (CommandData)serializer.Deserialize(buffer);
//                else
//                    CurrentCommandData = null;
//            }
//        }

//        [Serializable]
//        public abstract class CommandData
//        {
//            public abstract string Name { get; }
//            public int LastActionNumber { get; set; }
//        }
//    }

//    public class AddCommand : IStatefulCommand<AddCommand.Data>
//    {
//        public State State { get; }
//        public Data CommandData { get; }

//        public AddCommand(State state)
//        {
//            State = state;

//            if (State.CurrentCommandData != null)
//                CommandData = (Data)State.CurrentCommandData;
//            else
//            {
//                CommandData = new Data();
//                State.CurrentCommandData = CommandData;
//                State.Save(false);
//            }
//        }

//        public void Run(UserConsole console)
//        {
//            for (var i = CommandData.LastActionNumber; i < 2; i++)
//            {
//                CommandData.Sum += console.ReadNumber();
//                State.Save();
//            }

//            if (CommandData.LastActionNumber == 2)
//            {
//                console.WriteLine(CommandData.Sum.ToString());
//                State.Save();
//            }
//        }

//        [Serializable]
//        public class Data : State.CommandData
//        {
//            public override string Name { get; } = CommandNames.Add;
//            public long Sum = 0;
//        }
//    }

//    public class MedianCommand : IStatefulCommand<MedianCommand.Data>
//    {
//        private static readonly CultureInfo culture = CultureInfo.GetCultureInfo("en-US");
//        public State State { get; }
//        public Data CommandData { get; }

//        public MedianCommand(State state)
//        {
//            State = state;

//            if (State.CurrentCommandData != null)
//                CommandData = (Data)State.CurrentCommandData;
//            else
//            {
//                CommandData = new Data();
//                State.CurrentCommandData = CommandData;
//                State.Save(false);
//            }
//        }

//        public void Run(UserConsole console)
//        {
//            if (CommandData.LastActionNumber == 0)
//            {
//                var count = console.ReadNumber();
//                CommandData.Numbers = new SortedList<int, int>(count);
//                State.Save();
//            }

//            for (var i = CommandData.LastActionNumber - 1; i < CommandData.Numbers.Capacity; i++)
//            {
//                var number = console.ReadNumber();
//                CommandData.Numbers.Add(number, number);
//                State.Save();
//            }

//            if (CommandData.LastActionNumber - 1 == CommandData.Numbers.Capacity)
//            {
//                var result = CalculateMedian(CommandData.Numbers);
//                console.WriteLine(result.ToString(culture));
//                State.Save();
//            }
//        }

//        private double CalculateMedian(SortedList<int, int> numbers)
//        {
//            var count = numbers.Count;

//            if (count == 0)
//                return 0;

//            if (count % 2 == 1)
//                return numbers.Keys[count / 2];

//            return (numbers.Keys[count / 2 - 1] + numbers.Keys[count / 2]) / 2.0;
//        }

//        [Serializable]
//        public class Data : State.CommandData
//        {
//            public override string Name { get; } = CommandNames.Median;
//            public SortedList<int, int> Numbers;
//        }
//    }

//    public class RandomCommand : IStatefulCommand<RandomCommand.Data>
//    {
//        private const int coeff = 16807;
//        public Data CommandData { get; }
//        public State State { get; }

//        public RandomCommand(State state)
//        {
//            State = state;

//            if (State.CurrentCommandData != null)
//                CommandData = (Data)State.CurrentCommandData;
//            else
//            {
//                CommandData = new Data();
//                State.CurrentCommandData = CommandData;
//                State.Save(false);
//            }
//        }

//        public void Run(UserConsole console)
//        {
//            if (CommandData.LastActionNumber == 0)
//            {
//                CommandData.RandomNumbersAmount = console.ReadNumber();
//                State.Save();
//            }

//            for (var i = CommandData.LastActionNumber - 1; i < CommandData.RandomNumbersAmount; i++)
//            {
//                console.WriteLine(State.X.ToString());
//                State.X = coeff * State.X % int.MaxValue;
//                State.Save();
//            }
//        }

//        [Serializable]
//        public class Data : State.CommandData
//        {
//            public override string Name { get; } = CommandNames.Rand;
//            public long RandomNumbersAmount = 0;
//        }
//    }

//    public class HelpCommand : IStatefulCommand<HelpCommand.Data>
//    {
//        private static readonly Dictionary<TextForUserNames, string> textsForUser = new Dictionary<TextForUserNames, string>()
//        {
//            { TextForUserNames.EnterCommand, "Укажите команду, для которой хотите посмотреть помощь" },
//            { TextForUserNames.ShowCommands, $"Доступные команды: {CommandNames.Add}, {CommandNames.Median}, {CommandNames.Rand}" },
//            { TextForUserNames.Exit, $"Чтобы выйти из режима помощи введите {CommandNames.End}" }
//        };

//        private static readonly Dictionary<string, List<string>> commandsInfo = new Dictionary<string, List<string>>
//        {
//            { CommandNames.Add,    new List<string> { "Вычисляет сумму двух чисел", textsForUser[TextForUserNames.Exit] } },
//            { CommandNames.Median, new List<string> { "Вычисляет медиану списка чисел", textsForUser[TextForUserNames.Exit] } },
//            { CommandNames.Rand,   new List<string> { "Генерирует список случайных чисел", textsForUser[TextForUserNames.Exit] } },
//            { CommandNames.None,   new List<string> { "Такой команды нет", textsForUser[TextForUserNames.ShowCommands], textsForUser[TextForUserNames.Exit] } },
//            { CommandNames.End,   new List<string>() }
//        };

//        public Data CommandData { get; }
//        public State State { get; }

//        public HelpCommand(State state)
//        {
//            State = state;

//            if (State.CurrentCommandData != null)
//                CommandData = (Data)State.CurrentCommandData;
//            else
//            {
//                CommandData = new Data();
//                State.CurrentCommandData = CommandData;
//                State.Save(false);
//            }
//        }

//        public void Run(UserConsole console)
//        {
//            for (var i = CommandData.LastActionNumber; i < textsForUser.Count; i++)
//            {
//                console.WriteLine(textsForUser[(TextForUserNames)CommandData.LastActionNumber]);
//                State.Save();
//            }

//            while (true)
//            {
//                if (string.IsNullOrEmpty(CommandData.InfoCommandName))
//                {
//                    CommandData.InfoCommandName = console.ReadLine();

//                    if (!commandsInfo.ContainsKey(CommandData.InfoCommandName))
//                        CommandData.InfoCommandName = CommandNames.None;

//                    State.Save();

//                    if (CommandData.InfoCommandName == CommandNames.End)
//                        return;
//                }

//                var messages = commandsInfo[CommandData.InfoCommandName];

//                for (var i = CommandData.LastActionNumber - 4; i < messages.Count; i++)
//                {
//                    console.WriteLine(messages[i]);
//                    if (i + 1 == messages.Count)
//                    {
//                        CommandData.InfoCommandName = null;
//                        CommandData.LastActionNumber = 3;
//                        State.Save(false);
//                        break;
//                    }
//                    State.Save();
//                }
//            }
//        }

//        [Serializable]
//        public class Data : State.CommandData
//        {
//            public override string Name { get; } = CommandNames.Help;
//            public string InfoCommandName { get; set; }
//        }

//        private enum TextForUserNames
//        {
//            EnterCommand,
//            ShowCommands,
//            Exit
//        }
//    }
//}