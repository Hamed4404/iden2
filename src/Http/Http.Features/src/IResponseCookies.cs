// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET6_0
using System;
using System.Collections.Generic;
#endif

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// A wrapper for the response Set-Cookie header.
    /// </summary>
    public interface IResponseCookies
    {
        /// <summary>
        /// Add a new cookie and value.
        /// </summary>
        /// <param name="key">Name of the new cookie.</param>
        /// <param name="value">Value of the new cookie.</param>
        void Append(string key, string value);

        /// <summary>
        /// Add a new cookie.
        /// </summary>
        /// <param name="key">Name of the new cookie.</param>
        /// <param name="value">Value of the new cookie.</param>
        /// <param name="options"><see cref="CookieOptions"/> included in the new cookie setting.</param>
        void Append(string key, string value, CookieOptions options);

#if NET6_0
        /// <summary>
        /// Add elements of specified dictionary as cookies.
        /// </summary>
        /// <param name="keyValuePairs">Key value pair collections whose elements will be added as cookies.</param>
        /// <param name="options"><see cref="CookieOptions"/> included in new cookie settings.</param>
        void Append(ReadOnlySpan<KeyValuePair<string, string>> keyValuePairs, CookieOptions options)
        {
            foreach (var keyValuePair in keyValuePairs)
            {
                Append(keyValuePair.Key, keyValuePair.Value, options);
            }
        }
#endif

        /// <summary>
        /// Sets an expired cookie.
        /// </summary>
        /// <param name="key">Name of the cookie to expire.</param>
        void Delete(string key);

        /// <summary>
        /// Sets an expired cookie.
        /// </summary>
        /// <param name="key">Name of the cookie to expire.</param>
        /// <param name="options">
        /// <see cref="CookieOptions"/> used to discriminate the particular cookie to expire. The
        /// <see cref="CookieOptions.Domain"/> and <see cref="CookieOptions.Path"/> values are especially important.
        /// </param>
        void Delete(string key, CookieOptions options);
    }
}
