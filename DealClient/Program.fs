
module Site  =
    open System
    open System.Threading
    open System.IO
    open Nancy
    open Nancy.Hosting.Self
    open Nancy.TinyIoc

    type DealClient() as self = 
        inherit NancyModule()

        do 
            self.Get.["/"] <- fun parameters ->
                //self.View.["index.html"] :> obj
                self.Response.AsFile("Content/index.html", "text/html") :> obj

        type DartClientBootstrapper() as self = 
            inherit DefaultNancyBootstrapper()

            let writeFileToStream stream filename =
                if filename = "/" then
                    use fileStream = File.OpenRead("../../Content/web/dartclient.html")
                    fileStream.CopyTo(stream)
                else
                    let file = "../../Content/web/" + filename
                    if File.Exists file then
                        use fileStream = File.OpenRead(file)
                        fileStream.CopyTo(stream)
                    else
                        ()

            override self.ApplicationStartup(container : TinyIoCContainer, pipelines : Nancy.Bootstrapper.IPipelines) =
                self.Conventions.StaticContentsConventions.Insert(0, fun context name ->
                    match context.Request.Method, not (context.Request.Path.ToLowerInvariant().StartsWith("/api")) with
                    | "GET", true -> 
                            let response = new Response()
                            response.Contents <- Action<IO.Stream> (fun stream -> writeFileToStream stream context.Request.Path)
                            response
                    | _ -> null)

    [<EntryPoint>]
    let main args = 
        StaticConfiguration.DisableErrorTraces <- false
        StaticConfiguration.Caching.EnableRuntimeViewUpdates <- true
        //let nancyHost = new NancyHost(new Uri("http://localhost:8889/"), new Uri("http://127.0.0.1:8889/"))
        let nancyHost = new NancyHost(new Uri("http://localhost:4000/"))
        nancyHost.Start()
        printfn "ready..."

        if Array.Exists(args, (fun s -> s.Equals("-d", StringComparison.CurrentCultureIgnoreCase))) then
            Thread.Sleep(Timeout.Infinite);
        else
            Console.ReadKey() |> ignore
        Thread.Sleep(Timeout.Infinite);
        nancyHost.Stop()
        0
