﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Identity.DefaultUI.WebSite;
using Identity.DefaultUI.WebSite.Data;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.IdentityUserTests
{
    public class ApplicationUserLoginTests : LoginTests<ApplicationUserStartup,ApplicationDbContext>
    {
        public ApplicationUserLoginTests(ServerFactory<ApplicationUserStartup, ApplicationDbContext> serverFactory) : base(serverFactory)
        {
        }
    }
}
