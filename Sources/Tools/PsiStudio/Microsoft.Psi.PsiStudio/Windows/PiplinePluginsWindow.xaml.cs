// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Windows
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.PsiStudio;

    /// <summary>
    /// Interaction logic for PiplinePluginsWindow.xaml.
    /// </summary>
    public partial class PiplinePluginsWindow : Window
    {
        // private List<(string, string)> listing;

        /// <summary>
        /// Initializes a new instance of the <see cref="PiplinePluginsWindow"/> class.
        /// </summary>
        /// <param name="owner">The window owner.</param>
        /// <param name="pluginsList">Previous plugins loaded.</param>
        public PiplinePluginsWindow(Window owner, List<string> pluginsList = null)
        {
            this.InitializeComponent();
            this.Title = AdditionalAssembliesWarning.Title;
            this.WarningLine1.Text = string.Format(AdditionalAssembliesWarning.Line1, MainWindowViewModel.ApplicationName);
            this.WarningLine2.Text = AdditionalAssembliesWarning.Line2;
            this.WarningQuestion.Text = string.Format(AdditionalAssembliesWarning.Question, MainWindowViewModel.ApplicationName);
            this.PipelinePluginPath = null;
            List<(string, string)> listing = new List<(string, string)>();
            foreach (string plugin in pluginsList)
            {
                listing.Add((Path.GetFileName(plugin), plugin));
            }

            this.plugins.ItemsSource = listing;
            this.DataContext = this;
            this.Owner = owner;
        }

        /// <summary>
        /// Gets or sets the pipeline assembly the user wishes to use with PsiStudio.
        /// </summary>
        public string PipelinePluginPath { get; set; }

        /// <summary>
        /// Gets or sets dataset path.
        /// </summary>
        public PsiStudioPipelineAssemblyHandler PsiStudioPipeline { get; set; }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.PipelinePluginPath != null)
            {
                this.PsiStudioPipeline = PsiStudioPipelineAssemblyHandler.Load(this.PipelinePluginPath, true);
            }

            this.DialogResult = true;
            e.Handled = true;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension
            dlg.DefaultExt = ".dll";
            dlg.Filter = "Assembly (.dll)|*.dll";

            // Display OpenFileDialog by calling ShowDialog method
            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                this.PipelinePluginPath = dlg.FileName;
                List<(string, string)> listing = new List<(string, string)>();
                listing.Add((Path.GetFileName(this.PipelinePluginPath), this.PipelinePluginPath));
                this.plugins.ItemsSource = listing;
            }

            e.Handled = true;
        }

        private void Plugins_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var selection = this.plugins.SelectedItem;
            if (selection != null)
            {
                this.PipelinePluginPath = (((string, string))selection).Item2;
            }
        }
    }
}
