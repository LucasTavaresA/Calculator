// Licensed under the GPL3 or later versions of the GPL license.
// See the LICENSE file in the project root for more information.
#if LINUX || ANDROID || WINDOWS || MACOS
#else
#error "Yout need to specify a platform: ANDROID, WINDOWS, MACOS, or LINUX"
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text;

using Eval;

using Raylib_cs;

using static Calculator.Conversion;
using static Calculator.Currency;
using static Calculator.Resource;
#if ANDROID
using Android.Content;
#endif

namespace Calculator;

public readonly struct CalculatorUI
{
	private const string APP_VERSION = "1.0.0";
	private const string APP_LICENSE = "GPL-3.0";
	private const string APP_NAME = "Calculator";
	private const int TARGET_FPS = 60;
	internal const int FONT_SPACING = 2;

	private static readonly Color LightGreen = new(0, 193, 47, 255);
	private static readonly Color DarkerGray = new(60, 60, 60, 255);

	private static readonly Color BackgroundColor = Color.BLACK;
	private static readonly Color ForegroundColor = Color.WHITE;
	private static readonly Color DarkForegroundColor = Color.GRAY;

	private static readonly Color DisplayBackgroundColor = DarkerGray;

	private static readonly Color BorderColor = Color.GRAY;
	private static readonly Color ButtonHoverColor = BorderColor;
	private static readonly Color ButtonBackgroundColor = Color.DARKGRAY;
	private static readonly Color ButtonPressedColor = DarkerGray;
	private static readonly Color ButtonSelectedColor = ForegroundColor;
	private static readonly Color ButtonDeselectedColor = Color.LIGHTGRAY;
	private static readonly Color ButtonShadowColor = ButtonPressedColor;

	private static readonly Color Transparent = Color.BLANK;
	private static readonly Color TransparentButtonHoverColor = new(100, 100, 100, 128);

	private static readonly Color MenuEntryBackgroundColor = Color.DARKGRAY;

	private static readonly Color RedButtonColor = Color.RED;
	private static readonly Color RedButtonPressedColor = Color.MAROON;
	private static readonly Color RedButtonShadowColor = RedButtonPressedColor;
	private static readonly Color RedButtonBorderColor = Color.ORANGE;
	private static readonly Color RedButtonHoveredColor = RedButtonBorderColor;

	private static readonly Color GreenButtonColor = LightGreen;
	private static readonly Color GreenButtonPressedColor = Color.DARKGREEN;
	private static readonly Color GreenButtonShadowColor = GreenButtonPressedColor;
	private static readonly Color GreenButtonBorderColor = Color.GREEN;
	private static readonly Color GreenButtonHoveredColor = GreenButtonBorderColor;

	private static readonly Color ErrorColor = Color.RED;

	private static readonly Color ToggleOffColor = Color.LIGHTGRAY;
	private static readonly Color ToggleOnColor = Color.SKYBLUE;

	private static void InsertExpression(string value)
	{
		Expression = Expression.Insert(TypingIndex, value);
		ErrorMessage = "";

		try
		{
			Result = Evaluator.Evaluate(Expression).ToString(CultureInfo.InvariantCulture);
		}
		catch (Exception)
		{
			Result = "";
		}

		TypingIndex += value.Length;
	}

	internal static readonly Action Paste = () =>
	{
		InsertExpression(Clipboard.Get());
	};

	internal static readonly Action Backspace = () =>
	{
		if (Expression == "" || TypingIndex == 0)
		{
			return;
		}

		ErrorMessage = "";
		Expression = Expression.Remove(TypingIndex - 1, 1);

		try
		{
			Result = Evaluator.Evaluate(Expression).ToString(CultureInfo.InvariantCulture);
		}
		catch (Exception)
		{
			Result = "";
		}

		TypingIndex = Math.Max(0, TypingIndex - 1);
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
					.ToString(CultureInfo.InvariantCulture);
				ErrorMessage = "";

				if (Settings.BookmarkOnEval)
				{
					History.Add(Expression);
				}

				Expression = result;
				TypingIndex = Expression.Length;
			}
			catch (Exception e)
			{
				ErrorMessage = e.Message;
			}
		}
	};

	private static void OpenBrowser(string url)
	{
#if WINDOWS
		Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
#elif LINUX
		Process.Start("xdg-open", url);
#elif ANDROID
		var uri = Android.Net.Uri.Parse(url);
		var intent = new Intent(Intent.ActionView, uri);
		intent.SetFlags(ActivityFlags.NewTask);
		Android.App.Application.Context.StartActivity(intent);
#elif MACOS
		Process.Start("open", url);
#endif
	}

#if ANDROID
	internal const float INITIAL_REPEAT_INTERVAL = 0.5f;
	internal static int TouchCount = 0;
	internal static Vector2 StartTouchPosition;
#else
	internal const float INITIAL_REPEAT_INTERVAL = 0.3f;
	internal static float MouseScroll = 0;
