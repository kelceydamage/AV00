# AV00
An embedded robotics project using C# and the Nvidia Xavier NX.

## Current Design Overview
![Untitled drawing (1)](https://github.com/kelceydamage/sensors-test/assets/16090219/1d6d7f56-4ac8-4e3f-91a6-4593bf0f7f37)

## Issues

# Version 1 Outstanding Goals
## TODO - Software (Next Phase)
* [ ] Update the DeviceRegistry to be useful.
* [ ] Add the MotorController to the ServiceRegistry
* [ ] Create a telemetry service for the robot.
* [ ] Create a desktop application to view telemery over WIFI.
* [ ] Add an ultrasonic sensor to the robot.
* [ ] Create a basic feedback loop the stops the motors near an obstacle.
* [ ] Add a gyro sensor to the robot.
* [ ] Create a basic feedback loop that keeps the robot driving straight.
* [ ] Remote input/override service.

## TODO - Infra (Next Phase)
* [ ] Setup a CI/CD pipeline.
* [ ] Dockerize the application to run in NV containers.

## TODO - Hardware (Next Phase)
* [ ] Add an ultrasonic sensor to the robot.
* [ ] Add a gyro sensor to the robot.
* [ ] Run some updated battery driven tests.
* [ ] Find a way to package the breadboard setup into the robot itself.

## TODO - Other
* [ ] Update documentation.
* [ ] Update design docs.
* [ ] Update spec sheets.

## Nice To Haves
* [ ] Make the MotorController public functions async to allow non-blocking timed operations.
* [ ] M3/4 mounts inside the chassis.