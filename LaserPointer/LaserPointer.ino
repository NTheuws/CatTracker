#include <Servo.h>

#define SERVOX 9
#define SERVOY 10
#define LED 13

Servo _servoX; // X axis servo.
Servo _servoY; // Y axis servo.
int _angleX = 90; //X current angle.
int _angleY = 90; //Y current angle.

int lastMsg = 0; // Last corner msg received.

void setup() {
  _servoX.attach(SERVOX);
  _servoY.attach(SERVOY);
  pinMode(LED, OUTPUT);
  digitalWrite(LED, HIGH);
  Serial.begin(9600);
}

void loop() {
  String receivedMessage = readSerial();
  // Top left.
  if (receivedMessage == "1" && lastMsg != 1)
  {
    moveServo(_angleX, _angleY, 115, 115, _servoX, _servoY);
    lastMsg = 1;
  }
  // Bottom left.
  if (receivedMessage == "2" && lastMsg != 2)
  {
    moveServo(_angleX, _angleY, 115, 65, _servoX, _servoY);
    lastMsg = 2;
  }
  // Top right.
  if (receivedMessage == "3" && lastMsg != 3)
  {
    moveServo(_angleX, _angleY, 65, 115, _servoX, _servoY);
    lastMsg = 3;
  }
  // Bottom right.
  if (receivedMessage == "4" && lastMsg != 4)
  {
    moveServo(_angleX, _angleY, 65, 65, _servoX, _servoY);
    lastMsg = 4;
  }
}
