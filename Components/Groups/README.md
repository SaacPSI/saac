# Groups

## Summary
Project containing components for groups detections, the following diagram explains the hierarchy of components:  

![Groups](docs/groups.png)

## Files

### Detectors
* [Entry Groups Detector](src/EntryGroupsDetector.cs)
* [Entry Groups Detector Configuration](src/EntryGroupsDetectorConfiguration.cs)
* [Flock Group Intersection](src/FlockGroupIntersection.cs)
* [Instant Groups Detector](src/InstantGroupsDetector.cs)
* [Instant Groups Detector Configuration](src/InstantGroupsDetectorConfiguration.cs)
* [Integrated Groups Detector](src/IntegratedGroupsDetector.cs)
* [Integrated Groups Detector Configuration](src/IntegratedGroupsDetectorConfiguration.cs)
* [Simplified Flock Group](src/SimplifiedFlockGroup.cs)
* [Simplified Flock Groups Detector](src/SimplifiedFlockGroupsDetector.cs)
* [Simplified Flock Groups Detector Configuration](src/SimplifiedFlockGroupsDetectorConfiguration.cs)

### Graph Classes
* [Edge Width Mode](src/GraphClass/EdgeWidthMode.cs) - Enumeration for edge width display modes
* [Individual Pair Characteristics](src/GraphClass/IndividualPairCharacteristics.cs) - Characteristics for person pairs
* [Node Size Mode](src/GraphClass/NodeSizeMode.cs) - Enumeration for node size display modes
* [Person Edge](src/GraphClass/PersonEdge.cs) - Edge representation between persons
* [Person Group](src/GraphClass/PersonGroup.cs) - Group representation
* [Person Node](src/GraphClass/PersonNode.cs) - Node representation for persons

### Helpers
* [Helpers](src/Helpers/Helpers.cs) contains methods for ID generation and group simplifications.

## Current issues

## Future works
