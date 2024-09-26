using Microsoft.KernelMemory;
using System.Diagnostics;
using Microsoft.KernelMemory.DocumentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.AI.Ollama;
using Microsoft.KernelMemory.AI.OpenAI;

namespace Ingester;

public class TicketIngester
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TicketIngester> _logger;

    public TicketIngester(IConfiguration configuration, ILogger<TicketIngester> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task IngestAsync()
    {
        var ingestPath = _configuration["IngestPath"] ?? throw new ArgumentNullException("Ingest path is not set");

        var memory = CreateKernalMemory(_configuration);

        foreach (var file in Directory.EnumerateFiles(ingestPath, "*"))
        {
            Stopwatch sw = Stopwatch.StartNew();

            var fileInfo = new FileInfo(file);

            _logger.LogInformation("Importing {File}...", fileInfo.Name);

            await memory.ImportDocumentAsync(fileInfo.FullName, fileInfo.Name, steps: Constants.PipelineWithSummary);

            _logger.LogInformation("Imported {File} in {TotalSeconds} seconds", file, sw.Elapsed.TotalSeconds);
        }
    }

    private static IKernelMemory CreateKernalMemory(IConfiguration configuration)
    {
        var ollamaEndpoint = configuration["OllamaEndpoint"] ?? throw new ArgumentNullException("Ollama endpoint is not set");
        var qdrantEndpoint = configuration["QdrantEndpoint"] ?? throw new ArgumentNullException("Qdrant endpoint is not set");
        var storagePath = configuration["StoragePath"] ?? throw new ArgumentNullException("Storage path is not set");

        //SearchClientConfig searchClientConfig = new()
        //{
        //    MaxMatchesCount = 1,
        //    AnswerTokens = 100,
        //};

        //TextPartitioningOptions parseOptions = new()
        //{
        //    MaxTokensPerParagraph = 50,
        //    MaxTokensPerLine = 50,
        //    OverlappingTokens = 40
        //};

        SimpleFileStorageConfig storageConfig = new()
        {
            Directory = storagePath,
            StorageType = FileSystemTypes.Disk,
        };

        OllamaConfig ollamaConfig = new OllamaConfig
        {
            Endpoint = ollamaEndpoint,
            TextModel = new OllamaModelConfig("llama3.1:8b", 131072),
            EmbeddingModel = new OllamaModelConfig("nomic-embed-text", 2048)
        };

        QdrantConfig qdrantConfig = new()
        {
            Endpoint = qdrantEndpoint,
        };

        return new KernelMemoryBuilder()
            .WithSimpleFileStorage(storageConfig)
            .WithQdrantMemoryDb(qdrantConfig)
            .WithOllamaTextGeneration(ollamaConfig, new GPT4oTokenizer())
            .WithOllamaTextEmbeddingGeneration(ollamaConfig, new GPT4oTokenizer())
            //.WithSearchClientConfig(searchClientConfig)
            //.With(parseOptions)
            .Build();
    }
}

public record Ticket(string Id, string Title, string Description, string Reporter);