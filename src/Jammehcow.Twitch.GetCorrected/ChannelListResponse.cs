using System.Collections.Generic;

namespace Jammehcow.Twitch.GetCorrected;

public class ChannelListResponse
{
    public class Bot
    {
        public string Username { get; set; } = string.Empty;
        public List<string> Channels { get; set; } = new();
    }

    public List<Bot> Bots { get; set; } = new();
}
