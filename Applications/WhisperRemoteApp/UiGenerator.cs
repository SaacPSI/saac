// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC
{
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Shapes;
    using Microsoft.Win32;

    /// <summary>
    /// Provides UI generation utilities for SAAC applications.
    /// </summary>
    internal static class UiGenerator
    {
        private static ColorAnimation? errorColorAnimation = null;

        private static ColorAnimation GetErrorColorAnimation()
        {
            if (errorColorAnimation is null)
            {
                errorColorAnimation = new ColorAnimation(Colors.White, Colors.Red, TimeSpan.FromMilliseconds(250));
                errorColorAnimation.RepeatBehavior = new RepeatBehavior(TimeSpan.FromMilliseconds(500));
                errorColorAnimation.AutoReverse = true;
            }

            return errorColorAnimation;
        }

        /// <summary>
        /// Generates a label with the specified content.
        /// </summary>
        /// <param name="content">The content for the label.</param>
        /// <returns>The generated label.</returns>
        public static Label GenerateLabel(string content)
        {
            Label label = new Label();
            label.Content = content;
            return label;
        }

        /// <summary>
        /// Generates a text block with the specified text.
        /// </summary>
        /// <param name="text">The text content.</param>
        /// <param name="width">The width of the text block.</param>
        /// <param name="name">The optional name for the text block.</param>
        /// <returns>The generated text block.</returns>
        public static TextBlock GenerateText(string text, double width, string name = "")
        {
            TextBlock textBlock = new TextBlock();
            textBlock.TextTrimming = TextTrimming.None;
            textBlock.HorizontalAlignment = HorizontalAlignment.Left;
            textBlock.Text = text;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textBlock.TextWrapping = TextWrapping.NoWrap;
            textBlock.Name = name;
            return textBlock;
        }

        /// <summary>
        /// Generates a button with the specified content and click handler.
        /// </summary>
        /// <param name="content">The content for the button.</param>
        /// <param name="onClickHandler">The click event handler.</param>
        /// <param name="name">The optional name for the button.</param>
        /// <returns>The generated button.</returns>
        public static Button GenerateButton(string content, RoutedEventHandler onClickHandler, string name = "")
        {
            Button button = new Button();
            button.Content = content;
            button.Name = name;
            button.Click += onClickHandler;
            return button;
        }

        /// <summary>
        /// Generates a browse directory button.
        /// </summary>
        /// <param name="content">The content for the button.</param>
        /// <param name="textBox">The text box to update with the selected path.</param>
        /// <param name="name">The optional name for the button.</param>
        /// <returns>The generated button.</returns>
        public static Button GenerateBrowseDirectoryButton(string content, TextBox textBox, string name = "")
        {
            Button button = new Button();
            button.Content = content;
            button.Name = name;
            button.Click += (sender, e) =>
            {
                FolderPicker openFileDialog = new FolderPicker();
                if (openFileDialog.ShowDialog() == true)
                {
                    textBox.Text = openFileDialog.ResultName;
                }
            };
            return button;
        }

        /// <summary>
        /// Generates an ellipse shape.
        /// </summary>
        /// <param name="size">The size of the ellipse.</param>
        /// <param name="fill">The fill brush.</param>
        /// <param name="stroke">The stroke brush.</param>
        /// <param name="strokeThickness">The stroke thickness.</param>
        /// <param name="name">The optional name for the ellipse.</param>
        /// <returns>The generated ellipse.</returns>
        public static Ellipse GenerateEllipse(double size, Brush fill, Brush? stroke = null, double strokeThickness = 1, string name = "")
        {
            var e = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = fill,
                Stroke = stroke ?? Brushes.Transparent,
                StrokeThickness = strokeThickness,
                Name = name,
            };
            return e;
        }

        /// <summary>
        /// Generates a checkbox with the specified content and state.
        /// </summary>
        /// <param name="content">The content for the checkbox.</param>
        /// <param name="defaultState">The default checked state.</param>
        /// <param name="onClickHandler">The optional click event handler.</param>
        /// <param name="name">The optional name for the checkbox.</param>
        /// <returns>The generated checkbox.</returns>
        public static CheckBox GenerateCheckBox(string content, bool defaultState = true, RoutedEventHandler? onClickHandler = null, string name = "")
        {
            CheckBox checkBox = new CheckBox();
            checkBox.Content = content;
            checkBox.Name = name;
            checkBox.IsChecked = defaultState;
            if (onClickHandler != null)
            {
                checkBox.Click += onClickHandler;
            }

            return checkBox;
        }

        /// <summary>
        /// Generates a browse filename button.
        /// </summary>
        /// <param name="content">The content for the button.</param>
        /// <param name="textBox">The text box to update with the selected filename.</param>
        /// <param name="filters">The file filters.</param>
        /// <param name="name">The optional name for the button.</param>
        /// <returns>The generated button.</returns>
        public static Button GenerateBrowseFilenameButton(string content, TextBox textBox, string filters, string name = "")
        {
            Button button = new Button();
            button.Content = content;
            button.Name = name;
            button.Click += (sender, e) =>
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = filters; // example : "Librairy (*.dll)|*.dll";
                if (openFileDialog.ShowDialog() == true)
                {
                    textBox.Text = openFileDialog.FileName;
                }
            };
            return button;
        }

        /// <summary>
        /// Generates a text box with the specified width.
        /// </summary>
        /// <param name="width">The width of the text box.</param>
        /// <param name="name">The optional name for the text box.</param>
        /// <returns>The generated text box.</returns>
        public static TextBox GenerateTextBox(double width, string name = "")
        {
            return GeneratorTextBox(name, width);
        }

        /// <summary>
        /// Generates a path text box with validation.
        /// </summary>
        /// <param name="width">The width of the text box.</param>
        /// <param name="name">The optional name for the text box.</param>
        /// <returns>The generated text box.</returns>
        public static TextBox GeneratePathTextBox(double width, string name = "")
        {
            TextBox textBox = GeneratorTextBox(name, width);
            return SetTextBoxOutFocusChecker<Uri>(textBox, UriTryParse);
        }

        /// <summary>
        /// Generates a filename text box with validation.
        /// </summary>
        /// <param name="width">The width of the text box.</param>
        /// <param name="name">The optional name for the text box.</param>
        /// <returns>The generated text box.</returns>
        public static TextBox GenerateFilenameTextBox(double width, string name = "")
        {
            TextBox textBox = GeneratorTextBox(name, width);
            return SetTextBoxPreviewTextChecker<string>(textBox, PathTryParse);
        }

        /// <summary>
        /// Generates an IP address text box with validation.
        /// </summary>
        /// <param name="width">The width of the text box.</param>
        /// <param name="name">The optional name for the text box.</param>
        /// <returns>The generated text box.</returns>
        public static TextBox GenerateIPAddressTextBox(double width, string name = "")
        {
            TextBox textBox = GeneratorTextBox(name, width);
            return SetTextBoxOutFocusChecker<IPAddress>(textBox, IPAddressTryParse);
        }

        /// <summary>
        /// Generates an integer text box with validation.
        /// </summary>
        /// <param name="width">The width of the text box.</param>
        /// <param name="name">The optional name for the text box.</param>
        /// <returns>The generated text box.</returns>
        public static TextBox GenerateIntergerTextBox(double width, string name = "")
        {
            TextBox textBox = GeneratorTextBox(name, width);
            return SetTextBoxPreviewTextChecker<int>(textBox, int.TryParse);
        }

        /// <summary>
        /// Generates a double text box with validation.
        /// </summary>
        /// <param name="width">The width of the text box.</param>
        /// <param name="name">The optional name for the text box.</param>
        /// <returns>The generated text box.</returns>
        public static TextBox GenerateDoubleTextBox(double width, string name = "")
        {
            TextBox textBox = GeneratorTextBox(name, width);
            return SetTextBoxPreviewTextChecker<double>(textBox, double.TryParse);
        }

        /// <summary>
        /// Delegate for try-parse handlers.
        /// </summary>
        /// <typeparam name="T">The type to parse to.</typeparam>
        /// <param name="value">The value to parse.</param>
        /// <param name="result">The parsed result.</param>
        /// <returns>True if parsing succeeded; otherwise, false.</returns>
        public delegate bool TryParseHandler<T>(string value, out T result);

        /// <summary>
        /// Tries to parse an IP address string.
        /// </summary>
        /// <param name="value">The string to parse.</param>
        /// <param name="result">The parsed IP address.</param>
        /// <returns>True if parsing succeeded; otherwise, false.</returns>
        public static bool IPAddressTryParse(string value, out IPAddress result)
        {
            try
            {
                result = System.Net.IPAddress.Parse(value);
            }
            catch (Exception)
            {
                result = new IPAddress(0);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Tries to parse a URI string.
        /// </summary>
        /// <param name="value">The string to parse.</param>
        /// <param name="result">The parsed URI.</param>
        /// <returns>True if parsing succeeded; otherwise, false.</returns>
        public static bool UriTryParse(string value, out Uri result)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out result);
        }

        /// <summary>
        /// Tries to parse a path string.
        /// </summary>
        /// <param name="value">The string to parse.</param>
        /// <param name="result">The parsed path.</param>
        /// <returns>True if parsing succeeded; otherwise, false.</returns>
        public static bool PathTryParse(string value, out string result)
        {
            try
            {
                string fullPath = System.IO.Path.GetFullPath(value);
                result = fullPath;
            }
            catch (Exception)
            {
                result = string.Empty;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Sets up a text box with preview text validation.
        /// </summary>
        /// <typeparam name="T">The type to parse.</typeparam>
        /// <param name="inputText">The text box to configure.</param>
        /// <param name="handler">The parse handler.</param>
        /// <returns>The configured text box.</returns>
        public static TextBox SetTextBoxPreviewTextChecker<T>(TextBox inputText, TryParseHandler<T> handler)
        {
            inputText.Background = new SolidColorBrush(Colors.White);
            inputText.PreviewTextInput += (obj, evt) =>
            {
                bool result = !handler(evt.Text, out _);
                evt.Handled = result;
                if (result)
                {
                    var input = obj as TextBox;
                    input?.Background.BeginAnimation(SolidColorBrush.ColorProperty, GetErrorColorAnimation());
                }
            };
            return inputText;
        }

        /// <summary>
        /// Sets up a text box with out-focus validation.
        /// </summary>
        /// <typeparam name="T">The type to parse.</typeparam>
        /// <param name="inputText">The text box to configure.</param>
        /// <param name="handler">The parse handler.</param>
        /// <returns>The configured text box.</returns>
        public static TextBox SetTextBoxOutFocusChecker<T>(TextBox inputText, TryParseHandler<T> handler)
        {
            inputText.Background = new SolidColorBrush(Colors.White);
            inputText.LostFocus += (obj, evt) =>
            {
                bool result = !handler((obj as TextBox)?.Text, out _);
                evt.Handled = result;
                if (result)
                {
                    var input = obj as TextBox;
                    input?.Background.BeginAnimation(SolidColorBrush.ColorProperty, GetErrorColorAnimation());
                }
            };
            return inputText;
        }

        /// <summary>
        /// Creates a routed event handler that checks if a file exists.
        /// </summary>
        /// <param name="message">The message to display if the file exists.</param>
        /// <param name="extension">The optional file extension.</param>
        /// <param name="pathTextBox">The optional path text box.</param>
        /// <returns>The routed event handler.</returns>
        public static RoutedEventHandler IsFileExistChecker(string message, string extension = "", TextBox? pathTextBox = null)
        {
            return (obj, evt) =>
            {
                TextBox? text = (obj as TextBox);
                if (text is null)
                {
                    return;
                }

                string filename = text.Text = System.IO.Path.HasExtension(text.Text) ? text.Text : $"{text.Text}{extension}";
                System.Windows.Data.BindingExpression? binding = text.GetBindingExpression(TextBox.TextProperty);
                binding?.UpdateSource();
                if (pathTextBox != null)
                {
                    filename = System.IO.Path.Combine(pathTextBox.Text, filename);
                }

                if (File.Exists(filename))
                {
                    MessageBox.Show(message, "File already exist", MessageBoxButton.OK, MessageBoxImage.Hand);
                }
            };
        }

        /// <summary>
        /// Internal text box generator.
        /// </summary>
        /// <param name="name">The name for the text box.</param>
        /// <param name="width">The width of the text box.</param>
        /// <returns>The generated text box.</returns>
        public static TextBox GeneratorTextBox(string name, double width)
        {
            TextBox inputText = new TextBox();
            inputText.Width = width;
            inputText.Name = name;
            inputText.AcceptsReturn = inputText.AcceptsTab = false;
            return inputText;
        }

        /// <summary>
        /// Generates a grid with the specified dimensions.
        /// </summary>
        /// <param name="length">The grid length.</param>
        /// <param name="numberofColumn">The number of columns.</param>
        /// <param name="numberofRows">The number of rows.</param>
        /// <returns>The generated grid.</returns>
        public static Grid GenerateGrid(GridLength length, int numberofColumn, int numberofRows)
        {
            Grid grid = new Grid();
            AddColumnsDefinitionToGrid(grid, GridLength.Auto, numberofColumn);
            AddRowsDefinitionToGrid(grid, GridLength.Auto, numberofRows);
            return grid;
        }

        /// <summary>
        /// Sets an element in the grid at the specified position.
        /// </summary>
        /// <param name="grid">The grid.</param>
        /// <param name="element">The element to add.</param>
        /// <param name="column">The column index.</param>
        /// <param name="row">The row index.</param>
        public static void SetElementInGrid(Grid grid, UIElement element, int column, int row)
        {
            grid.Children.Add(element);
            Grid.SetRow(element, row);
            Grid.SetColumn(element, column);
        }

        /// <summary>
        /// Adds row definitions to a grid.
        /// </summary>
        /// <param name="grid">The grid.</param>
        /// <param name="length">The row height.</param>
        /// <param name="number">The number of rows to add.</param>
        public static void AddRowsDefinitionToGrid(Grid grid, GridLength length, int number)
        {
            for (int row = 0; row < number; row++)
            {
                RowDefinition newRow = new RowDefinition();
                newRow.Height = length;
                grid.RowDefinitions.Add(newRow);
            }
        }

        /// <summary>
        /// Adds column definitions to a grid.
        /// </summary>
        /// <param name="grid">The grid.</param>
        /// <param name="length">The column width.</param>
        /// <param name="number">The number of columns to add.</param>
        public static void AddColumnsDefinitionToGrid(Grid grid, GridLength length, int number)
        {
            for (int column = 0; column < number; column++)
            {
                ColumnDefinition newColumns = new ColumnDefinition();
                newColumns.Width = GridLength.Auto;
                grid.ColumnDefinitions.Add(newColumns);
            }
        }

        /// <summary>
        /// Removes a row definition from a grid.
        /// </summary>
        /// <param name="grid">The grid.</param>
        /// <param name="rowIndex">The row index to remove.</param>
        public static void RemoveRowDefinitionFromGrid(Grid grid, int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= grid.RowDefinitions.Count)
            {
                return;
            }

            grid.RowDefinitions.RemoveAt(rowIndex);
        }

        /// <summary>
        /// Removes a row and its elements from a grid.
        /// </summary>
        /// <param name="grid">The grid.</param>
        /// <param name="rowIndex">The row index to remove.</param>
        public static void RemoveRowInGrid(Grid grid, int rowIndex)
        {
            List<UIElement> elementsToRemove = new List<UIElement>();
            foreach (UIElement element in grid.Children)
            {
                if (Grid.GetRow(element) == rowIndex)
                {
                    elementsToRemove.Add(element);
                }
                else if (Grid.GetRow(element) > rowIndex)
                {
                    Grid.SetRow(element, Grid.GetRow(element) - 1);
                }
            }

            foreach (UIElement element in elementsToRemove)
            {
                grid.Children.Remove(element);
            }

            UiGenerator.RemoveRowDefinitionFromGrid(grid, rowIndex);
        }

#pragma warning disable SA1600 // Elements should be documented
        /// <summary>
        /// Folder picker dialog wrapper.
        /// </summary>
        /// <remarks>From https://stackoverflow.com/questions/11624298/how-do-i-use-openfiledialog-to-select-a-folder/66187224#66187224.</remarks>
        public class FolderPicker
        {
            private readonly List<string> resultPaths = new List<string>();
            private readonly List<string> resultNames = new List<string>();

            /// <summary>
            /// Gets the result paths.
            /// </summary>
            public IReadOnlyList<string> ResultPaths => this.resultPaths;

            /// <summary>
            /// Gets the result names.
            /// </summary>
            public IReadOnlyList<string> ResultNames => this.resultNames;

            /// <summary>
            /// Gets the first result path.
            /// </summary>
            public string ResultPath => this.ResultPaths.FirstOrDefault();

            /// <summary>
            /// Gets the first result name.
            /// </summary>
            public string ResultName => this.ResultNames.FirstOrDefault();

            /// <summary>
            /// Gets or sets the input path.
            /// </summary>
            public virtual string InputPath { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether to force file system.
            /// </summary>
            public virtual bool ForceFileSystem { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether to allow multiple selection.
            /// </summary>
            public virtual bool Multiselect { get; set; }

            /// <summary>
            /// Gets or sets the dialog title.
            /// </summary>
            public virtual string Title { get; set; }

            /// <summary>
            /// Gets or sets the OK button label.
            /// </summary>
            public virtual string OkButtonLabel { get; set; }

            /// <summary>
            /// Gets or sets the file name label.
            /// </summary>
            public virtual string FileNameLabel { get; set; }

            /// <summary>
            /// Sets the dialog options.
            /// </summary>
            /// <param name="options">The base options.</param>
            /// <returns>The modified options.</returns>
            protected virtual int SetOptions(int options)
            {
                if (this.ForceFileSystem)
                {
                    options |= (int)FOS.FOS_FORCEFILESYSTEM;
                }

                if (this.Multiselect)
                {
                    options |= (int)FOS.FOS_ALLOWMULTISELECT;
                }

                return options;
            }

            /// <summary>
            /// Shows the folder picker dialog.
            /// </summary>
            /// <param name="owner">The owner window.</param>
            /// <param name="throwOnError">Whether to throw on error.</param>
            /// <returns>True if a folder was selected; otherwise, null.</returns>
            public bool? ShowDialog(Window? owner = null, bool throwOnError = false)
            {
                owner = owner ?? Application.Current?.MainWindow;
                return this.ShowDialog(owner != null ? new WindowInteropHelper(owner).Handle : IntPtr.Zero, throwOnError);
            }

            /// <summary>
            /// Shows the folder picker dialog.
            /// </summary>
            /// <param name="owner">The owner window handle.</param>
            /// <param name="throwOnError">Whether to throw on error.</param>
            /// <returns>True if a folder was selected; otherwise, null.</returns>
            public virtual bool? ShowDialog(IntPtr owner, bool throwOnError = false)
            {
                var dialog = (IFileOpenDialog)new FileOpenDialog();
                if (!string.IsNullOrEmpty(this.InputPath))
                {
                    if (CheckHr(SHCreateItemFromParsingName(this.InputPath, null, typeof(IShellItem).GUID, out var item), throwOnError) != 0)
                    {
                        return null;
                    }

                    dialog.SetFolder(item);
                }

                var options = FOS.FOS_PICKFOLDERS;
                options = (FOS)this.SetOptions((int)options);
                dialog.SetOptions(options);

                if (this.Title != null)
                {
                    dialog.SetTitle(this.Title);
                }

                if (this.OkButtonLabel != null)
                {
                    dialog.SetOkButtonLabel(this.OkButtonLabel);
                }

                if (this.FileNameLabel != null)
                {
                    dialog.SetFileName(this.FileNameLabel);
                }

                if (owner == IntPtr.Zero)
                {
                    owner = Process.GetCurrentProcess().MainWindowHandle;
                    if (owner == IntPtr.Zero)
                    {
                        owner = GetDesktopWindow();
                    }
                }

                var hr = dialog.Show(owner);
                if (hr == ERROR_CANCELLED)
                {
                    return null;
                }

                if (CheckHr(hr, throwOnError) != 0)
                {
                    return null;
                }

                if (CheckHr(dialog.GetResults(out var items), throwOnError) != 0)
                {
                    return null;
                }

                items.GetCount(out var count);
                for (var i = 0; i < count; i++)
                {
                    items.GetItemAt(i, out var item);
                    CheckHr(item.GetDisplayName(SIGDN.SIGDN_DESKTOPABSOLUTEPARSING, out var path), throwOnError);
                    CheckHr(item.GetDisplayName(SIGDN.SIGDN_DESKTOPABSOLUTEEDITING, out var name), throwOnError);
                    if (path != null || name != null)
                    {
                        this.resultPaths.Add(path);
                        this.resultNames.Add(name);
                    }
                }

                return true;
            }

            private static int CheckHr(int hr, bool throwOnError)
            {
                if (hr != 0 && throwOnError)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                return hr;
            }

            [DllImport("shell32")]
            private static extern int SHCreateItemFromParsingName([MarshalAs(UnmanagedType.LPWStr)] string pszPath, IBindCtx pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IShellItem ppv);

            [DllImport("user32")]
            private static extern IntPtr GetDesktopWindow();

#pragma warning disable IDE1006 // Naming Styles
            private const int ERROR_CANCELLED = unchecked((int)0x800704C7);
#pragma warning restore IDE1006 // Naming Styles

            [ComImport]
            [Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")] // CLSID_FileOpenDialog
            private class FileOpenDialog
            {
            }

            [ComImport]
            [Guid("d57c7288-d4ad-4768-be02-9d969532d960")]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            private interface IFileOpenDialog
            {
                [PreserveSig]
                int Show(IntPtr parent); // IModalWindow

                [PreserveSig]
                int SetFileTypes();  // not fully defined

                [PreserveSig]
                int SetFileTypeIndex(int iFileType);

                [PreserveSig]
                int GetFileTypeIndex(out int piFileType);

                [PreserveSig]
                int Advise(); // not fully defined

                [PreserveSig]
                int Unadvise();

                [PreserveSig]
                int SetOptions(FOS fos);

                [PreserveSig]
                int GetOptions(out FOS pfos);

                [PreserveSig]
                int SetDefaultFolder(IShellItem psi);

                [PreserveSig]
                int SetFolder(IShellItem psi);

                [PreserveSig]
                int GetFolder(out IShellItem ppsi);

                [PreserveSig]
                int GetCurrentSelection(out IShellItem ppsi);

                [PreserveSig]
                int SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);

                [PreserveSig]
                int GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);

                [PreserveSig]
                int SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);

                [PreserveSig]
                int SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);

                [PreserveSig]
                int SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

                [PreserveSig]
                int GetResult(out IShellItem ppsi);

                [PreserveSig]
                int AddPlace(IShellItem psi, int alignment);

                [PreserveSig]
                int SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);

                [PreserveSig]
                int Close(int hr);

                [PreserveSig]
                int SetClientGuid();  // not fully defined

                [PreserveSig]
                int ClearClientData();

                [PreserveSig]
                int SetFilter([MarshalAs(UnmanagedType.IUnknown)] object pFilter);

                [PreserveSig]
                int GetResults(out IShellItemArray ppenum);

                [PreserveSig]
                int GetSelectedItems([MarshalAs(UnmanagedType.IUnknown)] out object ppsai);
            }

            [ComImport]
            [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            private interface IShellItem
            {
                [PreserveSig]
                int BindToHandler(); // not fully defined

                [PreserveSig]
                int GetParent(); // not fully defined

                [PreserveSig]
                int GetDisplayName(SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);

                [PreserveSig]
                int GetAttributes();  // not fully defined

                [PreserveSig]
                int Compare();  // not fully defined
            }

            [ComImport]
            [Guid("b63ea76d-1f85-456f-a19c-48159efa858b")]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            private interface IShellItemArray
            {
                [PreserveSig]
                int BindToHandler();  // not fully defined

                [PreserveSig]
                int GetPropertyStore();  // not fully defined

                [PreserveSig]
                int GetPropertyDescriptionList();  // not fully defined

                [PreserveSig]
                int GetAttributes();  // not fully defined

                [PreserveSig]
                int GetCount(out int pdwNumItems);

                [PreserveSig]
                int GetItemAt(int dwIndex, out IShellItem ppsi);

                [PreserveSig]
                int EnumItems();  // not fully defined
            }

#pragma warning disable CA1712 // Do not prefix enum values with type name
            private enum SIGDN : uint
            {
                SIGDN_DESKTOPABSOLUTEEDITING = 0x8004c000,
                SIGDN_DESKTOPABSOLUTEPARSING = 0x80028000,
                SIGDN_FILESYSPATH = 0x80058000,
                SIGDN_NORMALDISPLAY = 0,
                SIGDN_PARENTRELATIVE = 0x80080001,
                SIGDN_PARENTRELATIVEEDITING = 0x80031001,
                SIGDN_PARENTRELATIVEFORADDRESSBAR = 0x8007c001,
                SIGDN_PARENTRELATIVEPARSING = 0x80018001,
                SIGDN_URL = 0x80068000,
            }

            [Flags]
            private enum FOS
            {
                FOS_OVERWRITEPROMPT = 0x2,
                FOS_STRICTFILETYPES = 0x4,
                FOS_NOCHANGEDIR = 0x8,
                FOS_PICKFOLDERS = 0x20,
                FOS_FORCEFILESYSTEM = 0x40,
                FOS_ALLNONSTORAGEITEMS = 0x80,
                FOS_NOVALIDATE = 0x100,
                FOS_ALLOWMULTISELECT = 0x200,
                FOS_PATHMUSTEXIST = 0x800,
                FOS_FILEMUSTEXIST = 0x1000,
                FOS_CREATEPROMPT = 0x2000,
                FOS_SHAREAWARE = 0x4000,
                FOS_NOREADONLYRETURN = 0x8000,
                FOS_NOTESTFILECREATE = 0x10000,
                FOS_HIDEMRUPLACES = 0x20000,
                FOS_HIDEPINNEDPLACES = 0x40000,
                FOS_NODEREFERENCELINKS = 0x100000,
                FOS_OKBUTTONNEEDSINTERACTION = 0x200000,
                FOS_DONTADDTORECENT = 0x2000000,
                FOS_FORCESHOWHIDDEN = 0x10000000,
                FOS_DEFAULTNOMINIMODE = 0x20000000,
                FOS_FORCEPREVIEWPANEON = 0x40000000,
                FOS_SUPPORTSTREAMABLEITEMS = unchecked((int)0x80000000),
            }
#pragma warning restore CA1712 // Do not prefix enum values with type name
#pragma warning restore SA1600 // Elements should be documented
        }
    }
}
