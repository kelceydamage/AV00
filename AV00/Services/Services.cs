namespace AV00.Services
{
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
