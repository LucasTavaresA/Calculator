using System;
using System.Collections.Generic;

#if LINUX
using System.IO;
using System.Linq;
#elif ANDROID || WINDOWS
using Xamarin.Essentials;
#endif

namespace Calculator;

// NOTE(LucasTA): no need for Path.Combine on Linux
internal readonly struct History
{
    internal static List<string> ExpressionHistory;
    internal static List<string> PinnedExpressions;

    internal static void Add(string expression)
    {
        if (PinnedExpressions.Contains(expression))
        {
            PinnedExpressions.Remove(expression);
            PinnedExpressions.Insert(0, expression);
        }
        else
        {
            ExpressionHistory.Remove(expression);
            ExpressionHistory.Insert(0, expression);
        }

        Save();
    }

    internal static void Remove(string expression)
    {
        PinnedExpressions.Remove(expression);
        ExpressionHistory.Remove(expression);
        Save();
    }

    internal static void Pin(string expression)
    {
        ExpressionHistory.Remove(expression);
        PinnedExpressions.Insert(0, expression);
        Save();
    }

    internal static void Unpin(string expression)
    {
        PinnedExpressions.Remove(expression);
        ExpressionHistory.Insert(0, expression);
        Save();
    }

    internal static void Save()
    {
#if LINUX
        if (Environment.GetEnvironmentVariable("HOME") is string home)
        {
            Directory.CreateDirectory($"{home}/.cache");
            File.WriteAllText($"{home}/.cache/CalculatorHistory", string.Join(Environment.NewLine, PinnedExpressions.Concat(new[] { ";" }).Concat(ExpressionHistory)));
        }
#elif ANDROID || WINDOWS
        Preferences.Set("PinnedExpressions", string.Join(Environment.NewLine, PinnedExpressions));
        Preferences.Set("ExpressionHistory", string.Join(Environment.NewLine, ExpressionHistory));
#endif
    }

    internal static void Load()
    {
#if LINUX
        PinnedExpressions = new();
        ExpressionHistory = new();

        if (Environment.GetEnvironmentVariable("HOME") is string home &&
            File.Exists($"{home}/.cache/CalculatorHistory"))
        {
            bool isPinned = true;

            foreach (string line in File.ReadAllLines($"{home}/.cache/CalculatorHistory"))
            {
                if (line == ";")
                {
                    isPinned = false;
                }
                else if (isPinned)
                {
                    PinnedExpressions.Remove(line);
                    PinnedExpressions.Add(line);
                }
                else
                {
                    ExpressionHistory.Remove(line);
                    ExpressionHistory.Add(line);
                }
            }
        }
#elif ANDROID || WINDOWS
        PinnedExpressions = new(Preferences.Get("PinnedExpressions", string.Empty)
                            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
        ExpressionHistory = new(Preferences.Get("ExpressionHistory", string.Empty)
                            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
#endif
    }
}
