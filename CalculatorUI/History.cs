using System;

#if LINUX
using System.IO;
#elif ANDROID
using Xamarin.Essentials;
#endif

namespace Calculator;

// NOTE(LucasTA): no need for Path.Combine on Linux
internal readonly struct History
{
    internal static void Save(string data)
    {
#if LINUX
        if (Environment.GetEnvironmentVariable("HOME") is string home)
        {
            Directory.CreateDirectory($"{home}/.cache");
            File.WriteAllText($"{home}/.cache/CalculatorHistory", data);
        }
#elif ANDROID || WINDOWS
        Preferences.Set("History", string.Join(Environment.NewLine, data));
#endif
    }

    internal static string[] Load()
    {
#if LINUX
        if (Environment.GetEnvironmentVariable("HOME") is string home &&
            File.Exists($"{home}/.cache/CalculatorHistory"))
        {
            return File.ReadAllLines($"{home}/.cache/CalculatorHistory");
        }
        else
        {
            return Array.Empty<string>();
        }
#elif ANDROID || WINDOWS
        return Preferences.Get("History", string.Empty).Split(Environment.NewLine, StringSplitOptions.None);
#endif
    }
}
