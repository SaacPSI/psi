﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Summarizers
{
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Represents a range summarizer that performs interval-based data summarization over nullable bool values.
    /// </summary>
    [Summarizer]
    public class NullableBoolRangeSummarizer : Summarizer<bool?, bool?>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NullableBoolRangeSummarizer"/> class.
        /// </summary>
        public NullableBoolRangeSummarizer()
            : base(NumericRangeSummarizer.Summarizer)
        {
        }
    }
}
