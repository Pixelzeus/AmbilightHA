using System.Collections.Generic;
using System.Linq;
using AmbilightHA.Core.Models;
using MediaColor = System.Windows.Media.Color;
using MediaColors = System.Windows.Media.Colors;

namespace AmbilightHA.UI.ViewModels;

public class ZoneMappingItemViewModel : ObservableObject
{
    private string _entityId = "";
    private ScreenZone _selectedZone;
    private MediaColor _previewColor = MediaColors.Black;

    public List<ScreenZone> AvailableZones { get; }

    public string EntityId
    {
        get => _entityId;
        set => SetProperty(ref _entityId, value);
    }

    public ScreenZone SelectedZone
    {
        get => _selectedZone;
        set
        {
            if (SetProperty(ref _selectedZone, value))
            {
                OnPropertyChanged(nameof(ZoneType));
                OnPropertyChanged(nameof(ZoneName));
            }
        }
    }

    public ZoneType ZoneType => SelectedZone.Type;
    public string ZoneName => SelectedZone.Name;

    public MediaColor PreviewColor
    {
        get => _previewColor;
        set => SetProperty(ref _previewColor, value);
    }

    public ZoneMappingItemViewModel(string entityId, ZoneType initialZoneType)
    {
        AvailableZones = ScreenZone.CreateDefaultZones();
        _entityId = entityId;
        _selectedZone = AvailableZones.FirstOrDefault(z => z.Type == initialZoneType) ?? AvailableZones[0];
    }
}
