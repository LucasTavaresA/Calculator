using System;
using System.Collections.Generic;
#if LINUX || MACOS || WINDOWS
using System.IO;
using System.Linq;
#elif ANDROID
using Xamarin.Essentials;
#endif

namespace Calculator;

internal readonly struct History
{
#if !ANDROID
	private static readonly string HistoryFilePath = Data.DataFolder + "CalculatorHistory";
#endif
	internal static List<string> ExpressionHistory;
	internal static List<string> PinnedExpressions;

	internal static void Add(string expression)
	{
		if (string.IsNullOrWhiteSpace(expression))
		{
			return;
		}

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

	internal static void Clear()
	{
		ExpressionHistory.Clear();
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
#if LINUX || MACOS || WINDOWS
		Directory.CreateDirectory(Data.DataFolder);
		File.WriteAllText(
			HistoryFilePath,
			string.Join(
				Environment.NewLine,
				PinnedExpressions.Concat(new[] { ";" }).Concat(ExpressionHistory)
			)
		);
#elif ANDROID
		Preferences.Set("PinnedExpressions", string.Join(Environment.NewLine, PinnedExpressions));
		Preferences.Set("ExpressionHistory", string.Join(Environment.NewLine, ExpressionHistory));
#endif
	}

	internal static void Load()
	{
#if LINUX || MACOS || WINDOWS
		PinnedExpressions = new();
		ExpressionHistory = new();

		if (File.Exists(HistoryFilePath))
		{
			bool isPinned = true;

			foreach (string line in File.ReadAllLines(HistoryFilePath))
			{
				if (string.IsNullOrWhiteSpace(line))
				{
					continue;
				}
				else if (line == ";")
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
#elif ANDROID
		PinnedExpressions = new(
			Preferences
				.Get("PinnedExpressions", string.Empty)
				.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
		);
		ExpressionHistory = new(
			Preferences
				.Get("ExpressionHistory", string.Empty)
				.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
		);
#endif
	}
}
