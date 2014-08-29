(defproject web-client "0.1.0-SNAPSHOT"
  :description "FIXME: write description"
  :url "http://example.com/FIXME"
  :dependencies [[org.clojure/clojure "1.5.1"]
                 [compojure "1.1.6"]
                 [ring "1.2.2"]
                 [clout "1.2.0"]
                 [http-kit "2.1.16"]
                 [org.clojure/data.json "0.2.4"]
                 [com.novemberain/monger "1.7.0"]]
  :plugins [[lein-ring "0.8.10"]]
  :ring {:handler web-client.handler/app}
  :profiles
  {:dev {:dependencies [[javax.servlet/servlet-api "2.5"]
                        [ring-mock "0.1.5"]]}})
