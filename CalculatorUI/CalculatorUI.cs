// Licensed under the GPL3 or later versions of the GPL license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.IO;

using Eval;
using Raylib_cs;

namespace Calculator;

internal struct Log
{
    /// <summary>Tolerable difference between colors</summary>
    private const int CONTRAST_LIMIT = 85;

    internal static string Message = "";

    [Conditional("DEBUG")]
    internal static void Draw()
    {
        Raylib.DrawTextEx(CalculatorUI.Fonte, Message, new(0, 0), CalculatorUI.FontSize, CalculatorUI.FONT_SPACING, Color.RED);
    }

    [Conditional("DEBUG")]
    internal static void IfTrue(bool result, string message)
    {
        if (result)
        {
            Message += message;
        }
    }

    [Conditional("DEBUG")]
    internal static void IfBadContrast(Color backgroundColor, Color textColor, string message)
    {
        int rDiff = Math.Abs(backgroundColor.R - textColor.R);
        int gDiff = Math.Abs(backgroundColor.G - textColor.G);
        int bDiff = Math.Abs(backgroundColor.B - textColor.B);

        if ((rDiff + gDiff + bDiff) / 3 < CONTRAST_LIMIT)
        {
            Message += message;
        }
    }
}

internal struct Layout
{
    internal readonly static Rectangle ICON_RECTANGLE = new(0, 0, 160, 160);

    internal enum ShadowKind
    {
        Float = 0,
        Cast = 1,
        Pillar = 2,
        Cube = 3,
        TransparentCube = 4,
    }

    internal readonly record struct TextFormat(
        string text,
        int fontSize,
        Color textColor
    );

    internal readonly record struct ShadowStyle(
        Color Color,
        int Distance,
        ShadowKind Kind = ShadowKind.Float
    );

    internal readonly record struct ButtonStyle(
        Color BackgroundColor,
        Color PressedColor,
        Color HoveredColor,
        Color? BorderColor = null,
        int BorderThickness = 1,
        ShadowStyle? ShadowStyle = null,
        Texture2D? Icon = null,
        int IconSize = 0
    );

    internal readonly record struct Button(
        int WidthPercentage,
        TextFormat? TextFormat,
        Action Callback,
        ButtonStyle Style,
        bool RepeatPresses = false
    );

    internal readonly record struct ButtonRow(int HeightPercentage, params Button[] Buttons);

    private static bool IsPointInsideRect(
        int x,
        int y,
        int recX,
        int recY,
        int recWidth,
        int recHeight
    )
    {
        return x >= recX && x <= recX + recWidth && y >= recY && y <= recY + recHeight;
    }

    internal static void DrawBox(
        int x,
        int y,
        int width,
        int height,
        Color backgroundColor,
        Color? borderColor = null,
        int borderThickness = 1,
        ShadowStyle? shadowStyle = null
    )
    {
        if (shadowStyle is ShadowStyle ss)
        {
            DrawShadow(x, y, width, height, ss, borderColor);
        }

        Raylib.DrawRectangle(x, y, width, height, backgroundColor);

        if (borderColor is Color bc)
        {
            Raylib.DrawRectangleLinesEx(
                new(x, y, width, height),
                borderThickness,
                bc
            );
        }
    }

    internal static void DrawText(
        int x,
        int y,
        int width,
        int height,
        string text,
        Color textColor,
        Color backgroundColor,
        int fontSize
    )
    {
        Vector2 textSize = Raylib.MeasureTextEx(CalculatorUI.Fonte, text, (int)fontSize, CalculatorUI.FONT_SPACING);

        Log.IfTrue(
            textSize.X > width || textSize.Y > height,
            $"ERROR: The text at the {x},{y} text box does not fit its box!\n"
        );

        Log.IfBadContrast(
            backgroundColor,
            textColor,
            $"ERROR: The text at the {x},{y} text box is not visible!\n"
        );

        int textX = x + ((width - (int)textSize.X) / 2);
        int textY = y + ((height - (int)textSize.Y) / 2);

        Raylib.DrawTextEx(CalculatorUI.Fonte, text, new(textX, textY), (int)fontSize, CalculatorUI.FONT_SPACING, textColor);
    }

