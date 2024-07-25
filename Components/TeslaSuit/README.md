# Groups

## Summary
Project containing components related to [TeslaSuit](https://teslasuit.io/) interoperability with \\psi. The project is divided in 2 parts, the Unity script to collect data and the 'server side' components to receive and process the data. 

## Files
In **Unity** folder :
* [Psi Tesla Suit](Unity/PsiTeslaSuit.unitypackage) package with scripts and exemple scene.
* [Psi Exporter Ts Motion](Unity/PsiExporterTsMotion.cs) mocap exporter. 
* [Psi Exporter Ts P P G](Unity/PsiExporterTsPPG.cs) PPG exporter.
* [Psi Exporter Ts Raw P P G](Unity/PsiExporterTsRawPPG.cs) Raw PPG exporter.
* [Psi Ts Hapic Player](Unity/PsiTsHapicPlayer.cs) exporter fot haptics events.
* [Psi Format Hapic Params](Unity/PsiFormatHapicParams.cs)
* [Psi Format Hapic Playable](Unity/PsiFormatHapicPlayable.cs)
* [Psi Format Ts Motion](Unity/PsiFormatTsMotion.cs)
* [Psi Format Ts P P G](Unity/PsiFormatTsPPG.cs)
* [Psi Format Ts Raw P P G](Unity/PsiFormatTsRawPPG.cs)

In **src** folder:
* [Psi Format Hapic Params](src/Formats/PsiFormatHapicParams.cs)
* [Psi Format Hapic Playable](src/Formats/PsiFormatHapicPlayable.cs)
* [Psi Format Ts Motion](src/Formats/PsiFormatTsMotion.cs)
* [Psi Format Ts P P G](src/Formats/PsiFormatTsPPG.cs)
* [Psi Format Ts Raw P P G](src/Formats/PsiFormatTsRawPPG.cs)
* [Haptic Params](src/Helpers/HapticParams.cs) structure from instant hapitic event.
* [Haptic Playable](src/Helpers/HapticPlayable.cs) structure for recorded hapitic event. 

## Curent issues

## Future works
* Integrate haptics activation from \\psi.
