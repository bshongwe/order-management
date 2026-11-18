using OrderManagement.Api.Contracts;
using OrderManagement.Application.Orders;
using OrderManagement.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddScoped<CreateOrderHandler>();
builder.Services.AddScoped<GetOrderByIdHandler>();
builder.Services.AddScoped<GetOrdersHandler>();
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

app.MapGet("/api/orders", async (
    int? page,
    int? pageSize,
    GetOrdersHandler handler,
    CancellationToken cancellationToken) =>
{
    var query = new GetOrdersQuery(
        page ?? 1,
        pageSize ?? GetOrdersHandler.DefaultPageSize);

    try
    {
        var result = await handler.HandleAsync(query, cancellationToken);
        return (IResult)Results.Ok(result);
    }
    catch (ArgumentOutOfRangeException exception)
    {
        var field = exception.ParamName switch
        {
            nameof(GetOrdersQuery.Page) => "page",
            nameof(GetOrdersQuery.PageSize) => "pageSize",
            _ => "query"
        };

        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [field] = [exception.Message]
        });
    }
})
.WithName("GetOrders")
.Produces<OrderPage>()
.ProducesValidationProblem();

app.MapGet("/api/orders/{id:guid}", async (
    Guid id,
    GetOrderByIdHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(new GetOrderByIdQuery(id), cancellationToken);

    if (result is null)
    {
        return (IResult)Results.NotFound();
    }

    return Results.Ok(result);
})
.WithName("GetOrderById")
.Produces<OrderDetails>()
.Produces(StatusCodes.Status404NotFound);

app.Run();

public partial class Program { }
