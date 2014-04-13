(ns dba-scraper.server
    (:use compojure.core
          org.httpkit.server)
  (:require [ring.middleware.reload :as reload]
            [compojure.handler :as handler]
            [ring.middleware.resource :as resources]
            [ring.middleware.cors :as cors]
            [ring.util.response :as response]
            [dba-scraper.core :as scraper])
  (:gen-class))

(defn return-edn [data]
  {:status 200
   :headers {"Content-Type" "application/edn"}
   :body (prn-str data)})
  
(defroutes app-routes
  (GET "/" [] "index")
  (GET "/:user-id" [user-id] (str "UserId " user-id))
  (GET "/search" {{name :name} :params} (return-edn (scraper/search-one name))))

(def app 
  (-> (handler/api app-routes)
      (reload/wrap-reload)
      (cors/wrap-cors :access-control-allow-origin "*")))

(defn -main [& args]
  (let [port (Integer/parseInt (or (System/getenv "PORT") "3000"))]
    (run-server app {:port port :join? false})))
