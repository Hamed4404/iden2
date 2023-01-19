// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
import { Blazor } from '../GlobalExports';
import { BINDING } from './Mono/MonoPlatform';
export class WebAssemblyConfigLoader {
    static async initAsync(bootConfigResult) {
        Blazor._internal.getApplicationEnvironment = () => BINDING.js_string_to_mono_string(bootConfigResult.applicationEnvironment);
        const configFiles = await Promise.all((bootConfigResult.bootConfig.config || [])
            .filter(name => name === 'appsettings.json' || name === `appsettings.${bootConfigResult.applicationEnvironment}.json`)
            .map(async (name) => ({ name, content: await getConfigBytes(name) })));
        Blazor._internal.getConfig = (dotNetFileName) => {
            const fileName = BINDING.conv_string(dotNetFileName);
            const resolvedFile = configFiles.find(f => f.name === fileName);
            return resolvedFile ? BINDING.js_typed_array_to_array(resolvedFile.content) : undefined;
        };
        async function getConfigBytes(file) {
            const response = await fetch(file, {
                method: 'GET',
                credentials: 'include',
                cache: 'no-cache',
            });
            return new Uint8Array(await response.arrayBuffer());
        }
    }
}
//# sourceMappingURL=WebAssemblyConfigLoader.js.map