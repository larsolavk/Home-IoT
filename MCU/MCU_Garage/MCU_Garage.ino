#include <dht.h>
#include <ESP8266WiFi.h>
#include <WiFiClient.h>
#include <ESP8266WebServer.h>
#include <ESP8266mDNS.h>
#include "FS.h"

extern "C" {
  #include "user_interface.h"
}

#define HOSTNAME "MCU_Garage"
#define DHT11_PIN D3
#define DOOR_OPENER_PIN D2
#define DOOR_SENSOR_TOP_PIN D1
#define DOOR_SENSOR_BOTTOM_PIN D4

unsigned long lastRead = 0;

dht DHT;
ESP8266WebServer server(80);
os_timer_t doorChangeTimer;
os_timer_t doorStateAlertTimer;

void setup(void){
  pinMode(LED_BUILTIN, OUTPUT);
  digitalWrite(LED_BUILTIN, 0);
  Serial.begin(115200);

  // File System
  if (!SPIFFS.begin()) {
    Serial.println("Unable to mount SPIFFS");
    digitalWrite(LED_BUILTIN, 1);
    return;  
  }
  Serial.println("SPIFFS mounted successfully!");

  // SECRETS FILE
  File secretsFile = SPIFFS.open("/secret/secrets.dat", "r");
  if(!secretsFile) {
    Serial.println("Couldn't load secrets");
    digitalWrite(LED_BUILTIN, 1);
    return;  
  }
  Serial.println("Secrets loaded successfully!");
  String sSsid = secretsFile.readStringUntil('\n').c_str();
  String sPassword = secretsFile.readStringUntil('\n').c_str();
  secretsFile.close();

  char ssid[sSsid.length() + 1];
  sSsid.toCharArray(ssid, sSsid.length());
  char password[sPassword.length() + 1];
  sPassword.toCharArray(password, sPassword.length());

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
  WiFi.mode(WIFI_STA);
  WiFi.hostname(HOSTNAME);
  WiFi.begin(ssid, password);
  Serial.println("");

  // Wait for connection
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    digitalWrite(LED_BUILTIN, !digitalRead(LED_BUILTIN));
    Serial.print(".");
  }
  digitalWrite(LED_BUILTIN, 0);

  Serial.println("");
  Serial.print("Connected to ");
  Serial.println(ssid);
  Serial.print("IP address: ");
  Serial.println(WiFi.localIP());

  if (MDNS.begin("esp8266")) {
    Serial.println("MDNS responder started");
  }

  // WEB SERVER
  server.on("/", handleRoot);
  server.on("/inline", [](){
    server.send(200, "text/plain", "this works as well");
  });
  server.on("/relay_on", handleRelayOn);
  server.on("/relay_off", handleRelayOff);
  server.onNotFound(handleNotFound);
  server.begin();
  Serial.println("HTTP server started");
}

void loop(void){
  if (millis() - lastRead >= 10000){
    lastRead = millis();
    readSensors();
  }

  server.handleClient();
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


  String msg = "DoorSensorTop: ";
  msg += digitalRead(DOOR_SENSOR_TOP_PIN);
  Serial.println(msg);
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

/**************************
 * WEB SERVER HANDLERS
 *************************/
void handleRoot() {
  digitalWrite(LED_BUILTIN, 1);
  String msg = "hello from esp8266!";
  msg = msg+ "\nHumidity: ";
  msg = msg + DHT.humidity;
  msg = msg + "% - Temp: ";
  msg = msg + DHT.temperature;
  msg = msg + " *C";
  
  server.send(200, "text/plain", msg);
  digitalWrite(LED_BUILTIN, 0);
}

void handleNotFound(){
  digitalWrite(LED_BUILTIN, 1);
  String message = "File Not Found\n\n";
  message += "URI: ";
  message += server.uri();
  message += "\nMethod: ";
  message += (server.method() == HTTP_GET)?"GET":"POST";
  message += "\nArguments: ";
  message += server.args();
  message += "\n";
  for (uint8_t i=0; i<server.args(); i++){
    message += " " + server.argName(i) + ": " + server.arg(i) + "\n";
  }
  server.send(404, "text/plain", message);
  digitalWrite(LED_BUILTIN, 0);
}

void handleRelayOn() {
  digitalWrite(DOOR_OPENER_PIN, HIGH);
  char* msg = "Relay turned on";
  Serial.println(msg);
  server.send(200, "text/plain", msg);
}

void handleRelayOff() {
  digitalWrite(DOOR_OPENER_PIN, LOW);
  char* msg = "Relay turned off";
  Serial.println(msg);
  server.send(200, "text/plain", msg);
}


