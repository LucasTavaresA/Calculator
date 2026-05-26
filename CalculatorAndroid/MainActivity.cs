// Licensed under the GPL3 or later versions of the GPL license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Versioning;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Window;

using Raylib_cs;

namespace Calculator;

[
	Activity(
		Name = "com.lucasta.calculator.MainActivity",
		Label = "@string/app_name",
		MainLauncher = true,
		ConfigurationChanges = ConfigChanges.Orientation
			| ConfigChanges.KeyboardHidden
			| ConfigChanges.ScreenSize,
		ScreenOrientation = ScreenOrientation.Portrait,
		ClearTaskOnLaunch = true
	),
	IntentFilter([Intent.ActionMain, Intent.CategoryLauncher]),
	MetaData(MetaDataLibName, Value = "raylib")
]
public class MainActivity : RaylibActivity
{
	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);
		Calculator.Context = this;

		if (OperatingSystem.IsAndroidVersionAtLeast(33))
		{
			OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(IOnBackInvokedDispatcher.PriorityDefault, new BackCallback(this));
		}
	}

	protected override void OnReady()
	{
		Calculator.MainLoop();
	}

	public override void OnBackPressed()
	{
		if (!Calculator.HandleBackButton())
		{
			MoveTaskToBack(true);
		}
	}

	[SupportedOSPlatform("android33.0")]
	private sealed class BackCallback(MainActivity activity) : Java.Lang.Object, IOnBackInvokedCallback
	{
		private readonly MainActivity mainActivity = activity;

		public void OnBackInvoked()
		{
			if (!Calculator.HandleBackButton())
			{
				mainActivity.MoveTaskToBack(true);
			}
		}
	}
}
