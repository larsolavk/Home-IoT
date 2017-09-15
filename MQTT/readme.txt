Note! In order to use certificates the NodeMCU should be set to run at 160MHz

Create certificates:
--------------------
# CA certificate:
openssl req -new -x509 -days 3650 -extensions v3_ca -keyout ca.key.pem -out ca.crt.pem

# Server certificate (MQTT broker)
## Generate a key
openssl genrsa -out mqtt.local.key.pem 2048

## Create a Certificate signing request
openssl req -out mqtt.local.csr.pem -key mqtt.local.key.pem -new

## Sign the certificate
openssl x509 -req -in mqtt.local.csr.pem -CA ca.crt.pem -CAkey ca.key.pem -CAcreateserial -out mqtt.local.crt.pem -days 3650

# Client certificate (NodeMCU)
## Generate a key
openssl genrsa -out garage.local.key.pem 2048
## Create a Certificate signing request
openssl req -out garage.local.csr.pem -key garage.local.key.pem -new
## Sign the certificate
openssl x509 -req -in garage.local.csr.pem -CA ca.crt.pem -CAkey ca.key.pem -CAcreateserial -out garage.local.crt.pem -days 3650

## Convert pem to der
openssl x509 -outform der -in garage.crt.pem -out garage.crt.der
openssl rsa -outform der -in garage.key.pem -out garage.key.der

## Create PFX certificate with private key (to use in dotnet core services)
openssl pkcs12 -export -in LarsOlav-PC.crt.pem -inkey LarsOlav-PC.key.pem -out LarsOlav-PC.crt.pfx

Create a service in docker swarm for mosquitto:
-----------------------------------------------
docker service create \
--name mosquitto \
--replicas 1 \
--constraint 'node.hostname == rpi3-1' \
--mount type=bind,source=/etc/mosquitto/config,destination=/mosquitto/config \
--mount type=bind,source=/etc/mosquitto/cert,destination=/etc/mosquitto/ca_certificates \
--mount type=bind,source=/etc/mosquitto/cert,destination=/etc/mosquitto/certs \
--hostname rpi3-1 \
-p 8883:8883 \
-p 9001:9001 \
larsolavk/mosquitto-rpi