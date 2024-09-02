using System;
using System.Diagnostics;

using Raylib_cs;

namespace Calculator;

internal readonly struct Log
{
	/// <summary>Tolerable difference between colors</summary>
	private const int CONTRAST_LIMIT = 85;

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

	/// <summary>Appends the given **message** to the logged message if the contrast between the background and text colors is too low</summary>
	[Conditional("DEBUG")]
	internal static void IfBadContrast(Color backgroundColor, Color textColor, string message)
	{
		int rDiff = Math.Abs(backgroundColor.R - textColor.R);
		int gDiff = Math.Abs(backgroundColor.G - textColor.G);
		int bDiff = Math.Abs(backgroundColor.B - textColor.B);

		if ((rDiff + gDiff + bDiff) / 3 < CONTRAST_LIMIT)
		{
			Message += message;
		}
	}
}
