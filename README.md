dealhunter
==========

### Notes


### Ubuntu 14.04 WmWare installation

#### Docker installation
http://docs.docker.io/installation/ubuntulinux/

#### Docker setup

Using mongo from docker image at https://index.docker.io/u/dockerfile/mongodb/:

docker pull dockerfile/mongodb


mkdir ~/mongo-data
mkdir ~/mongo-data/db

docker run -d -p 27017:27017 -v ~/mongo-data:/data dockerfile/mongodb
