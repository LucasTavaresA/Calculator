// Licensed under the GPL3 or later versions of the GPL license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Collections.Generic;

using Eval;
using Raylib_cs;

#if ANDROID
using Android.Content.Res;
using Xamarin.Essentials;
#endif

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
    internal enum ShadowKind
    {
        Float = 0,
        Cast = 1,
        Pillar = 2,
        Cube = 3,
        TransparentCube = 4,
    }

    internal readonly record struct ShadowStyle(
        Color Color,
        int Distance,
        ShadowKind Kind = ShadowKind.Float
    );

    internal readonly record struct ButtonStyle(
        Color TextColor,
        Color BackgroundColor,
        Color PressedColor,
        Color HoveredColor,
        int? FontSize = null,
        Color? BorderColor = null,
        int BorderThickness = 1,
        ShadowStyle? ShadowStyle = null
    );

    internal readonly record struct Button(int WidthPercentage, string Text, ButtonStyle Style);

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

    internal static void DrawTextBox(
        int x,
        int y,
        int width,
        int height,
        string text,
        Color textColor,
        Color backgroundColor,
        int? fontSize = null,
        Color? borderColor = null,
        int borderThickness = 1,
        ShadowStyle? shadowStyle = null
    )
    {
        if (fontSize == null)
        {
            fontSize = CalculatorUI.FontSize;
        }

        Vector2 textSize = Raylib.MeasureTextEx(CalculatorUI.Fonte, text, (int)fontSize, CalculatorUI.FONT_SPACING);

        Log.IfTrue(
            textSize.X > width || textSize.Y > height,
            $"ERROR: The text at the {x},{y} text box does not fit its box!\n"
        );

        int textX = x + ((width - (int)textSize.X) / 2);
        int textY = y + ((height - (int)textSize.Y) / 2);

        if (shadowStyle != null)
        {
            DrawShadow(x, y, width, height, (ShadowStyle)shadowStyle, borderColor);
        }

        Log.IfBadContrast(
            backgroundColor,
            textColor,
            $"ERROR: The text at the {x},{y} text box is not visible!\n"
        );

        Raylib.DrawRectangle(x, y, width, height, backgroundColor);

        if (borderColor != null)
        {
            Raylib.DrawRectangleLinesEx(
                new(x, y, width, height),
                borderThickness,
                (Color)borderColor
            );
        }

        Raylib.DrawTextEx(CalculatorUI.Fonte, text, new(textX, textY), (int)fontSize, CalculatorUI.FONT_SPACING, textColor);
    }

    internal static void DrawButton(
        int x,
        int y,
        int width,
        int height,
        string text,
        Color textColor,
        Color backgroundColor,
        Color pressedColor,
        Color hoveredColor,
        int? fontSize = null,
        Color? borderColor = null,
        int borderThickness = 1,
        ShadowStyle? shadowStyle = null
    )
    {
        if (
            Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT)
            && IsPointInsideRect(CalculatorUI.MouseX, CalculatorUI.MouseY, x, y, width, height)
        )
        {
            if (CalculatorUI.Commands.TryGetValue(text, out var command))
            {
                command();
            }
            else
            {
                CalculatorUI.ErrorMessage = $"The '{text}' button does not have a command defined!\n";
            }

            CalculatorUI.ButtonPressedTime = 0;
        }
        else if (
            Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT)
            && IsPointInsideRect(
                CalculatorUI.MousePressedX,
                CalculatorUI.MousePressedY,
                x,
                y,
                width,
                height
            )
        )
        {
            if (shadowStyle != null)
            {
                x += ((ShadowStyle)shadowStyle).Distance;
                y += ((ShadowStyle)shadowStyle).Distance;
            }

            DrawTextBox(
                x,
                y,
                width,
                height,
                text,
                textColor,
                pressedColor,
                fontSize: fontSize,
                borderColor: borderColor,
                borderThickness: borderThickness
            );

            if (CalculatorUI.ButtonPressedTime >= CalculatorUI.UPDATE_INTERVAL)
            {
                if (CalculatorUI.Commands.TryGetValue(text, out var command))
                {
                    command();
                }
                else
                {
                    CalculatorUI.ErrorMessage =
                        $"The '{text}' button does not have a command defined!\n";
                }
            }

            CalculatorUI.ButtonPressedTime += Raylib.GetFrameTime();
        }
        else
        {
            DrawTextBox(
                x,
                y,
                width,
                height,
                text,
                textColor,
                IsPointInsideRect(CalculatorUI.MouseX, CalculatorUI.MouseY, x, y, width, height)
#if ANDROID
                    && CalculatorUI.TouchCount > 0
#endif
                    ? hoveredColor : backgroundColor,
                fontSize: fontSize,
                borderColor: borderColor,
                borderThickness: borderThickness,
                shadowStyle: shadowStyle
            );
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

            if (outlineColor != null && shadowStyle.Kind != ShadowKind.Cast)
            {
                if (shadowStyle.Kind == ShadowKind.Cube)
                {
                    // left cube shadow outline
                    Raylib.DrawLine(
                        x + width + shadowStyle.Distance,
                        y + shadowStyle.Distance,
                        x + width + shadowStyle.Distance,
                        y + height + shadowStyle.Distance,
                        (Color)outlineColor
                    );

                    // bottom cube shadow outline
                    Raylib.DrawLine(
                        x + shadowStyle.Distance,
                        y + height + shadowStyle.Distance,
                        x + width + shadowStyle.Distance,
                        y + height + shadowStyle.Distance,
                        (Color)outlineColor
                    );
                }
                else if (shadowStyle.Kind == ShadowKind.TransparentCube)
                {
                    Raylib.DrawRectangleLines(
                        x + shadowStyle.Distance,
                        y + shadowStyle.Distance,
                        width,
                        height,
                        (Color)outlineColor
                    );
                }

                // top right outline
                Raylib.DrawLine(
                    x + width,
                    y,
                    x + width + shadowStyle.Distance,
                    y + shadowStyle.Distance,
                    (Color)outlineColor
                );

                // bottom left outline
                Raylib.DrawLine(
                    x,
                    y + height,
                    x + shadowStyle.Distance,
                    y + height + shadowStyle.Distance,
                    (Color)outlineColor
                );

                // bottom right outline
                Raylib.DrawLine(
                    x + width,
                    y + height,
                    x + width + shadowStyle.Distance,
                    y + height + shadowStyle.Distance,
                    (Color)outlineColor
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
                    rows[i].Buttons[j].Text,
                    rows[i].Buttons[j].Style.TextColor,
                    rows[i].Buttons[j].Style.BackgroundColor,
                    fontSize: rows[i].Buttons[j].Style.FontSize,
                    hoveredColor: rows[i].Buttons[j].Style.HoveredColor,
                    pressedColor: rows[i].Buttons[j].Style.PressedColor,
                    borderColor: rows[i].Buttons[j].Style.BorderColor,
                    borderThickness: rows[i].Buttons[j].Style.BorderThickness,
                    shadowStyle: rows[i].Buttons[j].Style.ShadowStyle
                );

                curX += colLength + padding;
                takenWidth += colLength;

                Log.IfTrue(
                    takenWidth > availableWidth,
                    $"ERROR: Button grid {j + 1} column takes more than the available width!\n"
                );

                Log.IfTrue(
                    !CalculatorUI.Commands.ContainsKey(rows[i].Buttons[j].Text),
                    $"ERROR: The '{rows[i].Buttons[j].Text}' button does not have a command defined!\n"
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

    internal static readonly Dictionary<string, Action> Commands =
        new()
        {
            {
                "=",
                () =>
                {
                    Result = "";

                    if (Expression != "")
                    {
                        try
                        {
                            Expression = Evaluator
                                .Evaluate(Expression)
                                .ToString("G", CultureInfo.InvariantCulture);
                            ErrorMessage = "";
                        }
                        catch (Exception e)
                        {
                            ErrorMessage = e switch
                            {
                                _ => e.Message,
                            };
                        }
                    }
                }
            },
            { "C", () => SetExpression("") },
            { "<-", () => SetExpression(Expression == "" ? "" : Expression![..^1]) },
            { "sin", () => SetExpression(Expression + "sin(") },
            { "cos", () => SetExpression(Expression + "cos(") },
            { "tan", () => SetExpression(Expression + "tan(") },
            { "mod", () => SetExpression(Expression + "mod(") },
            { "sqrt", () => SetExpression(Expression + "sqrt(") },
            { "log", () => SetExpression(Expression + "log(") },
            { "pi", () => SetExpression(Expression + "pi") },
            { "e", () => SetExpression(Expression + "e") },
            { "0", () => SetExpression(Expression + "0") },
            { "1", () => SetExpression(Expression + "1") },
            { "2", () => SetExpression(Expression + "2") },
            { "3", () => SetExpression(Expression + "3") },
            { "4", () => SetExpression(Expression + "4") },
            { "5", () => SetExpression(Expression + "5") },
            { "6", () => SetExpression(Expression + "6") },
            { "7", () => SetExpression(Expression + "7") },
            { "8", () => SetExpression(Expression + "8") },
            { "9", () => SetExpression(Expression + "9") },
            { "+", () => SetExpression(Expression + "+") },
            { "-", () => SetExpression(Expression + "-") },
            { "*", () => SetExpression(Expression + "*") },
            { "/", () => SetExpression(Expression + "/") },
            { "^", () => SetExpression(Expression + "^") },
            { "%", () => SetExpression(Expression + "%") },
            { "!", () => SetExpression(Expression + "!") },
            { ".", () => SetExpression(Expression + ".") },
            { ",", () => SetExpression(Expression + ", ") },
            { "(", () => SetExpression(Expression + "(") },
            { ")", () => SetExpression(Expression + ")") },
        };

    /// <summary>Time before updating animations and handling key presses</summary>
    internal const float UPDATE_INTERVAL = 0.5f;

    internal static float ButtonPressedTime;

    internal static int ScreenWidth = 0;
    internal static int ScreenHeight = 0;
    internal static int Padding = 0;
    internal static int BorderThickness = 0;
    internal static int ShadowDistance = 0;
    internal static int FontSize = 0;
    internal static int TouchCount = 0;

    internal static int MouseX;
    internal static int MouseY;
    internal static int MousePressedX;
    internal static int MousePressedY;

    internal static string Expression = "";
    internal static string Result = "";
    internal static string ErrorMessage = "";

    internal static Font Fonte;

#if ANDROID
    private static int GetStatusBarHeight(RaylibActivity activity)
    {
        int statusBarHeight = 0;
        int resourceId = activity.Resources.GetIdentifier("status_bar_height", "dimen", "android");

        if (resourceId > 0)
        {
            statusBarHeight = activity.Resources.GetDimensionPixelSize(resourceId);
        }

        return statusBarHeight;
    }

    public static void MainLoop(RaylibActivity activity)
#else
    public static void MainLoop()
#endif
    {
        // Raylib context
        {
            Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE | ConfigFlags.FLAG_WINDOW_HIGHDPI | ConfigFlags.FLAG_INTERLACED_HINT);
            Raylib.InitWindow(0, 0, APP_NAME);
            Raylib.SetTargetFPS(TARGET_FPS);
            Fonte = Raylib.GetFontDefault();

            Raylib.SetTextureFilter(Fonte.Texture, TextureFilter.TEXTURE_FILTER_POINT);

            while (!Raylib.WindowShouldClose())
            {
                // get screen information
                {
#if ANDROID
                    ScreenWidth = (int)DeviceDisplay.MainDisplayInfo.Width;
                    ScreenHeight = (int)DeviceDisplay.MainDisplayInfo.Height - GetStatusBarHeight(activity);
#else
                    ScreenWidth = Raylib.GetScreenWidth();
                    ScreenHeight = Raylib.GetScreenHeight();
#endif
                    FontSize = (ScreenWidth < ScreenHeight ? ScreenWidth : ScreenHeight) / 20;
                    TouchCount = Raylib.GetTouchPointCount();
                    BorderThickness = Math.Max(ScreenWidth / 500, 1);
                    ShadowDistance = BorderThickness * 4;
                    Padding = BorderThickness * 8;
                }

                // get mouse information
                {
                    MouseX = Raylib.GetMouseX();
                    MouseY = Raylib.GetMouseY();

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

                    // set and draw grid
                    {
                        Layout.ShadowStyle GreyButtonShadow =
                            new(DarkerGray, ShadowDistance, Layout.ShadowKind.Pillar);
                        Layout.ShadowStyle RedButtonShadow =
                            new(Color.MAROON, ShadowDistance, Layout.ShadowKind.Pillar);
                        Layout.ShadowStyle GreenButtonShadow =
                            new(Color.DARKGREEN, ShadowDistance, Layout.ShadowKind.Pillar);

                        Layout.ButtonStyle GreyButton =
                            new(
                                TextColor: FontColor,
                                BackgroundColor: DarkGray,
                                PressedColor: DarkerGray,
                                HoveredColor: Color.GRAY,
                                BorderColor: Color.GRAY,
                                BorderThickness: BorderThickness,
                                ShadowStyle: GreyButtonShadow
                            );

                        Layout.ButtonStyle RedButton =
                            new(
                                TextColor: FontColor,
                                BackgroundColor: Color.RED,
                                PressedColor: Color.MAROON,
                                HoveredColor: Color.ORANGE,
                                BorderColor: Color.ORANGE,
                                BorderThickness: BorderThickness,
                                ShadowStyle: RedButtonShadow
                            );

                        Layout.ButtonStyle GreenButton =
                            new(
                                TextColor: FontColor,
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
                                new Layout.Button(20, "(", GreyButton),
                                new Layout.Button(20, ")", GreyButton),
                                new Layout.Button(20, ",", GreyButton),
                                new Layout.Button(20, "^", GreyButton),
                                new Layout.Button(20, "pi", GreyButton)
                            ),
                            new Layout.ButtonRow(
                                heightPercentage,
                                new Layout.Button(20, "!", GreyButton),
                                new Layout.Button(20, "e", GreyButton),
                                new Layout.Button(20, "%", GreyButton),
                                new Layout.Button(20, "/", GreyButton),
                                new Layout.Button(20, "C", GreyButton)
                            ),
                            new Layout.ButtonRow(
                                heightPercentage,
                                new Layout.Button(25, "7", GreyButton),
                                new Layout.Button(25, "8", GreyButton),
                                new Layout.Button(25, "9", GreyButton),
                                new Layout.Button(25, "*", GreyButton)
                            ),
                            new Layout.ButtonRow(
                                heightPercentage,
                                new Layout.Button(25, "4", GreyButton),
                                new Layout.Button(25, "5", GreyButton),
                                new Layout.Button(25, "6", GreyButton),
                                new Layout.Button(25, "-", GreyButton)
                            ),
                            new Layout.ButtonRow(
                                heightPercentage,
                                new Layout.Button(25, "1", GreyButton),
                                new Layout.Button(25, "2", GreyButton),
                                new Layout.Button(25, "3", GreyButton),
                                new Layout.Button(25, "+", GreyButton)
                            ),
                            new Layout.ButtonRow(
                                heightPercentage,
                                new Layout.Button(25, "0", GreyButton),
                                new Layout.Button(25, ".", GreyButton),
                                new Layout.Button(25, "<-", RedButton),
                                new Layout.Button(25, "=", GreenButton)
                            ),
                            new Layout.ButtonRow(
                                heightPercentage,
                                new Layout.Button(17, "sqrt", GreyButton),
                                new Layout.Button(17, "mod", GreyButton),
                                new Layout.Button(17, "sin", GreyButton),
                                new Layout.Button(17, "cos", GreyButton),
                                new Layout.Button(16, "tan", GreyButton),
                                new Layout.Button(16, "log", GreyButton)
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

                    // Calculator Display
                    {
                        int DisplayX = Padding;
                        int DisplayY = Padding;
                        int DisplayWidth = ScreenWidth - (Padding * 2);
                        int DisplayHeight = (ScreenHeight / 6) - Padding;

                        if (ErrorMessage == "")
                        {
                            Layout.DrawTextBox(
                                    DisplayX,
                                    DisplayY,
                                    DisplayWidth,
                                    DisplayHeight,
                                    Expression,
                                    Color.WHITE,
                                    DarkerGray,
                                    fontSize: FontSize,
                                    borderColor: DarkGray,
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
                                    Expression,
                                    Color.WHITE,
                                    DarkerGray,
                                    fontSize: FontSize,
                                    borderColor: Color.RED,
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

                    if (
                        Commands.TryGetValue(
                            ((char)Raylib.GetCharPressed()).ToString(),
                            out var command
                        )
                    )
                    {
                        command();
                        ButtonPressedTime = 0;
                    }
                    else if (Raylib.IsKeyPressed(KeyboardKey.KEY_ENTER))
                    {
                        Commands["="]();
                        ButtonPressedTime = 0;
                    }
                    else if (Raylib.IsKeyPressed(KeyboardKey.KEY_BACKSPACE))
                    {
                        Commands["<-"]();
                        ButtonPressedTime = 0;
                    }
                    else if (Raylib.IsKeyDown(KeyboardKey.KEY_BACKSPACE))
                    {
                        if (ButtonPressedTime >= UPDATE_INTERVAL)
                        {
                            Commands["<-"]();
                        }

                        ButtonPressedTime += Raylib.GetFrameTime();
                    }

                    Log.Message =
                        $"Resolution: {ScreenWidth}x{ScreenHeight}\nFPS: {Raylib.GetFPS()}\nMouseXY: {MouseX}x{MouseY}\n{Log.Message}";
                    Log.Draw();
                    Log.Message = "";

                    Raylib.EndDrawing();
                }
            }

            Raylib.CloseWindow();
        }
    }
}
