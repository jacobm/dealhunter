(defproject dba-scraper "0.1.0-SNAPSHOT"
  :description "Deal tool for wife."
  :url "http://example.com/FIXME"
  :license {:name "Eclipse Public License"
            :url "http://www.eclipse.org/legal/epl-v10.html"}
  :dependencies [[org.clojure/clojure "1.5.1"]
                 [clj-time "0.6.0"]
                 [http-kit "2.1.16"]
                 [enlive "1.1.5"]
                 [compojure "1.1.6"]
                 [ring "1.2.2"]
                 [ring-cors "0.1.0"]
                 [org.clojure/data.json "0.2.4"]
                 [com.novemberain/monger "1.7.0"]]
  :plugins [[lein-midje "3.1.3"]
            [lein-ring "0.8.10"]]
  :ring {:handler dba-scraper.server/app}
  :profiles {:dev {:dependencies [[midje "1.6.3"]]}})