    internal static void DrawTextBox(
        int x,
        int y,
        int width,
        int height,
        TextFormat textFormat,
        Color backgroundColor,
        Color? borderColor = null,
        int borderThickness = 1,
        ShadowStyle? shadowStyle = null
    )
    {
        DrawBox(x, y, width, height, backgroundColor, borderColor, borderThickness, shadowStyle);
        DrawText(x, y, width, height, textFormat.text, textFormat.textColor, backgroundColor, textFormat.fontSize);
    }

    internal static void DrawButton(
        int x,
        int y,
        int width,
        int height,
        Color backgroundColor,
        Color pressedColor,
        Color hoveredColor,
        Action callback,
        TextFormat? textFormat = null,
        bool repeatPresses = false,
        Texture2D? icon = null,
        int iconSize = 0,
        Color? borderColor = null,
        int borderThickness = 1,
        ShadowStyle? shadowStyle = null
    )
    {
        if (!CalculatorUI.Dragging && !CalculatorUI.ButtonWasPressed &&
            Raylib.IsMouseButtonReleased(MouseButton.MOUSE_BUTTON_LEFT) &&
            IsPointInsideRect(
                CalculatorUI.MouseX,
                CalculatorUI.MouseY,
                x,
                y,
                width,
                height
                ) &&
            IsPointInsideRect(
                CalculatorUI.MousePressedX,
                CalculatorUI.MousePressedY,
                x,
                y,
                width,
                height
            )
        )
        {
            CalculatorUI.ButtonWasPressed = true;
            CalculatorUI.ButtonPressedTime = 0;
            CalculatorUI.KeyRepeatInterval = CalculatorUI.INITIAL_REPEAT_INTERVAL;
            callback();
        }
        else if (
            Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT)
        )
        {
            if (IsPointInsideRect(
                    CalculatorUI.MouseX,
                    CalculatorUI.MouseY,
                    x,
                    y,
                    width,
                    height
                )
            )
            {
                if (!CalculatorUI.Dragging && !CalculatorUI.ButtonWasPressed &&
                    IsPointInsideRect(CalculatorUI.MousePressedX, CalculatorUI.MousePressedY, x, y, width, height))
                {
                    if (shadowStyle is ShadowStyle ss)
                    {
                        x += ss.Distance;
                        y += ss.Distance;
                    }

                    DrawBox(
                        x,
                        y,
                        width,
                        height,
                        pressedColor,
                        borderColor,
                        borderThickness
                    );

                    if (textFormat is TextFormat tf)
                    {
                        DrawText(
                            x,
                            y,
                            width,
                            height,
                            tf.text,
                            tf.textColor,
                            pressedColor,
                            tf.fontSize
                        );
                    }

                    CalculatorUI.ButtonPressedTime += Raylib.GetFrameTime();

                    if (repeatPresses && CalculatorUI.ButtonPressedTime >= CalculatorUI.KeyRepeatInterval)
                    {
                        CalculatorUI.ButtonWasPressed = true;
                        CalculatorUI.ButtonPressedTime = 0;
                        CalculatorUI.KeyRepeatInterval = Math.Max(CalculatorUI.KeyRepeatInterval * CalculatorUI.INITIAL_REPEAT_INTERVAL,
                                                CalculatorUI.MIN_REPEAT_INTERVAL);
                        callback();
                    }
                }
                else
                {
                    backgroundColor =
#if PLATFORM_ANDROID
                                CalculatorUI.TouchCount > 0 ? hoveredColor : backgroundColor;
#else
                                hoveredColor;
#endif

                    DrawBox(x, y, width, height, backgroundColor,
                            borderColor, borderThickness, shadowStyle);

                    if (textFormat is TextFormat tf)
                    {
                        DrawText(
                                x,
                                y,
                                width,
                                height,
                                tf.text,
                                tf.textColor,
                                backgroundColor,
                                tf.fontSize
                                );
                    }
                }
            }
            else
            {
                if (!CalculatorUI.Dragging && !CalculatorUI.ButtonWasPressed &&
                    IsPointInsideRect(CalculatorUI.MousePressedX, CalculatorUI.MousePressedY, x, y, width, height))
                {
                    if (shadowStyle is ShadowStyle ss)
                    {
                        x += ss.Distance;
                        y += ss.Distance;
                    }

                    DrawBox(
                        x,
                        y,
                        width,
                        height,
                        pressedColor,
                        borderColor,
                        borderThickness,
                        shadowStyle
                    );

                    if (textFormat is TextFormat tf)
                    {
                        DrawText(
                            x,
                            y,
                            width,
                            height,
                            tf.text,
                            tf.textColor,
                            pressedColor,
                            tf.fontSize
                        );
                    }

                    CalculatorUI.ButtonPressedTime += Raylib.GetFrameTime();

                    if (repeatPresses && CalculatorUI.ButtonPressedTime >= CalculatorUI.KeyRepeatInterval)
                    {
                        CalculatorUI.ButtonWasPressed = true;
                        CalculatorUI.ButtonPressedTime = 0;
                        CalculatorUI.KeyRepeatInterval = Math.Max(CalculatorUI.KeyRepeatInterval * CalculatorUI.INITIAL_REPEAT_INTERVAL,
                                                CalculatorUI.MIN_REPEAT_INTERVAL);
                        callback();
                    }
                }
                else
                {
                    DrawBox(
                        x,
                        y,
                        width,
                        height,
                        backgroundColor,
                        borderColor,
                        borderThickness,
                        shadowStyle
                    );

                    if (textFormat is TextFormat tf)
                    {
                        DrawText(
                            x,
                            y,
                            width,
                            height,
                            tf.text,
                            tf.textColor,
                            backgroundColor,
                            tf.fontSize
                        );
                    }
                }
            }
        }
        else
        {
            if (IsPointInsideRect(CalculatorUI.MouseX, CalculatorUI.MouseY, x, y, width, height))
            {
                backgroundColor =
#if PLATFORM_ANDROID
                                CalculatorUI.TouchCount > 0 ? hoveredColor : backgroundColor;
#else
                            hoveredColor;
#endif

                DrawBox(x, y, width, height, backgroundColor,
                        borderColor, borderThickness, shadowStyle);

                if (textFormat is TextFormat tf)
                {
                    DrawText(
                            x,
                            y,
                            width,
                            height,
                            tf.text,
                            tf.textColor,
                            backgroundColor,
                            tf.fontSize
                            );
                }
            }
            else
            {
                DrawBox(
                    x,
                    y,
                    width,
                    height,
                    backgroundColor,
                    borderColor,
                    borderThickness,
                    shadowStyle
                );

                if (textFormat is TextFormat tf)
                {
                    DrawText(
                        x,
                        y,
                        width,
                        height,
                        tf.text,
                        tf.textColor,
                        backgroundColor,
                        tf.fontSize
                    );
                }
            }
        }

