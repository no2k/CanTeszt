using System;

using SharedModels.Interfaces;

namespace SharedModels.Models
{
    public abstract class ProtocolBase : IBaseModel, IProtocolBase
    {
        public long Id { get; private set; }
        public DateTime DateTime { get; private set; }
        public char Start { get; private set; }
        public char End { get; private set; }
        public char Delimiter { get; private  set; }

        public ProtocolBase(long id, DateTime dateTime, char start, char end, char delimiter)
        {
            Id = id;
            DateTime = dateTime;
            Start = start;
            End = end;
            Delimiter = delimiter;
        }
    }
}
