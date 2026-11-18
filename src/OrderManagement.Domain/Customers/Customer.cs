using System.Net.Mail;

namespace OrderManagement.Domain.Customers;

public sealed class Customer
{
    private Customer()
    {
        // Required by EF Core.
    }

    private Customer(Guid id, string name, string email, string countryCode)
    {
        Id = id;
        Name = name;
        Email = email;
        CountryCode = countryCode;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string CountryCode { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Customer Create(string name, string email, string countryCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(countryCode);

        var normalizedName = name.Trim();
        var normalizedEmail = email.Trim();
        var normalizedCountryCode = countryCode.Trim().ToUpperInvariant();

        if (normalizedName.Length > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(name), "Customer name cannot exceed 200 characters.");
        }

        if (normalizedEmail.Length > 254 ||
            !MailAddress.TryCreate(normalizedEmail, out var address) ||
            !string.Equals(address.Address, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("A valid email address is required.", nameof(email));
        }

        if (!SADCountries.IsSupported(normalizedCountryCode))
        {
            throw new ArgumentException(
                $"Country code '{normalizedCountryCode}' is not a supported SADC country.",
                nameof(countryCode));
        }

        return new Customer(Guid.NewGuid(), normalizedName, normalizedEmail, normalizedCountryCode);
    }
}
