using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderManagement.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OrdersDbContext))]
[Migration("20261118230000_CreateCustomers")]
public partial class CreateCustomers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Customers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                CountryCode = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Customers", x => x.Id));

        migrationBuilder.CreateIndex("IX_Customers_Email", "Customers", "Email", unique: true);
        migrationBuilder.CreateIndex("IX_Customers_CountryCode", "Customers", "CountryCode");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("Customers");
}
