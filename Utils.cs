// Licensed under the GPL3 or later versions of the GPL license.
// See the LICENSE file in the project root for more information.

using System;

namespace Calculator;

public struct Utils
{
    public static void Error(string message, int errorCode = 1)
    {
        Console.WriteLine($"ERROR: {message}.");
        Environment.Exit(errorCode);
    }

    public static (int Width, int Height) GetScreenResolution()
    {
        // TODO(LucasTA): support more platforms
        if (OperatingSystem.IsLinux())
        {
            string xdgSessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE");

            if (xdgSessionType == "x11")
            {
                return XLib.GetScreenResolution();
            }
            else if (
                    xdgSessionType == "wayland"
                    || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"))
                    )
            {
                Error("There is no wayland support");
            }
            else
            {
                Error("Could not detect compositor/display server");
            }
        }
        else
        {
            Error("There is only linux support");
        }

        return default;
    }
}
