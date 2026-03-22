using System.ComponentModel.DataAnnotations;

namespace F1.DataSyncWorker.Options;

public sealed class DataSyncOptions
{
    public const string SectionName = "DataSyncWorker";

    [Required]
    public string JolpicaBaseUrl { get; set; } = "https://api.jolpi.ca/ergast/f1/";

    [Range(0, 10080)]
    public int IntervalMinutes { get; set; } = 0;

    [Range(0, 10)]
    public int HttpRetryCount { get; set; } = 3;

    [Range(100, 60000)]
    public int HttpRetryDelayMs { get; set; } = 750;

    [Range(1, 240)]
    public int DeadlineMinutesBeforeStart { get; set; } = 30;

    public bool ContinueOnError { get; set; } = false;

    public bool AutoMigrate { get; set; } = true;

    [Required]
    [MinLength(1)]
    public List<CompetitionSeedDefinition> Competitions { get; set; } = [];

    [Required]
    [MinLength(1)]
    public List<SeasonSeedDefinition> Seasons { get; set; } = [];
}

public sealed class CompetitionSeedDefinition
{
    [Required]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    [Range(1900, 3000)]
    public int Year { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;
}

public sealed class SeasonSeedDefinition
{
    [Range(1900, 3000)]
    public int Season { get; set; }

    [Required]
    [MinLength(1)]
    public List<string> CompetitionKeys { get; set; } = [];
}