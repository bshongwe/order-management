using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OrderManagement.Infrastructure.Persistence;
#nullable disable
namespace OrderManagement.Infrastructure.Persistence.Migrations;
[DbContext(typeof(OrdersDbContext))]
[Migration("20261119020000_CreateOutboxMessages")]
public partial class CreateOutboxMessages:Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name:"OutboxMessages",columns:table=>new
        {
            Id=table.Column<Guid>(type:"uniqueidentifier",nullable:false),
            EventType=table.Column<string>(type:"nvarchar(200)",maxLength:200,nullable:false),
            Payload=table.Column<string>(type:"nvarchar(max)",nullable:false),
            OccurredAtUtc=table.Column<DateTimeOffset>(type:"datetimeoffset",nullable:false),
            NextAttemptAtUtc=table.Column<DateTimeOffset>(type:"datetimeoffset",nullable:false),
            PublishedAtUtc=table.Column<DateTimeOffset>(type:"datetimeoffset",nullable:true),
            DeadLetteredAtUtc=table.Column<DateTimeOffset>(type:"datetimeoffset",nullable:true),
            Attempts=table.Column<int>(type:"int",nullable:false),
            LastError=table.Column<string>(type:"nvarchar(2000)",maxLength:2000,nullable:true),
            LeaseId=table.Column<Guid>(type:"uniqueidentifier",nullable:true),
            LeaseExpiresAtUtc=table.Column<DateTimeOffset>(type:"datetimeoffset",nullable:true)
        },constraints:table=>table.PrimaryKey("PK_OutboxMessages",x=>x.Id));
        migrationBuilder.CreateIndex("IX_OutboxMessages_PublishedAtUtc_DeadLetteredAtUtc_NextAttemptAtUtc","OutboxMessages",new[]{"PublishedAtUtc","DeadLetteredAtUtc","NextAttemptAtUtc"});
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("OutboxMessages");
    }
}