namespace sensors_test.Drivers.IO
{
    public class PWM
    {
        private readonly IPwmController pwmController;
        private readonly ushort maxPwmValue;
        public float PwmMaxValue { get => pwmController.PwmMaxValue; }

        public PWM(IPwmController PwmController)
        {
            pwmController = PwmController;
            maxPwmValue = (ushort)(Math.Pow(2, pwmController.PwmBitDepth) - 1);
        }

        public void SetPwmFrequency(int Frequency)
        {
            if (Frequency < pwmController.PwmMinFrequencyHz || Frequency > pwmController.PwmMaxFrequencyHz)
            {
                throw new ArgumentException($"Frequency must be between {pwmController.PwmMinFrequencyHz} and {pwmController.PwmMaxFrequencyHz}");
            }
            pwmController.SetFrequency(Frequency);
        }

        public void SetChannelPWM(int ChannelId, ushort PwmAmount)
        {
            if (ChannelId < 0 || ChannelId > pwmController.PwmChannelCount)
            {
                throw new ArgumentException($"ChannelId must be between 0 and {pwmController.PwmChannelCount}");
            }
            if (PwmAmount < 0 || PwmAmount > maxPwmValue)
            {
                throw new Exception($"PwmAmount must be between 0 and {maxPwmValue} for this {pwmController.PwmBitDepth}-bit controller");
            }
            pwmController.SetChannelPwm(ChannelId, PwmAmount);
        }

        public void Reset()
        {
            pwmController.Reset();
        }
    }
}