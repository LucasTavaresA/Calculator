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
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;

using Eval;

using Raylib_cs;
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

	private static void InsertExpression(int index, string value)
	{
		Expression = Expression.Insert(index, value);
		ErrorMessage = "";

		try
		{
			Result = Evaluator.Evaluate(Expression).ToString("G", CultureInfo.InvariantCulture);
		}
		catch (Exception)
		{
			Result = "";
		}

		TypingIndex += value.Length;
	}

	internal static readonly Action Paste = () =>
	{
		InsertExpression(TypingIndex, Clipboard.Get());
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
			Result = Evaluator.Evaluate(Expression).ToString("G", CultureInfo.InvariantCulture);
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
					.ToString("G", CultureInfo.InvariantCulture);
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
	}

	internal static Scene CurrentScene = Scene.Calculator;

	[DllImport("raylib", CallingConvention = CallingConvention.Cdecl)]
	static extern Image LoadImageFromMemory(string fileType, byte[] fileData, int dataSize);

	public static Texture2D LoadTextureFromResource(string resourceName)
	{
		Texture2D texture;

		using (
			Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)!
		)
		{
			if (stream == null)
				throw new ArgumentException($"Resource '{resourceName}' not found.");

			using (MemoryStream memoryStream = new MemoryStream())
			{
				stream.CopyTo(memoryStream);

				Image image = LoadImageFromMemory(
					".png",
					memoryStream.ToArray(),
					(int)memoryStream.Length
				);
				texture = Raylib.LoadTextureFromImage(image);
				Raylib.UnloadImage(image);
			}
		}

		return texture;
	}

	[DllImport("raylib", CallingConvention = CallingConvention.Cdecl)]
	static extern unsafe Font LoadFontFromMemory(
		string fileType,
		byte[] fileData,
		int dataSize,
		int fontSize,
		int* codepoints,
		int codepointCount
	);

	public static unsafe Font LoadFontFromResource(string resourceName, int fontSize)
	{
		Font font;

		using (
			Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)!
		)
		{
			if (stream == null)
				throw new ArgumentException($"Resource '{resourceName}' not found.");

			using (MemoryStream memoryStream = new MemoryStream())
			{
				stream.CopyTo(memoryStream);
				font = LoadFontFromMemory(
					".ttf",
					memoryStream.ToArray(),
					(int)memoryStream.Length,
					fontSize,
					null,
					256
				);
			}
		}

		return font;
	}

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

			History.Load();
			Settings.Load();

			// NOTE(LucasTA): Without HIGHDPI the font has artifacts, so we load
			// it realy big and then scale it down
			// just the filter is really blurry
			Fonte = LoadFontFromResource("CalculatorUI.Resources.iosevka-regular.ttf", 64);
			Raylib.SetTextureFilter(Fonte.Texture, TextureFilter.TEXTURE_FILTER_BILINEAR);

			Texture2D plusTexture = LoadTextureFromResource("CalculatorUI.Resources.plus_icon.png");
			Texture2D historyTexture = LoadTextureFromResource(
				"CalculatorUI.Resources.history_icon.png"
			);
			Texture2D closeTexture = LoadTextureFromResource(
				"CalculatorUI.Resources.close_icon.png"
			);
			Texture2D copyTexture = LoadTextureFromResource("CalculatorUI.Resources.copy_icon.png");
			Texture2D pasteTexture = LoadTextureFromResource(
				"CalculatorUI.Resources.paste_icon.png"
			);
			Texture2D openTexture = LoadTextureFromResource("CalculatorUI.Resources.open_icon.png");
			Texture2D pinTexture = LoadTextureFromResource("CalculatorUI.Resources.pin_icon.png");
			Texture2D unpinTexture = LoadTextureFromResource(
				"CalculatorUI.Resources.unpin_icon.png"
			);
			Texture2D piTexture = LoadTextureFromResource("CalculatorUI.Resources.pi_icon.png");
			Texture2D trashTexture = LoadTextureFromResource(
				"CalculatorUI.Resources.trash_icon.png"
			);
			Texture2D trashAllTexture = LoadTextureFromResource(
				"CalculatorUI.Resources.trash_all_icon.png"
			);
			Texture2D bookmarkAddTexture = LoadTextureFromResource(
				"CalculatorUI.Resources.bookmark_add_icon.png"
			);
			Texture2D settingsTexture = LoadTextureFromResource(
				"CalculatorUI.Resources.settings_icon.png"
			);
			Texture2D toggleOnTexture = LoadTextureFromResource(
				"CalculatorUI.Resources.toggle_on_icon.png"
			);
			Texture2D toggleOffTexture = LoadTextureFromResource(
				"CalculatorUI.Resources.toggle_off_icon.png"
			);
			Texture2D githubTexture = LoadTextureFromResource(
				"CalculatorUI.Resources.github_icon.png"
			);
			Texture2D arrowLeftTexture = LoadTextureFromResource(
				"CalculatorUI.Resources.arrow_left.png"
			);
			Texture2D arrowRightTexture = LoadTextureFromResource(
				"CalculatorUI.Resources.arrow_right.png"
			);

			Raylib.SetWindowIcon(Raylib.LoadImageFromTexture(plusTexture));

			while (!Raylib.WindowShouldClose())
			{
				// get screen information
				{
					ScreenWidth = Raylib.GetScreenWidth();
					ScreenHeight = Raylib.GetScreenHeight();
					FontSize = (ScreenWidth < ScreenHeight ? ScreenWidth : ScreenHeight) / 15;
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

					int DisplayX = Padding;
					int DisplayY = Padding;
					int DisplayWidth = ScreenWidth - (Padding * 2);
					int DisplayHeight = (ScreenHeight / 6) - Padding;

					Vector2 FontTextSize = Raylib.MeasureTextEx(
						CalculatorUI.Fonte,
						"0",
						FontSize,
						CalculatorUI.FONT_SPACING
					);
					int topIconSize = DisplayY + (int)FontTextSize.Y;

					int menuSidePadding = Padding * 2 + topIconSize;
					int menuEntryHeight = ((int)FontTextSize.Y * 2) + Padding;
					int menuEntryX = 0;
					int menuEntryWidth = ScreenWidth - menuSidePadding;
					int menuVisibleEntries = ScreenHeight / menuEntryHeight;

					switch (CurrentScene)
					{
						case Scene.Calculator:
							// Draw buttons
							{
								Layout.ShadowStyle GreyButtonShadow =
									new(
										ButtonShadowColor,
										ShadowDistance,
										Layout.ShadowKind.Pillar
									);
								Layout.ShadowStyle RedButtonShadow =
									new(
										RedButtonShadowColor,
										ShadowDistance,
										Layout.ShadowKind.Pillar
									);
								Layout.ShadowStyle GreenButtonShadow =
									new(
										GreenButtonShadowColor,
										ShadowDistance,
										Layout.ShadowKind.Pillar
									);

								Layout.ButtonStyle GreyButton =
									new(
										BackgroundColor: ButtonBackgroundColor,
										PressedColor: ButtonPressedColor,
										HoveredColor: ButtonHoverColor,
										new(BorderColor, BorderThickness),
										ShadowStyle: GreyButtonShadow
									);

								Layout.ButtonStyle RedButton =
									new(
										BackgroundColor: RedButtonColor,
										PressedColor: RedButtonPressedColor,
										HoveredColor: RedButtonHoveredColor,
										new(RedButtonBorderColor, BorderThickness),
										ShadowStyle: RedButtonShadow
									);

								Layout.ButtonStyle GreenButton =
									new(
										BackgroundColor: GreenButtonColor,
										PressedColor: GreenButtonPressedColor,
										HoveredColor: GreenButtonHoveredColor,
										new(GreenButtonBorderColor, BorderThickness),
										ShadowStyle: GreenButtonShadow
									);

								int rowAmount = 7;
								int heightPercentage = 100 / rowAmount;

								Layout.ButtonRow[] ButtonGrid = new Layout.ButtonRow[]
								{
									new Layout.ButtonRow(
										heightPercentage,
										new Layout.Button(
											20,
											new("(", FontSize, ForegroundColor),
											() => InsertExpression(TypingIndex, "("),
											GreyButton,
											Layout.ButtonPressMode.HoldToRepeat
										),
										new Layout.Button(
											20,
											new(")", FontSize, ForegroundColor),
											() => InsertExpression(TypingIndex, ")"),
											GreyButton,
											Layout.ButtonPressMode.HoldToRepeat
										),
										new Layout.Button(
											20,
											new(",", FontSize, ForegroundColor),
											() => InsertExpression(TypingIndex, ","),
											GreyButton
										),
										new Layout.Button(
											20,
											new("^", FontSize, ForegroundColor),
											() => InsertExpression(TypingIndex, "^"),
											GreyButton
										),
										new Layout.Button(
											20,
											null,
											() => InsertExpression(TypingIndex, "pi"),
											new(
												BackgroundColor: ButtonBackgroundColor,
												PressedColor: ButtonPressedColor,
												HoveredColor: ButtonHoverColor,
												new(BorderColor, BorderThickness),
												ShadowStyle: GreyButtonShadow,
												Icon: new(
													piTexture,
													ForegroundColor,
													(int)FontTextSize.Y
												)
											)
										)
									),
									new Layout.ButtonRow(
										heightPercentage,
										new Layout.Button(
											20,
											new("!", FontSize, ForegroundColor),
											() => InsertExpression(TypingIndex, "!"),
											GreyButton
										),
										new Layout.Button(
											20,
											new("e", FontSize, ForegroundColor),
											() => InsertExpression(TypingIndex, "e"),
											GreyButton
										),
										new Layout.Button(
											20,
											new("%", FontSize, ForegroundColor),
											() => InsertExpression(TypingIndex, "%"),
											GreyButton
										),
										new Layout.Button(
											20,
											new("/", FontSize, ForegroundColor),
											() => InsertExpression(TypingIndex, "/"),
											GreyButton
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
											GreyButton
										)
									),
									new Layout.ButtonRow(
										heightPercentage,
										new Layout.Button(
											25,
											new("7", FontSize, ForegroundColor),
											() => InsertExpression(TypingIndex, "7"),
											GreyButton,
											Layout.ButtonPressMode.HoldToRepeat
										),
										new Layout.Button(
											25,
											new("8", FontSize, ForegroundColor),
											() => InsertExpression(TypingIndex, "8"),
											GreyButton,
											Layout.ButtonPressMode.HoldToRepeat
										),
										new Layout.Button(
											25,
											new("9", FontSize, ForegroundColor),
											() => InsertExpression(TypingIndex, "9"),
											GreyButton,
											Layout.ButtonPressMode.HoldToRepeat
										),
										new Layout.Button(
											25,
											new("*", FontSize, ForegroundColor),
											() => InsertExpression(TypingIndex, "*"),
											GreyButton
										)
									),
									new Layout.ButtonRow(
										heightPercentage,
										new Layout.Button(
											25,
											new("4", FontSize, ForegroundColor),
											() => InsertExpression(TypingIndex, "4"),
											GreyButton,
											Layout.ButtonPressMode.HoldToRepeat
										),
										new Layout.Button(
											25,
											new("5", FontSize, ForegroundColor),
											() => InsertExpression(TypingIndex, "5"),
											GreyButton,
											Layout.ButtonPressMode.HoldToRepeat
										),
										new Layout.Button(
											25,
											new("6", FontSize, ForegroundColor),
											() => InsertExpression(TypingIndex, "6"),
											GreyButton,
											Layout.ButtonPressMode.HoldToRepeat
										),
										new Layout.Button(
											25,
											new("-", FontSize, ForegroundColor),
											() => InsertExpression(TypingIndex, "-"),
											GreyButton
										)
									),
									new Layout.ButtonRow(
										heightPercentage,
										new Layout.Button(
											25,
											new("1", FontSize, ForegroundColor),
											() => InsertExpression(TypingIndex, "1"),
											GreyButton,
											Layout.ButtonPressMode.HoldToRepeat
										),
										new Layout.Button(
											25,
											new("2", FontSize, ForegroundColor),
											() => InsertExpression(TypingIndex, "2"),
											GreyButton,
											Layout.ButtonPressMode.HoldToRepeat
										),
										new Layout.Button(
											25,
											new("3", FontSize, ForegroundColor),
											() => InsertExpression(TypingIndex, "3"),
											GreyButton,
											Layout.ButtonPressMode.HoldToRepeat
										),
										new Layout.Button(
											25,
											new("+", FontSize, ForegroundColor),
											() => InsertExpression(TypingIndex, "+"),
											GreyButton
										)
									),
									new Layout.ButtonRow(
										heightPercentage,
										new Layout.Button(
											25,
											new("0", FontSize, ForegroundColor),
											() => InsertExpression(TypingIndex, "0"),
											GreyButton,
											Layout.ButtonPressMode.HoldToRepeat
										),
										new Layout.Button(
											25,
											new(".", FontSize, ForegroundColor),
											() => InsertExpression(TypingIndex, "."),
											GreyButton
										),
										new Layout.Button(
											25,
											new("<-", FontSize, ForegroundColor),
											Backspace,
											RedButton,
											Layout.ButtonPressMode.HoldToRepeat
										),
										new Layout.Button(
											25,
											new("=", FontSize, ForegroundColor),
											Equal,
											GreenButton
										)
									),
									new Layout.ButtonRow(
										heightPercentage,
										new Layout.Button(
											17,
											new("sqrt", FontSize, ForegroundColor),
											() => InsertExpression(TypingIndex, "sqrt("),
											GreyButton
										),
										new Layout.Button(
											17,
											new("mod", FontSize, ForegroundColor),
											() => InsertExpression(TypingIndex, "mod("),
											GreyButton
										),
										new Layout.Button(
											17,
											new("sin", FontSize, ForegroundColor),
											() => InsertExpression(TypingIndex, "sin("),
											GreyButton
										),
										new Layout.Button(
											17,
											new("cos", FontSize, ForegroundColor),
											() => InsertExpression(TypingIndex, "cos("),
											GreyButton
										),
										new Layout.Button(
											16,
											new("tan", FontSize, ForegroundColor),
											() => InsertExpression(TypingIndex, "tan("),
											GreyButton
										),
										new Layout.Button(
											16,
											new("log", FontSize, ForegroundColor),
											() => InsertExpression(TypingIndex, "log("),
											GreyButton
										)
									),
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
								bool error = ErrorMessage == "";
								string displayedExpression = Expression.Insert(TypingIndex, "|");

								Layout.DrawButton(
									DisplayX,
									DisplayY,
									DisplayWidth / 2,
									DisplayHeight,
									Transparent,
									ButtonPressedColor,
									TransparentButtonHoverColor,
									() =>
									{
										TypingIndex = Math.Max(0, TypingIndex - 1);
									},
									pressMode: Layout.ButtonPressMode.HoldToRepeat,
									icon: new(arrowLeftTexture, DisplayBackgroundColor)
								);

								Layout.DrawButton(
									DisplayX + DisplayWidth / 2,
									DisplayY,
									DisplayWidth / 2,
									DisplayHeight,
									Transparent,
									ButtonPressedColor,
									TransparentButtonHoverColor,
									() =>
									{
										TypingIndex = Math.Min(Expression.Length, TypingIndex + 1);
									},
									pressMode: Layout.ButtonPressMode.HoldToRepeat,
									icon: new(arrowRightTexture, DisplayBackgroundColor)
								);

								Layout.DrawTextBox(
									DisplayX,
									DisplayY,
									DisplayWidth,
									DisplayHeight,
									new(
										displayedExpression,
										FontSize,
										ForegroundColor,
										Layout.TextAlignment.Center,
										Layout.OverflowMode.Shrink
									),
									// NOTE(LucasTA): so that the arrows draw behind are visible
									TransparentButtonHoverColor,
									new(error ? BorderColor : ErrorColor, BorderThickness * 2)
								);

								Layout.DrawText(
									DisplayX,
									DisplayY + BorderThickness * 2,
									DisplayWidth,
									DisplayHeight,
									BorderThickness * 2,
									error ? Result : ErrorMessage,
									error ? DarkForegroundColor : ErrorColor,
									DisplayBackgroundColor,
									FontSize,
									Layout.TextAlignment.BottomLeft,
									Layout.OverflowMode.Shrink
								);
							}

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
									TypingIndex = Math.Min(Expression.Length, TypingIndex + 1);
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
										TypingIndex,
										((char)keycode).ToString().ToLowerInvariant()
									);
									ButtonPressedTime = 0;
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
									icon: new(settingsTexture, ForegroundColor)
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
									icon: new(historyTexture, ForegroundColor)
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
									icon: new(copyTexture, ForegroundColor)
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
									icon: new(pasteTexture, ForegroundColor)
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
									icon: new(bookmarkAddTexture, ForegroundColor)
								);
							}
							break;
						case Scene.History:
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
								icon: new(closeTexture, ForegroundColor)
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
								icon: new(trashAllTexture, RedButtonColor),
								pressMode: Layout.ButtonPressMode.HoldToPress
							);

							{
								for (int i = 0; i < expressions.Count; i++)
								{
									int menuEntryY = (int)HistoryScrollOffset + i * menuEntryHeight;

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
										icon: new(trashTexture, RedButtonColor),
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
										icon: new(copyTexture, ForegroundColor)
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
													.ToString("G", CultureInfo.InvariantCulture);
											}
											catch (Exception)
											{
												Result = "";
											}

											TypingIndex = Expression.Length;
											CurrentScene = Scene.Calculator;
										},
										icon: new(openTexture, ForegroundColor)
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
											pinned ? unpinTexture : pinTexture,
											ForegroundColor
										)
									);
								}
							}
							break;
						case Scene.Settings:
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
								icon: new(closeTexture, ForegroundColor)
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
									Settings.BookmarkOnEval ? toggleOnTexture : toggleOffTexture,
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
								Layout.TextAlignment.Left,
								Layout.OverflowMode.Shrink
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
									Layout.TextAlignment.Left,
									Layout.OverflowMode.Shrink
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
								Layout.TextAlignment.Right,
								Layout.OverflowMode.Shrink
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
									Layout.TextAlignment.Left,
									Layout.OverflowMode.Shrink
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
								Layout.TextAlignment.Right,
								Layout.OverflowMode.Shrink
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
								() => OpenBrowser("https://github.com/lucastavaresa/Calculator"),
								icon: new(githubTexture, Color.WHITE)
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
								Layout.TextAlignment.Left,
								Layout.OverflowMode.Shrink
							);
							break;
						default:
							throw new UnreachableException("Unknown scene");
					}

					if (Raylib.IsMouseButtonReleased(MouseButton.MOUSE_BUTTON_LEFT))
					{
						Dragging = false;
					}

					Layout.LastHotButton = Layout.HotButton;
					Layout.HotButton = null;

					Log.Message = $"""
Resolution: {ScreenWidth}x{ScreenHeight}
FPS: {Raylib.GetFPS()}
MouseXY: {MouseX}x{MouseY}
MousePressedXY: {MousePressedX}x{MousePressedY}
BorderThickness: {BorderThickness}
ShadowDistance: {ShadowDistance}
Padding: {Padding}
{Log.Message}
""";

					Log.Draw();
					Log.Message = "";

					Raylib.EndDrawing();
				}
			}

			History.Save();
			Raylib.CloseWindow();
		}
	}
}
