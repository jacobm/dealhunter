(ns web-client.handler
  (:use compojure.core)
  (:require [ring.util.response :as response]
            [ring.middleware.session :as session]
            [clout.core :as clout]
            [compojure.handler :as handler]
            [compojure.route :as route]
            [clojure.data.json :as json]
            [monger.core :as mg :refer [connect! set-db! get-db]]
            [monger.collection :as mc :refer [insert insert-batch]]
            [monger.operators :refer [$lte $gt $lt $gte]]
            [monger.query :as mq]
            [web-client.googleoauth :as oauth]))

;; === configuration ===
(def feed-server "http://localhost:3000")


;; === persistence ===
(mg/connect! { :host "127.0.0.1" :port 27017 })
(set-db! (monger.core/get-db "dealhunter-client"))

(defn get-searches [user-id]
  (map #(->
         (assoc % :id (str (:_id %)))
         (dissoc % :_id))
       (mc/find-maps "searches" {:user-id user-id})))

(defn append-search [user-id search-term]
  (mc/insert "searches" {:user-id user-id 
                         :search-term search-term}))

(defn set-search-position [user-id search-term url]
  (mc/insert "positions" {:user-id user-id
                          :search-term search-term
                          :position url}))

;; === web ===
(defn read-body [request]
  (if (= "application/edn" (:content-type request))
    (read-string (slurp (:body request)))
    (json/read-str (:body request))))

(defn base-url [request]
  (str "http://" (get (:headers request) "host")))

(defn return-data 
  ([request data] (return-data request data nil))
  ([request data session]
     (let [reply
           (if (= "application/edn" (:content-type request))
             {:status 200
              :headers {"Content-Type" "application/edn"}
              :body (pr-str data)}
             {:status 200
              :headers {"Content-Type" "application/json"}
              :body (json/write-str data)})]
       (if (not (nil? session))
         (assoc reply :session session)
         reply))))
  
(defn return-json [data]
  {:status 200
   :headers {"Content-Type" "application/json"}
   :body (json/write-str data)})

(defn user-feed [user-id base-url]
  (let [monitors (get-searches user-id)]
    {:user-id user-id
     :monitors (map #(identity {:link (str feed-server "/feed/" (:search-term %))
                                :search-term (:search-term %)}) monitors)}))


(defn wrap-userid-access [handler]
  (fn [request]
    (let [path-user (clout/route-matches "/:user-id/searches*" request)
          session-user-id (:user-id (:session request))]
      (if (not= (:user-id path-user) session-user-id)
        {:status 401
         :body "Access denied"}
        (handler request)))))

(defroutes user-routes
  (context "/:user-id/searches" [user-id :as request]
           (GET "/" []
                (return-data request (get-searches (read-string user-id))))
           (POST "/:search-term" [search-term]
                 (append-search (int (read-string user-id)) search-term) "ok")
           (POST "/:search-term/:url" [search-term url :as request] 
                  (return-data request (read-body request)))))


(defroutes app-routes
  (GET "/" [] (response/resource-response "index.html" {:root "public"}))

  (POST "/login" [googleId code :as request] 
        (return-data request (oauth/is-google-login-ok? googleId code) {:user-id googleId}))

  (GET "/session" [params :as request]
       (return-data request (:session request))))

(defroutes static-routes  
  (route/resources "/")
  (route/not-found "Not Found"))
  

(def app
  (-> (handler/site 
       (routes 
        app-routes
        (context "/user" [] (wrap-userid-access user-routes))
        static-routes))
      session/wrap-session))
