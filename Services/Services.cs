using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sensors_test.Services
{
    public interface IService
    {

    }

    public static class ServiceRegistry
    {
        private readonly static Dictionary<string, IService> Registry = new();
        public static void AddService<T>(T Service) where T : IService
        {
            string key = typeof(T).Name;
            Registry.Add(key, Service);
        }
    }
}
