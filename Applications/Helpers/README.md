# Applications Helpers

This folder contains utility files shared between multiple applications in the SAAC project.

## UiGenerator.cs

Utility class for generating WPF user interface elements.

### Usage

This file is used by the following projects via symbolic links:
- `CameraRemoteApp`
- `ServerApplication`
- `VideoRemoteApp`
- `WhisperRemoteApp`

### Main Features

- WPF control generation (Label, Button, TextBox, CheckBox, etc.)
- Input validation (IP, URI, file paths, numbers)
- Grid manager
- Folder picker (FolderPicker) with native Windows support

### Important Notes

- The file uses both `System.Windows.Shapes` and `System.IO`, so references to `Path` must be explicitly qualified with `System.IO.Path`
- The code is documented with XML comments for IntelliSense
- Changes made to this file affect all projects that use it

### Symbolic Link Structure

```
Applications\
├── Helpers\
│   └── UiGenerator.cs (source file)
├── CameraRemoteApp\
│   └── UiGenerator.cs -> ..\Helpers\UiGenerator.cs (symbolic link)
├── ServerApplication\
│   └── UiGenerator.cs -> ..\Helpers\UiGenerator.cs (symbolic link)
├── VideoRemoteApp\
│   └── UiGenerator.cs -> ..\Helpers\UiGenerator.cs (symbolic link)
└── WhisperRemoteApp\
    └── UiGenerator.cs -> ..\Helpers\UiGenerator.cs (symbolic link)
```

### History

This file was created by merging individual versions of `UiGenerator.cs` that existed in each project. The unique features from each version were integrated into this common version:

- **ServerApplication**: `GenerateText()` and `GenerateEllipse()` methods
- **CameraRemoteApp**: Base version
- **VideoRemoteApp**: Style improvements with `internal static class` and SA1503 braces
- **WhisperRemoteApp**: Complete XML documentation

## Maintenance

When modifying this file:
1. Ensure changes are compatible with all user projects
2. Test compilation of all affected projects
3. Update this documentation if necessary
