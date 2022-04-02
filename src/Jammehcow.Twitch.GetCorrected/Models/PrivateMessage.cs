using System;
using System.Collections.Generic;

namespace Jammehcow.Twitch.GetCorrected.Models;

public readonly struct PrivateMessage : IIrcMessage
{
    public readonly RawIrcMessage RawMessage;

    /// <summary>
    /// The username of the user that sent the PRIVMSG
    /// </summary>
    public readonly ReadOnlyMemory<char> Username;

    /// <summary>
    /// The channel that the PRIVMSG was sent in
    /// </summary>
    public readonly ReadOnlyMemory<char> Channel;

    /// <summary>
    /// The PRIVMSG's contents
    /// </summary>
    public readonly ReadOnlyMemory<char> Message;

    /// <summary>
    /// Parsed tags from the underlying raw message
    /// </summary>
    public readonly IDictionary<ReadOnlyMemory<char>, ReadOnlyMemory<char>> ParsedTags;

    /// <summary>
    /// The reported timestamp by Twitch's IRC server (tag-based)
    /// </summary>
    // public readonly DateTime IrcTimeStamp;

    /// <summary>
    /// List of IRC tags (parsed)
    /// </summary>
    // public readonly IReadOnlyList<string> Tags;

    public PrivateMessage(RawIrcMessage rawMessage, ReadOnlyMemory<char> username, ReadOnlyMemory<char> channel,
        ReadOnlyMemory<char> message, IDictionary<ReadOnlyMemory<char>, ReadOnlyMemory<char>> parsedTags)
    {
        RawMessage = rawMessage;
        Username = username;
        Message = message;
        ParsedTags = parsedTags;
        Channel = channel;
    }
}