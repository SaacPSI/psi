// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationPanels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Input;
    using GalaSoft.MvvmLight.Command;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Panel for external application.
    /// </summary>
    public class ExternalApplicationVisualizationPanel : CanvasVisualizationPanel
    {
        private string applicationPath;
        private bool enableWindowSizing = true;
        private bool clipToViewport = true;
        private Axis xAxis = new ();
        private Axis yAxis = new ();
        private Thickness viewportPadding = new Thickness(0);
        private RelayCommand<RoutedEventArgs> viewportLoadedCommand;
        private RelayCommand<SizeChangedEventArgs> viewportSizeChangedCommand;
        private RelayCommand<MouseWheelEventArgs> mouseWheelCommand;
        private RelayCommand<MouseButtonEventArgs> mouseRightButtonDownCommand;
        private RelayCommand<MouseButtonEventArgs> mouseRightButtonUpCommand;
        private RelayCommand<MouseEventArgs> mouseMoveCommand;
        private RelayCommand mouseEnterCommand;
        private RelayCommand mouseLeaveCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalApplicationVisualizationPanel"/> class.
        /// </summary>
        /// <param name="applicationPath">The path to the external application.</param>
        public ExternalApplicationVisualizationPanel(string applicationPath)
        {
            this.applicationPath = applicationPath;
            this.Name = $"External Application - {System.IO.Path.GetFileNameWithoutExtension(applicationPath)}";

            this.XAxis.Maximum = this.Width;
            this.YAxis.Maximum = this.Height;
            this.XAxis.PropertyChanged += this.OnXAxisPropertyChanged;
            this.YAxis.PropertyChanged += this.OnYAxisPropertyChanged;

            // Subscribe to Width and Height changes on the panel itself
            this.PropertyChanged += this.OnPanelPropertyChanged;

            // Set initial panel size based on axis maximums
            this.CalculateDisplayArea(true);
        }

        /// <summary>
        /// Gets or sets the X Axis for the panel.
        /// </summary>
        [IgnoreDataMember]
        [PropertyOrder(5)]
        [ExpandableObject]
        [DisplayName("X Axis")]
        [Description("Specifies the extents of the X axis.")]
        public Axis XAxis
        {
            get => this.xAxis;
            set
            {
                this.Set(nameof(this.XAxis), ref this.xAxis, value);
                this.CalculateDisplayArea();
            }
        }

        /// <summary>
        /// Gets or sets the Y Axis for the panel.
        /// </summary>
        [IgnoreDataMember]
        [PropertyOrder(6)]
        [ExpandableObject]
        [DisplayName("Y Axis")]
        public Axis YAxis
        {
            get => this.yAxis;
            set
            {
                this.Set(nameof(this.YAxis), ref this.yAxis, value);
                this.CalculateDisplayArea();
            }
        }

        /// <summary>
        /// Gets the viewport loaded command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<RoutedEventArgs> ViewportLoadedCommand
            => this.viewportLoadedCommand ??= new RelayCommand<RoutedEventArgs>(
                e =>
                {
                    // Event source is the viewport
                    var viewport = e.Source as FrameworkElement;

                    this.Width = viewport.Width;
                    this.Height = viewport.Height;
                    this.CalculateDisplayArea();
                });

        /// <summary>
        /// Gets the items control size changed command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<SizeChangedEventArgs> ViewportSizeChangedCommand
            => this.viewportSizeChangedCommand ??= new RelayCommand<SizeChangedEventArgs>(
                e =>
                {
                    // Event source is the viewport
                    var viewport = e.Source as FrameworkElement;

                    this.Width = viewport.Width;
                    this.Height = viewport.Height;
                    this.CalculateDisplayArea();
                });

        /// <summary>
        /// Gets or sets the viewport padding.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public Thickness ViewportPadding
        {
            get => this.viewportPadding;
            set
            {
                if (this.viewportPadding != value)
                {
                    this.viewportPadding = value;
                    this.RaisePropertyChanged(nameof(this.ViewportPadding));
                }
            }
        }

        /// <summary>
        /// Gets the application path.
        /// </summary>
        [DataMember]
        public string ApplicationPath => this.applicationPath;

        /// <summary>
        /// Gets or sets a value indicating whether the external window should be resized to match the viewport.
        /// </summary>
        [DataMember]
        [PropertyOrder(3)]
        [DisplayName("Enable Window Sizing")]
        [Description("Specifies is the window can be automatically resized.")]
        public bool EnableWindowSizing
        {
            get => this.enableWindowSizing;
            set
            {
                if (this.enableWindowSizing != value)
                {
                    this.enableWindowSizing = value;
                    this.RaisePropertyChanged(nameof(this.EnableWindowSizing));
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the external window should be clipped to the viewport bounds.
        /// </summary>
        [DataMember]
        [PropertyOrder(4)]
        [DisplayName("Clip To Viewport")]
        [Description("Specifies if the external window should be clipped to the viewport bounds.")]
        public bool ClipToViewport
        {
            get => this.clipToViewport;
            set
            {
                if (this.clipToViewport != value)
                {
                    this.clipToViewport = value;
                    this.RaisePropertyChanged(nameof(this.ClipToViewport));
                }
            }
        }

        /// <summary>
        /// Gets the mouse wheel command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<MouseWheelEventArgs> MouseWheelCommand
            => this.mouseWheelCommand ??= new RelayCommand<MouseWheelEventArgs>(e => { });

        /// <summary>
        /// Gets the mouse right button down command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<MouseButtonEventArgs> MouseRightButtonDownCommand
            => this.mouseRightButtonDownCommand ??= new RelayCommand<MouseButtonEventArgs>(e => { });

        /// <summary>
        /// Gets the mouse right button up command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<MouseButtonEventArgs> MouseRightButtonUpCommand
            => this.mouseRightButtonUpCommand ??= new RelayCommand<MouseButtonEventArgs>(e => { });

        /// <summary>
        /// Gets the mouse move command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<MouseEventArgs> MouseMoveCommand
            => this.mouseMoveCommand ??= new RelayCommand<MouseEventArgs>(e => { });

        /// <summary>
        /// Gets the mouse enter command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand MouseEnterCommand
            => this.mouseEnterCommand ??= new RelayCommand(() => { });

        /// <summary>
        /// Gets the mouse leave command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand MouseLeaveCommand
            => this.mouseLeaveCommand ??= new RelayCommand(() => { });

        /// <inheritdoc/>
        public override List<VisualizationPanelType> CompatiblePanelTypes => new List<VisualizationPanelType>() { VisualizationPanelType.XYZ, VisualizationPanelType.XY, VisualizationPanelType.Canvas };

        /// <inheritdoc/>
        public override List<ContextMenuItemInfo> ContextMenuItemsInfo()
        {
            var contextMenuItems = base.ContextMenuItemsInfo();
            contextMenuItems.Add(
               new ContextMenuItemInfo(
                   null,
                   $"Load External Viewer",
                   VisualizationContext.Instance.LoadExternalViewer,
                   isEnabled: true,
                   commandParameter: this));
            return contextMenuItems;
        }

        /// <inheritdoc/>
        public override void Clear()
        {
            base.Clear();
            this.RaisePropertyChanged("Clear");
        }

        /// <inheritdoc />
        protected override DataTemplate CreateDefaultViewTemplate()
        {
            return XamlHelper.CreateTemplate(this.GetType(), typeof(ExternalApplicationPanelView));
        }

        /// <summary>
        /// Called when a property of the X axis has changed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event args for the event.</param>
        private void OnXAxisPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Axis.Range))
            {
                this.CalculateDisplayArea();
            }
        }

        /// <summary>
        /// Called when a property of the Y axis has changed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event args for the event.</param>
        private void OnYAxisPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Axis.Range))
            {
                this.CalculateDisplayArea();
            }
        }

        private void CalculateDisplayArea(bool triggerProperties = false)
        {
            // Update panel width based on XAxis.Maximum
            this.Width = this.XAxis.Maximum > 0 ? this.XAxis.Maximum : 400;

            // Update panel height based on YAxis.Maximum
            this.Height = this.YAxis.Maximum > 0 ? this.YAxis.Maximum : 400;

            if (triggerProperties)
            {
                this.RaisePropertyChanged("ViewportSizeChanged");
            }
        }

        private void OnPanelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.Width) || e.PropertyName == nameof(this.Height))
            {
                this.XAxis.Maximum = this.Width is double.NaN ? this.XAxis.Maximum : this.Width;
                this.YAxis.Maximum = this.Height is double.NaN ? this.YAxis.Maximum : this.Height;
                this.RaisePropertyChanged("ViewportSizeChanged");
            }
        }
    }
}
