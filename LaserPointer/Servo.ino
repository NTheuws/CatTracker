void moveServo(int angleX, int angleY, int goalX, int goalY, Servo servoX, Servo servoY)
{
  int valX = (goalX - angleX) / 10;
  int valY = (goalY - angleY) / 10;
  for ( int num = 0; num < 10; num++)
  {
    servoX.write(angleX + valX * num);
    servoY.write(angleY + valY * num);
    delay(30);
  }

  // moving directly to the goal instead of curving
  //_angleX = goalX;
  //_angleY = goalY;
}
