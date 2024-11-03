// Licensed under the GPL3 or later versions of the GPL license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;

using static Calculator.Translations;

namespace Calculator;

internal readonly struct Conversions
{
	internal static int CurrentConverter = 0;
	internal static int ConverterTypingIndex = 0;
	internal static string ConverterExpression = "";
	internal static string ConverterResult = "";
	internal static int ConverterFromIndex = 0;
	internal static int ConverterToIndex = 0;

	internal readonly record struct Conversion(string Name, double Rate);

	internal readonly record struct Converter(
		string Title,
		string Icon,
		List<Conversion> Conversions
	);

	internal static void Convert()
	{
		if (
			double.TryParse(
				ConverterExpression,
				NumberStyles.Float,
				CultureInfo.InvariantCulture,
				out double number
			)
		)
		{
			if (ConverterFromIndex == ConverterToIndex)
			{
				ConverterResult = number.ToString("0.##########", CultureInfo.InvariantCulture);
			}
			else if (Converters[CurrentConverter].Title == "Temperature")
			{
				number = ConvertTemperature(
					number,
					Converters[CurrentConverter].Conversions[ConverterFromIndex].Name,
					Converters[CurrentConverter].Conversions[ConverterToIndex].Name
				);
			}
			else
			{
				// NOTE(LucasTA): Reversed for currency rates as they are the only converter with all bases derived from USD
				if (Converters[CurrentConverter].Title == "Currency")
				{
					number = ConvertFromBase(
						number,
						Converters[CurrentConverter].Conversions[ConverterToIndex].Rate,
						Converters[CurrentConverter].Conversions[ConverterFromIndex].Rate
					);
				}
				else
				{
					number = ConvertFromBase(
						number,
						Converters[CurrentConverter].Conversions[ConverterFromIndex].Rate,
						Converters[CurrentConverter].Conversions[ConverterToIndex].Rate
					);
				}
			}

			ConverterResult = number.ToString("0.##########", CultureInfo.InvariantCulture);
		}
		else
		{
			ConverterResult = "";
		}
	}

	internal static void ConverterBackspace()
	{
		if (ConverterExpression == "" || ConverterTypingIndex == 0)
		{
			return;
		}

		ConverterExpression = ConverterExpression.Remove(ConverterTypingIndex - 1, 1);
		ConverterTypingIndex = Math.Max(0, ConverterTypingIndex - 1);

		Convert();
	}

	internal static void InsertConverterExpression(string value)
	{
		ConverterExpression = ConverterExpression.Insert(ConverterTypingIndex, value);
		ConverterTypingIndex += value.Length;
		Convert();
	}

	private static double ConvertFromBase(double number, double fromBase, double toBase)
	{
		return number * fromBase / toBase;
	}

	private static double ConvertTemperature(double number, string from, string to)
	{
		if (from == "Fahrenheit")
		{
			number = (number - 32) * 5.0 / 9.0;
		}
		else if (from == "Kelvin")
		{
			number -= 273.15;
		}

		if (to == "Fahrenheit")
		{
			number = (number * 9.0 / 5.0) + 32;
		}
		else if (to == "Kelvin")
		{
			number += 273.15;
		}

		return number;
	}

	internal static readonly Converter[] Converters =
	[
#if HAS_APIKEY
		new("Currency", "money_icon.png", Currency.Load()),
#else
		new("App was not compiled with an openexchangerates.org API key.", "money_icon.png", new()),
#endif
		new(
			"Volume",
			"flask_icon.png",
			[
				new("Liter", 1),
				new("Milliliter", 0.001),
				new("Cubic meter", 1000),
				new("Cubic kilometer", 1000000000000),
				new("Cubic centimeter", 0.001),
				new("Cubic millimeter", 0.000001),
				new("US liquid gallon", 3.78541),
				new("US liquid quart", 0.9463525),
				new("US liquid pint", 0.47317625),
				new("US legal cup", 0.236588125),
				new("US fluid ounce", 0.0295735156),
				new("US tablespoon", 0.0147867578),
				new("US teaspoon", 0.0049289193),
				new("Imperial gallon", 4.54609),
				new("Imperial quart", 1.1365225),
				new("Imperial pint", 0.56826125),
				new("Imperial cup", 0.284131),
				new("Imperial fluid ounce", 0.0284130625),
				new("Imperial tablespoon", 0.0177581641),
				new("Imperial teaspoon", 0.005919388),
				new("Cubic mile", 4168180000000),
				new("Cubic yard", 764.55485798),
				new("Cubic foot", 28.316846592),
				new("Cubic inch", 0.016387064),
			]
		),
		new(
			"Length",
			"ruler_icon.png",
			[
				new("Meter", 1),
				new("Centimeter", 0.01),
				new("Kilometer", 1000),
				new("Millimeter", 0.001),
				new("Micrometer", 0.000001),
				new("Nanometer", 1.0e-9),
				new("Mile", 1609.35),
				new("Yard", 0.9144),
				new("Foot", 0.3048),
				new("Inch", 0.0254),
				new("Light Year", 9460660000000000),
				new("Nautical mile", 1852),
			]
		),
		new(
			"Weight/Mass",
			"weight_icon.png",
			[
				new("Kilogram", 1),
				new("Gram", 0.001),
				new("Milligram", 0.000001),
				new("Ton", 1000),
				new("Imperial ton", 1016.04608),
				new("US ton", 907.185),
				new("Microgram", 1.0e-9),
				new("Pound", 0.453592),
				new("Ounce", 0.0283495),
				new("Carat", 0.0002),
				new("Stone", 6.35029),
				new("Atomic mass unit", 1.660540199e-27),
			]
		),
		new(
			"Temperature",
			"termometer_icon.png",
			[new("Degree Celsius", 1), new("Fahrenheit", -272.15), new("Kelvin", -17.2222)]
		),
		new(
			"Energy",
			"fire_icon.png",
			[
				new("Gram calorie", 1),
				new("Kilocalorie", 1000),
				new("Watt hour", 860.421),
				new("Kilowatt-hour", 860421),
				new("Joule", 0.239006),
				new("Kilojoule", 239.006),
				new("Electronvolt", 3.8293e-20),
				new("British thermal unit", 252.164),
				new("US therm", 2.521e+7),
				new("Foot-pound", 0.324048),
			]
		),
		new(
			"Area",
			"cube_icon.png",
			[
				new("Square meter", 1),
				new("Square kilometer [km^2]", 1e+6),
				new("Square centimeter [cm^2]", 0.0001),
				new("Square millimeter [mm^2]", 0.000001),
				new("Square micrometer [µm^2]", 1.0e-12),
				new("Square mile [mi^2]", 2.59e+6),
				new("Square yard [yd^2]", 0.836127),
				new("Square foot [ft^2]", 0.092903),
				new("Square inch [in^2]", 0.00064516),
				new("Hectare [ha]", 10000),
				new("Acre [ac]", 4046.86),
				new("Square hectometer [hm^2]", 10000),
				new("Square dekameter [dam^2]", 100),
				new("Square decimeter [dm^2]", 0.01),
				new("Square nanometer [nm^2]", 1.0e-18),
				new("Are [a]", 100),
				new("Barn [b]", 1.0e-28),
				new("Square mile (US survey)", 2589998.4703),
				new("Square foot (US survey)", 0.0929034116),
				new("Circular inch", 0.0005067075),
				new("Township", 93239571.972),
				new("Section", 2589988.1103),
				new("Acre (US survey) [ac]", 4046.8726099),
				new("Rood", 1011.7141056),
				new("Square chain [ch^2]", 404.68564224),
				new("Square rod", 25.29285264),
				new("Square rod (US survey)", 25.292953812),
				new("Square perch", 25.29285264),
				new("Square pole", 25.29285264),
				new("Square mil [mil^2]", 6.4516e-10),
				new("Circular mil", 5.06707479e-10),
				new("Homestead", 647497.02758),
				new("Sabin", 0.09290304),
				new("Arpent", 3418.8929237),
				new("Cuerda", 3930.395625),
				new("Plaza", 6400),
				new("Varas castellanas cuad", 0.698737),
				new("Varas conuqueras cuad", 6.288633),
				new("Electron cross section", 6.652461599e-29),
			]
		),
		new(
			"Velocity",
			"speedometer_icon.png",
			[
				new("kilometer/hour [km/h]", 1),
				new("meter/second [m/s]", 3.6),
				new("mile/hour [mi/h]", 1.60934),
				new("foot/second [ft/s]", 1.09728),
				new("knot [kt, kn]", 1.852),
				new("meter/hour [m/h]", 0.001),
				new("meter/minute [m/min]", 0.06),
				new("kilometer/minute [km/min]", 60),
				new("kilometer/second [km/s]", 3600),
				new("centimeter/hour [cm/h]", 0.00001),
				new("centimeter/minute [cm/min]", 0.0006),
				new("centimeter/second [cm/s]", 0.036),
				new("millimeter/hour [mm/h]", 0.000001),
				new("millimeter/minute [mm/min]", 0.00006),
				new("millimeter/second [mm/s]", 0.0036),
				new("foot/hour [ft/h]", 0.0003048),
				new("foot/minute [ft/min]", 0.018288),
				new("yard/hour [yd/h]", 0.0009144),
				new("yard/minute [yd/min]", 0.054864),
				new("yard/second [yd/s]", 3.29184),
				new("mile/minute [mi/min]", 96.56064),
				new("mile/second [mi/s]", 5793.6384),
				new("knot (UK) [kt (UK)]", 1.853184),
				new("Velocity of light in vacuum", 1079252848.8),
				new("Cosmic velocity - first", 28440),
				new("Cosmic velocity - second", 40320),
				new("Cosmic velocity - third", 60012),
				new("Earth's velocity", 107154),
				new("Velocity of sound in pure water", 5337.72),
				new("Velocity of sound in sea water (20°C, 10 meter deep)", 5477.76),
				new("Mach (20°C, 1 atm)", 1236.96),
				new("Mach (SI standard)", 1062.16704),
			]
		),
		new(
			"Time",
			"clock_icon.png",
			[
				new("second [s]", 1),
				new("milisecond [ms]", 0.001),
				new("minute [min]", 60),
				new("hour [h]", 3600),
				new("day [d]", 86400),
				new("week", 604800),
				new("month", 2628000),
				new("year [y]", 31557600),
				new("decade", 315576000),
				new("century", 3155760000),
				new("millennium", 31557600000),
				new("microsecond [µs]", 0.000001),
				new("nanosecond [ns]", 1.0e-9),
				new("picosecond [ps]", 1.0e-12),
				new("femtosecond [fs]", 9.999999999e-16),
				new("attosecond [as]", 1.0e-18),
				new("shake", 1.0e-8),
				new("month (synodic)", 2551443.84),
				new("year (Julian)", 31557600),
				new("year (leap)", 31622400),
				new("year (tropical)", 31556930),
				new("year (sidereal)", 31558149.54),
				new("day (sidereal)", 86164.09),
				new("hour (sidereal)", 3590.1704167),
				new("minute (sidereal)", 59.836173611),
				new("second (sidereal)", 0.9972695602),
				new("fortnight", 1209600),
				new("septennial", 220752000),
				new("octennial", 252288000),
				new("novennial", 283824000),
				new("quindecennial", 473040000),
				new("quinquennial", 157680000),
				new("Planck time", 5.390559999e-44),
			]
		),
		new(
			"Power",
			"shock_icon.png",
			[
				new("watt [W]", 1),
				new("milliwatt [mW]", 0.001),
				new("kilowatt [kW]", 1000),
				new("megawatt [MW]", 1000000),
				new("gigawatt [GW]", 1000000000),
				new("terawatt [TW]", 1000000000000),
				new("petawatt [PW]", 1000000000000000),
				new("exawatt [EW]", 1000000000000000000),
				new("hectowatt [hW]", 100),
				new("dekawatt [daW]", 10),
				new("deciwatt [dW]", 0.1),
				new("centiwatt [cW]", 0.01),
				new("milliwatt [mW]", 0.001),
				new("microwatt [µW]", 0.000001),
				new("nanowatt [nW]", 1.0e-9),
				new("picowatt [pW]", 1.0e-12),
				new("femtowatt [fW]", 1.0e-15),
				new("attowatt [aW]", 9.999999999e-19),
				new("horsepower [hp, hp (UK)]", 745.69987158),
				new("horsepower (550 ft*lbf/s)", 745.69987158),
				new("horsepower (metric)", 735.49875),
				new("horsepower (boiler)", 9809.5),
				new("horsepower (electric)", 746),
				new("horsepower (water)", 746.043),
				new("pferdestarke (ps)", 735.49875),
				new("Btu (IT)/hour [Btu/h]", 0.2930710702),
				new("Btu (IT)/minute [Btu/min]", 17.58426421),
				new("Btu (IT)/second [Btu/s]", 1055.0558526),
				new("Btu (th)/hour [Btu (th)/h]", 0.292875),
				new("Btu (th)/minute", 17.5725),
				new("Btu (th)/second [Btu (th)/s]", 1054.35),
				new("MBtu (IT)/hour [MBtu/h]", 293071.07017),
				new("MBH", 293.07107017),
				new("ton (refrigeration)", 3516.8528421),
				new("kilocalorie (IT)/hour [kcal/h]", 1.163),
				new("kilocalorie (IT)/minute", 69.78),
				new("kilocalorie (IT)/second", 4186.8),
				new("kilocalorie (th)/hour", 1.1622222222),
				new("kilocalorie (th)/minute", 69.733333333),
				new("kilocalorie (th)/second", 4184),
				new("calorie (IT)/hour [cal/h]", 0.001163),
				new("calorie (IT)/minute [cal/min]", 0.06978),
				new("calorie (IT)/second [cal/s]", 4.1868),
				new("calorie (th)/hour [cal (th)/h]", 0.0011622222),
				new("calorie (th)/minute", 0.0697333333),
				new("calorie (th)/second", 4.184),
				new("foot pound-force/hour", 0.0003766161),
				new("foot pound-force/minute", 0.0225969658),
				new("foot pound-force/second", 1.3558179483),
				new("pound-foot/hour [lbf*ft/h]", 0.0003766161),
				new("pound-foot/minute", 0.0225969658),
				new("pound-foot/second", 1.3558179483),
				new("erg/second [erg/s]", 1.0e-7),
				new("kilovolt ampere [kV*A]", 1000),
				new("volt ampere [V*A]", 1),
				new("newton meter/second", 1),
				new("joule/second [J/s]", 1),
				new("exajoule/second [EJ/s]", 1000000000000000000),
				new("petajoule/second [PJ/s]", 1000000000000000),
				new("terajoule/second [TJ/s]", 1000000000000),
				new("gigajoule/second [GJ/s]", 1000000000),
				new("megajoule/second [MJ/s]", 1000000),
				new("kilojoule/second [kJ/s]", 1000),
				new("hectojoule/second [hJ/s]", 100),
				new("dekajoule/second [daJ/s]", 10),
				new("decijoule/second [dJ/s]", 0.1),
				new("centijoule/second [cJ/s]", 0.01),
				new("millijoule/second [mJ/s]", 0.001),
				new("microjoule/second [µJ/s]", 0.000001),
				new("nanojoule/second [nJ/s]", 1.0e-9),
				new("picojoule/second [pJ/s]", 1.0e-12),
				new("femtojoule/second [fJ/s]", 1.0e-15),
				new("attojoule/second [aJ/s]", 9.999999999e-19),
				new("joule/hour [J/h]", 0.0002777778),
				new("joule/minute [J/min]", 0.0166666667),
				new("kilojoule/hour [kJ/h]", 0.2777777778),
				new("kilojoule/minute [kJ/min]", 16.666666667),
			]
		),
		new(
			"Data",
			"hard_drive_icon.png",
			[
				new("Byte [B]", 1),
				new("Kilobyte [kB]", 1000),
				new("Megabyte [MB]", 1e+6),
				new("Gigabyte [GB]", 1e+9),
				new("Terabyte [TB]", 1e+12),
				new("Petabyte [PB]", 1e+15),
				new("Exabyte [EB]", 1e+18),
				new("Bit", 0.125),
				new("Kilobit [kb]", 125),
				new("Megabit [Mb]", 125000),
				new("Gigabit [Gb]", 1.25e+8),
				new("Terabit [Tb]", 1.25e+11),
				new("Petabit [Pb]", 1.25e+14),
				new("Exabit [Eb]", 1.25e+17),
				new("Kibibyte", 1024),
				new("Mebibyte", 1.049e+6),
				new("Gibibyte", 1.074e+9),
				new("Tebibyte", 1.1e+12),
				new("Pebibyte", 1.126e+15),
				new("Kibibit", 128),
				new("Mebibit", 131072),
				new("Gibibit", 1.342e+8),
				new("Tebibit", 1.374e+11),
				new("Pebibit", 1.407e+14),
				new("Nibble", 0.5),
				new("Character", 1),
				new("Word", 2),
				new("MAPM-Word", 4),
				new("Quadruple-Word", 8),
				new("Block", 512),
				new("floppy disk (3.5\", DD)", 728832),
				new("floppy disk (3.5\", HD)", 1457664),
				new("floppy disk (3.5\", ED)", 2915328),
				new("floppy disk (5.25\", DD)", 364416),
				new("floppy disk (5.25\", HD)", 1213952),
				new("Zip 100", 100431872),
				new("Zip 250", 251079680),
				new("Jaz 1GB", 1073741824),
				new("Jaz 2GB", 2147483648),
				new("CD (74 minute)", 681058304),
				new("CD (80 minute)", 736279247),
				new("DVD (1 layer, 1 side)", 5046586572.8),
				new("DVD (2 layer, 1 side)", 9126805504),
				new("DVD (1 layer, 2 side)", 10093173146),
				new("DVD (2 layer, 2 side)", 18253611008),
			]
		),
		new(
			"Pressure",
			"air_icon.png",
			[
				new("Pascal", 1),
				new("kilopascal [kPa]", 1000),
				new("bar", 100000),
				new("psi", 6894.7572932),
				new("ksi", 6894757.2932),
				new("torr", 133.322),
				new("Standard atmosphere [atm]", 101325),
				new("exapascal [EPa]", 1000000000000000000),
				new("petapascal [PPa]", 1000000000000000),
				new("terapascal [TPa]", 1000000000000),
				new("gigapascal [GPa]", 1000000000),
				new("megapascal [MPa]", 1000000),
				new("hectopascal [hPa]", 100),
				new("dekapascal [daPa]", 10),
				new("decipascal [dPa]", 0.1),
				new("centipascal [cPa]", 0.01),
				new("millipascal [mPa]", 0.001),
				new("micropascal [µPa]", 0.000001),
				new("nanopascal [nPa]", 1.0e-9),
				new("picopascal [pPa]", 1.0e-12),
				new("femtopascal [fPa]", 1.0e-15),
				new("attopascal [aPa]", 9.999999999e-19),
				new("newton/square meter", 1),
				new("newton/square centimeter", 10000),
				new("newton/square millimeter", 1000000),
				new("kilonewton/square meter", 1000),
				new("millibar [mbar]", 100),
				new("microbar [µbar]", 0.1),
				new("dyne/square centimeter", 0.1),
				new("kilogram-force/square meter", 9.80665),
				new("kilogram-force/sq. cm", 98066.5),
				new("kilogram-force/sq. millimeter", 9806650),
				new("gram-force/sq. centimeter", 98.0665),
				new("ton-force (short)/sq. foot", 95760.517961),
				new("ton-force (short)/sq. inch", 13789514.586),
				new("ton-force (long)/square foot", 107251.78012),
				new("ton-force (long)/square inch", 15444256.337),
				new("kip-force/square inch", 6894757.2932),
				new("pound-force/square foot", 47.88025898),
				new("pound-force/square inch", 6894.7572932),
				new("poundal/square foot", 1.4881639436),
				new("centimeter mercury (0°C)", 1333.22),
				new("millimeter mercury (0°C)", 133.322),
				new("inch mercury (32°F) [inHg]", 3386.38),
				new("inch mercury (60°F) [inHg]", 3376.85),
				new("centimeter water (4°C)", 98.0638),
				new("millimeter water (4°C)", 9.80638),
				new("inch water (4°C) [inAq]", 249.082),
				new("foot water (4°C) [ftAq]", 2988.98),
				new("inch water (60°F) [inAq]", 248.843),
				new("foot water (60°F) [ftAq]", 2986.116),
				new("atmosphere technical [at]", 98066.5),
			]
		),
		new(
			"Angle",
			"angle_icon.png",
			[
				new("degree [°]", 1),
				new("radian [rad]", 57.295779513),
				new("grad [^g]", 0.9),
				new("minute [']", 0.0166666667),
				new("second [\"]", 0.0002777778),
				new("gon", 0.9),
				new("sign", 30),
				new("mil", 0.05625),
				new("revolution [r]", 360),
				new("circle", 360),
				new("turn", 360),
				new("quadrant", 90),
				new("right angle", 90),
				new("sextant", 60),
			]
		),
		// NOTE(LucasTA): Special case that uses a separate scene
		new("Date", "calendar_icon.png", []),
	];
}

