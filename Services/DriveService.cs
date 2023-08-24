using sensors_test.Controllers.MotorController;
using System.Configuration;
using Transport.Client;

namespace sensors_test.Services
{
    internal class DriveService: IService
    {
        private readonly TaskExecutorClient taskExecutorClient;
        private readonly IMotorController motorController;
        public DriveService(IMotorController MotorController, ConnectionStringSettingsCollection Connections)
        {
            taskExecutorClient = new(Connections);
            motorController = MotorController;
            Console.WriteLine("Drive Service");
        }
    }
}
