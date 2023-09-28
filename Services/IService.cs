namespace AV00.Services
{
    public interface IService
    {
        public string ServiceName { get; }
        public void Start();
    }
}
