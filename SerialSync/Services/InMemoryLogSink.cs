using Serilog.Core;
using Serilog.Events;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace SerialSync.Services;

public sealed class InMemoryLogSink : ILogEventSink
{
    private readonly Subject<LogEvent> _flow = new();

    public IObservable<LogEvent> Stream => _flow.AsObservable();

    public void Emit(LogEvent logEvent) => _flow.OnNext(logEvent);
}
