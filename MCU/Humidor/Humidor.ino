#include <dht.h>
#include <WiFiClient.h>
#include <ESP8266mDNS.h>
#include <PubSubClient.h>

extern "C" {
  #include "user_interface.h"
}

#define HOSTNAME "humidor"
#define DHT11_PIN D3
#define SENSOR_TOPIC "humidor/sensors"

unsigned long lastRead = 0;
char ssid[30];
char password[30];

WiFiClientSecure wifi;
PubSubClient pubSubClient(wifi);
dht DHT;
long lastPubSubConnectionAttempt = 0;


void setup(void){
  pinMode(LED_BUILTIN, OUTPUT);
  digitalWrite(LED_BUILTIN, 0);
  Serial.begin(115200);
  
  // WIFI
  setupSecrets();
  getWifiSecrets(ssid, password);
  loadCertificates();
  connectWifi(ssid, password);
  
  PubSubSetup();

  if (MDNS.begin("humidor")) {
    Serial.println("MDNS responder started");
  }
}

void loop(void){
  if (millis() - lastRead >= 10000){
    lastRead = millis();
    readSensors();
  }

  PubSubLoop();

  // TODO: DEEP SLEEP
}

void readSensors(){
  Serial.println("Reading sensor...");
  int chk = DHT.read11(DHT11_PIN);
  Serial.print("chk: ");
  Serial.println(chk);

  Serial.print("Humidity: ");
  Serial.print(DHT.humidity);
  Serial.print(" %\t");
  Serial.print("Temperature: ");
  Serial.print(DHT.temperature);
  Serial.print(" *C\n");

  if (pubSubClient.connected()){
    String event = "{\"Humidity\":";
    event += DHT.humidity;
    event += ", \"Temperature\":";
    event += DHT.temperature;
    event += "}";
    if (!pubSubClient.publish(SENSOR_TOPIC, event.c_str(), false)) {
      Serial.println("Unable to publish event");
    }
  }
}

void PubSubCallback(char* topic, byte* payload, unsigned int length) {
  char *p = (char *)malloc((length + 1) * sizeof(char *));
  strncpy(p, (char *)payload, length);
  p[length] = '\0';

  Serial.print("Message received: ");
  Serial.print(topic);
  Serial.print(" - ");
  Serial.println(p);

  free(p);
}

