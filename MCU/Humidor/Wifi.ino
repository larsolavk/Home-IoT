#include <ESP8266WiFi.h>

void connectWifi(const char* ssid, const char* password) {
  int WiFiCounter = 0;
  // We start by connecting to a WiFi network
  Serial.print("Connecting to ");
  Serial.println(ssid);
  
  WiFi.disconnect();
  WiFi.mode(WIFI_STA);
  WiFi.hostname(HOSTNAME);
  wifi_station_set_hostname(HOSTNAME);
  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED && WiFiCounter < 30) {
    digitalWrite(LED_BUILTIN, !digitalRead(LED_BUILTIN));
    delay(1000);
    WiFiCounter++;
    Serial.print(".");
  }

  digitalWrite(LED_BUILTIN, 0);
  Serial.println("");
  Serial.println("WiFi connected");
  Serial.print("IP address: ");
  Serial.println(WiFi.localIP());
}
  
