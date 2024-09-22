// Licensed under the GPL3 or later versions of the GPL license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace Calculator;

internal readonly record struct Currency
{
	private static readonly string CurrenciesFilePath = Data.DataFolder + "CurrencyRates";
	internal readonly string Code = "";
	internal readonly string Name = "";

	internal Currency(string code, string name)
	{
		Code = code;
		Name = name;
	}

	internal static List<Conversion.Currency> Load()
	{
		List<Conversion.Currency> conversions = new();

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

	internal readonly record struct Response(Dictionary<string, double> rates);

	internal static readonly HttpClient HttpClient = new();

	internal static Dictionary<string, double> GetCurrencyRates()
	{
		return JsonSerializer
			.Deserialize<Response>(
				HttpClient
					.GetStringAsync(
						"https://openexchangerates.org/api/latest.json?app_id="
							+ Resource.LoadStringFromAssembly("CalculatorUI.APIKEY")
					)
					.Result
			)
			.rates;
	}

	internal static readonly Currency[] Currencies =
	{
		new("AED", "United Arab Emirates Dirham [AED]"),
		new("AFN", "Afghan Afghani [AFN]"),
		new("ALL", "Albanian Lek [ALL]"),
		new("AMD", "Armenian Dram [AMD]"),
		new("ANG", "Netherlands Antillean Guilder [ANG]"),
		new("AOA", "Angolan Kwanza [AOA]"),
		new("ARS", "Argentine Peso [ARS]"),
		new("AUD", "Australian Dollar [AUD]"),
		new("AWG", "Aruban Florin [AWG]"),
		new("AZN", "Azerbaijani Manat [AZN]"),
		new("BAM", "Bosnia-Herzegovina Convertible Mark [BAM]"),
		new("BBD", "Barbadian Dollar [BBD]"),
		new("BDT", "Bangladeshi Taka [BDT]"),
		new("BGN", "Bulgarian Lev [BGN]"),
		new("BHD", "Bahraini Dinar [BHD]"),
		new("BIF", "Burundian Franc [BIF]"),
		new("BMD", "Bermudan Dollar [BMD]"),
		new("BND", "Brunei Dollar [BND]"),
		new("BOB", "Bolivian Boliviano [BOB]"),
		new("BRL", "Brazilian Real [BRL]"),
		new("BSD", "Bahamian Dollar [BSD]"),
		new("BTC", "Bitcoin [BTC]"),
		new("BTN", "Bhutanese Ngultrum [BTN]"),
		new("BWP", "Botswanan Pula [BWP]"),
		new("BYN", "Belarusian Ruble [BYN]"),
		new("BZD", "Belize Dollar [BZD]"),
		new("CAD", "Canadian Dollar [CAD]"),
		new("CDF", "Congolese Franc [CDF]"),
		new("CHF", "Swiss Franc [CHF]"),
		new("CLF", "Chilean Unit of Account (UF) [CLF]"),
		new("CLP", "Chilean Peso [CLP]"),
		new("CNH", "Chinese Yuan (Offshore) [CNH]"),
		new("CNY", "Chinese Yuan [CNY]"),
		new("COP", "Colombian Peso [COP]"),
		new("CRC", "Costa Rican Colón [CRC]"),
		new("CUC", "Cuban Convertible Peso [CUC]"),
		new("CUP", "Cuban Peso [CUP]"),
		new("CVE", "Cape Verdean Escudo [CVE]"),
		new("CZK", "Czech Republic Koruna [CZK]"),
		new("DJF", "Djiboutian Franc [DJF]"),
		new("DKK", "Danish Krone [DKK]"),
		new("DOP", "Dominican Peso [DOP]"),
		new("DZD", "Algerian Dinar [DZD]"),
		new("EGP", "Egyptian Pound [EGP]"),
		new("ERN", "Eritrean Nakfa [ERN]"),
		new("ETB", "Ethiopian Birr [ETB]"),
		new("EUR", "Euro [EUR]"),
		new("FJD", "Fijian Dollar [FJD]"),
		new("FKP", "Falkland Islands Pound [FKP]"),
		new("GBP", "British Pound Sterling [GBP]"),
		new("GEL", "Georgian Lari [GEL]"),
		new("GGP", "Guernsey Pound [GGP]"),
		new("GHS", "Ghanaian Cedi [GHS]"),
		new("GIP", "Gibraltar Pound [GIP]"),
		new("GMD", "Gambian Dalasi [GMD]"),
		new("GNF", "Guinean Franc [GNF]"),
		new("GTQ", "Guatemalan Quetzal [GTQ]"),
		new("GYD", "Guyanaese Dollar [GYD]"),
		new("HKD", "Hong Kong Dollar [HKD]"),
		new("HNL", "Honduran Lempira [HNL]"),
		new("HRK", "Croatian Kuna [HRK]"),
		new("HTG", "Haitian Gourde [HTG]"),
		new("HUF", "Hungarian Forint [HUF]"),
		new("IDR", "Indonesian Rupiah [IDR]"),
		new("ILS", "Israeli New Sheqel [ILS]"),
		new("IMP", "Manx pound [IMP]"),
		new("INR", "Indian Rupee [INR]"),
		new("IQD", "Iraqi Dinar [IQD]"),
		new("IRR", "Iranian Rial [IRR]"),
		new("ISK", "Icelandic Króna [ISK]"),
		new("JEP", "Jersey Pound [JEP]"),
		new("JMD", "Jamaican Dollar [JMD]"),
		new("JOD", "Jordanian Dinar [JOD]"),
		new("JPY", "Japanese Yen [JPY]"),
		new("KES", "Kenyan Shilling [KES]"),
		new("KGS", "Kyrgystani Som [KGS]"),
		new("KHR", "Cambodian Riel [KHR]"),
		new("KMF", "Comorian Franc [KMF]"),
		new("KPW", "North Korean Won [KPW]"),
		new("KRW", "South Korean Won [KRW]"),
		new("KWD", "Kuwaiti Dinar [KWD]"),
		new("KYD", "Cayman Islands Dollar [KYD]"),
		new("KZT", "Kazakhstani Tenge [KZT]"),
		new("LAK", "Laotian Kip [LAK]"),
		new("LBP", "Lebanese Pound [LBP]"),
		new("LKR", "Sri Lankan Rupee [LKR]"),
		new("LRD", "Liberian Dollar [LRD]"),
		new("LSL", "Lesotho Loti [LSL]"),
		new("LYD", "Libyan Dinar [LYD]"),
		new("MAD", "Moroccan Dirham [MAD]"),
		new("MDL", "Moldovan Leu [MDL]"),
		new("MGA", "Malagasy Ariary [MGA]"),
		new("MKD", "Macedonian Denar [MKD]"),
		new("MMK", "Myanma Kyat [MMK]"),
		new("MNT", "Mongolian Tugrik [MNT]"),
		new("MOP", "Macanese Pataca [MOP]"),
		new("MRU", "Mauritanian Ouguiya [MRU]"),
		new("MUR", "Mauritian Rupee [MUR]"),
		new("MVR", "Maldivian Rufiyaa [MVR]"),
		new("MWK", "Malawian Kwacha [MWK]"),
		new("MXN", "Mexican Peso [MXN]"),
		new("MYR", "Malaysian Ringgit [MYR]"),
		new("MZN", "Mozambican Metical [MZN]"),
		new("NAD", "Namibian Dollar [NAD]"),
		new("NGN", "Nigerian Naira [NGN]"),
		new("NIO", "Nicaraguan Córdoba [NIO]"),
		new("NOK", "Norwegian Krone [NOK]"),
		new("NPR", "Nepalese Rupee [NPR]"),
		new("NZD", "New Zealand Dollar [NZD]"),
		new("OMR", "Omani Rial [OMR]"),
		new("PAB", "Panamanian Balboa [PAB]"),
		new("PEN", "Peruvian Nuevo Sol [PEN]"),
		new("PGK", "Papua New Guinean Kina [PGK]"),
		new("PHP", "Philippine Peso [PHP]"),
		new("PKR", "Pakistani Rupee [PKR]"),
		new("PLN", "Polish Zloty [PLN]"),
		new("PYG", "Paraguayan Guarani [PYG]"),
		new("QAR", "Qatari Rial [QAR]"),
		new("RON", "Romanian Leu [RON]"),
		new("RSD", "Serbian Dinar [RSD]"),
		new("RUB", "Russian Ruble [RUB]"),
		new("RWF", "Rwandan Franc [RWF]"),
		new("SAR", "Saudi Riyal [SAR]"),
		new("SBD", "Solomon Islands Dollar [SBD]"),
		new("SCR", "Seychellois Rupee [SCR]"),
		new("SDG", "Sudanese Pound [SDG]"),
		new("SEK", "Swedish Krona [SEK]"),
		new("SGD", "Singapore Dollar [SGD]"),
		new("SHP", "Saint Helena Pound [SHP]"),
		new("SLL", "Sierra Leonean Leone [SLL]"),
		new("SOS", "Somali Shilling [SOS]"),
		new("SRD", "Surinamese Dollar [SRD]"),
		new("SSP", "South Sudanese Pound [SSP]"),
		new("STD", "São Tomé and Príncipe Dobra (pre-2018) [STD]"),
		new("STN", "São Tomé and Príncipe Dobra [STN]"),
		new("SVC", "Salvadoran Colón [SVC]"),
		new("SYP", "Syrian Pound [SYP]"),
		new("SZL", "Swazi Lilangeni [SZL]"),
		new("THB", "Thai Baht [THB]"),
		new("TJS", "Tajikistani Somoni [TJS]"),
		new("TMT", "Turkmenistani Manat [TMT]"),
		new("TND", "Tunisian Dinar [TND]"),
		new("TOP", "Tongan Pa'anga [TOP]"),
		new("TRY", "Turkish Lira [TRY]"),
		new("TTD", "Trinidad and Tobago Dollar [TTD]"),
		new("TWD", "New Taiwan Dollar [TWD]"),
		new("TZS", "Tanzanian Shilling [TZS]"),
		new("UAH", "Ukrainian Hryvnia [UAH]"),
		new("UGX", "Ugandan Shilling [UGX]"),
		new("USD", "United States Dollar [USD]"),
		new("UYU", "Uruguayan Peso [UYU]"),
		new("UZS", "Uzbekistan Som [UZS]"),
		new("VEF", "Venezuelan Bolívar Fuerte (Old) [VEF]"),
		new("VES", "Venezuelan Bolívar Soberano [VES]"),
		new("VND", "Vietnamese Dong [VND]"),
		new("VUV", "Vanuatu Vatu [VUV]"),
		new("WST", "Samoan Tala [WST]"),
		new("XAF", "CFA Franc BEAC [XAF]"),
		new("XAG", "Silver Ounce [XAG]"),
		new("XAU", "Gold Ounce [XAU]"),
		new("XCD", "East Caribbean Dollar [XCD]"),
		new("XDR", "Special Drawing Rights [XDR]"),
		new("XOF", "CFA Franc BCEAO [XOF]"),
		new("XPD", "Palladium Ounce [XPD]"),
		new("XPF", "CFP Franc [XPF]"),
		new("XPT", "Platinum Ounce [XPT]"),
		new("YER", "Yemeni Rial [YER]"),
		new("ZAR", "South African Rand [ZAR]"),
		new("ZMW", "Zambian Kwacha [ZMW]"),
		new("ZWL", "Zimbabwean Dollar [ZWL]"),
	};
}
