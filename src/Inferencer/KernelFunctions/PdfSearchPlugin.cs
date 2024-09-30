using System.ComponentModel;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;

namespace Inferencer.KernelFunctions;

public sealed class PdfSearchPlugin
{
    private readonly MemoryWebClient _memoryWebClient;

    public PdfSearchPlugin(MemoryWebClient memoryWebClient)
    {
        _memoryWebClient = memoryWebClient;
    }

    [KernelFunction("search_pdf")]
    [Description("Search the pdf for the given question.")]
    public async Task<MemoryAnswer> SearchManuel(
        [Description("A phrase to use when searching the pdf")]
        string searchPhrase)
    {
        return await _memoryWebClient.AskAsync(searchPhrase);
    }
}