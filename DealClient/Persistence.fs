namespace DealClient

module Persistence =
    open Npgsql
    open Newtonsoft.Json
    open DealClientDomain

    let getConnection (connectionString : string) =
        let connection =
            new NpgsqlConnection(connectionString)
        connection.Open()
        connection

    let dropTables connectionString =
        use connection = getConnection connectionString
        let command = new NpgsqlCommand(@"drop table userdata;", connection)
        command.ExecuteNonQuery

    let createTables connectionString =
        use connection = getConnection connectionString
        let command = new NpgsqlCommand(@"
            create table userdata (id serial primary key, googleId numeric(25) not null, data json not null);", connection)
        command.ExecuteNonQuery() |> ignore
        let termCommand = new NpgsqlCommand(@"
            create table term_events (
                id serial primary key,
                googleId numeric(25) not null,
                term varchar(25) not null,
                createdAt timestamp not null,
                event json not null);", connection)
        termCommand.ExecuteNonQuery() |> ignore

    let saveEvent connectionString (googleId : GoogleId) (term : Term) (event : UserEvent) =
        let (Id id) = googleId
        let data = JsonConvert.SerializeObject(event)
        use connection = getConnection connectionString
        let command = new NpgsqlCommand(@"
            insert into term_events (googleId, term, createdAt, event)
            values (@id, @term, current_timestamp(), @event)", connection)
        command.Parameters.AddWithValue("@id", id) |> ignore
        command.Parameters.AddWithValue("@term", term) |> ignore
        command.Parameters.AddWithValue("@event", data) |> ignore
        command.ExecuteNonQuery() |> ignore

    let getEvents connectionString (googleId : GoogleId) =
        let (Id id) = googleId
        use connection = getConnection connectionString
        let command = new NpgsqlCommand(@"select data from userdata where googleId = @id", connection)
        command.Parameters.AddWithValue("@id", id) |> ignore
        seq {
            use reader = command.ExecuteReader()
            while reader.Read() do
                yield JsonConvert.DeserializeObject<UserEvent>(reader.GetString(0))
        }
          
    let getUserData connectionString (googleId : GoogleId) =
        let (Id id) = googleId
        use connection = getConnection connectionString
        let command = new NpgsqlCommand(@"select data from userdata where googleId = @id", connection)
        command.Parameters.AddWithValue("@id", id) |> ignore
        use reader = command.ExecuteReader()
        match reader.Read() with
        | true -> Some(JsonConvert.DeserializeObject<UserData>(reader.GetString(0)))
        | _ -> None

    let setUserData connectionString (googleId : GoogleId) userData =
        let (Id id) = googleId
        let data = JsonConvert.SerializeObject(userData)
        use connection = getConnection connectionString
        let command = new NpgsqlCommand(@"
             WITH upsert AS (
                 UPDATE userdata SET data = @data
                 WHERE googleId = @id RETURNING *)
             INSERT INTO userdata (googleId, data) SELECT @id, @data
             WHERE NOT EXISTS (SELECT * FROM upsert)
            ", connection)
        command.Parameters.AddWithValue("@id", id) |> ignore
        command.Parameters.AddWithValue("@data", data) |> ignore
        let result = command.ExecuteNonQuery()
        ()
