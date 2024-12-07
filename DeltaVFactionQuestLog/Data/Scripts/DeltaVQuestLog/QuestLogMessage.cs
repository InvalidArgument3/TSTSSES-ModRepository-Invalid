using System.Collections.Generic;
using ProtoBuf;

namespace Invalid.DeltaVQuestLog
{
    [ProtoContract]
    public class QuestLogMessage
    {
        [ProtoMember(1)]
        public long FactionId { get; set; }
        [ProtoMember(2)]
        public List<string> Objectives { get; set; }
        [ProtoMember(3)]
        public string Title { get; set; }
        [ProtoMember(4)]
        public int Duration { get; set; }
    }
}
