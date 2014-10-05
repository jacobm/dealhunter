namespace DealClient

module DealClientSettings =
    open System
    open ScraperCommon.Settings

    let postgreConnectionString (environment : string) =
        let user = get "postgre.user"
        let password = get "postgre.password"
        match environment.ToLowerInvariant() with
        | "development" -> String.Format("User ID={0};Password={1};Host=localhost;Port=5432;Database=UserState;", user, password)
        | "production" -> let host = getEnvironmentValue "DEALCLIENTDB_PORT_5432_TCP_ADDR"
                          String.Format("User ID={0};Password={1};Host={2};Port=5432;Database=UserState;", user, password, host)
        | _ -> raise (Exception("Unknown environment: " + environment))

module Site  =
    open System
    open System.Text
    open System.Threading
    open System.IO
    open Nancy
    open Nancy.Hosting.Self
    open Nancy.TinyIoc
    open Nancy.Session
    open Newtonsoft.Json
    open SigninValidation
    open ScraperCommon.Settings
    open DealClientDomain

    // fixme: move to common place
    let (?) (parameters : obj) param = (parameters :?> Nancy.DynamicDictionary).[param]

    let setUserIdInSession (session : ISession) (googleId : GoogleId option) =
         session.["GoogleId"] <- googleId

    let getUserIdFromSession (session : ISession) =
        session.["GoogleId"]  :?> (GoogleId option)
        
    let okResponse body = 
        let data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body))
        let resp = new Response()
        resp.StatusCode <- HttpStatusCode.OK
        resp.ContentType <- "application/json;charset=utf-8"
        resp.Contents <- fun s -> s.Write(data, 0, data.Length)
        resp

    // server understood the request, but declined to fulfill it.
    let respond403 (reason : string) =
        let data = Encoding.UTF8.GetBytes(reason)
        let resp = new Response()
        resp.StatusCode <- HttpStatusCode.OK
        resp.ContentType <- "application/json;charset=utf-8"
        resp.Contents <- fun s -> s.Write(data, 0, data.Length)
        resp


    type PostWatch = {term: Term}
    type PostSetPosition = {position: string}
    type State = {term : Term; position: string}

    type DealClient() as self = 
        inherit NancyModule()

        do 
            let postgre = DealClientSettings.postgreConnectionString (get "environment")
            let watchTerm = startWatching (Persistence.getEvents postgre) (Persistence.saveEvent postgre)
            let setTermPos = setTermPosition (Persistence.getEvents postgre) (Persistence.saveEvent postgre)

            self.Post.["/api/login"] <- fun _ ->
                use reader = new StreamReader(self.Request.Body)
                let codeRequest = JsonConvert.DeserializeObject<CodePostItem>(reader.ReadToEnd())
                let googleId = Id (getUserId codeRequest.code)
                setUserIdInSession self.Session (Some googleId)
                let events = Persistence.getEvents postgre googleId
                buildState events |> okResponse :> obj

            self.Get.["/api/watches"] <- fun _ ->
                let googleId = getUserIdFromSession self.Session
                match googleId with
                | None -> HttpStatusCode.Unauthorized :> obj
                | Some id -> let events = Persistence.getEvents postgre id
                             let state = buildState events
                             okResponse state :> obj

            self.Post.["/api/watches"] <- fun _ ->
                let googleId = getUserIdFromSession self.Session
                match googleId with
                | None -> HttpStatusCode.Unauthorized :> obj
                | Some id ->
                    use reader = new StreamReader(self.Request.Body)
                    let watch = JsonConvert.DeserializeObject<PostWatch>(reader.ReadToEnd())
                    let result = watchTerm id watch.term
                    match result with
                    | Success -> okResponse "success" :> obj
                    | Failure reason -> respond403 reason :> obj

            self.Post.["/api/watches/{term}"] <- fun parameters ->
                let term = (parameters?term).ToString()
                let googleId = getUserIdFromSession self.Session
                match googleId with
                | None -> HttpStatusCode.Unauthorized :> obj
                | Some id ->
                    use reader = new StreamReader(self.Request.Body)
                    let newPosition = JsonConvert.DeserializeObject<PostSetPosition>(reader.ReadToEnd())
                    let result = setTermPos id term newPosition.position
                    match result with
                    | Success -> okResponse "success" :> obj
                    | Failure reason -> respond403 reason :> obj


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

                CookieBasedSessions.Enable(pipelines) |> ignore

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
