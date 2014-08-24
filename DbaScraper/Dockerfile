FROM      ubuntu:trusty
MAINTAINER Jacob Mortensen <jacob.mortensen.it@gmail.com>

RUN apt-get update
RUN apt-get install -y mono-complete    	    	       

ADD ./bin/Release ./robot

CMD mono "./robot/DbaScraper.exe"