using Microsoft.Win32;
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

namespace SAAC
{
    static class UiGenerator
    {
        static private ColorAnimation? errorColorAnimation = null;
        static private ColorAnimation GetErrorColorAnimation()
        {
            if (errorColorAnimation is null)
            {
                errorColorAnimation = new ColorAnimation(Colors.White, Colors.Red, TimeSpan.FromMilliseconds(250));
                errorColorAnimation.RepeatBehavior = new RepeatBehavior(TimeSpan.FromMilliseconds(500));
                errorColorAnimation.AutoReverse = true;
            }
            return errorColorAnimation;
        }

        static public Label GenerateLabel(string content)
        {
            Label label = new Label();
            label.Content = content;
            return label;
        }

        static public Button GenerateButton(string content, RoutedEventHandler onClickHandler, string name = "")
        {
            Button button = new Button();
            button.Content = content;
            button.Name = name;
            button.Click += onClickHandler;
            return button;
        }

        static public Button GenerateBrowseDirectoryButton(string content, TextBox textBox, string name = "")
        {
            Button button = new Button();
            button.Content = content;
            button.Name = name;
            button.Click += (sender, e) => {
                FolderPicker openFileDialog = new FolderPicker();
                if (openFileDialog.ShowDialog() == true)
                    textBox.Text = openFileDialog.ResultName;
            };
            return button;
        }

        static public CheckBox GenerateCheckBox(string content, bool defaultState = true, RoutedEventHandler? onClickHandler = null, string name = "") 
        {
            CheckBox checkBox = new CheckBox();
            checkBox.Content = content;
            checkBox.Name = name;
            checkBox.IsChecked = defaultState;
            if (onClickHandler != null)
                checkBox.Click += onClickHandler;
            return checkBox;
        }

        static public Button GenerateBrowseFilenameButton(string content, TextBox textBox, string filters, string name = "")
        {
            Button button = new Button();
            button.Content = content;
            button.Name = name;
            button.Click += (sender, e) => {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = filters; // example : "Librairy (*.dll)|*.dll";
                if (openFileDialog.ShowDialog() == true)
                    textBox.Text = openFileDialog.FileName;
            };
            return button;
        }

        static public TextBox GenerateTextBox(double width, string name = "")
        {
            return GeneratorTextBox(name, width);
        }

        static public TextBox GeneratePathTextBox(double width, string name = "")
        {
            TextBox textBox = GeneratorTextBox(name, width);
            return SetTextBoxOutFocusChecker<Uri>(textBox, UriTryParse);
        }

        static public TextBox GenerateFilenameTextBox(double width, string name = "")
        {
            TextBox textBox = GeneratorTextBox(name, width);
            return SetTextBoxPreviewTextChecker<string>(textBox, PathTryParse);
        }

        static public TextBox GenerateIPAddressTextBox(double width, string name = "")
        {
            TextBox textBox = GeneratorTextBox(name, width);
            return SetTextBoxOutFocusChecker<IPAddress>(textBox, IPAddressTryParse);
        }

        static public TextBox GenerateIntergerTextBox(double width, string name="")
        {
            TextBox textBox = GeneratorTextBox(name, width);
            return SetTextBoxPreviewTextChecker<int>(textBox, int.TryParse);
        }

        static public TextBox GenerateDoubleTextBox(double width, string name = "")
        {
            TextBox textBox = GeneratorTextBox(name, width);
            return SetTextBoxPreviewTextChecker<double>(textBox, double.TryParse);
        }

        public delegate bool TryParseHandler<T>(string value, out T result);

        static public bool IPAddressTryParse(string value, out IPAddress result)
        {
            try
            {
                result = System.Net.IPAddress.Parse(value);
            }
            catch (Exception e)
            {
                result = new IPAddress(0);
                return false;
            }
            return true;
        }

