using System.IO;
using System.Numerics;
using System.Collections.ObjectModel;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using SkiaSharp.Views.WinUI;
using Grimoire.App.Services;
using Grimoire.App.ViewModels;
using Grimoire.Core.Enums;
using Grimoire.Core.Interfaces;
using Grimoire.Core.Models;
using Grimoire.Core.Narrative;
using Grimoire.Core.Tutorial;
using Grimoire.Data.Repositories;
using Grimoire.Engine.GameLoop;
using Grimoire.Engine.Input;
using Grimoire.Engine.Rendering;
using Grimoire.Engine.Juice;
using Grimoire.Core.Buildings;
using Grimoire.Core.Models;
using Grimoire.Engine.Ecology;
using Grimoire.Engine.Audio;
using Grimoire.Core.Input;
using Grimoire.Core.Bonding;
using Windows.Graphics;
using Windows.UI;

namespace Grimoire.App;

public sealed partial class MainWindow : Window
{
    // ─── Core services ───────────────────────────────────────────
    private readonly IGameStateService _stateService;
    private readonly NarrativeService _narrativeService;
    private readonly TutorialService _tutorialService;
    private readonly INotificationService _notificationService;
    private readonly ISettingsService _settingsService;
    private readonly GameRepository _repository;

    // ─── Engine ──────────────────────────────────────────────────
    private readonly GameLoopService _gameLoop;
    private readonly GameCanvas _renderer;
    private readonly ParticleSystem _particles;
    private readonly GestureRecognitionEngine _gestureEngine;
    private readonly ReturnRitual _returnRitual;
    private readonly PhotoMode _photoMode;
    private readonly JuiceEngine _juice;
    private readonly Grimoire.Engine.Ecology.ParticleEcology _particleEcology = new();
    private readonly Grimoire.Engine.Audio.AmbientSoundscape _soundscape = new();
    private readonly Grimoire.Engine.Audio.MusicalSpellcaster _musicalSpellcaster = new();
    private readonly Grimoire.Core.Input.GestureMastery _gestureMastery = new();
    private Timer? _autoSaveTimer;

    // ─── State ───────────────────────────────────────────────────
    private MainViewModel ViewModel { get; }
    private readonly List<Vector2> _currentStrokeTrail = [];
    private bool _narrativeActive;
    private NarrativeLine? _pendingNarrativeLine;

