# Biopac

## Summary
This component is based [Intelligent Human Perception Laboratory](https://www.ihp-lab.org/) OpenSense [repository](https://github.com/ihp-lab/OpenSense/tree/master/Components/Biopac).

We added the possibility start the acquisition without receiving the data. That means AcqKnowledge store the collected data synchronised with the rest of the application. 
This functionality was added due to an overflow in data transfert making AcqKnowledge stop the acquisition. 

This project must have a project dependency on [BiopacInterop](../../Interop/BiopacInterop/) project.

## Files
* [Biopac](src/Biopac.cs) is the component to communicate with AcqKnowledge
* [GTLoader](src/GTLoader.cs) allow to write AcqKnowledge gtl file into a dataset with an automatic synchronisation (from store present in the dataset).

## Curent issues

## Future works
* Fixing acquisition issue (if it's coming from our side).
* Improve GTLoader component to be used without dataset synch.
