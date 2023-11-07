import uuid

class MotorEvent:
    
    def __init__(ServiceName, Command, Direction, PwmAmount, Mode):
        # command = EnumMotorCommands
        # mode = EnumExecutionMode.Blocking
        direction = Direction
        pwm_amount = PwmAmount
        command = Command
        mode = Mode
        _id = uuid.uuid4()
        timestamp = "fake-time-stamp"
