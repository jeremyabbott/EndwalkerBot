#EndwalkerBot

Simple Discord bot that responds to a single command (`!finaldays`) and responds with the numbers of days until Endwalker early access and the offical launch happen

## Developer Setup

* Install Docker
* Install VS Code
* Install the [VS Code Remote - Containers extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers)

### Running the Bot for Local Testing

1. [Follow Mathias' instructions under Prequisities/Setup](https://brandewinder.com/2021/10/30/fsharp-discord-bot/)
1. Using the token from the previous step either 
    1. From the command line run `export DiscordConfig__BotToken=<yourtoken>`. This will make the token available to the app as long as you run the app from the same terminal session. I prefer doing it this way so I don't accidentally commit/push a token to GitHub 
    1. Paste the token into the `AppSettings.Json` for `DiscordConfig.BotToken`
