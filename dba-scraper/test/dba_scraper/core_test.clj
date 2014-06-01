(ns dba-scraper.core-test
  (:use [midje.sweet]
        [dba-scraper.core])
  (:require 
   [net.cgrand.enlive-html :as html]
   [clojure.string :as str]
   [clj-time.core :as time]
   [clj-time.format :as format]))

(defn dingo-search []
  (html/html-resource (java.io.StringReader. (slurp "./test/dba_scraper/dingo-search.txt"))))

(fact "Expected number of items is found"
  (count (get-items (dingo-search))) => 14)

(fact "Extraction works as expected for first item"
  (let [node (extract (first (get-listings (dingo-search))))]
    (:date node) => "7. maj"
    (:headline node) => ""
    (:link node) => "http://www.dba.dk/gummibaad-fabriksny-brig/id-92405123/"
    (:location node) => "5700 Svendborg"
    (:price node) => "6.900 kr."
    (:thumbnail node) => "http://dbastatic.dk/pictures/pictures/admanager/ac/fe/5513-8a99-49f1-a40d-a720736eca5e.jpg?preset=srpgallery"
    (:thumbnail-alt node) => nil
    (:text node) => ""
    (:text-alt node) => "Gummibåd, Fabriksny - Brig D265 Dingo - Kampagnepriser!, Beskrivelse\nTilbud med ny Yamaha F-2,5AMHS kr. 12.900,- / Yamaha F-4BMHS kr. 14.900,- / Yamaha F-5AMHS kr. 18.500,- og med Yamaha F-6CMHS kr. 19.500,-.\n\nVi har gjort en rigtig god deal med Brig fabrikken og har derfor disse gode tilbud til vore kunder.\nBrig 265D har trædørk og oppustelig køl 2+1 luftkamre og er en super kvalitets gummibåd. Den er hurtig at adskille og putte væk i stuverum eller bagagerummet i bilen fylder den..."))

(fact "First item is read in as expected"
  (let [item (first (get-items (dingo-search)))]
    (:date item) =>  "2014-05-07T00:00:00.000Z"
    (:headline item) => ""
    (:link item) => "http://www.dba.dk/gummibaad-fabriksny-brig/id-92405123/"
    (:location item) => "5700 Svendborg"
    (:price item) => 6900
    (:thumbnail item) => "http://dbastatic.dk/pictures/pictures/admanager/ac/fe/5513-8a99-49f1-a40d-a720736eca5e.jpg?preset=srpgallery"
    (:text item) => "Gummibåd, Fabriksny - Brig D265 Dingo - Kampagnepriser!, Beskrivelse\nTilbud med ny Yamaha F-2,5AMHS kr. 12.900,- / Yamaha F-4BMHS kr. 14.900,- / Yamaha F-5AMHS kr. 18.500,- og med Yamaha F-6CMHS kr. 19.500,-.\n\nVi har gjort en rigtig god deal med Brig fabrikken og har derfor disse gode tilbud til vore kunder.\nBrig 265D har trædørk og oppustelig køl 2+1 luftkamre og er en super kvalitets gummibåd. Den er hurtig at adskille og putte væk i stuverum eller bagagerummet i bilen fylder den..."))

