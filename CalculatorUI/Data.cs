// Licensed under the GPL3 or later versions of the GPL license.
// See the LICENSE file in the project root for more information.
using System;

namespace Calculator;

internal readonly struct Data
{
	internal static readonly string UserFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
#if WINDOWS
	internal static readonly string DataFolder = UserFolder + "/AppData/Local/Calculator/";
#elif LINUX || MACOS
	internal static readonly string DataFolder = UserFolder + "/.cache/";
#endif
}
