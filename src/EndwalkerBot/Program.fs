namespace WarriorOfLight.EndwalkerBot

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

module Program =
    open Bot

    let createHostBuilder args =
        Host.CreateDefaultBuilder(args)
            .ConfigureServices(fun hostContext services ->
                services.Configure<DiscordOptions>(hostContext.Configuration.GetSection(DiscordOptions.DiscordConfig)) |> ignore
                services.Configure<BotOptions>(hostContext.Configuration.GetSection(BotOptions.EndwalkerBotConfig)) |> ignore
                services.AddHostedService<Tataru>() |> ignore)
    
    [<EntryPoint>]
    let main args =
        createHostBuilder(args).Build().Run()

        0