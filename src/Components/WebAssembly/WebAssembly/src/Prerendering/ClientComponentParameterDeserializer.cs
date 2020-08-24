// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components
{
    internal class ClientComponentParameterDeserializer
    {
        private readonly ComponentParametersTypeCache _parametersCache;

        public ClientComponentParameterDeserializer(
            ComponentParametersTypeCache parametersCache)
        {
            _parametersCache = parametersCache;
        }

        public static ClientComponentParameterDeserializer Instance { get; } = new ClientComponentParameterDeserializer(new ComponentParametersTypeCache());

        public ParameterView DeserializeParameters(IList<ComponentParameter> parametersDefinitions, IList<object> parameterValues, out ParameterView parameters)
        {
            parameters = default;
            var parametersDictionary = new Dictionary<string, object>();

            if (parameterValues.Count != parametersDefinitions.Count)
            {
                // Mismatched number of definition/parameter values.
                throw new InvalidOperationException($"The number of parameter definitions '{parametersDefinitions.Count}' does not match the number parameter values '{parameterValues.Count}'.");
            }

            for (var i = 0; i < parametersDefinitions.Count; i++)
            {
                var definition = parametersDefinitions[i];
                if (definition.Name == null)
                {
                    throw new InvalidOperationException("The name is missing in a parameter definition.");
                }

                if (definition.TypeName == null && definition.Assembly == null)
                {
                    parametersDictionary.Add(definition.Name, null);
                }
                else if (definition.TypeName == null || definition.Assembly == null)
                {
                    throw new InvalidOperationException($"The parameter definition for '{definition.Name}' is incomplete: Type='{definition.TypeName}' Assembly='{definition.Assembly}'.");
                }
                else
                {
                    var parameterType = _parametersCache.GetParameterType(definition.Assembly, definition.TypeName);
                    if (parameterType == null)
                    {
                        throw new InvalidOperationException($"The parameter '{definition.Name} with type '{definition.TypeName}' in assembly '{definition.Assembly}' could not be found.");
                    }
                    try
                    {
                        var value = (JsonElement)parameterValues[i];
                        var parameterValue = JsonSerializer.Deserialize(
                            value.GetRawText(),
                            parameterType,
                            ClientComponentSerializationSettings.JsonSerializationOptions);

                        parametersDictionary.Add(definition.Name, parameterValue);
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException("Could not parse the parameter value for parameter '{definition.Name}' of type '{definition.TypeName}' and assembly '{definition.Assembly}'.", e);
                    }
                }
            }

            parameters = ParameterView.FromDictionary(parametersDictionary);
            return parameters;
        }

        public ComponentParameter[] GetParameterDefinitions(string parametersDefinitions)
        {
            return JsonSerializer.Deserialize<ComponentParameter[]>(parametersDefinitions, ClientComponentSerializationSettings.JsonSerializationOptions);
        }

        public IList<object> GetParameterValues(string parameterValues)
        {
            return JsonSerializer.Deserialize<IList<object>>(parameterValues, ClientComponentSerializationSettings.JsonSerializationOptions);
        }
    }
}
