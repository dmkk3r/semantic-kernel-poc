using Ingester;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging();

builder.Services.AddScoped<TicketIngester>();

var app = builder.Build();

app.MapGet("/ingest", async (TicketIngester ticketIngester) =>
{
    await ticketIngester.IngestAsync();

    return new { };
});

app.Run();