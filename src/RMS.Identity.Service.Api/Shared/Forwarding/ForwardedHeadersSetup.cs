using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using AspNetCoreIPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace RMS.Identity.Service.Api.Shared.Forwarding;

public static class ForwardedHeadersSetup
{
    public static void ApplyTo(ForwardedHeadersSettings settings, ForwardedHeadersOptions options)
    {
        if (!settings.Enabled)
        {
            options.ForwardedHeaders = ForwardedHeaders.None;
            return;
        }

        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = settings.ForwardLimit;

        foreach (var knownProxy in GetConfiguredValues(settings.KnownProxies))
        {
            options.KnownProxies.Add(IPAddress.Parse(knownProxy));
        }

        foreach (var knownNetwork in GetConfiguredValues(settings.KnownNetworks))
        {
            options.KnownNetworks.Add(ParseKnownNetwork(knownNetwork));
        }
    }

    public static bool HasValidKnownProxies(ForwardedHeadersSettings settings) =>
        GetConfiguredValues(settings.KnownProxies).All(value => IPAddress.TryParse(value, out _));

    public static bool HasValidKnownNetworks(ForwardedHeadersSettings settings) =>
        GetConfiguredValues(settings.KnownNetworks).All(value => TryParseKnownNetwork(value, out _));

    private static AspNetCoreIPNetwork ParseKnownNetwork(string value)
    {
        if (!TryParseKnownNetwork(value, out var network))
        {
            throw new FormatException($"Invalid forwarded headers known network '{value}'.");
        }

        return network;
    }

    private static bool TryParseKnownNetwork(string value, out AspNetCoreIPNetwork network)
    {
        network = default!;

        var parts = value.Split('/', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2
            || !IPAddress.TryParse(parts[0], out var prefix)
            || !int.TryParse(parts[1], out var prefixLength))
        {
            return false;
        }

        var maximumPrefixLength = prefix.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128;
        if (prefixLength < 0 || prefixLength > maximumPrefixLength)
        {
            return false;
        }

        network = new AspNetCoreIPNetwork(prefix, prefixLength);
        return true;
    }

    private static IEnumerable<string> GetConfiguredValues(IEnumerable<string>? values) =>
        values?.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim())
        ?? [];
}
