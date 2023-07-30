using sensors_test.Drivers;

namespace sensors_test.Services
{

    public class DeviceRegistryService : IService
    {
        private readonly Dictionary<string, IDevice> Devices = new();

        public void AddDevice(IDevice Device)
        {
            string key = Device.Name;
            Console.WriteLine($"Adding Device {key}");
            if (Devices.ContainsKey(key))
            {
                throw new Exception($"Device {key} already found in registry");
            }
            Devices.Add(key, Device);
        }

        public void RemoveDevice(string DeviceName)
        {
            if (!Devices.ContainsKey(DeviceName))
            {
                throw new Exception($"Device {DeviceName} not found in registry");
            }
            Devices.Remove(DeviceName);
        }

        public IDevice GetDevice(string DeviceName)
        {
            if (!Devices.ContainsKey(DeviceName))
            {
                throw new Exception($"Device {DeviceName} not found in registry");
            }
            return Devices[DeviceName];
        }
    }
}
