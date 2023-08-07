# Interop

Folder containing projects that wrapper of third part librairy used in our components. 
We try to use the Nuget system as much as we can.

## Projects

### BiopacInterop
This project is based [Intelligent Human Perception Laboratory](https://www.ihp-lab.org/) OpenSense [repository](https://github.com/intelligent-human-perception-laboratory/OpenSense/).

We added the possibility start the acquisition without receiving the data. That means AcqKnowledge store the collected data synchronised with the rest of the application. 
This functionality was added due to an overflow in data transfert making AcqKnowledge stop the acquisition. 
