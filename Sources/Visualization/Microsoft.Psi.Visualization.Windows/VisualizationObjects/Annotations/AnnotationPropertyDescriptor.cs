// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.ComponentModel;
    using Microsoft.Psi.Data.Annotations;

    /// <summary>
    /// Represents a property descriptor for an annotation.
    /// </summary>
    /// <typeparam name="T">The datatype of the value.</typeparam>
    public abstract class AnnotationPropertyDescriptor<T> : PropertyDescriptor
    {
        /// <summary>
        /// The display data for the annotation that contains the property for this property descriptor.
        /// </summary>
        private readonly T annotationDisplayData;
        private readonly Type propertyType;
        private readonly bool isReadOnly;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationPropertyDescriptor{T}"/> class.
        /// </summary>
        /// <param name="annotationDisplayData">The display data for the annotation that contains the property for this property descriptor.</param>
        /// <param name="propertyName">The name of the property in the annotation represented by this property descriptor.</param>
        /// <param name="isReadOnly">Specifies whether the property may be edited.</param>
        /// <param name="propertyType">The type of the property in this property descriptor.</param>
        /// <param name="propertyAttributes">A collection of attributes that apply to this property descriptor.</param>
        public AnnotationPropertyDescriptor(T annotationDisplayData, string propertyName, bool isReadOnly, Type propertyType, Attribute[] propertyAttributes)
            : base(propertyName, propertyAttributes)
        {
            this.annotationDisplayData = annotationDisplayData;
            this.isReadOnly = isReadOnly;
            this.propertyType = propertyType;
        }

        /// <inheritdoc/>
        public override Type ComponentType => typeof(T);

        /// <inheritdoc/>
        public override bool IsReadOnly => this.isReadOnly;

        /// <inheritdoc/>
        public override Type PropertyType => this.propertyType;

        /// <summary>
        /// Gets the display data for the annotation that contains the property for this property descriptor.
        /// </summary>
        protected T AnnotationDisplayData => this.annotationDisplayData;

        /// <inheritdoc/>
        public override bool CanResetValue(object component)
        {
            return !this.isReadOnly;
        }

        /// <inheritdoc/>
        public override void ResetValue(object component)
        {
        }

        /// <inheritdoc/>
        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }
    }
}
