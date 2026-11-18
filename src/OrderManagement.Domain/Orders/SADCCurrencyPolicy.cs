namespace OrderManagement.Domain.Orders;
public static class SADCCurrencyPolicy
{
    private static readonly IReadOnlyDictionary<string,string[]> SupportedCurrencies = new Dictionary<string,string[]>(StringComparer.Ordinal)
    {
        ["AO"]=["AOA"],["BW"]=["BWP"],["KM"]=["KMF"],["CD"]=["CDF"],["SZ"]=["SZL","ZAR"],["LS"]=["LSL","ZAR"],["MG"]=["MGA"],["MW"]=["MWK"],["MU"]=["MUR"],["MZ"]=["MZN"],["NA"]=["NAD","ZAR"],["SC"]=["SCR"],["ZA"]=["ZAR"],["TZ"]=["TZS"],["ZM"]=["ZMW"],["ZW"]=["ZWL","USD"]
    };
    public static bool IsSupported(string countryCode,string currencyCode) =>
        !string.IsNullOrWhiteSpace(countryCode) && !string.IsNullOrWhiteSpace(currencyCode) &&
        SupportedCurrencies.TryGetValue(countryCode.Trim().ToUpperInvariant(),out var currencies) &&
        currencies.Contains(currencyCode.Trim().ToUpperInvariant(),StringComparer.Ordinal);
    public static IReadOnlyCollection<string> GetSupportedCurrencies(string countryCode) =>
        SupportedCurrencies.TryGetValue(countryCode.Trim().ToUpperInvariant(),out var currencies) ? currencies : Array.Empty<string>();
}