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
  Serial.begin(115200);
  
  // WIFI
  setupSecrets();
  getWifiSecrets(ssid, password);
  loadCertificates();
  connectWifi(ssid, password);
  
  if (MDNS.begin("humidor")) {
    Serial.println("MDNS responder started");
  }

  while (!pubSubClient.connected()) {
    PubSubConnect();
    delay(5000);
  }

  readSensors();

  Serial.println("Going into deep sleep for 10 minutes");
  ESP.deepSleep(600e6);
}

void loop(void){
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
    } else {
      Serial.println(event);
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

