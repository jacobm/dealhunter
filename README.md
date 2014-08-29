dealhunter
==========

Small scraper of dba.dk written for my wife.

The project is to play around with a couple of technical ideas that I haven't had the chance to use yet. 

## Constraints

- backend storage is only allowed to append data, no in-place updates or deletes
- each api must do one thing only (provide search, scrape, app, user-data) (microservices)
- deployment must be done using docker
- one page app

## Notes to self

### Ubuntu 14.04 WmWare installation

#### Nodejs
sudo apt-get install nodejs

sudo apt-get install npm

sudo apt-get –purge remove node (in necessary)

sudo ln -s /usr/bin/nodejs /usr/bin/node

#### Docker installation
http://docs.docker.io/installation/ubuntulinux/

