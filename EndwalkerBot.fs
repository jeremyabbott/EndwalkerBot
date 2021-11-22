namespace WarriorOfLight.EndwalkerBot

open DSharpPlus.CommandsNext
open DSharpPlus.CommandsNext.Attributes
open System
open System.Threading.Tasks

module BotCommands =
    let daysUntil (target: DateTime) (now: DateTime) =
        let remainingDays = target - now
        remainingDays

    let formatDays (interval: TimeSpan) =
        $"{interval.Days} days, {interval.Hours} hours, and {interval.Minutes} minutes"

    let endwalker (message: string) (ctx: CommandContext) = 
        task {
            do! ctx.TriggerTypingAsync()
            let! _ = ctx.RespondAsync message 
            return ()
        } :> Task

open BotCommands

type EndwalkerBot() =
    inherit BaseCommandModule ()
    
    let endwalkerEarlyAccessString = "12/3/2021 1:00:00 AM"
    let endwalkerLaunchString = "12/7/2021 1:00:00 AM"
    let earlyAccess = DateTime.Parse(endwalkerEarlyAccessString)
    let launch = DateTime.Parse(endwalkerLaunchString)

    let daysUntilEarlyAccess now = daysUntil earlyAccess now
    let daysUntilLaunch now = daysUntil launch now

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

    [<Command "finalDays">]
    member __.FinalDays(ctx: CommandContext) = 
        let now = DateTime.UtcNow // todo timezones
        let earlyAccessMessage = 
            now
            |> daysUntilEarlyAccess
            |> formatDays
            |> getMessageString

        let launchMessage = 
            now
            |> daysUntilLaunch
            |> formatDays
            |> getMessageString

        let botMessage = $"EARLY ACCESS: {earlyAccessMessage}{Environment.NewLine}LAUNCH: {launchMessage}"
        endwalker botMessage ctx