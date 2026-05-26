namespace ARCYN.UI;

public enum AppPhase
{
    Boot = 0,
    Ready,
    Selecting,
    Launching,
    Closing
}

public sealed class AppState
{
    public AppPhase Phase { get; private set; } = AppPhase.Boot;
    public int SelectedModeIndex { get; set; } = -1;

    public event Action<AppPhase, AppPhase>? PhaseChanged;

    private static readonly Dictionary<AppPhase, AppPhase[]> Transitions = new()
    {
        [AppPhase.Boot] = [AppPhase.Ready, AppPhase.Closing],
        [AppPhase.Ready] = [AppPhase.Selecting, AppPhase.Closing],
        [AppPhase.Selecting] = [AppPhase.Launching, AppPhase.Ready, AppPhase.Closing],
        [AppPhase.Launching] = [AppPhase.Ready, AppPhase.Closing],
        [AppPhase.Closing] = []
    };

    public bool CanTransitionTo(AppPhase next)
    {
        return Transitions.TryGetValue(Phase, out var allowed) && Array.IndexOf(allowed, next) >= 0;
    }

    public bool TransitionTo(AppPhase next)
    {
        if (Phase == next) return false;
        if (!CanTransitionTo(next)) return false;
        var prev = Phase;
        Phase = next;
        PhaseChanged?.Invoke(prev, next);
        return true;
    }

    public string PhaseLabel => Phase switch
    {
        AppPhase.Boot => "BOOT",
        AppPhase.Ready => "RDY",
        AppPhase.Selecting => "SEL",
        AppPhase.Launching => "LCH",
        AppPhase.Closing => "CLS",
        _ => "???"
    };

    public bool IsActive => Phase != AppPhase.Closing;
}
