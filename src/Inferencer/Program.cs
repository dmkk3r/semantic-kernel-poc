using Inferencer.KernelFunctions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
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
    .Plugins.AddFromType<DateTimePlugin>("DateTime");

builder.Services.AddOpenAIChatCompletion("llama3.2:3b", new Uri(ollamaEndpoint), "ollama");

builder.Services.AddSingleton<QdrantClient>(sp => new QdrantClient(qdrantHost, int.Parse(qdrantPort), false));
builder.Services.AddQdrantVectorStore();

var app = builder.Build();

app.MapGet("/ask", async (IChatCompletionService chatCompletionService, Kernel kernel) =>
{
    var settings = new OpenAIPromptExecutionSettings()
    {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    };

    var history = new ChatHistory();

    history.AddSystemMessage("""
                             You are an Assistant, which job is to help users with their questions.
                             Answer in short sentences.
                             """);
    
    history.AddUserMessage("What is the current time of the day? My timezone is Berlin.");

    var response = await chatCompletionService.GetChatMessageContentAsync(history, settings, kernel);

    history.AddAssistantMessage(response.Content);

    return Results.Ok(history);
});

app.Run();