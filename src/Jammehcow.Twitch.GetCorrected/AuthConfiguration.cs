using TTools.Configuration.KeyedOptions.Shared;

namespace Jammehcow.Twitch.GetCorrected;

public class AuthConfiguration : IKeyedOptions
{
    public string SectionKey => "Authentication";

    public string Username { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}
