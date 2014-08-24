module Robot

open System
open System.Threading
open System.Globalization
open FSharp.Data
open System.Net
open System.Net.Cache
open ScraperCommon
open ScraperCommon.ScraperTypes
open Scrape

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
            //let front = frontPageUrl |> fetch
            //let numberOfPages = Scrape.getNumberOfPages (snd front)
            //yield front 
            for x in 46..50 do
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
    

module FetcherRobot = 
    open Crawler

    
    let partialImport (searchTerm : SearchTerm) : unit = 
        let item = Persistence.find searchTerm |> Seq.head
        let temp = importUntilDbaId searchTerm (Some item.dbaId) |> Seq.toList
        Console.WriteLine("her-------------")
        Seq.iter (fun x -> 
            let (DbaId id) = x.dbaId
            Console.WriteLine(id)) temp
        Seq.iter (fun item -> Persistence.saveListing searchTerm item |> ignore) temp
        ()
    
    [<EntryPoint>]
    let main argv = 
        // make sure Okt is parsed as October
        Thread.CurrentThread.CurrentCulture <- new CultureInfo("da-DK")

        let searchTerm = (SearchTerm "stokke")
        let dbaId = Some (DbaId "1008630748")
        let import = importUntilDbaId searchTerm None
        import |> List.iter (fun item -> Console.WriteLine(item.postedAt))
        0 // return an integer exit code
