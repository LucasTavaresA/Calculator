// Licensed under the GPL3 or later versions of the GPL license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Numerics;

using Raylib_cs;

namespace Calculator;

internal struct Log
{
    /// <summary>Tolerable difference between colors</summary>
    private const int CONTRAST_LIMIT = 128;

    internal static string Message;

    [Conditional("DEBUG")]
    internal static void Draw()
    {
        Raylib.DrawText(Message, 0, 0, 20, Color.RED);
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
        int rDiff = Math.Abs(backgroundColor.r - textColor.r);
        int gDiff = Math.Abs(backgroundColor.g - textColor.g);
        int bDiff = Math.Abs(backgroundColor.b - textColor.b);

        if ((rDiff + gDiff + bDiff) < CONTRAST_LIMIT)
        {
            Message += message;
        }
    }
}

internal struct Layout
{
    internal readonly record struct Button(
        int WidthPercentage,
        string Text,
        int FontSize,
        Color TextColor,
        Color BackgroundColor,
        Color PressedColor,
        Color HoveredColor,
        Color? ShadowColor = null,
        int ShadowDistance = 3,
        Color? BorderColor = null,
        int BorderThickness = 1
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

    // TODO(LucasTA): Allow 2.5D Shadows here, will require some shadowKind variable,
    // maybe just a better way to define all those colors and attributes?
    internal static void DrawTextBox(
        int x,
        int y,
        int width,
        int height,
        string text,
        int fontSize,
        Color textColor,
        Color backgroundColor,
        Color? borderColor = null,
        int borderThickness = 1,
        Color? shadowColor = null,
        int shadowDistance = 3
    )
    {
        Vector2 textSize = Raylib.MeasureTextEx(Raylib.GetFontDefault(), text, fontSize, 2);

        Log.IfTrue(
            textSize.X > width || textSize.Y > height,
            $"ERROR: The text at the {x},{y} text box does not fit its box\n"
        );
        Log.IfBadContrast(
            backgroundColor,
            textColor,
            $"ERROR: The text at the {x},{y} text box is not visible\n"
        );

        int textX = x + ((width - (int)textSize.X) / 2);
        int textY = y + ((height - (int)textSize.Y) / 2);

        if (shadowColor != null)
        {
            Raylib.DrawRectangle(
                x + shadowDistance,
                y + shadowDistance,
                width,
                height,
                (Color)shadowColor
            );
        }

        Raylib.DrawRectangle(x, y, width, height, backgroundColor);

        if (borderColor != null)
        {
            Raylib.DrawRectangleLinesEx(
                new(x, y, width, height),
                borderThickness,
                (Color)borderColor
            );
        }

        Raylib.DrawText(text, textX, textY, fontSize, textColor);
    }

    internal static void DrawButton(
        int x,
        int y,
        int width,
        int height,
        string text,
        int fontSize,
        Color textColor,
        Color backgroundColor,
        Color pressedColor,
        Color hoveredColor,
        Color? borderColor = null,
        Color? shadowColor = null,
        int shadowDistance = 3,
        int borderThickness = 1
    )
    {
        if (
            Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT)
            && IsPointInsideRect(Program.MousePressedX, Program.MousePressedY, x, y, width, height)
        )
        {
            DrawTextBox(
                x + shadowDistance,
                y + shadowDistance,
                width,
                height,
                text,
                fontSize,
                textColor,
                pressedColor,
                borderColor: borderColor,
                borderThickness: borderThickness
            );
        }
        else
        {
            if (shadowColor != null)
            {
                Draw2DCubeShadow(x, y, width, height, (Color)shadowColor, borderColor);
            }

            DrawTextBox(
                x,
                y,
                width,
                height,
                text,
                fontSize,
                textColor,
                IsPointInsideRect(Program.MouseX, Program.MouseY, x, y, width, height)
                    ? hoveredColor
                    : backgroundColor,
                borderColor: borderColor,
                borderThickness: borderThickness
            );
        }
    }

