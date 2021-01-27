// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    /// <summary>
    /// A <see cref="TagHelper"/> that saves the state of Razor components rendered on the page up to that point.
    /// </summary>
    [HtmlTargetElement(TagHelperName, TagStructure = TagStructure.WithoutEndTag)]
    public class PersistComponentStateTagHelper : TagHelper
    {
        private const string TagHelperName = "persist-component-state";
        private const string PersistenceModeName = "persist-mode";
        private PersistenceMode? _persistenceMode;

        /// <summary>
        /// Gets or sets the <see cref="Rendering.ViewContext"/> for the current request.
        /// </summary>
        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="PersistenceMode"/> for the state to persist.
        /// </summary>
        [HtmlAttributeName(PersistenceModeName)]
        public PersistenceMode PersistenceMode
        {
            get => _persistenceMode ?? throw new InvalidOperationException("Invalid persistence mode.");
            set => _persistenceMode = value;
        }

        /// <inheritdoc />
        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            var services = ViewContext.HttpContext.RequestServices;
            var manager = services.GetRequiredService<ComponentApplicationLifetime>();
            var renderer = services.GetRequiredService<HtmlRenderer>();
            var store = PersistenceMode switch
            {
                PersistenceMode.Server => new ProtectedPrerenderComponentApplicationStore(
                    services.GetRequiredService<IDataProtectionProvider>()),
                PersistenceMode.WebAssembly => new PrerenderComponentApplicationStore(),
                _ => throw new InvalidOperationException("Invalid persistence mode.")
            };

            await manager.PersistStateAsync(store, renderer);

            output.TagName = null;
            output.Content.SetHtmlContent(new PersistedStateContent(store.PersistedState));
        }
    }
}
