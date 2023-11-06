using System.Device.Gpio.Drivers;
using System.Device.Gpio;

namespace AV00.Drivers.IO
{
    public class GPIO
    {
        private readonly GpioController gpio;
        public GPIO(int GpioControllerId)
        {
            gpio = new GpioController(PinNumberingScheme.Logical, new LibGpiodDriver(GpioControllerId));
        }

        public void SafeWritePin(int PinNumber, PinValue Value)
        {
            OpenPin(PinNumber);
            SetPinMode(PinNumber, PinMode.Output);
            WritePin(PinNumber, Value);
            ClosePin(PinNumber);
        }

        public void WritePin(int PinNumber, PinValue Value)
        {
            gpio.Write(PinNumber, Value);
        }

        public void SetPinMode(int PinNumber, PinMode Mode)
        {
            if (!gpio.IsPinModeSupported(PinNumber, Mode))
            {
                throw new Exception($"**** GPIO Pin {PinNumber} does not support mode: {Mode}");
            }
            gpio.SetPinMode(PinNumber, Mode);
        }

        public void OpenPin(int PinNumber)
        {
            if (gpio.IsPinOpen(PinNumber))
            {
                throw new Exception($"**** GPIO Pin {PinNumber} already open");
            }
            gpio.OpenPin(PinNumber);
        }

        public void ClosePin(int PinNumber)
        {
            if (!gpio.IsPinOpen(PinNumber))
            {
                throw new Exception($"**** GPIO Pin {PinNumber} already closed");
            }
            gpio.ClosePin(PinNumber);
        }

        public void DisposeGpio()
        {
            gpio.Dispose();
        }

        public int GetGpioPinCount()
        {
            return gpio.PinCount;
        }
    }
}
