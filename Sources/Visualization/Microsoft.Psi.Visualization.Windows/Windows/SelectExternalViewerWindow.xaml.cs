// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Windows
{
    using System.Windows;
    using Microsoft.Psi.Common;
    using Microsoft.Win32;

    /// <summary>
    /// Interaction logic for SelectExternalViewerWindow.xaml.
    /// </summary>
    public partial class SelectExternalViewerWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SelectExternalViewerWindow"/> class.
        /// </summary>
        /// <param name="owner">The window owner.</param>
        public SelectExternalViewerWindow(Window owner)
        {
            this.InitializeComponent();
            this.Owner = owner;
            this.DataContext = this;
            this.WarningLine1.Text = string.Format(AdditionalAssembliesWarning.Line1, "the current application");
            this.WarningLine2.Text = AdditionalAssembliesWarning.Line2;
            this.ExecutablePathTextBox.LostFocus += (s, e) =>
            {
                this.OKButton.IsEnabled = System.IO.File.Exists(this.ExecutablePathTextBox.Text);
            };
            this.ExecutablePathTextBox.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    this.OKButton.IsEnabled = System.IO.File.Exists(this.ExecutablePathTextBox.Text);
                }
            };
        }

        /// <summary>
        /// Gets the selected executable path.
        /// </summary>
        public string SelectedExecutablePath { get; private set; }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select an External Application",
                Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*",
                FilterIndex = 0,
                CheckFileExists = true,
                CheckPathExists = true,
            };

            if (openFileDialog.ShowDialog(this) == true)
            {
                this.ExecutablePathTextBox.Text = openFileDialog.FileName;
                this.OKButton.IsEnabled = true;
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.SelectedExecutablePath = this.ExecutablePathTextBox.Text;
            this.DialogResult = true;
            e.Handled = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            e.Handled = true;
        }
    }
}
