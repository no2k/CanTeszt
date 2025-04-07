using System;

namespace SharedModels.Interfaces
{
    public interface IBaseModel
    {
        long Id { get; }

        DateTime DateTime { get; }
    }
}
