using Jammehcow.Twitch.GetCorrected;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TTools.Configuration.KeyedOptions.Net6;

var builder = new HostBuilder();

builder.ConfigureServices(collection =>
{
    collection.AddHostedService<IrcHostedService>();
    collection.AddSingleton<ChatHistoryService>();
    collection.AddKeyedOptions<AuthConfiguration>();
    collection.AddHttpClient<ChannelListService>();
});

builder.ConfigureLogging(loggingBuilder => loggingBuilder.AddConsole());
builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddEnvironmentVariables());

var app = builder.Build();
app.Run();
