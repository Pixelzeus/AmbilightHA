using System;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace AmbilightHA.Core.Capture;

public sealed class DxgiScreenCapture : IDisposable
{
    private ID3D11Device? _device;
    private ID3D11DeviceContext? _context;
    private IDXGIOutputDuplication? _deskDupl;
    private ID3D11Texture2D? _stagingTexture;

    private int _displayIndex;
    private int _width;
    private int _height;
    private bool _isInitialized;

    public int Width => _width;
    public int Height => _height;
    public bool IsInitialized => _isInitialized;

    public DxgiScreenCapture(int displayIndex = 0)
    {
        _displayIndex = displayIndex;
    }

    /// <summary>
    /// Initialise les ressources Direct3D11 et l'API DXGI Desktop Duplication.
    /// </summary>
    public void Initialize()
    {
        Dispose();

        // 1. Création du Device Direct3D11 avec le premier Adapter DXGI compatible
        using var factory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();
        if (factory == null)
            throw new Exception("Impossible de créer le Factory DXGI 1.1");

        if (factory.EnumAdapters1(0u, out IDXGIAdapter1? adapter).Failure || adapter == null)
            throw new Exception("Aucun adaptateur DXGI trouvé.");

        var featureLevels = new[] { FeatureLevel.Level_11_0, FeatureLevel.Level_10_1, FeatureLevel.Level_10_0 };
        if (D3D11.D3D11CreateDevice(adapter, DriverType.Unknown, DeviceCreationFlags.None, featureLevels, out _device, out _context).Failure || _device == null || _context == null)
        {
            throw new Exception("Échec de création du D3D11Device.");
        }

        // 2. Récupération de l'écran (Output)
        if (adapter.EnumOutputs((uint)_displayIndex, out IDXGIOutput? output).Failure || output == null)
            throw new Exception($"Écran #{_displayIndex} introuvable.");

        using var output1 = output.QueryInterface<IDXGIOutput1>();
        if (output1 == null)
            throw new Exception("Output ne supporte pas IDXGIOutput1.");

        // 3. Duplication du bureau (Desktop Duplication)
        _deskDupl = output1.DuplicateOutput(_device);
        if (_deskDupl == null)
        {
            throw new Exception($"Échec de la duplication du bureau sur l'écran #{_displayIndex}. Assurez-vous qu'une application plein écran exclusive ne bloque pas le DXGI.");
        }

        var modeDesc = output.Description;
        _width = modeDesc.DesktopCoordinates.Right - modeDesc.DesktopCoordinates.Left;
        _height = modeDesc.DesktopCoordinates.Bottom - modeDesc.DesktopCoordinates.Top;

        // 4. Texture Staging (CPU Read access) pour rapatrier l'image du GPU vers la RAM sans surcoût
        var stagingDesc = new Texture2DDescription
        {
            Width = (uint)_width,
            Height = (uint)_height,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.B8G8R8A8_UNorm,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Staging,
            BindFlags = BindFlags.None,
            CPUAccessFlags = CpuAccessFlags.Read,
            MiscFlags = ResourceOptionFlags.None
        };

        _stagingTexture = _device.CreateTexture2D(stagingDesc);
        _isInitialized = true;
    }

    /// <summary>
    /// Capture la frame courante et exécute l'action d'analyse sur le pointeur mémoire sous-jacent.
    /// Retourne true si une frame a été capturée avec succès.
    /// </summary>
    public unsafe bool CaptureFrame(Action<nint, int, int, int> processPointerAction, int timeoutMs = 20)
    {
        if (!_isInitialized || _deskDupl == null || _context == null || _stagingTexture == null)
        {
            Initialize();
        }

        var result = _deskDupl!.AcquireNextFrame((uint)timeoutMs, out OutduplFrameInfo frameInfo, out IDXGIResource? desktopResource);

        if (result.Code == Vortice.DXGI.ResultCode.WaitTimeout.Code)
        {
            return false; // Pas de changement d'écran dans le délai imparti
        }

        if (result.Failure)
        {
            // Re-initialisation si la session graphique a changé (résolution, alt-tab plein écran)
            _isInitialized = false;
            return false;
        }

        using (desktopResource)
        {
            using var desktopTexture = desktopResource?.QueryInterface<ID3D11Texture2D>();
            if (desktopTexture != null && _stagingTexture != null && _context != null)
            {
                // Copie GPU -> GPU (de la texture d'écran vers la texture staging accessible CPU)
                _context.CopyResource(_stagingTexture, desktopTexture);

                // Verrouillage de la mémoire système
                MappedSubresource mapped = _context.Map(_stagingTexture, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);

                try
                {
                    // Action sur la mémoire : Ptr, Width, Height, RowPitch (stride)
                    processPointerAction(mapped.DataPointer, _width, _height, (int)mapped.RowPitch);
                }
                finally
                {
                    _context.Unmap(_stagingTexture, 0);
                }
            }
        }

        _deskDupl.ReleaseFrame();
        return true;
    }

    public void Dispose()
    {
        _isInitialized = false;
        _stagingTexture?.Dispose();
        _stagingTexture = null;

        _deskDupl?.Dispose();
        _deskDupl = null;

        _context?.Dispose();
        _context = null;

        _device?.Dispose();
        _device = null;
    }
}
