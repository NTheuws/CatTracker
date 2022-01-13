// Moving the servo to a specific corner.
void moveServo(int angleX, int angleY, int goalX, int goalY, Servo servoX, Servo servoY)
{
  // Speed randomizer.
  int randomValue = random(5, 60);
  bool blinking = false;

  if (randomValue % 3 == 0)
  {
    blinking = true;
  }

  double valX = (goalX - angleX) / 24;
  double valY = (goalY - angleY) / 18;
  for ( int num = 0; num < 24; num++)
  {
    // Prevent it from blinking too fast. Only blink in slower movements.
    if (blinking && randomValue > 30)
    {
      if (num % 6 == 0)
      {
        digitalWrite(LED, LOW);
      }
      else if (num % 3 == 0)
      {
        digitalWrite(LED, HIGH);
      }
    }

    // Move both axis to the correct point.
    servoX.write(angleX + valX * num);

    if (num < 18)
    {
      servoY.write(angleY + valY * num);
    }
    delay(randomValue);
  }

  // moving directly to the goal instead of curving from the center.
  _angleX = angleX + valX * 24;
  _angleY = angleY + valY * 18;
  delay(300);
}

// IDLE movement. Moving around in squares.
void idleMovement(int angleX, int angleY, Servo servoX, Servo servoY)
{
  servoX.write(_angleX);
  servoY.write(_angleY);

  // Going a around in 4 steps.
  for (int num = 0; num < 20; num++)
  {
    if (num < 5) // First 5, 0-4.
    {
      _angleX += 2;
      servoX.write(_angleX);
    }
    else if (num > 4 && num < 10) // Second 5, 5-9.
    {
      _angleY += 2;
      servoY.write(_angleY);
    }
    else if (num > 9 && num < 15) // Third 5, 10-14.
    {
      _angleX += -2;
      servoX.write(_angleX);
    }
    else if (num < 20) // Fourth 5, 15-19.
    {
      _angleY += -2;
      servoY.write(_angleY);
    }
    delay(50);
  }
}
