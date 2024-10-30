// Licensed under the GPL3 or later versions of the GPL license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Reflection;

namespace Calculator;

internal readonly struct EnumExtensions
{
	internal static void CycleEnum<T>(ref T enumValue)
		where T : Enum
	{
		T[] enumValues = (T[])Enum.GetValues(typeof(T));
		int currentIndex = Array.IndexOf(enumValues, enumValue);
		int nextIndex = (currentIndex + 1) % enumValues.Length;
		enumValue = enumValues[nextIndex];
	}
}