        static public bool UriTryParse(string value, out Uri result)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out result);
        }

        static public bool PathTryParse(string value, out string result)
        {
            try
            {   
                string fullPath = System.IO.Path.GetFullPath(value);
                result = fullPath;
            }
            catch (Exception e)
            {
                result = "";
                return false;
            }
            return true;
        }

        static public TextBox SetTextBoxPreviewTextChecker<T>(TextBox inputText, TryParseHandler<T> handler)
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

        static public TextBox SetTextBoxOutFocusChecker<T>(TextBox inputText, TryParseHandler<T> handler)
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
        static public RoutedEventHandler IsFileExistChecker(string message, string extension = "", TextBox? pathTextBox = null)
        {
            return (obj, evt) =>
            {
                TextBox? text = (obj as TextBox);
                if (text is null)
                    return;
                string filename = text.Text = Path.HasExtension(text.Text) ? text.Text : $"{text.Text}{extension}";
                System.Windows.Data.BindingExpression? binding = text.GetBindingExpression(TextBox.TextProperty);
                binding?.UpdateSource();
                if (pathTextBox != null)
                    filename = Path.Combine(pathTextBox.Text, filename);
                if (File.Exists(filename))
                    MessageBox.Show(message, "File already exist", MessageBoxButton.OK, MessageBoxImage.Hand);
            };
        }

        static public TextBox GeneratorTextBox(string name, double width)
        {
            TextBox inputText = new TextBox();
            inputText.Width = width;
            inputText.Name = name;
            inputText.AcceptsReturn = inputText.AcceptsTab = false;
            return inputText;
        }

        static public Grid GenerateGrid(GridLength length, int numberofColumn, int numberofRows)
        {
            Grid grid = new Grid();
            AddColumnsDefinitionToGrid(grid, GridLength.Auto, numberofColumn);
            AddRowsDefinitionToGrid(grid, GridLength.Auto, numberofRows);
            return grid;
        }

        static public void SetElementInGrid(Grid grid, UIElement element, int column, int row)
        {
            grid.Children.Add(element);
            Grid.SetRow(element, row);
            Grid.SetColumn(element, column);
        }

        static public void AddRowsDefinitionToGrid(Grid grid, GridLength length, int number)
        {
            for (int row = 0; row < number; row++)
            {
                RowDefinition newRow = new RowDefinition();
                newRow.Height = length;
                grid.RowDefinitions.Add(newRow);
            }
        }

        static public void AddColumnsDefinitionToGrid(Grid grid, GridLength length, int number)
        {
            for (int column = 0; column < number; column++)
            {
                ColumnDefinition newColumns = new ColumnDefinition();
                newColumns.Width = GridLength.Auto;
                grid.ColumnDefinitions.Add(newColumns);
            }
        }

        static public void RemoveRowDefinitionFromGrid(Grid grid, int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= grid.RowDefinitions.Count)
                return;
            grid.RowDefinitions.RemoveAt(rowIndex);
        }

        static public void RemoveRowInGrid(Grid grid, int rowIndex)
        {
            List<UIElement> elementsToRemove = new List<UIElement>();
            foreach (UIElement element in grid.Children)
            {
                if (Grid.GetRow(element) == rowIndex)
                    elementsToRemove.Add(element);
                else if (Grid.GetRow(element) > rowIndex)
                    Grid.SetRow(element, Grid.GetRow(element) - 1);
            }
            foreach (UIElement element in elementsToRemove)
                grid.Children.Remove(element);
            UiGenerator.RemoveRowDefinitionFromGrid(grid, rowIndex);
        }

        // From https://stackoverflow.com/questions/11624298/how-do-i-use-openfiledialog-to-select-a-folder/66187224#66187224 
        public class FolderPicker
        {
            private readonly List<string> _resultPaths = new List<string>();
            private readonly List<string> _resultNames = new List<string>();

            public IReadOnlyList<string> ResultPaths => _resultPaths;
            public IReadOnlyList<string> ResultNames => _resultNames;
            public string ResultPath => ResultPaths.FirstOrDefault();
            public string ResultName => ResultNames.FirstOrDefault();
            public virtual string InputPath { get; set; }
            public virtual bool ForceFileSystem { get; set; }
            public virtual bool Multiselect { get; set; }
            public virtual string Title { get; set; }
            public virtual string OkButtonLabel { get; set; }
            public virtual string FileNameLabel { get; set; }

            protected virtual int SetOptions(int options)
            {
                if (ForceFileSystem)
                {
                    options |= (int)FOS.FOS_FORCEFILESYSTEM;
                }

                if (Multiselect)
                {
                    options |= (int)FOS.FOS_ALLOWMULTISELECT;
                }
                return options;
            }

            // for WPF support
            public bool? ShowDialog(Window owner = null, bool throwOnError = false)
            {
                owner = owner ?? Application.Current?.MainWindow;
                return ShowDialog(owner != null ? new WindowInteropHelper(owner).Handle : IntPtr.Zero, throwOnError);
            }

            // for all .NET
            public virtual bool? ShowDialog(IntPtr owner, bool throwOnError = false)
            {
                var dialog = (IFileOpenDialog)new FileOpenDialog();
                if (!string.IsNullOrEmpty(InputPath))
                {
                    if (CheckHr(SHCreateItemFromParsingName(InputPath, null, typeof(IShellItem).GUID, out var item), throwOnError) != 0)
                        return null;

                    dialog.SetFolder(item);
                }

                var options = FOS.FOS_PICKFOLDERS;
                options = (FOS)SetOptions((int)options);
                dialog.SetOptions(options);

                if (Title != null)
                {
                    dialog.SetTitle(Title);
                }

                if (OkButtonLabel != null)
                {
                    dialog.SetOkButtonLabel(OkButtonLabel);
                }

                if (FileNameLabel != null)
                {
                    dialog.SetFileName(FileNameLabel);
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
                    return null;

                if (CheckHr(hr, throwOnError) != 0)
                    return null;

                if (CheckHr(dialog.GetResults(out var items), throwOnError) != 0)
                    return null;

                items.GetCount(out var count);
                for (var i = 0; i < count; i++)
                {
                    items.GetItemAt(i, out var item);
                    CheckHr(item.GetDisplayName(SIGDN.SIGDN_DESKTOPABSOLUTEPARSING, out var path), throwOnError);
                    CheckHr(item.GetDisplayName(SIGDN.SIGDN_DESKTOPABSOLUTEEDITING, out var name), throwOnError);
                    if (path != null || name != null)
                    {
                        _resultPaths.Add(path);
                        _resultNames.Add(name);
                    }
                }
                return true;
            }

            private static int CheckHr(int hr, bool throwOnError)
            {
                if (hr != 0 && throwOnError) Marshal.ThrowExceptionForHR(hr);
                return hr;
            }

            [DllImport("shell32")]
            private static extern int SHCreateItemFromParsingName([MarshalAs(UnmanagedType.LPWStr)] string pszPath, IBindCtx pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IShellItem ppv);

            [DllImport("user32")]
            private static extern IntPtr GetDesktopWindow();

    #pragma warning disable IDE1006 // Naming Styles
            private const int ERROR_CANCELLED = unchecked((int)0x800704C7);
    #pragma warning restore IDE1006 // Naming Styles

            [ComImport, Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")] // CLSID_FileOpenDialog
            private class FileOpenDialog { }

            [ComImport, Guid("d57c7288-d4ad-4768-be02-9d969532d960"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            private interface IFileOpenDialog
            {
                [PreserveSig] int Show(IntPtr parent); // IModalWindow
                [PreserveSig] int SetFileTypes();  // not fully defined
                [PreserveSig] int SetFileTypeIndex(int iFileType);
                [PreserveSig] int GetFileTypeIndex(out int piFileType);
                [PreserveSig] int Advise(); // not fully defined
                [PreserveSig] int Unadvise();
                [PreserveSig] int SetOptions(FOS fos);
                [PreserveSig] int GetOptions(out FOS pfos);
                [PreserveSig] int SetDefaultFolder(IShellItem psi);
                [PreserveSig] int SetFolder(IShellItem psi);
                [PreserveSig] int GetFolder(out IShellItem ppsi);
                [PreserveSig] int GetCurrentSelection(out IShellItem ppsi);
                [PreserveSig] int SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
                [PreserveSig] int GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
                [PreserveSig] int SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
                [PreserveSig] int SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
                [PreserveSig] int SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
                [PreserveSig] int GetResult(out IShellItem ppsi);
                [PreserveSig] int AddPlace(IShellItem psi, int alignment);
                [PreserveSig] int SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
                [PreserveSig] int Close(int hr);
                [PreserveSig] int SetClientGuid();  // not fully defined
                [PreserveSig] int ClearClientData();
                [PreserveSig] int SetFilter([MarshalAs(UnmanagedType.IUnknown)] object pFilter);
                [PreserveSig] int GetResults(out IShellItemArray ppenum);
                [PreserveSig] int GetSelectedItems([MarshalAs(UnmanagedType.IUnknown)] out object ppsai);
            }

            [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            private interface IShellItem
            {
                [PreserveSig] int BindToHandler(); // not fully defined
                [PreserveSig] int GetParent(); // not fully defined
                [PreserveSig] int GetDisplayName(SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
                [PreserveSig] int GetAttributes();  // not fully defined
                [PreserveSig] int Compare();  // not fully defined
            }

            [ComImport, Guid("b63ea76d-1f85-456f-a19c-48159efa858b"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            private interface IShellItemArray
            {
                [PreserveSig] int BindToHandler();  // not fully defined
                [PreserveSig] int GetPropertyStore();  // not fully defined
                [PreserveSig] int GetPropertyDescriptionList();  // not fully defined
                [PreserveSig] int GetAttributes();  // not fully defined
                [PreserveSig] int GetCount(out int pdwNumItems);
                [PreserveSig] int GetItemAt(int dwIndex, out IShellItem ppsi);
                [PreserveSig] int EnumItems();  // not fully defined
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
                SIGDN_URL = 0x80068000
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
                FOS_SUPPORTSTREAMABLEITEMS = unchecked((int)0x80000000)
            }
    #pragma warning restore CA1712 // Do not prefix enum values with type name
        }
    }
}
