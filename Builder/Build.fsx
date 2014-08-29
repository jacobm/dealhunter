
#r @"../packages/FAKE.3.3.0/tools/FakeLib.dll"

open System
open System.IO
open System.Diagnostics
open Fake

let buildDir = "./build/"
let scraperWebName = "ScraperWeb"
let scraperRobotName = "DbaScraper"



let buildDockerImage path tagname = 
    let args = "build -t " + tagname + " " + path
    trace ("docker " + args)
    
    let p = new Process()
    p.StartInfo.FileName <- "docker"
    p.StartInfo.Arguments <- "build -t " + tagname + " " + path
    p.StartInfo.UseShellExecute <- false
    p.StartInfo.RedirectStandardOutput <- true
    p.Start() |> ignore

    let output = p.StandardOutput.ReadToEnd()
    p.WaitForExit()
    trace output

let buildDocker name tagName =
    let dir = buildDir + name
    let dockerfileName = name + "_Dockerfile"
    trace ("copying " + dockerfileName + " to " + dir)
    File.Copy("Builder/" + dockerfileName, dir + "/Dockerfile", true)
    buildDockerImage dir tagName

// Targets
Target "Clean" (fun _ ->
    CleanDir buildDir
)

Target "DbaScraper" (fun _ ->
    !! "./DbaScraper/**/*.fsproj"
      |> MSBuildRelease (buildDir + scraperRobotName + "/bin") "Build"
      |> Log "AppBuild-Output: "
)

Target "ScraperWeb" (fun _ ->
    !! "./ScraperWeb/**/*.fsproj"
      |> MSBuildRelease (buildDir + scraperWebName + "/bin") "Build"
      |> Log "AppBuild-Output: "
)

Target "BuildDbaScraperImage" (fun _ ->
    buildDocker "DbaScraper" "jacobm/dba-scraper"
)

Target "BuildScraperWebImage" (fun _ ->
    buildDocker "ScraperWeb" "jacobm/scraperweb"
)

Target "Default" (fun _ -> 
    trace "Done")

"Clean" 
    ==> "DbaScraper"
    ==> "ScraperWeb"
    ==> "BuildDbaScraperImage"
    ==> "BuildScraperWebImage"
    ==> "Default"

RunTargetOrDefault "Default"