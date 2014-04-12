(ns dba-scraper.core-test
  (:use [midje.sweet]
        [dba-scraper.core])
  (:require [clojure.string :as str]
            [clj-time.core :as time]
            [clj-time.format :as format]))

(defn dingo-search []
  (read-string (slurp "./test/dba_scraper/dingo-search.txt")))

(fact "Expected number of items is found"
  (count (get-items (dingo-search))) => 13)

(fact "Extraction works as expected for first item"
  (let [node (extract (first (get-listings (dingo-search))))]
    (:date node) => "I dag"
    (:headline node) => ""
    (:link node) => "http://www.dba.dk/ballerinasko-str-findes-i/id-93107954/"
    (:location node) => "1000 København K"
    (:price node) => "Kr. 507"
    (:thumbnail node) => "http://dbastatic.dk/pictures/pictures/admanager/6b/11/f276-bcf9-4b69-a30d-0a3eba572d9a.jpg?preset=srpgallery"
    (:thumbnail-alt node) => nil
    (:text node) => ""
    (:text-alt node) => "Ballerinasko, str. findes i flere str., Aster Dingo Kid, piger, Ballerinaer til børn Aster  DINGO KID  Marineblå Fås i pigestørrelse. 28,29,30,31,32. . Barn &gt; Pige &gt; Sko &gt; Ballerinaer.\n\nGratis levering ved køb over 300 kr. + 30 dages gratis returnering på Spartoo.dk\n\nSpartoo er specialist i salg af mærkevaresko på internettet..."))

(fact "First item is read in as expected"
  (let [item (first (get-items (dingo-search)))]
    (:date item) => (str (time/today))
    (:headline item) => ""
    (:link item) => "http://www.dba.dk/ballerinasko-str-findes-i/id-93107954/"
    (:location item) => "1000 København K"
    (:price item) => 507
    (:thumbnail item) => "http://dbastatic.dk/pictures/pictures/admanager/6b/11/f276-bcf9-4b69-a30d-0a3eba572d9a.jpg?preset=srpgallery"
    (:text item) => "Ballerinasko, str. findes i flere str., Aster Dingo Kid, piger, Ballerinaer til børn Aster  DINGO KID  Marineblå Fås i pigestørrelse. 28,29,30,31,32. . Barn &gt; Pige &gt; Sko &gt; Ballerinaer.\n\nGratis levering ved køb over 300 kr. + 30 dages gratis returnering på Spartoo.dk\n\nSpartoo er specialist i salg af mærkevaresko på internettet..."))

