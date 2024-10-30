using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CommandLineCalculator
{
    public sealed class StatefulInterpreter : Interpreter
    {
        public static CultureInfo Culture => CultureInfo.InvariantCulture;

        public override void Run(UserConsole userConsole, Storage storage)
        {
            var state = new State(storage);
            state.Load();

            var input = state.CurrentCommandData == null
                ? userConsole.ReadLine()
                : state.CurrentCommandData.Name;

            while (true)
            {
                switch (input)
                {
                    case CommandNames.Add:
                        new AddCommand(state).Run(userConsole);
                        break;
                    case CommandNames.Median:
                        new MedianCommand(state).Run(userConsole);
                        break;
                    case CommandNames.Rand:
                        new RandomCommand(state).Run(userConsole);
                        break;
                    case CommandNames.Help:
                        new HelpCommand(state).Run(userConsole);
                        break;
                    case CommandNames.Exit:
                        new ExitCommand(state).Run(userConsole);
                        return;
                    default:
                        new UnknownCommand(state).Run(userConsole);
                        break;
                }

                state.CurrentCommandData = null;
                input = userConsole.ReadLine();
            }
        }
    }

    internal static class CommandNames
    {
        public const string Add = "add";
        public const string Median = "median";
        public const string Rand = "rand";
        public const string Help = "help";
        public const string Exit = "exit";
        public const string Unknown = "unknown";
    }

    internal static class UserConsoleExtensions
    {
        public static int ReadNumber(this UserConsole console)
        {
            var input = console.ReadLine();
            return int.Parse(input);
        }
    }

    internal interface IStatefulCommand<T>
    {
        State State { get; }
        T CommandData { get; }
        void Run(UserConsole console);
    }

    internal class State
    {
        private readonly Dictionary<string, Func<CommandData>> commands = new Dictionary<string, Func<CommandData>>
        {
            { CommandNames.Add, () => new AddCommand.Data() },
            { CommandNames.Median, () => new MedianCommand.Data() },
            { CommandNames.Rand, () => new RandomCommand.Data() },
            { CommandNames.Help, () => new HelpCommand.Data() },
            { CommandNames.Exit, () => new ExitCommand.Data() },
            { CommandNames.Unknown, () => new UnknownCommand.Data() },
        };
        private readonly StringBuilder sb;
        private readonly Storage storage;
        public long X { get; set; } = 420;
        public CommandData CurrentCommandData { get; set; }

        public State(Storage storage)
        {
            this.storage = storage;
            sb = new StringBuilder(50);
        }

        public void Save(bool isIncreaseActionsCounter = true)
        {
            if (isIncreaseActionsCounter)
                CurrentCommandData.LastActionNumber++;

            var data = EncodeData();
            storage.Write(data);
        }

        public void Load()
        {
            var strData = Encoding.UTF8.GetString(storage.Read());

            if (!string.IsNullOrEmpty(strData))
                DecodeData(strData);
        }

        public void ClearStorage()
        {
            storage.Write(Array.Empty<byte>());
        }

        private byte[] EncodeData()
        {
            sb.Append($"{X}\n");
            sb.Append($"{CurrentCommandData.Name}\n");
            sb.Append($"{CurrentCommandData.LastActionNumber}\n");
            CurrentCommandData.EncodeData(sb);
            var data = Encoding.UTF8.GetBytes(sb.ToString());
            sb.Clear();
            return data;
        }

        private void DecodeData(string strData)
        {
            var data = strData.Split('\n');

            if (data.Length >= 1)
                X = int.Parse(data[0]);

            if (data.Length > 1)
            {
                var name = data[1];
                CurrentCommandData = commands[name].Invoke();
                CurrentCommandData.LastActionNumber = int.Parse(data[2]);
                CurrentCommandData.DecodeData(data);
            }
            else
                CurrentCommandData = null;
        }

        public abstract class CommandData
        {
            public abstract string Name { get; }
            public int LastActionNumber { get; set; }
            public abstract void EncodeData(StringBuilder sb);
            public abstract void DecodeData(string[] data);
        }
    }

    internal class AddCommand : IStatefulCommand<AddCommand.Data>
    {
        public State State { get; }
        public Data CommandData { get; }

        public AddCommand(State state)
        {
            State = state;

            if (State.CurrentCommandData != null)
                CommandData = (Data)State.CurrentCommandData;
            else
            {
                CommandData = new Data();
                State.CurrentCommandData = CommandData;
                State.Save(false);
            }
        }

        public void Run(UserConsole console)
        {
            for (var i = CommandData.LastActionNumber; i < 2; i++)
            {
                CommandData.Sum += console.ReadNumber();
                State.Save();
            }

            if (CommandData.LastActionNumber == 2)
            {
                console.WriteLine(CommandData.Sum.ToString());
                State.Save();
            }
        }

        public sealed class Data : State.CommandData
        {
            public override string Name { get; } = CommandNames.Add;
            public long Sum { get; set; } = 0;

            public override void EncodeData(StringBuilder sb)
            {
                sb.Append($"{Sum}");
            }

            public override void DecodeData(string[] data)
            {
                Sum = int.Parse(data[3]);
            }
        }
    }

    internal class MedianCommand : IStatefulCommand<MedianCommand.Data>
    {
        public State State { get; }
        public Data CommandData { get; }

        public MedianCommand(State state)
        {
            State = state;

            if (State.CurrentCommandData != null)
                CommandData = (Data)State.CurrentCommandData;
            else
            {
                CommandData = new Data();
                State.CurrentCommandData = CommandData;
                State.Save(false);
            }
        }

        public void Run(UserConsole console)
        {
            if (CommandData.LastActionNumber == 0)
            {
                CommandData.NumbersCapacity = console.ReadNumber();
                CommandData.Numbers = new List<int>();
                State.Save();
            }

            for (var i = CommandData.LastActionNumber - 1; i < CommandData.NumbersCapacity; i++)
            {
                var number = console.ReadNumber();
                CommandData.Numbers.Add(number);
                State.Save();
            }

            if (CommandData.LastActionNumber - 1 == CommandData.NumbersCapacity)
            {
                var result = CalculateMedian(CommandData.Numbers);
                console.WriteLine(result.ToString(StatefulInterpreter.Culture));
                State.Save();
            }
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

        public sealed class Data : State.CommandData
        {
            private const int possibleFailureThrough = 7;
            public override string Name { get; } = CommandNames.Median;
            public List<int> Numbers { get; set; }
            public int NumbersCapacity { get; set; }

            public override void EncodeData(StringBuilder sb)
            {
                if (Numbers != null)
                {
                    sb.Append($"{NumbersCapacity}\n");
                    sb.Append($"{Numbers.Count}\n");

                    foreach (var number in Numbers)
                        sb.Append($"{number}\n");
                }
                else
                    sb.Append($"{NumbersCapacity}");
            }

            public override void DecodeData(string[] data)
            {
                NumbersCapacity = int.Parse(data[3]);

                if (data.Length > 4)
                {
                    var count = int.Parse(data[4]);
                    Numbers = new List<int>(count + possibleFailureThrough);

                    for (var i = 5; i < count + 5; i++)
                        Numbers.Add(int.Parse(data[i]));
                }
            }
        }
    }

    internal class RandomCommand : IStatefulCommand<RandomCommand.Data>
    {
        private const int coeff = 16807;
        public Data CommandData { get; }
        public State State { get; }

        public RandomCommand(State state)
        {
            State = state;

            if (State.CurrentCommandData != null)
                CommandData = (Data)State.CurrentCommandData;
            else
            {
                CommandData = new Data();
                State.CurrentCommandData = CommandData;
                State.Save(false);
            }
        }

        public void Run(UserConsole console)
        {
            if (CommandData.LastActionNumber == 0)
            {
                CommandData.RandomNumbersAmount = console.ReadNumber();
                State.Save();
            }

            for (var i = CommandData.LastActionNumber - 1; i < CommandData.RandomNumbersAmount; i++)
            {
                console.WriteLine(State.X.ToString());
                State.X = coeff * State.X % int.MaxValue;
                State.Save();
            }
        }

        public sealed class Data : State.CommandData
        {
            public override string Name { get; } = CommandNames.Rand;
            public long RandomNumbersAmount { get; set; } = 0;

            public override void EncodeData(StringBuilder sb)
            {
                sb.Append($"{RandomNumbersAmount}");
            }

            public override void DecodeData(string[] data)
            {
                RandomNumbersAmount = int.Parse(data[3]);
            }
        }
    }

    internal class HelpCommand : IStatefulCommand<HelpCommand.Data>
    {
        private readonly Dictionary<TextNamesForUser, string> textsForUser;
        private readonly Dictionary<string, List<string>> commandsInfo;
        public Data CommandData { get; }
        public State State { get; }

        public HelpCommand(State state): this()
        {
            State = state;

            if (State.CurrentCommandData != null)
                CommandData = (Data)State.CurrentCommandData;
            else
            {
                CommandData = new Data();
                State.CurrentCommandData = CommandData;
                State.Save(false);
            }
        }

        private HelpCommand()
        {
            textsForUser = new Dictionary<TextNamesForUser, string>()
            {
                { TextNamesForUser.EnterCommand, "Укажите команду, для которой хотите посмотреть помощь" },
                { TextNamesForUser.ShowCommands, $"Доступные команды: {CommandNames.Add}, {CommandNames.Median}, {CommandNames.Rand}" },
                { TextNamesForUser.Exit, $"Чтобы выйти из режима помощи введите {HelpCommandNames.End}" }
            };
            commandsInfo = new Dictionary<string, List<string>>
            {
                { CommandNames.Add,    new List<string> { "Вычисляет сумму двух чисел", textsForUser[TextNamesForUser.Exit] } },
                { CommandNames.Median, new List<string> { "Вычисляет медиану списка чисел", textsForUser[TextNamesForUser.Exit] } },
                { CommandNames.Rand,   new List<string> { "Генерирует список случайных чисел", textsForUser[TextNamesForUser.Exit] } },
                { HelpCommandNames.None,   new List<string> { "Такой команды нет", textsForUser[TextNamesForUser.ShowCommands], textsForUser[TextNamesForUser.Exit] } },
                { HelpCommandNames.End,    new List<string>() }
            };
        }

        public void Run(UserConsole console)
        {
            for (var i = CommandData.LastActionNumber; i < textsForUser.Count; i++)
            {
                console.WriteLine(textsForUser[(TextNamesForUser)CommandData.LastActionNumber]);
                State.Save();
            }

            while (true)
            {
                if (string.IsNullOrEmpty(CommandData.InfoCommandName))
                {
                    CommandData.InfoCommandName = console.ReadLine();

                    if (!commandsInfo.ContainsKey(CommandData.InfoCommandName))
                        CommandData.InfoCommandName = HelpCommandNames.None;

                    State.Save();
                }

                if (CommandData.InfoCommandName == HelpCommandNames.End)
                    return;

                var messages = commandsInfo[CommandData.InfoCommandName];

                for (var i = CommandData.LastActionNumber - 4; i < messages.Count; i++)
                {
                    console.WriteLine(messages[i]);

                    if (i + 1 == messages.Count)
                    {
                        CommandData.InfoCommandName = "";
                        CommandData.LastActionNumber = 3;
                        State.Save(false);
                        break;
                    }

                    State.Save();
                }
            }
        }

        public sealed class Data : State.CommandData
        {
            public override string Name { get; } = CommandNames.Help;
            public string InfoCommandName { get; set; }

            public override void EncodeData(StringBuilder sb)
            {
                sb.Append($"{InfoCommandName}");
            }

            public override void DecodeData(string[] data)
            {
                InfoCommandName = data[3];
            }
        }

        private static class HelpCommandNames
        {
            public const string End = "end";
            public const string None = "none";
        }

        private enum TextNamesForUser
        {
            EnterCommand,
            ShowCommands,
            Exit
        }
    }

    internal class ExitCommand : IStatefulCommand<ExitCommand.Data>
    {
        public State State { get; }
        public Data CommandData { get; }

        public ExitCommand(State state)
        {
            State = state;

            if (State.CurrentCommandData != null)
                CommandData = (Data)State.CurrentCommandData;
            else
            {
                CommandData = new Data();
                State.CurrentCommandData = CommandData;
                State.Save(false);
            }
        }

        public void Run(UserConsole console)
        {
            State.ClearStorage();
        }

        public sealed class Data : State.CommandData
        {
            public override string Name { get; } = CommandNames.Exit;
            public override void EncodeData(StringBuilder sb) { }
            public override void DecodeData(string[] data) { }
        }
    }

    internal class UnknownCommand : IStatefulCommand<UnknownCommand.Data>
    {
        public State State { get; }
        public Data CommandData { get; }

        public UnknownCommand(State state)
        {
            State = state;

            if (State.CurrentCommandData != null)
                CommandData = (Data)State.CurrentCommandData;
            else
            {
                CommandData = new Data();
                State.CurrentCommandData = CommandData;
                State.Save(false);
            }
        }

        public void Run(UserConsole console)
        {
            if (CommandData.LastActionNumber == 0)
            {
                console.WriteLine($"Такой команды нет, используйте {CommandNames.Help} для списка команд");
                State.Save();
            }
        }

        public sealed class Data : State.CommandData
        {
            public override string Name { get; } = CommandNames.Unknown;
            public override void EncodeData(StringBuilder sb) { }
            public override void DecodeData(string[] data) { }
        }
    }
}