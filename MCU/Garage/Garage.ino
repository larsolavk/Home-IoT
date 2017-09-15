#include <dht.h>
#include <WiFiClient.h>
#include <ESP8266mDNS.h>
#include <PubSubClient.h>

extern "C" {
  #include "user_interface.h"
}

#define DHT22_PIN D3
#define DOOR_OPENER_PIN D2
#define DOOR_SENSOR_TOP_PIN D1
#define DOOR_SENSOR_BOTTOM_PIN D4
#define CMD_TOPIC "garage/cmd"
#define SENSOR_TOPIC "garage/sensors"

unsigned long lastRead = 0;
char ssid[30];
char password[30];

WiFiClientSecure wifi;
PubSubClient pubSubClient(wifi);
dht DHT;
os_timer_t doorChangeTimer;
os_timer_t doorStateAlertTimer;
long lastPubSubConnectionAttempt = 0;


void setup(void){
  pinMode(LED_BUILTIN, OUTPUT);
  digitalWrite(LED_BUILTIN, 0);
  Serial.begin(115200);
  
  // DOOR SENSORS (Default HIGH, when switch close pin is pulled LOW)
  pinMode(DOOR_SENSOR_TOP_PIN, INPUT_PULLUP);
  attachInterrupt(digitalPinToInterrupt(DOOR_SENSOR_TOP_PIN), onDoorSensorChange, CHANGE);
  pinMode(DOOR_SENSOR_BOTTOM_PIN, INPUT_PULLUP);
  attachInterrupt(digitalPinToInterrupt(DOOR_SENSOR_BOTTOM_PIN), onDoorSensorChange, CHANGE);
  os_timer_setfn(&doorChangeTimer, notifyDoorSensorChange, NULL);
  os_timer_setfn(&doorStateAlertTimer, alertDoorState, NULL);
  
  // DOOR OPENER
  pinMode(DOOR_OPENER_PIN, OUTPUT);
  digitalWrite(DOOR_OPENER_PIN, LOW);

  // WIFI
  setupSecrets();
  getWifiSecrets(ssid, password);
  loadCertificates();
  connectWifi(ssid, password);
  
  PubSubSetup();

  if (MDNS.begin("garage")) {
    Serial.println("MDNS responder started");
  }
}

void loop(void){
  if (millis() - lastRead >= 10000){
    lastRead = millis();
    readSensors();
  }

  PubSubLoop();
}

void readSensors(){
  Serial.println("Reading sensor...");
  int chk = DHT.read22(DHT22_PIN);
  Serial.print("chk: ");
  Serial.println(chk);

  Serial.print("Humidity: ");
  Serial.print(DHT.humidity);
  Serial.print(" %\t");
  Serial.print("Temperature: ");
  Serial.print(DHT.temperature);
  Serial.print(" *C\n");


  String msg = "DoorSensorTop: ";
  msg += digitalRead(DOOR_SENSOR_TOP_PIN);
  Serial.println(msg);

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

/******************************
 *  Sensor interrupt handlers
 *****************************/
void onDoorSensorChange() {
  os_timer_arm(&doorChangeTimer, 1000, false);
}

void notifyDoorSensorChange(void *pArg) {
  bool top = digitalRead(DOOR_SENSOR_TOP_PIN);
  bool bottom = digitalRead(DOOR_SENSOR_BOTTOM_PIN);
   
  Serial.println("DoorSensor CHANGE:");
  Serial.print("DoorSensorTop => \t\t");
  Serial.println(top);
  Serial.print("DoorSensorBottom => \t\t");
  Serial.println(bottom);

  if (top && bottom) {
    os_timer_arm(&doorStateAlertTimer, 10000, false);
    Serial.println("Porten er i bevegelse");
  }
  else if (top){
    os_timer_disarm(&doorStateAlertTimer);
    Serial.println("Porten er åpen");
  }
  else if (bottom) {
    os_timer_disarm(&doorStateAlertTimer);
    Serial.println("Porten er lukket");
  }
  else {
    Serial.println("Porten er i ugyldig status");
  }
}

void alertDoorState(void *pArg) {
  Serial.println("Porten ser ut til å være fastkjørt i halvåpen stilling!");
}

void handleRelayOn() {
  digitalWrite(DOOR_OPENER_PIN, HIGH);
  char* msg = "Relay turned on";
  Serial.println(msg);
}

void handleRelayOff() {
  digitalWrite(DOOR_OPENER_PIN, LOW);
  char* msg = "Relay turned off";
  Serial.println(msg);
}

void PubSubCallback(char* topic, byte* payload, unsigned int length) {
  char *p = (char *)malloc((length + 1) * sizeof(char *));
  strncpy(p, (char *)payload, length);
  p[length] = '\0';

  Serial.print("Message received: ");
  Serial.print(topic);
  Serial.print(" - ");
  Serial.println(p);

  if (strcmp(p, "RelayOn") == 0) {
    handleRelayOn(); 
  }
  else if (strcmp(p, "RelayOff") == 0) {
    handleRelayOff();
  }
  else if (strcmp(p, "Pulse") == 0) {
    handleRelayOff();
    delay(200);
    handleRelayOn(); 
    delay(200);
    handleRelayOff();
  }

  free(p);
}

