using System;
using System.Diagnostics;

using Raylib_cs;

namespace Calculator;

internal readonly struct Log
{
	/// <summary>The last message logged, used to not show the same message twice</summary>
	private static string OldMessage = "";

	/// <summary>The current message logged</summary>
	internal static string Message = "";

	/// <summary>Halts the program with the given message, does nothing in release builds</summary>
	[Conditional("DEBUG")]
	internal static void Halt(string message)
	{
		throw new InvalidOperationException(message);
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
	internal static void IfTrue(bool condition, string message)
	{
		if (condition)
		{
			Message += message;
		}
	}
}
