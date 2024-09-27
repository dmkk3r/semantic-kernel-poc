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

        OllamaConfig ollamaConfig = new OllamaConfig
        {
            Endpoint = ollamaEndpoint,
            //TextModel = new OllamaModelConfig("llama3.2:3b", 131072),
            TextModel = new OllamaModelConfig("llama3.1:8b", 131072),
            EmbeddingModel = new OllamaModelConfig("nomic-embed-text", 2048)
        };

        QdrantConfig qdrantConfig = new()
        {
            Endpoint = qdrantEndpoint,
        };

        kmBuilder.WithSimpleQueuesPipeline(queuesConfig)
            .WithSimpleFileStorage(storageConfig)
            .WithQdrantMemoryDb(qdrantConfig)
            .WithOllamaTextGeneration(ollamaConfig, new GPT4oTokenizer())
            .WithOllamaTextEmbeddingGeneration(ollamaConfig, new GPT4oTokenizer());
            //.With(parseOptions);
    }
);


builder.Services.AddScoped<TicketIngester>();

var app = builder.Build();

app.MapGet("/ingest", async (TicketIngester ticketIngester) =>
{
    await ticketIngester.IngestAsync();

    return Results.Ok();
});

app.MapGet("/ask", async (IKernelMemory kernelMemory) =>
{
    var memoryAnswer =
        await kernelMemory.AskAsync("Wie kann ich einen auftrag in lora anlegen?");

    return Results.Ok(new
    {
        Answer = memoryAnswer.Result,
        Sources = memoryAnswer.RelevantSources
    });
});

app.Run();