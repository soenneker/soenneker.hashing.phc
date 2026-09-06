[![](https://img.shields.io/nuget/v/soenneker.hashing.phc.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.hashing.phc/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.hashing.phc/build-and-test.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.hashing.phc/actions/workflows/build-and-test.yml)
[![](https://img.shields.io/nuget/dt/soenneker.hashing.phc.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.hashing.phc/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.hashing.phc/codeql.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.hashing.phc/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Hashing.Phc
### A dependency-free .NET library for formatting and parsing Password Hashing Competition (PHC) strings.

## Installation

```
dotnet add package Soenneker.Hashing.Phc
```

## Usage

```csharp
using Soenneker.Hashing.Phc;

PhcString value = PhcFormatter.Parse(
    "$argon2id$v=19$m=65536,t=3,p=4$c29tZXNhbHQ$YW5kYWZha2VoYXNo");

value.TryGetParameter("m", out string? memoryCost);
string encoded = value.ToString();
```

Build a PHC string while preserving explicit parameter order:

```csharp
var value = new PhcString(
    identifier: "argon2id",
    version: 19,
    parameters:
    [
        new("m", "65536"),
        new("t", "3"),
        new("p", "4")
    ],
    salt: "c29tZXNhbHQ",
    hash: "YW5kYWZha2VoYXNo");

string encoded = PhcFormatter.Format(value);
```

`TryParse` and `TryFormat` are available for failure-based and span-oriented call sites.