    /// <summary>
    /// Draw square shadow with corners to make it look like a 3D cube,
    /// triangles for the shadows, lines for the 3D borders
    /// </summary>
    internal static void Draw2DCubeShadow(
        int x,
        int y,
        int width,
        int height,
        Color color,
        Color? outlineColor = null,
        int distance = 3
    )
    {
        Raylib.DrawRectangle(x + distance, y + distance, width, height, color);

        // top right corner
        Raylib.DrawTriangle(
            new(x + distance, y + height),
            new(x, y + height),
            new(x + distance, y + height + distance),
            color
        );

        // bottom left corner
        Raylib.DrawTriangle(
            new(x + width, y),
            new(x + width, y + distance),
            new(x + width + distance, y + distance),
            color
        );

        if (outlineColor != null)
        {
            // top right outline
            Raylib.DrawLine(x + width, y, x + width + distance, y + distance, (Color)outlineColor);

            // bottom left outline
            Raylib.DrawLine(
                x,
                y + height,
                x + distance,
                y + height + distance,
                (Color)outlineColor
            );

            // bottom right outline
            Raylib.DrawLine(
                x + width,
                y + height,
                x + width + distance,
                y + height + distance,
                (Color)outlineColor
            );
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
                || x + width > Program.ScreenWidth
                || y + height > Program.ScreenHeight,
            "ERROR: Button grid is outside of the screen\n"
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
                    rows[i].Buttons[j].FontSize,
                    rows[i].Buttons[j].TextColor,
                    rows[i].Buttons[j].BackgroundColor,
                    hoveredColor: rows[i].Buttons[j].HoveredColor,
                    shadowColor: rows[i].Buttons[j].ShadowColor,
                    pressedColor: rows[i].Buttons[j].PressedColor,
                    borderColor: rows[i].Buttons[j].BorderColor,
                    shadowDistance: rows[i].Buttons[j].ShadowDistance,
                    borderThickness: rows[i].Buttons[j].BorderThickness
                );

                curX += colLength + padding;
                takenWidth += colLength;

                Log.IfTrue(
                    takenWidth > availableWidth,
                    $"ERROR: Button grid {j + 1} column takes more than the available width\n"
                );
            }

            curY += rowLength + padding;
            takenHeight += rowLength;

            Log.IfTrue(
                takenHeight > availableHeight,
                $"ERROR: Button grid {i + 1} row takes more than the available height\n"
            );
        }
    }
}

internal struct Program
{
    private const string APP_NAME = "Calculator";
    private const int TARGET_FPS = 60;
    private const int FONT_SIZE = 30;
    private const int SCREEN_PADDING = 10;

    private static readonly Color BackgroundColor = Color.BLACK;
    private static readonly Color FontColor = Color.WHITE;
    private static readonly Color DarkerGray = new(60, 60, 60, 255);
    private static readonly Color DarkGray = new(100, 100, 100, 255);
    private static readonly Color LightGreen = new(0, 193, 47, 255);