#endif

	internal const float MIN_REPEAT_INTERVAL = INITIAL_REPEAT_INTERVAL / 10;
	internal static float ButtonPressedTime;
	internal static float KeyRepeatInterval = INITIAL_REPEAT_INTERVAL;
	internal static float HistoryScrollOffset = 0;
	internal static float DropDownScrollOffset = 0;
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

	internal static int TypingIndex = 0;

	internal static string Expression = "";
	internal static string Result = "";
	internal static string ErrorMessage = "";

	internal static Font Fonte;

	internal enum Scene
	{
		Calculator,
		History,
		Settings,
		Converters,
		Conversions,
	}

	internal static Scene CurrentScene = Scene.Calculator;

	internal enum DropDown
	{
		From,
		To,
	}

	private static DropDown CurrentDropDown;

	public static void MainLoop()
	{
		// Raylib context
		{
			// NOTE(LucasTA): HIGHDPI stops the window from being scaled as its resized
			Raylib.SetConfigFlags(
				ConfigFlags.FLAG_WINDOW_RESIZABLE | ConfigFlags.FLAG_WINDOW_TOPMOST
			);
#if ANDROID
			Raylib.InitWindow(0, 0, APP_NAME);
#else
			Raylib.InitWindow(400, 400, APP_NAME);
#endif
			Raylib.SetTargetFPS(TARGET_FPS);
			Raylib.SetExitKey(KeyboardKey.KEY_NULL);

			Settings.Load();

			if ((DateTime.Now.Date - Settings.LastAPICallTime).TotalDays >= 1)
			{
				try
				{
					Dictionary<string, double> rates = GetCurrencyRates();
					Settings.LastAPICallTime = DateTime.Now.Date;
					Settings.Save();
					Converters[0].Conversions.Clear();
					StringBuilder ratesString = new();

					for (int i = 0; i < Currencies.Length; i++)
					{
						Currency currency = Currencies[i];

						if (rates.TryGetValue(currency.Code, out double rate))
						{
							Converters[0].Conversions.Add(new(currency.Name, rate));
							ratesString.AppendLine(
								currency.Name + "," + rate.ToString(CultureInfo.InvariantCulture)
							);
						}
					}

					Data.SaveString(ratesString.ToString(), "CurrencyRates");
				}
				catch (Exception) { }
			}

			if (Converters[0].Conversions.Count == 0)
			{
				Converters[0].Conversions.Add(new("United States Dollar [USD]", 1));
			}

			foreach (string resource in Assembly.GetManifestResourceNames())
			{
				if (resource.EndsWith(".png"))
				{
					Resources.Add(resource, LoadTextureFromAssembly(resource));
				}
				else if (resource.EndsWith(".ttf"))
				{
					// NOTE(LucasTA): Without HIGHDPI the font has artifacts, so we load
					// it realy big and then scale it down
					// just the filter is really blurry
					Fonte = LoadFontFromAssembly(resource, 64);
					Raylib.SetTextureFilter(Fonte.Texture, TextureFilter.TEXTURE_FILTER_BILINEAR);
				}
			}

			Raylib.SetWindowIcon(Raylib.LoadImageFromTexture(GetResource("appicon.png")));

			while (!Raylib.WindowShouldClose())
			{
				// get screen information
				{
					ScreenWidth = Raylib.GetScreenWidth();
					ScreenHeight = Raylib.GetScreenHeight();
					FontSize = (ScreenWidth + ScreenHeight) / 38;
					BorderThickness = Math.Max(
						(ScreenWidth > ScreenHeight ? ScreenWidth : ScreenHeight) / 500,
						1
					);
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

					int displayX = Padding;
					int displayY = Padding;
					int displayWidth = ScreenWidth - (Padding * 2);
					int displayHeight = (ScreenHeight / 6) - Padding;

					Vector2 textSize = Raylib.MeasureTextEx(
						CalculatorUI.Fonte,
						"0",
						FontSize,
						CalculatorUI.FONT_SPACING
					);
					int topIconSize = displayY + (int)textSize.Y;

					int menuSidePadding = Padding * 2 + topIconSize;
					int menuEntryHeight = ((int)textSize.Y * 2) + Padding;
					int menuEntryX = 0;
					int menuEntryWidth = ScreenWidth - menuSidePadding;
					int menuVisibleEntries = ScreenHeight / menuEntryHeight;

					int dropDownVisibleEntries = 16;
					int dropDownEntryHeight = ScreenHeight / dropDownVisibleEntries;

					Layout.ShadowStyle greyButtonShadow =
						new(ButtonShadowColor, ShadowDistance, Layout.ShadowKind.Pillar);
					Layout.ShadowStyle redButtonShadow =
						new(RedButtonShadowColor, ShadowDistance, Layout.ShadowKind.Pillar);
					Layout.ShadowStyle greenButtonShadow =
						new(GreenButtonShadowColor, ShadowDistance, Layout.ShadowKind.Pillar);

					Layout.ButtonStyle greyButton =
						new(
							BackgroundColor: ButtonBackgroundColor,
							PressedColor: ButtonPressedColor,
							HoveredColor: ButtonHoverColor,
							new(BorderColor, BorderThickness),
							ShadowStyle: greyButtonShadow
						);

					Layout.ButtonStyle backspaceButton =
						new(
							BackgroundColor: RedButtonColor,
							PressedColor: RedButtonPressedColor,
							HoveredColor: RedButtonHoveredColor,
							new(RedButtonBorderColor, BorderThickness),
							ShadowStyle: redButtonShadow,
							Icon: new(
								GetResource("backspace_icon.png"),
								ForegroundColor,
								(int)textSize.Y
							)
						);

					Layout.ButtonStyle greenButton =
						new(
							BackgroundColor: GreenButtonColor,
							PressedColor: GreenButtonPressedColor,
							HoveredColor: GreenButtonHoveredColor,
							new(GreenButtonBorderColor, BorderThickness),
							ShadowStyle: greenButtonShadow
						);

					switch (CurrentScene)
					{
						case Scene.Calculator:
							{
								// Draw buttons
								{
									int rowAmount = 7;
									int heightPercentage = 100 / rowAmount;

									Layout.ButtonRow[] calculatorButtons = new Layout.ButtonRow[]
									{
										new Layout.ButtonRow(
											heightPercentage,
											new Layout.Button(
												20,
												new("(", FontSize, ForegroundColor),
												() => InsertExpression("("),
												greyButton,
												Layout.ButtonPressMode.HoldToRepeat
											),
											new Layout.Button(
												20,
												new(")", FontSize, ForegroundColor),
												() => InsertExpression(")"),
												greyButton,
												Layout.ButtonPressMode.HoldToRepeat
											),
											new Layout.Button(
												20,
												new(",", FontSize, ForegroundColor),
												() => InsertExpression(","),
												greyButton
											),
											new Layout.Button(
												20,
												new("^", FontSize, ForegroundColor),
												() => InsertExpression("^"),
												greyButton
											),
											new Layout.Button(
												20,
												null,
												() => InsertExpression("pi"),
												new(
													BackgroundColor: ButtonBackgroundColor,
													PressedColor: ButtonPressedColor,
													HoveredColor: ButtonHoverColor,
													new(BorderColor, BorderThickness),
													ShadowStyle: greyButtonShadow,
													Icon: new(
														GetResource("pi_icon.png"),
														ForegroundColor,
														(int)textSize.Y
													)
												)
											)
										),
										new Layout.ButtonRow(
											heightPercentage,
											new Layout.Button(
												20,
												new("!", FontSize, ForegroundColor),
												() => InsertExpression("!"),
												greyButton
											),
											new Layout.Button(
												20,
												new("e", FontSize, ForegroundColor),
												() => InsertExpression("e"),
												greyButton
											),
											new Layout.Button(
												20,
												new("%", FontSize, ForegroundColor),
												() => InsertExpression("%"),
												greyButton
											),
											new Layout.Button(
												20,
												new("/", FontSize, ForegroundColor),
												() => InsertExpression("/"),
												greyButton
											),
											new Layout.Button(
												20,
												new("C", FontSize, ForegroundColor),
												() =>
												{
													Expression = "";
													ErrorMessage = "";
													Result = "";
													TypingIndex = 0;
												},
												greyButton
											)
										),
										new Layout.ButtonRow(
											heightPercentage,
											new Layout.Button(
												25,
												new("7", FontSize, ForegroundColor),
												() => InsertExpression("7"),
												greyButton,
												Layout.ButtonPressMode.HoldToRepeat
											),
											new Layout.Button(
												25,
												new("8", FontSize, ForegroundColor),
												() => InsertExpression("8"),
												greyButton,
												Layout.ButtonPressMode.HoldToRepeat
											),
											new Layout.Button(
												25,
												new("9", FontSize, ForegroundColor),
												() => InsertExpression("9"),
												greyButton,
												Layout.ButtonPressMode.HoldToRepeat
											),
											new Layout.Button(
												25,
												new("*", FontSize, ForegroundColor),
												() => InsertExpression("*"),
												greyButton
											)
										),
										new Layout.ButtonRow(
											heightPercentage,
											new Layout.Button(
												25,
												new("4", FontSize, ForegroundColor),
												() => InsertExpression("4"),
												greyButton,
												Layout.ButtonPressMode.HoldToRepeat
											),
											new Layout.Button(
												25,
												new("5", FontSize, ForegroundColor),
												() => InsertExpression("5"),
												greyButton,
												Layout.ButtonPressMode.HoldToRepeat
											),
											new Layout.Button(
												25,
												new("6", FontSize, ForegroundColor),
												() => InsertExpression("6"),
												greyButton,
												Layout.ButtonPressMode.HoldToRepeat
											),
											new Layout.Button(
												25,
												new("-", FontSize, ForegroundColor),
												() => InsertExpression("-"),
												greyButton
											)
										),
										new Layout.ButtonRow(
											heightPercentage,
											new Layout.Button(
												25,
												new("1", FontSize, ForegroundColor),
												() => InsertExpression("1"),
												greyButton,
												Layout.ButtonPressMode.HoldToRepeat
											),
											new Layout.Button(
												25,
												new("2", FontSize, ForegroundColor),
												() => InsertExpression("2"),
												greyButton,
												Layout.ButtonPressMode.HoldToRepeat
											),
											new Layout.Button(
												25,
												new("3", FontSize, ForegroundColor),
												() => InsertExpression("3"),
												greyButton,
												Layout.ButtonPressMode.HoldToRepeat
											),
											new Layout.Button(
												25,
												new("+", FontSize, ForegroundColor),
												() => InsertExpression("+"),
												greyButton
											)
										),
										new Layout.ButtonRow(
											heightPercentage,
											new Layout.Button(
												25,
												new(".", FontSize, ForegroundColor),
												() => InsertExpression("."),
												greyButton
											),
											new Layout.Button(
												25,
												new("0", FontSize, ForegroundColor),
												() => InsertExpression("0"),
												greyButton,
												Layout.ButtonPressMode.HoldToRepeat
											),
											new Layout.Button(
												25,
												null,
												Backspace,
												backspaceButton,
												Layout.ButtonPressMode.HoldToRepeat
											),
											new Layout.Button(
												25,
												new("=", FontSize, ForegroundColor),
												Equal,
												greenButton
											)
										),
										new Layout.ButtonRow(
											heightPercentage,
											new Layout.Button(
												17,
												new("sqrt", FontSize, ForegroundColor),
												() => InsertExpression("sqrt("),
												greyButton
											),
											new Layout.Button(
												17,
												new("mod", FontSize, ForegroundColor),
												() => InsertExpression("mod("),
												greyButton
											),
											new Layout.Button(
												17,
												new("sin", FontSize, ForegroundColor),
												() => InsertExpression("sin("),
												greyButton
											),
											new Layout.Button(
												17,
												new("cos", FontSize, ForegroundColor),
												() => InsertExpression("cos("),
												greyButton
											),
											new Layout.Button(
												16,
												new("tan", FontSize, ForegroundColor),
												() => InsertExpression("tan("),
												greyButton
											),
											new Layout.Button(
												16,
												new("log", FontSize, ForegroundColor),
												() => InsertExpression("log("),
												greyButton
											)
										),
									};

									Layout.DrawButtonGrid(
										Padding,
										Padding + (ScreenHeight / 6),
										ScreenWidth - (Padding * 2),
										ScreenHeight - (Padding * 2) - (ScreenHeight / 6),
										Padding,
										calculatorButtons
									);
								}

								// Draw Calculator Display
								{
									bool error = ErrorMessage == "";

									Layout.DrawButton(
										displayX,
										displayY,
										displayWidth / 2,
										displayHeight,
										Transparent,
										TransparentButtonHoverColor,
										Transparent,
										() =>
										{
											TypingIndex = Math.Max(0, TypingIndex - 1);
										},
										pressMode: Layout.ButtonPressMode.HoldToRepeat,
										icon: new(
											GetResource("arrow_left.png"),
											DisplayBackgroundColor
										)
									);

									Layout.DrawButton(
										displayX + displayWidth / 2,
										displayY,
										displayWidth / 2,
										displayHeight,
										Transparent,
										TransparentButtonHoverColor,
										Transparent,
										() =>
										{
											TypingIndex = Math.Min(
												Expression.Length,
												TypingIndex + 1
											);
										},
										pressMode: Layout.ButtonPressMode.HoldToRepeat,
										icon: new(
											GetResource("arrow_right.png"),
											DisplayBackgroundColor
										)
									);

									Layout.DrawTextBox(
										displayX,
										displayY,
										displayWidth,
										displayHeight,
										new(
											Expression.Insert(TypingIndex, "|"),
											FontSize,
											ForegroundColor,
											Layout.TextAlignment.Center
										),
										// NOTE(LucasTA): so that the arrows draw behind are visible
										TransparentButtonHoverColor,
										new(error ? BorderColor : ErrorColor, BorderThickness * 2)
									);

									Layout.DrawText(
										displayX,
										displayY + BorderThickness * 2,
										displayWidth,
										displayHeight,
										BorderThickness * 2,
										error ? Result : ErrorMessage,
										error ? DarkForegroundColor : ErrorColor,
										DisplayBackgroundColor,
										FontSize,
										Layout.TextAlignment.BottomLeft
									);

									int keycode = Raylib.GetCharPressed();

									if (Raylib.IsKeyPressed(KeyboardKey.KEY_ENTER))
									{
										Equal();
										ButtonPressedTime = 0;
									}
									if (Raylib.IsKeyPressed(KeyboardKey.KEY_LEFT))
									{
										TypingIndex = Math.Max(0, TypingIndex - 1);
										ButtonPressedTime = 0;
									}
									if (Raylib.IsKeyPressed(KeyboardKey.KEY_RIGHT))
									{
										TypingIndex = Math.Min(Expression.Length, TypingIndex + 1);
										ButtonPressedTime = 0;
									}
									else if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT))
									{
										if (ButtonPressedTime >= INITIAL_REPEAT_INTERVAL)
										{
											TypingIndex = Math.Max(0, TypingIndex - 1);
										}

										ButtonPressedTime += Raylib.GetFrameTime();
									}
									else if (Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT))
									{
										if (ButtonPressedTime >= INITIAL_REPEAT_INTERVAL)
										{
											TypingIndex = Math.Min(
												Expression.Length,
												TypingIndex + 1
											);
										}

										ButtonPressedTime += Raylib.GetFrameTime();
									}
									else if (
										(
											Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL)
											|| Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_CONTROL)
										) && Raylib.IsKeyPressed(KeyboardKey.KEY_C)
									)
									{
										Clipboard.Set(Expression);
										ButtonPressedTime = 0;
									}
									else if (
										(
											Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL)
											|| Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_CONTROL)
										) && Raylib.IsKeyPressed(KeyboardKey.KEY_V)
									)
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
										if (
											char.IsAsciiLetterOrDigit((char)keycode)
											|| (char)keycode
												is ' '
													or '('
													or ')'
													or ','
													or '^'
													or '!'
													or '%'
													or '/'
													or '+'
													or '-'
													or '*'
													or '.'
										)
										{
											InsertExpression(
												((char)keycode).ToString().ToLowerInvariant()
											);
											ButtonPressedTime = 0;
										}
									}
								}

								// top buttons
								{
									Layout.DrawButton(
										0,
										0,
										topIconSize,
										topIconSize,
										Transparent,
										ButtonPressedColor,
										TransparentButtonHoverColor,
										() => CurrentScene = Scene.Settings,
										icon: new(GetResource("settings_icon.png"), ForegroundColor)
									);

									Layout.DrawButton(
										topIconSize + Padding,
										0,
										topIconSize,
										topIconSize,
										Transparent,
										ButtonPressedColor,
										TransparentButtonHoverColor,
										() => CurrentScene = Scene.Converters,
										icon: new(GetResource("ruler_icon.png"), ForegroundColor)
									);

									Layout.DrawButton(
										ScreenWidth - topIconSize,
										0,
										topIconSize,
										topIconSize,
										Transparent,
										ButtonPressedColor,
										TransparentButtonHoverColor,
										() => CurrentScene = Scene.History,
										icon: new(GetResource("history_icon.png"), ForegroundColor)
									);

									Layout.DrawButton(
										ScreenWidth - (topIconSize) * 2 - Padding,
										0,
										topIconSize,
										topIconSize,
										Transparent,
										ButtonPressedColor,
										TransparentButtonHoverColor,
										() => Clipboard.Set(Expression),
										icon: new(GetResource("copy_icon.png"), ForegroundColor)
									);

									Layout.DrawButton(
										ScreenWidth - topIconSize * 3 - Padding * 2,
										0,
										topIconSize,
										topIconSize,
										Transparent,
										ButtonPressedColor,
										TransparentButtonHoverColor,
										Paste,
										icon: new(GetResource("paste_icon.png"), ForegroundColor)
									);

									Layout.DrawButton(
										ScreenWidth - topIconSize * 4 - Padding * 3,
										0,
										topIconSize,
										topIconSize,
										Transparent,
										ButtonPressedColor,
										TransparentButtonHoverColor,
										() => History.Add(Expression),
										icon: new(
											GetResource("bookmark_add_icon.png"),
											ForegroundColor
										)
									);
								}
							}
							break;
						case Scene.History:
							{
								List<string> expressions = new(History.PinnedExpressions);
								expressions.AddRange(History.ExpressionHistory);

								if (Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE))
								{
									CurrentScene = Scene.Calculator;
								}

