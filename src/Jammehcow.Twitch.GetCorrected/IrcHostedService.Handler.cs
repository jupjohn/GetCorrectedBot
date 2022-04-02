using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Humanizer;
using Humanizer.Localisation;
using Jammehcow.Twitch.GetCorrected.Helpers;
using Jammehcow.Twitch.GetCorrected.Models;
using MayBee;
using PHS.Networking.Enums;
using Tcp.NET.Client.Events.Args;

namespace Jammehcow.Twitch.GetCorrected;

public partial class IrcHostedService
{
    private static readonly char[] SubstitutionPrefix = "s/".ToCharArray();
    private static readonly char[] PingMessage = "s//ping".ToCharArray();
    private static readonly char[] JoinMessage = "s//join".ToCharArray();
    private static readonly char[] RejoinMessage = "s//rejoin".ToCharArray();

    // ReSharper disable once InconsistentNaming
    private async Task HandleMessage(string data)
    {
        var parsedMessage = IrcMessageParser.ParseMessageToTyped(data.AsMemory());

        // ReSharper disable once ConvertIfStatementToSwitchStatement
        if (parsedMessage is RawIrcMessage rawMessage)
        {
            Console.WriteLine($"\nRaw:\t{rawMessage.Message}");
        }
        else if (parsedMessage is PingMessage pingMessage)
        {
            await _tcpClient.SendToServerRawAsync($"PONG {pingMessage.Data}");
        }
        else if (parsedMessage is PrivateMessage privateMessage)
        {
            if (await TryHandlePrivateMessageAsync(privateMessage))
                return;

            Console.WriteLine(
                $"\nPRIVMSG: {privateMessage.Username} in " +
                $"#{privateMessage.Channel} {privateMessage.Message}");

            if (privateMessage.Message.Span.StartsWith("ACTION"))
                return;

            _chatHistoryService.AddLineForChannel(
                privateMessage.Channel.ToString(), privateMessage.Username.ToString(),
                privateMessage.Message.ToString());
        }
    }

    // ReSharper disable once InconsistentNaming
    private async Task<bool> TryHandlePrivateMessageAsync(PrivateMessage privateMessage)
    {
        // ReSharper disable once InvertIf
        if (privateMessage.Message.Span.StartsWith(PingMessage))
        {
            var pingTask = GetTmiPingInMillisAsync();
            var memTask = GetSystemMemoryStringAsync();

            var pingTime = (await pingTask).Exists ? $"{pingTask.Result.It}ms" : "TMI too too long to respond";
            var uptime = DateTime.Now.Subtract(Process.GetCurrentProcess().StartTime)
                .Humanize(minUnit: TimeUnit.Second, precision: 4);

            await _tcpClient.SendToServerRawAsync(
                $"PRIVMSG #{privateMessage.Channel} :🤓 TMI Ping: {pingTime} || Uptime: {uptime} || " +
                $"Memory: {await memTask}");

            return true;
        }

        if (privateMessage.Message.Span.StartsWith(JoinMessage))
        {
            if (!privateMessage.Username.Span.SequenceEqual("jammehcow"))
                return false;

            var channel = privateMessage.Message[(JoinMessage.Length+1)..];
            await _tcpClient.SendToServerRawAsync($"JOIN #{channel}");

            return true;
        }

        if (privateMessage.Message.Span.StartsWith(RejoinMessage))
        {
            if (!privateMessage.Username.Span.SequenceEqual("jammehcow"))
                return false;

            await _tcpClient.SendToServerRawAsync($"PRIVMSG #{privateMessage.Channel} :Starting rejoin");
            await JoinChannels();
            await _tcpClient.SendToServerRawAsync($"PRIVMSG #{privateMessage.Channel} :Rejoined!");

            return true;
        }

        if (privateMessage.Message.Span.StartsWith(SubstitutionPrefix))
        {
            // TODO: validate
            var split = privateMessage.Message.ToString().Split("/");

            if (split.Length != 3)
                return false;

            var from = split[1].Trim();
            var to = split[2].Trim();

            var hist = _chatHistoryService.GetHistorySnapshotForChannel(privateMessage.Channel.ToString());
            var possibleMatch = hist.FirstAsMaybe(line => line.message.Contains(from));

            if (possibleMatch.IsEmpty)
            {
                await _tcpClient.SendToServerRawAsync(
                    $"PRIVMSG #{privateMessage.Channel} :Couldn't find a message matching \"{from}\"");
                return true;
            }

            var replaced = possibleMatch.It.message.Replace(from, to);
            var thinksMessage = possibleMatch.It.sender == privateMessage.Username.ToString()
                ? $"{privateMessage.Username} meant to say"
                : $"{privateMessage.Username.ToString()} thinks @{possibleMatch.It.sender} meant to say";

            await _tcpClient.SendToServerRawAsync($"PRIVMSG #{privateMessage.Channel} :{thinksMessage}: {replaced}");

            return true;
        }

        return false;
    }

    private async Task<Maybe<long>> GetTmiPingInMillisAsync()
    {
        var sw = new Stopwatch();
        var pingId = Random.Shared.NextInt64();

        void HandleTmiPing(object sender, TcpMessageClientEventArgs args)
        {
            if (args.MessageEventType == MessageEventType.Receive &&
                args.Packet.Data.EndsWith($":tmi.twitch.tv PONG tmi.twitch.tv :{pingId.ToString()}"))
            {
                sw.Stop();
            }
        }

        _tcpClient.MessageEvent += HandleTmiPing;

        sw.Start();
        await _tcpClient.SendToServerRawAsync($"PING {pingId}");

        SpinWait.SpinUntil(() => !sw.IsRunning, TimeSpan.FromSeconds(5));

        _tcpClient.MessageEvent -= HandleTmiPing;

        return sw.IsRunning ? Maybe.Empty<long>() : new Maybe<long>(sw.ElapsedMilliseconds / 2);
    }

    private async Task<string> GetSystemMemoryStringAsync()
    {
        if (!OperatingSystem.IsLinux())
            return "unknown";

        var memInfoFile = await File.ReadAllLinesAsync("/proc/meminfo");
        var totalMemFromInfo = memInfoFile.First(s => s.StartsWith("MemTotal"));
        var totalMemKb = Regex.Split(totalMemFromInfo, "\\s+")[1];

        var totalMemory = (long)Convert.ToInt64(totalMemKb).Kilobytes().Megabytes;
        var usedMem = Process.GetCurrentProcess().PrivateMemorySize64 / 1024 / 1024;

        return $"{usedMem}/{totalMemory}MB";
    }
}
