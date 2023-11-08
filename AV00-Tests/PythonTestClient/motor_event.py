import uuid
import json
from enum import Enum


class EnumEventType(int, Enum):
    Event = 0
    EventReceipt = 1
    EventLog = 2


class EnumMotorCommands(int, Enum):
    Move = 0
    Turn = 1


class MotorDirection:
    # PinValues
    Forwards = {};
    Backwards = {};
    Left = {};
    Right = {};

    def __init__(self):
        self.Forwards = {};
        self.Backwards = {};
        self.Left = {};
        self.Right = {};


class Event:

    def __init__(self, motor_event):
        self.motor_event = motor_event
       
    def serialize(self):
        temp = [
            str.encode(self.motor_event.service_name),
            str.encode(EnumEventType(0).name),
            str.encode(self.motor_event._id),
            str.encode(json.dumps(self.motor_event.to_dict()))
        ]
        print(temp)
        return temp


class MotorEvent:
    
    def __init__(self, ServiceName, Command, Direction, PwmAmount, Mode=0):
        # mode = EnumExecutionMode.Blocking
        self.service_name = ServiceName
        self.direction = Direction
        self.pwm_amount = PwmAmount
        self.command = Command
        self.mode = Mode
        self._id =  str(uuid.uuid4())
        self.timestamp = "fake-time-stamp"
    
    def to_dict(self):
        return {
            "ServiceName": self.service_name,
            "Command": self.command,
            "Direction": self.direction,
            "PwmAmount": self.pwm_amount,
            "EnumExecutionMode": self.mode,
            "Id": self._id,
            "TimeStamp": self.timestamp,
        }
