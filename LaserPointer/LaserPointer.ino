#include <Servo.h>

// Pins for the servos and LED.
#define SERVOX 9
#define SERVOY 10
#define LED 13

Servo _servoX; // X axis servo.
Servo _servoY; // Y axis servo.
int _angleX = 90; //X current angle.
int _angleY = 90; //Y current angle.

int lastMsg = 0; // Last corner msg received.
int randomNum = 0; // Random value for events.

// 3 points for the y-axis.
const int topY = 108;
const int middleY = 90;
const int bottomY = 72;

// 3 points for the x-axis.
const int leftX = 114;
const int middleX = 90;
const int rightX = 66;

void setup() {
  _servoX.attach(SERVOX);
  _servoY.attach(SERVOY);
  pinMode(LED, OUTPUT);
  digitalWrite(LED, LOW); // Make sure LED is turned off before it starts.
  Serial.begin(9600);
}

void loop() {

  String receivedMessage = readSerial();

  // Turn the LED on when anything is received.
  if (receivedMessage != "")
  {
    digitalWrite(LED, HIGH);
  }
  
  /*  There is a 3x3 grid, which means there are 9 squares.
   *  The Pointer will be directed towards one of these.
   *  These squares are named as followed:
   * 
   *1: Top Left. 
   *2: Top Middle.
   *3: Top Right.
   *4. Middle Left.
   *5: Middle Middle.
   *6: Middle Right.
   *7: Bottom Left.
   *8: Bottom Middle.
   *9: Bottom Right.
   */
  switch (receivedMessage.toInt()) {
    case 1:
      if (lastMsg  != 1)
      {
        moveToPoint(leftX, topY, 1);
      }
      break;
    case 2:
      if (lastMsg  != 2)
      {
        moveToPoint(middleX, topY, 2);
      }
      break;
    case 3:
      if (lastMsg  != 3)
      {
        moveToPoint(rightX, topY, 3);
      }
      break;
    case 4:
      if (lastMsg  != 4)
      {
        moveToPoint(leftX, middleY, 4);
      }
      break;
    case 5:
      if (lastMsg  != 5)
      {
        moveToPoint(middleX, middleY, 5);
      }
      break;
    case 6:
      if (lastMsg  != 6)
      {
        moveToPoint(rightX, middleY, 6);
      }
      break;
    case 7:
      if (lastMsg  != 7)
      {
        moveToPoint(leftX, bottomY, 7);
      }
      break;
    case 8:
      if (lastMsg  != 8)
      {
        moveToPoint(middleX, bottomY, 8);
      }
      break;
    case 9:
      if (lastMsg  != 9)
      {
        moveToPoint(rightX, bottomY, 9);
      }
      break;
  }

  // When waiting blink 25% - 1/4 chance.
  if (randomNum % 4 == 1)
  {
    delay(240);
    digitalWrite(LED, LOW);
    delay(240);
    digitalWrite(LED, HIGH);
  }

  // When waiting move 20% - 1/5 chance.
  else if (randomNum % 5 == 1)
  {
    idleMovement(_angleX, _angleY, _servoX, _servoY);
  }
}

// Move the laser dot to a new position with a chance of making a fake movement.
void moveToPoint(int posX, int posY, int msg)
{
  nextRandom();

  // Chance to pretend to go to a different square.
  if (randomNum % 2 == 1)
  {
    fakeMovement();
  }

  // Move the laser dot.
  moveServo(_angleX, _angleY, posX, posY, _servoX, _servoY);
  lastMsg = msg;
}

// Prentend the square went to another corner.
void fakeMovement()
{
  int val = random(1, 10);
  switch (val) {
    case 1:
      moveServo(_angleX, _angleY, 60, 114, _servoX, _servoY);
      break;
    case 2:
      moveServo(_angleX, _angleY, 90, 114, _servoX, _servoY);
      break;
    case 3:
      moveServo(_angleX, _angleY, 120, 114, _servoX, _servoY);
      break;
    case 4:
      moveServo(_angleX, _angleY, 60, 90, _servoX, _servoY);
      break;
    case 5:
      moveServo(_angleX, _angleY, 90, 90, _servoX, _servoY);
      break;
    case 6:
      moveServo(_angleX, _angleY, 120, 90, _servoX, _servoY);
      break;
    case 7:
      moveServo(_angleX, _angleY, 60, 66, _servoX, _servoY);
      break;
    case 8:
      moveServo(_angleX, _angleY, 90, 66, _servoX, _servoY);
      break;
    case 9:
      moveServo(_angleX, _angleY, 120, 66, _servoX, _servoY);
      break;
  }
}

// Generate next random number between 1-100.
// This number is used to make random events happen.
void nextRandom()
{
  randomNum = random(1, 101);
}
