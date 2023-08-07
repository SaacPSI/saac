# LabJack

## Summary
Project allowing communication with Labjack T-Series devices and the LabJack Digit-Series devices.
See https://labjack.com/pages/support?doc=/software-driver/ljm-users-guide/ for more informations.

## Files
* [LabJackStructures](src/LabJackStructures.cs) contains communication structures.
* [LabJackCoreConfiguration](src/LabJackCoreConfiguration.cs) is the connecting configuration class.
* [LabJackCore](src/LabJackCore.cs) is the internal component doing the link with the device.
* [LabJackSensor](src/LabJackSensor.cs) is the interface component to be used in applications.
* The *dep* folder contains the librairy 

## Curent issues

## Future works
* Simplify and make high level commands.
* A lot of testing.
