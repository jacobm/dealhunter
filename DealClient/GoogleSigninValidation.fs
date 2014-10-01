namespace DealClient

module SigninValidation =
    open System
    open System.Net
    open System.Text
    open System.IO
    open Newtonsoft.Json
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
        result.sub
