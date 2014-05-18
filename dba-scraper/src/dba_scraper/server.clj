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
            [monger.operators :refer [$lte $gt $lt $gte]]
            [monger.query :refer :all]
            [clojure.string]
            [clojure.data.json :as json])
  (:refer-clojure :exclude [sort find])
  (:import [org.bson.types ObjectId])
  (:gen-class))


;; === persistence ===

(mg/connect! { :host "127.0.0.1" :port 27017 })
(set-db! (monger.core/get-db "dealhunter-dbascraper"))

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

(defn get-count [search-term]
  (mc/count "scrapes" {:search-term search-term}))

(defn get-next [id-str search-term]
  (if (clojure.string/blank? id-str)
    nil
    (let [id (ObjectId. id-str)]
      (first (with-collection "scrapes"
               (find {:search-term search-term :_id {$lt id}})
               (sort (array-map :_id -1))
               (limit 1))))))

(defn get-step [id-str search-term]
  (let [id (ObjectId. id-str)
        prev-current (with-collection "scrapes"
                           (find {:search-term search-term :_id {$gte id}})
                           (sort (array-map :_id 1))
                           (limit 2))
        next (get-next id-str search-term)]
    {:current (first prev-current)
     :previous (second prev-current)
     :next next}))

(defn feed-exists? [search-term]
  (mc/any? "scrapes" {:search-term search-term}))

;; === web ===
(defn return-data [request data]
  (if (= "application/edn" (:content-type request))
    {:status 200
     :headers {"Content-Type" "application/edn"
               "Access-Control-Allow-Origin:" "*"}
     :body (pr-str data)}
    {:status 200
     :headers {"Content-Type" "application/hal+json"
               "Access-Control-Allow-Origin:" "*"}
     :body (json/write-str data)}))

(defn read-body [request]
  (if (= "application/edn" (:content-type request))
    (read-string (slurp (:body request)))
    (json/read-str (:body request))))

(defn base-url [request]
  (str "http://" (get (:headers request) "host")))

(defn- feed-link [feed-base-url rel id]
  {:rel rel :href (str feed-base-url id) :nil (nil? id)})

; oldest -> next -> next -> newest (newest has not next)
; newest -> prev -> prev -> oldest (oldest has no prev)

(defn build-links [& links]
  (println links)
  (apply merge
   (->> links
        (filter #(not (:nil %)))
        (map #(hash-map (keyword (:rel %)) {:href (:href %)})))))

(defn feed-start [base-url search-term]
  (let [newest (get-newest search-term)
        oldest (get-oldest search-term)
        count (get-count search-term)
        next (get-next (str (:_id oldest)) search-term)
        feed-base-url (str base-url "/feed/" search-term "/")
        item (assoc (dissoc oldest :_id) :count count)
        self-link  (feed-link feed-base-url "self" (:_id oldest))
        first-link (feed-link feed-base-url "first" (:_id oldest))
        last-link  (feed-link feed-base-url "last" (:_id newest))
        next-link  (feed-link feed-base-url "next" (:_id next))]
      (assoc item :_links (build-links self-link next-link first-link last-link))))

(defn feed-item [base-url id search-term]
  (let [{next :next prev :previous :as data} (get-step id search-term)
        feed-base-url (str base-url "/feed/" search-term "/")
        item {:_embedded (dissoc (:current data) :_id)}
        self-link (feed-link feed-base-url "self" id)
        next-link (feed-link feed-base-url "next" (:_id next))
        prev-link (feed-link feed-base-url "prev" (:_id prev))]
    (assoc item :_links (build-links self-link next-link prev-link))))

(defroutes app-routes
  ;; test stuff
  (GET "/" request (do
                     (println request)
                     (return-data request {:fisk "fisk" :hest [1 2 3]})))
  (GET "/dingo" request (base-url request))

  ;; streams
  (context "/feed/:search-term" [search-term :as request]
           (GET "/" []
                (if-not (feed-exists? search-term)
                  (response/not-found "Feed was not found")
                  (return-data request (feed-start (base-url request) search-term))))
           (POST "/" []
                 (do (upsert-scrape search-term)
                     (return-data request "ok")))
           (GET "/:item-id" [item-id]
                (return-data request (feed-item (base-url request) item-id search-term))))

  (GET "/search" request
       (let [name (:name (:params request))]
         (do
           (scraper/search name (get-newest-id name))
           (return-data request
                        (map #(dissoc % :_id)
                                     (mc/find-maps "scrapes" {:search-term name}))))))
  (GET "/search-one" request
       (return-data request  (scraper/search-one (:name (:params request))))))

(def app 
  (-> (handler/api app-routes)
      (reload/wrap-reload)))

(defn -main [& args]
  (let [port (Integer/parseInt (or (System/getenv "PORT") "3000"))]
    (run-server app {:port port :join? false})))