#if ANDROID
								if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
								{
									StartTouchPosition = Raylib.GetTouchPosition(0);
								}

								if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT))
								{
									Vector2 currentTouchPosition = Raylib.GetTouchPosition(0);

									float touchMoveDistance = Math.Abs(
										currentTouchPosition.Y - StartTouchPosition.Y
									);

									if (
										touchMoveDistance > 2
										&&
										// NOTE(LucasTA): Don't scroll if touch starts outside the
										// list
										Layout.IsPointInsideRect(
											MousePressedX,
											MousePressedY,
											0,
											0,
											ScreenWidth - menuSidePadding,
											ScreenHeight
										)
									)
									{
										Dragging = true;

										HistoryScrollOffset = Math.Clamp(
											HistoryScrollOffset
												+ currentTouchPosition.Y
												- StartTouchPosition.Y,
											Math.Max(0, expressions.Count - menuVisibleEntries)
												* -menuEntryHeight,
											0
										);

										StartTouchPosition = currentTouchPosition;
									}
								}
#else
								if (MouseScroll > 0)
								{
									Dragging = true;
								}

								HistoryScrollOffset = Math.Clamp(
									HistoryScrollOffset + MouseScroll,
									Math.Max(0, expressions.Count - menuVisibleEntries)
										* -menuEntryHeight,
									0
								);
