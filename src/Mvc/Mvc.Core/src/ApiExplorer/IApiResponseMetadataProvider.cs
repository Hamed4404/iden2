// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    /// <summary>
    /// Provides a return type, status code and a set of possible content types returned by a
    /// successful execution of the action.
    /// </summary>
    public interface IApiResponseMetadataProvider : IFilterMetadata, IProducesResponseTypeMetadata
    {
        /// <summary>
        /// Gets the optimistic return type of the action.
        /// </summary>
        new Type? Type { get; }

        /// <summary>
        /// Gets the HTTP status code of the response.
        /// </summary>
        new int StatusCode { get; }

        /// <summary>
        /// Configures a collection of allowed content types which can be produced by the action.
        /// </summary>
        new void SetContentTypes(MediaTypeCollection contentTypes);
    }
}
