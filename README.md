# SAAC
(System As A Collaborator)

Project using https://github.com/microsoft/psi/ framework.

.NET 4.8 framework projects.

Four folders:
- Applications for ready to use projects see [Applications](Applications/README.md) for more details.
- Components containing projects regrouping components by dependencies or uses.
- Interop containing projects that wrapper of third part librairy see [Interop](Interop/README.md) for more details.
- Dependencies containing third part librairies for interop wrappers.

## Installation

See the detailed [Installation Guide](https://github.com/SaacPSI/saac/wiki/Installation) in the wiki for complete setup instructions.

**Quick setup:**
* Clone the \psi repo [fork](https://github.com/SaacPSI/psi) and build the PsiStudio branch in **Release mode**.
* Psi nuget packages will be output in `builds/PsiPackages`, add this folder to your nuget repository configuration.
* Clone this repository and build the solution (target: x64).

### Developer Setup (Optional)

#### Symbolic Links for Shared Files

Some files are shared across multiple applications (e.g., `UiGenerator.cs`). Developers can optionally use symbolic links for easier editing:

```powershell
# Run from repository root (requires Administrator or Developer Mode on Windows)
.\scripts\Setup-DevSymlinks.ps1

# To remove symlinks
.\scripts\Setup-DevSymlinks.ps1 -Remove
```

**Note:** This is **optional**. The repository works perfectly fine without symlinks. Other contributors cloning the repository will get regular file copies automatically.

**To enable Developer Mode on Windows 10/11** (to create symlinks without Administrator privileges):
1. Open **Settings** → **Privacy & security** → **For developers**
2. Turn on **Developer mode**

## Documents
* [\psi in Unity](PsiInUnity.md)
* [Remote Exporter Modification](RemoteExporterModification.md)
* [PsiSutio modifications](PsiStudioModifications.md)

## Roadmap
Developping generic applications

## Project status
In production.

## Authors and acknowledgment
- Cédric Dumas cedric.dumas@imt-atlantique.fr
- Mathieu Chollet mathieu.chollet@imt-atlantique.fr
- Alexandre Kabil alexandre.kabil@lisn.upsaclay.fr
- Aurélien Lechappé aurelien.lechappe@imt-atlantique.fr
- Arnaud Allemang-Trivalle arnaud.allemang-trivalle@imt-atlantique.fr
- Aurélien Milliat aurelien.milliat@gmail.com

## Technical Report
A description of the framework and goals of the project can be found [here](https://ieeexplore.ieee.org/abstract/document/11164117). Please cite as:

```text
@INPROCEEDINGS{11164117,
  author={Lechappé, Aurélien and Milliat, Aurélien and Kabil, Alexandre and Chollet, Mathieu and Dumas, Cédric},
  booktitle={2025 IEEE 21st International Conference on Automation Science and Engineering (CASE)}, 
  title={System As A Collaborator (SAAC): a Framework for Modeling, Capturing and Augmenting Collaborative Activities in Extended Reality}, 
  year={2025},
  volume={},
  number={},
  pages={759-765},
  keywords={Three-dimensional displays;Speech analysis;Extended reality;Software architecture;Soft sensors;Collaboration;Virtual environments;Systems architecture;Signal processing;Real-time systems},
  doi={10.1109/CASE58245.2025.11164117}}
```

## License
CeCILL-C see [LICENSE.md](LICENSE.md)