#endif

								Layout.DrawButton(
									ScreenWidth - topIconSize,
									0,
									topIconSize,
									topIconSize,
									Transparent,
									ButtonPressedColor,
									TransparentButtonHoverColor,
									() => CurrentScene = Scene.Calculator,
									icon: new(GetResource("close_icon.png"), ForegroundColor)
								);

								Layout.DrawButton(
									ScreenWidth - topIconSize,
									ScreenHeight - topIconSize,
									topIconSize,
									topIconSize,
									Transparent,
									RedButtonPressedColor,
									TransparentButtonHoverColor,
									() =>
									{
										History.Clear();
									},
									icon: new(GetResource("trash_all_icon.png"), RedButtonColor),
									pressMode: Layout.ButtonPressMode.HoldToPress
								);

								{
									for (int i = 0; i < expressions.Count; i++)
									{
										int menuEntryY =
											(int)HistoryScrollOffset + i * menuEntryHeight;

										Layout.DrawTextBox(
											menuEntryX,
											menuEntryY,
											menuEntryWidth,
											menuEntryHeight,
											new(
												expressions[i],
												FontSize,
												ForegroundColor,
												Layout.TextAlignment.BottomLeft,
												Layout.OverflowMode.Truncate
											),
											MenuEntryBackgroundColor,
											new(BorderColor, BorderThickness)
										);

										int deleteX =
											menuEntryX + menuEntryWidth - topIconSize - Padding;

										Layout.DrawButton(
											deleteX,
											menuEntryY + BorderThickness,
											topIconSize,
											topIconSize,
											Transparent,
											RedButtonPressedColor,
											TransparentButtonHoverColor,
											() =>
											{
												History.Remove(expressions[i]);
											},
											icon: new(
												GetResource("trash_icon.png"),
												RedButtonColor
											),
											pressMode: Layout.ButtonPressMode.HoldToPress
										);

										int copyX = deleteX - topIconSize - Padding;

										Layout.DrawButton(
											copyX,
											menuEntryY + BorderThickness,
											topIconSize,
											topIconSize,
											Transparent,
											ButtonPressedColor,
											TransparentButtonHoverColor,
											() => Clipboard.Set(expressions[i]),
											icon: new(GetResource("copy_icon.png"), ForegroundColor)
										);

										int pickX = copyX - topIconSize - Padding;

										Layout.DrawButton(
											pickX,
											menuEntryY + BorderThickness,
											topIconSize,
											topIconSize,
											Transparent,
											ButtonPressedColor,
											TransparentButtonHoverColor,
											() =>
											{
												Expression = expressions[i];
												ErrorMessage = "";

												try
												{
													Result = Evaluator
														.Evaluate(Expression)
														.ToString(CultureInfo.InvariantCulture);
												}
												catch (Exception)
												{
													Result = "";
												}

												TypingIndex = Expression.Length;
												CurrentScene = Scene.Calculator;
											},
											icon: new(GetResource("open_icon.png"), ForegroundColor)
										);

										int pinX = pickX - topIconSize - Padding;
										bool pinned = History.PinnedExpressions.Contains(
											expressions[i]
										);

										Layout.DrawButton(
											pinX,
											menuEntryY + BorderThickness,
											topIconSize,
											topIconSize,
											Transparent,
											ButtonPressedColor,
											TransparentButtonHoverColor,
											() =>
											{
												if (pinned)
												{
													History.Unpin(expressions[i]);
												}
												else
												{
													History.Pin(expressions[i]);
												}
											},
											icon: new(
												pinned
													? GetResource("unpin_icon.png")
													: GetResource("pin_icon.png"),
												ForegroundColor
											)
										);
									}
								}
							}
							break;
						case Scene.Settings:
							{
								if (Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE))
								{
									CurrentScene = Scene.Calculator;
								}

								Layout.DrawButton(
									0,
									0,
									topIconSize,
									topIconSize,
									Transparent,
									ButtonPressedColor,
									TransparentButtonHoverColor,
									() => CurrentScene = Scene.Calculator,
									icon: new(GetResource("close_icon.png"), ForegroundColor)
								);

								Layout.DrawBox(
									menuEntryX + menuSidePadding,
									0,
									menuEntryWidth,
									menuEntryHeight,
									MenuEntryBackgroundColor,
									new(BorderColor, BorderThickness)
								);

								Layout.DrawButton(
									ScreenWidth - menuSidePadding + BorderThickness,
									BorderThickness,
									menuSidePadding - BorderThickness * 2,
									menuEntryHeight - BorderThickness * 2,
									Transparent,
									ButtonPressedColor,
									TransparentButtonHoverColor,
									() =>
									{
										Settings.BookmarkOnEval = !Settings.BookmarkOnEval;
										Settings.Save();
									},
									icon: new(
										Settings.BookmarkOnEval
											? GetResource("toggle_on_icon.png")
											: GetResource("toggle_off_icon.png"),
										Settings.BookmarkOnEval ? ToggleOnColor : ToggleOffColor
									)
								);

								Layout.DrawText(
									menuEntryX + menuSidePadding,
									0,
									menuEntryWidth - menuSidePadding,
									menuEntryHeight,
									BorderThickness,
									"Add to history with '='",
									ForegroundColor,
									MenuEntryBackgroundColor,
									FontSize,
									Layout.TextAlignment.Left
								);

								Layout.DrawTextBox(
									menuEntryX + menuSidePadding,
									menuEntryHeight,
									menuEntryWidth,
									menuEntryHeight,
									new(
										"Version:",
										FontSize,
										ForegroundColor,
										Layout.TextAlignment.Left
									),
									MenuEntryBackgroundColor,
									new(BorderColor, BorderThickness)
								);

								Layout.DrawText(
									menuEntryX + menuSidePadding,
									menuEntryHeight,
									menuEntryWidth,
									menuEntryHeight,
									BorderThickness,
									APP_VERSION,
									ForegroundColor,
									MenuEntryBackgroundColor,
									FontSize,
									Layout.TextAlignment.Right
								);

								Layout.DrawTextBox(
									menuEntryX + menuSidePadding,
									menuEntryHeight * 2,
									menuEntryWidth,
									menuEntryHeight,
									new(
										"License:",
										FontSize,
										ForegroundColor,
										Layout.TextAlignment.Left
									),
									MenuEntryBackgroundColor,
									new(BorderColor, BorderThickness)
								);

								Layout.DrawText(
									menuEntryX + menuSidePadding,
									menuEntryHeight * 2,
									menuEntryWidth,
									menuEntryHeight,
									BorderThickness,
									APP_LICENSE,
									ForegroundColor,
									MenuEntryBackgroundColor,
									FontSize,
									Layout.TextAlignment.Right
								);

								Layout.DrawBox(
									menuEntryX + menuSidePadding,
									menuEntryHeight * 3,
									menuEntryWidth,
									menuEntryHeight,
									MenuEntryBackgroundColor,
									new(BorderColor, BorderThickness)
								);

								Layout.DrawButton(
									ScreenWidth - menuSidePadding + BorderThickness,
									menuEntryHeight * 3 + BorderThickness,
									menuSidePadding - BorderThickness * 2,
									menuEntryHeight - BorderThickness * 2,
									Transparent,
									ButtonPressedColor,
									TransparentButtonHoverColor,
									() =>
										OpenBrowser("https://github.com/lucastavaresa/Calculator"),
									icon: new(GetResource("github_icon.png"), Color.WHITE)
								);

								Layout.DrawText(
									menuEntryX + menuSidePadding,
									menuEntryHeight * 3,
									menuEntryWidth - menuSidePadding,
									menuEntryHeight,
									BorderThickness,
									"Source code:",
									ForegroundColor,
									MenuEntryBackgroundColor,
									FontSize,
									Layout.TextAlignment.Left
								);
							}
							break;
						case Scene.Converters:
							{
								if (Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE))
								{
									CurrentScene = Scene.Calculator;
								}

								int converterAmount = Converters.Length;
								int leftButtonSize = ScreenHeight / (converterAmount + 1);

								Layout.DrawButton(
									0,
									0,
									leftButtonSize,
									leftButtonSize,
									Transparent,
									ButtonPressedColor,
									TransparentButtonHoverColor,
									() => CurrentScene = Scene.Calculator,
									icon: new(GetResource("close_icon.png"), ForegroundColor)
								);

								for (int i = 0; i < converterAmount; i++)
								{
									Layout.DrawButton(
										0,
										(i + 1) * leftButtonSize,
										leftButtonSize,
										leftButtonSize,
										CurrentConverter == i
											? ButtonPressedColor
											: MenuEntryBackgroundColor,
										ButtonPressedColor,
										TransparentButtonHoverColor,
										() =>
										{
											CurrentConverter = i;
											ConverterFromIndex = 0;
											ConverterToIndex = 0;
											ConverterExpression = "";
											ConverterResult = "";
											ConverterTypingIndex = 0;
										},
										borderStyle: new(
											CurrentConverter == i
												? ButtonSelectedColor
												: Transparent,
											BorderThickness
										),
										icon: new(
											GetResource(Converters[i].Icon),
											CurrentConverter == i
												? ButtonSelectedColor
												: ButtonDeselectedColor,
											leftButtonSize
										)
									);
								}

								int converterBoxHeight = (int)textSize.Y * 3;

								// converter display and buttons
								{
									int converterBoxWidth = ScreenWidth - leftButtonSize;
									int gridButtonSize = (int)textSize.Y;

									// From display and buttons
									{
										string convertingFrom = Converters[CurrentConverter]
											.Conversions[ConverterFromIndex]
											.Name;
										int gridCols = 4;
										int gridWidthPercentage = 100 / gridCols;
										int gridWidth = gridButtonSize * gridCols;

										Layout.DrawBox(
											leftButtonSize,
											leftButtonSize,
											converterBoxWidth,
											converterBoxHeight,
											MenuEntryBackgroundColor,
											new(BorderColor, BorderThickness)
										);

										Layout.DrawButton(
											leftButtonSize,
											leftButtonSize,
											converterBoxWidth,
											gridButtonSize,
											Transparent,
											ButtonPressedColor,
											TransparentButtonHoverColor,
											() =>
											{
												CurrentDropDown = DropDown.From;
												CurrentScene = Scene.Conversions;
												DropDownScrollOffset = -(
													dropDownEntryHeight
													* (
														ConverterFromIndex
														- dropDownVisibleEntries / 2
													)
												);
											},
											borderStyle: new(BorderColor, BorderThickness)
										);

										Layout.DrawText(
											leftButtonSize,
											leftButtonSize,
											converterBoxWidth - gridButtonSize,
											gridButtonSize,
											0,
											convertingFrom,
											ForegroundColor,
											Transparent,
											FontSize,
											Layout.TextAlignment.Left
										);

										Raylib.DrawTexturePro(
											GetResource("down_arrow_icon.png"),
											Layout.ICON_RECTANGLE,
											new(
												leftButtonSize + converterBoxWidth - gridButtonSize,
												leftButtonSize,
												gridButtonSize,
												gridButtonSize
											),
											new(0, 0),
											0,
											ForegroundColor
										);

										Layout.DrawText(
											leftButtonSize,
											leftButtonSize,
											converterBoxWidth,
											converterBoxHeight,
											0,
											ConverterExpression.Insert(ConverterTypingIndex, "|"),
											ForegroundColor,
											MenuEntryBackgroundColor,
											FontSize,
											Layout.TextAlignment.Left
										);

										Layout.ButtonRow[] converterButtons = new Layout.ButtonRow[]
										{
											new Layout.ButtonRow(
												100,
												new Layout.Button(
													gridWidthPercentage,
													null,
													() =>
													{
														(ConverterFromIndex, ConverterToIndex) = (
															ConverterToIndex,
															ConverterFromIndex
														);
														(ConverterExpression, ConverterResult) = (
															ConverterResult,
															ConverterExpression
														);
														ConverterTypingIndex =
															ConverterExpression.Length;
														Convert();
													},
													new(
														BackgroundColor: ButtonBackgroundColor,
														PressedColor: ButtonPressedColor,
														HoveredColor: ButtonHoverColor,
														new(BorderColor, BorderThickness),
														Icon: new(
															GetResource("vertical_swap_icon.png"),
															ForegroundColor
														)
													)
												),
												new Layout.Button(
													gridWidthPercentage,
													null,
													() =>
														InsertConverterExpression(Clipboard.Get()),
													new(
														BackgroundColor: ButtonBackgroundColor,
														PressedColor: ButtonPressedColor,
														HoveredColor: ButtonHoverColor,
														new(BorderColor, BorderThickness),
														Icon: new(
															GetResource("paste_icon.png"),
															ForegroundColor
														)
													)
												),
												new Layout.Button(
													gridWidthPercentage,
													null,
													() => Clipboard.Set(ConverterExpression),
													new(
														BackgroundColor: ButtonBackgroundColor,
														PressedColor: ButtonPressedColor,
														HoveredColor: ButtonHoverColor,
														new(BorderColor, BorderThickness),
														Icon: new(
															GetResource("copy_icon.png"),
															ForegroundColor
														)
													)
												),
												new Layout.Button(
													gridWidthPercentage,
													new("C", FontSize, ForegroundColor),
													() =>
													{
														ConverterExpression = "";
														ConverterResult = "";
														ConverterTypingIndex = 0;
													},
													new(
														BackgroundColor: ButtonBackgroundColor,
														PressedColor: ButtonPressedColor,
														HoveredColor: ButtonHoverColor,
														new(BorderColor, BorderThickness)
													)
												)
											),
										};

										Layout.DrawButtonGrid(
											ScreenWidth - gridButtonSize * gridCols,
											leftButtonSize + converterBoxHeight - gridButtonSize,
											gridWidth,
											gridButtonSize - BorderThickness,
											BorderThickness,
											converterButtons
										);
									}

									// To display and buttons
									{
										string convertingTo = Converters[CurrentConverter]
											.Conversions[ConverterToIndex]
											.Name;

										Layout.DrawBox(
											leftButtonSize,
											converterBoxHeight + leftButtonSize,
											converterBoxWidth,
											converterBoxHeight,
											MenuEntryBackgroundColor,
											new(BorderColor, BorderThickness)
										);

										Layout.DrawButton(
											leftButtonSize,
											converterBoxHeight + leftButtonSize,
											converterBoxWidth,
											gridButtonSize,
											Transparent,
											ButtonPressedColor,
											TransparentButtonHoverColor,
											() =>
											{
												CurrentDropDown = DropDown.To;
												CurrentScene = Scene.Conversions;
												DropDownScrollOffset = -(
													dropDownEntryHeight
													* (
														ConverterToIndex
														- dropDownVisibleEntries / 2
													)
												);
											},
											borderStyle: new(BorderColor, BorderThickness)
										);

										Layout.DrawText(
											leftButtonSize,
											converterBoxHeight + leftButtonSize,
											converterBoxWidth - gridButtonSize,
											gridButtonSize,
											0,
											convertingTo,
											ForegroundColor,
											Transparent,
											FontSize,
											Layout.TextAlignment.Left
										);

										Raylib.DrawTexturePro(
											GetResource("down_arrow_icon.png"),
											Layout.ICON_RECTANGLE,
											new(
												leftButtonSize + converterBoxWidth - gridButtonSize,
												converterBoxHeight + leftButtonSize,
												gridButtonSize,
												gridButtonSize
											),
											new(0, 0),
											0,
											ForegroundColor
										);

										Layout.DrawText(
											leftButtonSize,
											converterBoxHeight + leftButtonSize,
											converterBoxWidth,
											converterBoxHeight,
											0,
											ConverterResult,
											ForegroundColor,
											MenuEntryBackgroundColor,
											FontSize,
											Layout.TextAlignment.Left
										);

										Layout.DrawButton(
											ScreenWidth - gridButtonSize,
											(leftButtonSize + converterBoxHeight * 2)
												- gridButtonSize,
											gridButtonSize - BorderThickness,
											gridButtonSize - BorderThickness,
											ButtonBackgroundColor,
											ButtonPressedColor,
											ButtonHoverColor,
											() => Clipboard.Set(ConverterResult),
											borderStyle: new(BorderColor, BorderThickness),
											icon: new(GetResource("copy_icon.png"), ForegroundColor)
										);
									}
								}

								Layout.DrawText(
									leftButtonSize,
									0,
									ScreenWidth - leftButtonSize,
									ScreenHeight,
									0,
									$"{Converters[CurrentConverter].Title}",
									ForegroundColor,
									Transparent,
									FontSize,
									Layout.TextAlignment.Top
								);

								// keyboard/inputs
								{
									int rowAmount = 4;
									int colAmount = 3;
									int heightPercentage = 100 / rowAmount;
									int widthPercentage = 100 / colAmount;

									Layout.ButtonRow[] converterKeyboardButtons =
										new Layout.ButtonRow[]
										{
											new Layout.ButtonRow(
												heightPercentage,
												new Layout.Button(
													widthPercentage,
													new("7", FontSize, ForegroundColor),
													() => InsertConverterExpression("7"),
													greyButton,
													Layout.ButtonPressMode.HoldToRepeat
												),
												new Layout.Button(
													widthPercentage,
													new("8", FontSize, ForegroundColor),
													() => InsertConverterExpression("8"),
													greyButton,
													Layout.ButtonPressMode.HoldToRepeat
												),
												new Layout.Button(
													widthPercentage,
													new("9", FontSize, ForegroundColor),
													() => InsertConverterExpression("9"),
													greyButton,
													Layout.ButtonPressMode.HoldToRepeat
												)
											),
											new Layout.ButtonRow(
												heightPercentage,
												new Layout.Button(
													widthPercentage,
													new("4", FontSize, ForegroundColor),
													() => InsertConverterExpression("4"),
													greyButton,
													Layout.ButtonPressMode.HoldToRepeat
												),
												new Layout.Button(
													widthPercentage,
													new("5", FontSize, ForegroundColor),
													() => InsertConverterExpression("5"),
													greyButton,
													Layout.ButtonPressMode.HoldToRepeat
												),
												new Layout.Button(
													widthPercentage,
													new("6", FontSize, ForegroundColor),
													() => InsertConverterExpression("6"),
													greyButton,
													Layout.ButtonPressMode.HoldToRepeat
												)
											),
											new Layout.ButtonRow(
												heightPercentage,
												new Layout.Button(
													widthPercentage,
													new("1", FontSize, ForegroundColor),
													() => InsertConverterExpression("1"),
													greyButton,
													Layout.ButtonPressMode.HoldToRepeat
												),
												new Layout.Button(
													widthPercentage,
													new("2", FontSize, ForegroundColor),
													() => InsertConverterExpression("2"),
													greyButton,
													Layout.ButtonPressMode.HoldToRepeat
												),
												new Layout.Button(
													widthPercentage,
													new("3", FontSize, ForegroundColor),
													() => InsertConverterExpression("3"),
													greyButton,
													Layout.ButtonPressMode.HoldToRepeat
												)
											),
											new Layout.ButtonRow(
												heightPercentage,
												new Layout.Button(
													widthPercentage,
													new(".", FontSize, ForegroundColor),
													() => InsertConverterExpression("."),
													greyButton
												),
												new Layout.Button(
													widthPercentage,
													new("0", FontSize, ForegroundColor),
													() => InsertConverterExpression("0"),
													greyButton,
													Layout.ButtonPressMode.HoldToRepeat
												),
												new Layout.Button(
													widthPercentage,
													null,
													ConverterBackspace,
													backspaceButton,
													Layout.ButtonPressMode.HoldToRepeat
												)
											),
										};

									int converterKeyboardY =
										converterBoxHeight * 2 + leftButtonSize + Padding;

									Layout.DrawButtonGrid(
										leftButtonSize + Padding,
										converterKeyboardY,
										ScreenWidth - leftButtonSize - Padding,
										ScreenHeight - converterKeyboardY - Padding,
										Padding,
										converterKeyboardButtons
									);

									// TODO(LucasTA): remove repetition when handling inputs
									int keycode = Raylib.GetCharPressed();

									if (Raylib.IsKeyPressed(KeyboardKey.KEY_LEFT))
									{
										ConverterTypingIndex = Math.Max(
											0,
											ConverterTypingIndex - 1
										);
										ButtonPressedTime = 0;
									}
									else if (Raylib.IsKeyPressed(KeyboardKey.KEY_RIGHT))
									{
										ConverterTypingIndex = Math.Min(
											ConverterExpression.Length,
											ConverterTypingIndex + 1
										);
										ButtonPressedTime = 0;
									}
									else if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT))
									{
										if (ButtonPressedTime >= INITIAL_REPEAT_INTERVAL)
										{
											ConverterTypingIndex = Math.Max(
												0,
												ConverterTypingIndex - 1
											);
										}

										ButtonPressedTime += Raylib.GetFrameTime();
									}
									else if (Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT))
									{
										if (ButtonPressedTime >= INITIAL_REPEAT_INTERVAL)
										{
											ConverterTypingIndex = Math.Min(
												ConverterExpression.Length,
												ConverterTypingIndex + 1
											);
										}

										ButtonPressedTime += Raylib.GetFrameTime();
									}
									else if (
										(
											Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL)
											|| Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_CONTROL)
										) && Raylib.IsKeyPressed(KeyboardKey.KEY_C)
									)
									{
										Clipboard.Set(ConverterExpression);
										ButtonPressedTime = 0;
									}
									else if (
										(
											Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL)
											|| Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_CONTROL)
										) && Raylib.IsKeyPressed(KeyboardKey.KEY_V)
									)
									{
										InsertConverterExpression(Clipboard.Get());
										ButtonPressedTime = 0;
									}
									else if (Raylib.IsKeyPressed(KeyboardKey.KEY_BACKSPACE))
									{
										ConverterBackspace();
										ButtonPressedTime = 0;
									}
									else if (Raylib.IsKeyDown(KeyboardKey.KEY_BACKSPACE))
									{
										if (ButtonPressedTime >= INITIAL_REPEAT_INTERVAL)
										{
											ConverterBackspace();
										}

										ButtonPressedTime += Raylib.GetFrameTime();
									}
									else if (keycode != 0)
									{
										if (
											char.IsAsciiDigit((char)keycode)
											|| (char)keycode == '.'
										)
										{
											InsertConverterExpression(((char)keycode).ToString());
											ButtonPressedTime = 0;
										}
									}
								}
							}
							break;
						case Scene.Conversions:
							{
								if (Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE))
								{
									CurrentScene = Scene.Converters;
								}

								int conversionsAmount = Converters[CurrentConverter]
									.Conversions
									.Count;

