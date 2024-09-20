// Licensed under the GPL3 or later versions of the GPL license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Calculator;

internal readonly struct Settings
{
	private static readonly string SettingsFilePath = Data.DataFolder + "CalculatorSettings";
	internal static bool BookmarkOnEval = true;
	internal static DateTime LastAPICallTime = DateTime.MinValue.Date;

	internal static void Save()
	{
		Directory.CreateDirectory(Data.DataFolder);
		File.WriteAllText(
			SettingsFilePath,
			string.Join(
				Environment.NewLine,
				$"BookmarkOnEval={BookmarkOnEval}",
				$"LastAPICallTime={LastAPICallTime}"
			)
		);
	}

	internal static void Load()
	{
		string settings = "";

		if (File.Exists(SettingsFilePath))
		{
			settings = File.ReadAllText(SettingsFilePath);
		}

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
				case "LastAPICallTime":
					if (DateTime.TryParse(split[1].Trim(), out DateTime dt))
					{
						LastAPICallTime = dt;
					}
					else
					{
						LastAPICallTime = DateTime.MinValue.Date;
					}
					break;
			}
		}
	}
}