        if (icon != null)
        {
            // in case iconSize is not set, adjust the icon to fit the smallest side and center it
            iconSize = iconSize > 0 ? iconSize : Math.Min(width, height);

            // Draw the texture
            Raylib.DrawTexturePro((Texture2D)icon, ICON_RECTANGLE,
                   new(x + width / 2 - iconSize / 2,
                       y + height / 2 - iconSize / 2,
                       iconSize,
                       iconSize),
                   new(0, 0), 0, Color.WHITE);
        }
    }

    internal static void DrawShadow(
        int x,
        int y,
        int width,
        int height,
        ShadowStyle shadowStyle,
        Color? outlineColor = null
    )
    {
        Raylib.DrawRectangle(
            x + shadowStyle.Distance,
            y + shadowStyle.Distance,
            width,
            height,
            shadowStyle.Color
        );

        if (shadowStyle.Kind != ShadowKind.Float)
        {
            // top right corner
            Raylib.DrawTriangle(
                new(x + shadowStyle.Distance, y + height),
                new(x, y + height),
                new(x + shadowStyle.Distance, y + height + shadowStyle.Distance),
                shadowStyle.Color
            );

            // bottom left corner
            Raylib.DrawTriangle(
                new(x + width, y),
                new(x + width, y + shadowStyle.Distance),
                new(x + width + shadowStyle.Distance, y + shadowStyle.Distance),
                shadowStyle.Color
            );

            if (outlineColor is Color oc && shadowStyle.Kind != ShadowKind.Cast)
            {
                if (shadowStyle.Kind == ShadowKind.Cube)
                {
                    // left cube shadow outline
                    Raylib.DrawLine(
                        x + width + shadowStyle.Distance,
                        y + shadowStyle.Distance,
                        x + width + shadowStyle.Distance,
                        y + height + shadowStyle.Distance,
                        oc
                    );

                    // bottom cube shadow outline
                    Raylib.DrawLine(
                        x + shadowStyle.Distance,
                        y + height + shadowStyle.Distance,
                        x + width + shadowStyle.Distance,
                        y + height + shadowStyle.Distance,
                        oc
                    );
                }
                else if (shadowStyle.Kind == ShadowKind.TransparentCube)
                {
                    Raylib.DrawRectangleLines(
                        x + shadowStyle.Distance,
                        y + shadowStyle.Distance,
                        width,
                        height,
                        oc
                    );
                }

                // top right outline
                Raylib.DrawLine(
                    x + width,
                    y,
                    x + width + shadowStyle.Distance,
                    y + shadowStyle.Distance,
                    oc
                );

                // bottom left outline
                Raylib.DrawLine(
                    x,
                    y + height,
                    x + shadowStyle.Distance,
                    y + height + shadowStyle.Distance,
                    oc
                );

                // bottom right outline
                Raylib.DrawLine(
                    x + width,
                    y + height,
                    x + width + shadowStyle.Distance,
                    y + height + shadowStyle.Distance,
                    oc
                );
            }
        }
    }

    internal static void DrawButtonGrid(
        int x,
        int y,
        int width,
        int height,
        int padding,
        params ButtonRow[] rows
    )
    {
        Log.IfTrue(
            x < 0
                || y < 0
                || width <= 0
                || height <= 0
                || x + width > CalculatorUI.ScreenWidth
                || y + height > CalculatorUI.ScreenHeight,
            "ERROR: Button grid is outside of the screen!\n"
        );

        int availableHeight = height - (padding * (rows.Length - 1));
        int curY = y;
        int takenHeight = 0;

        for (int i = 0; i < rows.Length; i++)
        {
            int rowLength = availableHeight * rows[i].HeightPercentage / 100;

            int availableWidth = width - (padding * (rows[i].Buttons.Length - 1));
            int curX = x;
            int takenWidth = 0;

            for (int j = 0; j < rows[i].Buttons.Length; j++)
            {
                int colLength = availableWidth * rows[i].Buttons[j].WidthPercentage / 100;

                DrawButton(
                    curX,
                    curY,
                    colLength,
                    rowLength,
                    rows[i].Buttons[j].Style.BackgroundColor,
                    rows[i].Buttons[j].Style.PressedColor,
                    rows[i].Buttons[j].Style.HoveredColor,
                    callback: rows[i].Buttons[j].Callback,
                    rows[i].Buttons[j].TextFormat,
                    rows[i].Buttons[j].RepeatPresses,
                    rows[i].Buttons[j].Style.Icon,
                    rows[i].Buttons[j].Style.IconSize,
                    rows[i].Buttons[j].Style.BorderColor,
                    rows[i].Buttons[j].Style.BorderThickness,
                    rows[i].Buttons[j].Style.ShadowStyle
                );

                curX += colLength + padding;
                takenWidth += colLength;

                Log.IfTrue(
                    takenWidth > availableWidth,
                    $"ERROR: Button grid {j + 1} column takes more than the available width!\n"
                );
            }

            curY += rowLength + padding;
            takenHeight += rowLength;

            Log.IfTrue(
                takenHeight > availableHeight,
                $"ERROR: Button grid {i + 1} row takes more than the available height!\n"
            );
        }
    }
}

