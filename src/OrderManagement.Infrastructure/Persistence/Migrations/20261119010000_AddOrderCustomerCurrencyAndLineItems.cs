using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OrderManagement.Infrastructure.Persistence;
#nullable disable
namespace OrderManagement.Infrastructure.Persistence.Migrations;
[DbContext(typeof(OrdersDbContext))]
[Migration("20261119010000_AddOrderCustomerCurrencyAndLineItems")]
public partial class AddOrderCustomerCurrencyAndLineItems:Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Nullable metadata preserves historical orders without inventing customer identities or currencies.
        migrationBuilder.AddColumn<Guid>("CustomerId","Orders",type:"uniqueidentifier",nullable:true);
        migrationBuilder.AddColumn<string>("CurrencyCode","Orders",type:"char(3)",unicode:false,fixedLength:true,maxLength:3,nullable:true);
        migrationBuilder.AddColumn<decimal>("TotalAmount","Orders",type:"decimal(18,2)",precision:18,scale:2,nullable:true);
        migrationBuilder.CreateTable(name:"OrderLineItems",columns:table=>new
        {
            Id=table.Column<Guid>(type:"uniqueidentifier",nullable:false),
            OrderId=table.Column<Guid>(type:"uniqueidentifier",nullable:false),
            ProductSku=table.Column<string>(type:"nvarchar(64)",maxLength:64,nullable:false),
            Quantity=table.Column<int>(type:"int",nullable:false),
            UnitPrice=table.Column<decimal>(type:"decimal(18,2)",precision:18,scale:2,nullable:false)
        },constraints:table=>
        {
            table.PrimaryKey("PK_OrderLineItems",x=>x.Id);
            table.ForeignKey("FK_OrderLineItems_Orders_OrderId",x=>x.OrderId,"Orders","Id",onDelete:ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex("IX_Orders_CustomerId_Status_CreatedAtUtc","Orders",new[]{"CustomerId","Status","CreatedAtUtc"});
        migrationBuilder.CreateIndex("IX_OrderLineItems_OrderId","OrderLineItems","OrderId");
        migrationBuilder.AddForeignKey("FK_Orders_Customers_CustomerId","Orders","CustomerId","Customers",principalColumn:"Id",onDelete:ReferentialAction.Restrict);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_Orders_Customers_CustomerId","Orders");
        migrationBuilder.DropTable("OrderLineItems");
        migrationBuilder.DropIndex("IX_Orders_CustomerId_Status_CreatedAtUtc","Orders");
        migrationBuilder.DropColumn("CustomerId","Orders");
        migrationBuilder.DropColumn("CurrencyCode","Orders");
        migrationBuilder.DropColumn("TotalAmount","Orders");
    }
}