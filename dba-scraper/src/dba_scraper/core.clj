(ns dba-scraper.core
  (:require [net.cgrand.enlive-html :as html]
            [clj-time.core :as time]
            [clj-time.format :as format]
            [clojure.string :as s]))

(def listings-selector [:table.searchResults :> :tbody :tr.dbaListing])
(def date-selector [(html/attr-has :title "Dato")])
(def price-selector [(html/attr-has :title "Pris")])
(def link-selector [:td.mainContent :a.listingLink])
(def headline-selector [:td.mainContent :span.headline])
(def text-selector [:td.mainContent :span.text])
(def text-selector-alt [:td.mainContent (html/nth-child 2) :a.listingLink])
(def location-selector [:td.mainContent :ul (html/nth-child 2) :span :a])
(def thumbnail-selector [:a.thumbnailContainerInner :img])
(def thumbnail-selector-alt [:div.casthumbnailContainerInner :img])
(def pager-size-selector [:ul.pager (html/nth-last-child 2) :a])

(defn- get-number-of-pages [page]
  (let [path (:href (:attrs (first (html/select page pager-size-selector))))
        number-of-pages (re-find #"\d+" (if (nil? path) "0" path))]
    (if (nil? number-of-pages)
      0
      (Integer. number-of-pages))))

(defn extract [node]
  (let [date  (s/trim (html/text (first (html/select node date-selector))))
        price (s/trim (first (:content (first (html/select node price-selector)))))
        link (get-in (first (html/select node link-selector)) [:attrs :href])
        headline (html/text (first (html/select node headline-selector)))
        text (html/text (first (html/select node text-selector)))
        text-alt (html/text (first (html/select node text-selector-alt)))
        location (s/trim (html/text (first (html/select node location-selector))))
        image (:data-original (:attrs (first (html/select node thumbnail-selector))))
        image-alt (:src (:attrs (first (html/select node thumbnail-selector-alt))))]
   {:date date 
    :price price
    :link link
    :headline headline
    :text text
    :text-alt text-alt
    :location location
    :thumbnail image
    :thumbnail-alt image-alt}))
 
(defn- get-price [item]
  (let [number (second (s/split (:price item) #"\s+"))]
    (if (nil? number)
      0
      (read-string (s/replace number "." "")))))

(def custom-formatter (format/formatter "dd. MMM"))

(defn- translate-date [date]
  (cond
      (.contains date "maj") (s/replace date #"maj" "may")
      (.contains date "okt") (s/replace date #"okt" "oct")
      :else date))
      
(defn- get-date [item]
  (let [today (time/today)]
  (case (:date item)
        "I dag" today
        "I går" (time/minus today (time/days 1))
        (let [parsed (format/parse custom-formatter (translate-date (:date item)))
              res (time/date-time (time/year today) (time/month parsed) (time/day parsed))]
          res))))

(defn- transform [item]
  (dissoc
   (assoc item 
     :price (get-price item) 
     :text (if (s/blank? (-> (:text item)
                             (s/trim-newline)
                             (s/trim)))
             (:text-alt item)
             (:text item))
     :thumbnail (if (nil? (:thumbnail item))
                  (:thumbnail-alt item)
                  (:thumbnail item))
     :date (str (get-date item)))
  :thumbnail-alt :text-alt))

(defn get-listings-first-page [page]
  (let [listings (html/select page listings-selector)]
        (concat
         (take-while #(not (nil? 
                           (re-find #"cpcListing" (:class (:attrs %))))) listings)
         (filter #(nil? (re-find #"cpcListing" (:class (:attrs %)))) listings))))

(defn get-listings [page]
  (let [listings (html/select page listings-selector)]
    (filter #(nil? (re-find #"cpcListing" (:class (:attrs %)))) listings)))

(defn get-items [page]
  (map #(transform (extract %)) (get-listings page)))

(defn get-items-first-page [page]
  (map #(transform (extract %)) (get-listings-first-page page)))

(defn- fetch-url [url]
  (html/html-resource (java.net.URL. url)))

(def dba-host "http://www.dba.dk")
(def ^:dynamic *base-url* "http://www.dba.dk/soeg/?soeg=dingo")
(def page (fetch-url *base-url*))

(defn prune-items [items last-known-link]
  (take-while #(not= (:link %) last-known-link) items))

(defn search [search-term last-known-link]
  (let [first-page (fetch-url (str dba-host "/soeg?soeg=" search-term))
        number-of-pages (get-number-of-pages first-page)
        items (get-items-first-page first-page)]
    (if (some #{last-known-link} (map #(:link %) items))
      (prune-items items last-known-link)
      (loop [it items page-number 2]
        (if (< number-of-pages page-number)
          it
          (let [url (str dba-host "/soeg/side-" page-number "?soeg=" search-term)
                page-items (get-items (fetch-url url))]
            (if (some #{last-known-link} (map #(:link %) it))
              (prune-items (into it page-items) last-known-link)
              (recur (into it page-items) (+ page-number 1)))))))))
