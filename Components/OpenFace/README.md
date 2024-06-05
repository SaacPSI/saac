# OpenFace

## Summary
This component is based [Intelligent Human Perception Laboratory](https://www.ihp-lab.org/) OpenSense [repository](https://github.com/ihp-lab/OpenSense/tree/master/Components/OpenFace.Windows).

We modified a few things from the OpenSense code removing emitters, forcing some pixel format and we add the heads boundingboxes emitter.

This project must have a project dependency on [OpenFaceInterop](../../Interop/OpenFaceInterop) project. It is the only project build in .Net6.0.

## Files
* [HeadInfos](src/HeadInfos.cs) containing data structures from https://github.com/ihp-lab/OpenSense/tree/master/Components/OpenFace.Common
* [OpenFaceConfiguration](src/OpenFaceConfiguration.cs) is the configuration class.
* [OpenFace](src/OpenFace.cs) is the main component that interface with *OpenFace*.
* [FaceBlurrer](src/FaceBlurrer.cs) is a proof of concept for anonymising individuals in a video stream.

## Curent issues

## Future works
* Test and optimise performance.

## Example
           static void OpenFace(Pipeline p)
            {

                AzureKinectSensor webcam = new AzureKinectSensor(p);

                OpenFaceConfiguration configuration = new OpenFaceConfiguration("./");
                configuration.Face = false;
                configuration.Eyes = false;
                configuration.Pose = false;
                OpenFace.OpenFace facer = new OpenFace.OpenFace(p, configuration);
                webcam.ColorImage.PipeTo(facer.In);

                FaceBlurrer faceBlurrer = new FaceBlurrer(p, "Blurrer");
                facer.OutBoundingBoxes.PipeTo(faceBlurrer.InBBoxes);
                webcam.ColorImage.PipeTo(faceBlurrer.InImage);

                var store = PsiStore.Create(p, "Blurrer", "D:\\Stores");
            }