// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace WhisperRemoteApp
{
    /// <summary>
    /// Dialog window for selecting a culture/language from available speech recognition options.
    /// </summary>
    public partial class CultureInfoWindow : Window, INotifyPropertyChanged
    {
        private List<System.Speech.Recognition.RecognizerInfo> infos;

        /// <summary>
        /// Initializes a new instance of the <see cref="CultureInfoWindow"/> class.
        /// </summary>
        /// <param name="infos">List of recognizer information.</param>
        public CultureInfoWindow(List<System.Speech.Recognition.RecognizerInfo> infos)
        {
            this.infos = infos;
            this.CultureInfo = new List<string>();
            foreach (var recognizerInfo in infos)
            {
                this.CultureInfo.Add(recognizerInfo.Culture.NativeName);
            }

            this.DataContext = this;
            this.InitializeComponent();
            this.CultureInfoComboBox.SelectedIndex = 0;
        }

        #region INotifyPropertyChanged

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        /// <summary>
        /// Gets or sets the selected culture.
        /// </summary>
        public string SelectedCulture { get; set; }

        /// <summary>
        /// Gets or sets the list of culture info strings.
        /// </summary>
        public List<string> CultureInfo { get; set; }

        /// <summary>
        /// Sets a property value and raises the PropertyChanged event if the value has changed.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="field">Reference to the backing field.</param>
        /// <param name="value">The new value.</param>
        /// <param name="propertyName">The name of the property (automatically provided by CallerMemberName).</param>
        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Handles the culture selection changed event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void CultureSelected(object sender, RoutedEventArgs e)
        {
            this.SelectedCulture = this.infos.ElementAt(this.CultureInfoComboBox.SelectedIndex).Culture.Name;
        }

        /// <summary>
        /// Handles button click and sets the dialog result.
        /// </summary>
        /// <param name="result">The dialog result.</param>
        /// <param name="e">The event arguments.</param>
        private void OnButtonClick(bool result, RoutedEventArgs e)
        {
            this.DialogResult = result;
            this.Close();
            e.Handled = true;
        }

        /// <summary>
        /// Handles the OK button click event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.OnButtonClick(true, e);
        }

        /// <summary>
        /// Handles the Cancel button click event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.OnButtonClick(false, e);
        }
    }
}
