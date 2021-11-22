namespace WarriorOfLight.EndwalkerBot

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open DSharpPlus.CommandsNext

module Program =
    open Bot

    let createHostBuilder args =
        Host.CreateDefaultBuilder(args)
            .ConfigureServices(fun hostContext services ->
                services.Configure<DiscordOptions>(hostContext.Configuration.GetSection(DiscordOptions.DiscordConfig)) |> ignore
                services.AddSingleton<CommandsNextConfiguration>(fun svcProvider -> 
                    let commandsConfig = CommandsNextConfiguration()
                    commandsConfig.StringPrefixes <- ["!"]
                    let svcs = ServiceCollection()
                    svcs.AddSingleton<BotOptions>(fun _ ->
                        let c = svcProvider.GetService<IConfiguration>()
                        let botOptions =
                            c.GetSection(BotOptions.EndwalkerBotConfig).Get<BotOptions>()
                        botOptions
                    ) |> ignore
                    commandsConfig.Services <- svcs.BuildServiceProvider()
                    commandsConfig
                ) |> ignore
                services.AddHostedService<Tataru>() |> ignore)
    
    [<EntryPoint>]
    let main args =
        createHostBuilder(args).Build().Run()

        0