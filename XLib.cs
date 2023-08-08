// Licensed under the GPL3 or later versions of the GPL license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace Calculator;

internal partial struct XLib
{
    private const string XLibName = "libX11.so.6";

    [LibraryImport(XLibName)]
    internal static partial nint XOpenDisplay(nint display);

    [LibraryImport(XLibName)]
    internal static partial void XCloseDisplay(nint display);

    [LibraryImport(XLibName)]
    internal static partial int XDefaultScreen(nint display);

    [LibraryImport(XLibName)]
    internal static partial int XDisplayWidth(nint display, int screenNumber);

    [LibraryImport(XLibName)]
    internal static partial int XDisplayHeight(nint display, int screenNumber);

    internal static (int Width, int Height) GetScreenResolution()
    {
        nint display = XOpenDisplay(nint.Zero);

        if (display == nint.Zero)
        {
            Utils.Error("Failed to open XLib display");
        }

        int screenNumber = XDefaultScreen(display);
        int width = XDisplayWidth(display, screenNumber);
        int height = XDisplayHeight(display, screenNumber);

        XCloseDisplay(display);

        return (width, height);
    }
}
