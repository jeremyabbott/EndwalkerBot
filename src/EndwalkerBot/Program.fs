namespace WarriorOfLight.EndwalkerBot

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

module Program =
    open Bot
    open Azure.Data.Tables
    open Microsoft.Extensions.Options
    open DSharpPlus
    
    let createHostBuilder args =
        Host.CreateDefaultBuilder(args)
            .ConfigureServices(fun hostContext services ->
                services.Configure<DiscordOptions>(hostContext.Configuration.GetSection(DiscordOptions.DiscordConfig)) |> ignore
                services.Configure<BotOptions>(hostContext.Configuration.GetSection(BotOptions.EndwalkerBotConfig)) |> ignore
                services.Configure<Db.TablesOptions>(hostContext.Configuration.GetSection(Db.TablesOptions.TablesConfig)) |> ignore
                services.AddSingleton<TableServiceClient>(fun svcProvider ->
                    let options = svcProvider.GetService<IOptions<Db.TablesOptions>>()
                    TableServiceClient(options.Value.AzureTableConnectionString)) |> ignore
                services.AddSingleton<Subscriptions.SubscriptionRepository>() |> ignore
                services.AddSingleton<Subscriptions.SubscriptionService>() |> ignore
                services.AddSingleton<DiscordClient>(fun svcProvider ->
                    let options = svcProvider.GetService<IOptions<DiscordOptions>>()
                    let config = DiscordConfiguration(Token=options.Value.BotToken, TokenType=TokenType.Bot)
                    new DiscordClient(config)) |> ignore
                services.AddHostedService<Tataru>() |> ignore)
    
    [<EntryPoint>]
    let main args =
        createHostBuilder(args).Build().Run()

        0