# Built-in Transformers

Transformers operate on build artifacts at a low level (IL manipulation, binary
patching, obfuscation) and run after a release build, before the post-processor
pipeline. Each derives from `ReleaseTransformer`.

## No built-in transformers ship

This package ships **no** built-in transformers. The canonical list lives in
`BuiltInTransformerRegistry.GetAll()`, which returns an empty array:

```csharp
public static IReadOnlyList<ReleaseTransformer> GetAll() => new ReleaseTransformer[]
{
    // No built-in transformers. Add entries here when built-in artifact transformations
    // are introduced in a future release.
};
```

The registry exists as the extension point for future additions. Until then, no
transformer runs unless you add your own.

## Adding a transformer

Derive from `ReleaseTransformer` in any Editor assembly. Discovery is gated by the
`transformers.autoDiscoverTransformers` setting (default off), and individual
transformers can be skipped via `transformers.disabledTransformerIds`. See:

- [api/custom-transformers](../api/custom-transformers.md) - full guide.
- [Settings reference](settings.md) - the Transformers settings page.

## See also

- [Built-in auditors](built-in-auditors.md)
- [Built-in post-processors](built-in-post-processors.md)
