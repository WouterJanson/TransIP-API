# TransIP.NET
> A client library for the TransIP API

[![GitHub issues](https://img.shields.io/github/issues/WouterJanson/TransIP.NET.svg)](https://github.com/WouterJanson/TransIP.NET/issues)
[![license](https://img.shields.io/github/license/WouterJanson/TransIP.NET.svg)](https://github.com/WouterJanson/TransIP.NET/blob/master/LICENSE)
[![NuGet](https://img.shields.io/nuget/v/TransIP.NET.svg)](https://www.nuget.org/packages/TransIP.NET/)

A .Net Core client library for the TransIP Api, based on the original work of jwvdiermen.

## Installation

NuGet:

```
PM> Install-Package TransIP.NET
```

## Usage example

Here are some code example on how to use the TransIP.NET package, currently only the `DomainService` is supported.

```csharp
using TransIp.Api;
using TransIp.Api.Dto;

//
// Retrieve current DNS entries and add a record
//
var domainService = new DomainService("YourUsername", ClientMode.ReadWrite, "YourPrivateKey");

var info = domainService.GetInfo("example.com");
var entries = info.DnsEntries.ToList();
entries.Add(new DnsEntry
{
	Name = "local",
	Type = DnsEntryType.A,
	Expire = 3600, // 1 hour
	Content = "127.0.0.1"
});
domainService.SetDnsEntries("example.com", entries.ToArray());
```

## Release History

* 1.1.0
    * Added `async` methods
    * Added `ToString()` method for `DnsEntry`
* 1.0.0
    * Initial release

## TODO

* Add other services
* Re-add unit tests
* Write more example code

## Contributing

1. Fork it (<https://github.com/WouterJanson/TransIP.NET/fork>)
2. Create your feature branch (`git checkout -b feature/fooBar`)
3. Commit your changes (`git commit -am 'Add some fooBar'`)
4. Push to the branch (`git push origin feature/fooBar`)
5. Create a new Pull Request

## Credits

Thanks to [jwvdiermen](https://github.com/jwvdiermen/TransIP-API) for the initial version 4 years ago.
