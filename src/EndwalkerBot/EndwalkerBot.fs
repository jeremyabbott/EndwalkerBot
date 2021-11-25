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
open Subscriptions

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
            .Build()

    let sendFromCommandContext (ctx: CommandContext) =
        fun (embed: DiscordEmbed) ->
            task {
                do! ctx.TriggerTypingAsync()
                let! message = ctx.RespondAsync(embed)
                return message
            }

    let sendFromDiscordClient (client: DiscordClient) (channel: DiscordChannel) =
        fun (embed: DiscordEmbed) ->
            task {
                let! message = client.SendMessageAsync(channel, embed)
                return message
            }

    let sendFinalDaysMessage<'t> finalDays (send: DiscordEmbed -> Task<'t>) =
        finalDays
        |> buildEmbed
        |> send

    type BotOptions() =
        let earlyAccessDefault = DateTime(2021, 12, 3, 9, 0, 0)
        let releaseDefault = DateTime(2021, 12, 7, 9, 0, 0)
        static member EndwalkerBotConfig = "EndwalkerBotConfig"
        member val EarlyAccessDate = earlyAccessDefault with get, set
        member val ReleaseDate = releaseDefault with get, set
        member this.ToFinalDays now =
            FinalDays.Create(this.EarlyAccessDate, this.ReleaseDate, now)

    type EndwalkerBot(options: IOptions<BotOptions>, subscriptionService: SubscriptionService) =
        inherit BaseCommandModule ()
        let botOptions = options.Value

        [<Command "finalDays">]
        member _.FinalDays(ctx: CommandContext) =
            sendFromCommandContext ctx
            |> sendFinalDaysMessage (botOptions.ToFinalDays DateTime.UtcNow)

        [<Command "subscribe">]
        [<RequireUserPermissions(Permissions.Administrator, false)>]
        member _.Info(ctx:CommandContext) =
            task {
                let channel = ctx.Channel.Id

                let subRequest = SubscriptionRequest.Create FinalDays channel ctx.User.Id ctx.Guild.Id
                try
                    let! _ = subscriptionService.Subscribe subRequest
                    ()
                with ex ->
                    printfn $"Suscribe go boom {ex}"
                do! ctx.TriggerTypingAsync()
                let! _ = ctx.RespondAsync("Verified!")
                return ()
            } :> Task

    let buildCommandsConfig (serviceProvider: IServiceProvider) =
        let commandsConfig = CommandsNextConfiguration()
        commandsConfig.StringPrefixes <- ["!"]
        commandsConfig.Services <- serviceProvider
        commandsConfig

type DiscordOptions() =
    static member DiscordConfig = "DiscordConfig"
    member val BotToken = "" with get, set

type Tataru(logger: ILogger<Tataru>, discordClient: DiscordClient, botOptions: IOptions<Bot.BotOptions>, subService: SubscriptionService,  serviceProvider:IServiceProvider) =
    let commandsConfig = Bot.buildCommandsConfig serviceProvider
    let commands = discordClient.UseCommandsNext(commandsConfig)
    let mutable timerTask: Task option = None
    let stoppingCts = new CancellationTokenSource()
    let mutable timer: PeriodicTimer = null
    let amTime = TimeOnly(3, 0)
    let pmTime = TimeOnly(4, 0)
    let times = Seq.init 13 (fun i -> amTime.AddMinutes(47 + i))

    let sendFinalDaysMessage (send: DiscordChannel -> DiscordEmbed -> Task<_>) (getChannel: uint64 -> Task<DiscordChannel>) finalDays =
        task {
            let! subs = subService.List()
            let! _ =
                subs
                |> Seq.map (fun s ->
                    task {
                        let! c = getChannel s.ChannelId
                        let! m =
                            send c
                            |> Bot.sendFinalDaysMessage finalDays
                        return m
                    })
                |> Task.WhenAll
            return ()
        } :> Task

    let doWork (ct: CancellationToken) =
        task {
            let mutable keepGoing = true
            while not ct.IsCancellationRequested && keepGoing do
                let! result = timer.WaitForNextTickAsync(ct)
                keepGoing <- result
                let now = DateTime.UtcNow
                let nowTime = TimeOnly.FromDateTime now
                let send = times |> Seq.exists (fun t -> t.Hour = nowTime.Hour && t.Minute = nowTime.Minute && t.Second = nowTime.Second)
                do!
                    if keepGoing && send then
                        sendFinalDaysMessage (fun c e -> discordClient.SendMessageAsync(c, e))  discordClient.GetChannelAsync (botOptions.Value.ToFinalDays DateTime.UtcNow)
                    else
                        Task.CompletedTask
        } :> Task

    let stopWork (ct: CancellationToken) =
        task {
            let! _ =
                match timerTask with
                | None -> Task.FromResult(Task.CompletedTask)
                | Some t ->
                    stoppingCts.Cancel()
                    Task.WhenAny(t, Task.Delay(Timeout.Infinite, ct))
            return ()
        }

    interface IHostedService with
        member _.StartAsync(ct: CancellationToken) =
            task {
                timer <- new PeriodicTimer(TimeSpan.FromSeconds(1.))
                do commands.RegisterCommands<Bot.EndwalkerBot>()
                do! discordClient.ConnectAsync()
                timerTask <- doWork(stoppingCts.Token) |> Some
                return!
                    timerTask
                    |> Option.filter(fun t -> t.IsCompleted)
                    |> Option.defaultValue Task.CompletedTask
            }

        member _.StopAsync(ct: CancellationToken) : Task =
            task {
                do! stopWork(ct)
                do! discordClient.DisconnectAsync()
            }

    interface IDisposable with
        member _.Dispose() =
            stoppingCts.Cancel()
            timer.Dispose()
