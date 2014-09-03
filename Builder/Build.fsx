
#r @"../packages/FAKE/tools/FakeLib.dll"

open System
open System.IO
open System.Diagnostics
open Fake
open Fake.ConfigurationHelper
open Fake.FileHelper

let buildDir = "./build/"
let scraperWebName = "ScraperWeb"
let scraperRobotName = "DbaScraper"
let dealClientName = "DealClient"

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

let compileFSharpProject (name :string) =
    !! ("./" + name + "/**/*.fsproj")
      |> MSBuildRelease (buildDir + name + "/bin") "Build"
      |> Log "AppBuild-Output: "

let execute cmd args = 
    let workTime = (TimeSpan.FromMinutes((float)1.0f))
    let exeInfo (info : ProcessStartInfo) : unit = 
        info.FileName <- cmd
        info.Arguments <- args
    let result = ProcessHelper.ExecProcessAndReturnMessages exeInfo workTime
    if result.OK then trace(String.Join("\n", result.Messages))
    else trace (cmd + " " + args + ": " + (String.Join("\n", result.Errors)))
         trace (cmd + " " + args + ": " + (String.Join("\n", result.Messages)))
         raise (Exception ("process exit"))

// Targets
Target "Clean" (fun _ ->
    //CleanDir buildDir <<= follows symbolic links and delete contents,
    // does not play well with darts packages links...
    execute "rm" "-rf ./build"
)

// DbaScraper robot
Target "DbaScraper.Compile" (fun _ ->
    compileFSharpProject scraperRobotName
)

Target "DbaScraper.BuildDockerImage" (fun _ ->
    buildDocker "DbaScraper" "jacobm/dba-scraper"
)

Target "DbaScraper" (fun _ -> 
    trace "Done"
)

// ScraperWeb
Target "ScraperWeb.Compile" (fun _ ->
    compileFSharpProject scraperWebName
)

Target "ScraperWeb.SetProductionConfig" (fun _ ->
    let scraperBuild = (buildDir + scraperWebName + "/bin")
    updateAppSetting "environment" "production" (scraperBuild @@ "/ScraperWeb.exe.config")
    updateAppSetting "port" "8888" (scraperBuild @@ "/ScraperWeb.exe.config")
)

Target "ScraperWeb.BuildDockerImage" (fun _ ->
    buildDocker "ScraperWeb" "jacobm/scraperweb"
)

Target "ScraperWeb" (fun _ ->
    trace "Done"
)

// DealClient    
Target "DealClient.Dart.CheckInstallation" (fun _ ->
    match tryFindFileOnPath "dart2js" with
    | None -> raise (Exception "dart2js compile not found on PATH")
    | Some path -> trace ("Found dart2js at " + path)
    match tryFindFileOnPath "pub" with
    | None -> raise (Exception "dart pub not found on PATH")
    | Some path -> trace ("Found dart pub at " + path)
)

Target "DealClient.Compile" (fun _ ->
    compileFSharpProject "DealClient"
)

Target "DealClient.Dart" (fun _ -> 
    let cwd = Directory.GetCurrentDirectory()
    let clientDir = ("./build/" + dealClientName + "/bin/Content")
    try
        FileHelper.CopyDir clientDir  "./DealClient/Content" (fun _ -> true)

        Directory.SetCurrentDirectory(clientDir)
        execute "pub" "get"
        Directory.SetCurrentDirectory(cwd)

        Directory.SetCurrentDirectory(clientDir + "/web")
        execute "dart2js" "--minify --package-root=packages --out=dartclient.js dartclient.dart"
    finally
        Directory.SetCurrentDirectory(cwd)
    ()
)

Target "DealClient" (fun _ ->
    trace "Done"
)

Target "Default" (fun _ -> 
    trace "Done")

"Clean" 
    ==> "DbaScraper.Compile"
    ==> "DbaScraper.BuildDockerImage"
    ==> "DbaScraper"

"Clean" 
    ==> "ScraperWeb.Compile"
    ==> "ScraperWeb.SetProductionConfig"
    ==> "ScraperWeb.BuildDockerImage"
    ==> "ScraperWeb"

"Clean"
    ==> "DealClient.Dart.CheckInstallation"
    ==> "DealClient.Compile"
    ==> "DealClient.Dart"
    ==> "DealClient"

"Clean" 
    ==> "DbaScraper"
    ==> "ScraperWeb"
    ==> "Default"    

RunTargetOrDefault "Default"