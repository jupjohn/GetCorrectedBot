using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Jammehcow.Twitch.GetCorrected;

public class ChatHistoryService
{
    private readonly ILogger<ChatHistoryService> _logger;
    private readonly Dictionary<string, ConcurrentQueue<(string username, string message)>> _history = new();

    public ChatHistoryService(ILogger<ChatHistoryService> logger)
    {
        _logger = logger;
    }

    public void AddLineForChannel(string channel, string user, string message)
    {
        if (!_history.ContainsKey(channel))
        {
            _history[channel] = new ConcurrentQueue<(string username, string message)>(new []{(user, message)});
            return;
        }

        var channelHistory = _history[channel];
        channelHistory.Enqueue((user, message));

        // TODO: schedule or make every few messages or make TruncatingConcurrentQueue
        while (channelHistory.Count > 100)
            channelHistory.TryDequeue(out _);
    }

    public IEnumerable<(string sender, string message)> GetHistorySnapshotForChannel(string channel)
    {
        // NOTE: better collection?
        return _history.TryGetValue(channel, out var history)
            ? history.Reverse().ToArray()
            : Array.Empty<(string, string)>();
    }
}
