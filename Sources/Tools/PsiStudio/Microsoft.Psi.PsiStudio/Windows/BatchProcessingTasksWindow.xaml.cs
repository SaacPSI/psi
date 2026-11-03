// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio.Windows
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using System.Windows;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Data.Annotations;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.ViewModels;
    using Microsoft.Psi.Visualization.Windows;
    using Microsoft.Win32;
    using Newtonsoft.Json;

    /// <summary>
    /// Interaction logic for BatchProcessingTasksWindow.xaml.
    /// </summary>
    public partial class BatchProcessingTasksWindow : Window, INotifyPropertyChanged
    {
        private SessionViewModel sessionViewModel;
        private TrackEntry selectedTrack;
        private string selectedTrackStatistics = "No track selected.";

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchProcessingTasksWindow"/> class.
        /// </summary>
        public BatchProcessingTasksWindow()
        {
            this.InitializeComponent();
            this.Tracks = new ObservableCollection<TrackEntry>();
            this.DataContext = this;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchProcessingTasksWindow"/> class.
        /// </summary>
        /// <param name="owner">Owner window.</param>
        /// <param name="sessionViewModel">Current session view model.</param>
        /// <param name="taskAnnotationsByTrack">Task annotations grouped by track.</param>
        public BatchProcessingTasksWindow(Window owner, SessionViewModel sessionViewModel, Dictionary<string, List<TaskAnnotation>> taskAnnotationsByTrack)
            : this()
        {
            this.Owner = owner;
            this.sessionViewModel = sessionViewModel;
            this.Tracks.Clear();
            foreach (var track in taskAnnotationsByTrack
                .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                .Select(kvp => new TrackEntry(kvp.Key, kvp.Value.OrderBy(a => a.Interval.Left).ToList())))
            {
                this.Tracks.Add(track);
            }

            this.SelectedTrack = this.Tracks.FirstOrDefault();
        }

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the available task tracks.
        /// </summary>
        public ObservableCollection<TrackEntry> Tracks { get; }

        /// <summary>
        /// Gets or sets selected track.
        /// </summary>
        public TrackEntry SelectedTrack
        {
            get => this.selectedTrack;
            set
            {
                if (!ReferenceEquals(this.selectedTrack, value))
                {
                    this.selectedTrack = value;
                    this.RaisePropertyChanged();
                    this.UpdateSelectedTrackStatistics();
                }
            }
        }

        /// <summary>
        /// Gets statistics string for the selected track.
        /// </summary>
        public string SelectedTrackStatistics
        {
            get => this.selectedTrackStatistics;
            private set
            {
                if (this.selectedTrackStatistics != value)
                {
                    this.selectedTrackStatistics = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        private async void RunButtonClick(object sender, RoutedEventArgs e)
        {
            if (this.SelectedTrack == null)
            {
                _ = new MessageBoxWindow(this, "No Track Selected", "Please select a track before running tasks.", "Close", null).ShowDialog();
                return;
            }

            var allTasks = VisualizationContext.Instance.PluginMap.BatchProcessingTasks.ToList();
            if (!allTasks.Any())
            {
                _ = new MessageBoxWindow(this, "No Batch Processing Task", "No batch processing task is currently available.", "Close", null).ShowDialog();
                return;
            }

            try
            {
                this.IsEnabled = false;
                this.Cursor = System.Windows.Input.Cursors.Wait;

                int successCount = 0;
                int missingCount = 0;
                int failedCount = 0;

                foreach (var annotation in this.SelectedTrack.Annotations)
                {
                    if (string.IsNullOrWhiteSpace(annotation.Task))
                    {
                        missingCount++;
                        continue;
                    }

                    var taskMetadata = allTasks.FirstOrDefault(task => string.Equals(task.Name, annotation.Task, StringComparison.Ordinal));
                    if (taskMetadata == null)
                    {
                        missingCount++;
                        continue;
                    }

                    try
                    {
                        var configuration = this.CreateConfiguration(taskMetadata, annotation);
                        var batchProcessingTask = taskMetadata.CreateBatchProcessingTask();
                        await this.sessionViewModel.Session.RunBatchProcessingTaskAsync(batchProcessingTask, configuration);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        Debug.WriteLine($"Failed to run batch task '{annotation.Task}' for track '{this.SelectedTrack.TrackName}': {ex}");
                    }
                }

                await this.RefreshSessionAfterRunAsync();

                var summary = $"Track: {this.SelectedTrack.TrackName}\n"
                    + $"Executed: {successCount}\n"
                    + $"Missing Task: {missingCount}\n"
                    + $"Failed: {failedCount}";
                _ = new MessageBoxWindow(this, "Run Complete", summary, "Close", null).ShowDialog();
            }
            finally
            {
                this.Cursor = null;
                this.IsEnabled = true;
            }
        }

        private void ExportToJsonButtonClick(object sender, RoutedEventArgs e)
        {
            if (this.SelectedTrack == null)
            {
                _ = new MessageBoxWindow(this, "No Track Selected", "Please select a track to export.", "Close", null).ShowDialog();
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                FileName = $"{this.SelectedTrack.TrackName}.json",
                AddExtension = true,
                DefaultExt = ".json",
            };

            if (saveFileDialog.ShowDialog(this) != true)
            {
                return;
            }

            try
            {
                var payload = new
                {
                    Track = this.SelectedTrack.TrackName,
                    Statistics = new
                    {
                        Count = this.SelectedTrack.Annotations.Count,
                        DistinctTasks = this.SelectedTrack.Annotations.Select(a => a.Task).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.Ordinal).Count(),
                    },
                    Annotations = this.SelectedTrack.Annotations.Select(annotation => new
                    {
                        annotation.Track,
                        annotation.Task,
                        annotation.File,
                        Start = annotation.Interval.Left,
                        End = annotation.Interval.Right,
                        Parameters = annotation.GetParameters(),
                    }).ToList(),
                };

                File.WriteAllText(saveFileDialog.FileName, JsonConvert.SerializeObject(payload, Formatting.Indented));
                _ = new MessageBoxWindow(this, "Export Succeeded", $"Track exported to\n{saveFileDialog.FileName}", "Close", null).ShowDialog();
            }
            catch (Exception ex)
            {
                _ = new MessageBoxWindow(this, "Export Failed", ex.Message, "Close", null).ShowDialog();
            }
        }

        private void CloseButtonClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private BatchProcessingTaskConfiguration CreateConfiguration(BatchProcessingTaskMetadata taskMetadata, TaskAnnotation annotation)
        {
            var defaultConfiguration = taskMetadata.GetDefaultConfiguration();
            var parameters = annotation.GetParameters();
            if (string.IsNullOrWhiteSpace(parameters))
            {
                return defaultConfiguration;
            }

            try
            {
                var configuration = JsonConvert.DeserializeObject<Dictionary<string, object>>(parameters);
                if (configuration == null)
                {
                    return defaultConfiguration;
                }

                var compatibleStreamList = this.sessionViewModel.GetStreams()
                    .Where(s => taskMetadata.IsCompatibleWith(Type.GetType(s.TypeName, false, true)))
                    .Select(s => s.Name)
                    .ToList();

                configuration["CurrentDataset"] = this.sessionViewModel.DatasetViewModel.Dataset;
                configuration["StreamList"] = compatibleStreamList;

                return taskMetadata.GetDefaultConfigurationWithAttributes(configuration);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to parse task configuration for annotation task '{annotation.Task}': {ex}");
                return defaultConfiguration;
            }
        }

        private async Task RefreshSessionAfterRunAsync()
        {
            this.sessionViewModel.Update(this.sessionViewModel.Session);

            if (this.sessionViewModel.DatasetViewModel.FileName != null)
            {
                await this.sessionViewModel.DatasetViewModel.SaveAsAsync(this.sessionViewModel.DatasetViewModel.FileName);
            }

            VisualizationContext.Instance.VisualizationContainer.UpdateStreamSources(VisualizationContext.Instance.DatasetViewModel?.CurrentSessionViewModel);
            DataManager.Instance.ReadAndPublishStreamValue(VisualizationContext.Instance.VisualizationContainer.Navigator.Cursor);
        }

        private void UpdateSelectedTrackStatistics()
        {
            if (this.SelectedTrack == null || this.SelectedTrack.Annotations.Count == 0)
            {
                this.SelectedTrackStatistics = "No data available for the selected track.";
                return;
            }

            var annotations = this.SelectedTrack.Annotations;
            var firstStart = annotations.Min(a => a.Interval.Left);
            var lastEnd = annotations.Max(a => a.Interval.Right);
            var totalDuration = new TimeSpan(annotations.Sum(a => Math.Max(0, (a.Interval.Right - a.Interval.Left).Ticks)));
            var tasks = annotations
                .Select(a => a.Task)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(t => t, StringComparer.Ordinal)
                .ToList();

            this.SelectedTrackStatistics =
                $"Track: {this.SelectedTrack.TrackName}{Environment.NewLine}"
                + $"Annotations: {annotations.Count}{Environment.NewLine}"
                + $"Distinct tasks: {tasks.Count}{Environment.NewLine}"
                + $"First start: {firstStart:O}{Environment.NewLine}"
                + $"Last end: {lastEnd:O}{Environment.NewLine}"
                + $"Accumulated duration: {totalDuration}{Environment.NewLine}{Environment.NewLine}"
                + "Tasks:" + Environment.NewLine
                + (tasks.Count > 0 ? string.Join(Environment.NewLine, tasks.Select(t => $"- {t}")) : "- none");
        }

        private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
            => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// Model for one track entry.
        /// </summary>
        public class TrackEntry
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TrackEntry"/> class.
            /// </summary>
            /// <param name="trackName">Track name.</param>
            /// <param name="annotations">Track annotations.</param>
            public TrackEntry(string trackName, List<TaskAnnotation> annotations)
            {
                this.TrackName = trackName;
                this.Annotations = annotations;
            }

            /// <summary>
            /// Gets track name.
            /// </summary>
            public string TrackName { get; }

            /// <summary>
            /// Gets annotations.
            /// </summary>
            public List<TaskAnnotation> Annotations { get; }
        }
    }
}
