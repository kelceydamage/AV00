# AV00
An embedded robotics project using C# and the Nvidia Xavier NX.

Relies on [AV00-Transport](https://github.com/kelceydamage/AV00-Transport) for inter-service communication.

## Current Design Overview
![Untitled drawing (1)](https://github.com/kelceydamage/sensors-test/assets/16090219/1d6d7f56-4ac8-4e3f-91a6-4593bf0f7f37)

## Issues

# Version 1 Outstanding Goals
## TODO - High Level
* [ ] Create a desktop application to view telemery over WIFI. - Halted for now due to this issue https://github.com/xamarin/Xamarin.Forms/issues/10818

## TODO - Embedded Software (Services)
* [ ] Update the DeviceRegistry to be useful.
* [x] Add the MotorController/DriveService to the ServiceRegistry
* [ ] Create a telemetry service for the robot.
* [ ] Add an ultrasonic sensor to the robot.
* [ ] Create a basic feedback loop the stops the motors near an obstacle.
* [ ] Add a gyro sensor to the robot.
* [ ] Create a basic feedback loop that keeps the robot driving straight.
* [x] Implement a strategy for handling blocking, non-blocking, and override motor operations. ![Motor Command Execution Flow (3)](https://github.com/kelceydamage/AV00/assets/16090219/5c59d172-462f-49ca-ab8d-6c5c2ca6b29d)

## TODO - Shared Libraries
[AV00 Shared](https://github.com/kelceydamage/AV00-Shared)

## TODO - Interservice Communication (Transport Layer)
[AV00 Transport](https://github.com/kelceydamage/AV00-Transport)
* [x] Create a transport layer to allow services to communicate with each other.

## TODO - Desktop Software (Event Viewer / Control) 
[AV00 Control Application Repo](https://github.com/kelceydamage/AV00-Control-Application)

* Halted for now due to this issue https://github.com/xamarin/Xamarin.Forms/issues/10818

* [x] Implement backing DB for storing event stream and powering UI.
* [ ] Display current events as they happen.
* [ ] Allow filtering of events.
* [ ] Remote input/override client.

## TODO - Infra (Code Management)
* [ ] Setup a CI/CD pipeline.
* [ ] Dockerize the application to run in NV containers.

## TODO - Hardware (Physical Robot)
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
* [ ] Remote input/override service.
* [ ] Create a basic console status screen while running program from terminal.

# Notes
* Digital Port: IO expansion board offers 28 groups (D0-D27) of digital ports that are led out via Raspberry Pi ports GPIO0~GPIO27 (BCM codes)
* Requires libgpiod-dev (Could not get to work on JP4)
* sudo apt install -y libgpiod-dev
* gpiochip1 [tegra-gpio-aon] (40 lines)
* Gpiod uses line numbers to access gpio. The line numbers can be found with `gpioinfo` and cross referenced with the nvidia pinmux spreadsheet.
* The pinmux spreadsheet can be found here: https://developer.nvidia.com/jetson-nano-pinmux-datasheet
* Notes for personal use, these are free GPIO pins.
    * gpiochip1 127 -- BCM 17 -- J41 Pin 11
    * gpiochip1 112 -- BCM 18 -- J41 Pin 12
* PWM2 on MD10A is driver motor, PWM1 is turning motor
* XavierNX built-in PWMs are not strong enough to hold a signal for motors even with external motor power supply. Would always drop below 2v.
* Requires PCA9685 or STM32 equivalent to drive motors.

# References
* https://github.com/dotnet/iot/blob/main/src/System.Device.Gpio/System/Device/I2c/I2cDevice.cs#L11
* https://developer.download.nvidia.com/assets/embedded/secure/jetson/xavier/docs/Xavier_TRM_DP09253002.pdf?VakqmUGBQ99wvpu9mQgUOnm_mwq8MupZBel1lXFho98PGkC6gCjYKa58bYSGYUZZYHMpkF-PPS9WNeV_KmsmQxUQX84dYaEQavkufeAuom2ZpN2P8oQ3I1gSljfF-7rzC1sxKAXvgDnQlzdmma_jvCdqtjyKvye5MFmhFtSSG63Huw==&t=eyJscyI6ImdzZW8iLCJsc2QiOiJodHRwczovL3d3dy5nb29nbGUuY29tLyJ9
* https://github.com/NVIDIA/jetson-gpio/blob/6cab53dc80f8f5ecd6257d90dc7c0c51cb5348a7/lib/python/Jetson/GPIO/gpio_pin_data.py#L321
* https://forums.developer.nvidia.com/t/gpio-numbers-and-sysfs-names-changed-in-jetpack-5-linux-5-10/218580/7
* https://forums.developer.nvidia.com/t/gpio-numbers-and-sysfs-names-changed-in-jetpack-5-linux-5-10/218580/3
* https://jetsonhacks.com/nvidia-jetson-xavier-nx-gpio-header-pinout/
