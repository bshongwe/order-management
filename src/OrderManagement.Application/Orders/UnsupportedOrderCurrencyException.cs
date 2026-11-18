namespace OrderManagement.Application.Orders;
public sealed class UnsupportedOrderCurrencyException(string countryCode,string currencyCode):Exception($"Currency '{currencyCode}' is not supported for customer country '{countryCode}'.")
{
    public string CountryCode { get; }=countryCode;
    public string CurrencyCode { get; }=currencyCode;
}