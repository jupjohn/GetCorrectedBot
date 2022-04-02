using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PHS.Networking.Enums;
using Tcp.NET.Client;
using Tcp.NET.Client.Events.Args;
using Tcp.NET.Client.Models;

namespace Jammehcow.Twitch.GetCorrected;

public partial class IrcHostedService : IHostedService
{
    /// <summary>
    /// Automatically injected by generic host builder
    /// </summary>
    private readonly ILogger<IrcHostedService> _logger;

    private readonly ChatHistoryService _chatHistoryService;
    private readonly ChannelListService _channelListService;

    private readonly AuthConfiguration _authConfig;

    // TODO: allow multiple + handle as single
    private ITcpNETClient _tcpClient = null!;

    public IrcHostedService(ILogger<IrcHostedService> logger, IOptions<AuthConfiguration> authConfig,
        ChatHistoryService chatHistoryService, ChannelListService channelListService)
    {
        _logger = logger;
        _authConfig = authConfig.Value;
        _chatHistoryService = chatHistoryService;
        _channelListService = channelListService;
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_authConfig.Username) ||
            !Regex.IsMatch(_authConfig.Username, "^[a-zA-Z0-9_]{3,25}$"))
        {
            throw new ArgumentException($"Username of {_authConfig.Username} is invalid");
        }

        if (string.IsNullOrWhiteSpace(_authConfig.Token) ||
            !Regex.IsMatch(_authConfig.Token, "^oauth:[a-z0-9_]+$"))
        {
            throw new ArgumentException("OAuth token is invalid");
        }

        var tcpClientParameters = new ParamsTcpClient
        {
            Uri = "irc.chat.twitch.tv",
            Port = 6697,
            EndOfLineCharacters = "\r\n",
            IsSSL = true
        };
        _tcpClient = new TcpNETClient(tcpClientParameters);

        _tcpClient.MessageEvent += (_, args) =>
        {
            switch (args.MessageEventType)
            {
                case MessageEventType.Sent:
                    break;
                case MessageEventType.Receive:
                    Task.Run(() => HandleMessage(args.Packet.Data), stoppingToken)
                        .SafeFireAndForget(exception =>
                        {
                            _logger.LogError(exception, "An exception occurred while handling a message");
                        });
                    break;
                default:
                    throw new InvalidOperationException(
                        $"An invalid message event type was received: {args.MessageEventType}");
            }
        };

        _logger.LogTrace("Connecting to {IrcAddress}:{IrcPort}", tcpClientParameters.Uri, tcpClientParameters.Port);

        await TryConnectWithRetry(stoppingToken);

        await Task.Delay(3000, stoppingToken);
        await _tcpClient.SendToServerRawAsync("CAP REQ :twitch.tv/tags twitch.tv/commands twitch.tv/membership");

        await JoinChannels(stoppingToken);
        // await _tcpClient.SendToServerRawAsync("JOIN #testaccount_8080");
    }

    private async Task JoinChannels(CancellationToken stoppingToken = default)
    {
        var channelList = await _channelListService.GetChannelListAsync(_authConfig.Username, stoppingToken);
        foreach (var channel in channelList)
        {
            await Task.Delay(340, stoppingToken);
            await _tcpClient.SendToServerRawAsync($"JOIN #{channel}");
        }
    }

    private async Task TryConnectWithRetry(CancellationToken stoppingToken)
    {
        using var connectionTestCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        void OnConnectionTest(object _, TcpMessageClientEventArgs args)
        {
            if (!args.Packet.Data.StartsWith(":tmi.twitch.tv 001"))
                return;

            _logger.LogInformation("Successfully connected");
            connectionTestCancellation.Cancel();
        }

        _tcpClient.MessageEvent += OnConnectionTest;

        const int maxRetries = 5;
        for (var retryCount = 0; retryCount < maxRetries; retryCount++)
        {
            _logger.LogInformation("Attempting to connect (attempt #{RetryCount})", retryCount + 1);

            if (retryCount > 0)
                await Task.Delay(2 * retryCount, stoppingToken);

            var connectionTask = _tcpClient.ConnectAsync(stoppingToken);
            var didEstablishConnection = Task.WaitAll(new []{connectionTask}, TimeSpan.FromSeconds(5));

            if (!didEstablishConnection)
            {
                _logger.LogWarning("Failed to establish connection, retrying");
                continue;
            }

            await _tcpClient.SendToServerRawAsync($"PASS {_authConfig.Token}");
            await _tcpClient.SendToServerRawAsync($"NICK {_authConfig.Username}");

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), connectionTestCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            // FIXME
            _tcpClient.Disconnect();
        }

        if (!connectionTestCancellation.IsCancellationRequested)
        {
            // Why
            _logger.LogError("Failed to connect after {MaxRetryCount} attempts", maxRetries);
            throw new InvalidOperationException();
        }

        _tcpClient.MessageEvent -= OnConnectionTest;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _tcpClient.Disconnect();
        _tcpClient.Dispose();

        return Task.CompletedTask;
    }
}
