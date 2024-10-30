// Licensed under the GPL3 or later versions of the GPL license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Raylib_cs;

namespace Calculator;

internal readonly struct Debug
{
	/// <summary>The last message logged, used to not show the same message twice</summary>
	private static string OldMessage = "";

	/// <summary>The current message logged</summary>
	internal static string Message = "";

	/// <summary>Ignores exceptions in async functions when not in Debug mode</summary>
	internal static async void IgnoreAsync(Func<Task> func)
	{
#if DEBUG
		await func();
#else
		try
		{
			await func();
		}
		catch (Exception e)
		{
			Message += e.Message;
		}
#endif
	}

	/// <summary>Ignores exceptions when not in Debug mode</summary>
	internal static void Ignore(Action action)
	{
#if DEBUG
		action();
#else
		try
		{
			action();
		}
		catch (Exception e)
		{
			Message += e.Message;
		}
#endif
	}

	/// <summary>Halts the program with the given message, does nothing in release builds</summary>
	[Conditional("DEBUG")]
	internal static void Halt(string message)
	{
		Console.WriteLine(message);
		Environment.Exit(1);
	}

	/// <summary>Halts the program if **condition** fails, does nothing in release builds</summary>
	[Conditional("DEBUG")]
	internal static void Assert(bool condition, string message)
	{
		if (!condition)
		{
			Halt(message);
		}
	}

	/// <summary>Prints the current message if it has changed since the last time it was printed</summary>
	[Conditional("DEBUG")]
	internal static void Print()
	{
		if (OldMessage != Message)
		{
			Console.WriteLine(Message);
			OldMessage = Message;
		}
	}

	/// <summary>Draw the current logged message using raylib</summary>
	[Conditional("DEBUG")]
	internal static void Draw()
	{
		Raylib.DrawTextEx(
			CalculatorUI.Fonte,
			Message,
			new(0, 0),
			CalculatorUI.FontSize,
			CalculatorUI.FONT_SPACING,
			Color.RED
		);
	}

	/// <summary>Appends the given **message** to the logged message if **condition** is true</summary>
	[Conditional("DEBUG")]
	internal static void If(bool condition, string message)
	{
		if (condition)
		{
			Message += message;
		}
	}

	/// <summary>Appends the given **message** to the logged message if **condition** is true and draws circle on the error location</summary>
	[Conditional("DEBUG")]
	internal static void IfDrawPoint(bool condition, string message, int x, int y)
	{
		if (condition)
		{
			Message += message;

			Raylib.DrawCircle(x, y, 5, Color.RED);
		}
	}

	/// <summary>Appends the given **message** to the logged message if **condition** is true and draws a border on the error location</summary>
	[Conditional("DEBUG")]
	internal static void IfDrawBorder(
		bool condition,
		string message,
		int x,
		int y,
		int width,
		int height
	)
	{
		if (condition)
		{
			Message += message;

			Raylib.DrawRectangleLinesEx(new(x, y, width, height), 5, Color.RED);
		}
	}
}
