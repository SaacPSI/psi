// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio.Windows
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    /// <summary>
    /// Interaction logic for AnnotationSearchWindow.xaml.
    /// </summary>
    public partial class AnnotationSearchWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationSearchWindow"/> class.
        /// </summary>
        /// <param name="owner">The owner window.</param>
        public AnnotationSearchWindow(Window owner)
        {
            this.InitializeComponent();
            this.Owner = owner;
            AnnotationSearchWindowViewModel model = new AnnotationSearchWindowViewModel();
            model.LoadAvailableStreams();
            this.DataContext = model;
        }

        private void FindAllResults_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.DataContext is AnnotationSearchWindowViewModel model)
            {
                var result = (sender as ListBox)?.SelectedItem as Search.AnnotationSearchResult;
                if (result != null && model.SelectResultCommand.CanExecute(result))
                {
                    model.SelectResultCommand.Execute(result);
                }
            }
        }
    }
}
