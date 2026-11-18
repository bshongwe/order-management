var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new
{
    service = "SADC Order Management API",
    status = "Running"
}));

app.Run();

public partial class Program;
