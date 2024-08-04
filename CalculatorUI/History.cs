using System;
using System.Collections.Generic;
using System.Linq;

#if LINUX
using System.IO;
#elif ANDROID || WINDOWS
using Xamarin.Essentials;
#endif

namespace Calculator;

// NOTE(LucasTA): no need for Path.Combine on Linux
internal readonly struct History
{
    internal static void Save(List<string> pinnedExpressions, List<string> expressionHistory)
    {
#if LINUX
        if (Environment.GetEnvironmentVariable("HOME") is string home)
        {
            Directory.CreateDirectory($"{home}/.cache");
            File.WriteAllText($"{home}/.cache/CalculatorHistory", string.Join(Environment.NewLine, pinnedExpressions.Concat(new[] { ";" }).Concat(expressionHistory)));
        }
#elif ANDROID || WINDOWS
        Preferences.Set("PinnedExpressions", string.Join(Environment.NewLine, pinnedExpressions));
        Preferences.Set("ExpressionHistory", string.Join(Environment.NewLine, expressionHistory));
#endif
    }

    internal static (List<string> pinnedExpressions, List<string> expressionHistory) Load()
    {
#if LINUX
        List<string> pinnedExpressions = new();
        List<string> expressionHistory = new();

        if (Environment.GetEnvironmentVariable("HOME") is string home &&
            File.Exists($"{home}/.cache/CalculatorHistory"))
        {
            bool pinned = true;

            foreach (string line in File.ReadAllLines($"{home}/.cache/CalculatorHistory"))
            {
                if (line == ";")
                {
                    pinned = false;
                    continue;
                }

                if (pinned)
                {
                    pinnedExpressions.Add(line);
                }
                else
                {
                    expressionHistory.Add(line);
                }
            }
        }

        return (pinnedExpressions, expressionHistory);
#elif ANDROID || WINDOWS
        return (
            new(Preferences.Get("PinnedExpressions", string.Empty).Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)),
            new(Preferences.Get("ExpressionHistory", string.Empty).Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        );
#endif
    }
}
