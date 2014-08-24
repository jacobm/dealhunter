./cluster/kubecfg.sh -alsologtostderr=true -verbose=true -c ~/src/dealhunter/deployment/scraperdb.json create pods

./cluster/kubecfg.sh -alsologtostderr=true -verbose=true -c ~/src/dealhunter/deployment/scraperdb-service.json create services

./cluster/kubecfg.sh -alsologtostderr=true -verbose=true -c ~/src/dealhunter/deployment/infrastructure.json create pods

./cluster/kubecfg.sh -alsologtostderr=true -verbose=true -c ~/src/dealhunter/deployment/dbascraper.json create pods



xbuild /p:Configuration=Release ../DbaScraper.sln
docker build -t jacobm/dba-scraper .


# Raw docker
docker run -d -p 5432:5432 --name scraperdb postgres
docker run -d -p 5672:5672 -p 15672:15672 --name rabbit jacobm/rabbitmq
docker run -d -p 5555:5555 -p 5555:5555/udp -p 5556:5556 --name riemann davidkelley/riemann
docker run -p 4567:4567 --link riemann:riemann --name riemann-dash davidkelley/riemann-dash
docker run -d --name scraper --link scraperdb:scraperdb --link rabbit:rabbit --link riemann:riemann  jacobm/dba-scraper