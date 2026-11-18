using OrderManagement.Api.Contracts;
using OrderManagement.Application.Orders;
using OrderManagement.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddScoped<CreateOrderHandler>();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.MapHealthChecks("/health");

app.MapGet("/", () => Results.Ok(new
{
    service = "SADC Order Management API",
    status = "Running"
}));

app.MapPost("/api/orders", async (
    CreateOrderRequest request,
    CreateOrderHandler handler,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.CustomerReference))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [nameof(request.CustomerReference)] = ["Customer reference is required."]
        });
    }

    var result = await handler.HandleAsync(
        new CreateOrderCommand(request.CustomerReference),
        cancellationToken);

    return Results.Created($"/api/orders/{result.Id}", result);
})
.WithName("CreateOrder")
.Produces<CreateOrderResult>(StatusCodes.Status201Created)
.ProducesValidationProblem();

app.Run();

public partial class Program { }
