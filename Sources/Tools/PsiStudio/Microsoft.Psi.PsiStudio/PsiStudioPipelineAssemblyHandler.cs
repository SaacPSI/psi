// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using Microsoft.Psi.Visualization.Windows;

    /// <summary>
    /// Class handling the load of pipeline assembly.
    /// </summary>
    public class PsiStudioPipelineAssemblyHandler
    {
        private object assemblyInstance;

        private MethodInfo showMethod;
        private MethodInfo getDatasetMethod;
        private MethodInfo runPipelineMethod;
        private MethodInfo stopPipelineMethod;
        private MethodInfo layoutMethod;
        private MethodInfo annotationMethod;

        private PsiStudioPipelineAssemblyHandler(in object assemblyInstance, in string name, in MethodInfo showMethod, in MethodInfo getDatasetMethod, in MethodInfo runPipelineMethod, in MethodInfo stopPipelineMethod, in MethodInfo layoutMethod = null, in MethodInfo annotationMethod = null)
        {
            this.assemblyInstance = assemblyInstance;
            this.showMethod = showMethod;
            this.getDatasetMethod = getDatasetMethod;
            this.runPipelineMethod = runPipelineMethod;
            this.stopPipelineMethod = stopPipelineMethod;
            this.layoutMethod = layoutMethod;
            this.annotationMethod = annotationMethod;
            this.IsRunning = false;
            this.Name = name;
        }

        /// <summary>
        /// Gets a the name of the pipeline assembly.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the pipeline is running or not.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Static method for instance creation, verify that the assembly is in the expected form.
        /// </summary>
        /// <returns>An instance of PsiStudioPipelineAssemblyHandler is correct, null otherwise.</returns>
        /// <param name="assemblyPath">Fullname of the assembly to load.</param>
        /// <param name="quietLoad">Boolean to active or not the message box if the creation fails.</param>
        public static PsiStudioPipelineAssemblyHandler Load(string assemblyPath, bool quietLoad = false)
        {
            try
            {
                // Load the assembly
                Assembly assembly = Assembly.LoadFrom(assemblyPath.Trim());

                // Check if only one class is public.
                if (Enumerable.Count(assembly.ExportedTypes) != 2)
                {
                    throw new Exception("The assembly require to have only one public class.");
                }

                // Get the class definition from the assembly
                Type classDefinition = assembly.ExportedTypes.First();

                // Check if only the class have Window for base => it make sure that ShowDialog method exist.
                if (classDefinition.BaseType != typeof(Window))
                {
                    throw new Exception($"The {classDefinition.Name} require to have Window as base.");
                }

                // Check if only the class have IPsiStudioPipeline for interface => it make sure that methods exist.
                if (classDefinition.GetInterfaces().Any((t) => { return t.Name == "IPsiStudioPipeline"; }) == false)
                {
                    throw new Exception($"The {classDefinition.Name} require to have IPsiStudioPipeline as interface.");
                }

                // Create an object from the assembly.
                var instance = assembly.CreateInstance(classDefinition.FullName, false, BindingFlags.ExactBinding, null, null, null, null);
                if (instance == null)
                {
                    throw new Exception($"Failed to instanciate {classDefinition.Name}.");
                }

                // Make a late-bound call to an instance method of the object.
                MethodInfo windowMethod = GetMethod(classDefinition, "Show");

                // Make a late-bound call to an instance method of the object.
                MethodInfo runMethod = GetMethod(classDefinition, "RunPipeline");

                // Make a late-bound call to an instance method of the object.
                MethodInfo stopMethod = GetMethod(classDefinition, "StopPipeline");

                // Make a late-bound call to an instance method of the object.
                MethodInfo storeMethod = GetMethod(classDefinition, "GetDataset");

                // Make a late-bound call to an instance method of the object.
                MethodInfo layoutMethod = GetMethod(classDefinition, "GetLayout", true);

                // Make a late-bound call to an instance method of the object.
                MethodInfo annotationMethod = GetMethod(classDefinition, "GetAnnotation", true);

                return new PsiStudioPipelineAssemblyHandler(instance, Path.GetFileNameWithoutExtension(assemblyPath), windowMethod, storeMethod, runMethod, stopMethod, layoutMethod, annotationMethod);
            }
            catch (Exception ex)
            {
                if (quietLoad)
                {
                    new MessageBoxWindow(Application.Current.MainWindow, "Error on assembly load", ex.Message, "OK", null).ShowDialog();
                }
            }

            return null;
        }

        /// <summary>
        /// Display the main window of the process.
        /// </summary>
        public void ShowWindow()
        {
            this.SecureInvoke(ref this.showMethod);
        }

        /// <summary>
        /// Get the fullname of the dataset.
        /// </summary>
        /// <returns>Return the path of the dataset or null, if the dataset is not created.</returns>
        public string GetDatasetPath()
        {
            var ret = this.SecureInvoke(ref this.getDatasetMethod);
            if (ret != null)
            {
                return (string)ret;
            }

            return null;
        }

        /// <summary>
        /// Start the pipeline.
        /// </summary>
        public void RunPipeline()
        {
            if (this.IsRunning)
            {
                return;
            }

            this.IsRunning = true;
            this.SecureInvoke(ref this.runPipelineMethod);
        }

        /// <summary>
        /// Stop the pipeline.
        /// </summary>
        public void StopPipeline()
        {
            if (!this.IsRunning)
            {
                return;
            }

            this.IsRunning = false;
            this.SecureInvoke(ref this.stopPipelineMethod);
        }

        /// <summary>
        /// Get the layout configuration.
        /// </summary>
        /// <returns>Return the json of the layout or null, if the layout is not created.</returns>
        public string GetLayout()
        {
            if (this.layoutMethod != null)
            {
                var ret = this.SecureInvoke(ref this.layoutMethod);
                if (ret != null)
                {
                    return (string)ret;
                }
            }

            return null;
        }

        /// <summary>
        /// Get the annotation configuration.
        /// </summary>
        /// <returns>Return the json of the layout or null, if the annotation is not created.</returns>
        public string GetAnnotation()
        {
            if (this.annotationMethod != null)
            {
                var ret = this.SecureInvoke(ref this.annotationMethod);
                if (ret != null)
                {
                    return (string)ret;
                }
            }

            return null;
        }

        private static MethodInfo GetMethod(Type classDefinition, string methodName, bool canBeNull = false)
        {
            MethodInfo method = classDefinition.GetMethod(methodName);
            if (method == null && canBeNull == false)
            {
                throw new Exception($"Failed to instanciate {methodName} from {classDefinition.Name}.");
            }

            return method;
        }

        private object SecureInvoke(ref MethodInfo method)
        {
            if (method == null)
            {
                return null;
            }

            if (this.assemblyInstance == null)
            {
                return null;
            }

            try
            {
                return method.Invoke(this.assemblyInstance, null);
            }
            catch (Exception ex)
            {
                new MessageBoxWindow(
                    Application.Current.MainWindow,
                    $"Pipeline Error: {method.Name}",
                    ex.Message,
                    cancelButtonText: null).ShowDialog();
            }

            return null;
        }
    }
}
