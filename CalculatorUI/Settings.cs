using System;
#if LINUX || MACOS || WINDOWS
using System.IO;
#elif ANDROID
using Xamarin.Essentials;
#endif

namespace Calculator;

internal readonly struct Settings
{
#if !ANDROID
	private static readonly string SettingsFilePath = Data.DataFolder + "CalculatorSettings";
#endif
	internal static bool BookmarkOnEval = true;

	internal static void Save()
	{
#if LINUX || MACOS || WINDOWS
		Directory.CreateDirectory(Data.DataFolder);
		File.WriteAllText(SettingsFilePath, $"BookmarkOnEval={BookmarkOnEval}");
#elif ANDROID
		Preferences.Set("BookmarkOnEval", BookmarkOnEval.ToString());
#endif
	}

	internal static void Load()
	{
#if LINUX || MACOS || WINDOWS
		string filePath = Data.DataFolder + "CalculatorSettings";
		string settings = "";

		if (File.Exists(filePath))
		{
			settings = File.ReadAllText(filePath);
		}
#elif ANDROID
		string settings = Preferences.Get("CalculatorSettings", string.Empty);
#endif

		foreach (
			string setting in settings.Split(
				Environment.NewLine,
				StringSplitOptions.RemoveEmptyEntries
			)
		)
		{
			string[] split = setting.Split('=');

			if (split.Length != 2 || split[0] == string.Empty || split[1] == string.Empty)
			{
				continue;
			}

			switch (split[0].Trim())
			{
				case "BookmarkOnEval":
					if (bool.TryParse(split[1].Trim(), out bool b))
					{
						BookmarkOnEval = b;
					}
					else
					{
						BookmarkOnEval = true;
					}
					break;
			}
		}
	}
}
