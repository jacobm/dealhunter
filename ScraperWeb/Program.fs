namespace WebScrape

open ScraperCommon.ScraperTypes

type HalLink = 
    { linkName : string
      href : string }

type HalItem = 
    { _links : HalLink seq
      _embedded : Listing }

type Feed = 
    { _links : HalLink seq }

module JsonFeedConversion = 
    open System
    open Newtonsoft.Json
    open Newtonsoft.Json.Linq
    
    type FeedConverter() = 
        inherit JsonConverter()
        
        override self.WriteJson((writer : JsonWriter), (value : obj), (serializer : JsonSerializer)) : unit = 
            let item = value :?> HalLink
            let o = new JObject()
            o.AddFirst(new JProperty(item.linkName, new JObject(new JProperty("href", item.href))))
            o.WriteTo(writer)
            ()
        
        override self.ReadJson((reader : JsonReader), (objectType : Type), (existingValue : obj), 
                               (serializer : JsonSerializer)) : obj = raise (Exception("Cannot read"))
        override self.CanConvert(objectType : Type) = objectType = typedefof<HalLink>
        override self.CanRead = false
    
    type DbaIdConverter() = 
        inherit JsonConverter()
        
        override self.WriteJson((writer : JsonWriter), (value : obj), (serializer : JsonSerializer)) : unit = 
            let item = value :?> DbaId
            let (DbaId id) = item
            writer.WriteValue(id)
            ()
        
        override self.ReadJson((reader : JsonReader), (objectType : Type), (existingValue : obj), 
                               (serializer : JsonSerializer)) : obj = raise (Exception("Cannot read"))
        override self.CanConvert(objectType : Type) = objectType = typedefof<DbaId>
        override self.CanRead = false

    type UriOptionConverter() =
        inherit JsonConverter()

        override self.WriteJson((writer : JsonWriter), (value : obj), (serializer : JsonSerializer)) : unit =
            let item = value :?> Uri option
            match item with
            | None -> writer.WriteValue("null");
            | Some url -> writer.WriteValue(url.ToString())
            ()

        override self.ReadJson((reader : JsonReader), (objectType : Type), (existingValue : obj),
                               (serializer : JsonSerializer)) : obj = raise (Exception("Cannot read"))
        override self.CanConvert(objectType : Type) = objectType.IsGenericType && objectType.GetGenericTypeDefinition() = typedefof<option<Uri>>
        override self.CanRead = false

