using AV00.Drivers.ExpansionBoards;

namespace AV00.Drivers.IO
{
    public class PWM
    {
        private readonly IPwmGenerator pwmGenerator;
        private readonly float maxPwmValue = 100.0f;
        public float PwmMaxValue { get => pwmGenerator.PwmMaxValue; }

        public PWM(IPwmGenerator PwmGenerator)
        {
            pwmGenerator = PwmGenerator;
            //maxPwmValue = (ushort)(Math.Pow(2, pwmGenerator.PwmBitDepth) - 1);
        }

        public void SetPwmFrequency(int Frequency)
        {
            if (Frequency < pwmGenerator.PwmMinFrequencyHz || Frequency > pwmGenerator.PwmMaxFrequencyHz)
            {
                throw new ArgumentException($"Frequency must be between {pwmGenerator.PwmMinFrequencyHz} and {pwmGenerator.PwmMaxFrequencyHz}");
            }
            pwmGenerator.SetFrequency(Frequency);
        }

        public void SetChannelPWM(int ChannelId, float PwmAmountPercent)
        {
            if (ChannelId < 0 || ChannelId > pwmGenerator.PwmChannelCount)
            {
                throw new ArgumentException($"ChannelId must be between 0 and {pwmGenerator.PwmChannelCount}");
            }
            if (PwmAmountPercent < 0 || PwmAmountPercent > maxPwmValue)
            {
                throw new Exception($"PwmAmount must be between 0 and {maxPwmValue} for this {pwmGenerator.PwmBitDepth}-bit controller");
            }
            pwmGenerator.SetChannelPwm(ChannelId, PwmAmountPercent);
        }

        public void Reset()
        {
            pwmGenerator.Reset();
        }
    }
}