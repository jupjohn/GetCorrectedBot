using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jammehcow.Twitch.GetCorrected;

public class ChannelListService
{
    private readonly ILogger<ChannelListService> _logger;
    private readonly HttpClient _httpClient;
    private readonly AuthConfiguration _authConfig;

    public ChannelListService(ILogger<ChannelListService> logger, HttpClient httpClient,
        IOptions<AuthConfiguration> config)
    {
        _logger = logger;
        _httpClient = httpClient;
        _authConfig = config.Value;
    }

    public async Task<List<string>> GetChannelListAsync(string username, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Fetching channels for bot {Username}", _authConfig.Username);

        var response = await _httpClient.GetFromJsonAsync<ChannelListResponse>(
            "https://gist.githubusercontent.com/jammehcow/cfe63f93a2a2bd89250951c0dff3906b/raw",
            stoppingToken);

        if (response is null || response.Bots.TrueForAll(b => b.Username != username))
        {
            _logger.LogError("Channel list URL is invalid");
            throw new InvalidOperationException();
        }

        var channels = response.Bots.First(b => b.Username == username).Channels;

        _logger.LogInformation("Found {ChannelCount} channels for user {Username}", channels.Count, username);
        return channels;
    }
}
