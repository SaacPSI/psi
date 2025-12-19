// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Interaction logic for ExternalApplicationView.xaml.
    /// </summary>
    public partial class ExternalApplicationPanelView : InstantVisualizationPanelView, IDisposable
    {
        private System.Diagnostics.ProcessStartInfo processStartInfo;
        private System.Diagnostics.Process process;
        private Point refPoint;
        private bool lastVisibleState;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalApplicationPanelView"/> class.
        /// </summary>
        public ExternalApplicationPanelView()
        {
            this.refPoint = new Point(0.0, 0.0);
            this.InitializeComponent();
            this.DataContextChanged += this.OnDataContextChanged;
            this.Loaded += this.OnViewLoaded;
            this.lastVisibleState = true;
        }

        private ExternalApplicationVisualizationPanel Panel => this.DataContext as ExternalApplicationVisualizationPanel;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.process != null && !this.process.HasExited)
            {
                this.process.Kill();
                this.process.Dispose();
                this.process = null;
            }
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.DataContext == null || this.process != null)
            {
                this.Dispose();
                return;
            }
            else if (this.Panel == null || this.Panel.ApplicationPath == null || !System.IO.File.Exists(this.Panel.ApplicationPath))
            {
                return;
            }

            try
            {
                this.processStartInfo = new System.Diagnostics.ProcessStartInfo(this.Panel.ApplicationPath);
                this.processStartInfo.UseShellExecute = false;
                this.process = System.Diagnostics.Process.Start(this.processStartInfo);
                this.process.WaitForInputIdle();
            }
            catch (Exception)
            {
                return;
            }

            this.Panel.PropertyChanged += this.OnParentPanelPropertyChanged;

            // Embed the external process window as a child of the current window
            if (this.process.MainWindowHandle != IntPtr.Zero)
            {
                IntPtr parentHandle = new System.Windows.Interop.WindowInteropHelper(Window.GetWindow(Application.Current.MainWindow)).Handle;
                NativeMethods.SetParent(this.process.MainWindowHandle, parentHandle);

                // Configure window styles for proper embedding
                this.ConfigureWindowStyles();

                // Set up clipping if configured
                if (this.Panel.ClipToViewport)
                {
                    NativeMethods.SetWindowRgn(this.process.MainWindowHandle, IntPtr.Zero, true);
                }
            }
        }

        private void OnParentPanelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (this.Panel == null)
            {
                return;
            }

            switch (e.PropertyName)
            {
                case "IsShown":
                    this.CheckVisibilityState();
                    break;
                case "ViewportSizeChanged":
                    this.ResizeProcessWindow();
                    break;
                case "Clear":
                    this.Dispose();
                    break;
            }
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            if (this.process != null && this.process.MainWindowHandle != IntPtr.Zero && this.Panel?.EnableWindowSizing == true)
            {
                this.ResizeProcessWindow();
            }
        }

        private void ConfigureWindowStyles()
        {
            // Remove window borders and title bar
            int style = NativeMethods.GetWindowLong(this.process.MainWindowHandle, NativeMethods.GWL_STYLE);
            style &= ~NativeMethods.WS_CAPTION;
            style &= ~NativeMethods.WS_BORDER;
            style &= ~NativeMethods.WS_DLGFRAME;

            // Disable manual resizing to prevent user from resizing the window
            style &= ~NativeMethods.WS_NORESIZE;
            style |= NativeMethods.WS_NORESIZE;

            NativeMethods.SetWindowLong(this.process.MainWindowHandle, NativeMethods.GWL_STYLE, style);

            // Position and size the window
            this.ResizeProcessWindow();
        }

        private Point GetRealtivePosition()
        {
            Vector diff = this.PointToScreen(this.refPoint) - Application.Current.MainWindow.PointToScreen(this.refPoint);
            return new Point(diff.X, diff.Y);
        }

        private void CheckVisibilityState()
        {
            if (this.Panel != null)
            {
                if (this.Panel.IsShown && !this.lastVisibleState)
                {
                    NativeMethods.ShowWindow(this.process.MainWindowHandle, NativeMethods.SW_SHOW);
                    this.lastVisibleState = true;
                }
                else if (!this.Panel.IsShown && this.lastVisibleState)
                {
                    NativeMethods.ShowWindow(this.process.MainWindowHandle, NativeMethods.SW_HIDE);
                    this.lastVisibleState = false;
                }
            }
        }

        private void ResizeProcessWindow()
        {
            if (this.Panel != null)
            {
                this.CheckVisibilityState();
                if (this.Panel.IsShown)
                {
                    Point offset = this.GetRealtivePosition();
                    NativeMethods.SetWindowPos(
                      this.process.MainWindowHandle,
                      NativeMethods.HWND_BOTTOM,
                      (int)offset.X + (int)this.Panel.XAxis.Minimum,
                      (int)offset.Y + (int)this.Panel.YAxis.Minimum,
                      (int)this.Panel.XAxis.Maximum,
                      (int)this.Panel.YAxis.Maximum,
                      NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_FRAMECHANGED);
                    this.Height = this.Panel.YAxis.Maximum;
                    this.Width = this.Panel.XAxis.Maximum;
                }
            }
        }

        /// <summary>
        /// Static class to handle native calls.
        /// </summary>
        internal static class NativeMethods
        {
            /// <summary>
            /// GWL_STYLE constant for window styles.
            /// </summary>
            internal const int GWL_STYLE = -16;

            /// <summary>
            /// Window style: caption bar.
            /// </summary>
            internal const int WS_CAPTION = 0xC00000;

            /// <summary>
            /// Window style: border.
            /// </summary>
            internal const int WS_BORDER = 0x800000;

            /// <summary>
            /// Window style: dialog frame.
            /// </summary>
            internal const int WS_DLGFRAME = 0x400000;

            /// <summary>
            /// Window style: no manual resizeable frame.
            /// </summary>
            internal const int WS_NORESIZE = 0x840000;

            /// <summary>
            /// SetWindowPos flag: don't change the Z-order.
            /// </summary>
            internal const int SWP_NOZORDER = 0x0004;

            /// <summary>
            /// SetWindowPos flag: frame changes.
            /// </summary>
            internal const int SWP_FRAMECHANGED = 0x0020;

            /// <summary>
            /// SetWindowPos flag: don't activate the window.
            /// </summary>
            internal const int SWP_NOACTIVATE = 0x0010;

            /// <summary>
            /// ShowWindow command: hide the window.
            /// </summary>
            internal const int SW_HIDE = 0;

            /// <summary>
            /// ShowWindow command: show the window.
            /// </summary>
            internal const int SW_SHOW = 5;

            /// <summary>
            /// Special handle for HWND_BOTTOM (places window at bottom of z-order).
            /// </summary>
#pragma warning disable SA1401 // FieldsMustBePrivate
            internal static IntPtr HWND_BOTTOM = new IntPtr(1);
#pragma warning restore SA1401 // FieldsMustBePrivate

            /// <summary>
            /// Sets the parent window.
            /// </summary>
            /// <param name="hWndChild">The child window handle.</param>
            /// <param name="hWndNewParent">The new parent window handle.</param>
            /// <returns>The handle to the previous parent window.</returns>
            [DllImport("user32.dll", SetLastError = true)]
            internal static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

            /// <summary>
            /// Gets the window long.
            /// </summary>
            /// <param name="hWnd">The window handle.</param>
            /// <param name="nIndex">The index.</param>
            /// <returns>The window long value.</returns>
            [DllImport("user32.dll", SetLastError = true)]
            internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);

            /// <summary>
            /// Sets the window long.
            /// </summary>
            /// <param name="hWnd">The window handle.</param>
            /// <param name="nIndex">The index.</param>
            /// <param name="dwNewLong">The new long value.</param>
            /// <returns>The previous window long value.</returns>
            [DllImport("user32.dll", SetLastError = true)]
            internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

            /// <summary>
            /// Sets the window position and size.
            /// </summary>
            /// <param name="hWnd">The window handle.</param>
            /// <param name="hWndInsertAfter">The z-order window handle.</param>
            /// <param name="x">The x position.</param>
            /// <param name="y">The y position.</param>
            /// <param name="cx">The width.</param>
            /// <param name="cy">The height.</param>
            /// <param name="uFlags">The flags.</param>
            /// <returns>True if successful; otherwise, false.</returns>
            [DllImport("user32.dll", SetLastError = true)]
            internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int uFlags);

            /// <summary>
            /// Sets the window region.
            /// </summary>
            /// <param name="hWnd">The window handle.</param>
            /// <param name="hRgn">The region handle.</param>
            /// <param name="bRedraw">Whether to redraw the window.</param>
            /// <returns>The previous region.</returns>
            [DllImport("user32.dll", SetLastError = true)]
            internal static extern IntPtr SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

            /// <summary>
            /// Shows or hides the window.
            /// </summary>
            /// <param name="hWnd">The window handle.</param>
            /// <param name="nCmdShow">The command to execute (SW_HIDE, SW_SHOW, etc.).</param>
            /// <returns>True if the window was previously visible; otherwise, false.</returns>
            [DllImport("user32.dll", SetLastError = true)]
            internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        }
    }
}
