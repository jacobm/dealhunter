﻿namespace ScraperCommon

module ScraperTypes = 
    open System
    
    type SearchTerm = 
        | SearchTerm of string
    
    type DbaId = 
        | DbaId of string
    
    type Location = 
        { postcode : int
          city : string }
    
    type Listing = 
        { text : string
          thumbnail : Uri option
          price : int
          location : Location
          dbaLink : Uri
          dbaId : DbaId
          postedAt : DateTime }

module WebTypes = 
    open System
    open ScraperTypes
    
    type ListingWithId = 
        { id : Guid
          listing : Listing }
    
    type PreviousId = 
        | PreviousId of Guid
    
    type NextId = 
        | NextId of Guid
    
    type TipId = 
        | TipId of Guid
    
    type OldestId = 
        | OldestId of Guid
    
    type FeedStart = 
        | EmptyFeed
        | OneItemFeed of TipId
        | OldestAndTip of OldestId * TipId
    
    type FeedItem = 
        | NotFound
        | LonelyTip of ListingWithId
        | Tip of ListingWithId * PreviousId
        | BodyItem of ListingWithId * PreviousId * NextId
        | Oldest of ListingWithId * NextId

module Persistence = 
    open System
    open System.Data.SqlClient
    open Npgsql
    open NpgsqlTypes
    open Newtonsoft.Json
    open ScraperTypes
    open WebTypes
    
    let getConnection() = 
        let connection = 
            new NpgsqlConnection("User ID=scraper;Password=dingo;Host=localhost;Port=5432;Database=scrape;Pooling=true;")
        connection.Open()
        connection
    
    let dropTables() = 
        use connection = getConnection()
        let command = new NpgsqlCommand(@"drop table listings;")
        command.ExecuteNonQuery
    
    let createTables() = 
        use connection = getConnection()
        let command = new NpgsqlCommand(@"
            CREATE TABLE listings (
                id SERIAL PRIMARY KEY, 
                search_term text NOT NULL, 
                listing_id UUID NOT NULL,
                dba_id text UNIQUE NOT NULL, 
                data JSON NOT NULL);", connection)
        command.ExecuteNonQuery()
    
    let saveListing searchTerm (listing : Listing) = 
        let (SearchTerm term) = searchTerm
        let (DbaId dbaId) = listing.dbaId
        let json = JsonConvert.SerializeObject listing
        use connection = getConnection()
        use command = new NpgsqlCommand(@"
            INSERT INTO listings (search_term, listing_id, dba_id, data) 
            VALUES (@searchTerm, @listingId, @dbaId, @json)", connection)
        command.Parameters.AddWithValue("@json", json) |> ignore
        command.Parameters.AddWithValue("@searchTerm", term) |> ignore
        command.Parameters.AddWithValue("@dbaId", dbaId) |> ignore
        command.Parameters.AddWithValue("@listingId", Guid.NewGuid()) |> ignore
        command.ExecuteNonQuery()
    
    let findListing (listingId : Guid) : Listing option = 
        use connection = getConnection()
        use command = new NpgsqlCommand(@"SELECT data FROM listings WHERE listing_id = @listingId", connection)
        command.Parameters.AddWithValue("@listingId", NpgsqlDbType.Uuid, listingId) |> ignore
        use reader = command.ExecuteReader()
        match reader.Read() with
        | true -> Some(JsonConvert.DeserializeObject<Listing>(reader.GetString(0)))
        | _ -> None
    
    let findLatest (searchTerm : SearchTerm) : Listing option = 
        let (SearchTerm term) = searchTerm
        use connection = getConnection()
        use command = new NpgsqlCommand(@"
            SELECT data FROM listings 
            WHERE search_term = @searchTerm
            ORDER BY id DESC
            LIMIT 1", connection)
        command.Parameters.AddWithValue("@searchTerm", term) |> ignore
        use reader = command.ExecuteReader()
        match reader.Read() with
        | true -> Some(JsonConvert.DeserializeObject<Listing>(reader.GetString(0)))
        | _ -> None
    
    let find (searchTerm : SearchTerm) : Listing seq = 
        seq { 
            let (SearchTerm term) = searchTerm
            use connection = getConnection()
            use command = 
                new NpgsqlCommand(@"SELECT data FROM listings WHERE search_term = @searchTerm order by id", connection)
            command.Parameters.AddWithValue("@searchTerm", term) |> ignore
            use reader = command.ExecuteReader()
            while reader.Read() do
                yield JsonConvert.DeserializeObject<Listing>(reader.GetString(0))
        }
    
    let findFeedStart (searchTerm : SearchTerm) : FeedStart = 
        let (SearchTerm term) = searchTerm
        use connection = getConnection()
        use command = new NpgsqlCommand(@"
            SELECT listing_id FROM listings 
            WHERE search_term = @searchTerm 
            AND id = (SELECT MAX(id) FROM listings WHERE search_term = @searchTerm)
            OR id = (SELECT MIN(id) FROM listings WHERE search_term = @searchTerm)
            ORDER BY id DESC", connection)
        command.Parameters.AddWithValue("@searchTerm", term) |> ignore
        use reader = command.ExecuteReader()
        match reader.Read() with
        | false -> EmptyFeed
        | true -> 
            let tip = reader.GetGuid(0)
            match reader.Read() with
            | false -> OneItemFeed(TipId tip)
            | true -> OldestAndTip(OldestId(reader.GetGuid(0)), (TipId tip))
    
    let findFeedItem (searchTerm : SearchTerm) (itemId : Guid) : FeedItem = 
        let (SearchTerm term) = searchTerm
        use connection = getConnection()
        use command = new NpgsqlCommand(@"
            WITH linked_list AS (SELECT 
	                listing_id, 
	                data, 
	                LAG(listing_id) OVER (ORDER BY id) AS prev,
	                LEAD(listing_id) OVER (ORDER BY id) AS next
                 FROM listings)
            SELECT 
                    listing_id, 
	                data, 
	                prev,
	                next
            FROM linked_list WHERE listing_id = @itemId", connection)
        command.Parameters.AddWithValue("@searchTerm", term) |> ignore
        command.Parameters.AddWithValue("@itemId", NpgsqlDbType.Uuid, itemId) |> ignore
        use reader = command.ExecuteReader()
        match reader.Read() with
        | false -> NotFound
        | true -> 
            let id = reader.GetGuid(0)
            let listing = JsonConvert.DeserializeObject<Listing>(reader.GetString(1))
            
            let prev = 
                match reader.IsDBNull(2) with
                | true -> None
                | false -> Some(reader.GetGuid(2))
            
            let next = 
                match reader.IsDBNull(3) with
                | true -> None
                | false -> Some(reader.GetGuid(3))
            
            let item = 
                { id = id
                  listing = listing }
            
            match prev, next with
            | None, None -> LonelyTip item
            | Some p, None -> Tip(item, (PreviousId p))
            | Some p, Some n -> BodyItem(item, (PreviousId p), (NextId n))
            | None, Some n -> Oldest(item, (NextId n))
