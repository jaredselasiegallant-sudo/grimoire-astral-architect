using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grimoire.Core.Enums;
using Grimoire.Core.Interfaces;
using Grimoire.Core.Models;

namespace Grimoire.App.ViewModels;

/// <summary>
/// Manages the familiar bar at the bottom of the screen.
/// Handles selecting familiars, viewing stats, and dispatching expeditions.
/// </summary>
public partial class FamiliarManagementViewModel : ObservableObject
{
    private readonly IGameStateService? _stateService;

    [ObservableProperty] private Familiar? _selectedFamiliar;
    [ObservableProperty] private string _expeditionStatus = "";

    public ObservableCollection<Familiar> Familiars { get; } = [];

    public FamiliarManagementViewModel() { }

    public FamiliarManagementViewModel(IGameStateService stateService)
    {
        _stateService = stateService;
    }

    /// <summary>Load familiars from the game state.</summary>
    public void LoadFrom(List<Familiar> familiars)
    {
        Familiars.Clear();
        foreach (var f in familiars)
            Familiars.Add(f);
    }

    [RelayCommand]
    private void SelectFamiliar(Familiar? familiar)
    {
        SelectedFamiliar = familiar;
    }

    /// <summary>
    /// Send the selected familiar on a timed expedition.
    /// The duration is based on the familiar's level.
    /// </summary>
    [RelayCommand]
    private void SendOnExpedition()
    {
        if (SelectedFamiliar is null || _stateService is null) return;

        if (SelectedFamiliar.IsOnExpedition)
        {
            ExpeditionStatus = $"{SelectedFamiliar.Name} is already on an expedition!";
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var durationSeconds = 300 + SelectedFamiliar.Level * 60; // 5 min + 1 min per level

        SelectedFamiliar.LastExpeditionUTC = now;
        SelectedFamiliar.ExpeditionDuration = TimeSpan.FromSeconds(durationSeconds);
        SelectedFamiliar.IsOnExpedition = true;
        SelectedFamiliar.ExpeditionReturnUTC = now.AddSeconds(durationSeconds);

        ExpeditionStatus = $"{SelectedFamiliar.Name} sent on expedition! Returns in {durationSeconds / 60} min.";
    }

    /// <summary>Recall a familiar early (half rewards).</summary>
    [RelayCommand]
    private void RecallFamiliar()
    {
        if (SelectedFamiliar is null || !SelectedFamiliar.IsOnExpedition) return;

        SelectedFamiliar.IsOnExpedition = false;
        SelectedFamiliar.ExpeditionReturnUTC = null;
        SelectedFamiliar.CurrentHealth = SelectedFamiliar.MaxHealth;

        ExpeditionStatus = $"{SelectedFamiliar.Name} recalled early.";
    }
}
