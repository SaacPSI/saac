# Bhaptics

## Summary
Project containing components related to [bHaptics](https://www.bhaptics.com/) interoperability with \psi. The project is split into two parts: Unity scripts that capture haptic events at runtime, and \psi formats/helpers used to serialize and transport those events to the server application.

## Setup
> **[BhapticsLibrary](src/Unity/BhapticsLibrary.cs)** is a modified version of the standard bHaptics SDK script and **must replace** the existing one in your Unity project in order to expose the PSI delegate hooks used by the exporters.

> Add **only** `PsiBhapticsManager` to your Unity scene – it will automatically subscribe to the bHaptics callbacks and dispatch data through the PSI exporters.

## Configuration
`Bhaptics.json` is an example configuration file. Topic names must be adapted to match your setup. It is used at build time to connect stream topics with the **ServerApplication** – see the wiki for more details: [External Configuration](https://github.com/SaacPSI/saac/wiki/ServerApplication#external-configuration).

## Files

In the **Unity** folder:
* [BhapticsLibrary](src/Unity/BhapticsLibrary.cs) – modified bHaptics SDK library that exposes three PSI delegates: `OnHapticPlay`, `OnMotorPlay` and `OnPauseResumeStop`.
* [PsiBhapticsManager](src/Unity/PsiBhapticsManager.cs) – Unity `MonoBehaviour` to add to your scene. Subscribes to the bHaptics callbacks and forwards data to the PSI exporters using the configured topic prefix.
* [PsiExporterHapticPlay](src/Unity/PsiExporterHapticPlay.cs) – PSI exporter for `HapticPlay` events.
* [PsiExporterMotorPlay](src/Unity/PsiExporterMotorPlay.cs) – PSI exporter for `MotorPlay` events.
* [PsiExporterPauseResumeStop](src/Unity/PsiExporterPauseResumeStop.cs) – PSI exporter for `PauseResumeStop` events.

In the **Formats** folder:
* [PsiFormatHapticPlay](src/Formats/PsiFormatHapticPlay.cs) – binary serialization format for `HapticPlay`.
* [PsiFormatMotorPlay](src/Formats/PsiFormatMotorPlay.cs) – binary serialization format for `MotorPlay`.
* [PsiFormatPauseResumeStop](src/Formats/PsiFormatPauseResumeStop.cs) – binary serialization format for `PauseResumeStop`.

In the **Helpers** folder:
* [HapticPlay](src/Helpers/HapticPlay.cs) – data structure for a pattern-based haptic play event.
* [MotorPlay](src/Helpers/MotorPlay.cs) – data structure for a direct motor-level haptic event.
* [PauseResumeStop](src/Helpers/PauseResumeStop.cs) – data structure for controlling the playback state of a running haptic event.

## Current issues

## Future works
