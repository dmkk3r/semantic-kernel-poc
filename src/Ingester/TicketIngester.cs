using Microsoft.KernelMemory;
using System.Diagnostics;

namespace Ingester;

public class TicketIngester
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TicketIngester> _logger;
    private readonly IKernelMemory _kernelMemory;

    public TicketIngester(IConfiguration configuration, ILogger<TicketIngester> logger, IKernelMemory kernelMemory)
    {
        _configuration = configuration;
        _logger = logger;
        _kernelMemory = kernelMemory;
    }

    public async Task IngestAsync()
    {
        var ingestPath = _configuration["IngestPath"] ?? throw new ArgumentNullException("Ingest path is not set");

        foreach (var file in Directory.EnumerateFiles(ingestPath, "*", SearchOption.AllDirectories))
        {
            var sw = Stopwatch.StartNew();

            var fileInfo = new FileInfo(file);

            _logger.LogInformation("Importing {File}...", fileInfo.Name);

            await _kernelMemory.ImportDocumentAsync(fileInfo.FullName,
                steps: Constants.PipelineWithoutSummary);

            _logger.LogInformation("Imported {File} in {TotalSeconds} seconds", file, sw.Elapsed.TotalSeconds);
        }
    }
}