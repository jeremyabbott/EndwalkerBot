namespace WarriorOfLight.EndwalkerBot

open DSharpPlus.CommandsNext
open DSharpPlus.CommandsNext.Attributes
open System
open System.Linq
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options
open DSharpPlus
open DSharpPlus.Entities

module Bot =
    let daysUntil (target: DateTime) (now: DateTime) =
        let remainingDays = target - now
        remainingDays

    let formatDays (interval: TimeSpan) =
        $"{interval.Days} days, {interval.Hours} hours, and {interval.Minutes} minutes"

    let messages =
            [|
                sprintf "The star is doomed to unravel in %s"
                sprintf "In %s days we scions will fight, until the heavens fall, until our last breath."
                sprintf "The final days will be upon us in %s"
            |]
    
    let getMessageString message =
        let r = Random()
        let l = messages.Length
        let n = r.Next(0, l)
        messages[n] message
    
    let buildEmbed (earlyAccessInterval: TimeSpan) (releaseInterval:TimeSpan) =
        let earlyAccessMessage =
            earlyAccessInterval
            |> formatDays
            |> getMessageString
        let releaseMessage =
            releaseInterval
            |> formatDays
            |> getMessageString
        let botMessage = $"EARLY ACCESS: {earlyAccessMessage}{Environment.NewLine}LAUNCH: {releaseMessage}{Environment.NewLine}"
        let eb = DiscordEmbedBuilder()
        eb
            .WithDescription(botMessage)
            .WithTitle("Time Until the Final Days")

    let sendFinalDaysMessage (earlyAccessInterval: TimeSpan) (releaseInterval:TimeSpan) (ctx: CommandContext) = 
        task {
            do! ctx.TriggerTypingAsync()
            let embed = buildEmbed earlyAccessInterval releaseInterval
            let! _ = 
                ctx.RespondAsync(embed)
            return ()
        } :> Task

    type BotOptions() =
        static member EndwalkerBotConfig = "EndwalkerBotConfig"
        member val EarlyAccessDate = DateTime.UtcNow with get, set
        member val ReleaseDate = DateTime.UtcNow with get, set

    type EndwalkerBot(options: BotOptions) =
        inherit BaseCommandModule ()
        
        let botOptions = options
        
        let daysUntilEarlyAccess now = daysUntil botOptions.EarlyAccessDate now
        let daysUntilLaunch now = daysUntil botOptions.ReleaseDate now

        [<Command "finalDays">]
        member _.FinalDays(ctx: CommandContext) = 
            let now = DateTime.UtcNow // todo timezones
            
            let earlyAccess = 
                now
                |> daysUntilEarlyAccess
                
            let launch = 
                now
                |> daysUntilLaunch
                
            sendFinalDaysMessage earlyAccess launch ctx

type DiscordOptions() =
    static member DiscordConfig = "DiscordConfig"
    member val BotToken = "" with get, set

type Tataru(logger: ILogger<Tataru>, options: IOptions<DiscordOptions>, cmdsConfig: CommandsNextConfiguration) =
    inherit BackgroundService()
    let discordConfig = DiscordConfiguration(Token=options.Value.BotToken, TokenType=TokenType.Bot)
    let commandsConfig = cmdsConfig
    let discordClient = new DiscordClient(discordConfig)
    let commands = discordClient.UseCommandsNext(commandsConfig)
    do commands.RegisterCommands<Bot.EndwalkerBot>()
    
    override _.ExecuteAsync(ct: CancellationToken) =
        task {
            do! discordClient.ConnectAsync()
            while not ct.IsCancellationRequested do
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now)
                do! Task.Delay(1000)
        } :> Task
