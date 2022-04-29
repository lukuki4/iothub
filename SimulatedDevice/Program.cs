// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This application uses the Azure IoT Hub device SDK for .NET
// For samples see: https://github.com/Azure/azure-iot-sdk-csharp/tree/master/iothub/device/samples

using Microsoft.Azure.Devices.Client;
using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SimulatedIotDevice
{
    /// <summary>
    /// This sample illustrates the very basics of a device app sending telemetry. For a more comprehensive device app sample, please see
    /// <see href="https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Samples/device/DeviceReconnectionSample"/>.
    /// </summary>
    internal class Program
    {
        private static DeviceClient s_deviceClient;
        private static readonly TransportType s_transportType = TransportType.Mqtt;

        // The device connection string to authenticate the device with your IoT hub.
        // Using the Azure CLI:
        // az iot hub device-identity show-connection-string --hub-name {YourIoTHubName} --device-id MyDotnetDevice --output table
        private static string s_connectionString = "HostName=LukmanIotSensorHub.azure-devices.net;DeviceId=lukmam_simdevice;SharedAccessKey=MB+4QE00jSxq61QVRI3VopnSY+hpG/bEHG9go0b2vtQ=";


        private static async Task Main(string[] args)
        {
            Console.WriteLine("IoT Hub Quickstarts #1 - Simulated device.");

            // This sample accepts the device connection string as a parameter, if present
            ValidateConnectionString(args);

            // Connect to the IoT hub using the MQTT protocol
            s_deviceClient = DeviceClient.CreateFromConnectionString(s_connectionString, s_transportType);

            // Set up a condition to quit the sample
            Console.WriteLine("Press control-C to exit.");
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Exiting...");
            };

            // Run the telemetry loop
            await SendDeviceToCloudMessagesAsync(cts.Token);

            s_deviceClient.Dispose();
            Console.WriteLine("Device simulator finished.");
        }

        private static void ValidateConnectionString(string[] args)
        {
            if (args.Any())
            {
                try
                {
                    var cs = IotHubConnectionStringBuilder.Create(args[0]);
                    s_connectionString = cs.ToString();
                }
                catch (Exception)
                {
                    Console.WriteLine($"Error: Unrecognizable parameter '{args[0]}' as connection string.");
                    Environment.Exit(1);
                }
            }
            else
            {
                try
                {
                    _ = IotHubConnectionStringBuilder.Create(s_connectionString);
                }
                catch (Exception)
                {
                    Console.WriteLine("This sample needs a device connection string to run. Program.cs can be edited to specify it, or it can be included on the command-line as the only parameter.");
                    Environment.Exit(1);
                }
            }
        }

        // Async method to send simulated telemetry
        private static async Task SendDeviceToCloudMessagesAsync(CancellationToken ct)
        {
            // Initial telemetry values
            double minBodyTemperature = 29;
            double minHeartrate = 60;
            var rand = new Random();





            //int minFanspeed = 0;
            //int maxFanspeed = 100;
            var randFanspeed = new Random();

            bool lightcontrol = false;

            while (!ct.IsCancellationRequested)
            {
                var metrics = new List<object>();

                double currentBodyTemperature = minBodyTemperature + rand.NextDouble() * 8;
                double currentHeartrate = minHeartrate + rand.NextDouble() * 13;
                bool light = lightcontrol;
                int fanSpeed = randFanspeed.Next(0, 100);

                bool streetlightStatus = default;


                //Get current time
                var time = TimeOnly.FromDateTime(DateTime.Now);

                //Save on start time and on end time.
                var onStartTime = new TimeOnly(19, 00, 00);
                var onEndTime = new TimeOnly(4, 59, 00);


                //Save off start time and off end time
                var offStartTime = new TimeOnly(5, 00, 00);
                var offEndTime = new TimeOnly(18, 59, 00);


                /// <summary>
                /// Make your decision
                /// </summary>
                if (time.IsBetween(onStartTime, onEndTime))
                {
                    Console.WriteLine("The light is currently turned on.");

                }
                if (time.IsBetween(offStartTime, offEndTime))
                {
                    Console.WriteLine("The light is currently turned off.");
                }

                // var date = DateTime.Now;
                // Console.WriteLine(date.ToString());
                // if ((date.Hour >= 7 && date.ToString("tt") == "PM") || (date.Hour >= 5 && date.ToString("tt") == "AM"))

                // {
                //     streetlightStatus = true;
                //     Console.WriteLine("hello streetlight is on");
                // }

                // if (date.Hour >= 5 && date.ToString("tt") == "AM")

                // {
                //     streetlightStatus = false;
                //     Console.WriteLine("hello streetlight is false");
                //     Console.WriteLine(date.ToString("tt"));
                // }


                var data = new
                {
                    deviceId = "IotSumilator",
                    Body_Temperature = currentBodyTemperature,
                    Heartrate = currentHeartrate,
                    lightsOn = (fanSpeed == 0) ? false : true,
                    electricityMonitor = (fanSpeed == 0) ? false : true,
                    ambientLight = (fanSpeed > 30 || fanSpeed == 0) ? true : false,
                    fanSpeed = fanSpeed,
                    streetlightStatus = streetlightStatus,

                };

                metrics.Add(data);

                // Create displayed sensor in JSON format
                string messageBody = JsonSerializer.Serialize(
                    data);
                Console.WriteLine(messageBody);
                using var message = new Message(Encoding.ASCII.GetBytes(messageBody))
                {
                    ContentType = "application/json",
                    ContentEncoding = "utf-8",
                };

                // Add a custom application property to the message.
                // An IoT hub can filter on these properties without access to the message body.
                message.Properties.Add("Warning Check Body Temperature", (currentBodyTemperature < 36 || currentBodyTemperature > 38.5) ? "true" : "false");

                message.Properties.Add("Warning Check Pulse Rate", (currentHeartrate < 60 || currentHeartrate > 100) ? "true" : "false");

                message.Properties.Add("Warning Streetlight is meant to be OFF", (DateTime.Now.Hour >= 7 && streetlightStatus) ? "false" : "true");

                message.Properties.Add("Warning Streetlight is meant to be ON", (DateTime.Now.Hour <= 5 && streetlightStatus) ? "true" : "false");


                // Send the telemetry message
                await s_deviceClient.SendEventAsync(message);
                Console.WriteLine($"{DateTime.Now} > Sending message: {messageBody}");

                await Task.Delay(3000);
            }
        }
    }
}