public struct CalculatorUI
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
                Clipboard.Save(string.Join(Environment.NewLine, ExpressionHistory));
                Expression = result;
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
            }
        }
    };

#if PLATFORM_ANDROID
    internal static Vector2 StartTouchPosition;
    internal static int TouchCount = 0;
    internal const float INITIAL_REPEAT_INTERVAL = 0.5f;
#else
    internal const float INITIAL_REPEAT_INTERVAL = 0.3f;
    internal static float MouseScroll = 0;
#endif

    /// <summary>Time before updating animations and handling key presses</summary>
    internal const float UPDATE_INTERVAL = 0.5f;
    internal const float MIN_REPEAT_INTERVAL = INITIAL_REPEAT_INTERVAL / 10;
    internal static float ButtonPressedTime;
    internal static float KeyRepeatInterval = INITIAL_REPEAT_INTERVAL;
    internal static bool ButtonWasPressed = false;
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

            ExpressionHistory = new List<string>(Clipboard.Load());

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

#if PLATFORM_ANDROID
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
                                        new Layout.Button(20, new("(", FontSize, FontColor), () => SetExpression(Expression + "("), GreyButton, true),
                                        new Layout.Button(20, new(")", FontSize, FontColor), () => SetExpression(Expression + ")"), GreyButton, true),
                                        new Layout.Button(20, new(",", FontSize, FontColor), () => SetExpression(Expression + ","), GreyButton, true),
                                        new Layout.Button(20, new("^", FontSize, FontColor), () => SetExpression(Expression + "^"), GreyButton, true),
                                        new Layout.Button(20, null, () => SetExpression(Expression + "pi"),
                                            new(
                                                BackgroundColor: DarkGray,
                                                PressedColor: DarkerGray,
                                                HoveredColor: Color.GRAY,
                                                BorderColor: Color.GRAY,
                                                BorderThickness: BorderThickness,
                                                ShadowStyle: GreyButtonShadow,
                                                Icon: piTexture,
                                                IconSize: (int)FontTextSize.Y
                                            ), true)
                                    ),
                                    new Layout.ButtonRow(
                                        heightPercentage,
                                        new Layout.Button(20, new("!", FontSize, FontColor), () => SetExpression(Expression + "!"), GreyButton, true),
                                        new Layout.Button(20, new("e", FontSize, FontColor), () => SetExpression(Expression + "e"), GreyButton, true),
                                        new Layout.Button(20, new("%", FontSize, FontColor), () => SetExpression(Expression + "%"), GreyButton, true),
                                        new Layout.Button(20, new("/", FontSize, FontColor), () => SetExpression(Expression + "/"), GreyButton, true),
                                        new Layout.Button(20, new("C", FontSize, FontColor), () => SetExpression(""), GreyButton)
                                    ),
                                    new Layout.ButtonRow(
                                        heightPercentage,
                                        new Layout.Button(25, new("7", FontSize, FontColor), () => SetExpression(Expression + "7"), GreyButton, true),
                                        new Layout.Button(25, new("8", FontSize, FontColor), () => SetExpression(Expression + "8"), GreyButton, true),
                                        new Layout.Button(25, new("9", FontSize, FontColor), () => SetExpression(Expression + "9"), GreyButton, true),
                                        new Layout.Button(25, new("*", FontSize, FontColor), () => SetExpression(Expression + "*"), GreyButton, true)
                                    ),
                                    new Layout.ButtonRow(
                                        heightPercentage,
                                        new Layout.Button(25, new("4", FontSize, FontColor), () => SetExpression(Expression + "4"), GreyButton, true),
                                        new Layout.Button(25, new("5", FontSize, FontColor), () => SetExpression(Expression + "5"), GreyButton, true),
                                        new Layout.Button(25, new("6", FontSize, FontColor), () => SetExpression(Expression + "6"), GreyButton, true),
                                        new Layout.Button(25, new("-", FontSize, FontColor), () => SetExpression(Expression + "-"), GreyButton, true)
                                    ),
                                    new Layout.ButtonRow(
                                        heightPercentage,
                                        new Layout.Button(25, new("1", FontSize, FontColor), () => SetExpression(Expression + "1"), GreyButton, true),
                                        new Layout.Button(25, new("2", FontSize, FontColor), () => SetExpression(Expression + "2"), GreyButton, true),
                                        new Layout.Button(25, new("3", FontSize, FontColor), () => SetExpression(Expression + "3"), GreyButton, true),
                                        new Layout.Button(25, new("+", FontSize, FontColor), () => SetExpression(Expression + "+"), GreyButton, true)
                                    ),
                                    new Layout.ButtonRow(
                                        heightPercentage,
                                        new Layout.Button(25, new("0", FontSize, FontColor), () => SetExpression(Expression + "0"), GreyButton, true),
                                        new Layout.Button(25, new(".", FontSize, FontColor), () => SetExpression(Expression + "."), GreyButton, true),
                                        new Layout.Button(25, new("<-", FontSize, FontColor), Backspace, RedButton, true),
                                        new Layout.Button(25, new("=", FontSize, FontColor), Equal, GreenButton)
                                    ),
                                    new Layout.ButtonRow(
                                        heightPercentage,
                                        new Layout.Button(17, new("sqrt", FontSize, FontColor), () => SetExpression(Expression + "sqrt("), GreyButton, true),
                                        new Layout.Button(17, new("mod", FontSize, FontColor), () => SetExpression(Expression + "mod("), GreyButton, true),
                                        new Layout.Button(17, new("sin", FontSize, FontColor), () => SetExpression(Expression + "sin("), GreyButton, true),
                                        new Layout.Button(17, new("cos", FontSize, FontColor), () => SetExpression(Expression + "cos("), GreyButton, true),
                                        new Layout.Button(16, new("tan", FontSize, FontColor), () => SetExpression(Expression + "tan("), GreyButton, true),
                                        new Layout.Button(16, new("log", FontSize, FontColor), () => SetExpression(Expression + "log("), GreyButton, true)
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
                                if (ButtonPressedTime >= UPDATE_INTERVAL)
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
                                    icon: historyTexture
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
                                    icon: copyTexture
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
                                    icon: pasteTexture
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
                                icon: closeTexture
                                );

                            {
                                int rightPadding = Padding * 2 + topIconSize;
                                int visibleColumns = 12;
                                int entryHeight = ScreenHeight / visibleColumns;
                                int entryX = 0;
                                int entryWidth = ScreenWidth - rightPadding;

                                for (int i = 0; i < ExpressionHistory.Count; i++)
                                {
                                    int entryY = i * entryHeight;

                                    Layout.DrawTextBox(
                                        entryX,
                                        entryY,
                                        entryWidth,
                                        entryHeight,
                                        new(ExpressionHistory[i], FontSize, FontColor),
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
                                        Color.DARKGRAY,
                                        TransparentDarkGray,
                                        () => ExpressionHistory.Remove(ExpressionHistory[i]),
                                        null,
                                        icon: trashTexture
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
                                        () => Clipboard.Set(ExpressionHistory[i]),
                                        null,
                                        icon: copyTexture
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
                                            SetExpression(ExpressionHistory[i]);
                                            CurrentScene = Scene.Calculator;
                                        },
                                        null,
                                        icon: openTexture
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

            Clipboard.Save(string.Join(Environment.NewLine, ExpressionHistory));
            Raylib.CloseWindow();
        }
    }
}
