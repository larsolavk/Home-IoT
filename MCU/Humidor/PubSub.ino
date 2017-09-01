#define QOS_LEVEL 0

void PubSubCallback(char* topic, byte* payload, unsigned int length);


void PubSubSetup() {
  pubSubClient.setCallback(PubSubCallback);
}

boolean PubSubConnect() {
  Serial.print("Connecting to MQTT server...");
  Serial.println("Sending mDNS Query");
  int n = MDNS.queryService("mqtt", "tcp");
  if (n == 0) {
    Serial.println("No MQTT server found!");
  } else if (n > 1) {
    Serial.println("Multiple MQTT services found - remove the unnecessary mDNS services!");
  } else {
    Serial.print(F("INFO: MQTT broker IP address: "));
    Serial.print(MDNS.IP(0));
    Serial.print(F(":"));
    Serial.println(MDNS.port(0));
    pubSubClient.setServer(MDNS.IP(0), int(MDNS.port(0)));
  }

  if(n != 1 || !pubSubClient.connect(HOSTNAME)) {
    digitalWrite(LED_BUILTIN, !digitalRead(LED_BUILTIN));
    Serial.println("\nCouldn't connect to MQTT server. Will try again in 5 seconds.");
    return false;
  }
  
  digitalWrite(LED_BUILTIN, 0);
  Serial.println(" Connected.");
  return true;
}

void PubSubLoop() {
  if(!pubSubClient.connected()) {
    long now = millis();
        
    if(now - lastPubSubConnectionAttempt > 5000) {
      lastPubSubConnectionAttempt = now;
      if(PubSubConnect()) {
        lastPubSubConnectionAttempt = 0;
      }
    }
  } else {
    pubSubClient.loop();
  }
}
