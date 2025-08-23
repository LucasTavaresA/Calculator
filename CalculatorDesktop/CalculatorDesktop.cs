// Licensed under the GPL3 or later versions of the GPL license.
// See the LICENSE file in the project root for more information.

namespace Calculator;

internal struct CalculatorDesktop
{
#if WINDOWS
	// NOTE(LucasTA): For clipboard on windows
	[System.STAThread]
#endif
	internal static void Main()
	{
		Calculator.MainLoop();
	}
}
