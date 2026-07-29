using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using MediaColor = System.Windows.Media.Color;
using AmbilightHA.Core.Engine;
using AmbilightHA.Core.Models;
using AmbilightHA.Models;
using AmbilightHA.Services;
using AmbilightHA.UI.Views;

namespace AmbilightHA.UI.ViewModels;

public class AnalysisModeOption
{
    public ColorAnalysisMode Mode { get; }
    public string Name { get; }

    public AnalysisModeOption(ColorAnalysisMode mode, string name)
    {
        Mode = mode;
        Name = name;
    }
}

public class MainViewModel : ObservableObject
{
    private readonly AmbilightOrchestrator _orchestrator;
    private readonly Dispatcher _uiDispatcher;
    private AppConfig _config;
    private ZoneOverlayWindow? _overlayWindow;

    private string _haUrl = "";
    private string _haToken = "";
    private int _displayIndex = 0;
    private AnalysisModeOption _selectedAnalysisMode;
    private float _accentWeightExponent = 2.0f;
    private float _saturation = 1.3f;
    private float _brightness = 1.0f;
    private float _gamma = 1.0f;
    private byte _minBrightness = 0;
    private float _transitionSeconds = 0.2f;
    private int _targetFps = 30;
    private int _rateLimitPerBulb = 8;
    private bool _restoreLightsOnStop = true;
    private bool _isRunning;
    private bool _isOverlayVisible;

    public bool RestoreLightsOnStop
    {
        get => _restoreLightsOnStop;
        set
        {
            if (SetProperty(ref _restoreLightsOnStop, value))
                _orchestrator.RestoreLightsOnStop = value;
        }
    }
    private string _statusMessage = "Prêt.";
    private string _logText = "";

    private DisplayInfo? _selectedDisplay;

    public ObservableCollection<ZoneMappingItemViewModel> ZoneMappings { get; } = new();
    public ObservableCollection<DisplayInfo> AvailableDisplays { get; } = new();
    public ObservableCollection<AnalysisModeOption> AvailableAnalysisModes { get; } = new()
    {
        new AnalysisModeOption(ColorAnalysisMode.VibrantAccent, "🎨 Couleur d'Accent (Filtre les gris / Recommandé Gaming)"),
        new AnalysisModeOption(ColorAnalysisMode.StandardAverage, "📊 Moyenne Standard (Écran classique)")
    };

    public string HaUrl
    {
        get => _haUrl;
        set => SetProperty(ref _haUrl, value);
    }

    public string HaToken
    {
        get => _haToken;
        set => SetProperty(ref _haToken, value);
    }

    public int DisplayIndex
    {
        get => _displayIndex;
        set
        {
            if (SetProperty(ref _displayIndex, value))
            {
                _orchestrator.DisplayIndex = value;
                var disp = AvailableDisplays.FirstOrDefault(d => d.Index == value);
                if (disp != null && _selectedDisplay != disp)
                {
                    _selectedDisplay = disp;
                    OnPropertyChanged(nameof(SelectedDisplay));
                }
            }
        }
    }

    public DisplayInfo? SelectedDisplay
    {
        get => _selectedDisplay;
        set
        {
            if (SetProperty(ref _selectedDisplay, value) && value != null)
            {
                DisplayIndex = value.Index;
            }
        }
    }

    public AnalysisModeOption SelectedAnalysisMode
    {
        get => _selectedAnalysisMode;
        set
        {
            if (SetProperty(ref _selectedAnalysisMode, value))
                _orchestrator.Mode = value.Mode;
        }
    }

    public float AccentWeightExponent
    {
        get => _accentWeightExponent;
        set
        {
            if (SetProperty(ref _accentWeightExponent, value))
                _orchestrator.AccentWeightExponent = value;
        }
    }

    public float Saturation
    {
        get => _saturation;
        set
        {
            if (SetProperty(ref _saturation, value))
                _orchestrator.Saturation = value;
        }
    }

