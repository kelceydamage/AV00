# sensors-test
 Figure out how to use c# for accessing sensors on NX

## Overview
![Untitled drawing (1)](https://github.com/kelceydamage/sensors-test/assets/16090219/1d6d7f56-4ac8-4e3f-91a6-4593bf0f7f37)

## Issues
* Currently working on getting the NX to power the MDD10A motor driver. The PWM signal on the NX is quite weak and will voltage drop at the slightest draw. The MDD10A was dropping the pin PWM voltage to 1.6v. I'm investigating using a PCA9685 now, as my earlier efforts with a basic STM32 failed. The NX would fry the STM32, and my best guess is pin changes introduced in Jetpack 5. If the PCA9685 fail, I might have to try using homebrewed buffers on the PWM pins.