    private static readonly Layout.ButtonRow[] ButtonGrid = new Layout.ButtonRow[]
    {
        new Layout.ButtonRow(
            20,
            new Layout.Button(
                25,
                "(",
                FONT_SIZE,
                TextColor: FontColor,
                BackgroundColor: DarkGray,
                PressedColor: DarkerGray,
                HoveredColor: Color.GRAY,
                BorderColor: Color.GRAY,
                ShadowColor: DarkerGray
            ),
            new Layout.Button(
                25,
                ")",
                FONT_SIZE,
                TextColor: FontColor,
                BackgroundColor: DarkGray,
                PressedColor: DarkerGray,
                HoveredColor: Color.GRAY,
                BorderColor: Color.GRAY,
                ShadowColor: DarkerGray
            ),
            new Layout.Button(
                25,
                "C",
                FONT_SIZE,
                TextColor: FontColor,
                BackgroundColor: DarkGray,
                PressedColor: DarkerGray,
                HoveredColor: Color.GRAY,
                BorderColor: Color.GRAY,
                ShadowColor: DarkerGray
            // TODO(LucasTA): if more buttons are needed i can do like those
            // old phone keys
            // HoldText: "AC"
            ),
            new Layout.Button(
                25,
                "<-",
                FONT_SIZE,
                TextColor: FontColor,
                BackgroundColor: Color.RED,
                PressedColor: Color.MAROON,
                HoveredColor: Color.ORANGE,
                BorderColor: Color.ORANGE,
                ShadowColor: Color.MAROON
            )
        ),
        new Layout.ButtonRow(
            20,
            new Layout.Button(
                25,
                "7",
                FONT_SIZE,
                TextColor: FontColor,
                BackgroundColor: DarkGray,
                PressedColor: DarkerGray,
                HoveredColor: Color.GRAY,
                BorderColor: Color.GRAY,
                ShadowColor: DarkerGray
            ),
            new Layout.Button(
                25,
                "8",
                FONT_SIZE,
                TextColor: FontColor,
                BackgroundColor: DarkGray,
                PressedColor: DarkerGray,
                HoveredColor: Color.GRAY,
                BorderColor: Color.GRAY,
                ShadowColor: DarkerGray
            ),
            new Layout.Button(
                25,
                "9",
                FONT_SIZE,
                TextColor: FontColor,
                BackgroundColor: DarkGray,
                PressedColor: DarkerGray,
                HoveredColor: Color.GRAY,
                BorderColor: Color.GRAY,
                ShadowColor: DarkerGray
            ),
            new Layout.Button(
                25,
                "=",
                FONT_SIZE,
                TextColor: FontColor,
                BackgroundColor: LightGreen,
                PressedColor: Color.DARKGREEN,
                HoveredColor: Color.GREEN,
                BorderColor: Color.GREEN,
                ShadowColor: Color.DARKGREEN
            )
        ),
        new Layout.ButtonRow(
            20,
            new Layout.Button(
                25,
                "4",
                FONT_SIZE,
                TextColor: FontColor,
                BackgroundColor: DarkGray,
                PressedColor: DarkerGray,
                HoveredColor: Color.GRAY,
                BorderColor: Color.GRAY,
                ShadowColor: DarkerGray
            ),
            new Layout.Button(
                25,
                "5",
                FONT_SIZE,
                TextColor: FontColor,
                BackgroundColor: DarkGray,
                PressedColor: DarkerGray,
                HoveredColor: Color.GRAY,
                BorderColor: Color.GRAY,
                ShadowColor: DarkerGray
            ),
            new Layout.Button(
                25,
                "6",
                FONT_SIZE,
                TextColor: FontColor,
                BackgroundColor: DarkGray,
                PressedColor: DarkerGray,
                HoveredColor: Color.GRAY,
                BorderColor: Color.GRAY,
                ShadowColor: DarkerGray
            ),
            new Layout.Button(
                25,
                "/",
                FONT_SIZE,
                TextColor: FontColor,
                BackgroundColor: DarkGray,
                PressedColor: DarkerGray,
                HoveredColor: Color.GRAY,
                BorderColor: Color.GRAY,
                ShadowColor: DarkerGray
            )
        ),
        new Layout.ButtonRow(
            20,
            new Layout.Button(
                25,
                "1",
                FONT_SIZE,
                TextColor: FontColor,
                BackgroundColor: DarkGray,
                PressedColor: DarkerGray,
                HoveredColor: Color.GRAY,
                BorderColor: Color.GRAY,
                ShadowColor: DarkerGray
            ),
            new Layout.Button(
                25,
                "2",
                FONT_SIZE,
                TextColor: FontColor,
                BackgroundColor: DarkGray,
                PressedColor: DarkerGray,
                HoveredColor: Color.GRAY,
                BorderColor: Color.GRAY,
                ShadowColor: DarkerGray
            ),
            new Layout.Button(
                25,
                "3",
                FONT_SIZE,
                TextColor: FontColor,
                BackgroundColor: DarkGray,
                PressedColor: DarkerGray,
                HoveredColor: Color.GRAY,
                BorderColor: Color.GRAY,
                ShadowColor: DarkerGray
            ),
            new Layout.Button(
                25,
                "*",
                FONT_SIZE,
                TextColor: FontColor,
                BackgroundColor: DarkGray,
                PressedColor: DarkerGray,
                HoveredColor: Color.GRAY,
                BorderColor: Color.GRAY,
                ShadowColor: DarkerGray
            )
        ),
        new Layout.ButtonRow(
            20,
            new Layout.Button(
                25,
                "0",
                FONT_SIZE,
                TextColor: FontColor,
                BackgroundColor: DarkGray,
                PressedColor: DarkerGray,
                HoveredColor: Color.GRAY,
                BorderColor: Color.GRAY,
                ShadowColor: DarkerGray
            ),
            new Layout.Button(
                25,
                ".",
                FONT_SIZE,
                TextColor: FontColor,
                BackgroundColor: DarkGray,
                PressedColor: DarkerGray,
                HoveredColor: Color.GRAY,
                BorderColor: Color.GRAY,
                ShadowColor: DarkerGray
            ),
            new Layout.Button(
                25,
                "+",
                FONT_SIZE,
                TextColor: FontColor,
                BackgroundColor: DarkGray,
                PressedColor: DarkerGray,
                HoveredColor: Color.GRAY,
                BorderColor: Color.GRAY,
                ShadowColor: DarkerGray
            ),
            new Layout.Button(
                25,
                "-",
                FONT_SIZE,
                TextColor: FontColor,
                BackgroundColor: DarkGray,
                PressedColor: DarkerGray,
                HoveredColor: Color.GRAY,
                BorderColor: Color.GRAY,
                ShadowColor: DarkerGray
            )
        )
    };

    internal static int ScreenWidth = 1024;
    internal static int ScreenHeight = 768;

    internal static int MouseX;
    internal static int MouseY;
    internal static int MousePressedX;
    internal static int MousePressedY;

    internal static void Main()
    {
        // Raylib context
        {
            Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
            Raylib.InitWindow(ScreenWidth, ScreenHeight, APP_NAME);
            Raylib.SetTargetFPS(TARGET_FPS);

            while (!Raylib.WindowShouldClose())
            {
                // get screen information
                {
                    ScreenWidth = Raylib.GetScreenWidth();
                    ScreenHeight = Raylib.GetScreenHeight();
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

                    Layout.DrawButtonGrid(
                        SCREEN_PADDING,
                        SCREEN_PADDING + (ScreenHeight / 6),
                        ScreenWidth - (SCREEN_PADDING * 2),
                        ScreenHeight - (ScreenHeight / 6) - (SCREEN_PADDING * 2),
                        SCREEN_PADDING,
                        ButtonGrid
                    );

                    Log.Message =
                        $"FPS: {Raylib.GetFPS()}\nMouseXY: {MouseX}x{MouseY}\n{Log.Message}";
                    Log.Draw();
                    Log.Message = "";

                    Raylib.EndDrawing();
                }
            }

            Raylib.CloseWindow();
        }
    }
}