    public float Brightness
    {
        get => _brightness;
        set
        {
            if (SetProperty(ref _brightness, value))
                _orchestrator.Brightness = value;
        }
    }

    public float Gamma
    {
        get => _gamma;
        set
        {
            if (SetProperty(ref _gamma, value))
                _orchestrator.Gamma = value;
        }
    }

    public byte MinBrightness
    {
        get => _minBrightness;
        set
        {
            if (SetProperty(ref _minBrightness, value))
                _orchestrator.MinBrightness = value;
        }
    }

    public float TransitionSeconds
    {
        get => _transitionSeconds;
        set
        {
            if (SetProperty(ref _transitionSeconds, value))
                _orchestrator.TransitionSeconds = value;
        }
    }

    public int TargetFps
    {
        get => _targetFps;
        set
        {
            if (SetProperty(ref _targetFps, value))
                _orchestrator.TargetFps = value;
        }
    }

    public int RateLimitPerBulb
    {
        get => _rateLimitPerBulb;
        set
        {
            if (SetProperty(ref _rateLimitPerBulb, value))
                _orchestrator.RateLimitPerBulb = value;
        }
    }

    private bool _isBusy;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(CanStart));
                OnPropertyChanged(nameof(CanStop));
                _uiDispatcher.InvokeAsync(() =>
                {
                    (StartCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (StopCommand as RelayCommand)?.RaiseCanExecuteChanged();
                });
            }
        }
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                OnPropertyChanged(nameof(CanStart));
                OnPropertyChanged(nameof(CanStop));
                _uiDispatcher.InvokeAsync(() =>
                {
                    (StartCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (StopCommand as RelayCommand)?.RaiseCanExecuteChanged();
                });
            }
        }
    }

    public bool IsOverlayVisible
    {
        get => _isOverlayVisible;
        set => SetProperty(ref _isOverlayVisible, value);
    }

    public bool CanStart => !IsRunning && !IsBusy;
    public bool CanStop => IsRunning && !IsBusy;

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string LogText
    {
        get => _logText;
        set => SetProperty(ref _logText, value);
    }

    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand SaveConfigCommand { get; }
    public ICommand ToggleOverlayCommand { get; }
    public ICommand AddEntityCommand { get; }
    public ICommand AddMultipleEntitiesCommand { get; }
    public ICommand RemoveEntityCommand { get; }

    public MainViewModel()
    {
        _uiDispatcher = Dispatcher.CurrentDispatcher;
        _orchestrator = new AmbilightOrchestrator();

        _selectedAnalysisMode = AvailableAnalysisModes[0];

        _orchestrator.OnLog += AppendLog;
        _orchestrator.OnFrameProcessed += UpdatePreviewColors;

        DetectDisplays();

        _config = ConfigurationService.Load();
        LoadConfigValues();

        StartCommand = new RelayCommand(ExecuteStart, () => CanStart);
        StopCommand = new RelayCommand(ExecuteStop, () => CanStop);
        SaveConfigCommand = new RelayCommand(SaveConfig);
        ToggleOverlayCommand = new RelayCommand(ToggleOverlay);
        AddEntityCommand = new RelayCommand(AddEntity);
        AddMultipleEntitiesCommand = new RelayCommand(AddMultipleEntities);
        RemoveEntityCommand = new RelayCommand<ZoneMappingItemViewModel>(RemoveEntity);
    }

    private void DetectDisplays()
    {
        AvailableDisplays.Clear();
        try
        {
            var screens = System.Windows.Forms.Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
            {
                var s = screens[i];
                AvailableDisplays.Add(new DisplayInfo(i, s.DeviceName, s.Bounds, s.Primary));
            }
        }
        catch { }

        if (AvailableDisplays.Count == 0)
        {
            AvailableDisplays.Add(new DisplayInfo(0, "Primary Display", new System.Drawing.Rectangle(0, 0, 1920, 1080), true));
        }

        SelectedDisplay = AvailableDisplays.FirstOrDefault(d => d.Index == DisplayIndex) ?? AvailableDisplays[0];
    }

    private void LoadConfigValues()
    {
        HaUrl = _config.HaUrl;
        HaToken = _config.HaToken;
        DisplayIndex = _config.DisplayIndex;

        SelectedDisplay = AvailableDisplays.FirstOrDefault(d => d.Index == _config.DisplayIndex) ?? AvailableDisplays[0];

        SelectedAnalysisMode = AvailableAnalysisModes.FirstOrDefault(m => m.Mode == _config.Mode) ?? AvailableAnalysisModes[0];
        AccentWeightExponent = _config.AccentWeightExponent;

        Saturation = _config.Saturation;
        Brightness = _config.Brightness;
        Gamma = _config.Gamma;
        MinBrightness = _config.MinBrightness;
        TransitionSeconds = _config.TransitionSeconds;
        TargetFps = _config.TargetFps;
        RateLimitPerBulb = _config.RateLimitPerBulb;
        RestoreLightsOnStop = _config.RestoreLightsOnStop;

        ZoneMappings.Clear();

        if (_config.Mappings.Count > 0)
        {
            foreach (var savedConfig in _config.Mappings)
            {
                ZoneMappings.Add(new ZoneMappingItemViewModel(
                    savedConfig.EntityId,
                    savedConfig.ZoneType,
                    savedConfig.DeviceType,
                    savedConfig.WledIpAddress
                ));
            }
        }
        else
        {
            ZoneMappings.Add(new ZoneMappingItemViewModel("light.salon_ambilight", ZoneType.Global));
            ZoneMappings.Add(new ZoneMappingItemViewModel("light.ampoule_gauche", ZoneType.Left));
            ZoneMappings.Add(new ZoneMappingItemViewModel("light.ampoule_droite", ZoneType.Right));
            ZoneMappings.Add(new ZoneMappingItemViewModel("light.bandeau_haut", ZoneType.Top));
        }
    }

    private void AddEntity()
    {
        ZoneMappings.Add(new ZoneMappingItemViewModel($"light.nouvelle_ampoule_{ZoneMappings.Count + 1}", ZoneType.Global));
        AppendLog("Nouvelle entité lumineuse ajoutée au mapping.");
    }

    private void AddMultipleEntities()
    {
        int countBefore = ZoneMappings.Count;
        ZoneMappings.Add(new ZoneMappingItemViewModel($"light.ampoule_gauche_{countBefore + 1}", ZoneType.Left));
        ZoneMappings.Add(new ZoneMappingItemViewModel($"light.ampoule_centre_{countBefore + 2}", ZoneType.Global));
        ZoneMappings.Add(new ZoneMappingItemViewModel($"light.ampoule_droite_{countBefore + 3}", ZoneType.Right));
        AppendLog("Lot de 3 nouvelles entités lumineuses (Gauche, Centre, Droite) ajouté.");
    }

    private void RemoveEntity(ZoneMappingItemViewModel? item)
    {
        if (item != null && ZoneMappings.Contains(item))
        {
            ZoneMappings.Remove(item);
            AppendLog($"Entité '{item.EntityId}' retirée du mapping.");
        }
    }

    private async void ExecuteStart()
    {
        if (IsBusy || IsRunning) return;
        if (string.IsNullOrWhiteSpace(HaUrl) || string.IsNullOrWhiteSpace(HaToken))
        {
            StatusMessage = "Erreur: URL Home Assistant et Token obligatoires !";
            return;
        }

        IsBusy = true;
        try
        {
            SaveConfig();

            lock (_orchestrator.Mappings)
            {
                _orchestrator.Mappings.Clear();
                foreach (var vm in ZoneMappings)
                {
                    string target = vm.DeviceType == LightDeviceType.WledDirectUdp ? vm.WledIpAddress : vm.EntityId;
                    if (!string.IsNullOrWhiteSpace(target))
                    {
                        _orchestrator.Mappings.Add(new ZoneLightMapping(
                            vm.SelectedZone,
                            vm.EntityId,
                            vm.DeviceType,
                            vm.WledIpAddress
                        ));
                    }
                }
            }

            _orchestrator.Mode = SelectedAnalysisMode.Mode;
            _orchestrator.AccentWeightExponent = AccentWeightExponent;
            _orchestrator.Saturation = Saturation;
            _orchestrator.Brightness = Brightness;
            _orchestrator.Gamma = Gamma;
            _orchestrator.MinBrightness = MinBrightness;
            _orchestrator.TransitionSeconds = TransitionSeconds;
            _orchestrator.TargetFps = TargetFps;
            _orchestrator.RateLimitPerBulb = RateLimitPerBulb;
            _orchestrator.DisplayIndex = DisplayIndex;
            _orchestrator.RestoreLightsOnStop = RestoreLightsOnStop;

            StatusMessage = "Connexion à Home Assistant et démarrage...";
            await _orchestrator.StartAsync(HaUrl, HaToken);

            IsRunning = _orchestrator.IsRunning;
            StatusMessage = IsRunning ? $"Moteur Ambilight Actif ({SelectedAnalysisMode.Name})" : "Échec du démarrage.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void ExecuteStop()
    {
        if (IsBusy || !IsRunning) return;
        IsBusy = true;
        try
        {
            StatusMessage = "Arrêt en cours...";
            await _orchestrator.StopAsync();
            IsRunning = false;
            StatusMessage = "Moteur Ambilight Arrêté.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SaveConfig()
    {
        _config.HaUrl = HaUrl;
        _config.HaToken = HaToken;
        _config.DisplayIndex = DisplayIndex;
        _config.Mode = SelectedAnalysisMode.Mode;
        _config.AccentWeightExponent = AccentWeightExponent;
        _config.Saturation = Saturation;
        _config.Brightness = Brightness;
        _config.Gamma = Gamma;
        _config.MinBrightness = MinBrightness;
        _config.TransitionSeconds = TransitionSeconds;
        _config.TargetFps = TargetFps;
        _config.RateLimitPerBulb = RateLimitPerBulb;
        _config.RestoreLightsOnStop = RestoreLightsOnStop;

        _config.Mappings = ZoneMappings.Select(z => new ZoneMappingConfig
        {
            ZoneType = z.ZoneType,
            EntityId = z.EntityId,
            DeviceType = z.DeviceType,
            WledIpAddress = z.WledIpAddress
        }).ToList();

        ConfigurationService.Save(_config);
        AppendLog("Configuration enregistrée dans config.json.");
    }

    private void ToggleOverlay()
    {
        if (_overlayWindow == null || !_overlayWindow.IsLoaded)
        {
            _overlayWindow = new ZoneOverlayWindow();
            var zones = ScreenZone.CreateDefaultZones();
            _overlayWindow.RenderZones(zones, SelectedDisplay);
            _overlayWindow.Show();
            IsOverlayVisible = true;
            AppendLog($"Affichage de la surimpression visuelle sur {SelectedDisplay?.Name ?? "l'écran sélectionné"}.");
        }
        else
        {
            _overlayWindow.Close();
            _overlayWindow = null;
            IsOverlayVisible = false;
            AppendLog("Fermeture de la surimpression visuelle.");
        }
    }

    private void UpdatePreviewColors(Dictionary<ZoneType, RgbColor> colors)
    {
        _uiDispatcher.InvokeAsync(() =>
        {
            foreach (var item in ZoneMappings)
            {
                if (colors.TryGetValue(item.ZoneType, out var rgb))
                {
                    item.PreviewColor = MediaColor.FromRgb(rgb.R, rgb.G, rgb.B);
                }
            }
        });
    }

    private void AppendLog(string message)
    {
        _uiDispatcher.InvokeAsync(() =>
        {
            LogText += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
        });
    }
}
