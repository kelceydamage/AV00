using sensors_test.Drivers;

namespace sensors_test.Services
{

    public class DeviceRegistryService : IService
    {
        private readonly Dictionary<string, IDeviceDriver> Devices = new();

        public void AddDevice(IDeviceDriver Device)
        {
            string key = nameof(Device);
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

        public IDeviceDriver GetDevice(string DeviceName)
        {
            if (!Devices.ContainsKey(DeviceName))
            {
                throw new Exception($"Device {DeviceName} not found in registry");
            }
            return Devices[DeviceName];
        }
    }
}