#if ANDROID
								if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
								{
									StartTouchPosition = Raylib.GetTouchPosition(0);
								}

								if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT))
								{
									Vector2 currentTouchPosition = Raylib.GetTouchPosition(0);

									float touchMoveDistance = Math.Abs(
										currentTouchPosition.Y - StartTouchPosition.Y
									);

									if (touchMoveDistance > 2)
									{
										Dragging = true;

										DropDownScrollOffset = Math.Clamp(
											DropDownScrollOffset
												+ currentTouchPosition.Y
												- StartTouchPosition.Y,
											Math.Max(0, conversionsAmount - dropDownVisibleEntries)
												* -dropDownEntryHeight,
											0
										);

										StartTouchPosition = currentTouchPosition;
									}
								}
#else
								if (MouseScroll > 0)
								{
									Dragging = true;
								}

								DropDownScrollOffset = Math.Clamp(
									DropDownScrollOffset + MouseScroll,
									Math.Max(0, conversionsAmount - dropDownVisibleEntries)
										* -dropDownEntryHeight,
									0
								);
#endif

								for (int i = 0; i < conversionsAmount; i++)
								{
									string converter = Converters[CurrentConverter]
										.Conversions[i]
										.Name;
									int selectedIndex =
										CurrentDropDown == DropDown.From
											? ConverterFromIndex
											: ConverterToIndex;

									Layout.DrawButton(
										0,
										(int)DropDownScrollOffset + i * dropDownEntryHeight,
										ScreenWidth,
										dropDownEntryHeight,
										selectedIndex == i
											? ButtonPressedColor
											: MenuEntryBackgroundColor,
										MenuEntryBackgroundColor,
										MenuEntryBackgroundColor,
										() =>
										{
											if (CurrentDropDown == DropDown.From)
											{
												ConverterFromIndex = i;
											}
											else if (CurrentDropDown == DropDown.To)
											{
												ConverterToIndex = i;
											}

											Conversion.Convert();
											CurrentScene = Scene.Converters;
										},
										textFormat: new(converter, FontSize, ForegroundColor),
										borderStyle: new(
											selectedIndex == i ? ButtonSelectedColor : BorderColor,
											BorderThickness
										)
									);
								}
							}
							break;
						default:
							{
								Log.Halt("Unknown scene");
							}
							break;
					}

					if (Raylib.IsMouseButtonReleased(MouseButton.MOUSE_BUTTON_LEFT))
					{
						Dragging = false;
					}

					Layout.LastHotButton = Layout.HotButton;
					Layout.HotButton = null;

					Log.Message += $"""
FPS: {Raylib.GetFPS()}
""";

					Log.Print();
					Log.Message = "";

					Raylib.EndDrawing();
				}
			}

			History.Save();
			Raylib.CloseWindow();
		}
	}
}
