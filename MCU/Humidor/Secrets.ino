#include "FS.h"

void setupSecrets() 
{
  if (!SPIFFS.begin()) {
    Serial.println("Unable to mount SPIFFS");
    return;  
  }
  Serial.println("SPIFFS mounted successfully!");
}

void getWifiSecrets(char* ssid, char* password) 
{
  File secretsFile = SPIFFS.open("/secret/secrets.dat", "r");
  if(!secretsFile) {
    Serial.println("Couldn't load secrets");
    return;  
  }
  Serial.println("Secrets loaded successfully!");
  String sSsid = secretsFile.readStringUntil('\n').c_str();
  String sPassword = secretsFile.readStringUntil('\n').c_str();
  secretsFile.close();

  //char ssid[sSsid.length() + 1];
  sSsid.toCharArray(ssid, sSsid.length());
  //char password[sPassword.length() + 1];
  sPassword.toCharArray(password, sPassword.length());
}

void loadCertificates() {
  File ca = SPIFFS.open("/secret/humidor.crt.der", "r");
  if(!ca) {
    Serial.println("Couldn't load cert");
    return;  
  }

  if(wifi.loadCertificate(ca)) {
    Serial.println("Loaded Cert");
  } else {
    Serial.println("Didn't load cert");
    return;
  }
    
  File key = SPIFFS.open("/secret/humidor.key.der", "r");
  if(!key) {
    Serial.println("Couldn't load key");
    return;  
  }
  
  if(wifi.loadPrivateKey(key)) {
    Serial.println("Loaded Key");
  } else {
    Serial.println("Didn't load Key");
  }
}

