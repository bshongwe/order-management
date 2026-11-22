using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using OrderManagement.Api.Observability;
using OrderManagement.Api.Authentication;
using OrderManagement.Api.Contracts;
using OrderManagement.Api.ExceptionHandling;
using OrderManagement.Api.HealthChecks;
using OrderManagement.Application.Customers;
using OrderManagement.Application.Orders;
using OrderManagement.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "SADC Order Management API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer",
        BearerFormat = "JWT", In = ParameterLocation.Header
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        }] = Array.Empty<string>()
    });
});
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()));
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Entra:Authority"];
        options.Audience = builder.Configuration["Entra:Audience"];
    });
builder.Services.AddSingleton<IAuthorizationHandler, EntraPermissionHandler>();
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy("Orders.Read", policy => policy
        .RequireAuthenticatedUser()
        .AddRequirements(new EntraPermissionRequirement("Orders.Read")));
    options.AddPolicy("Orders.Write", policy => policy
        .RequireAuthenticatedUser()
        .AddRequirements(new EntraPermissionRequirement("Orders.Write")));
    options.AddPolicy("Orders.Admin", policy => policy
        .RequireAuthenticatedUser()
        .AddRequirements(new EntraPermissionRequirement("Orders.Admin")));
});
builder.Services.AddExceptionHandler<OrderConcurrencyExceptionHandler>();
builder.Services.AddExceptionHandler<OrderCreationExceptionHandler>();
builder.Services.AddHealthChecks()
    .AddCheck<SqlServerReadinessHealthCheck>("sql-server", tags: ["ready"]);
builder.Services.AddScoped<CreateCustomerHandler>();
builder.Services.AddScoped<GetCustomerByIdHandler>();
builder.Services.AddScoped<GetCustomersHandler>();
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

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestMetricsMiddleware>();
app.UseExceptionHandler();
app.UseCors("Frontend");
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("OpenApi:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapOrderManagementHealthChecks();

app.MapGet("/", () => Results.Ok(new
{
    service = "SADC Order Management API",
    status = "Running"
})).AllowAnonymous();

app.MapPost("/api/customers", async (
    CreateCustomerRequest request,
    CreateCustomerHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var result = await handler.HandleAsync(
            new CreateCustomerCommand(request.Name, request.Email, request.CountryCode),
            cancellationToken);

        return (IResult)Results.Created($"/api/customers/{result.Id}", result);
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [exception.ParamName ?? "customer"] = [exception.Message]
        });
    }
})
.WithName("CreateCustomer")
.Produces<CustomerDetails>(StatusCodes.Status201Created)
.ProducesValidationProblem()
.RequireAuthorization("Orders.Write");

app.MapGet("/api/customers/{id:guid}", async (
    Guid id,
    GetCustomerByIdHandler handler,
    CancellationToken cancellationToken) =>
{
    var customer = await handler.HandleAsync(id, cancellationToken);
    return customer is null ? Results.NotFound() : Results.Ok(customer);
})
.WithName("GetCustomerById")
.Produces<CustomerDetails>()
.Produces(StatusCodes.Status404NotFound)
.RequireAuthorization("Orders.Read");

app.MapGet("/api/customers", async (
    string? search,
    int? page,
    int? pageSize,
    GetCustomersHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var result = await handler.HandleAsync(
            new GetCustomersQuery(
                search,
                page ?? 1,
                pageSize ?? GetCustomersHandler.DefaultPageSize),
            cancellationToken);

        return (IResult)Results.Ok(result);
    }
    catch (ArgumentOutOfRangeException exception)
    {
        var field = exception.ParamName switch
        {
            nameof(GetCustomersQuery.Page) => "page",
            nameof(GetCustomersQuery.PageSize) => "pageSize",
            _ => "query"
        };

        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [field] = [exception.Message]
        });
    }
})
.WithName("GetCustomers")
.Produces<CustomerPage>()
.ProducesValidationProblem()
.RequireAuthorization("Orders.Read");

