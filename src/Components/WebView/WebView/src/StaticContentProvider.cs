using System;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Components.WebView
{
    internal class StaticContentProvider
    {
        private readonly IFileProvider _fileProvider;
        private readonly Uri _appBaseUri;
        private readonly string _hostPageRelativePath;
        private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

        public StaticContentProvider(IFileProvider fileProvider, Uri appBaseUri, string hostPageRelativePath)
        {
            _fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
            _appBaseUri = appBaseUri ?? throw new ArgumentNullException(nameof(appBaseUri));
            _hostPageRelativePath = hostPageRelativePath ?? throw new ArgumentNullException(nameof(hostPageRelativePath));
        }

        public bool TryGetResponseContent(string requestUri, bool allowFallbackOnHostPage, out int statusCode, out string statusMessage, out Stream content, out string headers)
        {
            var fileUri = new Uri(requestUri);
            if (_appBaseUri.IsBaseOf(fileUri))
            {
                var relativePath = _appBaseUri.MakeRelativeUri(fileUri).ToString();

                // Content in the file provider takes first priority
                // Next we may fall back on supplying the host page to support deep linking
                // If there's no match, fall back on serving embedded framework content
                string contentType;
                var found = TryGetFromFileProvider(relativePath, out content, out contentType)
                    || (allowFallbackOnHostPage && TryGetFromFileProvider(_hostPageRelativePath, out content, out contentType))
                    || TryGetFrameworkFile(relativePath, out content, out contentType);

                if (found)
                {
                    statusCode = 200;
                    statusMessage = "OK";
                    headers = GetResponseHeaders(contentType);
                }
                else
                {
                    content = new MemoryStream(Encoding.UTF8.GetBytes($"There is no content at {relativePath}"));
                    statusCode = 404;
                    statusMessage = "Not found";
                    headers = GetResponseHeaders("text/plain");
                }

                // Always respond to requests within the base URI, even if there's no matching file
                return true;
            }
            else
            {
                // URL isn't within application base path, so let the network handle it
                statusCode = default;
                statusMessage = default;
                headers = default;
                content = default;
                return false;
            }
        }

        private bool TryGetFromFileProvider(string relativePath, out Stream content, out string contentType)
        {
            if (!string.IsNullOrEmpty(relativePath))
            {
                var fileInfo = _fileProvider.GetFileInfo(relativePath);
                if (fileInfo.Exists)
                {
                    content = fileInfo.CreateReadStream();
                    contentType = GetResponseContentTypeOrDefault(fileInfo.PhysicalPath);
                    return true;
                }
            }

            content = default;
            contentType = default;
            return false;
        }

        private static bool TryGetFrameworkFile(string relativePath, out Stream content, out string contentType)
        {
            // We're not trying to simulate everything a real webserver does. We don't need to
            // support querystring parameters, for example. It's enough to require an exact match.
            switch (relativePath)
            {
                case "_framework/blazor.webview.js":
                    var assembly = typeof(StaticContentProvider).Assembly;
                    content = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Resources.blazor.webview.js");
                    contentType = GetResponseContentTypeOrDefault(relativePath);
                    return true;
            }

            content = default;
            contentType = default;
            return false;
        }

        private static string GetResponseContentTypeOrDefault(string path)
            => ContentTypeProvider.TryGetContentType(path, out var matchedContentType)
            ? matchedContentType
            : "application/octet-stream";

        private static string GetResponseHeaders(string contentType)
            => $"Content-Type: {contentType}{Environment.NewLine}Cache-Control: no-cache, max-age=0, must-revalidate, no-store";
    }
}
