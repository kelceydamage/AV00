import time
from DFRobot_RaspberryPi_Expansion_Board import DFRobot_Expansion_Board_IIC as Board

board = Board(8, 0x10)    # Select i2c bus 1, set address to 0x10

board.begin()

board.set_pwm_enable()

board.set_pwm_frequency(18000)

board.set_pwm_duty(0, 50)
time.sleep(1)
board.set_pwm_duty(0, 0)

