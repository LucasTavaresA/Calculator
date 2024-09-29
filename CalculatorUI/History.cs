// Licensed under the GPL3 or later versions of the GPL license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Calculator;

internal readonly struct History
{
	internal static List<string> ExpressionHistory = Data.LoadList("ExpressionHistory");
	internal static List<string> PinnedExpressions = Data.LoadList("PinnedExpressions");

	internal static void Save()
	{
		Data.SaveList(ExpressionHistory, "ExpressionHistory");
		Data.SaveList(PinnedExpressions, "PinnedExpressions");
	}

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
}
