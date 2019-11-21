// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers
{
    internal partial class CharArrayBufferSource : Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.ICharBufferSource
    {
        public static readonly Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.CharArrayBufferSource Instance;
        public CharArrayBufferSource() { }
        public char[] Rent(int bufferSize) { throw null; }
        public void Return(char[] buffer) { }
    }
    internal partial interface ICharBufferSource
    {
        char[] Rent(int bufferSize);
        void Return(char[] buffer);
    }
    internal partial class PagedCharBuffer
    {
        public const int PageSize = 1024;
        public PagedCharBuffer(Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.ICharBufferSource bufferSource) { }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.ICharBufferSource BufferSource { get { throw null; } }
        public int Length { get { throw null; } }
        public System.Collections.Generic.List<char[]> Pages { get { throw null; } }
        public void Append(char value) { }
        public void Append(char[] buffer, int index, int count) { }
        public void Append(string value) { }
        public void Clear() { }
        public void Dispose() { }
    }
    internal partial class ViewBuffer : Microsoft.AspNetCore.Html.IHtmlContentBuilder
    {
        public static readonly int PartialViewPageSize;
        public static readonly int TagHelperPageSize;
        public static readonly int ViewComponentPageSize;
        public static readonly int ViewPageSize;
        public ViewBuffer(Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.IViewBufferScope bufferScope, string name, int pageSize) { }
        public int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.ViewBufferPage this[int index] { get { throw null; } }
        public Microsoft.AspNetCore.Html.IHtmlContentBuilder Append(string unencoded) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContentBuilder AppendHtml(Microsoft.AspNetCore.Html.IHtmlContent content) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContentBuilder AppendHtml(string encoded) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContentBuilder Clear() { throw null; }
        public void CopyTo(Microsoft.AspNetCore.Html.IHtmlContentBuilder destination) { }
        public void MoveTo(Microsoft.AspNetCore.Html.IHtmlContentBuilder destination) { }
        public void WriteTo(System.IO.TextWriter writer, System.Text.Encodings.Web.HtmlEncoder encoder) { }
        public System.Threading.Tasks.Task WriteToAsync(System.IO.TextWriter writer, System.Text.Encodings.Web.HtmlEncoder encoder) { throw null; }
    }
    internal partial class ViewBufferTextWriter : System.IO.TextWriter
    {
        public ViewBufferTextWriter(Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.ViewBuffer buffer, System.Text.Encoding encoding) { }
        public ViewBufferTextWriter(Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.ViewBuffer buffer, System.Text.Encoding encoding, System.Text.Encodings.Web.HtmlEncoder htmlEncoder, System.IO.TextWriter inner) { }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.ViewBuffer Buffer { get { throw null; } }
        public override System.Text.Encoding Encoding { get { throw null; } }
        public bool Flushed { get { throw null; } }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync() { throw null; }
        public void Write(Microsoft.AspNetCore.Html.IHtmlContent value) { }
        public void Write(Microsoft.AspNetCore.Html.IHtmlContentContainer value) { }
        public override void Write(char value) { }
        public override void Write(char[] buffer, int index, int count) { }
        public override void Write(object value) { }
        public override void Write(string value) { }
        public override System.Threading.Tasks.Task WriteAsync(char value) { throw null; }
        public override System.Threading.Tasks.Task WriteAsync(char[] buffer, int index, int count) { throw null; }
        public override System.Threading.Tasks.Task WriteAsync(string value) { throw null; }
        public override void WriteLine() { }
        public override void WriteLine(object value) { }
        public override void WriteLine(string value) { }
        public override System.Threading.Tasks.Task WriteLineAsync() { throw null; }
        public override System.Threading.Tasks.Task WriteLineAsync(char value) { throw null; }
        public override System.Threading.Tasks.Task WriteLineAsync(char[] value, int start, int offset) { throw null; }
        public override System.Threading.Tasks.Task WriteLineAsync(string value) { throw null; }
    }
    internal partial class ViewBufferPage
    {
        public ViewBufferPage(Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.ViewBufferValue[] buffer) { }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.ViewBufferValue[] Buffer { get { throw null; } }
        public int Capacity { get { throw null; } }
        public int Count { get { throw null; } set { } }
        public bool IsFull { get { throw null; } }
        public void Append(Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.ViewBufferValue value) { }
    }
}
namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Filters
{
    internal partial class ViewDataAttributeApplicationModelProvider
    {
        public ViewDataAttributeApplicationModelProvider() { }
        public int Order { get { throw null; } }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext context) { }
    }
    internal static partial class ViewDataAttributePropertyProvider
    {
        public static System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ViewFeatures.Filters.LifecycleProperty> GetViewDataProperties(System.Type type) { throw null; }
    }
    internal partial interface ISaveTempDataCallback
    {
        void OnTempDataSaving(Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary tempData);
    }
    internal partial interface IViewDataValuesProviderFeature
    {
        void ProvideViewDataValues(Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary viewData);
    }
    internal abstract partial class SaveTempDataPropertyFilterBase : Microsoft.AspNetCore.Mvc.ViewFeatures.Filters.ISaveTempDataCallback
    {
        protected readonly Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory _tempDataFactory;
        public SaveTempDataPropertyFilterBase(Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory tempDataFactory) { }
        public System.Collections.Generic.IDictionary<System.Reflection.PropertyInfo, object> OriginalValues { get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ViewFeatures.Filters.LifecycleProperty> Properties { get { throw null; } set { } }
        public object Subject { get { throw null; } set { } }
        public static System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ViewFeatures.Filters.LifecycleProperty> GetTempDataProperties(Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure.TempDataSerializer tempDataSerializer, System.Type type) { throw null; }
        public void OnTempDataSaving(Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary tempData) { }
        protected void SetPropertyValues(Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary tempData) { }
    }
    internal readonly partial struct LifecycleProperty
    {
        public LifecycleProperty(System.Reflection.PropertyInfo propertyInfo, string key) { throw null; }
        public string Key { get { throw null; } }
        public System.Reflection.PropertyInfo PropertyInfo { get { throw null; } }
        public object GetValue(object instance) { throw null; }
        public void SetValue(object instance, object value) { }
    }
}
namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    internal partial class TempDataApplicationModelProvider
    {
        public TempDataApplicationModelProvider(Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure.TempDataSerializer tempDataSerializer) { }
        public int Order { get { throw null; } }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext context) { }
    }
    internal partial class NullView : Microsoft.AspNetCore.Mvc.ViewEngines.IView
    {
        public static readonly Microsoft.AspNetCore.Mvc.ViewFeatures.NullView Instance;
        public NullView() { }
        public string Path { get { throw null; } }
        public System.Threading.Tasks.Task RenderAsync(Microsoft.AspNetCore.Mvc.Rendering.ViewContext context) { throw null; }
    }
    internal partial class TemplateRenderer
    {
        public const string IEnumerableOfIFormFileName = "IEnumerable`IFormFile";
        public TemplateRenderer(Microsoft.AspNetCore.Mvc.ViewEngines.IViewEngine viewEngine, Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers.IViewBufferScope bufferScope, Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary viewData, string templateName, bool readOnly) { }
        public static System.Collections.Generic.IEnumerable<string> GetTypeNames(Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata modelMetadata, System.Type fieldType) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent Render() { throw null; }
    }
    internal static partial class FormatWeekHelper
    {
        public static string GetFormattedWeek(Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExplorer modelExplorer) { throw null; }
    }
    internal static partial class ViewDataDictionaryFactory
    {
        public static System.Func<Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary, Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary> CreateFactory(System.Reflection.TypeInfo modelType) { throw null; }
        public static System.Func<Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary, Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary> CreateNestedFactory(System.Reflection.TypeInfo modelType) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure
{
    internal partial class DefaultTempDataSerializer : Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure.TempDataSerializer
    {
        public DefaultTempDataSerializer() { }
        public override bool CanSerializeType(System.Type type) { throw null; }
        public override System.Collections.Generic.IDictionary<string, object> Deserialize(byte[] value) { throw null; }
        public override byte[] Serialize(System.Collections.Generic.IDictionary<string, object> values) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.Rendering
{
    internal partial class SystemTextJsonHelper : Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper
    {
        public SystemTextJsonHelper(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.JsonOptions> options) { }
        public Microsoft.AspNetCore.Html.IHtmlContent Serialize(object value) { throw null; }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    internal partial class MvcViewOptionsSetup
    {
        public MvcViewOptionsSetup(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.DataAnnotations.MvcDataAnnotationsLocalizationOptions> dataAnnotationLocalizationOptions, Microsoft.AspNetCore.Mvc.DataAnnotations.IValidationAttributeAdapterProvider validationAttributeAdapterProvider) { }
        public MvcViewOptionsSetup(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.DataAnnotations.MvcDataAnnotationsLocalizationOptions> dataAnnotationOptions, Microsoft.AspNetCore.Mvc.DataAnnotations.IValidationAttributeAdapterProvider validationAttributeAdapterProvider, Microsoft.Extensions.Localization.IStringLocalizerFactory stringLocalizerFactory) { }
        public void Configure(Microsoft.AspNetCore.Mvc.MvcViewOptions options) { }
    }
    internal partial class TempDataMvcOptionsSetup
    {
        public TempDataMvcOptionsSetup() { }
        public void Configure(Microsoft.AspNetCore.Mvc.MvcOptions options) { }
    }
}