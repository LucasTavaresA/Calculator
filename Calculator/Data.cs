// Licensed under the GPL3 or later versions of the GPL license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;

namespace Calculator;

internal readonly struct Data
{
#if WINDOWS || LINUX || MACOS
	internal static readonly string UserFolder = Environment.GetFolderPath(
		Environment.SpecialFolder.UserProfile
	);
#endif

#if WINDOWS
	internal static readonly string DataFolder = UserFolder + "/AppData/Local/Calculator/";
#elif LINUX || MACOS
	internal static readonly string DataFolder = UserFolder + "/.local/share/Calculator/";
#elif ANDROID
	internal static readonly string DataFolder = Calculator.Context.FilesDir.AbsolutePath;
#endif

	internal static void SaveList(List<string> list, string fileName)
	{
		Directory.CreateDirectory(DataFolder);
		File.WriteAllText(DataFolder + fileName, string.Join(Environment.NewLine, list));
	}

	internal static List<string> LoadList(string fileName)
	{
		string filePath = DataFolder + fileName;
		List<string> list = [];

		if (File.Exists(filePath))
		{
			foreach (string line in File.ReadAllLines(filePath))
			{
				if (!string.IsNullOrWhiteSpace(line))
				{
					list.Add(line);
				}
			}
		}

		return list;
	}

	internal static void SaveString(string str, string fileName)
	{
		Directory.CreateDirectory(DataFolder);
		File.WriteAllText(DataFolder + fileName, str);
	}
}
