using OrderManagement.Api.Contracts;
using OrderManagement.Api.ExceptionHandling;
using OrderManagement.Application.Orders;
using OrderManagement.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<OrderConcurrencyExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddScoped<CreateOrderHandler>();
builder.Services.AddScoped<GetOrderByIdHandler>();
builder.Services.AddScoped<GetOrdersHandler>();
builder.Services.AddScoped<SubmitOrderHandler>();
builder.Services.AddScoped<CancelOrderHandler>();
builder.Services.AddScoped<ConfirmOrderHandler>();
builder.Services.AddScoped<ProcessOrderHandler>();
builder.Services.AddScoped<FulfillOrderHandler>();
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

app.MapPost("/api/orders/{id:guid}/submit", async (
    Guid id,
    SubmitOrderHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(id, cancellationToken);

    IResult response = result.Outcome switch
    {
        SubmitOrderOutcome.Submitted => Results.Ok(result.Order),
        SubmitOrderOutcome.NotFound => Results.NotFound(),
        SubmitOrderOutcome.Conflict => Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Order cannot be submitted",
            detail: $"An order in status '{result.Order!.Status}' cannot be submitted."),
        _ => throw new InvalidOperationException("Unknown order submission outcome.")
    };

    return response;
})
.WithName("SubmitOrder")
.Produces<OrderDetails>()
.Produces(StatusCodes.Status404NotFound)
.ProducesProblem(StatusCodes.Status409Conflict);

app.MapPost("/api/orders/{id:guid}/confirm", async (
    Guid id,
    ConfirmOrderHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(id, cancellationToken);

    IResult response = result.Outcome switch
    {
        ConfirmOrderOutcome.Confirmed => Results.Ok(result.Order),
        ConfirmOrderOutcome.NotFound => Results.NotFound(),
        ConfirmOrderOutcome.Conflict => Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Order cannot be confirmed",
            detail: $"An order in status '{result.Order!.Status}' cannot be confirmed."),
        _ => throw new InvalidOperationException("Unknown order confirmation outcome.")
    };

    return response;
})
.WithName("ConfirmOrder")
.Produces<OrderDetails>()
.Produces(StatusCodes.Status404NotFound)
.ProducesProblem(StatusCodes.Status409Conflict);

app.MapPost("/api/orders/{id:guid}/process", async (
    Guid id,
    ProcessOrderHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(id, cancellationToken);

    IResult response = result.Outcome switch
    {
        ProcessOrderOutcome.ProcessingStarted => Results.Ok(result.Order),
        ProcessOrderOutcome.NotFound => Results.NotFound(),
        ProcessOrderOutcome.Conflict => Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Order cannot be processed",
            detail: $"An order in status '{result.Order!.Status}' cannot be processed."),
        _ => throw new InvalidOperationException("Unknown order processing outcome.")
    };

    return response;
})
.WithName("ProcessOrder")
.Produces<OrderDetails>()
.Produces(StatusCodes.Status404NotFound)
.ProducesProblem(StatusCodes.Status409Conflict);

app.MapPost("/api/orders/{id:guid}/fulfill", async (
    Guid id,
    FulfillOrderHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(id, cancellationToken);

    IResult response = result.Outcome switch
    {
        FulfillOrderOutcome.Fulfilled => Results.Ok(result.Order),
        FulfillOrderOutcome.NotFound => Results.NotFound(),
        FulfillOrderOutcome.Conflict => Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Order cannot be fulfilled",
            detail: $"An order in status '{result.Order!.Status}' cannot be fulfilled."),
        _ => throw new InvalidOperationException("Unknown order fulfilment outcome.")
    };

    return response;
})
.WithName("FulfillOrder")
.Produces<OrderDetails>()
.Produces(StatusCodes.Status404NotFound)
.ProducesProblem(StatusCodes.Status409Conflict);

app.MapPost("/api/orders/{id:guid}/cancel", async (
    Guid id,
    CancelOrderHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(id, cancellationToken);

    IResult response = result.Outcome switch
    {
        CancelOrderOutcome.Cancelled => Results.Ok(result.Order),
        CancelOrderOutcome.NotFound => Results.NotFound(),
        CancelOrderOutcome.Conflict => Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Order cannot be cancelled",
            detail: $"An order in status '{result.Order!.Status}' cannot be cancelled."),
        _ => throw new InvalidOperationException("Unknown order cancellation outcome.")
    };

    return response;
})
.WithName("CancelOrder")
.Produces<OrderDetails>()
.Produces(StatusCodes.Status404NotFound)
.ProducesProblem(StatusCodes.Status409Conflict);

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
