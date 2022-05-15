Gyro Test 1 IIC Map
```bash
     0  1  2  3  4  5  6  7  8  9  a  b  c  d  e  f
00:          -- -- -- -- -- -- -- -- -- 0c -- -- -- 
10: 10 -- -- -- -- -- -- -- -- 19 -- -- -- -- -- -- 
20: -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- 
30: -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- 
40: -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- 
50: -- -- -- 53 -- -- -- -- -- -- -- -- -- -- -- -- 
60: -- -- -- -- -- -- -- -- 68 69 -- -- -- -- -- -- 
70: -- -- -- -- -- -- -- 77
```

`0c` - AK09918 Grove IMU 9DOF Compass

`10` - ?

`19` - BMI088 - Grove 6 Axis Accelerometer (optional 0x18)

`53` - ADXL-345 Grove 3 Axis Digital Accelerometer (+-16g)

`68` - MPU-9250 Grove IMU 10DOF Gyroscope + Accelerometer + Magnetometer -- (BMI088 Grove 6 Axis Gyroscope?)

`69` - LCM20600  Grove IMU 9DOF Gyroscope + Accelerometer -- (BMI088 Grove 6 Axis Gyroscope?)

`77` - BMP280 Grove IMU 10DOF Barometer Sensor
