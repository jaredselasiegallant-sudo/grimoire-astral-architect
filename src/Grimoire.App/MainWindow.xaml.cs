using System.IO;
using System.Numerics;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using SkiaSharp.Views.Windows;
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
using Grimoire.Engine.Ecology;
using Grimoire.Engine.Audio;
using Grimoire.Core.Input;
using Windows.Graphics;
using Windows.UI;

namespace Grimoire.App;

public sealed partial class MainWindow : Window
{
    private readonly IGameStateService _stateService;
    private readonly NarrativeService _narrativeService;
    private readonly TutorialService _tutorialService;
    private readonly INotificationService _notificationService;
    private readonly ISettingsService _settingsService;
    private readonly GameRepository _repository;

    private readonly GameLoopService _gameLoop;
    private readonly GameCanvas _renderer;
    private readonly ParticleSystem _particles;
    private readonly GestureRecognitionEngine _gestureEngine;
    private readonly ReturnRitual _returnRitual;
    private readonly PhotoMode _photoMode;
    private readonly JuiceEngine _juice;
    private readonly ParticleEcology _particleEcology = new();
    private readonly AmbientSoundscape _soundscape = new();
    private readonly MusicalSpellcaster _musicalSpellcaster = new();
    private readonly GestureMastery _gestureMastery = new();
    private Timer? _autoSaveTimer;

    private SKXamlCanvas? _skiaCanvas;
    private MainViewModel ViewModel { get; }
    private readonly List<Vector2> _currentStrokeTrail = [];
    private bool _narrativeActive;
    private NarrativeLine? _pendingNarrativeLine;