internal readonly struct DateConversion
{
	internal enum DateFields
	{
		From,
		To,
	}

	internal enum DatePickers
	{
		Days,
		Months,
	}

	internal static DateOnly ConverterFromDate = DateOnly.FromDateTime(DateTime.Now);
	internal static DateOnly ConverterToDate = DateOnly.FromDateTime(DateTime.Now);
	internal static DateFields SelectedDateField = DateFields.From;
	internal static DatePickers SelectedDatePicker = DatePickers.Days;

	internal static string DateDifferenceDescription(DateOnly start, DateOnly end)
	{
		if (start > end)
		{
			(start, end) = (end, start);
		}

		int years = end.Year - start.Year;
		int months = end.Month - start.Month;
		int days = end.Day - start.Day;

		// Adjust for negative days or months
		if (days < 0)
		{
			months--;
			days += DateTime.DaysInMonth(end.Year, end.Month == 1 ? 12 : end.Month - 1);
		}

		if (months < 0)
		{
			years--;
			months += 12;
		}

		string result = "";

		if (years > 0)
			result += $"{years} {GetTranslation($"year{(years != 1 ? "s" : "")}")}";

		if (months > 0)
		{
			if (result.Length > 0)
				result += ", ";
			result += $"{months} {GetTranslation($"month{(months != 1 ? "s" : "")}")}";
		}

		if (days > 0)
		{
			if (result.Length > 0)
				result += $", {GetTranslation("and")} ";
			result += $"{days} {GetTranslation($"day{(days != 1 ? "s" : "")}")}";
		}

		return result;
	}
}
