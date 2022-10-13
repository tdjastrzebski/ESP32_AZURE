using AzureIoT_BMP280;
using Iot.Device.Bmxx80;
using Iot.Device.Bmxx80.ReadResult;
using nanoFramework.Azure.Devices.Client;
using nanoFramework.Azure.Devices.Shared;
using nanoFramework.Networking;
using System;
using System.Device.Gpio;
using System.Device.I2c;
using System.Device.Wifi;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

const string IntervalSeconds = "intervalSeconds";

var gpio = new GpioController();
var led = gpio.OpenPin(4, PinMode.Output); // note: LED connected to GPIO pin 4

var i2cSettings = new I2cConnectionSettings(1, Bmp280.DefaultI2cAddress); // note: using I2C bus 1, GPIO pins: SDA 18, SCL 19
var i2cDevice = I2cDevice.Create(i2cSettings);
var bmp280Sensor = new Bmp280(i2cDevice)
{
    TemperatureSampling = Sampling.LowPower,
    PressureSampling = Sampling.UltraHighResolution
};

DeviceClient azureIoT = new(Config.IotHubAddress, Config.DeviceID, Config.SasKey, azureCert: new X509Certificate(Resources.GetBytes(Resources.BinaryResources.AzureRoot)));

Bmp280ReadResult readResult;
bool isTwinUpdated = false;
int intervalSeconds = 60;

while (true) {
    DateTime startTime = DateTime.UtcNow;
    DateTime continueTime = startTime.AddSeconds(intervalSeconds);
    uint blinkPattern = 0xF0F0F0F0;
    int patternShift = 0;

    try {
        led.Write(PinValue.High);

        // get temperature and pressure measurements
        readResult = bmp280Sensor.Read();
        // use to simulate the sensor measurement:
        //readResult = new Bmp280ReadResult(new UnitsNet.Temperature(21.0, UnitsNet.Units.TemperatureUnit.DegreeCelsius), new UnitsNet.Pressure(1020.0, UnitsNet.Units.PressureUnit.Hectopascal));

        if (readResult != null) {
            Debug.WriteLine($"Measurement result obtained. T={readResult.Temperature.DegreesCelsius:f2}°C, P={readResult.Pressure.Hectopascals:f2}hPa");
        } else {
            Debug.WriteLine("Measurement result unavailable.");
            continue;
        }

        if (WifiNetworkHelper.Status != NetworkHelperStatus.NetworkIsReady) {
            if (WifiNetworkHelper.ConnectDhcp(Config.Ssid, Config.Password, WifiReconnectionKind.Automatic, requiresDateTime: true, token: new CancellationTokenSource(10_000).Token)) {
                Debug.WriteLine("WiFi connection succeeded.");
            } else {
                Debug.WriteLine("WiFi connection failed.");
                continue;
            }
        }

        if (!azureIoT.IsConnected) {
            if (azureIoT.Open()) {
                Debug.WriteLine("Azure connection succeeded.");
            } else {
                Debug.WriteLine("Azure connection failed.");
                continue;
            }
        }

        // get the device twin
        var twin = azureIoT.GetTwin(new CancellationTokenSource(5_000).Token);

        if ((twin != null) && (twin.Properties.Desired.Contains(IntervalSeconds))) {
            intervalSeconds = (int)twin.Properties.Desired[IntervalSeconds];
            continueTime = startTime.AddSeconds(intervalSeconds);
        }

        if (!isTwinUpdated) {
            // update reported properties
            TwinCollection reported = new();
            reported.Add(IntervalSeconds, intervalSeconds);
            reported.Add("firmware", "nanoFramework");
            reported.Add("firmwareVersion", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            azureIoT.UpdateReportedProperties(reported, new CancellationTokenSource(5_000).Token);
            isTwinUpdated = true;
        }

        // send data to Azure IoT Hub
        string payload = $"{{\"temperature\":{readResult.Temperature.DegreesCelsius:f2},\"pressure\":{readResult.Pressure.Hectopascals:f2}}}";

        if (azureIoT.SendMessage(payload, new CancellationTokenSource(5_000).Token)) {
            Debug.WriteLine("Azure message sent.");
            blinkPattern = 0xF0F0CCC0;
        } else {
            Debug.WriteLine("Azure message send failed.");
            continue;
        }
    } catch (Exception ex) {
        Debug.WriteLine("Unhandled exception:");
        Debug.WriteLine(ex.Message);
        blinkPattern = 0xAAAAAAAA;
    }

    do {
        // wait for the next interval blinking the LED
        led.Write(((blinkPattern >> patternShift) & 0x1) == 1 ? PinValue.High : PinValue.Low);
        patternShift++;
        patternShift %= 32;
        Thread.Sleep(100);
    } while (DateTime.UtcNow < continueTime);
}
