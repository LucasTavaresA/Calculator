using System;
using System.Diagnostics;

using Raylib_cs;

namespace Calculator;

internal readonly struct Log
{
	/// <summary>Tolerable difference between colors</summary>
	private const int CONTRAST_LIMIT = 85;

	internal static string Message = "";

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

	[Conditional("DEBUG")]
	internal static void IfTrue(bool result, string message)
	{
		if (result)
		{
			Message += message;
		}
	}

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
