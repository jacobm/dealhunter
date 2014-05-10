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
            [monger.operators :refer [$lte $gt]]
            [monger.query :refer :all]
            [clojure.data.json :as json])
  (:refer-clojure :exclude [sort find])
  (:import [org.bson.types ObjectId])
  (:gen-class))


;; === persistence ===

(mg/connect! { :host "127.0.0.1" :port 27017 })
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

(defn get-oldest [search-term]
  (first (with-collection "scrapes"
           (find {:search-term search-term})
           (sort (array-map :_id -1))
           (limit 1))))

(defn get-newest [search-term]
  (first (with-collection "scrapes"
           (find {:search-term search-term})
           (sort (array-map :_id 1))
           (limit 1))))

(defn get-step [id-str search-term]
  (let [id (ObjectId. id-str)
        next-current (with-collection "scrapes"
                           (find {:search-term search-term :_id {$lte id}})
                           (sort (array-map :_id -1))
                           (limit 2))
        previous (with-collection "scrapes"
               (find {:search-term search-term :_id {$gt id}})
               (sort (array-map :_id 1))
               (limit 1))]
    {:current (first next-current)
     :next (second next-current)
     :previous (first previous)}))

;; === web ===
(defn return-data [request data]
  (if (= "application/edn" (:content-type request))
    {:status 200
     :headers {"Content-Type" "application/edn"}
     :body (pr-str data)}
    {:status 200
     :headers {"Content-Type" "application/json"}
     :body (json/write-str data)}))

(defn read-post-body [request]
  (if (= "application/edn" (:content-type request))
    (read-string (slurp (:body request)))
    (json/read-str (:body request))))

(defn base-url [request]
  (str "http://" (get (:headers request) "host")))

(defn user-feed [user-id base-url]
  (let [monitors (get-monitors user-id)]
    {:user-id user-id
     :monitors (map #(str base-url "/feed/" (:search-term %)) monitors)}))

(defn feed-item [request id search-term]
  (let [{next :next prev :previous :as data} (get-step id search-term)
        feed-base-url (str (base-url request) "/feed/" search-term "/")
        item      {:entry (dissoc
                           (:current data)
                           :_id)}
        self-link {:rel "self" :href (str feed-base-url id) :nil false}
        next-link {:rel "next" :href (str feed-base-url (:_id next)) :nil (nil? (:_id next))}
        prev-link {:rel "prev" :href (str feed-base-url (:_id prev)) :nil (nil? (:_id prev))}]
  (let [links (map #(dissoc % :nil)
                     (filter #(not (:nil %)) [self-link next-link prev-link]))]
    (assoc item :links links))))


(defroutes app-routes
  (GET "/" [] "index")
  (POST "/user/:user-id" [user-id :as request]
        (insert-monitor (read-string user-id) (:search-term (read-post-body request))) "ok")
  (POST "/user-test" request (return-edn (read-post-body request)))
  (GET "/user/:user-id" [user-id] (return-edn (get-monitors (read-string user-id))))
  (GET "/search" {{name :name} :params} (return-edn (scraper/search-one name))))

(def app 
  (-> (handler/api app-routes)
      (reload/wrap-reload)))
;http://stackoverflow.com/questions/5584923/a-cors-post-request-works-from-plain-javascript-but-why-not-with-jquery
      ;(cors/wrap-cors :access-control-allow-origin "x-requested-with")))

(defn -main [& args]
  (let [port (Integer/parseInt (or (System/getenv "PORT") "3000"))]
    (run-server app {:port port :join? false})))
