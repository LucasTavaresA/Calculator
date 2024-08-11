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
		if (File.Exists(SettingsFilePath))
		{
			foreach (string line in File.ReadAllLines(SettingsFilePath))
			{
				if (string.IsNullOrWhiteSpace(line))
				{
					continue;
				}
				else if (line.StartsWith("BookmarkOnEval="))
				{
					string value = line.Split('=')[1];

					if (bool.TryParse(value, out bool b))
					{
						BookmarkOnEval = b;
					}
					else
					{
						throw new InvalidDataException(
							"Invalid settings file! Wrong value for BookmarkOnEval, it can only be true or false."
						);
					}
				}
				else
				{
					throw new InvalidDataException(
						$"Invalid settings file! '{line}' is an invalid setting."
					);
				}
			}
		}
#elif ANDROID
		BookmarkOnEval = Preferences.Get("BookmarkOnEval", bool.TrueString) == bool.TrueString;
#endif
	}
}
