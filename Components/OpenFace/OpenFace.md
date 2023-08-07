# OpenFace

## Summary
This component is based [Intelligent Human Perception Laboratory](https://www.ihp-lab.org/) OpenSense [repository](https://github.com/ihp-lab/OpenSense/tree/master/Components/OpenFace.Windows).

We modified a few things from the OpenSense code removing emitters, forcing some pixel format and we add the heads boundingboxes emitter.

This project must have a project dependency on [OpenFaceInterop](../../Interop/OpenFaceInterop) project.

## Files
* [HeadInfos](src/HeadInfos.cs) containing data structures from https://github.com/ihp-lab/OpenSense/tree/master/Components/OpenFace.Common
* [OpenFaceConfiguration](src/OpenFaceConfiguration.cs) is the configuration class.
* [OpenFace](src/OpenFace.cs) is the main component that interface with *OpenFace*.
* [FaceBlurrer](src/FaceBlurrer.cs) is a proof of concept for anonymising individuals in a video stream.

## Curent issues

## Future works
* Test and optimise performance.
