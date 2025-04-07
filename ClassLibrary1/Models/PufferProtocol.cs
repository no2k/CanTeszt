using System;
using SharedModels.Interfaces;

namespace SharedModels.Models
{
    public class PufferProtocol : ProtocolBase, IPufferProtocol
    {
        public string Command { get; private set; }

        public string ProtoId { get; private set; }

        public string Value { get; private set; }

        public PufferProtocol(
            long id, 
            DateTime dateTime, 
            char start, 
            char end, 
            char delimiter,
            string command,
            string protoId,
            string value
            ) 
            : base(id, dateTime, start, end, delimiter)
        {
            Command = command;
            ProtoId = protoId;
            Value = value;
        }

    }
}
