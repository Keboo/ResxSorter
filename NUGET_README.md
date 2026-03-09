# ResxSorter

A .NET global tool that sorts `<data>` elements in `.resx` resource files alphabetically by name.

## Installation

```shell
dotnet tool install -g Keboo.ResxSorter
```

## Usage

```shell
ResxSorter -i <input-file> [options]
```

### Options

| Option | Alias | Description |
|---|---|---|
| `--input-file` | `-i` | **(Required)** The input `.resx` file to sort. |
| `--output-file` | `-o` | The output file. If omitted, the input file is overwritten in place. |
| `--force` | `-f` | Always write the output, even if the file is already sorted. |
| `--verbose` | `-v` | Write verbose output. |

### Examples

Sort a `.resx` file in place:

```shell
ResxSorter -i Resources.resx
```

Sort and write to a new file:

```shell
ResxSorter -i Resources.resx -o Resources.Sorted.resx
```

Force overwrite even if already sorted:

```shell
ResxSorter -i Resources.resx -f
```

## Why?

Keeping `.resx` entries sorted alphabetically reduces merge conflicts and makes diffs easier to review when multiple developers are editing resource files.
