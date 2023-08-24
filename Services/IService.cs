namespace sensors_test.Services
{
    public interface IService
    {
        public string ServiceName { get; }
        public void Start();
    }
}
