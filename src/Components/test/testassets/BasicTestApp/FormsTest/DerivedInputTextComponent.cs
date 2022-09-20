// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Forms;

namespace BasicTestApp.FormsTest;

public class DerivedInputTextComponent : InputText
{
    // Supports InputsTwoWayBindingComponent tests
    // Repro for https://github.com/dotnet/aspnetcore/issues/40097

    protected override bool TryParseValueFromString(string value, out string result, out string validationErrorMessage)
    {
        if (value == "INVALID")
        {
            result = default;
            validationErrorMessage = "INVALID is not allowed value.";
            return false;
        }
        else if (value == "24h")
        {
            result = "24:00:00";
        }
        else
        {
            result = value;
        }
        validationErrorMessage = null;
        return true;
    }
}
