namespace WarriorOfLight.EndwalkerBot

module Program =

    open System.IO
    open System.Threading.Tasks
    open Microsoft.Extensions.Configuration
    open DSharpPlus
    open DSharpPlus.CommandsNext

    let config =
        ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("AppSettings.json", true, true)
            .Build()

    [<EntryPoint>]
    let main argv =
        printfn "Starting"
        let token = config.["DiscordBotToken"]
        let config = DiscordConfiguration(Token=token, TokenType=TokenType.Bot)

        let commandsConfig = CommandsNextConfiguration ()
        commandsConfig.StringPrefixes <- ["!"]

        let client = new DiscordClient(config)

        let commands = client.UseCommandsNext(commandsConfig)
        commands.RegisterCommands<EndwalkerBot>()

        client.ConnectAsync()
        |> Async.AwaitTask
        |> Async.RunSynchronously

        Task.Delay(-1)
        |> Async.AwaitTask
        |> Async.RunSynchronously


        0