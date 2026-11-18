namespace OrderManagement.Domain.Customers;

public static class SADCountries
{
    private static readonly HashSet<string> CountryCodes = new(StringComparer.Ordinal)
    {
        "AO", "BW", "KM", "CD", "SZ", "LS", "MG", "MW",
        "MU", "MZ", "NA", "SC", "ZA", "TZ", "ZM", "ZW"
    };

    public static bool IsSupported(string countryCode) =>
        !string.IsNullOrWhiteSpace(countryCode) &&
        CountryCodes.Contains(countryCode.Trim().ToUpperInvariant());
}
