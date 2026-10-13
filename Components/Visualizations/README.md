# Visualizations

## Summary
This project contains visualizations for PsiStudio. It needs, as NuGet packages, two projects built from the \psi sources (https://github.com/microsoft/psi/issues/301).  

## Use in PsiStudio
Add `Visualizations.dll` to "Additional Assemblies" in the settings of PsiStudio and start PsiStudio again; then right-click a stream and choose the visualization.
PsiStudio looks for the assemblies a plugin refers to in its own folder first: keep no copy of `Groups.dll`, of `Visualizations.dll` or of another SAAC assembly there.

## Visualizations of a group
Two visualizations show a group of any number of participants from what the CollaborationIndices component stores: what the group did over the last window, and what it is doing at the instant of the cursor. They are distributed separately from this repository; section 8 of `docs/wiki/CollaborationIndices-Component.md` says which streams they read.

## Files
* [Augmented Body List Visualization Object](src/VisualizationObjects/AugmentedBodyListVisualizationObject.cs)
* [Augmented Body Visualization Object](src/VisualizationObjects/AugmentedBodyVisualizationObject.cs)
* [Augmented Coordinate System List Visualization Object](src/VisualizationObjects/AugmentedCoordinateSystemListVisualizationObject.cs)
* [Augmented Coordinate System Visualization Object](src/VisualizationObjects/AugmentedCoordinateSystemVisualizationObject.cs)
* [Augmented Skeleton Visualization Object](src/VisualizationObjects/AugmentedSkeletonVisualizationObject.cs)
* [Labeled Line2 D Visualization Object](src/VisualizationObjects/LabeledLine2DVisualizationObject.cs)
* [Labeled Point2 D Visualization Object](src/VisualizationObjects/LabeledPoint2DVisualizationObject.cs)
* [Position Orientation List Visualization Object](src/VisualizationObjects/PositionOrientationListVisualizationObject.cs)
* [Position Orientation Visualization Object](src/VisualizationObjects/PositionOrientationVisualizationObject.cs)
* [Simplified Body List Visualization Object](src/VisualizationObjects/SimplifiedBodyListVisualizationObject.cs)
* [Simplified Body Visualization Object](src/VisualizationObjects/SimplifiedBodyVisualizationObject.cs)
* [Simplified Flock Group Dictionnary Visualisation Object](src/VisualizationObjects/SimplifiedFlockGroupDictionnaryVisualisationObject.cs)
* [Simplified Flock Group Visualisation Object](src/VisualizationObjects/SimplifiedFlockGroupVisualisationObject.cs)
* [Social Graph XY Visualization Object](src/VisualizationObjects/SocialGraphXYVisualizationObject.cs)

## Current issues

## Future works
* Integrating 2D visualizations displaying positions of HMD users in virtual environment.
