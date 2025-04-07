namespace SharedModels.Interfaces
{
    public interface IProtocolBase
    {
        char Start { get; }
        char End { get; }
        char Delimiter { get; }
    }
}
