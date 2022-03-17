# RegFile

A .NET library that can be used to read `.reg` files. The resulting list of "commands"
can then be used to apply them to a live registry or dump them back into a `.reg` file.

## Example Usage

Reading a `.reg` file:

```csharp
IEnumerable<RegistrySubKeyCommand> commands;
using (var reader = new RegFileReader(fileName))
{
    commands = reader.Read();
}
```

Applying commands to the Windows Registry:

```csharp
var processor = new RegCommandProcessor(RegistryView.Default);
processor.Process(commands);
```

Writing commands back to a `.reg` file:

```csharp
using (var writer = new RegFileWriter("somefile.reg"))
{
    writer.Write(commands);
}
```

## Builds

[![Windows](https://github.com/cklutz/Cklutz.RegFile/workflows/Windows/badge.svg)](https://github.com/cklutz/Cklutz.RegFile/actions?query=workflow%3AWindows)
