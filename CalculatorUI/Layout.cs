using System;
using System.Diagnostics;
using System.Numerics;

using Raylib_cs;

namespace Calculator;

internal readonly struct Layout
{
	internal static readonly Rectangle ICON_RECTANGLE = new(0, 0, 160, 160);

	internal enum ShadowKind
	{
		Float = 0,
		Cast = 1,
		Pillar = 2,
		Cube = 3,
		TransparentCube = 4,
	}

	internal enum TextAlignment
	{
		Center,
		TopLeft,
		Left,
		BottomLeft,
		TopRight,
		Right,
		BottomRight
	}

	internal enum ButtonPressMode
	{
		Once,
		HoldToRepeat,
		HoldToPress,
	}

	internal enum OverflowMode
	{
		Overflow,
		Truncate,
		Shrink,
	}

	internal readonly record struct TextFormat(
		string Text,
		int FontSize,
		Color TextColor,
		TextAlignment Alignment = TextAlignment.Center,
		OverflowMode Overflow = OverflowMode.Overflow
	);

	internal readonly record struct ShadowStyle(
		Color Color,
		int Distance,
		ShadowKind Kind = ShadowKind.Float
	);

	internal record struct Icon(Texture2D Texture, Color Tint, int Size = 0);

	internal readonly record struct ButtonStyle(
		Color BackgroundColor,
		Color PressedColor,
		Color HoveredColor,
		Color? BorderColor = null,
		int BorderThickness = 1,
		ShadowStyle? ShadowStyle = null,
		Icon? Icon = null
	);

	internal readonly record struct Button(
		int WidthPercentage,
		TextFormat? TextFormat,
		Action Callback,
		ButtonStyle Style,
		ButtonPressMode PressMode = ButtonPressMode.Once
	);

	internal readonly record struct ButtonRow(int HeightPercentage, params Button[] Buttons);

	internal static bool IsPointInsideRect(
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
			Raylib.DrawRectangleLinesEx(new(x, y, width, height), borderThickness, bc);
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
		int fontSize,
		TextAlignment alignment = TextAlignment.Center,
		OverflowMode overflow = OverflowMode.Overflow
	)
	{
		Vector2 textSize = Raylib.MeasureTextEx(
			CalculatorUI.Fonte,
			text,
			(int)fontSize,
			CalculatorUI.FONT_SPACING
		);

		Log.IfTrue(
			textSize.X > width || textSize.Y > height,
			$"ERROR: The text at the {x},{y} text box does not fit its box!\n"
		);

		Log.IfBadContrast(
			backgroundColor,
			textColor,
			$"ERROR: The text at the {x},{y} text box is not visible!\n"
		);

		int textX;
		int textY;

		switch (alignment)
		{
			case TextAlignment.TopLeft:
				textX = x;
				textY = y;
				break;
			case TextAlignment.Left:
				textX = x;
				textY = y + (height - (int)textSize.Y) / 2;
				break;
			case TextAlignment.BottomLeft:
				textX = x;
				textY = y + height - (int)textSize.Y;
				break;
			case TextAlignment.TopRight:
				textX = x + (width - (int)textSize.X);
				textY = y;
				break;
			case TextAlignment.Right:
				textX = x + (width - (int)textSize.X);
				textY = y + (height - (int)textSize.Y) / 2;
				break;
			case TextAlignment.BottomRight:
				textX = x + (width - (int)textSize.X);
				textY = y + (height - (int)textSize.Y);
				break;
			default:
				// FIXME(LucasTA): Center alignment with OverflowMode.Truncate still
				// 								 overflows textbox is smaller than the text
				textX = x + (width - (int)textSize.X) / 2;
				textY = y + (height - (int)textSize.Y) / 2;
				break;
		}

		switch (overflow)
		{
			case OverflowMode.Overflow:
				Raylib.DrawTextEx(
					CalculatorUI.Fonte,
					text,
					new(textX, textY),
					(int)fontSize,
					CalculatorUI.FONT_SPACING,
					textColor
				);
				break;
			case OverflowMode.Truncate:
				Vector2 charSize = Raylib.MeasureTextEx(
					CalculatorUI.Fonte,
					"-",
					(int)fontSize,
					CalculatorUI.FONT_SPACING
				);
				int charsLimit = width / (int)Math.Ceiling(charSize.X + CalculatorUI.FONT_SPACING);

				if (text.Length > charsLimit)
				{
					text = text[..Math.Max(0, charsLimit - 3)] + "...";
				}

				if (charsLimit > 3)
				{
					Raylib.DrawTextEx(
						CalculatorUI.Fonte,
						text,
						new(textX, textY),
						(int)fontSize,
						CalculatorUI.FONT_SPACING,
						textColor
					);
				}
				break;
			case OverflowMode.Shrink:
				// HACK(LucasTA): probably slow and terrible
				while (textSize.X > width)
				{
					fontSize -= 1;

					textSize = Raylib.MeasureTextEx(
						CalculatorUI.Fonte,
						text,
						(int)fontSize,
						CalculatorUI.FONT_SPACING
					);
				}

				Raylib.DrawTextEx(
					CalculatorUI.Fonte,
					text,
					new(textX, textY),
					(int)fontSize,
					CalculatorUI.FONT_SPACING,
					textColor
				);
				break;
			default:
				throw new UnreachableException("Unknown overflow mode");
		}
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
		DrawText(
			x,
			y,
			width,
			height,
			textFormat.Text,
			textFormat.TextColor,
			backgroundColor,
			textFormat.FontSize,
			textFormat.Alignment,
			textFormat.Overflow
		);
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
		ButtonPressMode pressMode = ButtonPressMode.Once,
		Icon? icon = null,
		Color? borderColor = null,
		int borderThickness = 1,
		ShadowStyle? shadowStyle = null
	)
	{
		if (
			!CalculatorUI.Dragging
			&& !CalculatorUI.ButtonWasPressed
			&& Raylib.IsMouseButtonReleased(MouseButton.MOUSE_BUTTON_LEFT)
			&& IsPointInsideRect(CalculatorUI.MouseX, CalculatorUI.MouseY, x, y, width, height)
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
			CalculatorUI.ButtonWasHeldPressed = false;
			CalculatorUI.ButtonWasPressed = true;
			CalculatorUI.ButtonPressedTime = 0;
			CalculatorUI.KeyRepeatInterval = CalculatorUI.INITIAL_REPEAT_INTERVAL;

			if (pressMode != ButtonPressMode.HoldToPress)
			{
				callback();
			}
		}
		else if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT))
		{
			if (IsPointInsideRect(CalculatorUI.MouseX, CalculatorUI.MouseY, x, y, width, height))
			{
				if (
					!CalculatorUI.Dragging
					&& !CalculatorUI.ButtonWasHeldPressed
					&& !CalculatorUI.ButtonWasPressed
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
					if (shadowStyle is ShadowStyle ss)
					{
						x += ss.Distance;
						y += ss.Distance;
					}

					int progress = height;

					if (pressMode == Layout.ButtonPressMode.HoldToPress)
					{
						progress = (int)(
							height
							/ CalculatorUI.ButtonHoldToPressTime
							* CalculatorUI.ButtonPressedTime
						);
					}

					DrawBox(
						x,
						y + height - progress,
						width,
						progress,
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
							tf.Text,
							tf.TextColor,
							pressedColor,
							tf.FontSize,
							tf.Alignment,
							tf.Overflow
						);
					}

					CalculatorUI.ButtonPressedTime += Raylib.GetFrameTime();

					if (
						pressMode == ButtonPressMode.HoldToRepeat
						&& CalculatorUI.ButtonPressedTime >= CalculatorUI.KeyRepeatInterval
					)
					{
						CalculatorUI.ButtonWasPressed = true;
						CalculatorUI.ButtonPressedTime = 0;
						CalculatorUI.KeyRepeatInterval = Math.Max(
							CalculatorUI.KeyRepeatInterval * CalculatorUI.INITIAL_REPEAT_INTERVAL,
							CalculatorUI.MIN_REPEAT_INTERVAL
						);
						callback();
					}
					else if (
						pressMode == ButtonPressMode.HoldToPress
						&& CalculatorUI.ButtonPressedTime >= CalculatorUI.ButtonHoldToPressTime
					)
					{
						CalculatorUI.ButtonWasHeldPressed = true;
						CalculatorUI.ButtonWasPressed = true;
						CalculatorUI.ButtonPressedTime = 0;
						CalculatorUI.KeyRepeatInterval = CalculatorUI.INITIAL_REPEAT_INTERVAL;

						callback();
					}
				}
				else
				{
					backgroundColor =
#if ANDROID
						CalculatorUI.TouchCount > 0 ? hoveredColor : backgroundColor;
#else
					hoveredColor;
#endif

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
							tf.Text,
							tf.TextColor,
							backgroundColor,
							tf.FontSize,
							tf.Alignment,
							tf.Overflow
						);
					}
				}
			}
			else
			{
				if (
					!CalculatorUI.Dragging
					&& !CalculatorUI.ButtonWasHeldPressed
					&& !CalculatorUI.ButtonWasPressed
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
					if (shadowStyle is ShadowStyle ss)
					{
						x += ss.Distance;
						y += ss.Distance;
					}

					int progress = height;

					if (pressMode == Layout.ButtonPressMode.HoldToPress)
					{
						progress = (int)(
							height
							/ CalculatorUI.ButtonHoldToPressTime
							* CalculatorUI.ButtonPressedTime
						);
					}

					DrawBox(
						x,
						y + height - progress,
						width,
						progress,
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
							tf.Text,
							tf.TextColor,
							pressedColor,
							tf.FontSize,
							tf.Alignment,
							tf.Overflow
						);
					}

					CalculatorUI.ButtonPressedTime += Raylib.GetFrameTime();

					if (
						pressMode == ButtonPressMode.HoldToRepeat
						&& CalculatorUI.ButtonPressedTime >= CalculatorUI.KeyRepeatInterval
					)
					{
						CalculatorUI.ButtonWasPressed = true;
						CalculatorUI.ButtonPressedTime = 0;
						CalculatorUI.KeyRepeatInterval = Math.Max(
							CalculatorUI.KeyRepeatInterval * CalculatorUI.INITIAL_REPEAT_INTERVAL,
							CalculatorUI.MIN_REPEAT_INTERVAL
						);
						callback();
					}
					else if (
						pressMode == ButtonPressMode.HoldToPress
						&& CalculatorUI.ButtonPressedTime >= CalculatorUI.ButtonHoldToPressTime
					)
					{
						CalculatorUI.ButtonWasHeldPressed = true;
						CalculatorUI.ButtonWasPressed = true;
						CalculatorUI.ButtonPressedTime = 0;
						CalculatorUI.KeyRepeatInterval = CalculatorUI.INITIAL_REPEAT_INTERVAL;

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
							tf.Text,
							tf.TextColor,
							backgroundColor,
							tf.FontSize,
							tf.Alignment,
							tf.Overflow
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
#if ANDROID
					CalculatorUI.TouchCount > 0 ? hoveredColor : backgroundColor;
#else
				hoveredColor;
#endif

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
						tf.Text,
						tf.TextColor,
						backgroundColor,
						tf.FontSize,
						tf.Alignment,
						tf.Overflow
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
						tf.Text,
						tf.TextColor,
						backgroundColor,
						tf.FontSize,
						tf.Alignment,
						tf.Overflow
					);
				}
			}
		}

		if (icon is Icon i)
		{
			// if size is not set, scale the icon to fit the rectangle and center
			i.Size = i.Size > 0 ? i.Size : Math.Min(width, height);

			// Draw the texture
			Raylib.DrawTexturePro(
				i.Texture,
				ICON_RECTANGLE,
				new(x + width / 2 - i.Size / 2, y + height / 2 - i.Size / 2, i.Size, i.Size),
				new(0, 0),
				0,
				i.Tint
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
					rows[i].Buttons[j].PressMode,
					rows[i].Buttons[j].Style.Icon,
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
