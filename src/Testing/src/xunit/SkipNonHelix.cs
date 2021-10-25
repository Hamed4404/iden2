// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Testing
{
    /// <summary>
    /// Skip test if running on CI
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class SkipNonHelixAttribute : Attribute, ITestCondition
    {
        public SkipNonHelixAttribute(string issueUrl = "")
        {
            IssueUrl = issueUrl;
        }

        public string IssueUrl { get; }

        public bool IsMet
        {
            get
            {
                return OnHelix();
            }
        }

        public string SkipReason
        {
            get
            {
                return "This test is skipped if not on Helix";
            }
        }

        public static bool OnHelix() => !string.IsNullOrEmpty(GetTargetHelixQueue());
        public static string GetTargetHelixQueue() => Environment.GetEnvironmentVariable("helix");
    }
}
