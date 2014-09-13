
module Site  =
    open System
    open System.Text
    open System.Threading
    open System.IO
    open Nancy
    open Nancy.Hosting.Self
    open Nancy.TinyIoc
    open Newtonsoft.Json

    // fixme: move to common place
    let (?) (parameters : obj) param = (parameters :?> Nancy.DynamicDictionary).[param]

    type codePostItem = {code: string}

    type DealClient() as self = 
        inherit NancyModule()

        do 
            self.Post.["/api/login"] <- fun _ ->
                use reader = new StreamReader(self.Request.Body)
                let token = JsonConvert.DeserializeObject<codePostItem>(reader.ReadToEnd())
                true :> obj

        type DartClientBootstrapper() =
            inherit DefaultNancyBootstrapper()

            let writeFileToStream stream filename =
                if filename = "/" then
                    use fileStream = File.OpenRead("../../Content/web/dartclient.html")
                    fileStream.CopyTo(stream)
                else
                    let file = "../../Content/web/" + filename
                    use fileStream = File.OpenRead(file)
                    fileStream.CopyTo(stream)

            override self.ApplicationStartup(container : TinyIoCContainer, pipelines : Nancy.Bootstrapper.IPipelines) =
                self.Conventions.StaticContentsConventions.Insert(0, fun context name ->
                    match context.Request.Method, not (context.Request.Path.ToLowerInvariant().StartsWith("/api")) with
                    | "GET", true -> 
                            let response = new Response()
                            if context.Request.Path = "/"
                            then
                               response.Contents <- Action<IO.Stream> (fun stream -> writeFileToStream stream context.Request.Path)
                            else if (File.Exists  ("../../Content/web/" + context.Request.Path)) then
                               response.Contents <- Action<IO.Stream> (fun stream -> writeFileToStream stream context.Request.Path)
                               if context.Request.Path.EndsWith("css") then response.ContentType <- "text/css"
                            else
                                let content = Encoding.UTF8.GetBytes("404 - Page not found");
                                response.Contents <- Action<IO.Stream> (fun stream -> stream.Write(content, 0, content.Length))
                                response.StatusCode <- HttpStatusCode.NotFound
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
