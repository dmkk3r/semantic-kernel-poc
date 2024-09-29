using Ingester;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.Ollama;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.DocumentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.Pipeline.Queue.DevTools;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging();

builder.Services.AddKernelMemory(kmBuilder =>
    {
        var ollamaEndpoint = builder.Configuration["OllamaEndpoint"] ??
                             throw new ArgumentNullException("Ollama endpoint is not set");
        var qdrantEndpoint = builder.Configuration["QdrantEndpoint"] ??
                             throw new ArgumentNullException("Qdrant endpoint is not set");
        var storagePath = builder.Configuration["StoragePath"] ??
                          throw new ArgumentNullException("Storage path is not set");

        // TextPartitioningOptions parseOptions = new()
        // {
        //     MaxTokensPerParagraph = 300,
        //     MaxTokensPerLine = 100,
        //     OverlappingTokens = 50
        // };

        SimpleFileStorageConfig storageConfig = new()
        {
            Directory = storagePath,
            StorageType = FileSystemTypes.Disk,
        };

        SimpleQueuesConfig queuesConfig = new()
        {
            StorageType = FileSystemTypes.Volatile,
        };

        OllamaConfig ollamaConfig = new()
        {
            Endpoint = ollamaEndpoint,
            EmbeddingModel = new OllamaModelConfig("nomic-embed-text", 2048)
        };

        QdrantConfig qdrantConfig = new()
        {
            Endpoint = qdrantEndpoint,
        };

        kmBuilder.WithSimpleQueuesPipeline(queuesConfig)
            .WithSimpleFileStorage(storageConfig)
            .WithQdrantMemoryDb(qdrantConfig)
            .WithOllamaTextEmbeddingGeneration(ollamaConfig, new GPT4oTokenizer());
    }
);


builder.Services.AddScoped<TicketIngester>();

var app = builder.Build();

app.MapGet("/ingest", async (TicketIngester ticketIngester) =>
{
    await ticketIngester.IngestAsync();

    return Results.Ok();
});

app.Run();