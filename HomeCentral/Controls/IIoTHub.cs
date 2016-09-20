using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

namespace HomeCentral.Controls
{
    public class IIoTHub
    {
        public static async Task<string> AddDeviceAsync(string devicename)
        {
            string connectionString = "HostName=HomeCentralLucas.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=m7RnWrKR1pw5uIgGPigWRpkmQn8hYrI2/YCMaT5TGgQ=";
            RegistryManager registryManager = RegistryManager.CreateFromConnectionString(connectionString);

            string deviceId = devicename;
            Device device;
            try
            {
                device = await registryManager.AddDeviceAsync(new Device(deviceId));
            }
            catch (DeviceAlreadyExistsException)
            {
                device = await registryManager.GetDeviceAsync(deviceId);
            }
            return device.Authentication.SymmetricKey.PrimaryKey;
        }

        static DeviceClient deviceClient;
        static string iotHubUri = "HomeCentralLucas.azure-devices.net";
        

        public static async void SendData(Object telemetryDataPoint, string deviceKey)
        {
            deviceClient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(App.deviceName, deviceKey));

            while (true)
            {
                var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                var message = new Microsoft.Azure.Devices.Client.Message(Encoding.ASCII.GetBytes(messageString));

                await deviceClient.SendEventAsync(message);
            }
        }

    }

}
