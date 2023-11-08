
from os import name
import zmq
import time
import argparse
from motor_event import *


class CLI:
    
    def __init__(self):
        self.parser = argparse.ArgumentParser()

    def configure_arguments(self):
        self.parser.add_argument(
            "-c",
            "--command",
            choices=["Move", "Turn"] 
        )
        self.parser.add_argument(
            "-d",
            "--direction",
            choices=list(vars(MotorDirection()).keys())
        )
        self.parser.add_argument(
            "-p",
            "--power",
            help="Float between 0 and 100"
        )
        self.args = self.parser.parse_args()


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
    cli = CLI()
    cli.configure_arguments()
    
    relay_client = RelayClient()
    
    motor_event = MotorEvent(
         ServiceName="DriveService",
         Command=EnumMotorCommands(cli.args["comman"]),
         Direction={},
         PwmAmount=cli.args["power"],
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
    
    