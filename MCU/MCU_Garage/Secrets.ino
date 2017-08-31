
void setupSecrets() 
{
  if (!SPIFFS.begin()) {
    Serial.println("Unable to mount SPIFFS");
    digitalWrite(LED_BUILTIN, 1);
    return;  
  }
  Serial.println("SPIFFS mounted successfully!");
}

void getWifiSecrets(char* ssid, char* password) 
{
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

  //char ssid[sSsid.length() + 1];
  sSsid.toCharArray(ssid, sSsid.length());
  //char password[sPassword.length() + 1];
  sPassword.toCharArray(password, sPassword.length());
}

