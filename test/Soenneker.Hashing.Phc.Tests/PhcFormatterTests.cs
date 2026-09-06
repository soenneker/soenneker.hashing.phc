using System;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Soenneker.Hashing.Phc.Tests;

public sealed class PhcFormatterTests
{
    [Test]
    public async Task IsValid_identifies_valid_and_invalid_values()
    {
        await Assert.That(PhcFormatter.IsValid("$argon2id$v=19$m=65536,t=3,p=4$c29tZXNhbHQ$YW5kYWZha2VoYXNo")).IsTrue();
        await Assert.That(PhcFormatter.IsValid("argon2id$v=19")).IsFalse();
        await Assert.That(PhcFormatter.IsValid(null)).IsFalse();
    }

    [Test]
    public async Task Parse_and_format_round_trip_argon2id()
    {
        const string encoded = "$argon2id$v=19$m=65536,t=3,p=4$c29tZXNhbHQ$YW5kYWZha2VoYXNo";

        PhcString parsed = PhcFormatter.Parse(encoded);

        await Assert.That(parsed.Identifier).IsEqualTo("argon2id");
        await Assert.That(parsed.Version).IsEqualTo(19);
        await Assert.That(parsed.Parameters.Select(static p => p.Name)).IsEquivalentTo(["m", "t", "p"]);
        await Assert.That(parsed.TryGetParameter("m", out string? memory)).IsTrue();
        await Assert.That(memory).IsEqualTo("65536");
        await Assert.That(parsed.ToString()).IsEqualTo(encoded);
    }

    [Test]
    public async Task Format_supports_records_without_version_or_parameters()
    {
        var value = new PhcString("example", salt: "c2FsdA", hash: "aGFzaA");

        await Assert.That(PhcFormatter.Format(value)).IsEqualTo("$example$c2FsdA$aGFzaA");
    }

    [Test]
    [Arguments("argon2id")]
    [Arguments("$ARGON2ID$v=19$m=1$salt$hash")]
    [Arguments("$argon2id$v=x$m=1$salt$hash")]
    [Arguments("$argon2id$m=1,m=2$salt$hash")]
    [Arguments("$argon2id$m=1$salt$hash$extra")]
    public async Task TryParse_rejects_invalid_values(string encoded)
    {
        await Assert.That(PhcFormatter.TryParse(encoded, out _)).IsFalse();
    }

    [Test]
    public async Task TryFormat_reports_a_small_destination()
    {
        var value = new PhcString("example");
        var destination = new char[2];

        bool success = PhcFormatter.TryFormat(value, destination, out int written);

        await Assert.That(success).IsFalse();
        await Assert.That(written).IsEqualTo(0);
    }
}
