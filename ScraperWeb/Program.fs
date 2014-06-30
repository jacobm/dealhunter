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

module WebFeed = 
    open System
    open System.Text
    open Nancy
    open Nancy.Hosting.Self
    open ScraperCommon.Persistence
    open ScraperCommon.ScraperTypes
    open ScraperCommon.WebTypes
    open Newtonsoft.Json
    open JsonFeedConversion
    
    let (?) (parameters : obj) param = (parameters :?> Nancy.DynamicDictionary).[param]
    let serialize item = 
        JsonConvert.SerializeObject(item, Formatting.Indented, new FeedConverter(), new DbaIdConverter())
    
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
    
    type HelloModule() as self = 
        inherit NancyModule()
        
        do 
            self.Get.["/{searchTerm}"] <- fun parameters -> 
                let searchTerm = (parameters?searchTerm).ToString()
                let feedBaseUrl = (sprintf "%s/%s" self.Request.Url.SiteBase searchTerm)
                
                let selfLink = 
                    { linkName = "self"
                      href = feedBaseUrl }
                
                let link = self.GetLink feedBaseUrl
                
                let feed = 
                    match (SearchTerm searchTerm) |> findFeedStart with
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
                        let feedItem = findFeedItem (SearchTerm searchTerm) id.Value
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
                response :> obj
        
        member self.GetLink feedBaseUrl (name : string) (id : Guid) : HalLink = 
            { linkName = name
              href = sprintf "%s/%s" feedBaseUrl (id.ToString()) }
    
    let buildDb() = 
        dropTables() |> ignore
        let create = createTables()
        
        let result = 
            saveListing (SearchTerm "stokke") { text = "testing like a boss"
                                                dbaId = (DbaId "12gf")
                                                thumbnail = Some(new Uri("http://localhost/image"))
                                                price = 200
                                                dbaLink = new Uri("http://localhost/link")
                                                location = 
                                                    { postcode = 2300
                                                      city = "Amager" }
                                                postedAt = DateTime.Now }
        
        let res = find (SearchTerm "stokke")
        let fisk = findListing (Guid.Parse("6426fe91-0964-4d22-831c-abcf8551a811"))
        ()
    
    [<EntryPoint>]
    let main args = 
        let item = findFeedStart (SearchTerm "stokke")
        let feed2 = findFeedStart (SearchTerm "notfound")
        let id = Guid.Parse("5231ab0c-b231-472f-9412-806a57da74df")
        let oldestItem = findFeedItem (SearchTerm "stokke") id
        let id = Guid.Parse("e14dfbbf-354d-4183-981d-a0c6db8e12c4")
        let bodyItem = findFeedItem (SearchTerm "stokke") id
        let id = Guid.Parse("21f06d23-cd46-4240-9445-c62f5fd4a93d")
        let tipItem = findFeedItem (SearchTerm "stokke") id
        let latest = findLatest (SearchTerm "stokke")
        let nancyHost = new NancyHost(new Uri("http://localhost:8888/"), new Uri("http://127.0.0.1:8888/"))
        nancyHost.Start()
        printfn "ready..."
        Console.ReadKey()
        nancyHost.Stop()
        0
