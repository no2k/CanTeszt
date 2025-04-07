namespace SharedModels.Interfaces
{
    internal interface IPufferProtocol : IBaseModel, IProtocolBase
    {
        string Command { get; }
        string ProtoId { get; }
        string Value { get; }
    }
}
