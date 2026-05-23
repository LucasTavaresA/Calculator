// Licensed under the GPL3 or later versions of the GPL license.
// See the LICENSE file in the project root for more information.

#if LINUX || MACOS
using System;
using System.Diagnostics;
#elif ANDROID
using Android.Content;
#elif WINDOWS
using System;
using System.Runtime.InteropServices;
#endif

namespace Calculator;

internal readonly partial struct Clipboard
{
#if WINDOWS
	[LibraryImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool OpenClipboard(IntPtr hWndNewOwner);

	[LibraryImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool CloseClipboard();

	[LibraryImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool EmptyClipboard();

	[LibraryImport("user32.dll", SetLastError = true)]
	private static partial IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

	[LibraryImport("kernel32.dll", SetLastError = true)]
	private static partial IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

	[LibraryImport("kernel32.dll", SetLastError = true)]
	private static partial IntPtr GlobalLock(IntPtr hMem);

	[LibraryImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool GlobalUnlock(IntPtr hMem);

	[LibraryImport("user32.dll", SetLastError = true)]
	private static partial IntPtr GetClipboardData(uint uFormat);

	private const uint CF_UNICODETEXT = 13;
	private const uint GMEM_MOVEABLE = 0x0002;
#endif

	internal static void Set(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

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
		if (!OpenClipboard(IntPtr.Zero))
		{
			return;
		}

		EmptyClipboard();

		int bytes = (text.Length + 1) * 2;
		IntPtr hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)bytes);
		if (hGlobal == IntPtr.Zero)
		{
			CloseClipboard();
			return;
		}

		IntPtr target = GlobalLock(hGlobal);
		if (target == IntPtr.Zero)
		{
			CloseClipboard();
			return;
		}

		Marshal.Copy(text.ToCharArray(), 0, target, text.Length);
		Marshal.WriteInt16(target, text.Length * 2, 0);
		GlobalUnlock(hGlobal);

		SetClipboardData(CF_UNICODETEXT, hGlobal);
		CloseClipboard();
#elif ANDROID
		ClipboardManager? clipboard =
			Calculator.Context.GetSystemService(Context.ClipboardService) as ClipboardManager;

		if (clipboard is null)
			return;

		clipboard.PrimaryClip = ClipData.NewPlainText("Calculator", text);
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
		if (!OpenClipboard(IntPtr.Zero))
		{
			return string.Empty;
		}

		IntPtr handle = GetClipboardData(CF_UNICODETEXT);
		if (handle == IntPtr.Zero)
		{
			CloseClipboard();
			return string.Empty;
		}

		IntPtr pointer = GlobalLock(handle);
		if (pointer == IntPtr.Zero)
		{
			CloseClipboard();
			return string.Empty;
		}

		string? result = Marshal.PtrToStringUni(pointer);
		GlobalUnlock(handle);
		CloseClipboard();

		return result ?? string.Empty;
#elif ANDROID
		ClipboardManager? clipboard =
			Calculator.Context.GetSystemService(Context.ClipboardService) as ClipboardManager;

		if (clipboard is null || !clipboard.HasPrimaryClip || clipboard.PrimaryClip is null || clipboard.PrimaryClip.ItemCount == 0)
		{
				return string.Empty;
		}

		string? text = clipboard.PrimaryClip.GetItemAt(0)?.CoerceToText(Calculator.Context);
		return text?.ToString() ?? string.Empty;
#endif
	}
}
