using ARCYN.UI.Models;

namespace ARCYN.UI.Services;

public sealed class ModeService
{
    private readonly List<ModeConfig> _modes = [];
    private readonly AppState _state;

    public int Count => _modes.Count;
    public IReadOnlyList<ModeConfig> Modes => _modes;
    public bool HasSelection => _state.SelectedModeIndex >= 0 && _state.SelectedModeIndex < _modes.Count;

    public int SelectedIndex
    {
        get => _state.SelectedModeIndex;
        set => _state.SelectedModeIndex = value;
    }

    public ModeConfig? SelectedMode => HasSelection ? _modes[_state.SelectedModeIndex] : null;

    public string ActiveLabel
    {
        get
        {
            if (!HasSelection) return "STANDBY";
            return _modes[_state.SelectedModeIndex].Name.ToUpperInvariant();
        }
    }

    public int TotalProcesses => _modes.Sum(m => m.ProcessCount);
    public string ShortcutHint => Count == 0 ? "[1]" : Count == 1 ? "[1]" : $"[1-{Count}]";

    public ModeService(AppState state)
    {
        _state = state;
    }

    public void Load(List<ModeConfig> modes)
    {
        _modes.Clear();
        _modes.AddRange(modes);
        Reindex();
    }

    public void Add(ModeConfig mode)
    {
        _modes.Add(mode);
        Reindex();
    }

    public bool RemoveAt(int index)
    {
        if (index < 0 || index >= _modes.Count) return false;
        _modes.RemoveAt(index);
        Reindex();
        return true;
    }

    public bool MoveUp(int index)
    {
        if (index <= 0 || index >= _modes.Count) return false;
        (_modes[index], _modes[index - 1]) = (_modes[index - 1], _modes[index]);
        Reindex();
        return true;
    }

    public void Insert(int index, ModeConfig mode)
    {
        if (index < 0) index = 0;
        if (index > _modes.Count) index = _modes.Count;
        _modes.Insert(index, mode);
        Reindex();
    }

    public bool MoveDown(int index)
    {
        if (index < 0 || index >= _modes.Count - 1) return false;
        (_modes[index], _modes[index + 1]) = (_modes[index + 1], _modes[index]);
        Reindex();
        return true;
    }

    public ModeConfig? Get(int index)
    {
        return index >= 0 && index < _modes.Count ? _modes[index] : null;
    }

    public int IndexOf(ModeConfig mode) => _modes.IndexOf(mode);

    private void Reindex()
    {
        for (int i = 0; i < _modes.Count; i++)
            _modes[i].Index = i + 1;
    }
}
