// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio.Search
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents a search filter for querying annotations based on keywords and attribute criteria.
    /// </summary>
    /// <remarks>
    /// This filter allows searching annotation streams for entries containing specific keywords
    /// across all attribute values. The search is case-insensitive and supports multiple keywords
    /// with optional matching requirements (all keywords must match or any keyword match).
    /// </remarks>
    public class AnnotationSearchFilter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationSearchFilter"/> class.
        /// </summary>
        /// <param name="keywords">The keywords to search for in annotation attribute values.</param>
        /// <param name="attributeNames">Optional list of specific attribute names to search within. If null, searches all attributes.</param>
        /// <param name="requireAllKeywords">If true, all keywords must match; if false, any keyword match is sufficient.</param>
        public AnnotationSearchFilter(IEnumerable<string> keywords, IEnumerable<string> attributeNames = null, bool requireAllKeywords = false)
        {
            if (keywords == null)
            {
                throw new ArgumentNullException(nameof(keywords));
            }

            // Normalize keywords to lowercase for case-insensitive comparison
            this.Keywords = keywords
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Select(k => k.ToLowerInvariant())
                .ToList();

            this.AttributeNames = attributeNames != null ? attributeNames.ToList() : null;
            this.RequireAllKeywords = requireAllKeywords;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationSearchFilter"/> class with a single keyword.
        /// </summary>
        /// <param name="keyword">The keyword to search for.</param>
        /// <param name="attributeNames">Optional list of specific attribute names to search within.</param>
        public AnnotationSearchFilter(string keyword, IEnumerable<string> attributeNames = null)
            : this(new[] { keyword }, attributeNames, requireAllKeywords: true)
        {
        }

        /// <summary>
        /// Gets the collection of keywords to search for.
        /// </summary>
        public IReadOnlyList<string> Keywords { get; }

        /// <summary>
        /// Gets the optional list of specific attribute names to search within.
        /// If null, the search includes all attributes.
        /// </summary>
        public IReadOnlyList<string> AttributeNames { get; }

        /// <summary>
        /// Gets a value indicating whether all keywords must match (true) or any keyword (false).
        /// </summary>
        public bool RequireAllKeywords { get; }

        /// <summary>
        /// Validates the filter configuration.
        /// </summary>
        /// <returns>True if the filter is valid, false otherwise.</returns>
        public bool IsValid()
        {
            return this.Keywords != null && this.Keywords.Count > 0;
        }
    }
}
