using System.ComponentModel.DataAnnotations;

namespace Collecta.API.Features.Me;

/// <param name="Copies">How many copies the user has; 0 removes the item from the collection.</param>
public sealed record SetCopiesRequest([Range(0, 999)] int Copies);

public sealed record AchievementResponse(string Id, string Name, string Mark, bool Unlocked, double Progress);

/// <param name="XpInLevel">XP earned inside the current level.</param>
/// <param name="XpLevelSize">XP the current level spans.</param>
public sealed record ProgressionResponse(
    int Level,
    int XpInLevel,
    int XpLevelSize,
    IReadOnlyList<AchievementResponse> Achievements);
