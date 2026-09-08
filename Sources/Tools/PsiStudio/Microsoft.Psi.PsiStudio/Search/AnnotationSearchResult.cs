// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio.Search
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Data.Annotations;

    /// <summary>
    /// Represents the result of an annotation search operation.
    /// </summary>
    /// <remarks>
    /// This class encapsulates information about a found annotation including the annotation itself,
    /// the temporal distance from the search cursor, and a relevance score indicating how well
    /// the annotation matches the search criteria.
    /// </remarks>
    public class AnnotationSearchResult
    {
        private IReadOnlyList<string> filterAttributeNames;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationSearchResult"/> class.
        /// </summary>
        /// <param name="annotation">The annotation that was found.</param>
        /// <param name="distanceFromCursor">The temporal distance (TimeSpan) from the search cursor to the annotation.</param>
        /// <param name="matchScore">A relevance score indicating how well the annotation matches the search criteria (0.0-1.0).</param>
        /// <param name="filterAttributeNames">The names of the attributes to filter on.</param>
        public AnnotationSearchResult(TimeIntervalAnnotation annotation, TimeSpan distanceFromCursor, double matchScore, IReadOnlyList<string> filterAttributeNames)
        {
            if (annotation == null)
            {
                throw new ArgumentNullException(nameof(annotation));
            }

            if (matchScore < 0.0 || matchScore > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(matchScore), "Match score must be between 0.0 and 1.0.");
            }

            this.Annotation = annotation;
            this.DistanceFromCursor = distanceFromCursor;
            this.MatchScore = matchScore;
            this.filterAttributeNames = filterAttributeNames;
        }

        /// <summary>
        /// Gets the annotation that was found.
        /// </summary>
        public TimeIntervalAnnotation Annotation { get; }

        /// <summary>
        /// Gets the temporal distance from the search cursor to the annotation.
        /// Positive values indicate annotations after the cursor, negative values indicate annotations before.
        /// </summary>
        public TimeSpan DistanceFromCursor { get; }

        /// <summary>
        /// Gets a relevance score indicating how well the annotation matches the search criteria.
        /// Score ranges from 0.0 (poor match) to 1.0 (perfect match).
        /// </summary>
        public double MatchScore { get; }

        /// <summary>
        /// Gets the time interval of the annotation.
        /// </summary>
        public TimeInterval Interval => this.Annotation.Interval;

        /// <summary>
        /// Gets the track name of the annotation.
        /// </summary>
        public string Track => this.Annotation.Track;

        /// <summary>
        /// Gets the text of the annotation.
        /// </summary>
        public string Text => this.DisplayText();

        /// <summary>
        /// Gets the text to display for the annotation based on attribute keys. It will display only the values of the attributes NOT specified in the filterAttributeNames list. If the list is empty or null, it will display all attribute values.
        /// </summary>
        /// <returns>The string representation of the attributes value.</returns>
        public string DisplayText()
        {
            string value = string.Empty;

            if (this.filterAttributeNames == null || this.filterAttributeNames.Count == 0)
            {
                foreach (var attribute in this.Annotation.AttributeValues)
                {
                    value += attribute.Value.ValueAsString + " ";
                }
            }
            else
            {
                foreach (var attributeName in this.filterAttributeNames)
                {
                    if (!this.Annotation.AttributeValues.ContainsKey(attributeName))
                    {
                        value += this.Annotation.AttributeValues[attributeName].ValueAsString + " ";
                    }
                }
            }

            return value.Length > 0 ? value.Remove(value.Length - 1, 1) : this.Annotation.AttributeValues.Values.First().ValueAsString;
        }
    }
}
