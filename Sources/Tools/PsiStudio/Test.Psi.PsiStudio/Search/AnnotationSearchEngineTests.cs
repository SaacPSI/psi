// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.PsiStudio.Search
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using Microsoft.Psi;
    using Microsoft.Psi.Data.Annotations;
    using Microsoft.Psi.PsiStudio.Search;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Unit tests for the annotation search engine.
    /// </summary>
    [TestClass]
    public class AnnotationSearchEngineTests
    {
        /// <summary>
        /// Helper class to represent a simple annotation value for testing.
        /// </summary>
        private class TestAnnotationValue : IAnnotationValue
        {
            private readonly string value;

            public TestAnnotationValue(string value)
            {
                this.value = value;
            }

            public string ValueAsString => this.value;

            public Color FillColor => Color.White;

            public Color TextColor => Color.Black;
        }

        /// <summary>
        /// Tests finding next annotation with a keyword match.
        /// </summary>
        [TestMethod]
        [Timeout(5000)]
        public void FindNext_WithMatchingKeyword()
        {
            var engine = new AnnotationSearchEngine();
            var annotations = this.CreateTestAnnotations();
            engine.LoadAnnotations(annotations);

            var filter = new AnnotationSearchFilter("test");
            var cursorTime = new DateTime(2024, 1, 1, 10, 0, 0);

            var result = engine.FindNext(cursorTime, filter, false);

            Assert.IsNotNull(result);
            Assert.AreEqual("test_track_1", result.Track);
            Assert.IsTrue(result.MatchScore > 0.0);
        }

        /// <summary>
        /// Tests finding previous annotation with a keyword match.
        /// </summary>
        [TestMethod]
        [Timeout(5000)]
        public void FindPrevious_WithMatchingKeyword()
        {
            var engine = new AnnotationSearchEngine();
            var annotations = this.CreateTestAnnotations();
            engine.LoadAnnotations(annotations);

            var filter = new AnnotationSearchFilter("data");
            var cursorTime = new DateTime(2024, 1, 1, 10, 30, 0);

            var result = engine.FindPrevious(cursorTime, filter, false);

            Assert.IsNotNull(result);
            Assert.AreEqual("test_track_0", result.Track);
            Assert.IsTrue(result.MatchScore > 0.0);
        }

        /// <summary>
        /// Tests that no result is returned when no matching annotation exists forward.
        /// </summary>
        [TestMethod]
        [Timeout(5000)]
        public void FindNext_NoMatch_ReturnsNull()
        {
            var engine = new AnnotationSearchEngine();
            var annotations = this.CreateTestAnnotations();
            engine.LoadAnnotations(annotations);

            var filter = new AnnotationSearchFilter("nonexistent");
            var cursorTime = new DateTime(2024, 1, 1, 10, 0, 0);

            var result = engine.FindNext(cursorTime, filter, false);

            Assert.IsNull(result);
        }

        /// <summary>
        /// Tests that no result is returned when no matching annotation exists backward.
        /// </summary>
        [TestMethod]
        [Timeout(5000)]
        public void FindPrevious_NoMatch_ReturnsNull()
        {
            var engine = new AnnotationSearchEngine();
            var annotations = this.CreateTestAnnotations();
            engine.LoadAnnotations(annotations);

            var filter = new AnnotationSearchFilter("nonexistent");
            var cursorTime = new DateTime(2024, 1, 1, 10, 0, 0);

            var result = engine.FindPrevious(cursorTime, filter, false);

            Assert.IsNull(result);
        }

        /// <summary>
        /// Tests finding all annotations matching a filter.
        /// </summary>
        [TestMethod]
        [Timeout(5000)]
        public void FindAll_ReturnsAllMatches()
        {
            var engine = new AnnotationSearchEngine();
            var annotations = this.CreateTestAnnotations();
            engine.LoadAnnotations(annotations);

            var filter = new AnnotationSearchFilter("test");
            var results = engine.FindAll(filter);

            Assert.IsNotNull(results);
            Assert.AreEqual(2, results.Count);
        }

        /// <summary>
        /// Tests case-insensitive keyword matching.
        /// </summary>
        [TestMethod]
        [Timeout(5000)]
        public void FindNext_CaseInsensitive()
        {
            var engine = new AnnotationSearchEngine();
            var annotations = this.CreateTestAnnotations();
            engine.LoadAnnotations(annotations);

            var filter = new AnnotationSearchFilter("TEST");
            var cursorTime = new DateTime(2024, 1, 1, 10, 0, 0);

            var result = engine.FindNext(cursorTime, filter, false);

            Assert.IsNotNull(result);
            Assert.AreEqual("test_track_0", result.Track);
        }

        /// <summary>
        /// Tests multiple keyword search with requireAllKeywords = true.
        /// </summary>
        [TestMethod]
        [Timeout(5000)]
        public void FindAll_MultipleKeywords_RequireAll()
        {
            var engine = new AnnotationSearchEngine();
            var annotations = this.CreateTestAnnotations();
            engine.LoadAnnotations(annotations);

            var keywords = new[] { "test", "data" };
            var filter = new AnnotationSearchFilter(keywords, requireAllKeywords: true);
            var results = engine.FindAll(filter);

            Assert.IsNotNull(results);

            // Only the first annotation contains both "test" and "data"
            Assert.AreEqual(1, results.Count);
        }

        /// <summary>
        /// Tests multiple keyword search with requireAllKeywords = false.
        /// </summary>
        [TestMethod]
        [Timeout(5000)]
        public void FindAll_MultipleKeywords_RequireAny()
        {
            var engine = new AnnotationSearchEngine();
            var annotations = this.CreateTestAnnotations();
            engine.LoadAnnotations(annotations);

            var keywords = new[] { "test", "sample" };
            var filter = new AnnotationSearchFilter(keywords, requireAllKeywords: false);
            var results = engine.FindAll(filter);

            Assert.IsNotNull(results);

            // Both annotations contain either "test" or "sample"
            Assert.AreEqual(2, results.Count);
        }

        /// <summary>
        /// Tests that empty cache returns no results.
        /// </summary>
        [TestMethod]
        [Timeout(5000)]
        public void FindNext_EmptyCache_ReturnsNull()
        {
            var engine = new AnnotationSearchEngine();
            var filter = new AnnotationSearchFilter("test");
            var cursorTime = new DateTime(2024, 1, 1, 10, 0, 0);

            var result = engine.FindNext(cursorTime, filter, false);

            Assert.IsNull(result);
        }

        /// <summary>
        /// Tests cache invalidation.
        /// </summary>
        [TestMethod]
        [Timeout(5000)]
        public void InvalidateCache_IncrementsCacheVersion()
        {
            var engine = new AnnotationSearchEngine();
            var annotations = this.CreateTestAnnotations();
            engine.LoadAnnotations(annotations);

            int initialVersion = engine.CacheVersion;
            engine.InvalidateCache();
            int newVersion = engine.CacheVersion;

            Assert.AreNotEqual(initialVersion, newVersion);
        }

        /// <summary>
        /// Tests temporal distance calculation for forward search.
        /// </summary>
        [TestMethod]
        [Timeout(5000)]
        public void FindNext_CorrectTemporalDistance()
        {
            var engine = new AnnotationSearchEngine();
            var annotations = this.CreateTestAnnotations();
            engine.LoadAnnotations(annotations);

            var filter = new AnnotationSearchFilter("test");
            var cursorTime = new DateTime(2024, 1, 1, 10, 0, 0);

            var result = engine.FindNext(cursorTime, filter, false);

            Assert.IsNotNull(result);

            // First annotation starts at 10:00:00, cursor at 10:00:00, distance should be 0
            Assert.AreEqual(TimeSpan.Zero, result.DistanceFromCursor);
        }

        /// <summary>
        /// Tests filtering by specific attribute names.
        /// </summary>
        [TestMethod]
        [Timeout(5000)]
        public void FindNext_FilterByAttributeName()
        {
            var engine = new AnnotationSearchEngine();
            var annotations = this.CreateTestAnnotations();
            engine.LoadAnnotations(annotations);

            var attributeNames = new[] { "attr_0" };
            var filter = new AnnotationSearchFilter("test", attributeNames);
            var cursorTime = new DateTime(2024, 1, 1, 10, 0, 0);

            var result = engine.FindNext(cursorTime, filter, false);

            Assert.IsNotNull(result);
        }

        /// <summary>
        /// Tests that invalid filter raises exception.
        /// </summary>
        [TestMethod]
        [Timeout(5000)]
        [ExpectedException(typeof(ArgumentException))]
        public void FindNext_InvalidFilter_ThrowsException()
        {
            var engine = new AnnotationSearchEngine();
            var annotations = this.CreateTestAnnotations();
            engine.LoadAnnotations(annotations);

            var filter = new AnnotationSearchFilter(new string[] { });
            engine.FindNext(DateTime.UtcNow, filter, false);
        }

        /// <summary>
        /// Tests that null filter raises exception.
        /// </summary>
        [TestMethod]
        [Timeout(5000)]
        [ExpectedException(typeof(ArgumentNullException))]
        public void FindNext_NullFilter_ThrowsException()
        {
            var engine = new AnnotationSearchEngine();
            engine.FindNext(DateTime.UtcNow, null, false);
        }

        /// <summary>
        /// Creates test annotations for use in tests.
        /// </summary>
        /// <returns>A list of test annotations.</returns>
        private List<TimeIntervalAnnotation> CreateTestAnnotations()
        {
            var annotations = new List<TimeIntervalAnnotation>();

            // First annotation: 10:00:00 - 10:01:00
            var attr1 = new Dictionary<string, IAnnotationValue>
            {
                { "attr_0", new TestAnnotationValue("test data") },
                { "attr_1", new TestAnnotationValue("value 1") },
            };
            annotations.Add(new TimeIntervalAnnotation(
                new TimeInterval(new DateTime(2024, 1, 1, 10, 0, 0), new DateTime(2024, 1, 1, 10, 1, 0)),
                "test_track_0",
                attr1));

            // Second annotation: 10:05:00 - 10:06:00
            var attr2 = new Dictionary<string, IAnnotationValue>
            {
                { "attr_0", new TestAnnotationValue("sample test") },
                { "attr_1", new TestAnnotationValue("value 2") },
            };
            annotations.Add(new TimeIntervalAnnotation(
                new TimeInterval(new DateTime(2024, 1, 1, 10, 5, 0), new DateTime(2024, 1, 1, 10, 6, 0)),
                "test_track_1",
                attr2));

            // Third annotation: 10:10:00 - 10:11:00
            var attr3 = new Dictionary<string, IAnnotationValue>
            {
                { "attr_0", new TestAnnotationValue("other data") },
                { "attr_1", new TestAnnotationValue("value 3") },
            };
            annotations.Add(new TimeIntervalAnnotation(
                new TimeInterval(new DateTime(2024, 1, 1, 10, 10, 0), new DateTime(2024, 1, 1, 10, 11, 0)),
                "test_track_2",
                attr3));

            return annotations;
        }
    }
}
