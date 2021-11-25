namespace WarriorOfLight.EndwalkerBot

[<AutoOpen>]
module AsyncSeqHelp =
    open System.Collections.Generic
    type IAsyncEnumerable<'T> with
        member this.AsTask () = task {
            let mutable moreData = true
            let output = ResizeArray ()
            let enumerator = this.GetAsyncEnumerator()
            while moreData do
                let! next = enumerator.MoveNextAsync()
                moreData <- next
                if moreData then output.Add enumerator.Current
            return output.ToArray()
        }

module Db =
    open Azure.Data.Tables

    type TablesOptions() =
        static member TablesConfig = "TablesConfig"
        member val AzureTableConnectionString: string = null with get, set

    type TablesService(tableServiceClient: TableServiceClient, tableName: string) =
        do tableServiceClient.CreateTableIfNotExists(tableName) |> ignore
        let tableClient = tableServiceClient.GetTableClient(tableName)

        member _.UpsertAsync(entity) =
            tableClient.AddEntityAsync(entity)

        member _.List() =
            task {
                let! results = tableClient.QueryAsync<TableEntity>().AsTask()
                return results
            }

module Core =
    type ChannelId = uint64
    type UserId = uint64
    type GuildId = uint64

module Subscriptions =

    open System
    open Core
    open Azure.Data.Tables

    type SubscriptionType =
        | FinalDays
        | Other of string

    type SubscriptionRequest =
        { SubscriptionType: SubscriptionType
          ChannelId: ChannelId
          UserId: UserId
          GuildId: GuildId
          RequestedOn: DateTime }
        static member Create subType channelId userId guildId =
            { SubscriptionType = subType
              ChannelId = channelId
              UserId = userId
              GuildId = guildId
              RequestedOn = DateTime.UtcNow }

    let toTableEntity request =
        let subscription = Map.empty
        let entity = TableEntity subscription
        entity.PartitionKey <-
            match request.SubscriptionType with
            | FinalDays -> "finaldays"
            | Other s -> s.ToLower()
        entity.RowKey <- request.ChannelId.ToString()
        entity.Add ("UserId", request.UserId :> obj)
        entity.Add ("GuildId", request.GuildId :> obj)
        entity.Add("RequestedOn", request.RequestedOn :> obj)
        entity

    let fromTableEntity (entity: TableEntity) =
        let subType =
            match entity.PartitionKey with
            | "finaldays" -> FinalDays
            | s -> Other s
        let channelId = entity.RowKey |> uint64
        let userId =
            match entity.TryGetValue("UserId") with
            | false, _ -> 0UL
            | true, uid -> System.Convert.ToUInt64(uid)
        let guildId =
            match entity.TryGetValue("GuildId") with
            | false, _ -> 0UL
            | true, gid -> System.Convert.ToUInt64(gid)
        let requestedOn =
            entity.GetDateTimeOffset("RequestedOn")
            |> Option.ofNullable
            |> Option.map (fun dto -> dto.DateTime)
            |> Option.defaultValue DateTime.UtcNow

        { SubscriptionType = subType
          ChannelId = channelId
          UserId = userId
          GuildId = guildId
          RequestedOn = requestedOn }

    type SubscriptionRepository(tableClient) =
        let tablesService = Db.TablesService(tableClient, "subscriptions")
        member _.UpsertAsync(subEntity) = tablesService.UpsertAsync(subEntity)
        member _.List() = tablesService.List()

    type SubscriptionService(subRepo: SubscriptionRepository) =
        member _.Subscribe(subRequest) =
            subRequest
            |> toTableEntity
            |> subRepo.UpsertAsync
        member _.List() =
            task {
                let! subs = subRepo.List()
                return subs |> Seq.map fromTableEntity
            }
