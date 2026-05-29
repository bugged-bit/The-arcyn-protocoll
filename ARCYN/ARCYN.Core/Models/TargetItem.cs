using System.Text.Json.Serialization;

namespace ARCYN.Core.Models;

public enum TargetKind { App, Website, Folder }

public sealed record TargetItem(
    [property: JsonIgnore] string DisplayLabel,
    [property: JsonIgnore] string LaunchCmd,
    [property: JsonIgnore] string LaunchArg,
    [property: JsonIgnore] TargetKind Kind);
