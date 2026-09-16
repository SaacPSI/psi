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
    public class TaskAnnotationPropertyDescriptor : AnnotationPropertyDescriptor<TaskAnnotationDisplayData>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TaskAnnotationPropertyDescriptor"/> class.
        /// </summary>
        /// <param name="annotationDisplayData">The display data for the annotation that contains the property for this property descriptor.</param>
        /// <param name="propertyName">The name of the property in the annotation represented by this property descriptor.</param>
        /// <param name="isReadOnly">Specifies whether the property may be edited.</param>
        /// <param name="propertyType">The type of the property in this property descriptor.</param>
        /// <param name="propertyAttributes">A collection of attributes that apply to this property descriptor.</param>
        public TaskAnnotationPropertyDescriptor(TaskAnnotationDisplayData annotationDisplayData, string propertyName, bool isReadOnly, Type propertyType, Attribute[] propertyAttributes)
            : base(annotationDisplayData, propertyName, isReadOnly, propertyType, propertyAttributes)
        {
        }

        /// <inheritdoc/>
        public override object GetValue(object component)
        {
            return this.AnnotationDisplayData.GetAttributeValue(this.Name);
        }

        /// <inheritdoc/>
        public override void SetValue(object component, object value)
        {
            this.AnnotationDisplayData.SetAttributeValue(this.Name, (IAnnotationValue)value);
        }
    }
}
