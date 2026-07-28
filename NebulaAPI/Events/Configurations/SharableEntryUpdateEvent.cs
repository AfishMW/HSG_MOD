using Virial.Configuration;

namespace Virial.Events.Configurations;

public class SharableEntryUpdateEvent : Event
{
    public ISharableEntry SharableEntry { get; }
    internal SharableEntryUpdateEvent(ISharableEntry entry)
    {
        SharableEntry = entry;
    }
}
