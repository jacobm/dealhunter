module Robot

open System
open System.Threading
open System.Globalization
open FSharp.Data
open ScraperCommon
open ScraperCommon.ScraperTypes

module Logging = 
    open Riemann

    let log (client : Client) service state description metric  =     
        client.SendEvent(service, state, description, metric)
    
    let logStatus (client : Client) service description =
        log client service "status" description 1.0f

module Crawler = 
    open Scrape

    let getFrontPageUrl (searchTerm : string) : Uri = 
        new Uri("http://www.dba.dk/soeg?soeg=" + searchTerm + "&fra=privat")
    let getPageUrl (searchTerm : string) (pageNumber : int) : Uri = 
        new Uri("http://www.dba.dk/soeg/side-" + (string pageNumber) + " /?soeg=" + searchTerm + "&fra=privat")
 
    let fetch (url : Uri) = 
        Console.WriteLine("Fetching " + url.ToString())
        let result =  Page (Http.RequestString(url.ToString()))
        (url, result)

    let crawl (SearchTerm searchTerm) : (Uri * Html) seq =
        let frontPageUrl = getFrontPageUrl searchTerm
        seq {
            let front = frontPageUrl |> fetch
            let numberOfPages = Scrape.getNumberOfPages (snd front)
            yield front 
            for x in 2..numberOfPages do
                let page = x |> getPageUrl searchTerm
                yield page |> fetch 
        }

    let fixupDates (listings : Listing seq) : Listing list = 
        let update it listings year =
            let item = { it with postedAt = new DateTime(year, it.postedAt.Month, it.postedAt.Day) }
            (item::listings, item.postedAt, year)

        let assignMonths (acc : Listing list * DateTime * int) (it : Listing) = 
            let listings, currentDate, year = acc
            match currentDate.Month.CompareTo(it.postedAt.Month) with
            | 1 -> update it listings year
            | 0 -> match currentDate.Day < it.postedAt.Day with
                   | false -> update it listings year
                   | true -> update it listings (year - 1)
            | _ -> update it listings (year - 1)

        let lastDayOfYear = DateTime.ParseExact(sprintf "31-12-%i" DateTime.Now.Year, "dd-MM-yyyy", CultureInfo.CurrentCulture)
        Console.WriteLine(lastDayOfYear)

        let listings, _, _ = Seq.fold assignMonths (List.empty, lastDayOfYear, DateTime.Now.Year) listings
        listings |> List.rev
    
    let extractListings pages dbaId =
        let pageListings = pages |> Seq.map getListings |> Seq.concat
        let listings = 
            match dbaId with
            | None -> pageListings
            | Some latest -> pageListings |> Seq.takeWhile (fun x -> x.dbaId <> latest)

        listings
        |> Seq.distinctBy (fun it -> it.dbaId) // fix for paging duplicates
        |> fixupDates

    let importUntilDbaId (searchTerm : SearchTerm) dbaId = 
        extractListings (crawl searchTerm) dbaId

    let import (searchTerm, dbaId) =
        (searchTerm, extractListings (crawl searchTerm) dbaId)
    

module FetcherRobot = 
    open System
    open System.Collections
    open System.ServiceProcess
    open Crawler
    open Settings
    open Riemann
    open EasyNetQ
    open Logging
    open Settings
    open ScraperCommon.ScraperTypes

    let receiveSeachRequest request : SearchTerm =
        SearchTerm request

    let findCurrent connectionString (searchTerm : SearchTerm) =
        match Persistence.findLatest connectionString searchTerm with
        | None -> (searchTerm, None)
        | Some it -> (searchTerm, Some it.dbaId)

    let persistListings connectionString (searchTerm : SearchTerm, listings : Listing seq) =
        let saveItem item = Persistence.saveListing connectionString searchTerm item |> ignore
        listings |> Seq.iter saveItem

    let listen connectionString handleFunc : unit =
        use bus = RabbitHutch.CreateBus(connectionString)
        bus.Subscribe<ScrapeRequest>("Scraper", handleFunc) |> ignore
        Console.WriteLine("Press any key to exit")
        Console.ReadKey() |> ignore
        while true do
            Thread.Sleep(1000)
        

        

    let start unit : int =     
        // make sure Okt is parsed as October
        Thread.CurrentThread.CurrentCulture <- new CultureInfo("da-DK")
        Console.WriteLine("Starting")
        let env = Environment.GetEnvironmentVariables()
        
        for entry in env |> Seq.cast<DictionaryEntry> do
            let line = entry.Key.ToString() + ": " + entry.Value.ToString()
            Console.WriteLine(line)

        let rabbitHost = getEnvValue "RABBIT_PORT_5672_TCP_ADDR" "localhost"
        let rabbitConnectionString = "amqp://guest:guest@" + rabbitHost
        
        let postgreHost = getEnvValue "SCRAPERDB_PORT_5432_TCP_ADDR" "localhost"
        let postgreConnectionString = getEnvValue "postgre" "User ID=Scraper;Password=dingo;Host=" + postgreHost + ";Port=5432;Database=Scraper;"
        
        let riemannHost = getEnvValue "RIEMANN_PORT_5555_UDP_ADDR" "localhost"
        
        Console.WriteLine(rabbitConnectionString)
        Console.WriteLine(postgreConnectionString)
        Console.WriteLine(riemannHost)
                
        let client = new Client(riemannHost, 5555, true)
        let log = Logging.logStatus client "Scraper"
        
        log "Starting up scraper"
        
        use bus = RabbitHutch.CreateBus(rabbitConnectionString)
        bus.Publish<ScrapeRequest>({term = "bliss" })
        
        
        log ("Rabbit: " + rabbitConnectionString) 
        log ("Postgre: " + postgreConnectionString) 

        let logCurrent item  : SearchTerm * DbaId option =
            let it = match item with
                     | (SearchTerm _, None) -> log "Fresh import"
                     | (SearchTerm _, Some (DbaId x)) -> log ("Importing from " + x)
            item

        let logImportedItems (SearchTerm term, listings) =
            match listings with
            | [] -> log "no new items"
            | _ -> listings |> List.iter (fun it -> log (term + ": " + it.text)) 
            (SearchTerm term, listings)
    
        let ignoreSaveError saveFunc =
            let save (searchTerm : SearchTerm, listings : Listing list) =
                try
                    saveFunc (searchTerm, listings)
                with :? System.Exception -> log "possible double import, ignoring this import"
            save

        listen rabbitConnectionString
            (Action<ScrapeRequest> (fun request -> 
                                 log ("Received " + request.term)

                                 let saveFunc = persistListings postgreConnectionString

                                 request.term
                                 |> receiveSeachRequest 
                                 |> findCurrent postgreConnectionString
                                 |> logCurrent
                                 |> import 
                                 |> logImportedItems
                                 |> ignoreSaveError saveFunc
                                 log "import finished" ))
   
        Console.ReadKey() |> ignore
      
        0 // return an integer exit code
    
    type ScraperService() = 
        inherit ServiceBase(ServiceName = "ScraperService")

        override self.OnStart (args : string[]) =
            self.CanShutdown <- true
            self.CanStop <- true
            self.CanPauseAndContinue <- false
            start() |> ignore
            ()

        override self.OnStop() =
            ()

        override self.OnShutdown() = 
            self.OnStop()

    [<EntryPoint>]
    let main argv = 
        start()

