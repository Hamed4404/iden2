// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class FileStreamResultTest
    {
        [Theory]
        [InlineData(0, 4, "Hello", 5)]
        [InlineData(6, 10, "World", 5)]
        [InlineData(null, 5, "World", 5)]
        [InlineData(6, null, "World", 5)]
        public async Task WriteFileAsync_PreconditionStateShouldProcess_WritesRangeRequested(long? start, long? end, string expectedString, long contentLength)
        {
            var action = new Func<FileStreamResult, HttpContext, Task>(async (result, context) => await ((IResult)result).ExecuteAsync(context));

            await BaseFileStreamResultTest.WriteFileAsync_PreconditionStateShouldProcess_WritesRangeRequested(
                start,
                end,
                expectedString,
                contentLength,
                action);
        }

        [Fact]
        public async Task WriteFileAsync_IfRangeHeaderValid_WritesRequestedRange()
        {
            var action = new Func<FileStreamResult, HttpContext, Task>(async (result, context) => await ((IResult)result).ExecuteAsync(context));

            await BaseFileStreamResultTest.WriteFileAsync_IfRangeHeaderValid_WritesRequestedRange(action);
        }

        [Fact]
        public async Task WriteFileAsync_RangeProcessingNotEnabled_RangeRequestedIgnored()
        {
            var action = new Func<FileStreamResult, HttpContext, Task>(async (result, context) => await ((IResult)result).ExecuteAsync(context));

            await BaseFileStreamResultTest.WriteFileAsync_RangeProcessingNotEnabled_RangeRequestedIgnored(action);
        }

        [Fact]
        public async Task WriteFileAsync_IfRangeHeaderInvalid_RangeRequestedIgnored()
        {
            var action = new Func<FileStreamResult, HttpContext, Task>(async (result, context) => await ((IResult)result).ExecuteAsync(context));

            await BaseFileStreamResultTest.WriteFileAsync_IfRangeHeaderInvalid_RangeRequestedIgnored(action);
        }

        [Theory]
        [InlineData("0-5")]
        [InlineData("bytes = ")]
        [InlineData("bytes = 1-4, 5-11")]
        public async Task WriteFileAsync_PreconditionStateUnspecified_RangeRequestIgnored(string rangeString)
        {
            var action = new Func<FileStreamResult, HttpContext, Task>(async (result, context) => await ((IResult)result).ExecuteAsync(context));

            await BaseFileStreamResultTest.WriteFileAsync_PreconditionStateUnspecified_RangeRequestIgnored(rangeString, action);
        }

        [Theory]
        [InlineData("bytes = 12-13")]
        [InlineData("bytes = -0")]
        public async Task WriteFileAsync_PreconditionStateUnspecified_RangeRequestedNotSatisfiable(string rangeString)
        {
            var action = new Func<FileStreamResult, HttpContext, Task>(async (result, context) => await ((IResult)result).ExecuteAsync(context));

            await BaseFileStreamResultTest.WriteFileAsync_PreconditionStateUnspecified_RangeRequestedNotSatisfiable(rangeString, action);
        }

        [Fact]
        public async Task WriteFileAsync_RangeRequested_PreconditionFailed()
        {
            var action = new Func<FileStreamResult, HttpContext, Task>(async (result, context) => await ((IResult)result).ExecuteAsync(context));

            await BaseFileStreamResultTest.WriteFileAsync_RangeRequested_PreconditionFailed(action);
        }

        [Fact]
        public async Task WriteFileAsync_NotModified_RangeRequestedIgnored()
        {
            var action = new Func<FileStreamResult, HttpContext, Task>(async (result, context) => await ((IResult)result).ExecuteAsync(context));

            await BaseFileStreamResultTest.WriteFileAsync_NotModified_RangeRequestedIgnored(action);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(null)]
        public async Task WriteFileAsync_RangeRequested_FileLengthZeroOrNull(long? fileLength)
        {
            var action = new Func<FileStreamResult, HttpContext, Task>(async (result, context) => await ((IResult)result).ExecuteAsync(context));

            await BaseFileStreamResultTest.WriteFileAsync_RangeRequested_FileLengthZeroOrNull(fileLength, action);
        }

        [Fact]
        public async Task WriteFileAsync_WritesResponse_InChunksOfFourKilobytes()
        {
            var action = new Func<FileStreamResult, HttpContext, Task>(async (result, context) => await ((IResult)result).ExecuteAsync(context));

            await BaseFileStreamResultTest.WriteFileAsync_WritesResponse_InChunksOfFourKilobytes(action);
        }

        [Fact]
        public async Task WriteFileAsync_CopiesProvidedStream_ToOutputStream()
        {
            var action = new Func<FileStreamResult, HttpContext, Task>(async (result, context) => await ((IResult)result).ExecuteAsync(context));

            await BaseFileStreamResultTest.WriteFileAsync_CopiesProvidedStream_ToOutputStream(action);
        }

        [Fact]
        public async Task SetsSuppliedContentTypeAndEncoding()
        {
            var action = new Func<FileStreamResult, HttpContext, Task>(async (result, context) => await ((IResult)result).ExecuteAsync(context));

            await BaseFileStreamResultTest.SetsSuppliedContentTypeAndEncoding(action);
        }

        [Fact]
        public async Task HeadRequest_DoesNotWriteToBody_AndClosesReadStream()
        {
            var action = new Func<FileStreamResult, HttpContext, Task>(async (result, context) => await ((IResult)result).ExecuteAsync(context));

            await BaseFileStreamResultTest.HeadRequest_DoesNotWriteToBody_AndClosesReadStream(action);
        }
    }
}
