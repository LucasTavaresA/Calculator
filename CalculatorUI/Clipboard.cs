// Licensed under the GPL3 or later versions of the GPL license.
// See the LICENSE file in the project root for more information.

using System;
#if LINUX || MACOS
using System.Diagnostics;
#elif ANDROID
using Plugin.Clipboard;
#elif WINDOWS
using System.Windows.Forms;
#endif

namespace Calculator;

internal readonly struct Clipboard
{
	internal static void Set(string text)
	{
#if LINUX || MACOS
		string processName;
		string args;

#if LINUX
		if (Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") != null)
		{
			processName = "wl-copy";
			args = string.Empty;
		}
		else
		{
			processName = "xsel";
			args = "--clipboard --input";
		}
#elif MACOS
		processName = "pbcopy";
		args = string.Empty;
#endif

		Debug.Ignore(() =>
		{
			Process process =
				new()
				{
					StartInfo = new()
					{
						FileName = processName,
						Arguments = args,
						RedirectStandardInput = true,
						UseShellExecute = false,
					},
				};
			process.Start();
			process.StandardInput.Write(text);
			process.StandardInput.Close();
			process.WaitForExit();
		});
#elif WINDOWS
		// NOTE(LucasTA): Why SetText fucking crashes on ""? WTF!
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		System.Windows.Forms.Clipboard.SetText(text);
#elif ANDROID
		CrossClipboard.Current.SetText(text);
#endif
	}

	internal static string Get()
	{
#if LINUX || MACOS
		string processName;
		string args;
		string output = string.Empty;

#if LINUX
		if (Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") != null)
		{
			processName = "wl-paste";
			args = string.Empty;
		}
		else
		{
			processName = "xsel";
			args = "--clipboard --output";
		}
#elif MACOS
		processName = "pbpaste";
		args = string.Empty;
#endif

		Debug.Ignore(() =>
		{
			Process process =
				new()
				{
					StartInfo = new()
					{
						FileName = processName,
						Arguments = args,
						RedirectStandardOutput = true,
						UseShellExecute = false,
						CreateNoWindow = true,
					},
				};
			process.Start();
			output = process.StandardOutput.ReadToEnd().Trim();
			process.WaitForExit();
		});

		return output;
#elif WINDOWS
		return System.Windows.Forms.Clipboard.GetText();
#elif ANDROID
		return CrossClipboard.Current.GetTextAsync().Result;
#endif
	}
}
