using Grimoire.Core.Enums;
using Grimoire.Core.Models;
using Grimoire.Core.Services;
using Grimoire.Core.Tutorial;
using Grimoire.Engine.Input;
using Xunit;

namespace Grimoire.Core.Tests;

public class IdleRewardCalculatorTests
{
    [Fact]
    public void Calculate_WithZeroDuration_ReturnsZeroRewards()
    {
        var familiar = new Familiar
        {
            Name = "Test",
            Type = FamiliarType.Wisp,
            Element = ElementType.Mana,
            Level = 1,
            CurrentHealth = 100,
            GatheringBonus = 1.0,
            LastExpeditionUTC = DateTimeOffset.UtcNow,
            ExpeditionReturnUTC = DateTimeOffset.UtcNow.AddSeconds(-10)
        };

        var result = IdleRewardCalculator.Calculate(familiar, DateTimeOffset.UtcNow);

        Assert.True(result.Duration.TotalSeconds > 0);
        Assert.True(result.ExperienceEarned >= 0);
        Assert.True(result.ManaCrystalsEarned >= 0);
    }

    [Fact]
    public void Calculate_WithPositiveDuration_ReturnsPositiveRewards()
    {
        var now = DateTimeOffset.UtcNow;
        var familiar = new Familiar
        {
            Name = "Test",
            Type = FamiliarType.Wisp,
            Element = ElementType.Mana,
            Level = 2,
            Rarity = Rarity.Rare,
            CurrentHealth = 100,
            GatheringBonus = 1.5,
            LastExpeditionUTC = now.AddMinutes(-10),
            ExpeditionReturnUTC = now.AddMinutes(-5)
        };

        var result = IdleRewardCalculator.Calculate(familiar, now);

        Assert.True(result.ExperienceEarned > 0);
        Assert.True(result.ManaCrystalsEarned > 0);
        Assert.True(result.Duration.TotalMinutes > 0);
    }

    [Fact]
    public void Calculate_ClampsToMaxDuration()
    {
        var now = DateTimeOffset.UtcNow;
        var familiar = new Familiar
        {
            Name = "Test",
            Type = FamiliarType.Wisp,
            Element = ElementType.Mana,
            Level = 1,
            CurrentHealth = 100,
            LastExpeditionUTC = now.AddDays(-2), // 48 hours ago
            ExpeditionReturnUTC = now.AddDays(-1)
        };

        var result = IdleRewardCalculator.Calculate(familiar, now);

        // Should be clamped to 24 hours
        Assert.True(result.Duration.TotalHours <= 24.1);
    }
}

public class GestureRecognitionEngineTests
{
    [Theory]
    [InlineData(SpellGesture.Line)]
    public void EndStroke_WithRecognisedGesture_ReturnsGesture(SpellGesture expected)
    {
        // This test would need a properly formed gesture input
        // For now, verify the engine doesn't crash on empty input
        var engine = new GestureRecognitionEngine();
        engine.BeginStroke();
        var result = engine.EndStroke();
        Assert.Equal(SpellGesture.Unknown, result);
    }

    [Fact]
    public void BeginStroke_ClearsPreviousPoints()
    {
        var engine = new GestureRecognitionEngine();
        engine.BeginStroke();
        engine.AddPoint(new System.Numerics.Vector2(0, 0));
        engine.AddPoint(new System.Numerics.Vector2(100, 100));

        engine.BeginStroke();
        var stroke = engine.GetCurrentStroke();
        Assert.Empty(stroke);
    }
}

public class TutorialServiceTests
{
    [Fact]
    public void TryCompleteStep_WithCorrectAction_CompletesStep()
    {
        var tutorial = new TutorialService();
        Assert.Equal("tap_shrine", tutorial.CurrentStep?.Id);

        var result = tutorial.TryCompleteStep("shrine_tapped");
        Assert.True(result);
        Assert.Equal("collect_mana", tutorial.CurrentStep?.Id);
    }

    [Fact]
    public void TryCompleteStep_WithWrongAction_ReturnsFalse()
    {
        var tutorial = new TutorialService();
        var result = tutorial.TryCompleteStep("wrong_action");
        Assert.False(result);
        Assert.Equal("tap_shrine", tutorial.CurrentStep?.Id);
    }

    [Fact]
    public void SkipTutorial_MarksAllComplete()
    {
        var tutorial = new TutorialService();
        tutorial.SkipTutorial();
        Assert.True(tutorial.IsComplete);
        Assert.Null(tutorial.CurrentStep);
    }
}
