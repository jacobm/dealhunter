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

(defn insert-search [user-id url search-term]
  (let [existing? (mc/any? "searches" {:user-id user-id :search-term search-term})]
    (println existing? user-id search-term)
    (if (not existing?)
      (mc/insert "searches" {:user-id user-id 
                            :search-term search-term
                            :last-seen url}))))


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



(defroutes app-routes
  (GET "/" [] (response/resource-response "index.html" {:root "public"}))

  ;; user monitors and position in stream
  (context "/:user-id/monitors" [user-id :as request]
           (GET "/" []
                (return-data request (user-feed (read-string user-id) (base-url request))))
           ;; [{:replace "last-seen-link" :value "http://localhost/feed/1234"}]
           (PATCH "/:monitor-id" [user-id monitor-id :as request] 
                  (return-data request (read-body request)))
           (POST "/" []
                 (insert-monitor (int (read-string user-id))
                                 (base-url request)
                                 (:search-term (read-body request))) "ok"))
  
  (route/resources "/")
  (route/not-found "Not Found"))

(def app
  (handler/site app-routes))
