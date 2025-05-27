# PipelineServices

## Introduction
Theirs are two main classes :
*[Rendez Vous Pipeline](src/RendezVousPipeline.cs) handling network and recording.
*[Replay Pipeline](src/ReplayPipeline.cs) handling dataset that will be replayed.  

## Replay through RendezVous 

To be able to use streams from a dataset, example replaying a Unity session. You need to excecute the following steps:
* Instantiate a ReplayPipelineConfiguration and set it up with the dataset you want to replay.
* Instantiate a ReplayPipeline with the configuration and load the dataset and connectors.
* Instantiate a RendezVousPipelineConfiguration and setup the network and incoming streams configuration.
* Instantiate a RendezVousPipeline with the ReplayPipeline Pipeline and the connectors, the configuration.
* Call the method GenerateTCPProcessFromConnectors() with the session name you want to replay (if you want all streams to be exposed).
    
        ReplayPipelineConfiguration replayConfig = new ReplayPipelineConfiguration();
        replayConfig...

        ReplayPipeline replayPipeline = new ReplayPipeline(replayConfig);
        replayPipeline.LoadDatasetAndConnectors();

        RendezVousPipelineConfiguration configuration = new RendezVousPipelineConfiguration();
        configuration...

        RendezVousPipeline rdvPipeline = new RendezVousPipeline(replayPipeline.Pipeline, configuration, "Server", null, null, replayPipeline.Connectors);
        rdvPipeline.GenerateTCPProcessFromConnectors("Unity", 15651);

In order to be able to synchronize time over \psi applications, the RendezVousPipeline need to use the Pipeline from the ReplayPipeline (that will mangage the run of the application). And if the RendezVousPipeline need to expose streams inside the RendezVous system the streams loaded in the ReplayPipeline should be given in the instantiation.
