﻿// Copyright (c) .NET Foundation. All rights reserved
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    public class SystemTextJsonHelperTest : JsonHelperTestBase
    {
        protected override IJsonHelper GetJsonHelper()
        {
            var options = new JsonOptions() { JsonSerializerOptions = { PropertyNamingPolicy = JsonNamingPolicy.CamelCase } };
            return new SystemTextJsonHelper(Options.Create(options));
        }

        [Fact]
        public override void Serialize_EscapesHtmlByDefault()
        {
            // Arrange
            var helper = GetJsonHelper();
            var obj = new
            {
                HTML = "<b>John Doe</b>"
            };
            var expectedOutput = "{\"html\":\"\\u003CB\\u003EJohn Doe\\u003C\\/b\\u003E\"}";

            // Act
            var result = helper.Serialize(obj);

            // Assert
            var htmlString = Assert.IsType<HtmlString>(result);
            Assert.Equal(expectedOutput, htmlString.ToString());
        }

        [Fact]
        public override void Serialize_WithNonAsciiChars()
        {
            // Arrange
            var helper = GetJsonHelper();
            var obj = new
            {
                HTML = $"Hello pingüino"
            };
            var expectedOutput = "{\"html\":\"Hello ping\\u00FCino\"}";

            // Act
            var result = helper.Serialize(obj);

            // Assert
            var htmlString = Assert.IsType<HtmlString>(result);
            Assert.Equal(expectedOutput, htmlString.ToString());
        }
    }
}
