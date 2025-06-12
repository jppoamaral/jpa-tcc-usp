public interface IAgent
{
    string Name { get; }
    Task RunAsync(CancellationToken token);
}