    public MainWindow()
    {
        this.InitializeComponent();

        // WinUI 3: set size via code-behind (not valid in XAML)
        this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1400, 800));

        // Database path
        var dbDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GrimoireAstralArchitect");
        Directory.CreateDirectory(dbDir);
        var dbPath = Path.Combine(dbDir, "grimoire.db");

        // Services
        _repository = new GameRepository(dbPath);
        _stateService = new GameStateService(_repository);
        _narrativeService = new NarrativeService();
        _tutorialService = new TutorialService();
        _notificationService = new NotificationService();
        _settingsService = new SettingsService();
        _settingsService.Load();

        // ViewModels
        ViewModel = new MainViewModel(_stateService);

        // Engine
        _renderer = new GameCanvas();
        _particles = new ParticleSystem();
        _gameLoop = new GameLoopService { TargetFps = _settingsService.Settings.TargetFps };
        _gestureEngine = new GestureRecognitionEngine();
        _returnRitual = new ReturnRitual();
        _photoMode = new PhotoMode();
        _juice = new JuiceEngine();

        // Events
        _gameLoop.FrameTick += OnFrameTick;
        _narrativeService.OnNarrativeReady += OnNarrativeReady;
        _narrativeService.OnChapterCompleted += OnChapterCompleted;
        _tutorialService.OnStepActivated += OnTutorialStepActivated;
        _tutorialService.OnTutorialComplete += OnTutorialComplete;
        _notificationService.OnNotificationQueued += OnNotificationQueued;

        this.Closed += OnWindowClosed;
        this.Activated += OnWindowActivated;

        // Restore window position
        RestoreWindowState();
    }

    // ─── Lifecycle ───────────────────────────────────────────────

    private async void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated) return;
        if (_gameLoop.IsRunning) return;

        await _repository.InitialiseAsync();
        await _stateService.InitialiseAsync();

        ViewModel.Inventory.LoadFrom(_stateService.CurrentState.Inventory);
        ViewModel.FamiliarManagement.LoadFrom(_stateService.CurrentState.Familiars);
        ViewModel.CraftingCauldron.LoadFrom(_stateService.CurrentState.Inventory);

        // Restore narrative and tutorial progress
        _narrativeService.RestoreShownLines(_stateService.CurrentState.ShownNarrativeLines);
        _tutorialService.RestoreProgress(_stateService.CurrentState.CompletedTutorialSteps);

        // Start game loop
        _gameLoop.Start();
        _particleEcology.Initialise();
        _soundscape.Start();
        _autoSaveTimer = SaveLoadService.StartAutoSaveTimer(TimeSpan.FromSeconds(30));
        SaveLoadService.Initialise(_stateService);

        // Start return ritual on first launch
        if (!_gameLoop.IsRunning) return; // guard against double-fire
        _returnRitual.Start();
        _returnRitual.OnRitualComplete += () =>
        {
            _ = DispatcherQueue.TryEnqueue(() =>
            {
                // Trigger prologue if first launch
                if (_stateService.CurrentState.ExpeditionLog.Count == 0 &&
                    !_stateService.CurrentState.TutorialCompleted)
                {
                    _narrativeService.FireEvent("game_launch");
                }
                // Show active astral events
                foreach (var evt in _stateService.CurrentState.ActiveEvents.Where(e => e.IsActive))
                {
                    _notificationService.Show("Astral Event", $"{evt.Name} — {evt.Description}",
                        NotificationType.Narrative, 5f);
                }
            });
        };
    }

    private async void OnWindowClosed(object sender, WindowEventArgs args)
    {
        _gameLoop.FrameTick -= OnFrameTick;
        _autoSaveTimer?.Dispose();
        await _gameLoop.StopAsync();

        // Persist window state
        SaveWindowState();
        _settingsService.Save();

        await SaveLoadService.SaveOnExitAsync();
        _repository.Dispose();
    }

    // ─── Render Loop ─────────────────────────────────────────────

    private void OnFrameTick(float deltaTime, double elapsedTime)
    {
        _particles.Update(deltaTime);
        _returnRitual.Update(deltaTime);
        _juice.Update(deltaTime);
        _particleEcology.Update(deltaTime);
        _soundscape.Update(deltaTime);
        _musicalSpellcaster.Update(deltaTime);

        // Tick mana regen and corruption
        _stateService.TickManaRegen(deltaTime);
        _stateService.TickCorruption(deltaTime);

        // Check egg hatching
        var hatchedEggs = _stateService.CheckAndHatchEggs();
        foreach (var egg in hatchedEggs)
        {
            _notificationService.ShowHatchComplete($"{egg.Element} Familiar");
            _narrativeService.FireEvent("familiar_hatched");
            _juice.OnHatch();
        }

        // Update cooldown display
        _ = DispatcherQueue.TryEnqueue(() =>
        {
            ViewModel.UpdateCooldownDisplay(_stateService);
            SKCanvasView?.Invalidate();
        });
    }

    private void SKCanvasView_PaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var surface = e.Surface;
        var size = e.Info.Size;

        // Apply screen shake offset
        var shake = _juice.ShakeOffset;
        surface.Canvas.Translate(shake.X, shake.Y);

        // Render game world
        _renderer.Render(surface.Canvas, new SKSizeI(size.Width, size.Height),
            _gameLoop.ElapsedTime,
            _stateService.CurrentState.Buildings,
            _stateService.CurrentState.Familiars,
            _stateService.CurrentState.Corruption,
            _stateService.CurrentState.Weather,
            _stateService.CurrentState.Constellations);

        // Draw particles
        _particles.Draw(surface.Canvas);

        _particleEcology.Render(surface.Canvas, new SKSizeI(size.Width, size.Height), _gameLoop.ElapsedTime);

        // Draw gesture trail
        if (_currentStrokeTrail.Count > 1)
            _renderer.SetGestureTrail(_currentStrokeTrail);

        // Return ritual overlay
        _returnRitual.Render(surface.Canvas, size.Width, size.Height, _gameLoop.ElapsedTime);

        // Photo mode overlay
        _photoMode.RenderOverlay(surface.Canvas, size.Width, size.Height);

        // Colour flash overlay
        if (_juice.IsFlashing)
        {
            using var flashPaint = new SKPaint
            {
                Color = new SKColor(_juice.FlashColor.Red, _juice.FlashColor.Green,
                    _juice.FlashColor.Blue, (byte)(_juice.FlashAlpha * 80))
            };
            surface.Canvas.DrawRect(0, 0, size.Width, size.Height, flashPaint);
        }

        // Reset shake transform
        surface.Canvas.Translate(-shake.X, -shake.Y);
    }

    // ─── Gesture Input ───────────────────────────────────────────

    private void GameCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_narrativeActive) return;

        var pos = e.GetCurrentPoint(SKCanvasView).Position;
        _currentStrokeTrail.Clear();
        _currentStrokeTrail.Add(new Vector2((float)pos.X, (float)pos.Y));

        _gestureEngine.BeginStroke();
        _gestureEngine.AddPoint(new Vector2((float)pos.X, (float)pos.Y));
    }

    private void GameCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_narrativeActive) return;

        var pos = e.GetCurrentPoint(SKCanvasView).Position;
        _currentStrokeTrail.Add(new Vector2((float)pos.X, (float)pos.Y));
        _gestureEngine.AddPoint(new Vector2((float)pos.X, (float)pos.Y));
        _particleEcology.SetCursor((float)pos.X, (float)pos.Y);
    }

    private void GameCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_narrativeActive) return;

        var gesture = _gestureEngine.EndStroke();
        var pos = e.GetCurrentPoint(SKCanvasView).Position;

        // Record gesture quality for mastery
        var qualityRecord = _gestureMastery.RecordAttempt(gesture, 
            _gestureEngine.GetLastStrokeDeviations(), 
            _gestureEngine.GetLastStrokeSpeed(),
            _gestureEngine.GetLastStrokeDuration());

        // Musical spellcasting
        _musicalSpellcaster.CastSpell(gesture);

        if (gesture != SpellGesture.Unknown)
        {
            // Check cooldown
            if (!_stateService.IsSpellReady(gesture))
            {
                ViewModel.CurrentSpellStatus = $"Spell on cooldown — wait {_stateService.GetSpellCooldownRemaining(gesture):mm\\:ss}";
                _currentStrokeTrail.Clear();
                return;
            }

            // Check mana cost
            var manaCost = GetSpellManaCost(gesture);
            if (_stateService.CurrentState.ManaCrystals < manaCost)
            {
                ViewModel.CurrentSpellStatus = $"Not enough mana for {gesture} (need {manaCost})";
                _currentStrokeTrail.Clear();
                return;
            }

            _stateService.CurrentState.ManaCrystals -= manaCost;
            _stateService.PutSpellOnCooldown(gesture, TimeSpan.FromSeconds(10));
            _stateService.CurrentState.TotalSpellsCast++;

            // Check for combo
            var comboTracker = _stateService.GetComboTracker();
            var combo = comboTracker.AddGesture(gesture);

            if (combo is not null)
            {
                // Combo completed!
                ViewModel.CurrentSpellStatus = $"COMBO: {combo.Name} — {combo.Description}";
                _juice.OnComboComplete();
                _notificationService.Show("Spell Combo", combo.Name, NotificationType.Success, 3f);
            }
            else
            {
                ViewModel.CurrentSpellStatus = GetSpellDescription(gesture);
                _juice.OnSpellCast(gesture.ToString());
            }

            if (qualityRecord.AverageQuality > 0.7f)
                ViewModel.CurrentSpellStatus += $" (Quality: {qualityRecord.Tier})";

            // Particle burst
            var color = gesture switch
            {
                SpellGesture.Circle => new SKColor(100, 200, 255),
                SpellGesture.Triangle => new SKColor(255, 100, 200),
                SpellGesture.Line => new SKColor(200, 255, 100),
                SpellGesture.Zigzag => new SKColor(255, 200, 100),
                SpellGesture.Spiral => new SKColor(200, 100, 255),
                _ => new SKColor(255, 255, 255)
            };
            _particles.Burst((float)pos.X, (float)pos.Y, color, count: 30, speed: 150f);

            // Tutorial trigger
            if (gesture == SpellGesture.Circle)
                _tutorialService.TryCompleteStep("circle_gesture");

            // Narrative trigger
            _narrativeService.FireEvent("first_spell_cast");
        }
        else
        {
            ViewModel.CurrentSpellStatus = "Gesture not recognised — try again";
        }

        _currentStrokeTrail.Clear();
        _renderer.ClearGestureTrail();
    }

    // ─── Keyboard Shortcuts ──────────────────────────────────────

    private void MainWindow_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case Windows.System.VirtualKey.S when e.KeyModifiers == Windows.System.VirtualKeyModifiers.Control:
                _ = ViewModel.SaveGameCommand.ExecuteAsync(null);
                e.Handled = true;
                break;

            case Windows.System.VirtualKey.F5:
                _ = ViewModel.SaveGameCommand.ExecuteAsync(null);
                e.Handled = true;
                break;

            case Windows.System.VirtualKey.Escape:
                if (_narrativeActive)
                    DismissNarrative();
                e.Handled = true;
                break;

            case Windows.System.VirtualKey.F1:
                _narrativeService.FireEvent("game_launch");
                e.Handled = true;
                break;
        }
    }

    // ─── Narrative Overlay ───────────────────────────────────────

    private void OnNarrativeReady(NarrativeLine line)
    {
        _pendingNarrativeLine = line;
        _ = DispatcherQueue.TryEnqueue(() => ShowNarrative(line));
    }

    private void OnChapterCompleted(NarrativeChapter chapter)
    {
        _ = DispatcherQueue.TryEnqueue(() =>
        {
            _notificationService.Show("Chapter Complete", chapter.Subtitle,
                NotificationType.Narrative, 5f);
        });
    }

    private void ShowNarrative(NarrativeLine line)
    {
        _narrativeActive = true;
        NarrativeOverlay.Visibility = Visibility.Visible;

        NarrativeSpeaker.Text = line.Type switch
        {
            NarrativeType.Narrator => "NARRATOR",
            NarrativeType.Familiar => line.Speaker.ToUpperInvariant(),
            NarrativeType.DistantVoice => "???",
            _ => line.Speaker.ToUpperInvariant()
        };

        NarrativeText.Text = line.Text;

        NarrativeDismissButton.Visibility = Visibility.Visible;
    }

    private void DismissNarrative()
    {
        if (_pendingNarrativeLine is not null)
        {
            _narrativeService.MarkLineShown(_pendingNarrativeLine.Id);

            // Persist
            _stateService.CurrentState.ShownNarrativeLines.Add(_pendingNarrativeLine.Id);
            _pendingNarrativeLine = null;
        }

        _narrativeActive = false;
        NarrativeOverlay.Visibility = Visibility.Collapsed;

        // Check for queued follow-up
        var next = _narrativeService.GetNextPending();
        if (next is not null)
        {
            _pendingNarrativeLine = next;
            ShowNarrative(next);
        }
    }

    private void NarrativeDismissButton_Click(object sender, RoutedEventArgs e) => DismissNarrative();

    // ─── Tutorial ────────────────────────────────────────────────

    private void OnTutorialStepActivated(TutorialStep step)
    {
        _ = DispatcherQueue.TryEnqueue(() =>
        {
            _notificationService.Show("Tutorial", step.Instruction,
                NotificationType.Info, 5f);
        });
    }

    private void OnTutorialComplete()
    {
        _stateService.CurrentState.TutorialCompleted = true;
        _narrativeService.FireEvent("chapter1_complete");
    }

    // ─── Notifications (Toast) ───────────────────────────────────

    private void OnNotificationQueued(NotificationEvent evt)
    {
        _ = DispatcherQueue.TryEnqueue(() => ShowToast(evt));
    }

    private void ShowToast(NotificationEvent evt)
    {
        var color = evt.Type switch
        {
            NotificationType.Success => Color.FromArgb(200, 40, 180, 80),
            NotificationType.Warning => Color.FromArgb(200, 220, 160, 40),
            NotificationType.Narrative => Color.FromArgb(200, 160, 100, 255),
            _ => Color.FromArgb(200, 60, 80, 120)
        };

        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(220, 18, 18, 42)),
            BorderBrush = new SolidColorBrush(color),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 10, 16, 10),
            MaxWidth = 360,
            Margin = new Thickness(0, 0, 0, 4)
        };

        var stack = new StackPanel { Spacing = 4 };
        stack.Children.Add(new TextBlock
        {
            Text = evt.Title,
            Foreground = new SolidColorBrush(color),
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });
        stack.Children.Add(new TextBlock
        {
            Text = evt.Message,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 200, 210, 230)),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        });

        border.Child = stack;
        ToastContainer.Children.Add(border);

        // Auto-remove after duration
        var timer = new Timer(_ =>
        {
            _ = DispatcherQueue.TryEnqueue(() =>
            {
                ToastContainer.Children.Remove(border);
            });
        }, null, TimeSpan.FromSeconds(evt.DurationSeconds), Timeout.InfiniteTimeSpan);
    }

    // ─── Window State Persistence ────────────────────────────────

    private void RestoreWindowState()
    {
        var s = _settingsService.Settings;
        if (s.WindowX >= 0 && s.WindowY >= 0)
        {
            this.AppWindow.Move(new PointInt32(s.WindowX, s.WindowY));
        }
        this.AppWindow.Resize(new SizeInt32(s.WindowWidth, s.WindowHeight));
    }

    private void SaveWindowState()
    {
        var pos = this.AppWindow.Position;
        var size = this.AppWindow.Size;
        _settingsService.Settings.WindowX = pos.X;
        _settingsService.Settings.WindowY = pos.Y;
        _settingsService.Settings.WindowWidth = size.Width;
        _settingsService.Settings.WindowHeight = size.Height;
        _settingsService.Settings.IsMaximized = this.AppWindow.Presenter is Microsoft.UI.WindowManagement.AppWindowPresenterKind.Overlapped;
    }

    // ─── Ambient Effects ─────────────────────────────────────────

    private void RegisterAmbientEmitters()
    {
        foreach (var building in _stateService.CurrentState.Buildings)
        {
            _particles.AddEmitter(new ParticleSystem.Emitter
            {
                X = building.GridX * 80 + 40,
                Y = building.GridY * 80 + 40,
                Color = new SKColor(100, 180, 255, 120),
                Speed = 8f,
                Lifetime = 3f,
                Size = 2f,
                Spread = 30f,
                Interval = 0.5f
            });
        }
    }

    // ─── Spell Helpers ───────────────────────────────────────────

    private static int GetSpellManaCost(SpellGesture gesture) => gesture switch
    {
        SpellGesture.Circle => 10,
        SpellGesture.Triangle => 20,
        SpellGesture.Line => 5,
        SpellGesture.Zigzag => 15,
        SpellGesture.Spiral => 30,
        _ => 0
    };

    private static string GetSpellDescription(SpellGesture gesture) => gesture switch
    {
        SpellGesture.Circle => "Circle of Warding cast — protective barrier active",
        SpellGesture.Triangle => "Triangle of Binding cast — essence trapped",
        SpellGesture.Line => "Line of Division cast — obstacles cleaved",
        SpellGesture.Zigzag => "Zigzag of Disruption cast — area scattered",
        SpellGesture.Spiral => "Spiral of Unravelling cast — hidden loot revealed",
        _ => "Spell cast"
    };
}
