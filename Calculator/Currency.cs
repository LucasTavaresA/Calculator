// Licensed under the GPL3 or later versions of the GPL license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

using static Calculator.AssemblyResources;

namespace Calculator;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(Currency.RatesResponse))]
internal partial class AppJsonContext : JsonSerializerContext
{
}

internal readonly struct Currency
{
	private static readonly string CurrenciesFilePath = Data.DataFolder + "CurrencyRates";

	internal static List<Conversions.Conversion> Load()
	{
		List<Conversions.Conversion> conversions = [];

		string rates = "";

		if (File.Exists(CurrenciesFilePath))
		{
			rates = File.ReadAllText(CurrenciesFilePath);
		}

		foreach (
			string currency in rates.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
		)
		{
			string[] split = currency.Split(',');

			if (split.Length != 2 || split[0] == string.Empty || split[1] == string.Empty)
			{
				continue;
			}

			if (double.TryParse(split[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double r))
			{
				conversions.Add(new(split[0], r));
			}
		}

		return conversions;
	}

	internal readonly record struct RatesResponse(Dictionary<string, double> rates);

	internal static readonly HttpClient HttpClient = new();

	internal static async Task GetCurrencyRatesAsync()
	{
		Dictionary<string, string> currencies =
			JsonSerializer.Deserialize(
				await HttpClient.GetStringAsync("https://openexchangerates.org/api/currencies.json"),
				AppJsonContext.Default.DictionaryStringString
			) ?? [];

		Dictionary<string, double> rates =
			JsonSerializer
				.Deserialize(
					await HttpClient.GetStringAsync(
						"https://openexchangerates.org/api/latest.json?app_id="
							+ LoadStringFromAssembly("Calculator.APIKEY")
					),
					AppJsonContext.Default.RatesResponse
				)
				.rates ?? [];

		Settings.LastAPICallTime = DateTime.Now;
		Settings.Save();
		Conversions.Converters[0].Conversions.Clear();
		StringBuilder ratesString = new();

		foreach ((string code, string name) in currencies)
		{
			string title = $"{name} [{code}]";

			if (rates.TryGetValue(code, out double rate))
			{
				Conversions.Converters[0].Conversions.Add(new(title, rate));
				ratesString.AppendLine(title + "," + rate.ToString(CultureInfo.InvariantCulture));
			}
		}

		Data.SaveString(ratesString.ToString(), "CurrencyRates");
	}
}
