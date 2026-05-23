using System;
using System.Runtime.InteropServices;

using Android.App;
using Android.OS;

namespace Raylib_cs;

public abstract partial class RaylibActivity : NativeActivity
{
	protected override void OnCreate(Bundle? savedInstanceState)
	{
		RaylibSetAndroidCallback(OnReady);
		base.OnCreate(savedInstanceState);
	}

	protected abstract void OnReady();

	[LibraryImport(Raylib.nativeLibName)]
	[UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
	private static partial void RaylibSetAndroidCallback(Action callback);
}
