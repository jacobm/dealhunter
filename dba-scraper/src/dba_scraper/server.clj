(ns dba-scraper.server
    (:use compojure.core
          org.httpkit.server)
  (:require [ring.middleware.reload :as reload]
            [compojure.handler :as handler]
            [ring.middleware.resource :as resources]
            [ring.middleware.cors :as cors]
            [ring.util.response :as response]
            [dba-scraper.core :as scraper]
            [monger.core :as mg :refer [connect! set-db! get-db]]
            [monger.collection :as mc :refer [insert insert-batch]]
            [monger.query :refer :all])
  (:refer-clojure :exclude [sort find])
  (:import [org.bson.types ObjectId])
  (:gen-class))


;; === persistence ===

(mg/connect! { :host "127.0.0.1" :port 49153 })
(set-db! (monger.core/get-db "monger-test"))

(defn upsert-scrape [search-term]
  (let [newest-item (first (with-collection "scrapes"
                             (find {:search-term search-term})
                             (sort (array-map :_id 1))
                             (limit 1)))
        new-items (scraper/search search-term (:dba-id newest-item))]
    (println "Newest scrape item from mongo: " (:dba-id newest-item))
    (if (nil? (:dba-id newest-item))
      (mc/insert-batch "scrapes" (map #(assoc % :search-term search-term) new-items))
      ;; scraping is not stable: may hit server that does not have the newest-item yet
      ;; loop until known item is met
      (loop [items new-items]
        (let [current (first items)
              mongo-current (mc/find-one-as-map "scrapes" {:dba-id (:dba-id current)})]
          (if (nil? mongo-current)
            (do
              (mc/insert "scrapes" (assoc current :search-term search-term))
              (recur (rest items)))))))))

(defn get-newest-id [search-term]
  (let [newest-scrape-item (first (with-collection "scrapes"
                                    (find {:search-term search-term})
                                    (sort (array-map :_id 1))
                                    (limit 1)))]
        (:dba-id newest-scrape-item)))

(defn insert-monitor [user-id search-term]
  (let [existing (mc/find-one-as-map "monitor" {:user-ud user-id :search-term search-term})]
    (if (nil? existing)
      (mc/insert "monitor" {:user-id user-id :search-term search-term}))
    (upsert-scrape search-term)))

(defn get-monitors [user-id]
  (map #(->
         (assoc % :id (str (:_id %)))
         (dissoc % :_id))
       (mc/find-maps "monitor" {:user-id user-id})))

(defn return-edn [data]
  {:status 200
   :headers {"Content-Type" "application/edn"}
   :body (prn-str data)})

(defn read-post-body [request]
  (read-string (slurp (:body request))))
  
(defroutes app-routes
  (GET "/" [] "index")
  (POST "/user/:user-id" [user-id :as request]
        (println (read-post-body request)) user-id)
  (POST "/user-test" request (return-edn (read-post-body request)))
  (GET "/user/:user-id" [user-id] (str "UserId " user-id))
  (GET "/search" {{name :name} :params} (return-edn (scraper/search-one name))))

(def app 
  (-> (handler/api app-routes)
      (reload/wrap-reload)))
;http://stackoverflow.com/questions/5584923/a-cors-post-request-works-from-plain-javascript-but-why-not-with-jquery
      ;(cors/wrap-cors :access-control-allow-origin "x-requested-with")))

(defn -main [& args]
  (let [port (Integer/parseInt (or (System/getenv "PORT") "3000"))]
    (run-server app {:port port :join? false})))
