# Azure MQTT sample using BMP280 barometric pressure sensor and nanoFramework Azure IoT SDK Library
![](image.jpg)

This example demonstrates how to use the Azure IoT SDK library and BMP280 pressure sensor.
> Note:  
> 1. This example is configured for ESP32 but can be easily reconfigured for any nanoFramework-supported board with WiFi and I2C bus.
> 1. Before building, rename `Config_template` to `Config` and provide the required setting values.
> 1. BMP280 sensor on the picture has non-standard pinout. Typical pinout is: black GND, red Vcc, yellow TX/SDA, white RX/SCL

## Prerequisites
* Visual Studio 2022 with nanoFramework extension
* nanoFramework-supported board with WiFi and I2C bus

## Board preparation
From command prompt execute:  
* `dotnet tool install -g nanoff`
* `nanoff --platform esp32 --serialport COM7 --update` (replace COM7 with your com port)  

For more info and instructions on how to prepare other boards refer to: [nanoframework - getting started](https://docs.nanoframework.net/content/getting-started-guides/getting-started-managed.html)

## Source Code
https://github.com/tdjastrzebski/ESP32_AZURE

## References
* [nanoframework website](https://www.nanoframework.net)
* [nanoframework GitHub](https://github.com/nanoframework)
* [nanoFramework Network helpers](https://github.com/nanoframework/System.Device.Wifi)
* [nanoFramework Azure IoT SDK](https://github.com/nanoframework/nanoFramework.Azure.Devices)
* [Azure IoT documentation for MQTT](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-mqtt-support)
* [nanoFramework.IoT.Device Bmxx80 devices](https://github.com/nanoframework/nanoFramework.IoT.Device/tree/develop/devices/Bmxx80)
