﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Identity.DefaultUI.WebSite;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.IdentityUserTests
{
    public class IdentityUserAuthorizationTests : AuthorizationTests<Startup, IdentityDbContext>
    {
        public IdentityUserAuthorizationTests(ServerFactory<Startup, IdentityDbContext> serverFactory) : base(serverFactory)
        {
        }
    }
}
