# RegFile

A .NET library that can be used to read and write `.reg` files, and import into or export from
the Windows Registry.

Putting the functionality together, one can recreate the `REG.EXE IMPORT` and `REG.EXE EXPORT` commands easily.

## Current Limitations

In short all native registry types (and formats) that are represented by the `RegistryValueKind`-type are supported.
As file format, currently only the `Windows Registry Editor Version 5.00` signature is supported (which is the
current format since Windows 2000). The older `REGEDIT4` format is not currently supported.

Also, please refer to `RegFileReaderTests.cs` for currently supported data types and `.reg` file formats.

## Example Usage

### Reading a `.reg` file

```csharp
IEnumerable<RegistrySubKeyCommand> commands;
using (var reader = new RegFileReader(fileName))
{
    commands = reader.Read();
}
```

### Writing a `.reg` file

```csharp
using (var writer = new RegFileWriter("somefile.reg"))
{
    writer.Write(commands);
}
```

### Applying commands to the Windows Registry

```csharp
var processor = new RegCommandProcessor(RegistryView.Default);
processor.Process(commands);
```

### Extracing commands from the Windows Registry

```csharp
var extractor = new RegCommandExtractor(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\.NETFramework", RegistryView.Registry64);
var commands = extractor.Extract();
```

## Builds

[![Windows](https://github.com/cklutz/RegFile/workflows/Windows/badge.svg)](https://github.com/cklutz/RegFile/actions?query=workflow%3AWindows)
