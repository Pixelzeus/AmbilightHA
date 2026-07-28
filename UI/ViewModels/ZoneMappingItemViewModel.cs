using System.Collections.Generic;
using System.Linq;
using AmbilightHA.Core.Models;
using MediaColor = System.Windows.Media.Color;
using MediaColors = System.Windows.Media.Colors;

namespace AmbilightHA.UI.ViewModels;

public class DeviceTypeOption
{
    public LightDeviceType Type { get; }
    public string Name { get; }

    public DeviceTypeOption(LightDeviceType type, string name)
    {
        Type = type;
        Name = name;
    }
}

public class ZoneMappingItemViewModel : ObservableObject
{
    private string _entityId = "";
    private string _wledIpAddress = "";
    private DeviceTypeOption _selectedDeviceType;
    private ScreenZone _selectedZone;
    private MediaColor _previewColor = MediaColors.Black;

    public List<ScreenZone> AvailableZones { get; }
    public List<DeviceTypeOption> AvailableDeviceTypes { get; } = new()
    {
        new DeviceTypeOption(LightDeviceType.HomeAssistant, "💡 Home Assistant Light"),
        new DeviceTypeOption(LightDeviceType.WledHA, "🌈 WLED (via Home Assistant)"),
        new DeviceTypeOption(LightDeviceType.WledDirectUdp, "⚡ WLED Direct UDP (IP <1ms)")
    };

    public string EntityId
    {
        get => _entityId;
        set => SetProperty(ref _entityId, value);
    }

    public string WledIpAddress
    {
        get => _wledIpAddress;
        set => SetProperty(ref _wledIpAddress, value);
    }

    public DeviceTypeOption SelectedDeviceType
    {
        get => _selectedDeviceType;
        set
        {
            if (SetProperty(ref _selectedDeviceType, value))
            {
                OnPropertyChanged(nameof(DeviceType));
                OnPropertyChanged(nameof(IsWledUdpMode));
                OnPropertyChanged(nameof(TargetLabel));
            }
        }
    }

    public LightDeviceType DeviceType => SelectedDeviceType.Type;
    public bool IsWledUdpMode => DeviceType == LightDeviceType.WledDirectUdp;
    public string TargetLabel => IsWledUdpMode ? "Adresse IP WLED (ex: 192.168.1.100):" : "Entité Light / Sensor HA:";

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

    public ZoneMappingItemViewModel(string entityId, ZoneType initialZoneType, LightDeviceType deviceType = LightDeviceType.HomeAssistant, string wledIpAddress = "")
    {
        AvailableZones = ScreenZone.CreateDefaultZones();
        _entityId = entityId;
        _wledIpAddress = wledIpAddress;
        _selectedDeviceType = AvailableDeviceTypes.FirstOrDefault(d => d.Type == deviceType) ?? AvailableDeviceTypes[0];
        _selectedZone = AvailableZones.FirstOrDefault(z => z.Type == initialZoneType) ?? AvailableZones[0];
    }
}
