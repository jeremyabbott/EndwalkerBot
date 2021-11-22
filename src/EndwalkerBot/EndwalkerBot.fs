namespace WarriorOfLight.EndwalkerBot

open DSharpPlus.CommandsNext
open DSharpPlus.CommandsNext.Attributes
open System
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options
open DSharpPlus
open DSharpPlus.Entities
open Microsoft.Extensions.DependencyInjection

module Bot =
    
    let daysUntil (target: DateTime) (now: DateTime) =
        let remainingDays = target - now
        remainingDays

    let formatDays (interval: TimeSpan) =
        $"{interval.Days} days, {interval.Hours} hours, and {interval.Minutes} minutes"
    
    type FinalDays =
        { EarlyAccessDate: DateTime
          EarlyAccessInterval: TimeSpan
          EarlyAccessTimeStamp: int64
          ReleaseDate: DateTime
          ReleaseInterval: TimeSpan
          ReleaseTimeStamp: int64
          Now: DateTime } 
        static member Create(earlyAccessDate, releaseDate, now) =
            { EarlyAccessDate = earlyAccessDate
              EarlyAccessInterval = daysUntil earlyAccessDate now 
              EarlyAccessTimeStamp = DateTimeOffset(earlyAccessDate).ToUnixTimeSeconds()
              ReleaseDate = releaseDate
              ReleaseInterval = daysUntil releaseDate now
              ReleaseTimeStamp = DateTimeOffset(releaseDate).ToUnixTimeSeconds()
              Now = now }

    let messages =
            [|
                sprintf "The star is doomed to unravel in %s"
                sprintf "In %s days we scions will fight, until the heavens fall, until our last breath."
                sprintf "The final days will be upon us in %s"
            |]
    
    let rec getRandomExclusive (random: unit -> int) (exclude: int) =
        let r = random()
        if (r = exclude) then getRandomExclusive random exclude
        else r

    let getMessageStrings () =
        let r = Random()
        let l = messages.Length
        let f = r.Next(0, l)
        let s = getRandomExclusive (fun _ -> r.Next(0, l)) f
        messages[f], messages[s]
    
    /// https://discord.com/developers/docs/reference#message-formatting-formats
    let getRelativeEpochTag (ts: int64) =
        $"<t:{ts}:R>"

    /// https://discord.com/developers/docs/reference#message-formatting-formats
    let getFullDateTimeEpochTag (ts: int64) =
        $"<t:{ts}:F>"

    let buildEmbed (finalDays: FinalDays) =
        let (firstMessage, secondMessage) = getMessageStrings ()
        let earlyAccessMessage =
            let formattedDate = 
                finalDays.EarlyAccessTimeStamp
                |> getFullDateTimeEpochTag
            let relativeDate =
                finalDays.EarlyAccessTimeStamp
                |> getRelativeEpochTag
            let cuteMessage =
                finalDays.EarlyAccessInterval
                |> formatDays
                |> firstMessage
            $"{formattedDate}{Environment.NewLine}{relativeDate}{Environment.NewLine}{cuteMessage}"
        let releaseMessage =
            let formattedDate = 
                finalDays.ReleaseTimeStamp
                |> getFullDateTimeEpochTag
            let relativeDate =
                finalDays.ReleaseTimeStamp
                |> getRelativeEpochTag
            let cuteMessage =
                finalDays.ReleaseInterval
                |> formatDays
                |> secondMessage
            $"{formattedDate}{Environment.NewLine}{relativeDate}{Environment.NewLine}{cuteMessage}"

        let eb = DiscordEmbedBuilder()
        eb
            .WithTitle("Time Until the Final Days")
            .AddField("Early Access", earlyAccessMessage)
            .AddField("Offical Launch", releaseMessage)
            .WithTimestamp(finalDays.Now)

    let sendFinalDaysMessage (finalDays: FinalDays) (ctx: CommandContext) = 
        task {
            do! ctx.TriggerTypingAsync()
            let embed = buildEmbed finalDays
            let! _ = 
                ctx.RespondAsync(embed)
            return ()
        } :> Task

    type BotOptions() =
        let earlyAccessDefault = DateTime(2021, 12, 3, 9, 0, 0)
        let releaseDefault = DateTime(2021, 12, 7, 9, 0, 0)
        static member EndwalkerBotConfig = "EndwalkerBotConfig"
        
        member val EarlyAccessDate = earlyAccessDefault with get, set
        member val ReleaseDate = releaseDefault with get, set

    type EndwalkerBot(options: BotOptions) =
        inherit BaseCommandModule ()
        
        let botOptions = options
        
        [<Command "finalDays">]
        member _.FinalDays(ctx: CommandContext) = 
            let now = DateTime.UtcNow 
            
            let finalDays = FinalDays.Create(botOptions.EarlyAccessDate, botOptions.ReleaseDate, DateTime.UtcNow)
                
            sendFinalDaysMessage finalDays ctx
    
    let buildCommandsConfig (botOptions: BotOptions) =
        let commandsConfig = CommandsNextConfiguration()
        commandsConfig.StringPrefixes <- ["!"]
        let svcs = ServiceCollection()
        svcs.AddSingleton<BotOptions>(fun _ -> botOptions) |> ignore
        commandsConfig.Services <- svcs.BuildServiceProvider()
        commandsConfig

type DiscordOptions() =
    static member DiscordConfig = "DiscordConfig"
    member val BotToken = "" with get, set

type Tataru(logger: ILogger<Tataru>, options: IOptions<DiscordOptions>, botOptions: IOptions<Bot.BotOptions>) =
    inherit BackgroundService()
    
    do logger.LogInformation("Starting: {time}", DateTimeOffset.Now)
    let discordConfig = DiscordConfiguration(Token=options.Value.BotToken, TokenType=TokenType.Bot)
    do logger.LogInformation($"botOptions: EarlyAccessDate {botOptions.Value.EarlyAccessDate}")
    let commandsConfig = Bot.buildCommandsConfig botOptions.Value
    let discordClient = new DiscordClient(discordConfig)
    let commands = discordClient.UseCommandsNext(commandsConfig)
    do commands.RegisterCommands<Bot.EndwalkerBot>()
    
    override _.ExecuteAsync(ct: CancellationToken) =
        task {
            do! discordClient.ConnectAsync()
            while not ct.IsCancellationRequested do
                do! Task.Delay(1000)
        } :> Task
