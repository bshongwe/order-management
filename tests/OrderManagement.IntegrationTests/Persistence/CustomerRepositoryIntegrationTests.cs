using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Customers;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.IntegrationTests.Infrastructure;
using Xunit;

namespace OrderManagement.IntegrationTests.Persistence;

[Collection(SqlServerCollection.Name)]
public sealed class CustomerRepositoryIntegrationTests(SqlServerFixture fixture)
{
    [Fact]
    public async Task Customer_CanBeCreatedRetrievedAndSearched()
    {
        Guid id;
        await using (var writeContext = fixture.CreateDbContext())
        {
            writeContext.Customers.Add(Customer.Create("SADC Customer", "sadc.customer@example.com", "ZA"));
            await writeContext.SaveChangesAsync();
            id = await writeContext.Customers.Where(customer => customer.Email == "sadc.customer@example.com")
                .Select(customer => customer.Id).SingleAsync();
        }

        await using var readContext = fixture.CreateDbContext();
        var customer = await readContext.Customers.AsNoTracking()
            .Where(candidate => candidate.Id == id)
            .Select(candidate => new { candidate.Name, candidate.CountryCode }).SingleAsync();
        Assert.Equal("SADC Customer", customer.Name);
        Assert.Equal("ZA", customer.CountryCode);
        Assert.Equal(1, await readContext.Customers.CountAsync(candidate => candidate.Email.Contains("sadc.customer")));
    }

    [Fact]
    public async Task Customer_EmailMustBeUnique()
    {
        await using var context = fixture.CreateDbContext();
        context.Customers.Add(Customer.Create("First Customer", "duplicate@example.com", "ZA"));
        await context.SaveChangesAsync();
        context.Customers.Add(Customer.Create("Second Customer", "duplicate@example.com", "BW"));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
