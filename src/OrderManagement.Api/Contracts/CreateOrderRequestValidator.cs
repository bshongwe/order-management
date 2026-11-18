namespace OrderManagement.Api.Contracts;

public static class CreateOrderRequestValidator
{
    private const decimal MaxMoney = 9_999_999_999_999_999.99m;

    public static Dictionary<string, string[]> Validate(CreateOrderRequest? request)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        void Add(string field, string message)
        {
            if (!errors.TryGetValue(field, out var messages))
                errors[field] = messages = [];
            messages.Add(message);
        }

        if (request is null)
        {
            Add("request", "A request body is required.");
            return errors.ToDictionary(x => x.Key, x => x.Value.ToArray());
        }

        if (request.CustomerId == Guid.Empty)
            Add(nameof(request.CustomerId), "A customer ID is required.");

        if (string.IsNullOrWhiteSpace(request.CurrencyCode) ||
            request.CurrencyCode.Trim().Length != 3 ||
            !request.CurrencyCode.Trim().All(char.IsAsciiLetter))
            Add(nameof(request.CurrencyCode), "Currency code must be a three-letter ISO code.");

        if (request.Items is null || request.Items.Count == 0)
            Add(nameof(request.Items), "At least one order line item is required.");
        else
        {
            for (var index = 0; index < request.Items.Count; index++)
            {
                var item = request.Items[index];
                var prefix = $"items[{index}]";
                if (item is null)
                {
                    Add(prefix, "Order line item cannot be null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.ProductSku) || item.ProductSku.Trim().Length > 64)
                    Add($"{prefix}.productSku", "Product SKU is required and cannot exceed 64 characters.");
                if (item.Quantity <= 0)
                    Add($"{prefix}.quantity", "Quantity must be greater than zero.");
                if (item.UnitPrice < 0 || item.UnitPrice > MaxMoney || decimal.Round(item.UnitPrice, 2) != item.UnitPrice)
                    Add($"{prefix}.unitPrice", "Unit price must fit decimal(18,2) and have at most two decimal places.");
            }
        }

        return errors.ToDictionary(x => x.Key, x => x.Value.ToArray());
    }
}
