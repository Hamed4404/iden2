// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using BasicWebSite.Models;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class HttpResultsTests : IClassFixture<MvcTestFixture<BasicWebSite.StartupWithSystemTextJson>>
{
    public HttpResultsTests(MvcTestFixture<BasicWebSite.StartupWithSystemTextJson> fixture)
    {
        Client = fixture.CreateDefaultClient();
    }

    public HttpClient Client { get; }

    [Fact]
    public async Task ActionCanReturnHttpResultsActionResult()
    {
        // Arrange
        var id = 1;
        var url = $"/contact/{nameof(BasicWebSite.ContactApiController.ActionReturningHttpResultsActionResult)}/{id}";
        var response = await Client.GetAsync(url);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        var result = JsonConvert.DeserializeObject<Contact>(await response.Content.ReadAsStringAsync());
        Assert.NotNull(result);
        Assert.Equal(id, result.ContactId);
    }

    [Fact]
    public async Task ActionCanReturnIResultWithContent()
    {
        // Arrange
        var id = 1;
        var url = $"/contact/{nameof(BasicWebSite.ContactApiController.ActionReturningObjectIResult)}/{id}";
        var response = await Client.GetAsync(url);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        var result = JsonConvert.DeserializeObject<Contact>(await response.Content.ReadAsStringAsync());
        Assert.NotNull(result);
        Assert.Equal(id, result.ContactId);
    }

    [Fact]
    public async Task ActionCanReturnIResultWithStatusCodeOnly()
    {
        // Arrange
        var url = $"/contact/{nameof(BasicWebSite.ContactApiController.ActionReturningStatusCodeIResult)}";
        var response = await Client.GetAsync(url);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.NoContent);

    }

}
