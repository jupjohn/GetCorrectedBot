using System;

namespace Jammehcow.Twitch.GetCorrected.Models;

public readonly struct RawIrcMessage : IIrcMessage
{
    /// <summary>
    /// The raw IRc message that this message is derived from
    /// </summary>
    public readonly ReadOnlyMemory<char> Message;

    /// <summary>
    /// IRCv3 tags attached to the message
    /// </summary>
    public readonly ReadOnlyMemory<char> Tags;

    /// <summary>
    /// IRC hostmask of the originating user (e.g. jammehcow!jammehcow@jammehcow.tmi.twitch.tv)
    /// </summary>
    public readonly ReadOnlyMemory<char> Prefix;

    /// <summary>
    /// The type or IRC message (PRIVMSG, PING, ROOMSTATE etc.)
    /// </summary>
    public readonly ReadOnlyMemory<char> Command;

    /// <summary>
    /// The content of the message (after the message type, if any)
    /// </summary>
    public readonly ReadOnlyMemory<char> Parameters;

    public RawIrcMessage(ReadOnlyMemory<char> ircMessage, ReadOnlyMemory<char> tags, ReadOnlyMemory<char> prefix,
        ReadOnlyMemory<char> command, ReadOnlyMemory<char> parameters)
    {
        Message = ircMessage;
        Tags = tags;
        Prefix = prefix;
        Command = command;
        Parameters = parameters;
    }
}