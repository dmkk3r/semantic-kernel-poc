var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/ingest", () =>
{
    return new { };
});

app.Run();