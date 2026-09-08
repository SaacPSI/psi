// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio.Windows
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.PsiStudio.Search;
    using Microsoft.Psi.Visualization;

    /// <summary>
    /// ViewModel for the annotation search window.
    /// </summary>
    public class AnnotationSearchWindowViewModel : ViewModelBase
    {
        private string searchText = string.Empty;
        private bool requireAllKeywords = false;
        private bool setAsSelection = true;
        private string status = string.Empty;
        private string selectedStream = null;
        private IReadOnlyList<string> availableAttributes = null;
        private AnnotationStreamProvider streamProvider;
        private AnnotationStreamCache cache;
        private AnnotationSearchEngine searchEngine;
        private DateTime currentCursorTime = DateTime.UtcNow;
        private AnnotationSearchResult currentResult;
        private RelayCommand searchNextCommand;
        private RelayCommand searchPreviousCommand;
        private RelayCommand searchAllCommand;
        private RelayCommand loadStreamCommand;
        private RelayCommand<AnnotationSearchResult> selectResultCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationSearchWindowViewModel"/> class.
        /// </summary>
        public AnnotationSearchWindowViewModel()
        {
            this.AllResults = new ObservableCollection<AnnotationSearchResult>();
            this.AvailableStreams = new ObservableCollection<string>();
            this.streamProvider = new AnnotationStreamProvider();
            this.cache = new AnnotationStreamCache();
            this.searchEngine = new AnnotationSearchEngine();
            this.Status = "Ready. Please select an annotation stream and click Load.";
        }

        /// <summary>
        /// Gets or sets the search text entered by the user.
        /// </summary>
        public string SearchText
        {
            get => this.searchText;
            set
            {
                if (this.searchText != value)
                {
                    this.searchText = value;
                    this.RaisePropertyChanged();
                    this.searchNextCommand?.RaiseCanExecuteChanged();
                    this.searchPreviousCommand?.RaiseCanExecuteChanged();
                    this.searchAllCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether all keywords must match.
        /// </summary>
        public bool RequireAllKeywords
        {
            get => this.requireAllKeywords;
            set
            {
                if (this.requireAllKeywords != value)
                {
                    this.requireAllKeywords = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the found annotation should be set as the current selection.
        /// </summary>
        public bool SetAsSelection
        {
            get => this.setAsSelection;
            set
            {
                if (this.setAsSelection != value)
                {
                    this.setAsSelection = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets the status message.
        /// </summary>
        public string Status
        {
            get => this.status;
            private set
            {
                if (this.status != value)
                {
                    this.status = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets the list of available annotation streams.
        /// </summary>
        public ObservableCollection<string> AvailableStreams { get; }

        /// <summary>
        /// Gets or sets the selected annotation stream.
        /// </summary>
        public string SelectedStream
        {
            get => this.selectedStream;
            set
            {
                if (this.selectedStream != value)
                {
                    this.selectedStream = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets the list of all search results.
        /// </summary>
        public ObservableCollection<AnnotationSearchResult> AllResults { get; }

        /// <summary>
        /// Gets or sets the current search result.
        /// </summary>
        public AnnotationSearchResult CurrentResult
        {
            get => this.currentResult;
            set
            {
                if (this.currentResult != value)
                {
                    this.currentResult = value;
                    this.RaisePropertyChanged(nameof(this.CurrentResult));
                    if (this.currentResult != null && this.currentResult.Interval != null)
                    {
                        this.currentCursorTime = this.currentResult.Interval.Left;
                    }
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether there are results.
        /// </summary>
        public bool HasResults => this.AllResults.Count > 0;

        /// <summary>
        /// Gets index statistics for display.
        /// </summary>
        public string IndexStatistics
        {
            get
            {
                if (this.cache != null && this.cache.Count > 0)
                {
                    return $"Cached: {this.cache.Count} annotations";
                }

                return "No annotations loaded";
            }
        }

        /// <summary>
        /// Gets the search next command.
        /// </summary>
        public RelayCommand SearchNextCommand
            => this.searchNextCommand ??= new RelayCommand(
                this.SearchNext);

        /// <summary>
        /// Gets the search previous command.
        /// </summary>
        public RelayCommand SearchPreviousCommand
            => this.searchPreviousCommand ??= new RelayCommand(
                this.SearchPrevious);

        /// <summary>
        /// Gets the search all command.
        /// </summary>
        public RelayCommand SearchAllCommand
            => this.searchAllCommand ??= new RelayCommand(
                this.SearchAll);

        /// <summary>
        /// Gets the load stream command.
        /// </summary>
        public RelayCommand LoadStreamCommand
            => this.loadStreamCommand ??= new RelayCommand(
                this.LoadStream,
                () => !string.IsNullOrWhiteSpace(this.SelectedStream));

        /// <summary>
        /// Gets the select result command (called on double-click).
        /// </summary>
        public RelayCommand<AnnotationSearchResult> SelectResultCommand
            => this.selectResultCommand ??= new RelayCommand<AnnotationSearchResult>(
                this.SelectResult);

        /// <summary>
        /// Gets the list of available streams.
        /// </summary>
        public void LoadAvailableStreams()
        {
            try
            {
                this.AvailableStreams.Clear();
                var streams = this.streamProvider.GetAvailableAnnotationStreams();
                foreach (var stream in streams)
                {
                    this.AvailableStreams.Add(stream.Item1);
                }

                if (this.AvailableStreams.Count == 0)
                {
                    this.Status = "No annotation streams found. Please load a session with annotations.";
                }
                else
                {
                    this.Status = $"Found {this.AvailableStreams.Count} annotation stream(s). Select one and click Load.";
                }
            }
            catch (Exception ex)
            {
                this.Status = $"Error loading streams: {ex.Message}";
            }
        }

        private bool CanSearch() => this.cache.Count > 0;

        private void LoadStream()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(this.SelectedStream))
                {
                    this.Status = "Please select a stream first.";
                    return;
                }

                this.Status = "Loading annotations...";
                var annotations = this.streamProvider.LoadAnnotationsFromStream(this.SelectedStream);
                if (annotations.Count > 0)
                {
                    this.availableAttributes = annotations.First().AttributeValues.Keys.ToList();
                    this.currentCursorTime = annotations.Min(a => a.Interval.Left);
                }

                this.searchEngine.LoadAnnotations(annotations);

                this.Status = $"Loaded {annotations.Count} annotations from '{this.SelectedStream}'.";
                this.RaisePropertyChanged(nameof(this.IndexStatistics));
            }
            catch (Exception ex)
            {
                this.Status = $"Error loading stream: {ex.Message}";
            }
        }

        private void SearchNext()
        {
            try
            {
                var keywords = this.SearchText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var filter = new AnnotationSearchFilter(
                    keywords,
                    this.searchEngine.GetAttributes(keywords, this.availableAttributes),
                    this.RequireAllKeywords);

                var result = this.searchEngine.FindNext(this.currentCursorTime, filter, this.SetAsSelection);
                if (result != null)
                {
                    this.CurrentResult = result;
                    this.Status = $"Found: {result.Track} at {result.Interval.Left}";
                    this.currentCursorTime = result.Interval.Right;
                }
                else
                {
                    this.Status = "No more results found.";
                }
            }
            catch (Exception ex)
            {
                this.Status = $"Error searching: {ex.Message}";
            }
        }

        private void SearchPrevious()
        {
            try
            {
                var keywords = this.SearchText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var filter = new AnnotationSearchFilter(
                    keywords,
                    this.searchEngine.GetAttributes(keywords, this.availableAttributes),
                    this.RequireAllKeywords);

                var result = this.searchEngine.FindPrevious(this.currentCursorTime, filter, this.SetAsSelection);
                if (result != null)
                {
                    this.CurrentResult = result;
                    this.Status = $"Found: {result.Track} at {result.Interval.Left}";
                    this.currentCursorTime = result.Interval.Left;
                }
                else
                {
                    this.Status = "No more results found.";
                }
            }
            catch (Exception ex)
            {
                this.Status = $"Error searching: {ex.Message}";
            }
        }

        private void SearchAll()
        {
            try
            {
                var keywords = this.SearchText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var filter = new AnnotationSearchFilter(
                    keywords,
                    this.searchEngine.GetAttributes(keywords, this.availableAttributes),
                    this.RequireAllKeywords);

                var results = this.searchEngine.FindAll(filter);
                this.AllResults.Clear();
                foreach (var result in results.OrderBy(r => r.Interval.Left))
                {
                    this.AllResults.Add(result);
                }

                if (this.AllResults.Count > 0)
                {
                    this.CurrentResult = this.AllResults[0];
                    this.Status = $"Found {this.AllResults.Count} result(s).";
                }
                else
                {
                    this.Status = "No results found.";
                }

                this.RaisePropertyChanged(nameof(this.HasResults));
            }
            catch (Exception ex)
            {
                this.Status = $"Error searching: {ex.Message}";
            }
        }

        /// <summary>
        /// Selects a result from the list (called by double-click via command).
        /// </summary>
        /// <param name="result">The result to select.</param>
        private void SelectResult(AnnotationSearchResult result)
        {
            if (result != null)
            {
                this.CurrentResult = result;
                VisualizationContext.Instance.VisualizationContainer.ZoomToRange(result.Interval);
                if (this.SetAsSelection)
                {
                    VisualizationContext.Instance.VisualizationContainer.Navigator.SelectionRange.Set(result.Interval);
                }

                this.Status = $"Selected: {result.Track} at {result.Interval.Left}";
            }
        }
    }
}
