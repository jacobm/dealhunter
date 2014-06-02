(ns web-client.googleoauth
  (:require [org.httpkit.client :as http]
            [clojure.data.json :as json]))


(def oauth-uri "https://accounts.google.com/o/oauth2/token")
(def people-url "https://www.googleapis.com/plus/v1/people/");

(def google-com-oauth2
    {:redirect-uri "postmessage"
     :client-id "108491861456-g9oajn3u17m0dc6e0fu1o8phoeju2v1d.apps.googleusercontent.com"
     :client-secret "SECRET"
     :access-query-param :access_token
     :scope ["https://www.googleapis.com/auth/plus.login"]
     :grant-type "authorization_code"
     :access-type "online"
     :approval_prompt ""})

;https://developers.google.com/accounts/docs/OAuth2WebServer

(defn is-google-login-ok? [google-id code]
  (let [options {:form-params {"code" code
                               "client_id" (:client-id google-com-oauth2)
                               "client_secret" (:client-secret google-com-oauth2)
                               "redirect_uri" (:redirect-uri google-com-oauth2) 
                               "grant_type" "authorization_code"}
                 :headers {"Content-Type" "application/x-www-form-urlencoded"}}
        {:keys [status headers body error] :as resp} @(http/post oauth-uri options)]
    (if (= status 200)
      (let [token (json/read-json body true)
            me-url (str people-url google-id "?access_token=" (:access_token token))
            {:keys [status headers body error] :as resp} @(http/get me-url)]
        (if (= status 200)
          (= google-id (:id (json/read-json body true)))
          false))
      false)))
        
    
    







  
  
