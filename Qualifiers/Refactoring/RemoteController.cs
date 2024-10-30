using System.Collections.Generic;
using System.Text;

namespace Refactoring
{
    public static class CommandNames
    {
        public const string TvOn = "Tv On";
        public const string TvOff = "Tv Off";
        public const string VolumeUp = "Volume Up";
        public const string VolumeDown = "Volume Down";
        public const string OptionsChange = "Options change";
        public const string OptionsShow = "Options show";
    }

    public static class OptionsChangeCommandNames
    {
        public const string BrightnessUp = "brightness up";
        public const string BrightnessDown = "brightness down";
        public const string ContrastUp = "contrast up";
        public const string ContrastDown = "contrast down";
    }

    public static class OptionNames
    {
        public const string IsOnline = "IsOnline";
        public const string Volume = "Volume";
        public const string Brightness = "Brightness";
        public const string Contrast = "Contrast";
    }

    public static class OptionValues
    {
        public const int Default = 30;
        public const int Min = 0;
        public const int Max = 100;
        public const int Interval = 1;
        public const int TVOff = 0;
        public const int TVOn = 1;
    }

    public class RemoteController
    {
        private readonly Dictionary<string, ICommand> commands;
        private readonly Dictionary<string, int> options;

        public RemoteController()
        {
            commands = new Dictionary<string, ICommand>();
            options = new Dictionary<string, int>();
            InitCommands();
            InitOptionsChangeCommands();
            InitOptions();
        }

        private void InitCommands()
        {
            commands.Add(CommandNames.TvOn, new TVOnCommand());
            commands.Add(CommandNames.TvOff, new TVOffCommand());
            commands.Add(CommandNames.VolumeUp, new VolumeUpCommand());
            commands.Add(CommandNames.VolumeDown, new VolumeDownCommand());
            commands.Add(CommandNames.OptionsShow, new OptionsShowCommand());
        }

        private void InitOptionsChangeCommands()
        {
            commands.Add(OptionsChangeCommandNames.BrightnessUp, new BrightnessUpCommand());
            commands.Add(OptionsChangeCommandNames.BrightnessDown, new BrightnessDownCommand());
            commands.Add(OptionsChangeCommandNames.ContrastUp, new ContrastUpCommand());
            commands.Add(OptionsChangeCommandNames.ContrastDown, new ContrastDownCommand());
        }

        private void InitOptions()
        {
            options.Add(OptionNames.Volume, OptionValues.Default);
            options.Add(OptionNames.IsOnline, OptionValues.TVOff);
            options.Add(OptionNames.Brightness, OptionValues.Default);
            options.Add(OptionNames.Contrast, OptionValues.Default);
        }

        public string Call(string command)
        {
            if (command.StartsWith(CommandNames.OptionsChange))
            {
                var subCommand = command.Substring(CommandNames.OptionsChange.Length + 1);
                return commands[subCommand].Execute(options);
            }
            else
                return commands[command].Execute(options);
        }
    }

    public interface ICommand
    {
        string Name { get; }
        string Execute(IDictionary<string, int> options);
    }

    public class TVOnCommand : ICommand
    {
        public string Name { get; } = CommandNames.TvOn;

        public string Execute(IDictionary<string, int> options)
        {
            options[OptionNames.IsOnline] = OptionValues.TVOn;
            return "";
        }
    }

    public class TVOffCommand : ICommand
    {
        public string Name { get; } = CommandNames.TvOff;

        public string Execute(IDictionary<string, int> options)
        {
            options[OptionNames.IsOnline] = OptionValues.TVOff;
            return "";
        }
    }

    public class VolumeUpCommand : ICommand
    {
        public string Name { get; } = CommandNames.VolumeUp;

        public string Execute(IDictionary<string, int> options)
        {
            if (options[OptionNames.Volume] < OptionValues.Max)
                options[OptionNames.Volume] += OptionValues.Interval;

            return "";
        }
    }

    public class VolumeDownCommand : ICommand
    {
        public string Name { get; } = CommandNames.VolumeDown;

        public string Execute(IDictionary<string, int> options)
        {
            if (options[OptionNames.Volume] > OptionValues.Min)
                options[OptionNames.Volume] -= OptionValues.Interval;

            return "";
        }
    }

    public class OptionsShowCommand : ICommand
    {
        public string Name { get; } = CommandNames.OptionsShow;

        public string Execute(IDictionary<string, int> options)
        {
            var sb = new StringBuilder("Options:", options.Count * 17);

            foreach (var option in options)
            {
                if (option.Key == OptionNames.IsOnline)
                {
                    sb.AppendLine($"{option.Key} {option.Value != OptionValues.TVOff}");
                    continue;
                }

                sb.AppendLine($"{option.Key} {option.Value}");
            }

            return sb.ToString();
        }
    }

    public class BrightnessUpCommand : ICommand
    {
        public string Name { get; } = OptionsChangeCommandNames.BrightnessUp;

        public string Execute(IDictionary<string, int> options)
        {
            if (options[OptionNames.Brightness] < OptionValues.Max)
                options[OptionNames.Brightness] += OptionValues.Interval;

            return "";
        }
    }

    public class BrightnessDownCommand : ICommand
    {
        public string Name { get; } = OptionsChangeCommandNames.BrightnessDown;

        public string Execute(IDictionary<string, int> options)
        {
            if (options[OptionNames.Brightness] > OptionValues.Min)
                options[OptionNames.Brightness] -= OptionValues.Interval;

            return "";
        }
    }

    public class ContrastUpCommand : ICommand
    {
        public string Name { get; } = OptionsChangeCommandNames.ContrastUp;

        public string Execute(IDictionary<string, int> options)
        {
            if (options[OptionNames.Contrast] < OptionValues.Max)
                options[OptionNames.Contrast] += OptionValues.Interval;

            return "";
        }
    }

    public class ContrastDownCommand : ICommand
    {
        public string Name { get; } = OptionsChangeCommandNames.ContrastDown;

        public string Execute(IDictionary<string, int> options)
        {
            if (options[OptionNames.Contrast] > OptionValues.Min)
                options[OptionNames.Contrast] -= OptionValues.Interval;

            return "";
        }
    }
}