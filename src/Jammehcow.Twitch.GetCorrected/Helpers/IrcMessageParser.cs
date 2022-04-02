using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Jammehcow.Twitch.GetCorrected.Models;

namespace Jammehcow.Twitch.GetCorrected.Helpers;

public class IrcMessageParser
{
    public static IIrcMessage ParseMessageToTyped(ReadOnlyMemory<char> rawMessage)
    {
        var parsed = ParseRawMessage(rawMessage);

        if (parsed.Command.Span.SequenceEqual("PRIVMSG"))
            return ParsePrivateMessage(parsed);
        if (parsed.Command.Span.SequenceEqual("PING"))
            return new PingMessage(parsed, parsed.Parameters);

        return parsed;
    }

    public static RawIrcMessage ParseRawMessage(ReadOnlyMemory<char> rawMessage)
    {
        // FIXME: return ':' for future parsers to handle
        var (tags, postTags) = ParseTags(rawMessage);
        var (possiblePrefix, postPrefix) = SliceNextSpace(postTags);

        // TODO bounds
        if (possiblePrefix.Span[0] != ':')
            return new RawIrcMessage(rawMessage, tags, ReadOnlyMemory<char>.Empty, possiblePrefix, postPrefix);

        var (command, parameters) = SliceNextSpace(postPrefix);
        return new RawIrcMessage(rawMessage, tags, possiblePrefix.TrimStart(":"), command, parameters);
    }

    private static (ReadOnlyMemory<char> tags, ReadOnlyMemory<char> post) ParseTags(ReadOnlyMemory<char> message)
    {
        // TODO: bounds check
        if (message.IsEmpty || message.Span[0] != '@')
            return (ReadOnlyMemory<char>.Empty, message);

        // We search for the first occurrence of ' :' which denotes the end of tags
        for (var messagePos = 0; messagePos < message.Length; messagePos++)
        {
            // FIXME: out of bounds
            if (message.Span[messagePos] != ' ')
                continue;

            if (message.Span[messagePos + 1] != ':')
                continue;

            return (message[1..messagePos], message[(messagePos+1)..]);
        }

        return (message, ReadOnlyMemory<char>.Empty);
    }

    private static (ReadOnlyMemory<char> segment, ReadOnlyMemory<char> post) SliceNextSpace(
        ReadOnlyMemory<char> message)
    {
        for (var messagePos = 0; messagePos < message.Length; messagePos++)
        {
            if (message.Span[messagePos] != ' ')
                continue;

            return (message[..messagePos], message[(messagePos + 1)..]);
        }

        return (message, ReadOnlyMemory<char>.Empty);
    }

    public static PrivateMessage ParsePrivateMessage(RawIrcMessage message)
    {
        var username = message.Prefix[..message.Prefix.Span.IndexOf('!')];
        var spannedParameters = message.Parameters.Span;

        // TODO: handle # being pushed out a char
        // TODO: maybe FF, caused by recent messages api
        var separatorIndex = spannedParameters.IndexOf(':');
        if (separatorIndex == -1)
            separatorIndex = spannedParameters.LastIndexOf(" ");
        var channel = message.Parameters[1..(separatorIndex - 1)];
        // TODO: add test
        var content = message.Parameters[(separatorIndex + 1)..];

        var parsedTags = ParseTagsToDictionary(message.Tags);
        return new PrivateMessage(message, username, channel, content, parsedTags);
    }

    public static IDictionary<ReadOnlyMemory<char>, ReadOnlyMemory<char>> ParseTagsToDictionary(ReadOnlyMemory<char> tags)
    {
        if (tags.Length == 0)
        {
            return
                new Dictionary<ReadOnlyMemory<char>, ReadOnlyMemory<char>>(0)
                    .ToImmutableDictionary();
        }

        var parsedTags = new Dictionary<ReadOnlyMemory<char>, ReadOnlyMemory<char>>();
        var remainingTags = tags;

        do
        {
            var endOfTagIndex = remainingTags.Span.IndexOf(';');

            // BUGBUG: empty string at end could cause -1 index?
            if (endOfTagIndex == -1)
                endOfTagIndex = remainingTags.Length;

            var currentTag = remainingTags[..endOfTagIndex];
            var tagKey = currentTag[..currentTag.Span.IndexOf('=')];
            var tagValue = currentTag[(tagKey.Length + 1)..];

            // FIXME: Maybe don't use strings?
            parsedTags.Add(tagKey, tagValue);

            var realEndIndex = Math.Min(remainingTags.Length, endOfTagIndex + 1);
            remainingTags = remainingTags[realEndIndex..];
        } while (remainingTags.Length > 0);

        return parsedTags.ToImmutableDictionary();
    }
}