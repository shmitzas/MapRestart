using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Plugins;

namespace MapRestart;

[PluginMetadata(
    Id = "MapRestart",
    Version = "1.1.0",
    Name = "MapRestart",
    Author = "Shmitzas",
    Description = "Reloads the current map (via map / host_workshop_map) when the server is empty and the map has been running for over an hour, to mitigate tick drift."
)]
public partial class MapRestart : BasePlugin
{
    public static new ISwiftlyCore Core { get; private set; } = null!;

    private ILogger<MapRestart> logger = null!;
    private Config cfg = null!;

    private string _currentMap = string.Empty;
    private DateTime _mapLoadedAt = DateTime.UtcNow;
    private bool _restartTriggered;
    private CancellationTokenSource? _pendingEvaluation;

    public MapRestart(ISwiftlyCore core) : base(core) { }

    public override void ConfigureSharedInterface(IInterfaceManager interfaceManager) { }

    public override void UseSharedInterface(IInterfaceManager interfaceManager) { }

    public override void Load(bool hotReload)
    {
        Core = base.Core;
        LoadConfig();

        Core.Event.OnMapLoad += OnMapLoad;
        Core.Event.OnClientDisconnected += OnClientDisconnected;

        // On hot reload we don't know when the map originally loaded, so
        // assume "now" — this avoids an immediate accidental restart.
        _mapLoadedAt = DateTime.UtcNow;
        _restartTriggered = false;

        if (cfg.DetailedLogging)
            logger.LogInformation("MapRestart loaded. Threshold: {Minutes} minutes.", cfg.MapRestartThresholdMinutes);
    }

    public override void Unload()
    {
        _pendingEvaluation?.Cancel();
        _pendingEvaluation?.Dispose();
        _pendingEvaluation = null;
        Core.Event.OnMapLoad -= OnMapLoad;
        Core.Event.OnClientDisconnected -= OnClientDisconnected;
    }

    private void LoadConfig()
    {
        const string fileName = "config.jsonc";
        const string section = "MapRestart";

        Core.Configuration
            .InitializeJsonWithModel<Config>(fileName, section)
            .Configure(builder =>
            {
                builder.AddJsonFile(
                    Core.Configuration.GetConfigPath(fileName),
                    optional: false,
                    reloadOnChange: true
                );
            });

        ServiceCollection services = new();
        services
            .AddSwiftly(Core, addLogger: true, addConfiguration: true)
            .AddOptionsWithValidateOnStart<Config>()
            .BindConfiguration(section);

        var provider = services.BuildServiceProvider();

        logger = provider.GetRequiredService<ILogger<MapRestart>>();
        cfg = provider.GetRequiredService<IOptions<Config>>().Value;
    }

    private void OnMapLoad(IOnMapLoadEvent @event)
    {
        _pendingEvaluation?.Cancel();
        _pendingEvaluation?.Dispose();
        _pendingEvaluation = null;

        _currentMap = @event.MapName;
        _mapLoadedAt = DateTime.UtcNow;
        _restartTriggered = false;

        if (cfg.DetailedLogging)
            logger.LogInformation("Map loaded: {Map} at {Time:O}", _currentMap, _mapLoadedAt);
    }

    private void OnClientDisconnected(IOnClientDisconnectedEvent @event)
    {
        if (_restartTriggered) return;

        var elapsed = DateTime.UtcNow - _mapLoadedAt;
        if (elapsed < TimeSpan.FromMinutes(cfg.MapRestartThresholdMinutes))
        {
            if (cfg.DetailedLogging)
                logger.LogInformation(
                    "Client disconnected — map age {Elapsed} below threshold ({Threshold} min); skipping.",
                    elapsed, cfg.MapRestartThresholdMinutes);
            return;
        }

        if (cfg.DetailedLogging)
            logger.LogInformation(
                "Client disconnected — map age {Elapsed}; deferring human count check by 2s.",
                elapsed);

        // The disconnecting player may still register as valid the instant this
        // event fires, so defer the human count check until the slot frees up.
        _pendingEvaluation?.Cancel();
        _pendingEvaluation?.Dispose();
        _pendingEvaluation = Core.Scheduler.DelayBySeconds(2, () =>
        {
            _pendingEvaluation = null;
            EvaluateRestart(DateTime.UtcNow - _mapLoadedAt);
        });
    }

    private void EvaluateRestart(TimeSpan elapsed)
    {
        if (_restartTriggered) return;

        int humanCount;
        try
        {
            humanCount = Core.PlayerManager.GetAllValidPlayers()
                .Count(p => p is { IsValid: true, IsFakeClient: false });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to enumerate players; deferring restart evaluation.");
            return;
        }

        if (cfg.DetailedLogging)
            logger.LogInformation(
                "Evaluating restart — map age {Elapsed}, human player count: {Count}",
                elapsed, humanCount);

        if (humanCount != 0) return;

        if (string.IsNullOrWhiteSpace(_currentMap))
        {
            logger.LogWarning("Restart conditions met but current map name is unknown; skipping.");
            return;
        }

        if (Core.Engine is not { } engine)
        {
            logger.LogWarning("Restart conditions met but Core.Engine is unavailable; skipping.");
            return;
        }

        var workshopId = engine.WorkshopId;
        var isWorkshop = !string.IsNullOrWhiteSpace(workshopId);
        var mapToLoad = isWorkshop ? workshopId : _currentMap;
        var command = isWorkshop ? "host_workshop_map" : "map";
        var fullCommand = $"{command} {mapToLoad}";

        if (cfg.DetailedLogging)
            logger.LogInformation(
                "Triggering map restart via '{Command}' (map: {Map}, workshopId: {WorkshopId}, map age: {Elapsed}, humans: {Count}).",
                fullCommand, _currentMap, workshopId, elapsed, humanCount);

        try
        {
            _restartTriggered = true;
            engine.ExecuteCommand(fullCommand);
        }
        catch (Exception ex)
        {
            _restartTriggered = false;
            logger.LogError(ex, "Failed to execute '{Command}'.", fullCommand);
        }
    }
}