app.MapPost("/api/orders", async (
    CreateOrderRequest request,
    CreateOrderHandler handler,
    CancellationToken cancellationToken) =>
{
    var validationErrors = CreateOrderRequestValidator.Validate(request);
    if (validationErrors.Count > 0)
        return (IResult)Results.ValidationProblem(validationErrors);

    var items = request.Items.Select(item =>
        new CreateOrderLineItemCommand(item.ProductSku, item.Quantity, item.UnitPrice)).ToArray();

    var result = await handler.HandleAsync(
        new CreateOrderCommand(request.CustomerId, request.CurrencyCode, items),
        cancellationToken);

    return (IResult)Results.Created($"/api/orders/{result.Id}", result);
})
.WithName("CreateOrder")
.Produces<CreateOrderResult>(StatusCodes.Status201Created)
.ProducesValidationProblem()
.RequireAuthorization("Orders.Write");

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
.ProducesValidationProblem()
.RequireAuthorization("Orders.Read");

app.MapPost("/api/orders/{id:guid}/submit", async (
    Guid id,
    [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
    SubmitOrderHandler handler,
    IIdempotencyExecutor idempotencyExecutor,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
    await ExecuteIdempotently(
        idempotencyKey,
        "submit",
        id,
        idempotencyExecutor,
        token => handler.HandleAsync(id, token),
        result => result.Outcome switch
        {
            SubmitOrderOutcome.Submitted => Results.Ok(result.Order),
            SubmitOrderOutcome.NotFound => Results.NotFound(),
            SubmitOrderOutcome.Conflict => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Order cannot be submitted",
                detail: $"An order in status '{result.Order!.Status}' cannot be submitted."),
            _ => throw new InvalidOperationException("Unknown order submission outcome.")
        },
        httpContext,
        cancellationToken))
.WithName("SubmitOrder")
.Produces<OrderDetails>()
.Produces(StatusCodes.Status404NotFound)
.ProducesValidationProblem()
.ProducesProblem(StatusCodes.Status409Conflict)
.RequireAuthorization("Orders.Write");

app.MapPost("/api/orders/{id:guid}/confirm", async (
    Guid id,
    [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
    ConfirmOrderHandler handler,
    IIdempotencyExecutor idempotencyExecutor,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
    await ExecuteIdempotently(
        idempotencyKey,
        "confirm",
        id,
        idempotencyExecutor,
        token => handler.HandleAsync(id, token),
        result => result.Outcome switch
        {
            ConfirmOrderOutcome.Confirmed => Results.Ok(result.Order),
            ConfirmOrderOutcome.NotFound => Results.NotFound(),
            ConfirmOrderOutcome.Conflict => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Order cannot be confirmed",
                detail: $"An order in status '{result.Order!.Status}' cannot be confirmed."),
            _ => throw new InvalidOperationException("Unknown order confirmation outcome.")
        },
        httpContext,
        cancellationToken))
.WithName("ConfirmOrder")
.Produces<OrderDetails>()
.Produces(StatusCodes.Status404NotFound)
.ProducesValidationProblem()
.ProducesProblem(StatusCodes.Status409Conflict)
.RequireAuthorization("Orders.Write");

app.MapPost("/api/orders/{id:guid}/process", async (
    Guid id,
    [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
    ProcessOrderHandler handler,
    IIdempotencyExecutor idempotencyExecutor,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
    await ExecuteIdempotently(
        idempotencyKey,
        "process",
        id,
        idempotencyExecutor,
        token => handler.HandleAsync(id, token),
        result => result.Outcome switch
        {
            ProcessOrderOutcome.ProcessingStarted => Results.Ok(result.Order),
            ProcessOrderOutcome.NotFound => Results.NotFound(),
            ProcessOrderOutcome.Conflict => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Order cannot be processed",
                detail: $"An order in status '{result.Order!.Status}' cannot be processed."),
            _ => throw new InvalidOperationException("Unknown order processing outcome.")
        },
        httpContext,
        cancellationToken))
.WithName("ProcessOrder")
.Produces<OrderDetails>()
.Produces(StatusCodes.Status404NotFound)
.ProducesValidationProblem()
.ProducesProblem(StatusCodes.Status409Conflict)
.RequireAuthorization("Orders.Write");

app.MapPost("/api/orders/{id:guid}/fulfill", async (
    Guid id,
    [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
    FulfillOrderHandler handler,
    IIdempotencyExecutor idempotencyExecutor,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
    await ExecuteIdempotently(
        idempotencyKey,
        "fulfill",
        id,
        idempotencyExecutor,
        token => handler.HandleAsync(id, token),
        result => result.Outcome switch
        {
            FulfillOrderOutcome.Fulfilled => Results.Ok(result.Order),
            FulfillOrderOutcome.NotFound => Results.NotFound(),
            FulfillOrderOutcome.Conflict => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Order cannot be fulfilled",
                detail: $"An order in status '{result.Order!.Status}' cannot be fulfilled."),
            _ => throw new InvalidOperationException("Unknown order fulfilment outcome.")
        },
        httpContext,
        cancellationToken))
.WithName("FulfillOrder")
.Produces<OrderDetails>()
.Produces(StatusCodes.Status404NotFound)
.ProducesValidationProblem()
.ProducesProblem(StatusCodes.Status409Conflict)
.RequireAuthorization("Orders.Write");

app.MapPost("/api/orders/{id:guid}/cancel", async (
    Guid id,
    [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
    CancelOrderHandler handler,
    IIdempotencyExecutor idempotencyExecutor,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
    await ExecuteIdempotently(
        idempotencyKey,
        "cancel",
        id,
        idempotencyExecutor,
        token => handler.HandleAsync(id, token),
        result => result.Outcome switch
        {
            CancelOrderOutcome.Cancelled => Results.Ok(result.Order),
            CancelOrderOutcome.NotFound => Results.NotFound(),
            CancelOrderOutcome.Conflict => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Order cannot be cancelled",
                detail: $"An order in status '{result.Order!.Status}' cannot be cancelled."),
            _ => throw new InvalidOperationException("Unknown order cancellation outcome.")
        },
        httpContext,
        cancellationToken))
.WithName("CancelOrder")
.Produces<OrderDetails>()
.Produces(StatusCodes.Status404NotFound)
.ProducesValidationProblem()
.ProducesProblem(StatusCodes.Status409Conflict)
.RequireAuthorization("Orders.Write");

app.MapGet("/api/orders/{id:guid}", async (
    Guid id, GetOrderByIdHandler handler, HttpContext httpContext, CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(new GetOrderByIdQuery(id), cancellationToken);
    if (result is null) return (IResult)Results.NotFound();

    var etag = string.Concat('"', Convert.ToBase64String(result.RowVersion ?? []), '"');
    httpContext.Response.Headers["ETag"] = etag;
    if (httpContext.Request.Headers["If-None-Match"].ToString()
        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .Any(value => value == "*" || value == etag))
        return Results.StatusCode(StatusCodes.Status304NotModified);
    return Results.Ok(result);
})
.WithName("GetOrderById")
.Produces<OrderDetails>()
.Produces(StatusCodes.Status404NotFound)
.RequireAuthorization("Orders.Read");

app.Run();

static async Task<IResult> ExecuteIdempotently<T>(
    string? key,
    string operationName,
    Guid orderId,
    IIdempotencyExecutor executor,
    Func<CancellationToken, Task<T>> operation,
    Func<T, IResult> responseFactory,
    HttpContext httpContext,
    CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(key) || key.Length > 128)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["Idempotency-Key"] = ["A non-empty Idempotency-Key header of at most 128 characters is required."]
        });
    }

    var execution = await executor.ExecuteAsync(
        key,
        $"{operationName}:{orderId:D}",
        operation,
        cancellationToken);

    if (execution.Replayed)
    {
        httpContext.Response.Headers["Idempotency-Replayed"] = "true";
    }

    return responseFactory(execution.Value);
}

public partial class Program { }
