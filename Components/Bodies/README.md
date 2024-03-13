# Bodies

## Summary
Project containing components related to body tracking. We have tried to set a normalized body definition to be able to use both Kinect Azure and Nuitrack skeletons.  

## Files
* [Bodies Converter](src/BodiesConverter.cs) convert Azure Kinect and/or Nuitrack bodies into [Simplified Body](src/data/SimplifiedBody.cs).
* [Bodies Identification](src/BodiesIdentification.cs) component that try to mitigate new ids for bodies already known, usually it comes from oculsion or the individual leaving the field of tracking.
* [Bodies Identification Configuration](src/BodiesIdentificationConfiguration.cs) configuration class for [Bodies Identification](src/BodiesIdentification.cs).
* [Bodies Selection](src/BodiesSelection.cs) select the best body from two cameras and apply a transformation matrix to provide positions on the camera master coordinate space.
* [Bodies Selection Configuration](src/BodiesSelectionConfiguration.cs) configuration class for [Bodies Selection](src/BodiesSelection.cs)
* [Body Postures Detector](src/BodyPosturesDetector.cs) component that provide information about posture on frame level.
* [Body Postures Detector Configuration](src/BodyPosturesDetectorConfiguration.cs) configuration class for [Body Postures Detector](src/BodyPosturesDetector.cs)
* [Calibration By Bodies](src/CalibrationByBodies.cs) low cost calibration component that provide the transformation matrix between two camera, using a single tracked body.
* [Calibration By Bodies Configuration](src/CalibrationByBodiesConfiguration.cs) configuration class for [Calibration By Bodies](src/CalibrationByBodies.cs)
* [Hands Proximity Detector](src/HandsProximityDetector.cs) is inspired from [OpenSense ArmsProximityDetector](https://github.com/ihp-lab/OpenSense/blob/master/Components/BodyGestureDetectors/ArmsProximityDetector.cs), it detect hand proximity of bodies and give wich hands.
* [Hands Proximity Detector Configuration](src/HandsProximityDetectorConfiguration.cs) configuration class for [Hands Proximity Detector](src/HandsProximityDetector.cs)
* [Simple Bodies Position Extraction](src/SimpleBodiesPositionExtraction.cs) basic component that emit the position of the selected joint as a position for bodies.
* [Simple Bodies Position Extraction Configuration](src/SimpleBodiesPositionExtractionConfiguration.cs) configuration class for [Simple Bodies Position Extraction](src/SimpleBodiesPositionExtraction.cs)
* [Calibration Statistics](src/statistics/CalibrationStatistics.cs) generate csv file containing statiscs to evaluate the calibration.
* [Bodies Statistics](src/statistics/BodiesStatistics.cs) components generate csv file containing statiscs to evaluate the camera and the identification by bones.
* [Leaning Body](src/data/LeaningBody.cs) and [Learned Body](src/data/LearnedBody.cs) are used in [Bodies Identification](src/BodiesIdentification.cs).
* [Helpers](src/data/Helpers.cs) contains various general process to handle bodies.

## Curent issues

## Future works
* Integrate more than two cameras for [Bodies Selection](src/BodiesSelection.cs), [Calibration By Bodies](src/CalibrationByBodies.cs) and [Calibration Statistics](src/statistics/CalibrationStatistics.cs)
* Add high level movement detection.
* Add more postures detection.

## Example
        static void testBodies(Pipeline p)
        {
            AzureKinectSensorConfiguration configKinect = new AzureKinectSensorConfiguration();
            configKinect.DeviceIndex = 0;
            configKinect.BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration();
            AzureKinectSensor sensor = new AzureKinectSensor(p, configKinect);

            Bodies.BodiesConverter bodiesConverter = new Bodies.BodiesConverter(p);

            Bodies.HandsProximityDetectorConfiguration configHands = new Bodies.HandsProximityDetectorConfiguration();
            configHands.IsPairToCheckGiven = false;
            Bodies.HandsProximityDetector detector = new Bodies.HandsProximityDetector(p, configHands);


            Bodies.BodyPosturesDetectorConfiguration configPostures = new Bodies.BodyPosturesDetectorConfiguration();
            Bodies.BodyPosturesDetector postures = new Bodies.BodyPosturesDetector(p, configPostures);

            sensor.Bodies.PipeTo(bodiesConverter.InBodiesAzure);

            bodiesConverter.Out.PipeTo(detector.In);
            bodiesConverter.Out.PipeTo(postures.In);

            detector.Out.Do((m, e) => { 
                foreach (var data in m)
                {
                    foreach(var item in data.Value)
                        Console.WriteLine($"{data.Key} - {item}");
                } });

            postures.Out.Do((m, e) => {
                foreach (var data in m)
                {
                    foreach (var item in data.Value)
                        Console.WriteLine($"{data.Key} - {item}");
                }
            });
        }