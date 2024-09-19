// Licensed under the GPL3 or later versions of the GPL license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using Raylib_cs;

namespace Calculator;

internal readonly struct Resource
{
	internal static readonly Assembly Assembly = Assembly.GetExecutingAssembly();
	internal static readonly Dictionary<string, Texture2D> Resources = new();

	internal static Texture2D GetResource(string resource)
	{
		if (Resources.TryGetValue(resource, out Texture2D texture))
		{
			return texture;
		}

		Log.Halt($"Resource '{resource}' not found");
		return Resources["not_found.png"];
	}

	internal static string LoadStringFromAssembly(string resource)
	{
		using Stream s = Assembly.GetManifestResourceStream(resource);
		using StreamReader sr = new(s);
		return sr.ReadToEnd();
	}

	[DllImport("raylib", CallingConvention = CallingConvention.Cdecl)]
	static extern Image LoadImageFromMemory(string fileType, byte[] fileData, int dataSize);

	internal static Texture2D LoadTextureFromAssembly(string resource)
	{
		Texture2D texture;

		using (Stream stream = Assembly.GetManifestResourceStream(resource)!)
		{
			using (MemoryStream memoryStream = new MemoryStream())
			{
				stream.CopyTo(memoryStream);

				Image image = LoadImageFromMemory(
					".png",
					memoryStream.ToArray(),
					(int)memoryStream.Length
				);
				texture = Raylib.LoadTextureFromImage(image);
				Raylib.UnloadImage(image);
			}
		}

		return texture;
	}

	[DllImport("raylib", CallingConvention = CallingConvention.Cdecl)]
	static extern unsafe Font LoadFontFromMemory(
		string fileType,
		byte[] fileData,
		int dataSize,
		int fontSize,
		int* codepoints,
		int codepointCount
	);

	internal static unsafe Font LoadFontFromAssembly(string resource, int fontSize)
	{
		Font font;

		using (Stream stream = Assembly.GetManifestResourceStream(resource)!)
		{
			using (MemoryStream memoryStream = new MemoryStream())
			{
				stream.CopyTo(memoryStream);
				font = LoadFontFromMemory(
					".ttf",
					memoryStream.ToArray(),
					(int)memoryStream.Length,
					fontSize,
					null,
					256
				);
			}
		}

		return font;
	}
}
