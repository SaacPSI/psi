// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio.Search
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;
    using Microsoft.Psi.Data.Annotations;
    using Microsoft.Psi.Visualization;

    /// <summary>
    /// Provides search capabilities for annotations within a stream.
    /// </summary>
    /// <remarks>
    /// The search engine queries annotation streams using keyword filters and supports both
    /// forward and backward searches from a cursor position. It maintains an in-memory cache
    /// of annotations for efficient searching. Usage example:
    /// <code>
    /// var engine = new AnnotationSearchEngine();
    /// engine.LoadAnnotations(annotationStream);
    /// var filter = new AnnotationSearchFilter("keyword");
    /// var result = engine.FindNext(cursorTime, filter);
    /// if (result != null)
    /// {
    ///     Console.WriteLine($"Found: {result.Annotation.Track} at {result.Annotation.Interval.Left}");
    /// }
    /// </code>
    /// </remarks>
    public class AnnotationSearchEngine
    {
        private readonly AnnotationStreamCache cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationSearchEngine"/> class.
        /// </summary>
        public AnnotationSearchEngine()
        {
            this.cache = new AnnotationStreamCache();
        }

        /// <summary>
        /// Gets the cache version. Useful for detecting when cache has been invalidated.
        /// </summary>
        public int CacheVersion => this.cache.CacheVersion;

        /// <summary>
        /// Loads annotations into the search engine's cache from a provided enumerable.
        /// </summary>
        /// <param name="annotations">The annotations to index for searching.</param>
        public virtual void LoadAnnotations(IEnumerable<TimeIntervalAnnotation> annotations)
        {
            if (annotations == null)
            {
                throw new ArgumentNullException(nameof(annotations));
            }

            this.cache.LoadAnnotations(annotations);
        }

        /// <summary>
        /// Invalidates the cache, forcing a reload of annotations on the next search.
        /// </summary>
        public void InvalidateCache()
        {
            this.cache.Invalidate();
        }

        /// <summary>
        /// Finds the next annotation after the specified cursor time that matches the filter.
        /// </summary>
        /// <param name="cursorTime">The reference time to search from.</param>
        /// <param name="filter">The search filter criteria.</param>
        /// <param name="setAsSelection">Whether to set the found annotation as the current selection.</param>
        /// <returns>The closest matching annotation after the cursor time, or null if none found.</returns>
        public AnnotationSearchResult FindNext(DateTime cursorTime, AnnotationSearchFilter filter, bool setAsSelection)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            if (this.cache.IsEmpty)
            {
                return null;
            }

            // Find the first annotation at or after the cursor time
            int startIndex = this.cache.FindFirstAnnotationIndexAtOrAfter(cursorTime);
            if (startIndex == -1)
            {
                return null;
            }

            // Search forward from this position
            var allAnnotations = this.cache.GetAllAnnotations();
            for (int i = startIndex; i < allAnnotations.Count; i++)
            {
                var annotation = allAnnotations[i];
                double matchScore = filter.IsValid() ? this.CalculateMatchScore(annotation, filter) : 1.0;

                if (matchScore > 0.0)
                {
                    var distance = annotation.Interval.Left - cursorTime;
                    VisualizationContext.Instance.VisualizationContainer.ZoomToRange(annotation.Interval);
                    if (setAsSelection)
                    {
                        VisualizationContext.Instance.VisualizationContainer.Navigator.SelectionRange.Set(annotation.Interval);
                    }

                    return new AnnotationSearchResult(annotation, distance, matchScore, filter.AttributeNames);
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the previous annotation before the specified cursor time that matches the filter.
        /// </summary>
        /// <param name="cursorTime">The reference time to search from.</param>
        /// <param name="filter">The search filter criteria.</param>
        /// <param name="setAsSelection">Whether to set the found annotation as the current selection.</param>
        /// <returns>The closest matching annotation before the cursor time, or null if none found.</returns>
        public AnnotationSearchResult FindPrevious(DateTime cursorTime, AnnotationSearchFilter filter, bool setAsSelection)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            if (this.cache.IsEmpty)
            {
                return null;
            }

            // Find the last annotation before the cursor time
            int startIndex = this.cache.FindLastAnnotationIndexBefore(cursorTime);
            if (startIndex == -1)
            {
                return null;
            }

            // Search backward from this position
            var allAnnotations = this.cache.GetAllAnnotations();
            for (int i = startIndex; i >= 0; i--)
            {
                var annotation = allAnnotations[i];
                double matchScore = filter.IsValid() ? this.CalculateMatchScore(annotation, filter) : 1.0;

                if (matchScore > 0.0)
                {
                    var distance = annotation.Interval.Left - cursorTime;
                    VisualizationContext.Instance.VisualizationContainer.ZoomToRange(annotation.Interval);
                    if (setAsSelection)
                    {
                        VisualizationContext.Instance.VisualizationContainer.Navigator.SelectionRange.Set(annotation.Interval);
                    }

                    return new AnnotationSearchResult(annotation, distance, matchScore, filter.AttributeNames);
                }
            }

            return null;
        }

        /// <summary>
        /// Finds all annotations in the stream that match the filter criteria.
        /// </summary>
        /// <param name="filter">The search filter criteria.</param>
        /// <returns>A list of all matching annotations, sorted by time.</returns>
        public List<AnnotationSearchResult> FindAll(AnnotationSearchFilter filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            var results = new List<AnnotationSearchResult>();

            if (this.cache.IsEmpty)
            {
                return results;
            }

            var allAnnotations = this.cache.GetAllAnnotations();
            foreach (var annotation in allAnnotations)
            {
                double matchScore = filter.IsValid() ? this.CalculateMatchScore(annotation, filter) : 1.0;
                if (matchScore > 0.0)
                {
                    // Use the start time as reference for distance calculation
                    var distance = annotation.Interval.Left - allAnnotations[0].Interval.Left;
                    results.Add(new AnnotationSearchResult(annotation, distance, matchScore, filter.AttributeNames));
                }
            }

            return results;
        }

        /// <summary>
        /// Gets the list of attribute texts that match any of the provided keywords.
        /// </summary>
        /// <param name="keywords">The keywords to search for.</param>
        /// <param name="attributeTexts">The attribute texts to search in.</param>
        /// <returns>Required. A list of matching attribute texts.</returns>
        public List<string> GetAttributes(string[] keywords, IEnumerable<string> attributeTexts)
        {
            List<string> matchingAttributes = new List<string>();

            // Normalize all texts to lowercase for case-insensitive comparison
            var normalizedTexts = keywords.Select(t => t.ToLowerInvariant()).ToList();

            // Check each keyword against the attribute texts
            foreach (string attribute in attributeTexts)
            {
                string normalizedAttribute = attribute.ToLowerInvariant();
                bool keywordMatched = normalizedTexts.Any(text => text.Contains(normalizedAttribute));
                if (keywordMatched)
                {
                    matchingAttributes.Add(attribute);
                }
            }

            return matchingAttributes;
        }

        /// <summary>
        /// Calculates a match score for an annotation against a search filter.
        /// </summary>
        /// <remarks>
        /// The match score is between 0.0 (no match) and 1.0 (perfect match).
        /// Score is based on:
        /// - Whether all required keywords match.
        /// - Number of attribute keywords matched.
        /// </remarks>
        /// <param name="annotation">The annotation to score.</param>
        /// <param name="filter">The search filter criteria.</param>
        /// <returns>A score between 0.0 and 1.0.</returns>
        private double CalculateMatchScore(TimeIntervalAnnotation annotation, AnnotationSearchFilter filter)
        {
            if (annotation == null || filter == null)
            {
                return 0.0;
            }

            var attributeTexts = this.ExtractKeywordsFromAttributes(annotation, filter);
            if (attributeTexts.Count == 0)
            {
                return 0.0;
            }

            // Normalize all texts to lowercase for case-insensitive comparison
            var normalizedTexts = attributeTexts.Select(t => t.ToLowerInvariant()).ToList();
            int matchCount = 0;

            // Check each keyword against the attribute texts
            foreach (var keyword in filter.Keywords)
            {
                bool keywordMatched = normalizedTexts.Any(text => text.Contains(keyword));
                if (keywordMatched)
                {
                    matchCount++;
                }
            }

            // If requireAllKeywords is true, all keywords must match
            if (filter.RequireAllKeywords && matchCount < filter.Keywords.Count)
            {
                return 0.0;
            }

            // If requireAllKeywords is false, at least one keyword must match
            if (!filter.RequireAllKeywords && matchCount == 0)
            {
                return 0.0;
            }

            // Calculate score: ratio of matched keywords to total keywords
            double score = (double)matchCount / filter.Keywords.Count;
            return Math.Min(1.0, score);
        }

        /// <summary>
        /// Extracts keyword texts from all attributes of an annotation.
        /// </summary>
        /// <param name="annotation">The annotation to extract from.</param>
        /// <param name="filter">The search filter (to determine which attributes to include).</param>
        /// <returns>A list of text values from annotation attributes.</returns>
        private List<string> ExtractKeywordsFromAttributes(TimeIntervalAnnotation annotation, AnnotationSearchFilter filter)
        {
            var results = new List<string>();

            if (annotation?.AttributeValues == null || annotation.AttributeValues.Count == 0)
            {
                return results;
            }

            // If specific attribute names are requested, filter to those
            if (filter.AttributeNames != null && filter.AttributeNames.Count > 0)
            {
                foreach (var attributeName in filter.AttributeNames)
                {
                    if (annotation.AttributeValues.TryGetValue(attributeName, out var value))
                    {
                        if (value != null && !string.IsNullOrWhiteSpace(value.ValueAsString))
                        {
                            results.Add(value.ValueAsString);
                        }
                    }
                }
            }
            else
            {
                // Include all attributes
                foreach (var kvp in annotation.AttributeValues)
                {
                    if (kvp.Value != null && !string.IsNullOrWhiteSpace(kvp.Value.ValueAsString))
                    {
                        results.Add(kvp.Value.ValueAsString);
                    }
                }
            }

            return results;
        }
    }
}
