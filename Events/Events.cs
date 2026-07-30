
namespace LightInDark.Events
{
    public abstract class BasePlayerEvent : IEvent
    {
        public PlayerControl Player { get; set; }
    }
    public class PlayerSuicideEvent : BasePlayerEvent
    {
        public string Reason { get; set; } = "suicide";
        public bool NeedLog { get; set; } = true;
    }

    public class PlayerMurderEvent : BasePlayerEvent
    {
        public PlayerControl Killer => Player;
        public PlayerControl Victim { get; set; }
    }
    public class ShowChatEvent : IEvent { }
    public class HideChatEvent : IEvent { }
}