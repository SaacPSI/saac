Guidelines

- Create a Unity project under version 6.0.X (version 6.0.62f1 for us)
- Import the Unity package 'UnityPsiExample'
- Import the TextMeshPro package
- Change the IP address according to the one used on the ServerApplication
> Update the IP address in the 'PsiPipelineManager' script on the PsiManager object of the scene
- Go to Project Settings > Player > Other Settings > Script Compilation >
> Add "PSI_TCP_STREAMS" to 'Scripting Define Symbols'
- Various exporters exist in the scene: Continuous and Discrete events.
> 'Continuous Events' include 'User_Head' and 'SpecificArea_PositionOrientation' with various frequencies
> 'Discrete Events' include 'TaskEvent' and 'SendMessage' corresponding to specific events
=> For each exporter, check that Export Type is set to 'TCP Writer'

TopicName must be the same as the one defined in the JSON file added in the ServerApplication