module WebFeed = 
    open System
    open System.Threading
    open System.Text
    open Nancy
    open Nancy.Hosting.Self
    open ScraperCommon.Persistence
    open ScraperCommon.ScraperTypes
    open ScraperCommon.WebTypes
    open Newtonsoft.Json
    open JsonFeedConversion
    open ScraperCommon.Settings
    open EasyNetQ
    open ScraperCommon.ScraperTypes
    open FSharp.Data
    open Scrape

    let (?) (parameters : obj) param = (parameters :?> Nancy.DynamicDictionary).[param]
    let serialize item = 
        JsonConvert.SerializeObject(item, Formatting.Indented, new FeedConverter(), new DbaIdConverter(), new UriOptionConverter())
    
    let notFoundResponse() = 
        let resp = new Response()
        resp.StatusCode <- HttpStatusCode.NotFound
        resp.ContentType <- "application/json"
        resp
    
    let okResponse body = 
        let data = Encoding.UTF8.GetBytes(serialize body)
        let resp = new Response()
        resp.StatusCode <- HttpStatusCode.OK
        resp.ContentType <- "application/json"
        resp.Contents <- fun s -> s.Write(data, 0, data.Length)
        resp
    
    let errorResponse errorCode (reason : string) = 
        let message = sprintf "{\"error\": \"%s\"}" reason
        let data = Encoding.UTF8.GetBytes(message)
        let resp = new Response()
        resp.StatusCode <- errorCode
        resp.ContentType <- "application/json"
        resp.Contents <- fun s -> s.Write(data, 0, data.Length)
        resp
    
    type FeedModule() as self = 
        inherit NancyModule()
        
        let postgreHost = getEnvValue "SCRAPERDB_PORT_5432_TCP_ADDR" "localhost"
        let connectionString = getEnvValue "postgre" "User ID=Scraper;Password=dingo;Host=" + postgreHost + ";Port=5432;Database=Scraper;"
        let rabbitHost = getEnvValue "RABBIT_PORT_5672_TCP_ADDR" "localhost"
        let rabbitConnectionString = "amqp://guest:guest@" + rabbitHost
        let bus = RabbitHutch.CreateBus(rabbitConnectionString)


        do 
            self.Post.["/{searchTerm}"] <- fun parameters ->
                let searchTerm = (parameters?searchTerm).ToString()
                bus.Publish<ScrapeRequest>({term = searchTerm })
                HttpStatusCode.OK :> obj

            self.Get.["/{searchTerm}"] <- fun parameters -> 
                let searchTerm = (parameters?searchTerm).ToString()
                let feedBaseUrl = (sprintf "%s/%s" self.Request.Url.SiteBase searchTerm)
                
                let selfLink = 
                    { linkName = "self"
                      href = feedBaseUrl }
                
                let link = self.GetLink feedBaseUrl
                
                let feed = 
                    match (SearchTerm searchTerm) |> findFeedStart connectionString with
                    | EmptyFeed -> { _links = [| selfLink |] }
                    | OneItemFeed(TipId id) -> 
                        { _links = 
                              [| selfLink
                                 link "next" id |] }
                    | OldestAndTip(OldestId o, TipId t) -> 
                        { _links = 
                              [| selfLink
                                 link "tip" t
                                 link "oldest" o |] }
                okResponse feed :> obj
            self.Get.["/{searchTerm}/{itemId}"] <- fun parameters -> 
                let searchTerm = (parameters?searchTerm).ToString()
                let itemId = (parameters?itemId).ToString()
                let feedBaseUrl = (sprintf "%s/%s" self.Request.Url.SiteBase searchTerm)
                let link = self.GetLink feedBaseUrl
                
                let feedLink = 
                    { linkName = "prev"
                      href = feedBaseUrl }
                
                let id = ref Guid.Empty
                
                let response = 
                    match Guid.TryParse(itemId, id) with
                    | false -> errorResponse HttpStatusCode.BadRequest "Invalid listing id"
                    | true -> 
                        let feedItem = findFeedItem connectionString (SearchTerm searchTerm) id.Value
                        match feedItem with
                        | NotFound -> notFoundResponse()
                        | LonelyTip item -> 
                            { _links = 
                                  [| link "self" item.id
                                     feedLink |]
                              _embedded = item.listing }
                            |> okResponse
                        | Tip(item, (PreviousId p)) -> 
                            { _links = 
                                  [| link "self" item.id
                                     link "prev" p |]
                              _embedded = item.listing }
                            |> okResponse
                        | BodyItem(item, (PreviousId p), (NextId n)) -> 
                            { _links = 
                                  [| link "self" item.id
                                     link "prev" p
                                     link "next" n |]
                              _embedded = item.listing }
                            |> okResponse
                        | Oldest(item, (NextId n)) -> 
                            { _links = 
                                  [| link "self" item.id
                                     link "next" n |]
                              _embedded = item.listing }
                            |> okResponse
                okResponse response :> obj
        
        member self.GetLink feedBaseUrl (name : string) (id : Guid) : HalLink = 
            { linkName = name
              href = sprintf "%s/%s" feedBaseUrl (id.ToString()) }
    
    let buildDb connectionString = 
        dropTables connectionString |> ignore
        let create = createTables connectionString
        ()
        

    [<EntryPoint>]
    let main args = 
        StaticConfiguration.DisableErrorTraces <- false
        //let nancyHost = new NancyHost(new Uri("http://localhost:8889/"), new Uri("http://127.0.0.1:8889/"))
        let nancyHost = new NancyHost(new Uri("http://localhost:8888/"))
        nancyHost.Start()
        printfn "ready..."

        if Array.Exists(args, (fun s -> s.Equals("-d", StringComparison.CurrentCultureIgnoreCase))) then
            Thread.Sleep(Timeout.Infinite);
        else
            Console.ReadKey() |> ignore
        Thread.Sleep(Timeout.Infinite);
        nancyHost.Stop()
        0
