
using Serilog.Events;
using Serilog.Formatting;
using System.IO;

namespace VigilantMeshAgent.Logging;

public class ConsoleLogFormatter : ITextFormatter
{
    public void Format(LogEvent logEvent, TextWriter output)
    {
        
        
        output.Write($"[{logEvent.Timestamp:HH:mm:ss} {logEvent.Level.ToString().ToUpper()}] {logEvent.RenderMessage()}");
        output.WriteLine();
    }
}
