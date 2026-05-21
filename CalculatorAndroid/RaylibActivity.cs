using Android.App;
using Android.OS;
using System;
using System.Runtime.InteropServices;

namespace Raylib_cs;

public abstract class RaylibActivity : NativeActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        RaylibSetAndroidCallback(OnReady);
        base.OnCreate(savedInstanceState);
    }

    protected abstract void OnReady();

    [DllImport("raylib", CallingConvention = CallingConvention.Cdecl)]
    private static extern void RaylibSetAndroidCallback(Action callback);
}
