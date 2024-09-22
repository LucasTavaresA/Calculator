// Licensed under the GPL3 or later versions of the GPL license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Calculator;

internal readonly struct Currency
{
	private static readonly string CurrenciesFilePath = Data.DataFolder + "CurrencyRates";

	internal static List<Conversions.Conversion> Load()
	{
		List<Conversions.Conversion> conversions = new();

		string rates = "";

		if (File.Exists(CurrenciesFilePath))
		{
			rates = File.ReadAllText(CurrenciesFilePath);
		}

		foreach (
			string currency in rates.Split(
				Environment.NewLine,
				StringSplitOptions.RemoveEmptyEntries
			)
		)
		{
			string[] split = currency.Split(',');

			if (split.Length != 2 || split[0] == string.Empty || split[1] == string.Empty)
			{
				continue;
			}

			if (
				double.TryParse(
					split[1],
					NumberStyles.Any,
					CultureInfo.InvariantCulture,
					out double r
				)
			)
			{
				conversions.Add(new(split[0], r));
			}
		}

		return conversions;
	}

	internal readonly record struct RatesResponse(Dictionary<string, double> rates);

	internal static readonly HttpClient HttpClient = new();

	[UnconditionalSuppressMessage(
		"Warning",
		"IL2026",
		Justification = "Trimming is disabled for JSON."
	)]
	internal static async void GetCurrencyRatesAsync()
	{
		Dictionary<string, string> currencies = JsonSerializer.Deserialize<
			Dictionary<string, string>
		>(await HttpClient.GetStringAsync("https://openexchangerates.org/api/currencies.json"));

		Dictionary<string, double> rates = JsonSerializer
			.Deserialize<RatesResponse>(
				await HttpClient.GetStringAsync(
					"https://openexchangerates.org/api/latest.json?app_id="
						+ Resource.LoadStringFromAssembly("CalculatorUI.APIKEY")
				)
			)
			.rates;

		Settings.LastAPICallTime = DateTime.Now.Date;
		Settings.Save();
		Conversions.Converters[0].Conversions.Clear();
		StringBuilder ratesString = new();

		foreach ((string code, string name) in currencies)
		{
			string title = $"{name} [{code}]";
			Conversions.Converters[0].Conversions.Add(new(title, rates[code]));
			ratesString.AppendLine(
				title + "," + rates[code].ToString(CultureInfo.InvariantCulture)
			);
		}

		Data.SaveString(ratesString.ToString(), "CurrencyRates");
	}
}
