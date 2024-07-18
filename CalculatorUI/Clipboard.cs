using System;

#if LINUX
using System.Diagnostics;
#elif ANDROID
using Plugin.Clipboard;
#endif

namespace Calculator;

internal readonly struct Clipboard
{
    internal static void Set(string text)
    {
#if LINUX
        string processName;
        string args;

        if (Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") != null)
        {
            processName = "wl-copy";
            args = string.Empty;
        }
        else
        {
            processName = "xsel";
            args = "--clipboard --input";
        }

        try
        {
            Process process = new()
            {
                StartInfo = new()
                {
                    FileName = processName,
                    Arguments = args,
                    RedirectStandardInput = true,
                    UseShellExecute = false
                }
            };
            process.Start();
            process.StandardInput.Write(text);
            process.StandardInput.Close();
            process.WaitForExit();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to set text to clipboard: {e.Message}");
        }
#elif ANDROID || WINDOWS
        CrossClipboard.Current.SetText(text);
#endif
    }

    internal static string Get()
    {
#if LINUX
        string processName;
        string args;
        string output = string.Empty;

        if (Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") != null)
        {
            processName = "wl-paste";
            args = string.Empty;
        }
        else
        {
            processName = "xsel";
            args = "--clipboard --output";
        }

        try
        {
            Process process = new()
            {
                StartInfo = new()
                {
                    FileName = processName,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to get text from clipboard: {e.Message}");
        }

        return output;
#elif ANDROID || WINDOWS
        return CrossClipboard.Current.GetTextAsync().Result;
#endif
    }
}
