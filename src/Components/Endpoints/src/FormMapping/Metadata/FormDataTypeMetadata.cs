// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping.Metadata;

internal class FormDataTypeMetadata
{
    public FormDataTypeMetadata(Type type)
    {
        Type = type;
    }

    public TypeKind Kind { get; set; }

    public Type Type { get; set; }

    public FormDataTypeMetadata? ElementType { get; set; }

    public FormDataTypeMetadata? KeyType { get; set; }

    public FormDataTypeMetadata? ValueType { get; set; }

    public ConstructorInfo Constructor { get; set; }

    public IList<FormDataPropertyMetadata> Properties { get; set; } = new List<FormDataPropertyMetadata>();
    public bool IsRecursive { get; internal set; }
}

internal class FormDataMetadataFactory(List<IFormDataConverterFactory> factories)
{
    private readonly FormMetadataContext _context = new();
    private readonly ParsableConverterFactory _parsableFactory = factories.OfType<ParsableConverterFactory>().Single();
    private readonly DictionaryConverterFactory _dictionaryFactory = factories.OfType<DictionaryConverterFactory>().Single();
    private readonly CollectionConverterFactory _collectionFactory = factories.OfType<CollectionConverterFactory>().Single();

    public FormDataTypeMetadata GetOrCreateMetadataFor(Type type, FormDataMapperOptions options)
    {
        // We are walking the graph in order to detect recursive types.
        // We evaluate whether a type is:
        // 1. Primitive
        // 2. Dictionary
        // 3. Collection
        // 4. Complex
        // Only complex types can be recursive.
        // We only compute metadata when we are dealing with objects, other classes of
        // types are handled directly by the appropriate converters.
        // We keep track of the metadata for the types because it is useful when we generate
        // the specific object converter for a type.
        // The code generation varies depending on whether there is recursion or not within
        // the type graph.
        var shouldClearContext = !_context.ResolutionInProgress;
        if (shouldClearContext)
        {
            _context.BeginResolveGraph();
        }

        // Try to get the metadata for the type or create and add a new instance.
        var result = _context.TypeMetadata.TryGetValue(type, out var value) ? value : new FormDataTypeMetadata(type);
        if (value == null)
        {
            _context.TypeMetadata[type] = result;
        }

        // Check for cycles and mark any type as recursive if needed.
        DetectCyclesAndMarkMetadataTypesAsRecursive(type, result);

        // We found the value on the existing metadata, we can return it.
        if (value != null)
        {
            return result;
        }

        // These blocks are evaluated in a specific order.
        if (_parsableFactory.CanConvert(type, options) ||
            (Nullable.GetUnderlyingType(type) is { } underlyingType &&
                _parsableFactory.CanConvert(underlyingType, options)))
        {
            result.Kind = TypeKind.Primitive;
            return result;
        }

        if (_dictionaryFactory.CanConvert(type, options))
        {
            result.Kind = TypeKind.Dictionary;
            var (keyType, valueType) = DictionaryConverterFactory.ResolveDictionaryTypes(type)!;
            result.KeyType = GetOrCreateMetadataFor(keyType, options);
            result.ValueType = GetOrCreateMetadataFor(valueType, options);
            return result;
        }

        if (_collectionFactory.CanConvert(type, options))
        {
            result.Kind = TypeKind.Collection;
            result.ElementType = GetOrCreateMetadataFor(CollectionConverterFactory.ResolveElementType(type)!, options);
            return result;
        }

        result.Kind = TypeKind.Object;
        _context.Track(type);
        var constructors = type.GetConstructors();

        if (constructors.Length == 1)
        {
            result.Constructor = constructors[0];
        }

        var candidateProperty = PropertyHelper.GetVisibleProperties(type);
        foreach (var property in candidateProperty)
        {
            var propertyTypeInfo = GetOrCreateMetadataFor(property.PropertyInfo.PropertyType, options);
            var propertyInfo = new FormDataPropertyMetadata(property, propertyTypeInfo);
            result.Properties.Add(propertyInfo);
        }

        if (shouldClearContext)
        {
            _context.EndResolveGraph();
        }

        return result;
    }

    internal bool HasMetadataFor(Type type) => _context.TypeMetadata.ContainsKey(type);

    private void DetectCyclesAndMarkMetadataTypesAsRecursive(Type type, FormDataTypeMetadata result)
    {
        // Mark any type as recursive if its already present in the current resolution graph or
        // if there is a base type for it on the resolution graph.
        // For example, given A and B : A. With A having a property of type B.
        // when we inspect B, we can tell that A is recursive because you can have A -> B -> B -> B -> ...
        // The opposite scenario is not something we need to worry about because we don't support polymorphism,
        // meaning that we will never create an instance of B if the declared type is A.
        // In that scenario, A and B : A. With B having a property of type A.
        // The recursion stops at A, because A doesn't define the property and we will never bind B to A.
        // If in the future we support polymorphism, it's a matter or updating the logic below to account for it.
        for (var i = 0; i < _context.CurrentTypes.Count; i++)
        {
            if (_context.CurrentTypes[i] == type)
            {
                // We found an exact match in the current resolution graph.
                // This means that the type is recursive.
                result.IsRecursive = true;
            }
            else if (type.IsSubclassOf(_context.CurrentTypes[i]))
            {
                // We found a type that is assignable from the current type.
                // This means that the type is recursive.
                // The type must have already been registered in DI.
                var existingType = _context.TypeMetadata[_context.CurrentTypes[i]];
                existingType.IsRecursive = true;
            }
        }
    }

    private class FormMetadataContext
    {
        public Dictionary<Type, FormDataTypeMetadata> TypeMetadata { get; set; } = new();

        public List<Type> CurrentTypes { get; set; } = new();

        public bool ResolutionInProgress { get; internal set; }

        internal void BeginResolveGraph()
        {
            if (ResolutionInProgress)
            {
                throw new InvalidOperationException("Cannot begin a new resolution graph while one is already in progress.");
            }
            ResolutionInProgress = true;
        }

        internal void EndResolveGraph()
        {
            if (!ResolutionInProgress)
            {
                throw new InvalidOperationException("Cannot end a resolution graph while one is not in progress.");
            }
            ResolutionInProgress = false;
            CurrentTypes.Clear();
        }

        internal void Track(Type type)
        {
            CurrentTypes.Add(type);
        }
    }
}

internal enum TypeKind
{
    Primitive,
    Collection,
    Dictionary,
    Object,
}
