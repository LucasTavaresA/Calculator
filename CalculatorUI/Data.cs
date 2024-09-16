// Licensed under the GPL3 or later versions of the GPL license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
#if LINUX || MACOS || WINDOWS
using System.IO;
#elif ANDROID
using Xamarin.Essentials;
#endif

namespace Calculator;

internal readonly struct Data
{
	internal static readonly string UserFolder = Environment.GetFolderPath(
		Environment.SpecialFolder.UserProfile
	);
#if WINDOWS
	internal static readonly string DataFolder = UserFolder + "/AppData/Local/Calculator/";
#elif LINUX || MACOS
	internal static readonly string DataFolder = UserFolder + "/.cache/";
#endif

	internal static void SaveList(List<string> list, string fileName)
	{
#if LINUX || MACOS || WINDOWS
		Directory.CreateDirectory(Data.DataFolder);
		File.WriteAllText(Data.DataFolder + fileName, string.Join(Environment.NewLine, list));
#elif ANDROID
		Preferences.Set(fileName, string.Join(Environment.NewLine, list));
#endif
	}

	internal static List<string> LoadList(string fileName)
	{
		List<string> list = new();

#if LINUX || MACOS || WINDOWS
		string filePath = Data.DataFolder + fileName;

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
#elif ANDROID
		list = new(
			Preferences
				.Get(fileName, string.Empty)
				.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
		);
#endif

		return list;
	}
}
