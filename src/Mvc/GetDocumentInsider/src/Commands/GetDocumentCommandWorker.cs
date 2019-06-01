﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.ApiDescription.Tool.Commands
{
    internal class GetDocumentCommandWorker
    {
        private const string DefaultDocumentName = "v1";
        private const string DocumentService = "Microsoft.Extensions.ApiDescriptions.IDocumentProvider";
        private const string DotString = ".";
        private const string InvalidFilenameString = "..";
        private const string JsonExtension = ".json";
        private const string UnderscoreString = "_";
        private static readonly char[] InvalidFilenameCharacters = Path.GetInvalidFileNameChars();

        private const string GetDocumentsMethodName = "GetDocumentNames";
        private static readonly object[] GetDocumentsArguments = Array.Empty<object>();
        private static readonly Type[] GetDocumentsParameterTypes = Type.EmptyTypes;
        private static readonly Type GetDocumentsReturnType = typeof(IEnumerable<string>);

        private const string GenerateMethodName = "GenerateAsync";
        private static readonly Type[] GenerateMethodParameterTypes = new[] { typeof(string), typeof(TextWriter) };
        private static readonly Type GenerateMethodReturnType = typeof(Task);

        public static int Process(GetDocumentCommandContext context)
        {
            var assemblyName = new AssemblyName(context.AssemblyName);
            var assembly = Assembly.Load(assemblyName);
            var entryPointType = assembly.EntryPoint?.DeclaringType;
            if (entryPointType == null)
            {
                Reporter.WriteError(Resources.FormatMissingEntryPoint(context.AssemblyPath));
                return 3;
            }

            try
            {
                var serviceFactory = HostFactoryResolver.ResolveServiceProviderFactory(assembly);
                if (serviceFactory == null)
                {
                    Reporter.WriteError(Resources.FormatMethodsNotFound(
                        HostFactoryResolver.BuildWebHost,
                        HostFactoryResolver.CreateHostBuilder,
                        HostFactoryResolver.CreateWebHostBuilder,
                        entryPointType));

                    return 4;
                }

                var services = serviceFactory(Array.Empty<string>());
                if (services == null)
                {
                    Reporter.WriteError(Resources.FormatServiceProviderNotFound(
                        typeof(IServiceProvider),
                        HostFactoryResolver.BuildWebHost,
                        HostFactoryResolver.CreateHostBuilder,
                        HostFactoryResolver.CreateWebHostBuilder,
                        entryPointType));

                    return 5;
                }

                var success = GetDocuments(context, services);
                if (!success)
                {
                    return 6;
                }
            }
            catch (Exception ex)
            {
                Reporter.WriteError(ex.ToString());
                return 7;
            }

            return 0;
        }

        private static bool GetDocuments(GetDocumentCommandContext context, IServiceProvider services)
        {
            Type serviceType = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                serviceType = assembly.GetType(DocumentService, throwOnError: false);
                if (serviceType != null)
                {
                    break;
                }
            }

            if (serviceType == null)
            {
                Reporter.WriteError(Resources.FormatServiceTypeNotFound(DocumentService));
                return false;
            }

            var getDocumentsMethod = GetMethod(
                GetDocumentsMethodName,
                serviceType,
                GetDocumentsParameterTypes,
                GetDocumentsReturnType);
            if (getDocumentsMethod == null)
            {
                return false;
            }

            var generateMethod = GetMethod(
                GenerateMethodName,
                serviceType,
                GenerateMethodParameterTypes,
                GenerateMethodReturnType);
            if (generateMethod == null)
            {
                return false;
            }

            var service = services.GetService(serviceType);
            if (service == null)
            {
                Reporter.WriteError(Resources.FormatServiceNotFound(DocumentService));
                return false;
            }

            var documentNames = (IEnumerable<string>)InvokeMethod(getDocumentsMethod, service, GetDocumentsArguments);
            if (documentNames == null)
            {
                return false;
            }

            // Write out the documents.
            Directory.CreateDirectory(context.OutputDirectory);
            var filePathList = new List<string>();
            foreach (var documentName in documentNames)
            {
                var filePath = GetDocument(
                    documentName,
                    context.ProjectName,
                    context.OutputDirectory,
                    generateMethod,
                    service);
                if (filePath == null)
                {
                    return false;
                }

                filePathList.Add(filePath);
            }

            // Write out the cache file.
            var stream = File.Create(context.FileListPath);
            using var writer = new StreamWriter(stream);
            writer.WriteLine(string.Join(Environment.NewLine, filePathList));

            return true;
        }

        private static string GetDocument(
            string documentName,
            string projectName,
            string outputDirectory,
            MethodInfo generateMethod,
            object service)
        {
            Reporter.WriteInformation(Resources.FormatGeneratingDocument(documentName));

            var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);
            var resultTask = (Task)InvokeMethod(generateMethod, service, new object[] { documentName, writer });
            if (resultTask == null)
            {
                return null;
            }

            using (resultTask)
            {
                var finished = resultTask.Wait(TimeSpan.FromMinutes(1));
                if (!finished)
                {
                    Reporter.WriteError(Resources.FormatMethodTimedOut(GenerateMethodName, DocumentService, 1));
                    return null;
                }
            }

            if (stream.Length == 0L)
            {
                Reporter.WriteError(
                    Resources.FormatMethodWroteNoContent(GenerateMethodName, DocumentService, documentName));

                return null;
            }

            var filePath = GetDocumentPath(documentName, projectName, outputDirectory);
            Reporter.WriteInformation(Resources.FormatWritingDocument(documentName, filePath));
            try
            {
                stream.Position = 0L;

                // Create the output FileStream last to avoid corrupting an existing file or writing partial data.
                using var outStream = File.Create(filePath);
                stream.CopyTo(outStream);
            }
            catch
            {
                File.Delete(filePath);
                throw;
            }

            return filePath;
        }

        private static string GetDocumentPath(string documentName, string projectName, string outputDirectory)
        {
            string path;
            if (string.Equals(DefaultDocumentName, documentName, StringComparison.Ordinal))
            {
                // Leave default document name out of the filename.
                path = projectName + JsonExtension;
            }
            else
            {
                // Sanitize the document name because it may contain almost any character, including illegal filename
                // characters such as '/' and '?' and the string "..". Do not treat slashes as folder separators.
                var sanitizedDocumentName = string.Join(
                    UnderscoreString,
                    documentName.Split(InvalidFilenameCharacters));

                while (sanitizedDocumentName.Contains(InvalidFilenameString))
                {
                    sanitizedDocumentName = sanitizedDocumentName.Replace(InvalidFilenameString, DotString);
                }

                path = $"{projectName}_{documentName}{JsonExtension}";
            }

            if (!string.IsNullOrEmpty(outputDirectory))
            {
                path = Path.Combine(outputDirectory, path);
            }

            return path;
        }

        private static MethodInfo GetMethod(string methodName, Type type, Type[] parameterTypes, Type returnType)
        {
            var method = type.GetMethod(methodName, parameterTypes);
            if (method == null)
            {
                Reporter.WriteError(Resources.FormatMethodNotFound(methodName, type));
                return null;
            }

            if (method.IsStatic)
            {
                Reporter.WriteError(Resources.FormatMethodIsStatic(methodName, type));
                return null;
            }

            if (!returnType.IsAssignableFrom(method.ReturnType))
            {
                Reporter.WriteError(
                    Resources.FormatMethodReturnTypeUnsupported(methodName, type, method.ReturnType, returnType));

                return null;
            }

            return method;
        }

        private static object InvokeMethod(MethodInfo method, object instance, object[] arguments)
        {
            var result = method.Invoke(instance, arguments);
            if (result == null)
            {
                Reporter.WriteError(
                    Resources.FormatMethodReturnedNull(method.Name, method.DeclaringType, method.ReturnType));
            }

            return result;
        }
    }
}
