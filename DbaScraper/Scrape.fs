module Scrape

open System
open System.Text.RegularExpressions
open AgilityWrapper
open ScraperCommon.ScraperTypes
open System.Net

type Html = | Page of string

type ScrapeError = 
    { node : HtmlAgilityPack.HtmlNode
      rawHtml : string
      message : string }

///Match the pattern using a cached compiled Regex
let (|CompiledMatch|_|) pattern input = 
    if input = null then None
    else 
        let m = Regex.Match(input, pattern, RegexOptions.Compiled)
        if m.Success then 
            Some [ for x in m.Groups -> x ]
        else None

let parsePrice (value : string) : int option = 
    match value with
    | CompiledMatch @"-?(?!0)((?:\d+|\d{1,3}(?:\.\d{3})+))\s*(kr.)" [ all; price; kr ] -> 
        Some(Int32.Parse(price.Value.Replace(".", "")))
    | _ -> None

let parseLocation (loc : string) : Location option = 
    match loc with
    | CompiledMatch @"\s*(\d+)\s*(\w+)\s*" [ all; postcode; city ] -> 
        Some({ postcode = (Int32.Parse postcode.Value)
               city = city.Value })
    | _ -> None

let parseDbaId (url : string) : string option = 
    match url with
    | CompiledMatch @"\w+/id-(\d+)" [ all; id ] -> Some id.Value
    | _ -> None

let parseDbaDate (date : string) : (int * string) option = 
    match date with
    | CompiledMatch @"\s*(\d+).\s*(\w+)\s*" [ all; day; month ] -> Some(Int32.Parse(day.Value), month.Value)
    | _ -> None

let parseDate (value : string) : DateTime = 
    let guessDate s = 
        match parseDbaDate s with
        | Some(day, month) -> 
            let dateString = sprintf "%d %s %d" day month DateTime.Now.Year
            DateTime.ParseExact(dateString, "d MMM yyyy",  new Globalization.CultureInfo("da-DK"))
        | None -> DateTime.MinValue
    match value.Trim() with
    | "I dag" -> DateTime.Today.Date
    | "I går" -> DateTime.Today.AddDays(-1.0).Date
    | date -> guessDate date

let getThumbnail (item : HtmlAgilityPack.HtmlNode) : Uri option = 
    let images = 
        item
        |> descendants "td"
        |> Seq.filter (hasClass "pictureColumn")
        |> Seq.head
        |> descendants "img"
    match Seq.toList images with
    | [] -> None
    | _ -> 
        let url = 
            images
            |> Seq.head
            |> attr "data-original"
        Some(new Uri(url))

let getPrice (item : HtmlAgilityPack.HtmlNode) : Choice<int, ScrapeError> = 
    let result = 
        item
        |> descendants "td"
        |> Seq.filter (hasAttr "title" "Pris")
        |> Seq.head
        |> innerText
    match result |> parsePrice with
    | None -> 
        Choice2Of2 { node = item
                     rawHtml = result
                     message = "Not found" }
    | Some price -> Choice1Of2 price

let getText (item : HtmlAgilityPack.HtmlNode) : string = 
    item
    |> descendants "td"
    |> Seq.filter (hasClass "mainContent")
    |> Seq.head
    |> descendants "a"
    |> Seq.filter (hasClass "listingLink")
    |> Seq.last
    |> innerText
    |> WebUtility.HtmlDecode

let getLocation (item : HtmlAgilityPack.HtmlNode) : Location option = 
    item
    |> descendants "td"
    |> Seq.filter (hasClass "mainContent")
    |> Seq.head
    |> descendants "ul"
    |> Seq.head
    |> descendants "li"
    |> Seq.nth 1
    |> descendants "a"
    |> Seq.head
    |> innerText
    |> WebUtility.HtmlDecode
    |> parseLocation

let getDbaLink (item : HtmlAgilityPack.HtmlNode) : string = 
    item
    |> descendants "td"
    |> Seq.filter (hasClass "mainContent")
    |> Seq.head
    |> descendants "a"
    |> Seq.filter (hasClass "listingLink")
    |> Seq.head
    |> attr "href"

let getDate (item : HtmlAgilityPack.HtmlNode) : DateTime = 
    item
    |> descendants "td"
    |> Seq.filter (hasAttr "title" "Dato")
    |> Seq.head
    |> innerText
    |> WebUtility.HtmlDecode
    |> parseDate

let toListing (item : HtmlAgilityPack.HtmlNode) = 
    let thumbnail = getThumbnail item
    
    let price = 
        match getPrice item with
        | Choice1Of2 amount -> amount
        | Choice2Of2 error -> 
            let node = error.node
            let html = error.rawHtml
            -1
    
    let text = getText item
    let location = getLocation item
    let dbaLink = getDbaLink item
    let dbaId = parseDbaId dbaLink
    let createdAt = getDate item
    { text = text
      thumbnail = thumbnail
      price = price
      location = location.Value
      dbaLink = new Uri(dbaLink)
      dbaId = (DbaId dbaId.Value)
      postedAt = createdAt }

let getListings (item : Uri * Html) = 
    let result = 
        match item with
        | (_, Page scrape) -> 
            createDoc scrape
            |> descendants "table"
            |> Seq.filter (hasClass "searchResults srpListView")
            |> Seq.head
            |> descendants "tr"
            |> Seq.filter 
                   (fun x -> 
                   (hasClass "dbaListing listing      " x) || (hasClass "dbaListing listing     lastListing " x))
    
    let listings = Seq.map toListing result
    listings

let getNumberOfPages page = 
    let html = 
        match page with
        | Page it -> 
            createDoc it
            |> descendants "ul"
            |> Seq.filter (hasClass "pager right")
    if Seq.isEmpty html then 0
    else 
        let secondLast = 
            html
            |> Seq.head
            |> descendants "li"
            |> Seq.toList
            |> List.rev
            |> Seq.nth 1
        secondLast
        |> element "span"
        |> innerText
        |> Int32.Parse
