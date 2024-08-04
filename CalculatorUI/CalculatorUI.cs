// Licensed under the GPL3 or later versions of the GPL license.
// See the LICENSE file in the project root for more information.
#if LINUX || ANDROID || WINDOWS
#else
#error "Yout need to specify a platform: ANDROID, WINDOWS, or LINUX"
#endif

using System;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Collections.Generic;

// for getting resources from the assembly
using System.Runtime.InteropServices;
using System.Reflection;
using System.IO;

using Eval;
using Raylib_cs;

namespace Calculator;

public readonly struct CalculatorUI
{
    private const string APP_NAME = "Calculator";
    private const int TARGET_FPS = 60;
    internal const int FONT_SPACING = 2;

    private static readonly Color BackgroundColor = Color.BLACK;
    private static readonly Color FontColor = Color.WHITE;
    private static readonly Color DarkerGray = new(60, 60, 60, 255);
    private static readonly Color DarkGray = new(100, 100, 100, 255);
    private static readonly Color LightGreen = new(0, 193, 47, 255);
    private static readonly Color TransparentDarkGray = new(100, 100, 100, 128);
    private static readonly Color LightRed = new(230, 70, 70, 255);

    private static void SetExpression(string value)
    {
        Expression = value;
        ErrorMessage = "";

        try
        {
            Result = Evaluator
                .Evaluate(Expression)
                .ToString("G", CultureInfo.InvariantCulture);
        }
        catch (Exception)
        {
            Result = "";
        }
    }

    internal static readonly Action Paste = () =>
    {
        SetExpression(Expression + Clipboard.Get());
    };

    internal static readonly Action Backspace = () =>
    {
        SetExpression(Expression == "" ? "" : Expression[..^1]);
    };

    internal static readonly Action Equal = () =>
    {
        Result = "";

        if (Expression != "")
        {
            try
            {
                string result = Evaluator
                    .Evaluate(Expression)
                    .ToString("G", CultureInfo.InvariantCulture);
                ErrorMessage = "";

                ExpressionHistory.Remove(Expression);
                ExpressionHistory.Add(Expression);
                History.Save(PinnedExpressions, ExpressionHistory);
                Expression = result;
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
            }
        }
    };

#if ANDROID
    internal const float INITIAL_REPEAT_INTERVAL = 0.5f;
    internal static int TouchCount = 0;
#else
    internal const float INITIAL_REPEAT_INTERVAL = 0.3f;
    internal static float MouseScroll = 0;
#endif

    internal const float MIN_REPEAT_INTERVAL = INITIAL_REPEAT_INTERVAL / 10;
    internal static float ButtonPressedTime;
    internal static float KeyRepeatInterval = INITIAL_REPEAT_INTERVAL;
    internal static float ButtonHoldToPressTime = 0.5f;
    internal static bool ButtonWasPressed = false;
    internal static bool ButtonWasHeldPressed = false;
    internal static bool Dragging = false;

    internal static int ScreenWidth = 0;
    internal static int ScreenHeight = 0;
    internal static int Padding = 0;
    internal static int BorderThickness = 0;
    internal static int ShadowDistance = 0;
    internal static int FontSize = 0;

    internal static int MouseX;
    internal static int MouseY;
    internal static int MousePressedX;
    internal static int MousePressedY;

    internal static string Expression = "";
    internal static string Result = "";
    internal static string ErrorMessage = "";
    internal static List<string> ExpressionHistory = new();
    internal static List<string> PinnedExpressions = new();

    internal static Font Fonte;

    internal enum Scene
    {
        Calculator,
        History,
        Settings,
        About,
    }

    internal static Scene CurrentScene = Scene.Calculator;

    [DllImport("raylib", CallingConvention = CallingConvention.Cdecl)]
    static extern Image LoadImageFromMemory(string fileType, byte[] fileData, int dataSize);

    public static Texture2D LoadTextureFromResource(string resourceName)
    {
        Texture2D texture;

        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)!)
        {
            if (stream == null)
                throw new ArgumentException($"Resource '{resourceName}' not found.");

            using (MemoryStream memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);

                Image image = LoadImageFromMemory(".png", memoryStream.ToArray(), (int)memoryStream.Length);
                texture = Raylib.LoadTextureFromImage(image);
                Raylib.UnloadImage(image);
            }
        }

        return texture;
    }

    [DllImport("raylib", CallingConvention = CallingConvention.Cdecl)]
    static extern unsafe Font LoadFontFromMemory(string fileType, byte[] fileData, int dataSize, int fontSize, int* codepoints, int codepointCount);

    public static unsafe Font LoadFontFromResource(string resourceName, int fontSize)
    {
        Font font;

        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)!)
        {
            if (stream == null)
                throw new ArgumentException($"Resource '{resourceName}' not found.");

            using (MemoryStream memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                font = LoadFontFromMemory(".ttf", memoryStream.ToArray(), (int)memoryStream.Length, fontSize, null, 256);
            }
        }

        return font;
    }

    public static void MainLoop()
    {
        // Raylib context
        {
            // NOTE(LucasTA): HIGHDPI stops the window from being scaled as its resized
            Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
            Raylib.InitWindow(0, 0, APP_NAME);
            Raylib.SetTargetFPS(TARGET_FPS);
            Raylib.SetExitKey(KeyboardKey.KEY_NULL);

            (PinnedExpressions, ExpressionHistory) = History.Load();

            // NOTE(LucasTA): Without HIGHDPI the font has artifacts, so we load
            // it realy big and then scale it down
            // just the filter is really blurry
            Fonte = LoadFontFromResource("CalculatorUI.Resources.iosevka-regular.ttf", 64);
            Raylib.SetTextureFilter(Fonte.Texture, TextureFilter.TEXTURE_FILTER_BILINEAR);

            Texture2D plusTexture = LoadTextureFromResource("CalculatorUI.Resources.plus_icon.png");
            Texture2D historyTexture = LoadTextureFromResource("CalculatorUI.Resources.history_icon.png");
            Texture2D closeTexture = LoadTextureFromResource("CalculatorUI.Resources.close_icon.png");
            Texture2D copyTexture = LoadTextureFromResource("CalculatorUI.Resources.copy_icon.png");
            Texture2D pasteTexture = LoadTextureFromResource("CalculatorUI.Resources.paste_icon.png");
            Texture2D openTexture = LoadTextureFromResource("CalculatorUI.Resources.open_icon.png");
            Texture2D pinTexture = LoadTextureFromResource("CalculatorUI.Resources.pin_icon.png");
            Texture2D unpinTexture = LoadTextureFromResource("CalculatorUI.Resources.unpin_icon.png");
            Texture2D piTexture = LoadTextureFromResource("CalculatorUI.Resources.pi_icon.png");
            Texture2D trashTexture = LoadTextureFromResource("CalculatorUI.Resources.trash_icon.png");

            Raylib.SetWindowIcon(Raylib.LoadImageFromTexture(plusTexture));

            while (!Raylib.WindowShouldClose())
            {
                // get screen information
                {
                    ScreenWidth = Raylib.GetScreenWidth();
                    ScreenHeight = Raylib.GetScreenHeight();
                    FontSize = (ScreenWidth < ScreenHeight ? ScreenWidth : ScreenHeight) / 20;
                    BorderThickness = Math.Max((ScreenWidth > ScreenHeight ? ScreenWidth : ScreenHeight) / 500, 1);
                    ShadowDistance = BorderThickness * 4;
                    Padding = BorderThickness * 8;

#if ANDROID
      TouchCount = Raylib.GetTouchPointCount();
#else
                    MouseScroll = Raylib.GetMouseWheelMove() * 32;
#endif
                }

                // get mouse information
                {
                    MouseX = Raylib.GetMouseX();
                    MouseY = Raylib.GetMouseY();
                    ButtonWasPressed = false;

                    if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
                    {
                        MousePressedX = MouseX;
                        MousePressedY = MouseY;
                    }

                    if (Raylib.IsMouseButtonReleased(MouseButton.MOUSE_BUTTON_LEFT))
                    {
                        CalculatorUI.ButtonWasHeldPressed = false;
                        CalculatorUI.ButtonPressedTime = 0;
                    }
                }

                // draw
                {
                    Raylib.BeginDrawing();
                    Raylib.ClearBackground(BackgroundColor);

                    int DisplayX = Padding;
                    int DisplayY = Padding;
                    int DisplayWidth = ScreenWidth - (Padding * 2);
                    int DisplayHeight = (ScreenHeight / 6) - Padding;

                    Vector2 FontTextSize = Raylib.MeasureTextEx(CalculatorUI.Fonte, "0", FontSize, CalculatorUI.FONT_SPACING);
                    int topIconSize = DisplayY + (int)FontTextSize.Y;

                    switch (CurrentScene)
                    {
                        case Scene.Calculator:
                            // Draw buttons
                            {
                                Layout.ShadowStyle GreyButtonShadow =
                                    new(DarkerGray, ShadowDistance, Layout.ShadowKind.Pillar);
                                Layout.ShadowStyle RedButtonShadow =
                                    new(Color.MAROON, ShadowDistance, Layout.ShadowKind.Pillar);
                                Layout.ShadowStyle GreenButtonShadow =
                                    new(Color.DARKGREEN, ShadowDistance, Layout.ShadowKind.Pillar);

                                Layout.ButtonStyle GreyButton =
                                    new(
                                        BackgroundColor: DarkGray,
                                        PressedColor: DarkerGray,
                                        HoveredColor: Color.GRAY,
                                        BorderColor: Color.GRAY,
                                        BorderThickness: BorderThickness,
                                        ShadowStyle: GreyButtonShadow
                                    );

                                Layout.ButtonStyle RedButton =
                                    new(
                                        BackgroundColor: Color.RED,
                                        PressedColor: Color.MAROON,
                                        HoveredColor: Color.ORANGE,
                                        BorderColor: Color.ORANGE,
                                        BorderThickness: BorderThickness,
                                        ShadowStyle: RedButtonShadow
                                    );

                                Layout.ButtonStyle GreenButton =
                                    new(
                                        BackgroundColor: LightGreen,
                                        PressedColor: Color.DARKGREEN,
                                        HoveredColor: Color.GREEN,
                                        BorderColor: Color.GREEN,
                                        BorderThickness: BorderThickness,
                                        ShadowStyle: GreenButtonShadow
                                    );

                                int rowAmount = 7;
                                int heightPercentage = 100 / rowAmount;

                                Layout.ButtonRow[] ButtonGrid = new Layout.ButtonRow[]
                                {
                                    new Layout.ButtonRow(
                                        heightPercentage,
                                        new Layout.Button(20, new("(", FontSize, FontColor), () => SetExpression(Expression + "("), GreyButton, Layout.ButtonPressMode.HoldToRepeat),
                                        new Layout.Button(20, new(")", FontSize, FontColor), () => SetExpression(Expression + ")"), GreyButton, Layout.ButtonPressMode.HoldToRepeat),
                                        new Layout.Button(20, new(",", FontSize, FontColor), () => SetExpression(Expression + ","), GreyButton),
                                        new Layout.Button(20, new("^", FontSize, FontColor), () => SetExpression(Expression + "^"), GreyButton),
                                        new Layout.Button(20, null, () => SetExpression(Expression + "pi"),
                                            new(
                                                BackgroundColor: DarkGray,
                                                PressedColor: DarkerGray,
                                                HoveredColor: Color.GRAY,
                                                BorderColor: Color.GRAY,
                                                BorderThickness: BorderThickness,
                                                ShadowStyle: GreyButtonShadow,
                                                Icon: new(piTexture, Color.WHITE, (int)FontTextSize.Y)
                                            ))
                                    ),
                                    new Layout.ButtonRow(
                                        heightPercentage,
                                        new Layout.Button(20, new("!", FontSize, FontColor), () => SetExpression(Expression + "!"), GreyButton),
                                        new Layout.Button(20, new("e", FontSize, FontColor), () => SetExpression(Expression + "e"), GreyButton),
                                        new Layout.Button(20, new("%", FontSize, FontColor), () => SetExpression(Expression + "%"), GreyButton),
                                        new Layout.Button(20, new("/", FontSize, FontColor), () => SetExpression(Expression + "/"), GreyButton),
                                        new Layout.Button(20, new("C", FontSize, FontColor), () => SetExpression(""), GreyButton)
                                    ),
                                    new Layout.ButtonRow(
                                        heightPercentage,
                                        new Layout.Button(25, new("7", FontSize, FontColor), () => SetExpression(Expression + "7"), GreyButton, Layout.ButtonPressMode.HoldToRepeat),
                                        new Layout.Button(25, new("8", FontSize, FontColor), () => SetExpression(Expression + "8"), GreyButton, Layout.ButtonPressMode.HoldToRepeat),
                                        new Layout.Button(25, new("9", FontSize, FontColor), () => SetExpression(Expression + "9"), GreyButton, Layout.ButtonPressMode.HoldToRepeat),
                                        new Layout.Button(25, new("*", FontSize, FontColor), () => SetExpression(Expression + "*"), GreyButton)
                                    ),
                                    new Layout.ButtonRow(
                                        heightPercentage,
                                        new Layout.Button(25, new("4", FontSize, FontColor), () => SetExpression(Expression + "4"), GreyButton, Layout.ButtonPressMode.HoldToRepeat),
                                        new Layout.Button(25, new("5", FontSize, FontColor), () => SetExpression(Expression + "5"), GreyButton, Layout.ButtonPressMode.HoldToRepeat),
                                        new Layout.Button(25, new("6", FontSize, FontColor), () => SetExpression(Expression + "6"), GreyButton, Layout.ButtonPressMode.HoldToRepeat),
                                        new Layout.Button(25, new("-", FontSize, FontColor), () => SetExpression(Expression + "-"), GreyButton)
                                    ),
                                    new Layout.ButtonRow(
                                        heightPercentage,
                                        new Layout.Button(25, new("1", FontSize, FontColor), () => SetExpression(Expression + "1"), GreyButton, Layout.ButtonPressMode.HoldToRepeat),
                                        new Layout.Button(25, new("2", FontSize, FontColor), () => SetExpression(Expression + "2"), GreyButton, Layout.ButtonPressMode.HoldToRepeat),
                                        new Layout.Button(25, new("3", FontSize, FontColor), () => SetExpression(Expression + "3"), GreyButton, Layout.ButtonPressMode.HoldToRepeat),
                                        new Layout.Button(25, new("+", FontSize, FontColor), () => SetExpression(Expression + "+"), GreyButton)
                                    ),
                                    new Layout.ButtonRow(
                                        heightPercentage,
                                        new Layout.Button(25, new("0", FontSize, FontColor), () => SetExpression(Expression + "0"), GreyButton, Layout.ButtonPressMode.HoldToRepeat),
                                        new Layout.Button(25, new(".", FontSize, FontColor), () => SetExpression(Expression + "."), GreyButton),
                                        new Layout.Button(25, new("<-", FontSize, FontColor), Backspace, RedButton, Layout.ButtonPressMode.HoldToRepeat),
                                        new Layout.Button(25, new("=", FontSize, FontColor), Equal, GreenButton)
                                    ),
                                    new Layout.ButtonRow(
                                        heightPercentage,
                                        new Layout.Button(17, new("sqrt", FontSize, FontColor), () => SetExpression(Expression + "sqrt("), GreyButton),
                                        new Layout.Button(17, new("mod", FontSize, FontColor), () => SetExpression(Expression + "mod("), GreyButton),
                                        new Layout.Button(17, new("sin", FontSize, FontColor), () => SetExpression(Expression + "sin("), GreyButton),
                                        new Layout.Button(17, new("cos", FontSize, FontColor), () => SetExpression(Expression + "cos("), GreyButton),
                                        new Layout.Button(16, new("tan", FontSize, FontColor), () => SetExpression(Expression + "tan("), GreyButton),
                                        new Layout.Button(16, new("log", FontSize, FontColor), () => SetExpression(Expression + "log("), GreyButton)
                                    )
                                };

                                Layout.DrawButtonGrid(
                                    Padding,
                                    Padding + (ScreenHeight / 6),
                                    ScreenWidth - (Padding * 2),
                                    ScreenHeight - (Padding * 2) - (ScreenHeight / 6),
                                    Padding,
                                    ButtonGrid
                                );
                            }

                            // Draw Calculator Display
                            {
                                if (ErrorMessage == "")
                                {
                                    Layout.DrawTextBox(
                                            DisplayX,
                                            DisplayY,
                                            DisplayWidth,
                                            DisplayHeight,
                                            new(Expression, FontSize, FontColor),
                                            DarkerGray,
                                            DarkGray,
                                            BorderThickness * 2
                                            );

                                    Raylib.DrawTextEx(
                                            CalculatorUI.Fonte,
                                            Result,
                                            new(
                                                DisplayX + Padding,
                                                ScreenHeight / 8 - Padding
                                            ),
                                            FontSize,
                                            CalculatorUI.FONT_SPACING,
                                            Color.GRAY
                                            );
                                }
                                else
                                {
                                    Layout.DrawTextBox(
                                            DisplayX,
                                            DisplayY,
                                            DisplayWidth,
                                            DisplayHeight,
                                            new(Expression, FontSize, FontColor),
                                            DarkerGray,
                                            Color.RED,
                                            BorderThickness * 2
                                            );

                                    Raylib.DrawTextEx(
                                            CalculatorUI.Fonte,
                                            ErrorMessage,
                                            new(
                                                DisplayX + Padding,
                                                ScreenHeight / 8 - Padding
                                            ),
                                            FontSize,
                                            CalculatorUI.FONT_SPACING,
                                            Color.RED
                                            );
                                }
                            }

                            int keycode = Raylib.GetCharPressed();

                            if (Raylib.IsKeyPressed(KeyboardKey.KEY_ENTER))
                            {
                                Equal();
                                ButtonPressedTime = 0;
                            }
                            else if ((Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL)
                                        || Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_CONTROL))
                                    && Raylib.IsKeyPressed(KeyboardKey.KEY_C))
                            {
                                Clipboard.Set(Expression);
                                ButtonPressedTime = 0;
                            }
                            else if ((Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL) || Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_CONTROL)) && Raylib.IsKeyPressed(KeyboardKey.KEY_V))
                            {
                                Paste();
                                ButtonPressedTime = 0;
                            }
                            else if (Raylib.IsKeyPressed(KeyboardKey.KEY_BACKSPACE))
                            {
                                Backspace();
                                ButtonPressedTime = 0;
                            }
                            else if (Raylib.IsKeyDown(KeyboardKey.KEY_BACKSPACE))
                            {
                                if (ButtonPressedTime >= INITIAL_REPEAT_INTERVAL)
                                {
                                    Backspace();
                                }

                                ButtonPressedTime += Raylib.GetFrameTime();
                            }
                            else if (keycode != 0)
                            {
                                if (char.IsAsciiLetterOrDigit((char)keycode) ||
                                        (char)keycode is ' ' or '(' or ')' or ',' or '^'
                                                      or '!' or '%' or '/' or '+' or '-' or '*' or '.')
                                {
                                    SetExpression(Expression + ((char)keycode).ToString().ToLowerInvariant());
                                    ButtonPressedTime = 0;
                                }
                            }

                            // top buttons
                            {
                                Layout.DrawButton(
                                    ScreenWidth - topIconSize,
                                    0,
                                    topIconSize,
                                    topIconSize,
                                    Color.BLANK,
                                    Color.DARKGRAY,
                                    TransparentDarkGray,
                                    () => CurrentScene = Scene.History,
                                    null,
                                    icon: new(historyTexture, Color.WHITE)
                                );

                                Layout.DrawButton(
                                    ScreenWidth - (topIconSize) * 2 - Padding,
                                    0,
                                    topIconSize,
                                    topIconSize,
                                    Color.BLANK,
                                    Color.DARKGRAY,
                                    TransparentDarkGray,
                                    () => Clipboard.Set(Expression),
                                    null,
                                    icon: new(copyTexture, Color.WHITE)
                                );

                                Layout.DrawButton(
                                    ScreenWidth - topIconSize * 3 - Padding * 2,
                                    0,
                                    topIconSize,
                                    topIconSize,
                                    Color.BLANK,
                                    Color.DARKGRAY,
                                    TransparentDarkGray,
                                    Paste,
                                    null,
                                    icon: new(pasteTexture, Color.WHITE)
                                );
                            }
                            break;
                        case Scene.History:
                            if (Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE))
                            {
                                CurrentScene = Scene.Calculator;
                            }

                            Layout.DrawButton(
                                ScreenWidth - topIconSize,
                                0,
                                topIconSize,
                                topIconSize,
                                Color.BLANK,
                                Color.DARKGRAY,
                                TransparentDarkGray,
                                () => CurrentScene = Scene.Calculator,
                                null,
                                icon: new(closeTexture, Color.WHITE)
                            );

                            {
                                int rightPadding = Padding * 2 + topIconSize;
                                int visibleColumns = 12;
                                int entryHeight = ScreenHeight / visibleColumns;
                                int entryX = 0;
                                int entryWidth = ScreenWidth - rightPadding;

                                List<string> expressions = new(PinnedExpressions);
                                expressions.AddRange(ExpressionHistory);

                                for (int i = 0; i < expressions.Count; i++)
                                {
                                    int entryY = i * entryHeight;

                                    Layout.DrawTextBox(
                                        entryX,
                                        entryY,
                                        entryWidth,
                                        entryHeight,
                                        new(expressions[i], FontSize, FontColor),
                                        Color.DARKGRAY,
                                        Color.GRAY,
                                        BorderThickness);

                                    int buttonsY = entryY + ((entryHeight - topIconSize - BorderThickness) / 2);
                                    int deleteX = entryX + entryWidth - topIconSize - Padding;

                                    Layout.DrawButton(
                                        deleteX,
                                        buttonsY,
                                        topIconSize,
                                        topIconSize,
                                        Color.BLANK,
                                        Color.MAROON,
                                        TransparentDarkGray,
                                        () =>
                                        {
                                            PinnedExpressions.Remove(expressions[i]);
                                            ExpressionHistory.Remove(expressions[i]);
                                            History.Save(PinnedExpressions, ExpressionHistory);
                                        },
                                        null,
                                        icon: new(trashTexture, LightRed),
                                        pressMode: Layout.ButtonPressMode.HoldToPress
                                    );

                                    int copyX = deleteX - topIconSize - Padding;

                                    Layout.DrawButton(
                                        copyX,
                                        buttonsY,
                                        topIconSize,
                                        topIconSize,
                                        Color.BLANK,
                                        Color.DARKGRAY,
                                        TransparentDarkGray,
                                        () => Clipboard.Set(expressions[i]),
                                        null,
                                        icon: new(copyTexture, Color.WHITE)
                                    );

                                    int pickX = copyX - topIconSize - Padding;

                                    Layout.DrawButton(
                                        pickX,
                                        buttonsY,
                                        topIconSize,
                                        topIconSize,
                                        Color.BLANK,
                                        Color.DARKGRAY,
                                        TransparentDarkGray,
                                        () =>
                                        {
                                            SetExpression(expressions[i]);
                                            CurrentScene = Scene.Calculator;
                                        },
                                        null,
                                        icon: new(openTexture, Color.WHITE)
                                    );

                                    int pinX = pickX - topIconSize - Padding;
                                    bool pinned = PinnedExpressions.Contains(expressions[i]);

                                    Layout.DrawButton(
                                        pinX,
                                        buttonsY,
                                        topIconSize,
                                        topIconSize,
                                        Color.BLANK,
                                        Color.DARKGRAY,
                                        TransparentDarkGray,
                                        () =>
                                        {
                                            if (pinned)
                                            {
                                                PinnedExpressions.Remove(expressions[i]);
                                                ExpressionHistory.Remove(expressions[i]);
                                                ExpressionHistory.Add(expressions[i]);
                                            }
                                            else
                                            {
                                                ExpressionHistory.Remove(expressions[i]);
                                                PinnedExpressions.Add(expressions[i]);
                                            }

                                            History.Save(PinnedExpressions, ExpressionHistory);
                                        },
                                        null,
                                        icon: new(pinned ? unpinTexture : pinTexture, Color.WHITE)
                                    );
                                }
                            }
                            break;
                        case Scene.Settings:
                            if (Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE))
                            {
                                CurrentScene = Scene.Calculator;
                            }
                            break;
                        case Scene.About:
                            if (Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE))
                            {
                                CurrentScene = Scene.Calculator;
                            }
                            break;
                        default:
                            throw new UnreachableException("Unknown scene");
                    }

                    Log.Message =
$"""
Resolution: {ScreenWidth}x{ScreenHeight}
FPS: {Raylib.GetFPS()}
MouseXY: {MouseX}x{MouseY}
MousePressedXY: {MousePressedX}x{MousePressedY}
BorderThickness: {BorderThickness}
ShadowDistance: {ShadowDistance}
Padding: {Padding}
{Log.Message}
"""
;

                    Log.Draw();
                    Log.Message = "";

                    Raylib.EndDrawing();
                }
            }

            History.Save(PinnedExpressions, ExpressionHistory);
            Raylib.CloseWindow();
        }
    }
}
