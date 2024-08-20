# AttentionMeasures

## Summary
Project containing components related toeye tracking and attention measure.  

## Files

## Curent issues

## Future works
* Finishing the rating of attention.

Code from Mael Chakma

Sample:

        namespace AttMesPipeline
        {
            internal class Program
            {
                private static void LaunchPipeline()
                {
                    //Configuration of the pipeline
                    RendezVousPipelineConfiguration configuration = new RendezVousPipelineConfiguration();
                    configuration.AutomaticPipelineRun = true;
                    configuration.RendezVousHost = "192.168.84.223";
                    configuration.TopicsTypes.Add("LeftEye", typeof(Tuple<System.Numerics.Vector3, System.Numerics.Vector3>));
                    configuration.TopicsTypes.Add("EyeTracking", typeof(Dictionary<ETData, IEyeTracking>));
                    configuration.TopicsTypes.Add("EyeTrackingEvent", typeof(EyeTrackingEvent));
                    configuration.TypesSerializers.Add(typeof(Dictionary<ETData, IEyeTracking>), new PsiFormatEyeTracking());
                    configuration.TypesSerializers.Add(typeof(EyeTrackingEvent), new PsiFormatEyeTrackingEvent());

                    Console.WriteLine("Debug ? (y/n)");
                    string input = Console.ReadLine();
                    if (input == "y" || input == "Y") { configuration.Debug = true; }
                    else if (input == "n" || input == "N") { configuration.Debug = false; }

            
                    configuration.DatasetPath = "C:/Users/maelc/Documents/MaelChakma/Stores/RecordStores/";// args[0];
                    configuration.DatasetName = "Unity.pds";// args[1];
                    RendezVousPipeline server = new RendezVousPipeline(configuration);

                    // Start the server
                    server.Start();
                    server.RunPipeline();

                    // Waiting for an out key
                    Console.WriteLine("Press any key to stop the application.");
                    Console.ReadLine();

                    // Stop correctly the server.
                    server.Stop();
                }



                private static void ReplayAndProcess()
                {
                    Pipeline p = Pipeline.Create();
                    Microsoft.Psi.Data.Dataset dataset = Microsoft.Psi.Data.Dataset.Load("C:/Users/maelc/Documents/MaelChakma/Stores/SavedStores/Pretest_1_A/Unity.pds");
                    IProducer<Dictionary<ETData, IEyeTracking>> eyeTrackingProducer = null;
                    IProducer<EyeTrackingEvent> eyeTrackingEventProducer = null;
                    foreach (var session in dataset.Sessions)
                    {
                        foreach (var item in session.Partitions)
                        {
                            var store = PsiStore.Open(p, item.StoreName, item.StorePath);
                            switch (item.StoreName)
                            {
                                case ("Unity-LeftEye"):
                                    break;
                                case ("Unity-EyeTracking"): 
                                    eyeTrackingProducer = store.OpenStream<Dictionary<ETData, IEyeTracking>>("EyeTracking");
                                    break;
                                case ("Unity-EyeTrackingEvent"):
                                    eyeTrackingEventProducer = store.OpenStream<EyeTrackingEvent>("EyeTrackingEvent");
                                    break;

                            }
                        }
                    }

                    var processedDataStore = PsiStore.Create(p, "ProcessedData", "C:/Users/maelc/Documents/MaelChakma/Stores/ProcessedStores/");

                    //Creating variables for components
                    TimeSpan slidingWindow = TimeSpan.FromSeconds(1);
                    var timer = Timers.Timer(p,TimeSpan.FromMilliseconds(100));

                    //Creating components
                    EyeMovementClassifier eyeMovementClassifier = new EyeMovementClassifier(p);
                    FixCount fixCount = new FixCount(p, slidingWindow);
                    MeanFixDuration meanFixDuration = new MeanFixDuration(p, slidingWindow);
                    RatioSaccFix ratioSaccFix = new RatioSaccFix(p, slidingWindow);
                    SaccRate saccRate = new SaccRate(p, slidingWindow);
                    GazeAgitation gazeAgitation = new GazeAgitation(p);
                    ObjectsRanker objectsRanker = new ObjectsRanker(p);
                    ObjectRankDisplay objectRankDisplay1 = new ObjectRankDisplay(p, 1); 
                    ObjectRankDisplay objectRankDisplay2 = new ObjectRankDisplay(p, 2); 
                    ObjectRankDisplay objectRankDisplay3 = new ObjectRankDisplay(p, 3);
                    FixCountByObjects fixCountByObjects = new FixCountByObjects(p, slidingWindow);

            
                    //EyeTrackingEvents
                    eyeTrackingEventProducer.Select(x => x.eventType.ToString()).Write("EyeTrackingEvents", processedDataStore);

                    //EyeMovementClassifier
                    eyeTrackingProducer.PipeTo(eyeMovementClassifier.In);
           
                    //Displaying info out of EyeMovementClassifier
                    eyeMovementClassifier.Out.Select(x => x.isFixation).Write("FixationOrSaccade", processedDataStore);
                    eyeMovementClassifier.Out.Select(x => x.GetDuration()).Write("EyeMovementDuration", processedDataStore);
                    eyeMovementClassifier.Out.Where(x => x.isFixation).Select(x => x.GetDuration()).Write("FixationDuration", processedDataStore);
                    eyeMovementClassifier.Out.Select(x => x.messagesCount).Write("EyeMovementMessageCount", processedDataStore);
                    eyeMovementClassifier.Out.Where(x => !x.isFixation).Select(x => x.GetSaccAmplitude()).Write("SaccAmplitude", processedDataStore);

                    //FixCount
                    eyeMovementClassifier.Out.PipeTo(fixCount.In);
                    timer.Out.PipeTo(fixCount.TimerIn);
                    fixCount.Out.Write("FixCount", processedDataStore);

                    //MeanFixDuration
                    eyeMovementClassifier.Out.PipeTo(meanFixDuration.In);
                    timer.Out.PipeTo(meanFixDuration.TimerIn);
                    meanFixDuration.Out.Write("MeanFixDuration", processedDataStore);

                    //RatioSaacFix
                    eyeMovementClassifier.Out.PipeTo(ratioSaccFix.In);
                    timer.Out.PipeTo(ratioSaccFix.TimerIn);
                    ratioSaccFix.Out.Write("RatioSaccFix", processedDataStore);

                    //SaccRate
                    eyeMovementClassifier.Out.PipeTo(saccRate.In);
                    timer.Out.PipeTo(saccRate.TimerIn);
                    saccRate.Out.Write("SaccRate", processedDataStore);

                    //GazeAgitation
                    var joined = fixCount.Out.Join(meanFixDuration.Out).Join(ratioSaccFix.Out).Join(saccRate.Out);
                    //For GazeAgitation testing, uncomment the next line
                    //joined = Generators.Sequence(p, (6, TimeSpan.FromMilliseconds(10), 0.1, 0.3), x => (6, TimeSpan.FromMilliseconds(10), 0.1, 0.3), 100, TimeSpan.FromMilliseconds(100));
                    joined.PipeTo(gazeAgitation.In);
                    gazeAgitation.Out.Select(x => (int)x).Write("GazeAgitation", processedDataStore);

                    //ObjectsRanking
                    eyeTrackingProducer.PipeTo(objectsRanker.In);
                    timer.Out.PipeTo(objectsRanker.TimerIn);
                    objectsRanker.Out.PipeTo(objectRankDisplay1.In);
                    objectsRanker.Out.PipeTo(objectRankDisplay2.In);
                    objectsRanker.Out.PipeTo(objectRankDisplay3.In);
                    objectRankDisplay1.ScoreOut.Write("Object1Score", processedDataStore);
                    objectRankDisplay1.NameOut.Write("Object1Name", processedDataStore);            
                    objectRankDisplay2.ScoreOut.Write("Object2Score", processedDataStore);
                    objectRankDisplay2.NameOut.Write("Object2Name", processedDataStore);            
                    objectRankDisplay3.ScoreOut.Write("Object3Score", processedDataStore);
                    objectRankDisplay3.NameOut.Write("Object3Name", processedDataStore);

                    //FixCountByObjects
                    eyeMovementClassifier.Out.PipeTo(fixCountByObjects.In);
                    timer.Out.PipeTo(fixCountByObjects.TimerIn);

                    ///////TEST
                    TestInput testInput = new TestInput(p);
                    eyeTrackingProducer.Out.PipeTo(testInput.In);
                    testInput.Out.Write("TestInput",processedDataStore);



                    p.RunAsync(ReplayDescriptor.ReplayAllRealTime);
                    // Waiting for an out key
                    Console.WriteLine("Press any key to stop the application.");
                    Console.ReadLine();

                    // Stop correctly the server.
                    p.Dispose();
                }


                static void Main(string[] args)
                {
                    Console.WriteLine("Press R to record data or P to process data");
                    string input = Console.ReadLine();
                    if (input == "r" || input == "R") { LaunchPipeline(); }
                    else if (input == "p" || input == "P") { ReplayAndProcess(); }
                    return;
                }
            }
        }