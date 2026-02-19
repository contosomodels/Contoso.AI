# Contoso.AI Text Embedder

This folder contains the text embedding projects for the Contoso.AI family.

## Structure

- **Contoso.AI.TextEmbedder/** - Base interface library defining `ITextEmbedder`
- **Contoso.AI.TextEmbedder.MiniLML6/** - Implementation using all-MiniLM-L6-v2 model
- **Contoso.AI.TextEmbedder.MiniLML6.ConsoleTest/** - Test console application

## Building

```bash
dotnet build
```

## Running Tests

```bash
dotnet run --project Contoso.AI.TextEmbedder.MiniLML6.ConsoleTest
```

## Adding More Models

To add a new embedding model:

1. Create a new project folder (e.g., `Contoso.AI.TextEmbedder.MiniLML12`)
2. Implement the `ITextEmbedder` interface
3. Follow the standard API pattern (GetReadyState, EnsureReadyAsync, CreateAsync)
4. Add MSBuild targets for model download
5. Create a console test project
6. Update the solution file

See `Contoso.AI.TextEmbedder.MiniLML6` as a reference implementation.

## License

MIT
