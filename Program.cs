// Licensed under the GPL3 or later versions of the GPL license.
// See the LICENSE file in the project root for more information.

using System;
using System.Numerics;

using Raylib_cs;

namespace Calculator;

internal struct Layout
{
    /// <summary>Tolerable difference in contrast between colors</summary>
    private const int CONTRAST_THRESHOLD = 128;

    internal readonly record struct Button(
        int WidthPercentage,
        string Text,
        int FontSize,
        Color TextColor,
        Color Color,
        Color? PressedColor = null,
        Color? HoveredColor = null,
        Color? ShadowColor = null,
        int ShadowDistance = 3,
        Color? BorderColor = null,
        int BorderThickness = 1
    );

    internal readonly record struct ButtonRow(int HeightPercentage, params Button[] Buttons);

    private static bool IsInsideRectangle(
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

    private static bool IsTextVisible(Color backgroundColor, Color textColor)
    {
        int rDiff = Math.Abs(backgroundColor.r - textColor.r);
        int gDiff = Math.Abs(backgroundColor.g - textColor.g);
        int bDiff = Math.Abs(backgroundColor.b - textColor.b);

        return (rDiff + gDiff + bDiff) > CONTRAST_THRESHOLD;
    }

    internal static void DrawTextBox(
        int x,
        int y,
        int width,
        int height,
        string text,
        int fontSize,
        Color textColor,
        Color color,
        Color? borderColor = null,
        int borderThickness = 1,
        Color? shadowColor = null,
        int shadowDistance = 3
    )
    {
        Vector2 textSize = Raylib.MeasureTextEx(Raylib.GetFontDefault(), text, fontSize, 2);

        if (x + textSize.X > x + width || y + textSize.Y > y + height)
        {
            Program.DebugInfo += $"ERROR: The text at the {x},{y} text box does not fit its box\n";
        }
        else if (!IsTextVisible(color, textColor))
        {
            Program.DebugInfo += $"ERROR: The text at the {x},{y} text box is not visible\n";
        }

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

        Raylib.DrawRectangle(x, y, width, height, color);

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
        Color color,
        Color? borderColor = null,
        Color? pressedColor = null,
        Color? hoveredColor = null,
        Color? shadowColor = null,
        int shadowDistance = 3
    )
    {
        if (
            pressedColor != null
            && Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT)
            && IsInsideRectangle(Program.MousePressedX, Program.MousePressedY, x, y, width, height)
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
                (Color)pressedColor,
                borderColor: borderColor
            );
        }
        else if (
            hoveredColor != null
            && IsInsideRectangle(Program.MouseX, Program.MouseY, x, y, width, height)
        )
        {
            DrawTextBox(
                x,
                y,
                width,
                height,
                text,
                fontSize,
                textColor,
                (Color)hoveredColor,
                shadowColor: shadowColor,
                shadowDistance: shadowDistance,
                borderColor: borderColor
            );
        }
        else
        {
            DrawTextBox(
                x,
                y,
                width,
                height,
                text,
                fontSize,
                textColor,
                color,
                shadowColor: shadowColor,
                shadowDistance: shadowDistance,
                borderColor: borderColor
            );
        }
    }

    internal static void ButtonGrid(
        int x,
        int y,
        int width,
        int height,
        int padding,
        params ButtonRow[] rows
    )
    {
        if (x < 0 || y < 0 || x + width > Program.ScreenWidth || y + height > Program.ScreenHeight)
        {
            Program.DebugInfo += "ERROR: Button grid is outside of the screen\n";
        }

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
                    rows[i].Buttons[j].Color,
                    hoveredColor: rows[i].Buttons[j].HoveredColor,
                    shadowColor: rows[i].Buttons[j].ShadowColor,
                    pressedColor: rows[i].Buttons[j].PressedColor,
                    borderColor: rows[i].Buttons[j].BorderColor,
                    shadowDistance: rows[i].Buttons[j].ShadowDistance
                );

                curX += colLength + padding;
                takenWidth += colLength;

                if (takenWidth > availableWidth)
                {
                    Program.DebugInfo +=
                        $"ERROR: Button grid {j + 1} column takes more than the available width\n";
                }
            }

            curY += rowLength + padding;
            takenHeight += rowLength;

            if (takenHeight > availableHeight)
            {
                Program.DebugInfo +=
                    $"ERROR: Button grid {i + 1} row takes more than the available height\n";
            }
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
                Color: DarkGray,
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
                Color: DarkGray,
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
                Color: DarkGray,
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
                Color: Color.RED,
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
                Color: DarkGray,
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
                Color: DarkGray,
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
                Color: DarkGray,
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
                Color: LightGreen,
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
                Color: DarkGray,
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
                Color: DarkGray,
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
                Color: DarkGray,
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
                Color: DarkGray,
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
                Color: DarkGray,
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
                Color: DarkGray,
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
                Color: DarkGray,
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
                Color: DarkGray,
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
                Color: DarkGray,
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
                Color: DarkGray,
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
                Color: DarkGray,
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
                Color: DarkGray,
                PressedColor: DarkerGray,
                HoveredColor: Color.GRAY,
                BorderColor: Color.GRAY,
                ShadowColor: DarkerGray
            )
        )
    };

    internal static int ScreenWidth;
    internal static int ScreenHeight;

    internal static int MouseX;
    internal static int MouseY;
    internal static int MousePressedX;
    internal static int MousePressedY;

    internal static string DebugInfo;
    internal static Color DebugTextColor = Color.RED;

    internal static void Main()
    {
        // Get Screen Resolution
        (ScreenWidth, ScreenHeight) = Utils.GetScreenResolution();

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

                    Layout.ButtonGrid(
                        SCREEN_PADDING,
                        SCREEN_PADDING + (ScreenHeight / 6),
                        ScreenWidth - (SCREEN_PADDING * 2),
                        ScreenHeight - (ScreenHeight / 6) - (SCREEN_PADDING * 2),
                        SCREEN_PADDING,
                        ButtonGrid
                    );

                    // TODO(LucasTA): remove debug stuff from release builds
                    Raylib.DrawText(
                        $"FPS: {Raylib.GetFPS()}\nMouseXY: {MouseX}x{MouseY}\n{DebugInfo}",
                        0,
                        0,
                        (int)(FONT_SIZE / 1.5),
                        DebugTextColor
                    );
                    DebugInfo = "";

                    Raylib.EndDrawing();
                }
            }

            Raylib.CloseWindow();
        }
    }
}
