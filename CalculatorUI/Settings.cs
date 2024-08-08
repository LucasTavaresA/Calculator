#if LINUX
using System;
using System.IO;
#elif ANDROID || WINDOWS
using Xamarin.Essentials;
#endif

namespace Calculator;

// NOTE(LucasTA): no need for Path.Combine on Linux
internal readonly struct Settings
{
	private const string SettingsFileName = ".cache/CalculatorSettings";
	internal static bool BookmarkOnEval = true;

	internal static void Save()
	{
#if LINUX
		if (Environment.GetEnvironmentVariable("HOME") is string home)
		{
			Directory.CreateDirectory($"{home}/.cache");
			File.WriteAllText($"{home}/{SettingsFileName}", $"BookmarkOnEval={BookmarkOnEval}");
		}
#elif ANDROID || WINDOWS
		Preferences.Set("BookmarkOnEval", BookmarkOnEval.ToString());
#endif
	}

	internal static void Load()
	{
#if LINUX
		if (
			Environment.GetEnvironmentVariable("HOME") is string home
			&& File.Exists($"{home}/{SettingsFileName}")
		)
		{
			foreach (string line in File.ReadAllLines($"{home}/{SettingsFileName}"))
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
#elif ANDROID || WINDOWS
		BookmarkOnEval = Preferences.Get("BookmarkOnEval", bool.TrueString) == bool.TrueString;
#endif
	}
}
