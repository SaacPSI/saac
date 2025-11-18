using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace WhisperRemoteApp
{
    /// <summary>
    /// Logique d'interaction pour CultureInfoWindow.xaml
    /// </summary>
    public partial class CultureInfoWindow : Window, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
            {

                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        public string SelectedCulture { get; set; } 

        public List<string> CultureInfo { get; set; }

        private List<System.Speech.Recognition.RecognizerInfo> infos;
        public CultureInfoWindow(List<System.Speech.Recognition.RecognizerInfo> infos)
        {
            this.infos = infos;
            CultureInfo = new List<string>() { };
            foreach (var recognizerInfo in infos)
                CultureInfo.Add(recognizerInfo.Culture.NativeName);
            DataContext = this;
            InitializeComponent();
            CultureInfoComboBox.SelectedIndex = 0;
        }

        private void CultureSelected(object sender, RoutedEventArgs e)
        {
            SelectedCulture = infos.ElementAt(CultureInfoComboBox.SelectedIndex).Culture.Name;
        }

        private void OnButtonClick(bool result, RoutedEventArgs e)
        {
            this.DialogResult = result;
            Close();
            e.Handled = true;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            OnButtonClick(true, e);
        }


        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            OnButtonClick(false, e);
        }
    }
}
