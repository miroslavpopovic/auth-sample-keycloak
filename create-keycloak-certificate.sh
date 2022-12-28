#! /bin/bash

openssl req -newkey rsa:2048 -nodes \
  -keyout ./build/keycloak/cert/server.key.pem -x509 -days 3650 -out ./build/keycloak/cert/server.crt.pem
