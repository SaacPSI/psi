// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio.PipelinePlugin
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Visualization.Windows;

    /// <summary>
    /// Class handling the load of pipeline assembly.
    /// </summary>
    public class PsiStudioPipelineAssemblyHandler
    {
        private object assemblyInstance;
        private MethodInfo showMethod;
        private MethodInfo closeMethod;
        private MethodInfo getDatasetMethod;
        private MethodInfo runPipelineMethod;
        private MethodInfo stopPipelineMethod;
        private MethodInfo startTimeMethod;
        private MethodInfo layoutMethod;
        private MethodInfo annotationMethod;
        private MethodInfo getReplayableModeMethod;
        private MethodInfo onDatasetLoadedMethod;

        private PsiStudioPipelineAssemblyHandler(in object assemblyInstance, in string name, in MethodInfo showMethod, in MethodInfo closeMethod, in MethodInfo getDatasetMethod, in MethodInfo runPipelineMethod, in MethodInfo stopPipelineMethod, in MethodInfo startTimeMethod, in MethodInfo getReplayableModeMethod, in MethodInfo layoutMethod = null, in MethodInfo annotationMethod = null,  in MethodInfo onDatasetLoadedMethod = null)
        {
            this.assemblyInstance = assemblyInstance;
            this.showMethod = showMethod;
            this.closeMethod = closeMethod;
            this.getDatasetMethod = getDatasetMethod;
            this.runPipelineMethod = runPipelineMethod;
            this.stopPipelineMethod = stopPipelineMethod;
            this.startTimeMethod = startTimeMethod;
            this.layoutMethod = layoutMethod;
            this.annotationMethod = annotationMethod;
            this.getReplayableModeMethod = getReplayableModeMethod;
            this.onDatasetLoadedMethod = onDatasetLoadedMethod;
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

                // Get the class definition from the assembly
                Type classDefinition = null;

                // Try to find a class with Window as base.
                foreach (Type exportedType in assembly.ExportedTypes)
                {
                    if (exportedType.BaseType == typeof(Window))
                    {
                        classDefinition = exportedType;
                        break;
                    }
                }

                // Check if their is a class that have Window for base => it make sure that ShowDialog method exist.
                if (classDefinition == null)
                {
                    throw new Exception($"The assembly require to have a class with Window as base.");
                }

                // Check if only the class have IPsiStudioPipeline for interface => it make sure that methods exist.
                if (classDefinition.GetInterfaces().Any((t) => { return t.Name == "IPsiStudioPipeline"; }) == false)
                {
                    throw new Exception($"The {classDefinition.Name} require to have IPsiStudioPipeline as interface.");
                }

                // Create an object from the assembly. MAKING a lock on PsiStudio closing...
                var instance = assembly.CreateInstance(classDefinition.FullName, false, BindingFlags.ExactBinding, null, null, null, null);
                if (instance == null)
                {
                    throw new Exception($"Failed to instanciate {classDefinition.Name}.");
                }

                // Make a late-bound call to an instance method of the object.
                MethodInfo showMethod = GetMethod(classDefinition, "Show");

                // Make a late-bound call to an instance method of the object.
                MethodInfo closeMethod = GetMethod(classDefinition, "Close");

                // Make a late-bound call to an instance method of the object.
                MethodInfo runMethod = GetMethod(classDefinition, "RunPipeline");

                // Make a late-bound call to an instance method of the object.
                MethodInfo stopMethod = GetMethod(classDefinition, "StopPipeline");

                // Make a late-bound call to an instance method of the object.
                MethodInfo storeMethod = GetMethod(classDefinition, "GetDataset");

                // Make a late-bound call to an instance method of the object.
                MethodInfo timeMethod = GetMethod(classDefinition, "GetStartTime");

                // Make a late-bound call to an instance method of the object.
                MethodInfo replayableModeMethod = GetMethod(classDefinition, "GetReplaybleMode");

                // Make a late-bound call to an instance method of the object.
                MethodInfo layoutMethod = GetMethod(classDefinition, "GetLayout", true);

                // Make a late-bound call to an instance method of the object.
                MethodInfo annotationMethod = GetMethod(classDefinition, "GetAnnotation", true);

                // Make a late-bound call to an instance method of the object.
                MethodInfo onDatasetLoaded = GetMethod(classDefinition, "OnDatasetLoaded", true);

                return new PsiStudioPipelineAssemblyHandler(instance, Path.GetFileNameWithoutExtension(assemblyPath), showMethod, closeMethod, storeMethod, runMethod, stopMethod, timeMethod, replayableModeMethod, layoutMethod, annotationMethod, onDatasetLoaded);
            }
            catch (Exception ex)
            {
                if (!quietLoad)
                {
                    new MessageBoxWindow(Application.Current.MainWindow, "Error on assembly load", ex.Message, "OK", null).ShowDialog();
                }
            }

            return null;
        }

        /// <summary>
        /// Dispose the assembly.
        /// </summary>
        public void Dispose()
        {
            this.StopPipeline();
            this.showMethod = null;
            this.assemblyInstance = null;
            this.showMethod = null;
            this.getDatasetMethod = null;
            this.runPipelineMethod = null;
            this.stopPipelineMethod = null;
            this.startTimeMethod = null;
            this.layoutMethod = null;
            this.annotationMethod = null;
            this.getReplayableModeMethod = null;
            this.onDatasetLoadedMethod = null;
            this.IsRunning = false;
            this.Name = null;
        }

        /// <summary>
        /// Display the main window of the process.
        /// </summary>
        public void ShowWindow()
        {
            this.SecureInvoke(ref this.showMethod);
        }

        /// <summary>
        /// Close the main window of the process.
        /// </summary>
        public void CloseWindow()
        {
            this.SecureInvoke(ref this.closeMethod);
        }

        /// <summary>
        /// Get the dataset.
        /// </summary>
        /// <returns>Return the dataset or null, if the dataset is not created.</returns>
        public Dataset GetDataset()
        {
            var ret = this.SecureInvoke(ref this.getDatasetMethod);
            if (ret != null)
            {
                return (Dataset)ret;
            }

            return null;
        }

        /// <summary>
        /// Get the dataset.
        /// </summary>
        /// <param name="action">the delegate provided by PsiStudio.</param>
        public void OnDatasetLoaded(Action<Dataset> action)
        {
#pragma warning disable SA1010 // Opening square brackets should be spaced correctly
            this.SecureInvoke(ref this.onDatasetLoadedMethod, [action]);
#pragma warning restore SA1010 // Opening square brackets should be spaced correctly
        }

        /// <summary>
        /// Start the pipeline.
        /// </summary>
        /// <returns>Return True if done, False otherwise.</returns>
        /// <param name="timeInterval">Interval for replay mode.</param>
        public bool RunPipeline(TimeInterval timeInterval)
        {
            if (this.IsRunning)
            {
                return true;
            }

            this.IsRunning = true;
#pragma warning disable SA1010 // Opening square brackets should be spaced correctly
            return this.SecureInvokeBool(ref this.runPipelineMethod, [timeInterval]);
#pragma warning restore SA1010 // Opening square brackets should be spaced correctly
        }

        /// <summary>
        /// Stop the pipeline.
        /// </summary>
        /// <returns>Return True if done, False otherwise.</returns>
        public bool StopPipeline()
        {
            if (!this.IsRunning)
            {
                return true;
            }

            this.IsRunning = false;
            return this.SecureInvokeBool(ref this.stopPipelineMethod);
        }

        /// <summary>
        /// Get the start time of the pipeline.
        /// </summary>
        /// <returns>Return the DateTime of the pipeline.</returns>
        public DateTime GetStartTime()
        {
            if (!this.IsRunning)
            {
                return DateTime.MinValue;
            }

            var res = this.SecureInvoke(ref this.startTimeMethod);
            return res == null ? DateTime.UtcNow : (DateTime)res;
        }

        /// <summary>
        /// Get the plugin will replay a dataset.
        /// </summary>
        /// <returns>Return replayable mode of the plugin.</returns>
        public PipelineReplaybleMode GetReplayableMode()
        {
            var ret = this.SecureInvoke(ref this.getReplayableModeMethod);
            return (PipelineReplaybleMode)ret;
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

        private object SecureInvoke(ref MethodInfo method, object[] args = null)
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
                return method.Invoke(this.assemblyInstance, args);
            }
            catch (Exception ex)
            {
                new MessageBoxWindow(
                    Application.Current.MainWindow,
                    $"Pipeline Error: {method.Name}",
                    $"{ex.Message} : {ex.InnerException}",
                    cancelButtonText: null).ShowDialog();
            }

            return null;
        }

        private bool SecureInvokeBool(ref MethodInfo method, object[] args = null)
        {
            if (method == null)
            {
                return false;
            }

            if (this.assemblyInstance == null)
            {
                return false;
            }

            try
            {
                method.Invoke(this.assemblyInstance, args);
            }
            catch (Exception ex)
            {
                new MessageBoxWindow(
                    Application.Current.MainWindow,
                    $"Pipeline Error: {method.Name}",
                    $"{ex.Message} : {ex.InnerException}",
                    cancelButtonText: null).ShowDialog();
                return false;
            }

            return true;
        }
    }
}
