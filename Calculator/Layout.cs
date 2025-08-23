// Licensed under the GPL3 or later versions of the GPL license.
// See the LICENSE file in the project root for more information.

using System;
using System.Numerics;

using Raylib_cs;

namespace Calculator;

// TODO(LucasTA): Make a container with things inside like approach,
// so i can draw multiple things inside it without calling DrawBox and DrawText separately,
// also would make customizing the box behind both easier since currently we use transparent backgrounds
// also would make dealing with the internal padding caused by the border easier
// also try to deal with text on both sides of the box and overflow properly
// TODO(LucasTA): Add OverflowMode.Wrap
// TODO(LucasTA): Allow getting the index for row/column when drawing a button grid
// TODO(LucasTA): Have a CalculateScroll() function to reduce repetition
internal readonly struct Layout
{
	/// <summary>Tolerable difference between colors</summary>
	private const int CONTRAST_LIMIT = 70;

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
		BottomRight,
		Top,
		Bottom,
	}

	internal enum ButtonPressMode
	{
		Once,
		HoldToRepeat,
		HoldToPress,
	}

	internal enum OverflowMode
	{
		Shrink,
		Overflow,
		Truncate,
	}

	internal readonly record struct TextFormat(
		string Text,
		int FontSize,
		Color TextColor,
		TextAlignment Alignment = TextAlignment.Center,
		OverflowMode Overflow = OverflowMode.Shrink
	);

	internal readonly record struct ShadowStyle(
		Color Color,
		int Distance,
		ShadowKind Kind = ShadowKind.Float
	);

	internal readonly record struct BorderStyle(Color Color, int Thickness = 1);

	internal record struct Icon(Texture2D Texture, Color Tint, int Size = 0);

	internal readonly record struct ButtonStyle(
		Color BackgroundColor,
		Color PressedColor,
		Color HoveredColor,
		BorderStyle? BorderStyle = null,
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

	internal static bool IsBadContrast(Color backgroundColor, Color textColor)
	{
		int rDiff = Math.Abs(backgroundColor.R - textColor.R);
		int gDiff = Math.Abs(backgroundColor.G - textColor.G);
		int bDiff = Math.Abs(backgroundColor.B - textColor.B);

		if ((rDiff + gDiff + bDiff) / 3 < CONTRAST_LIMIT)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

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
		BorderStyle? borderStyle = null,
		ShadowStyle? shadowStyle = null
	)
	{
		if (shadowStyle is ShadowStyle ss)
		{
			DrawShadow(x, y, width, height, ss, borderStyle);
		}

		Raylib.DrawRectangle(x, y, width, height, backgroundColor);

		if (borderStyle is BorderStyle bs)
		{
			Raylib.DrawRectangleLinesEx(new(x, y, width, height), bs.Thickness, bs.Color);
		}
	}

	internal static void DrawText(
		int x,
		int y,
		int width,
		int height,
		int padding,
		string text,
		Color textColor,
		Color backgroundColor,
		int fontSize,
		TextAlignment alignment = TextAlignment.Center,
		OverflowMode overflow = OverflowMode.Shrink
	)
	{
		x += padding;
		y += padding;
		width -= padding * 2;
		height -= padding * 2;

		Vector2 textSize = Raylib.MeasureTextEx(
			Calculator.Fonte,
			text,
			fontSize,
			Calculator.FONT_SPACING
		);

		// FIXME(LucasTA): stop checking this here when containers are added,
		// keeping this here for now due to this functions doing one thing pattern
		// and i don't want to make DrawTextBox not receive text sometimes
		Debug.IfDrawPoint(
			IsBadContrast(backgroundColor, textColor),
			$"ERROR: The text at the {x},{y} text box is not visible!\n",
			x,
			y
		);

		switch (overflow)
		{
			case OverflowMode.Truncate:
				Vector2 charSize = Raylib.MeasureTextEx(
					Calculator.Fonte,
					"-",
					fontSize,
					Calculator.FONT_SPACING
				);
				int charsLimit = width / (int)Math.Ceiling(charSize.X + Calculator.FONT_SPACING);

				if (charsLimit <= 3)
				{
					text = text[..charsLimit];
				}
				else if (text.Length > charsLimit)
				{
					text = text[..Math.Max(0, charsLimit - 3)] + "...";
				}

				textSize = Raylib.MeasureTextEx(
					Calculator.Fonte,
					text,
					fontSize,
					Calculator.FONT_SPACING
				);
				break;
			case OverflowMode.Shrink:
				// HACK(LucasTA): This will blow up someday
				while (textSize.X > width || textSize.Y > height)
				{
					fontSize -= 1;

					textSize = Raylib.MeasureTextEx(
						Calculator.Fonte,
						text,
						fontSize,
						Calculator.FONT_SPACING
					);
				}
				break;
		}

		Debug.IfDrawPoint(
			textSize.X > width || textSize.Y > height,
			$"ERROR: The text at the {x},{y} text box does not fit its box!\n",
			x,
			y
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
			case TextAlignment.Top:
				textX = x + (width - (int)textSize.X) / 2;
				textY = y;
				break;
			case TextAlignment.Bottom:
				textX = x + (width - (int)textSize.X) / 2;
				textY = y + (height - (int)textSize.Y);
				break;
			default:
				textX = x + (width - (int)textSize.X) / 2;
				textY = y + (height - (int)textSize.Y) / 2;
				break;
		}

		Raylib.DrawTextEx(
			Calculator.Fonte,
			text,
			new(textX, textY),
			fontSize,
			Calculator.FONT_SPACING,
			textColor
		);
	}

	internal static void DrawTextBox(
		int x,
		int y,
		int width,
		int height,
		TextFormat textFormat,
		Color backgroundColor,
		BorderStyle? borderStyle = null,
		ShadowStyle? shadowStyle = null
	)
	{
		DrawBox(x, y, width, height, backgroundColor, borderStyle, shadowStyle);
		DrawText(
			x,
			y,
			width,
			height,
			borderStyle?.Thickness ?? 0,
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
		BorderStyle? borderStyle = null,
		ShadowStyle? shadowStyle = null
	)
	{
		if (
			!Calculator.Dragging
			&& !Calculator.ButtonWasPressed
			&& Raylib.IsMouseButtonReleased(MouseButton.MOUSE_BUTTON_LEFT)
			&& IsPointInsideRect(Calculator.MouseX, Calculator.MouseY, x, y, width, height)
			&& IsPointInsideRect(
				Calculator.MousePressedX,
				Calculator.MousePressedY,
				x,
				y,
				width,
				height
			)
		)
		{
			Calculator.ButtonWasHeldPressed = false;
			Calculator.ButtonWasPressed = true;
			Calculator.ButtonPressedTime = 0;
			Calculator.KeyRepeatInterval = Calculator.INITIAL_REPEAT_INTERVAL;

			if (pressMode != ButtonPressMode.HoldToPress)
			{
				callback();
			}
		}
		else if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT))
		{
			if (IsPointInsideRect(Calculator.MouseX, Calculator.MouseY, x, y, width, height))
			{
				if (
					!Calculator.Dragging
					&& !Calculator.ButtonWasHeldPressed
					&& !Calculator.ButtonWasPressed
					&& IsPointInsideRect(
						Calculator.MousePressedX,
						Calculator.MousePressedY,
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

					if (pressMode == ButtonPressMode.HoldToPress)
					{
						progress = (int)(
							height / Calculator.ButtonHoldToPressTime * Calculator.ButtonPressedTime
						);
					}

					DrawBox(x, y + height - progress, width, progress, pressedColor, borderStyle);

					if (textFormat is TextFormat tf)
					{
						DrawText(
							x,
							y,
							width,
							height,
							borderStyle?.Thickness ?? 0,
							tf.Text,
							tf.TextColor,
							pressedColor,
							tf.FontSize,
							tf.Alignment,
							tf.Overflow
						);
					}

					Calculator.ButtonPressedTime += Raylib.GetFrameTime();

					if (
						pressMode == ButtonPressMode.HoldToRepeat
						&& Calculator.ButtonPressedTime >= Calculator.KeyRepeatInterval
					)
					{
						Calculator.ButtonWasPressed = true;
						Calculator.ButtonPressedTime = 0;
						Calculator.KeyRepeatInterval = Math.Max(
							Calculator.KeyRepeatInterval * Calculator.INITIAL_REPEAT_INTERVAL,
							Calculator.MIN_REPEAT_INTERVAL
						);
						callback();
					}
					else if (
						pressMode == ButtonPressMode.HoldToPress
						&& Calculator.ButtonPressedTime >= Calculator.ButtonHoldToPressTime
					)
					{
						Calculator.ButtonWasHeldPressed = true;
						Calculator.ButtonWasPressed = true;
						Calculator.ButtonPressedTime = 0;
						Calculator.KeyRepeatInterval = Calculator.INITIAL_REPEAT_INTERVAL;

						callback();
					}
				}
				else
				{
					backgroundColor =
#if ANDROID
						Calculator.TouchCount > 0 ? hoveredColor : backgroundColor;
#else
					hoveredColor;
#endif

					DrawBox(x, y, width, height, backgroundColor, borderStyle, shadowStyle);

					if (textFormat is TextFormat tf)
					{
						DrawText(
							x,
							y,
							width,
							height,
							borderStyle?.Thickness ?? 0,
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
					!Calculator.Dragging
					&& !Calculator.ButtonWasHeldPressed
					&& !Calculator.ButtonWasPressed
					&& IsPointInsideRect(
						Calculator.MousePressedX,
						Calculator.MousePressedY,
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

					if (pressMode == ButtonPressMode.HoldToPress)
					{
						progress = (int)(
							height / Calculator.ButtonHoldToPressTime * Calculator.ButtonPressedTime
						);
					}

					DrawBox(x, y + height - progress, width, progress, pressedColor, borderStyle);

					if (textFormat is TextFormat tf)
					{
						DrawText(
							x,
							y,
							width,
							height,
							borderStyle?.Thickness ?? 0,
							tf.Text,
							tf.TextColor,
							pressedColor,
							tf.FontSize,
							tf.Alignment,
							tf.Overflow
						);
					}

					Calculator.ButtonPressedTime += Raylib.GetFrameTime();

					if (
						pressMode == ButtonPressMode.HoldToRepeat
						&& Calculator.ButtonPressedTime >= Calculator.KeyRepeatInterval
					)
					{
						Calculator.ButtonWasPressed = true;
						Calculator.ButtonPressedTime = 0;
						Calculator.KeyRepeatInterval = Math.Max(
							Calculator.KeyRepeatInterval * Calculator.INITIAL_REPEAT_INTERVAL,
							Calculator.MIN_REPEAT_INTERVAL
						);
						callback();
					}
					else if (
						pressMode == ButtonPressMode.HoldToPress
						&& Calculator.ButtonPressedTime >= Calculator.ButtonHoldToPressTime
					)
					{
						Calculator.ButtonWasHeldPressed = true;
						Calculator.ButtonWasPressed = true;
						Calculator.ButtonPressedTime = 0;
						Calculator.KeyRepeatInterval = Calculator.INITIAL_REPEAT_INTERVAL;

						callback();
					}
				}
				else
				{
					DrawBox(x, y, width, height, backgroundColor, borderStyle, shadowStyle);

					if (textFormat is TextFormat tf)
					{
						DrawText(
							x,
							y,
							width,
							height,
							borderStyle?.Thickness ?? 0,
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
			if (IsPointInsideRect(Calculator.MouseX, Calculator.MouseY, x, y, width, height))
			{
				backgroundColor =
#if ANDROID
					Calculator.TouchCount > 0 ? hoveredColor : backgroundColor;
#else
				hoveredColor;
#endif

				DrawBox(x, y, width, height, backgroundColor, borderStyle, shadowStyle);

				if (textFormat is TextFormat tf)
				{
					DrawText(
						x,
						y,
						width,
						height,
						borderStyle?.Thickness ?? 0,
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
				DrawBox(x, y, width, height, backgroundColor, borderStyle, shadowStyle);

				if (textFormat is TextFormat tf)
				{
					DrawText(
						x,
						y,
						width,
						height,
						borderStyle?.Thickness ?? 0,
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

			int borderThickness = borderStyle?.Thickness ?? 0;

			// Draw the texture
			Raylib.DrawTexturePro(
				i.Texture,
				new(0, 0, i.Texture.Width, i.Texture.Height),
				new(
					x + width / 2 - i.Size / 2 + borderThickness,
					y + height / 2 - i.Size / 2 + borderThickness,
					i.Size - borderThickness * 2,
					i.Size - borderThickness * 2
				),
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
		BorderStyle? borderStyle = null
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

			if (borderStyle is BorderStyle bs && shadowStyle.Kind != ShadowKind.Cast)
			{
				if (shadowStyle.Kind == ShadowKind.Cube)
				{
					// left cube shadow outline
					Raylib.DrawLine(
						x + width + shadowStyle.Distance,
						y + shadowStyle.Distance,
						x + width + shadowStyle.Distance,
						y + height + shadowStyle.Distance,
						bs.Color
					);

					// bottom cube shadow outline
					Raylib.DrawLine(
						x + shadowStyle.Distance,
						y + height + shadowStyle.Distance,
						x + width + shadowStyle.Distance,
						y + height + shadowStyle.Distance,
						bs.Color
					);
				}
				else if (shadowStyle.Kind == ShadowKind.TransparentCube)
				{
					Raylib.DrawRectangleLines(
						x + shadowStyle.Distance,
						y + shadowStyle.Distance,
						width,
						height,
						bs.Color
					);
				}

				// top right outline
				Raylib.DrawLine(
					x + width,
					y,
					x + width + shadowStyle.Distance,
					y + shadowStyle.Distance,
					bs.Color
				);

				// bottom left outline
				Raylib.DrawLine(
					x,
					y + height,
					x + shadowStyle.Distance,
					y + height + shadowStyle.Distance,
					bs.Color
				);

				// bottom right outline
				Raylib.DrawLine(
					x + width,
					y + height,
					x + width + shadowStyle.Distance,
					y + height + shadowStyle.Distance,
					bs.Color
				);
			}
		}
	}

	// NOTE(LucasTA): only using this as an indicator for the scroll cause i need way more work on this
	// TODO(LucasTA): add mouse and touch scroll
	// TODO(LucasTA): handle scrollbar height correctly, it should be based on the amount of screens to scroll
	internal static void DrawScrollbar(
		int x,
		int y,
		int width,
		int height,
		int itemAmount,
		float offset,
		float maxOffset,
		int borderThickness,
		Color handleColor,
		Color borderColor,
		Color backgroundColor
	)
	{
		int handleHeight = Math.Max(width, height / Math.Max(1, itemAmount));
		int handleY = Math.Clamp(
			(int)(Math.Abs(offset / maxOffset) * height),
			0,
			height - handleHeight
		);

		Raylib.DrawRectangle(x, y, width, height, backgroundColor);

		DrawBox(x, handleY, width, handleHeight, handleColor, new(borderColor, borderThickness));
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
		Debug.IfDrawBorder(
			x < 0
				|| y < 0
				|| width <= 0
				|| height <= 0
				|| x + width > Calculator.ScreenWidth
				|| y + height > Calculator.ScreenHeight,
			"ERROR: Button grid is outside of the screen!\n",
			x,
			y,
			width,
			height
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

				// HACK(LucasTA): raylib uses integers, this adds some remainder pixels to the last button
				if (i == rows.Length - 1 && takenHeight + rowLength + padding != height)
				{
					rowLength += height - (takenHeight + rowLength + padding * (rows.Length - 1));
				}

				// HACK(LucasTA): same as above
				if (j == rows[i].Buttons.Length - 1 && takenWidth + colLength + padding != width)
				{
					colLength += width - (takenWidth + colLength + padding * (rows[i].Buttons.Length - 1));
				}

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
					rows[i].Buttons[j].Style.BorderStyle,
					rows[i].Buttons[j].Style.ShadowStyle
				);

				curX += colLength + padding;
				takenWidth += colLength;

				Debug.IfDrawBorder(
					takenWidth > availableWidth,
					$"ERROR: Button grid {j + 1} column takes more than the available width!\n",
					x,
					y,
					width,
					rowLength
				);
			}

			curY += rowLength + padding;
			takenHeight += rowLength;

			Debug.IfDrawBorder(
				takenHeight > availableHeight,
				$"ERROR: Button grid {i + 1} row takes more than the available height!\n",
				x,
				y,
				width,
				height
			);
		}
	}
}
