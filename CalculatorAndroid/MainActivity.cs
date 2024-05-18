// Licensed under the GPL3 or later versions of the GPL license.
// See the LICENSE file in the project root for more information.

using Android.App;
using Android.Content;
using Android.Content.PM;

using Raylib_cs;

namespace Calculator;

[
    Activity(
        Label = "@string/app_name",
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.Orientation
            | ConfigChanges.KeyboardHidden
            | ConfigChanges.ScreenSize,
        ScreenOrientation = ScreenOrientation.Portrait,
        ClearTaskOnLaunch = true
    ),
    IntentFilter(new[] { Intent.ActionMain, Intent.CategoryLauncher }),
    MetaData(MetaDataLibName, Value = "raylib")
]
public class MainActivity : RaylibActivity
{
    protected override void OnReady()
    {
        CalculatorUI.MainLoop(Resources);
    }
}
