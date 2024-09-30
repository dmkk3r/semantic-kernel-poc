using Inferencer.KernelFunctions;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

#pragma warning disable SKEXP0110
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0020
#pragma warning disable SKEXP0070

var builder = WebApplication.CreateBuilder(args);

var ollamaEndpoint = builder.Configuration["OllamaEndpoint"] ??
                     throw new ArgumentNullException("Ollama endpoint is not set");

var kernelMemoryEndpoint = builder.Configuration["KernelMemoryServiceEndpoint"] ??
                           throw new ArgumentNullException("Kernel memory endpoint is not set");

builder.Services
    .AddKernel()
    .Plugins.AddFromType<PdfSearchPlugin>("PdfSearchPlugin");

//builder.Services.AddOpenAIChatCompletion("llama3.1:8b", new Uri(ollamaEndpoint), "ollama");
builder.Services.AddOpenAIChatCompletion("llama3.2:3b", new Uri(ollamaEndpoint), "ollama");
//builder.Services.AddOpenAIChatCompletion("mistral-nemo:12b", new Uri(ollamaEndpoint), "ollama");
builder.Services.AddSingleton<MemoryWebClient>(sp => new MemoryWebClient(kernelMemoryEndpoint));

var app = builder.Build();

app.MapGet("/ask", async (Kernel kernel) =>
{
    OpenAIPromptExecutionSettings settings = new()
    {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
    };

    const string domainName = "PDF Search";
    const string hostName = $"{domainName} Assistant";

    const string hostInstructions = $"""
                                     You are an Assistant to search content from the {domainName} guide to help users to answer the question. 

                                     You can answer general questions like greetings, good bye with your response without using any plugins. 
                                     For all other questions, use the list of available plugin below to get the answer. 

                                     List of Available Plugins:
                                         PdfSearchPlugin: Search answers from pdf files for questions related to {domainName}.

                                     If any one of the plugin can not be used for the give query, even if you know the answer,
                                     you should not provide the answer outside of the {domainName} context.
                                     Respond back with ""I dont have the answer for your question"" 
                                     Be precise with the response. Do not add what plugin you have used to get the answers in the response.
                                     """;

    ChatCompletionAgent agent = new()
    {
        Instructions = hostInstructions,
        Name = hostName,
        Kernel = kernel,
        Arguments = new KernelArguments(settings),
    };

    ChatHistory history = [];

    history.AddUserMessage("how can i embed fonts into the pdf with latex?");

    var arguments = new KernelArguments(settings)
    {
        // { "input", "" },
        // { "collection", "default" }
    };

    await foreach (var content in agent.InvokeAsync(history, arguments, kernel))
    {
        if (!content.Items.Any(i => i is FunctionCallContent or FunctionResultContent))
        {
            history.Add(content);
        }
    }

    return Results.Ok(history);
});

app.Run();