# AudioRecording

## Summary
This component allow to records from Rhode  microphones, it split the track into two separate streams of AudioBuffer.
This component have been developped by Arnaud Allemang-Trivalle & Aurélien Lechappé.
This project need a modified version of Psi.Audio.Windows from our frok, in the PsiStudio branch.

## Files
* [User](src/Helpers/User.cs) structure to link a microphone and a channel to a user.
* [Audio Splitter](src/AudioSplitter.cs) class that split the incoming data from Rhode into multiple data stream.
* [Audio Recording Manager](src/AudioRecordingManager.cs) class example to setup the audio recoring.

## Curent issues

## Future works
