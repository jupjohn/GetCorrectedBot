using System;

namespace Jammehcow.Twitch.GetCorrected.Models;

public struct PingMessage : IIrcMessage
{
    public readonly RawIrcMessage RawMessage;

    /// <summary>
    /// Data (if any) expected to be returned by a subsequent PONG
    /// </summary>
    public readonly ReadOnlyMemory<char> Data;

    public PingMessage(RawIrcMessage rawMessage, ReadOnlyMemory<char> data)
    {
        RawMessage = rawMessage;
        Data = data[1..];
    }
}