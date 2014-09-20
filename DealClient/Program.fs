
module DealClientTypes =
    type TermPosition = { term : string; position : string }
    type UserData = { positions : TermPosition list }
    type GoogleId = Id of string

module SigninValidation =
    open System
    open System.Net
    open System.Text
    open System.IO
    open Newtonsoft.Json
    open DealClientTypes
    open ScraperCommon.Settings

    type CodePostItem = {code: string}
    type GoogleTokenResponse = { access_token: string; token_type: string; experies_in: int; id_token: string }
    type IdToken = { sub: string; email: string  }

    let clientId = get "google.clientId"
    let clientSecret = get "google.clientSecret"

    let getToken code =
        let request = HttpWebRequest.Create("https://accounts.google.com/o/oauth2/token")
        request.Method <- "POST"
        let clientId = clientId
        let clientSecret = clientSecret
        let content = String.Format("grant_type=authorization_code&code={0}&client_id={1}&client_secret={2}&redirect_uri=postmessage",
                                        code, clientId, clientSecret)
        let postBody = Encoding.ASCII.GetBytes(content)
        request.ContentType <- "application/x-www-form-urlencoded"
        request.ContentLength <- int64 postBody.Length
        use reqStream = request.GetRequestStream()
        reqStream.Write(postBody, 0, postBody.Length)
        let response = request.GetResponse()
        let stream = response.GetResponseStream()
        let reader = new StreamReader(stream)
        let responseString = reader.ReadToEnd()
        JsonConvert.DeserializeObject<GoogleTokenResponse>(responseString)

    let base64UrlDecode (arg : string) =
        let s = arg.Replace('-', '+').Replace('_', '/')
        let result = match s.Length % 4 with
                     | 0 -> s
                     | 2 -> s + "=="
                     | 3 -> s + "="
                     | _ -> raise (Exception("Illegal base64url string"))
        Convert.FromBase64String(result)

    let parseIdToken token =
        let parts = token.id_token.Split('.')
        let data = Encoding.UTF8.GetString(base64UrlDecode parts.[1])
        JsonConvert.DeserializeObject<IdToken>(data)

    let getUserId code =
        let token = getToken code
        let result = parseIdToken token
        (Id result.sub)

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
