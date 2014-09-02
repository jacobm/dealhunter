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

module ScraperWebSettings = 
    open System
    open ScraperCommon.Settings
    
    let rabbitConnectionString (environment :string) = 
        let rabbitUser = get "rabbit.user"
        let rabbitPassword = get "rabbit.password"
        match environment.ToLowerInvariant() with
        | "development" -> String.Format("amqp://{0}:{1}@localhost", rabbitUser, rabbitPassword)
        | "production" -> let host = getEnvironmentValue "RABBIT_PORT_5672_TCP_ADDR"
                          String.Format("amqp://{0}:{1}@{2}", rabbitUser, rabbitPassword, host)
        | _ -> raise (Exception("Unknown environment: " + environment))

    let postgreConnectionString (environment : string) =
        let user = get "postgre.user"
        let password = get "postgre.password"
        match environment.ToLowerInvariant() with
        | "development" -> String.Format("User ID={0};Password={1};Host=localhost;Port=5432;Database=Scraper;", user, password)
        | "production" -> let host = getEnvironmentValue "SCRAPERDB_PORT_5432_TCP_ADDR"
                          String.Format("User ID={0};Password={1};Host={2};Port=5432;Database=Scraper;", user, password, host)
        | _ -> raise (Exception("Unknown environment: " + environment))

module WebFeed = 
    open System
    open System.Threading
    open System.Text
    open Nancy
    open Nancy.Hosting.Self
    open ScraperCommon.Persistence
    open ScraperCommon.WebTypes
    open Newtonsoft.Json
    open JsonFeedConversion
    open ScraperCommon.Settings
    open EasyNetQ
    open ScraperCommon.ScraperTypes
    open FSharp.Data
    open Scrape
    open Nancy.TinyIoc
    open ScraperWebSettings

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
        
        let environment = get "environment"
        let rabbitConnectionString = rabbitConnectionString environment
        let connectionString = postgreConnectionString environment
        let bus = RabbitHutch.CreateBus(rabbitConnectionString)

        do 
            self.Post.["/{searchTerm}"] <- fun parameters ->
                let searchTerm = (parameters?searchTerm).ToString()
                bus.Publish<ScrapeRequest>({term = searchTerm })
                HttpStatusCode.OK :> obj

            self.Get.["search/{searchTerm}"] <- fun parameters -> 
                let searchTerm = (parameters?searchTerm).ToString()
                let url = new Uri("http://www.dba.dk/soeg?soeg=" + searchTerm + "&fra=privat")
                let firstPage = Http.RequestString(url.ToString())
                let listings = getListings (url, Page firstPage)
                (okResponse listings) :> obj

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
        
    type ScraperWebBootstrapper() = 
            inherit DefaultNancyBootstrapper()

            override self.RequestStartup(container : TinyIoCContainer, pipelines : Nancy.Bootstrapper.IPipelines, context : NancyContext) =
                pipelines.AfterRequest.AddItemToEndOfPipeline(fun (ctx : NancyContext) ->
                    ctx.Response.WithHeaders(("Access-Control-Allow-Origin", "*"),
                                             ("Access-Control-Allow-Methods", "POST,GET"),
                                             ("Access-Control-Allow-Headers", "Accept, Origin, Content-type")) |> ignore)

    [<EntryPoint>]
    let main args = 
        StaticConfiguration.DisableErrorTraces <- false
        //let nancyHost = new NancyHost(new Uri("http://localhost:8889/"), new Uri("http://127.0.0.1:8889/"))
        let port = get "port"
        let nancyHost = new NancyHost(new Uri(String.Format("http://localhost:{0}/", port)))
        nancyHost.Start()
        printfn "ready..."

        if Array.Exists(args, (fun s -> s.Equals("-d", StringComparison.CurrentCultureIgnoreCase))) then
            Thread.Sleep(Timeout.Infinite);
        else
            Console.ReadKey() |> ignore
        Thread.Sleep(Timeout.Infinite);
        nancyHost.Stop()
        0
