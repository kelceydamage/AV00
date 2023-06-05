using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Device.Pwm;

namespace sensors_test.Drivers.MotorController
{
    internal class MDD10A
    {
        private readonly PwmChannel _pwmChannel;
        private readonly int _dutyCycleInterval = 1;
        private readonly float _dutyCycleDelay = 0.03f;
        private readonly int _motor1DirectionPin = 17;
        private readonly int _motor2DirectionPin = 16;
        private readonly int _pwmChip;
        private readonly int _pwmChannelNumber;

        public MDD10A(int pin, int pwmChip, int pwmChannelNumber, int frequency, int dutyCycle)
        {
            _pin = pin;
            _pwmChip = pwmChip;
            _pwmChannelNumber = pwmChannelNumber;
            _frequency = frequency;
            _dutyCycle = dutyCycle;
            _pwmChannel = PwmChannel.Create(pwmChip, pwmChannelNumber, frequency, dutyCycle);
        }

        public void Start()
        {
            _pwmChannel.Start();
        }

        public void Stop()
        {
            _pwmChannel.Stop();
        }

        public void SetDutyCycle(int dutyCycle)
        {
            _pwmChannel.DutyCycle = dutyCycle;
        }

        public void SetFrequency(int frequency)
        {
            _pwmChannel.Frequency = frequency;
        }
    }
}
