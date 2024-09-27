using Microsoft.SemanticKernel;
using Qdrant.Client;

var builder = WebApplication.CreateBuilder(args);

var ollamaEndpoint = builder.Configuration["OllamaEndpoint"] ??
                     throw new ArgumentNullException("Ollama endpoint is not set");

var qdrantHost = builder.Configuration["QdrantHost"] ??
                 throw new ArgumentNullException("Qdrant host is not set");

var qdrantPort = builder.Configuration["QdrantPort"] ??
                 throw new ArgumentNullException("Qdrant port is not set");

#pragma warning disable SKEXP0070
builder.Services
    .AddKernel()
    .AddOllamaChatCompletion("llama3.2:3b", new Uri(ollamaEndpoint), null);
#pragma warning restore SKEXP0070

#pragma warning disable SKEXP0020
builder.Services.AddSingleton<QdrantClient>(sp => new QdrantClient(qdrantHost, int.Parse(qdrantPort),false));
builder.Services.AddQdrantVectorStore();
#pragma warning restore SKEXP0020

var app = builder.Build();

app.MapGet("/ask", async () =>
{
    return new { };
});

app.Run();