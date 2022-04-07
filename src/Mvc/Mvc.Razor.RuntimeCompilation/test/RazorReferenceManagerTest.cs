// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;

public class RazorReferenceManagerTest
{
    private static readonly string ApplicationPartReferencePath = "some-path";

    [Fact]
    public void GetCompilationReferences_CombinesApplicationPartAndOptionMetadataReferences()
    {
        // Arrange
        var options = new MvcRazorRuntimeCompilationOptions();
        var additionalReferencePath = "additional-path";
        options.AdditionalReferencePaths.Add(additionalReferencePath);

        var applicationPartManager = GetApplicationPartManager();
        var referenceManager = new RazorReferenceManager(
            applicationPartManager,
            Options.Create(options));

        var expected = new[] { ApplicationPartReferencePath, additionalReferencePath };

        // Act
        var references = referenceManager.GetReferencePaths();

        // Assert
        Assert.Equal(expected, references);
    }

    [Fact]
    public void CompilationReferences_ShouldCache_WhenCacheIsEnabled()
    {
        (RazorReferenceManager referenceManager, ApplicationPartManager applicationPartManager) = SetupCacheTest();

        applicationPartManager.ApplicationParts.Add(new AssemblyPart(typeof(RazorReferenceManager).Assembly));

        Assert.True(referenceManager.CompilationReferences.Count == 1);

        applicationPartManager.ApplicationParts.Add(new AssemblyPart(typeof(object).Assembly));

        Assert.True(referenceManager.CompilationReferences.Count == 1);
    }

    [Fact]
    public void CompilationReferences_ShouldNotCache_WhenCacheIsDisabled()
    {
        (RazorReferenceManager referenceManager, ApplicationPartManager applicationPartManager) = SetupCacheTest(false);

        applicationPartManager.ApplicationParts.Add(new AssemblyPart(typeof(RazorReferenceManager).Assembly));

        Assert.True(referenceManager.CompilationReferences.Count == 1);

        applicationPartManager.ApplicationParts.Add(new AssemblyPart(typeof(object).Assembly));

        Assert.True(referenceManager.CompilationReferences.Count == 2);
    }

    private static ApplicationPartManager GetApplicationPartManager()
    {
        var applicationPartManager = new ApplicationPartManager();
        var part = new Mock<ApplicationPart>();

        part.As<ICompilationReferencesProvider>()
            .Setup(p => p.GetReferencePaths())
            .Returns(new[] { ApplicationPartReferencePath });

        applicationPartManager.ApplicationParts.Add(part.Object);

        return applicationPartManager;
    }

    private static (RazorReferenceManager, ApplicationPartManager) SetupCacheTest(bool cacheEnabled = true)
    {
        var options = new MvcRazorRuntimeCompilationOptions { CacheAssemblyReferences = cacheEnabled };
        var applicationPartManager = new ApplicationPartManager();
        var referenceManager = new RazorReferenceManager(
            applicationPartManager,
            Options.Create(options));

        return (referenceManager, applicationPartManager);
    }
}
