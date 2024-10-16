﻿namespace UVOCBot.Config;

public record LoggingOptions
{
    public const string CONFIG_NAME = "LoggingOptions";

    /// <summary>
    /// An optional endpoint at which to ingest logs to a Seq instance.
    /// </summary>
    public string? SeqIngestionEndpoint { get; init; }

    /// <summary>
    /// See <see cref="SeqIngestionEndpoint"/>.
    /// </summary>
    public string? SeqApiKey { get; init; }
}
