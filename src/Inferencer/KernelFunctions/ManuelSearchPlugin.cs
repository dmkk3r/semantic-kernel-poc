using System.ComponentModel;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;

namespace Inferencer.KernelFunctions;

public sealed class ManuelSearchPlugin
{
    private readonly IKernelMemory _kernelMemory;

    public ManuelSearchPlugin(IKernelMemory kernelMemory)
    {
        _kernelMemory = kernelMemory;
    }

    [KernelFunction("search_manuel")]
    [Description("Search the manuel for the given query.")]
    public async Task<string> SearchManuel(
        [Description("A phrase to use when searching the manual")] string searchPhrase)
    {
        var result = await _kernelMemory.SearchAsync(searchPhrase);

        return result.NoResult
            ? "No results found."
            : $"""
               Here are the results:
               {result.ToJson()}
               """;
    }
}