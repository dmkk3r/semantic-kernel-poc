using Inferencer.KernelFunctions;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.Ollama;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Qdrant.Client;

#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0020
#pragma warning disable SKEXP0070

var builder = WebApplication.CreateBuilder(args);

var ollamaEndpoint = builder.Configuration["OllamaEndpoint"] ??
                     throw new ArgumentNullException("Ollama endpoint is not set");

var qdrantHost = builder.Configuration["QdrantHost"] ??
                 throw new ArgumentNullException("Qdrant host is not set");

var qdrantPort = builder.Configuration["QdrantPort"] ??
                 throw new ArgumentNullException("Qdrant port is not set");

builder.Services
    .AddKernel()
    .Plugins.AddFromType<ManuelSearchPlugin>("ManuelSearch");

//builder.Services.AddOpenAIChatCompletion("llama3.1:8b", new Uri(ollamaEndpoint), "ollama");
//builder.Services.AddOpenAIChatCompletion("llama3.2:3b", new Uri(ollamaEndpoint), "ollama");
builder.Services.AddOpenAIChatCompletion("mistral-nemo:12b", new Uri(ollamaEndpoint), "ollama");

builder.Services.AddKernelMemory(kmBuilder =>
{
    var qdrantEndpoint = builder.Configuration["QdrantEndpoint"] ??
                         throw new ArgumentNullException("Qdrant endpoint is not set");

    TextPartitioningOptions textPartitioningOptions = new()
    {
        MaxTokensPerParagraph = 300,
        MaxTokensPerLine = 100,
        OverlappingTokens = 50
    };

    OllamaConfig ollamaConfig = new()
    {
        Endpoint = ollamaEndpoint,
        TextModel = new OllamaModelConfig("mistral-nemo:12b", 128000),
        EmbeddingModel = new OllamaModelConfig("nomic-embed-text", 2048),
    };

    QdrantConfig qdrantConfig = new()
    {
        Endpoint = qdrantEndpoint,
    };

    var httpClient = new HttpClient();
    httpClient.Timeout = TimeSpan.FromMinutes(5);

    kmBuilder
        .WithQdrantMemoryDb(qdrantConfig)
        .WithOllamaTextGeneration(ollamaConfig, new GPT4oTokenizer())
        .WithOllamaTextEmbeddingGeneration(ollamaConfig, new GPT4oTokenizer())
        .WithOpenAITextGeneration(new OpenAIConfig
        {
            APIKey = "ollama",
            TextModel = "mistral-nemo:12b",
            Endpoint = ollamaEndpoint
        }, new GPT4oTokenizer(), httpClient: httpClient)
        .WithOpenAITextEmbeddingGeneration(new OpenAIConfig
        {
            APIKey = "ollama",
            EmbeddingModel = "nomic-embed-text",
            Endpoint = ollamaEndpoint
        }, new GPT4oTokenizer(), httpClient: httpClient)
        .With(textPartitioningOptions);
});

builder.Services.AddSingleton<QdrantClient>(sp => new QdrantClient(qdrantHost, int.Parse(qdrantPort), false));
builder.Services.AddQdrantVectorStore();

var app = builder.Build();

app.MapGet("/ask", async (IChatCompletionService chatCompletionService, Kernel kernel) =>
{
    var settings = new OpenAIPromptExecutionSettings()
    {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    };

    var history = new ChatHistory($$"""
                                    You are a helpful AI assistant called 'Assistant' whose job is to help customers working for on-geo, an saas focusing on real estate valuation.

                                    The most recent message from the customer is this:
                                    <customer_message>Was sagt die Hilfe zum Thema Vollgutachten?</customer_message>
                                    However, that is only provided for context. You are not answering that question directly. The real question will be asked by the user below.

                                    If this is a question about the product, ALWAYS search the product manual.

                                    ALWAYS justify your answer by citing a search result. Do this by including this syntax in your reply:
                                    <cite searchResultId=number>shortVerbatimQuote</cite>
                                    shortVerbatimQuote must be a very short, EXACT quote (max 10 words) from whichever search result you are citing.
                                    Only give one citation per answer. Always give a citation because this is important to the business.
                                    """);

    history.AddUserMessage("Was sagt die Hilfe zum Thema Vollgutachten?");

    var response = await chatCompletionService.GetChatMessageContentAsync(history, settings, kernel);

    if (!string.IsNullOrWhiteSpace(response.Content))
    {
        history.AddAssistantMessage(response.Content);
    }

    return Results.Ok(history);
});

app.Run();