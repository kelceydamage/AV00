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
            if (!Devices.TryAdd(key, Device))
                throw new Exception($"Failed to add device {key} to registry");
        }

        public void RemoveDevice(string DeviceName)
        {
            Devices.Remove(DeviceName);
        }

        public IDevice GetDevice(string DeviceName)
        {
            if (!Devices.TryGetValue(DeviceName, out IDevice? device))
                throw new Exception($"Device {DeviceName} not found in registry");
            return device;
        }
    }
}
