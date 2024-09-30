using Ingester;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.Ollama;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.Pipeline.Queue.DevTools;
using Microsoft.KernelMemory.Service.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging();

builder.Services.AddKernelMemory(kmBuilder =>
    {
        var ollamaEndpoint = builder.Configuration["OllamaEndpoint"] ??
                             throw new ArgumentNullException("Ollama endpoint is not set");
        var qdrantEndpoint = builder.Configuration["QdrantEndpoint"] ??
                             throw new ArgumentNullException("Qdrant endpoint is not set");

        SimpleQueuesConfig queuesConfig = new()
        {
            StorageType = FileSystemTypes.Volatile,
        };

        OllamaConfig ollamaConfig = new()
        {
            Endpoint = ollamaEndpoint,
            TextModel = new OllamaModelConfig("llama3.2:3b"),
            EmbeddingModel = new OllamaModelConfig("snowflake-arctic-embed:335m")
        };

        QdrantConfig qdrantConfig = new()
        {
            Endpoint = qdrantEndpoint,
        };

        kmBuilder.WithSimpleQueuesPipeline(queuesConfig)
            .WithQdrantMemoryDb(qdrantConfig)
            .WithOllamaTextGeneration(ollamaConfig, new GPT4oTokenizer())
            .WithOllamaTextEmbeddingGeneration(ollamaConfig, new GPT4oTokenizer());
    }
);

builder.Services.AddScoped<TicketIngester>();

var app = builder.Build();

app.AddKernelMemoryEndpoints("/km/");

app.MapGet("/ingest", async (TicketIngester ticketIngester) =>
{
    await ticketIngester.IngestAsync();

    return Results.Ok();
});

app.Run();