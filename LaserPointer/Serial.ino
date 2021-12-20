void sendInt(int sentInt) {
  Serial.print("#");
  Serial.print(sentInt);           
  Serial.print("%");
}

String readSerial() {
  static bool readingMessage;
  static String receivedMessage;

  if (Serial.available() > 0) {
    int incomingByte = Serial.read();                           // Read Serial.
    char receivedChar = (char) incomingByte;                    

    if (readingMessage == false) {                              // Clear last msg.
      receivedMessage = "";
    }

    if (receivedChar == '%') {                                  // Last symbol of the message.
      readingMessage = false;
    }
    else if (readingMessage == true) {                          
      receivedMessage += receivedChar;
    }
    else if (receivedChar == '#') {                             // First symbol of the message.
      readingMessage = true;                                    
    }                                                           
  }

  if (readingMessage == false) {                                // When the message is done return.
    return receivedMessage;
  }
  else {                                                        // Nothing received, return.
    return "";
  }
}
