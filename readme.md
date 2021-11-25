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

#### Azurite

The connection string `"AzureTableConnectionString": "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;TableEndpoint=http://host.docker.internal:10002/devstoreaccount1;"` from `AppSettings.json` only works with Azurite which you can run via docker via the following commands

1. `docker pull mcr.microsoft.com/azure-storage/azurite`
1. `docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite`

If you want to persist storage across runs you'll need to mount a volume (check the [Azurite docks](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=docker-hub) for details)
