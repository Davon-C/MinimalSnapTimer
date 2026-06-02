using MinimalSnapTimer.Models;

namespace MinimalSnapTimer.Services;

public sealed class CommandLineService
{
    public CommandLineOptions Parse(string[] args)
    {
        var options = new CommandLineOptions();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (i == 0 && !arg.StartsWith('-') && TryParseDuration(arg, out var positionalDuration))
            {
                options.Duration = positionalDuration;
                continue;
            }

            switch (arg.ToLowerInvariant())
            {
                case "--minutes":
                    if (!TryReadValue(args, ref i, out var minutesValue) || !int.TryParse(minutesValue, out var minutes))
                    {
                        return Invalid(options, "参数 --minutes 需要提供整数分钟数。");
                    }

                    options.Duration = TimeSpan.FromMinutes(minutes);
                    break;

                case "--preset":
                    if (!TryReadValue(args, ref i, out var presetName))
                    {
                        return Invalid(options, "参数 --preset 需要提供预设名称。");
                    }

                    options.PresetName = presetName;
                    break;

                case "--mode":
                    if (!TryReadValue(args, ref i, out var modeValue))
                    {
                        return Invalid(options, "参数 --mode 需要提供 sit 或 stand。");
                    }

                    options.Mode = modeValue.ToLowerInvariant() switch
                    {
                        "sit" => WorkflowStage.Sit,
                        "stand" => WorkflowStage.Stand,
                        _ => null
                    };

                    if (options.Mode is null)
                    {
                        return Invalid(options, "参数 --mode 仅支持 sit 或 stand。");
                    }

                    break;

                case "--start":
                    options.AutoStart = true;
                    break;

                case "--paused":
                    options.StartPaused = true;
                    break;

                case "--compact":
                case "--pure":
                    options.StartInPureMode = true;
                    break;

                case "--always-on-top":
                    options.AlwaysOnTop = true;
                    break;

                case "--minimize-to-tray":
                    options.MinimizeToTray = true;
                    break;

                case "--sound":
                    if (!TryReadValue(args, ref i, out var soundMode))
                    {
                        return Invalid(options, "参数 --sound 需要提供 on 或 off。");
                    }

                    options.SoundOff = soundMode.Equals("off", StringComparison.OrdinalIgnoreCase);
                    break;

                case "--auto-restart":
                    options.AutoRestart = true;
                    break;

                case "--help":
                case "-h":
                case "/?":
                    options.ShowHelp = true;
                    break;

                default:
                    if (TryParseDuration(arg, out var directDuration))
                    {
                        options.Duration = directDuration;
                    }
                    else
                    {
                        return Invalid(options, $"无法识别参数：{arg}");
                    }

                    break;
            }
        }

        return options;
    }

    public string GetHelpText()
    {
        return """
MinimalSnapTimer 命令行示例
MinimalSnapTimer.exe 40
MinimalSnapTimer.exe 00:40:00
MinimalSnapTimer.exe --minutes 40
MinimalSnapTimer.exe --preset "坐着工作"
MinimalSnapTimer.exe --mode sit
MinimalSnapTimer.exe --mode stand
MinimalSnapTimer.exe --start
MinimalSnapTimer.exe --paused
MinimalSnapTimer.exe --pure
MinimalSnapTimer.exe --always-on-top
MinimalSnapTimer.exe --minimize-to-tray
MinimalSnapTimer.exe --sound off
MinimalSnapTimer.exe --auto-restart
MinimalSnapTimer.exe --help

说明：--mode sit 表示开始坐着工作
--mode stand 表示开始站立活动
--pure 表示启动后直接进入纯时间模式
""";
    }

    private static bool TryReadValue(string[] args, ref int index, out string value)
    {
        if (index + 1 >= args.Length)
        {
            value = string.Empty;
            return false;
        }

        value = args[++index];
        return true;
    }

    private static bool TryParseDuration(string input, out TimeSpan duration)
    {
        if (int.TryParse(input, out var minutes))
        {
            duration = TimeSpan.FromMinutes(minutes);
            return true;
        }

        return TimeSpan.TryParse(input, out duration);
    }

    private static CommandLineOptions Invalid(CommandLineOptions options, string message)
    {
        options.IsValid = false;
        options.ValidationError = message;
        return options;
    }
}