    public MainWindow()
    {
        this.InitializeComponent();
        this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1400, 800));

        // Create SkiaSharp canvas programmatically (avoids XAML type resolution)
        _skiaCanvas = new SKXamlCanvas();
        _skiaCanvas.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
            Microsoft.UI.Xaml.ColorHelper.FromArgb(255, 5, 5, 16));
        _skiaCanvas.PaintSurface += SKCanvasView_PaintSurface;
        _skiaCanvas.PointerPressed += GameCanvas_PointerPressed;
        _skiaCanvas.PointerMoved += GameCanvas_PointerMoved;
        _skiaCanvas.PointerReleased += GameCanvas_PointerReleased;
        CanvasHost.Child = _skiaCanvas;

        var dbDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GrimoireAstralArchitect");
        Directory.CreateDirectory(dbDir);
        var dbPath = Path.Combine(dbDir, "grimoire.db");

        _repository = new GameRepository(dbPath);
        _stateService = new GameStateService(_repository);
        _narrativeService = new NarrativeService();
        _tutorialService = new TutorialService();
        _notificationService = new NotificationService();
        _settingsService = new SettingsService();
        _settingsService.Load();

        ViewModel = new MainViewModel(_stateService);

        _renderer = new GameCanvas();
        _particles = new ParticleSystem();
        _gameLoop = new GameLoopService { TargetFps = _settingsService.Settings.TargetFps };
        _gestureEngine = new GestureRecognitionEngine();
        _returnRitual = new ReturnRitual();
        _photoMode = new PhotoMode();
        _juice = new JuiceEngine();

        _gameLoop.FrameTick += OnFrameTick;
        _narrativeService.OnNarrativeReady += OnNarrativeReady;
        _narrativeService.OnChapterCompleted += OnChapterCompleted;
        _tutorialService.OnStepActivated += OnTutorialStepActivated;
        _tutorialService.OnTutorialComplete += OnTutorialComplete;
        _notificationService.OnNotificationQueued += OnNotificationQueued;

        this.Closed += OnWindowClosed;
        this.Activated += OnWindowActivated;

        RestoreWindowState();
    }

    private async void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated) return;
        if (_gameLoop.IsRunning) return;

        await _repository.InitialiseAsync();
        await _stateService.InitialiseAsync();

        _narrativeService.RestoreShownLines(_stateService.CurrentState.ShownNarrativeLines);
        _tutorialService.RestoreProgress(_stateService.CurrentState.CompletedTutorialSteps);

        _gameLoop.Start();
        _particleEcology.Initialise(1400, 800);
        _autoSaveTimer = SaveLoadService.StartAutoSaveTimer(TimeSpan.FromSeconds(30));
        SaveLoadService.Initialise(_stateService);

        if (!_gameLoop.IsRunning) return;
        _returnRitual.Start();
        _returnRitual.OnRitualComplete += () =>
        {
            _ = DispatcherQueue.TryEnqueue(() =>
            {
                if (_stateService.CurrentState.ExpeditionLog.Count == 0 &&
                    !_stateService.CurrentState.TutorialCompleted)
                {
                    _narrativeService.FireEvent("game_launch");
                }
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

        SaveWindowState();
        _settingsService.Save();

        await SaveLoadService.SaveOnExitAsync();
        _repository.Dispose();
    }

    private void OnFrameTick(float deltaTime, double elapsedTime)
    {
        _particles.Update(deltaTime);
        _returnRitual.Update(deltaTime);
        _juice.Update(deltaTime);
        _particleEcology.Update(deltaTime, 1400, 800);
        _soundscape.Update("morning", "spring", _stateService.CurrentState.Buildings.Count, deltaTime);
        _musicalSpellcaster.Update(deltaTime);

        _stateService.TickManaRegen(deltaTime);
        _stateService.TickCorruption(deltaTime);

        var hatchedEggs = _stateService.CheckAndHatchEggs();
        foreach (var egg in hatchedEggs)
        {
            _notificationService.ShowHatchComplete($"{egg.Element} Familiar");
            _narrativeService.FireEvent("familiar_hatched");
            _juice.OnHatch();
        }

        _ = DispatcherQueue.TryEnqueue(() =>
        {
            ManaText.Text = _stateService.CurrentState.ManaCrystals.ToString();

            var onCooldown = Enum.GetValues<SpellGesture>()
                .Where(g => g != SpellGesture.Unknown)
                .Where(g => !_stateService.IsSpellReady(g))
                .ToList();
            var cooldownText = onCooldown.Count > 0
                ? $"\u23F3 {_stateService.GetSpellCooldownRemaining(onCooldown[0]):mm\\:ss}"
                : "";
            CooldownText.Text = cooldownText;

            _skiaCanvas?.Invalidate();
        });
    }

    private void SKCanvasView_PaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var surface = e.Surface;
        var size = e.Info.Size;

        var shake = _juice.ShakeOffset;
        surface.Canvas.Translate(shake.X, shake.Y);

        _renderer.Render(surface.Canvas, new SKSizeI(size.Width, size.Height),
            _gameLoop.ElapsedTime,
            _stateService.CurrentState.Buildings,
            _stateService.CurrentState.Familiars,
            _stateService.CurrentState.Corruption,
            _stateService.CurrentState.Weather,
            _stateService.CurrentState.Constellations);

        _particles.Draw(surface.Canvas);
        _particleEcology.Render(surface.Canvas);

        if (_currentStrokeTrail.Count > 1)
            _renderer.SetGestureTrail(_currentStrokeTrail);

        _returnRitual.Render(surface.Canvas, size.Width, size.Height, _gameLoop.ElapsedTime);
        _photoMode.RenderOverlay(surface.Canvas, size.Width, size.Height);

        if (_juice.IsFlashing)
        {
            using var flashPaint = new SKPaint
            {
                Color = new SKColor(_juice.FlashColor.Red, _juice.FlashColor.Green,
                    _juice.FlashColor.Blue, (byte)(_juice.FlashAlpha * 80))
            };
            surface.Canvas.DrawRect(0, 0, size.Width, size.Height, flashPaint);
        }

        surface.Canvas.Translate(-shake.X, -shake.Y);
    }

    private void GameCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_narrativeActive || _skiaCanvas is null) return;

        var pos = e.GetCurrentPoint(_skiaCanvas).Position;
        _currentStrokeTrail.Clear();
        _currentStrokeTrail.Add(new Vector2((float)pos.X, (float)pos.Y));

        _gestureEngine.BeginStroke();
        _gestureEngine.AddPoint(new Vector2((float)pos.X, (float)pos.Y));
    }

    private void GameCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_narrativeActive || _skiaCanvas is null) return;

        var pos = e.GetCurrentPoint(_skiaCanvas).Position;
        _currentStrokeTrail.Add(new Vector2((float)pos.X, (float)pos.Y));
        _gestureEngine.AddPoint(new Vector2((float)pos.X, (float)pos.Y));
        _particleEcology.SetCursor((float)pos.X, (float)pos.Y, true);
    }

    private void GameCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_narrativeActive || _skiaCanvas is null) return;

        var gesture = _gestureEngine.EndStroke();
        var pos = e.GetCurrentPoint(_skiaCanvas).Position;

        var qualityRecord = _gestureMastery.RecordAttempt(gesture,
            _gestureEngine.GetLastStrokeDeviations().ToArray(),
            _gestureEngine.GetLastStrokeSpeed(),
            _gestureEngine.GetLastStrokeDuration());

        _musicalSpellcaster.CastSpell(gesture);

        if (gesture != SpellGesture.Unknown)
        {
            if (!_stateService.IsSpellReady(gesture))
            {
                SpellStatusText.Text = $"Spell on cooldown \u2014 wait {_stateService.GetSpellCooldownRemaining(gesture):mm\\:ss}";
                _currentStrokeTrail.Clear();
                return;
            }

            var manaCost = GetSpellManaCost(gesture);
            if (_stateService.CurrentState.ManaCrystals < manaCost)
            {
                SpellStatusText.Text = $"Not enough mana for {gesture} (need {manaCost})";
                _currentStrokeTrail.Clear();
                return;
            }

            _stateService.CurrentState.ManaCrystals -= manaCost;
            _stateService.PutSpellOnCooldown(gesture, TimeSpan.FromSeconds(10));
            _stateService.CurrentState.TotalSpellsCast++;

            var comboTracker = _stateService.GetComboTracker();
            var combo = comboTracker.AddGesture(gesture);

            if (combo is not null)
            {
                SpellStatusText.Text = $"COMBO: {combo.Name} \u2014 {combo.Description}";
                _juice.OnComboComplete();
                _notificationService.Show("Spell Combo", combo.Name, NotificationType.Success, 3f);
            }
            else
            {
                SpellStatusText.Text = GetSpellDescription(gesture);
                _juice.OnSpellCast(gesture.ToString());
            }

            if (qualityRecord.AverageQuality > 0.7f)
                SpellStatusText.Text += $" (Quality: {qualityRecord.Tier})";

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

            if (gesture == SpellGesture.Circle)
                _tutorialService.TryCompleteStep("circle_gesture");

            _narrativeService.FireEvent("first_spell_cast");
        }
        else
        {
            SpellStatusText.Text = "Gesture not recognised \u2014 try again";
        }

        _currentStrokeTrail.Clear();
        _renderer.ClearGestureTrail();
    }

    private void MainWindow_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
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

    private void CraftButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CraftingCauldron.CraftCommand.Execute(null);
        CraftingResultText.Text = ViewModel.CraftingCauldron.CraftingResult;
    }

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
            _stateService.CurrentState.ShownNarrativeLines.Add(_pendingNarrativeLine.Id);
            _pendingNarrativeLine = null;
        }

        _narrativeActive = false;
        NarrativeOverlay.Visibility = Visibility.Collapsed;

        var next = _narrativeService.GetNextPending();
        if (next is not null)
        {
            _pendingNarrativeLine = next;
            ShowNarrative(next);
        }
    }

    private void NarrativeDismissButton_Click(object sender, RoutedEventArgs e) => DismissNarrative();

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

        var timer = new Timer(_ =>
        {
            _ = DispatcherQueue.TryEnqueue(() =>
            {
                ToastContainer.Children.Remove(border);
            });
        }, null, TimeSpan.FromSeconds(evt.DurationSeconds), Timeout.InfiniteTimeSpan);
    }

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
        _settingsService.Settings.IsMaximized = this.AppWindow.Presenter != null;
    }

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
        SpellGesture.Circle => "Circle of Warding cast \u2014 protective barrier active",
        SpellGesture.Triangle => "Triangle of Binding cast \u2014 essence trapped",
        SpellGesture.Line => "Line of Division cast \u2014 obstacles cleaved",
        SpellGesture.Zigzag => "Zigzag of Disruption cast \u2014 area scattered",
        SpellGesture.Spiral => "Spiral of Unravelling cast \u2014 hidden loot revealed",
        _ => "Spell cast"
    };
}
