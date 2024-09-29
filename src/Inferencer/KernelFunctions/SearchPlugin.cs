using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace Inferencer.KernelFunctions;

public sealed class DateTimePlugin
{
    [KernelFunction("get_current_datetime")]
    [Description("Returns the current date and time.")]
    public static DateTime GetCurrentTime()
    {
        return DateTime.Now;
    }
}