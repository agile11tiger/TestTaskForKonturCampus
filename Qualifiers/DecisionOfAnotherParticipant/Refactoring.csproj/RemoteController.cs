using System.Text;

namespace Refactoring
{
    public class RemoteController
    {
        private class DeviceSettings
        {
            public bool isOnline = false;
            public int volume = 20;
            public int brightness = 20;
            public int contrast = 20;
        };
        private readonly DeviceSettings _deviceSettings = new DeviceSettings();

        public string Call(string command)
        {
            string subCommand = null;
            if (command.StartsWith("Options change"))
            {
                subCommand = command.Substring(14).Trim();
                command = "Options change";
            }

            switch (command)
            {
                case "Tv On":
                    _deviceSettings.isOnline = true;
                    break;
                case "Tv Off":
                    _deviceSettings.isOnline = false;
                    break;
                case "Volume Up":
                    _deviceSettings.volume += 10;
                    break;
                case "Volume Down":
                    _deviceSettings.volume -= 10;
                    break;
                case "Options change":
                    OptionsSwitch(subCommand);
                    break;
                case "Options show":
                    return OptionsShow();
            }

            return "";
        }

        private void OptionsSwitch(string command)
        {
            switch (command)
            {
                case "brightness up":
                    _deviceSettings.brightness += 10;
                    break;
                case "brightness down":
                    _deviceSettings.brightness -= 10;
                    break;
                case "contrast up":
                    _deviceSettings.contrast += 10;
                    break;
                case "contrast down":
                    _deviceSettings.contrast -= 10;
                    break;
            }
        }

        private string OptionsShow()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Options:");
            builder.AppendLine($"Volume {_deviceSettings.volume}");
            builder.AppendLine($"IsOnline {_deviceSettings.isOnline}");
            builder.AppendLine($"Brightness {_deviceSettings.brightness}");
            builder.AppendLine($"Contrast {_deviceSettings.contrast}");
            return builder.ToString();
        }
    }
}