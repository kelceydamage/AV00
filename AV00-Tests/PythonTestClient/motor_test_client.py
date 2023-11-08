
import zmq
import time
from motor_event import *


class RelayClient:
    
    def __init__(self):
        self.context = zmq.Context()
        self.socket = self.context.socket(zmq.PUSH)
        self.socket.connect("tcp://127.0.0.1:5556")
    
    def send_motor_event(self, motor_event):
        event = Event(motor_event)
        self.socket.send_multipart(event.serialize())

class main:
    
    def __init__(self):
        pass
    


if __name__ == "__main__":
    relay_client = RelayClient()
    
    motor_event = MotorEvent(
         ServiceName="DriveService",
         Command=EnumMotorCommands.Move,
         Direction={},
         PwmAmount=30.0,
    )
    relay_client.send_motor_event(motor_event)    
    time.sleep(1)
    
    motor_event = MotorEvent(
         ServiceName="DriveService",
         Command=EnumMotorCommands.Move,
         Direction={},
         PwmAmount=0.0,
    )
    relay_client.send_motor_event(motor_event)
    
    