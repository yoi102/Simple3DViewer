using ODA.Kernel.TD_RootIntegrated;
using ODA.Visualize.TV_Visualize;
using ODA.Visualize.TV_VisualizeTools;
using Simple3DViewer.Shared.Extensions;
using Simple3DViewer.Shared.Scopes;
using Simple3DViewer.Winform.Controls.OdaVisualize.Draggers;
using System.ComponentModel;
using System.Diagnostics;
using OdTvDragger = Simple3DViewer.Winform.Controls.OdaVisualize.Draggers.OdTvDragger;

namespace Simple3DViewer.Winform.Controls.OdaVisualize;

public enum DraggerType
{
    None,
    Orbit,
    Pan,
    Select
}

[Flags]
public enum RenderMode
{
    k2DOptimized = 0,
    kWireframe = 1,
    kHiddenLine = 2,
    kFlatShaded = 3,
    kGouraudShaded = 4,
    kFlatShadedWithWireframe = 5,
    kGouraudShadedWithWireframe = 6,
    kNone = 7
}

public class OdaVisualizeControl : Control
{
    public OdTvSelectionSet? SelectionSet = null;
    private readonly Dictionary<ulong, OdTvExtendedView> _extendedViewDict = [];
    private OdTvAnimation? _animation = null;
    private OdTvDragger? _dragger = null;
    private DraggerType _leftButtonDragger = DraggerType.Select;
    private DraggerType _middleButtonDragger = DraggerType.Pan;
    private RenderMode _renderMode = RenderMode.kNone;
    private DraggerType _rightButtonDragger = DraggerType.Orbit;
    private bool _showFPS = true;
    private bool _showViewCube = true;
    private bool _showWCS = true;

    private OdTvModelId? _tvDraggersModelId = null;

    private bool _useAnimation = true;

    private double _zoomStep = 2;

    private OdaVisualizeContext? odaVisualizeContext;

    public OdaVisualizeControl()
    {
        this.Paint += PaintEvent;
        this.Resize += ResizePanel;
        this.MouseWheel += MouseWheelEvent;
        this.MouseDown += MouseDownEvent;
        this.MouseUp += MouseUpEvent;
        this.MouseMove += MouseMoveEvent;
        this.KeyPress += KeyPressEvent;
    }

    public event EventHandler? RenderModeChanged;

    public OdTvModelId? _tvActiveModelId { get; private set; }
    public OdTvRegAppId? AppTvId { get; private set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DraggerType LeftButtonDragger
    {
        get { return _leftButtonDragger; }
        set { _leftButtonDragger = value; }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DraggerType MiddleButtonDragger
    {
        get { return _middleButtonDragger; }
        set { _middleButtonDragger = value; }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public OdaVisualizeContext? OdaVisualizeContext
    {
        get { return odaVisualizeContext; }
        set
        {
            ClearDevices();
            odaVisualizeContext = value;
            InitVisualizeDevice();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RenderMode RenderMode
    {
        get { return _renderMode; }
        set
        {
            if (_renderMode == value)
                return;
            _renderMode = value;
            SetRenderMode((OdTvGsView_RenderMode)(int)value);
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DraggerType RightButtonDragger
    {
        get { return _rightButtonDragger; }
        set { _rightButtonDragger = value; }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowFPS
    {
        get { return _showFPS; }
        set
        {
            _showFPS = value;
            OnOffFPS(value);
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowViewCube
    {
        get { return _showViewCube; }
        set
        {
            _showViewCube = value;
            OnOffViewCube(value);
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowWCS
    {
        get { return _showWCS; }
        set
        {
            _showWCS = value;
            OnOffWCS(value);
        }
    }

    public OdTvGsDeviceId? TvDeviceId { get; private set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool UseAnimation
    {
        get { return _useAnimation; }
        set
        {
            _useAnimation = value;
            OnOffAnimation(value);
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double ZoomStep
    {
        get { return _zoomStep; }
        set
        {
            _zoomStep = value;
            SetZoomStep(value);
        }
    }

    public void ClearDevices()
    {
        using MemoryManagerScope _ = new();

        foreach (KeyValuePair<ulong, OdTvExtendedView> extView in _extendedViewDict)
        {
            extView.Value.Dispose();
        }

        _extendedViewDict.Clear();
        if (odaVisualizeContext?.TvDatabaseId is not null)
        {
            OdTvFactoryId factId = TV_Visualize_Globals.odTvGetFactory();
            factId.removeDatabase(odaVisualizeContext.TvDatabaseId);
        }
        odaVisualizeContext = null;
        TvDeviceId = null;
        _tvDraggersModelId = null;
        _dragger = null;
    }

    public OdTvExtendedView? GetActiveTvExtendedView()
    {
        if (TvDeviceId.IsNull())
            return null;

        using MemoryManagerScope _ = new();
        OdTvExtendedView? exView = null;

        OdTvGsViewId viewId = TvDeviceId.openObject().getActiveView();
        if (viewId.IsNull())
            return null;
        OdTvResult rc = OdTvResult.tvOk;
        OdTvGsView view = viewId.openObject();
        ulong handle = view.getDatabaseHandle(ref rc);

        if (_extendedViewDict.ContainsKey(handle))
            exView = _extendedViewDict[handle];
        else
        {
            exView = new OdTvExtendedView(viewId);
            exView.setAnimationEnabled(_useAnimation);
            exView.setZoomScale(_zoomStep);
            exView.setAnimationDuration(0.9);

            if (view != null)
            {
                OdGeBoundBlock3d lastExt = new();
                if (view.getLastViewExtents(lastExt))
                    exView.setViewExtentsForCaching(lastExt);
            }

            _extendedViewDict.Add(handle, exView);
        }

        return exView;
    }

    public OdGeExtents3d GetMainModelExtents()
    {
        OdGeExtents3d extents = new();

        if (_tvActiveModelId.IsNull())
            return extents;

        using MemoryManagerScope _ = new();

        OdTvModel pModel = _tvActiveModelId.openObject();

        // Get extents
        OdTvResult res = pModel.getExtents(extents);
        if (res != OdTvResult.tvOk)
            return extents;

        return extents;
    }

    public void SetAnimation(OdTvAnimation animation)
    {
        _animation = animation;
        if (_animation != null)
            _animation.start();

        if (_dragger != null)
            _dragger.NotifyAboutViewChange(DraggerViewChangeType.ViewChangeFull);
    }

    internal void ClearSelectionSet()
    {
        if (SelectionSet == null || TvDeviceId.IsNull())
            return;
        OdTvExtendedView? exView = GetActiveTvExtendedView();
        if (exView == null)
            return;

        using MemoryManagerScope _ = new();

        OdTvGsView view = exView.getViewId().openObject();
        if (view != null)
        {
            OdTvSelectionSetIterator pIter = SelectionSet.getIterator();
            for (; pIter != null && !pIter.done(); pIter.step())
            {
                //get entity
                OdTvEntityId id = pIter.getEntity();
                //get sub item
                OdTvSubItemPath path = new();
                pIter.getPath(path);
                //perform highlight
                view.highlight(id, path, false);
            }

            SelectionSet.Dispose();
            SelectionSet = null;

            Invalidate();
        }
    }

    internal void FinishDragger()
    {
        if (_dragger is null)
            return;

        if (_dragger.HasPrevious() && _dragger.CanFinish())
        {
            // release current dragger
            OdTvDragger? prevDragger = _dragger.Finish(out DraggerResult res);
            ActionAferDragger(res);

            // activate previous dragger
            _dragger = prevDragger;
            if (_dragger is null)
                return;
            res = _dragger.Start(null, Cursor);
            ActionAferDragger(res);
        }
    }

    internal OdTvGsViewId? GetActiveTvViewId()
    {
        using MemoryManagerScope _ = new();
        OdTvGsViewId? viewId = TvDeviceId?.openObject()?.getActiveView();
        return viewId;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ClearDevices();
        }
        base.Dispose(disposing);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
    }

    private void ActionAferDragger(DraggerResult res)
    {
        if (_dragger is null)
            return;
        if ((res & DraggerResult.NeedUpdateCursor) != 0)
            this.Cursor = _dragger.CurrentCursor;

        if ((res & DraggerResult.NeedUpdateView) != 0)
        {
            using MemoryManagerScope _ = new();
            OdTvGsDevice? pDevice = TvDeviceId?.openObject();
            pDevice?.update();
        }

        if ((res & DraggerResult.NeedUFinishDragger) != 0)
            FinishDragger();
    }

    private OdTvGsDeviceId? CreateNewDevice()
    {
        if (odaVisualizeContext == null)
            return null;
        if (odaVisualizeContext.TvDatabaseId is null)
            return null;

        using MemoryManagerScope _ = new();
        OdTvGsDeviceId? newDevId = null;

        IntPtr wndHndl = new(Handle.ToInt32());
        OdTvDCRect rect = new(0, this.Width, this.Height, 0);
        newDevId = odaVisualizeContext.TvDatabaseId.openObject().createDevice("TV_Device", wndHndl, rect, OdTvGsDevice_Name.kOpenGLES2);
        // Open device
        OdTvGsDevice pDevice = newDevId.openObject(OdTv_OpenMode.kForWrite);
        if (pDevice == null)
            return null;

        //                 bool val;
        //                 pDevice.getOption(OdTvGsDevice_Options.kUseVisualStyles, out val);

        // Create view
        OdTvGsViewId newViewId = pDevice.createView("TV_View");

        // Add view to device
        pDevice.addView(newViewId);

        // Add current model to the view
        OdTvGsView viewPtr = newViewId.openObject(OdTv_OpenMode.kForWrite);

        // Setup view to make it contr directional with the WCS normal
        viewPtr.setView(new OdGePoint3d(0, 0, 1), new OdGePoint3d(0, 0, 0), new OdGeVector3d(0, 1, 0), 1, 1);

        // Add main model to the view
        viewPtr.addModel(_tvActiveModelId);

        // Set current view as active
        viewPtr.setActive(true);

        // Set the render mode
        viewPtr.setMode(OdTvGsView_RenderMode.k2DOptimized);

        newDevId.openObject().onSize(rect);

        return newDevId;
    }

    private OdTvDragger? GetOdTvDragger(DraggerType draggerType)
    {
        if (TvDeviceId.IsNull() || _tvDraggersModelId.IsNull() || _tvActiveModelId.IsNull())
            return null;
        switch (draggerType)
        {
            case DraggerType.Orbit:

                return new Draggers.OdTvOrbitDragger(TvDeviceId, _tvDraggersModelId, this);

            case DraggerType.Pan:

                return new Draggers.OdTvPanDragger(TvDeviceId, _tvDraggersModelId, this);

            case DraggerType.Select:

                return new Draggers.OdTvSelectDragger(this, _tvActiveModelId, TvDeviceId, _tvDraggersModelId);

            default:
                return null;
        }
    }

    private OdTvDragger? GetOrCreateOdTvDragger(OdTvDragger? dragger, DraggerType draggerType)
    {
        if (IsDraggerOfType(dragger, draggerType))
            return dragger;
        return GetOdTvDragger(draggerType);
    }

    private void Init()
    {
        if (odaVisualizeContext == null)
            return;

        if (odaVisualizeContext.TvDatabaseId == null || TvDeviceId.IsNull())
            return;
        using MemoryManagerScope _ = new();

        OdTvDatabase pDb = odaVisualizeContext.TvDatabaseId.openObject(OdTv_OpenMode.kForWrite);
        _tvDraggersModelId = pDb.createModel("Draggers", OdTvModel_Type.kDirect, false);

        AppTvId = odaVisualizeContext.TvDatabaseId.openObject().registerAppName("Visualize Viewer", out bool exist);

        OdTvGsDevice tvDevice = TvDeviceId.openObject();

        OdTvGsView view = tvDevice.getActiveView().openObject();
        // Anti-Aliasing
        tvDevice.setLineSmoothing(true);
        tvDevice.setOption(OdTvGsDevice_Options.kAntiAliasLevel, 1d);
        tvDevice.setOption(OdTvGsDevice_Options.kAntiAliasLevelExt, 0.25d);
        tvDevice.setOption(OdTvGsDevice_Options.kFXAAEnable, true);
        var renderMode = (RenderMode)(int)view.mode();
        if (_renderMode != renderMode)
        {
            _renderMode = renderMode;
            RenderModeChanged?.Invoke(this, EventArgs.Empty);
        }

        // enable or disable wcs, fps and grid
        OnOffViewCube(ShowViewCube);
        OnOffFPS(ShowFPS);
        OnOffWCS(ShowWCS);
        OnOffAnimation(UseAnimation);
    }

    private void InitVisualizeDevice()
    {
        if (odaVisualizeContext == null)
            return;
        if (!odaVisualizeContext.IsInitialized)
            return;

        this.Cursor = Cursors.WaitCursor;
        using MemoryManagerScope _ = new();
        using DisposableAction __ = new(() => this.Cursor = Cursors.Default);

        OdTvDatabase? pDb = odaVisualizeContext.TvDatabaseId.openObject(OdTv_OpenMode.kForWrite);
        _tvActiveModelId = pDb?.getModelsIterator().getModel();
        if (_tvActiveModelId is null)
        {
            Debugger.Break();
            return;
        }

        OdTvDevicesIterator? devIt = pDb?.getDevicesIterator();
        if (devIt != null && !devIt.done())
        {
            TvDeviceId = devIt.getDevice();
            OdTvGsDevice dev = TvDeviceId.openObject(OdTv_OpenMode.kForWrite);
            IntPtr wndHndl = new(this.Handle.ToInt32());
            OdTvDCRect rect = new(0, this.Width, this.Height, 0);
            dev.setupGs(wndHndl, rect, OdTvGsDevice_Name.kOpenGLES2);

            dev.onSize(rect);
            Stopwatch timer = Stopwatch.StartNew();
            timer.Start();
            dev.update();
            timer.Stop();
            odaVisualizeContext.DatabaseInfo.FirstUpdateTime = timer.ElapsedMilliseconds;
        }
        else if (devIt != null && devIt.done())
        {
            TvDeviceId = CreateNewDevice();
            if (TvDeviceId.IsNull())
            {
                return;
            }
            Stopwatch timer = Stopwatch.StartNew();
            timer.Start();
            TvDeviceId.openObject().update();
            timer.Stop();
            odaVisualizeContext.DatabaseInfo.FirstUpdateTime = timer.ElapsedMilliseconds;
        }
        Init();
    }

    private bool IsDraggerOfType(OdTvDragger? dragger, DraggerType draggerType)
    {
        switch (draggerType)
        {
            case DraggerType.Orbit:
                return dragger is OdTvOrbitDragger;

            case DraggerType.Pan:
                return dragger is OdTvPanDragger;

            case DraggerType.Select:
                return dragger is OdTvSelectDragger;

            default:
                return false;
        }
    }

    private void KeyPressEvent(object? sender, KeyPressEventArgs e)
    {
        if ((int)e.KeyChar == (int)Keys.Escape)
            return;
        ClearSelectionSet();
    }

    private void MouseDownEvent(object? sender, MouseEventArgs e)
    {
        OdTvExtendedView? extView = GetActiveTvExtendedView();
        if (extView == null)
            return;

        if (e.Button == MouseButtons.Left && extView != null && extView.getEnabledViewCube())
        {
            if (extView.viewCubeProcessClick(e.X, e.Y))
            {
                if (extView.getAnimationEnabled())
                {
                    SetAnimation(extView.getAnimation());
                    Invalidate();
                }
                return;
            }
        }

        if (e.Button == MouseButtons.Left)
        {
            OdTvDragger? newDragger = GetOrCreateOdTvDragger(_dragger, LeftButtonDragger);
            _dragger = newDragger;
            if (newDragger is not null)
                StartDragger(newDragger, true);
        }
        else if (e.Button == MouseButtons.Middle)
        {
            OdTvDragger? newDragger = GetOrCreateOdTvDragger(_dragger, MiddleButtonDragger);
            _dragger = newDragger;
            if (newDragger is not null)
                StartDragger(newDragger, true);
        }
        else if (e.Button == MouseButtons.Right)
        {
            OdTvDragger? newDragger = GetOrCreateOdTvDragger(_dragger, RightButtonDragger);
            _dragger = newDragger;
            if (newDragger is not null)
                StartDragger(newDragger, true);
        }

        if (_dragger == null) return;
        // activation first
        DraggerResult res = _dragger.Activate();
        ActionAferDragger(res);
        res = _dragger.NextPoint(e.X, e.Y);
        ActionAferDragger(res);
    }

    private void MouseMoveEvent(object? sender, MouseEventArgs e)
    {
        var extView = GetActiveTvExtendedView();
        if (extView is not null && extView.getEnabledViewCube())
        {
            extView.viewCubeProcessHover(e.X, e.Y);
        }

        if (_dragger is not null)
        {
            DraggerResult res = _dragger.Drag(e.X, e.Y);
            ActionAferDragger(res);
        }
    }

    private void MouseUpEvent(object? sender, MouseEventArgs e)
    {
        if (_dragger is null)
            return;
        DraggerResult res = _dragger.NextPointUp(e.X, e.Y);
        ActionAferDragger(res);
        this.Cursor = Cursors.Default;
    }

    private void MouseWheelEvent(object? sender, MouseEventArgs e)
    {
        if (odaVisualizeContext is null)
            return;
        if (odaVisualizeContext.TvDatabaseId is null || TvDeviceId is null)
            return;

        using MemoryManagerScope _ = new();

        FinishDragger();

        OdTvGsDevice dev = TvDeviceId.openObject(OdTv_OpenMode.kForWrite);
        OdTvGsViewId viewId = dev.getActiveView();
        OdTvGsView pView = viewId.openObject(OdTv_OpenMode.kForWrite);
        if (pView == null)
            return;

        OdGePoint2d point = new(e.X, e.Y);

        OdGePoint3d pos = new(pView.position());
        pos.transformBy(pView.worldToDeviceMatrix());

        int vx, vy;
        vx = (int)pos.x;
        vy = (int)pos.y;

        vx = (int)point.x - vx;
        vy = (int)point.y - vy;

        double scale = 0.9; // wheel down
        if (e.Delta > 0)
            scale = 1.0 / scale; // wheel up

        ScreenDolly(vx, vy);
        pView.zoom(scale);
        ScreenDolly(-vx, -vy);

        dev.update();

        if (_dragger != null)
            _dragger.NotifyAboutViewChange(DraggerViewChangeType.ViewChangeZoom);

        GetActiveTvExtendedView()?.setViewType(OdTvExtendedView_e3DViewType.kCustom);
    }

    private void PaintEvent(object? sender, PaintEventArgs e)
    {
        if (this.Disposing || TvDeviceId.IsNull())
            return;
        using MemoryManagerScope _ = new();

        OdTvGsDevice pDevice = TvDeviceId.openObject();
        pDevice.update();

        if (_animation != null && _animation.isRunning())
        {
            _animation.step();
            Invalidate();

            if (_animation.isRunning())
                return;

            _animation.Dispose();
            _animation = null;
            _dragger?.NotifyAboutViewChange(DraggerViewChangeType.ViewChangeFull);
        }
    }

    private void ResizePanel(object? sender, EventArgs e)
    {
        if (this.Width <= 0 || this.Height <= 0)
            return;

        if (TvDeviceId.IsNull() || this.Disposing)
            return;

        using MemoryManagerScope _ = new();
        OdTvGsDevice dev = TvDeviceId.openObject(OdTv_OpenMode.kForWrite);
        dev.onSize(new OdTvDCRect(0, this.Width, this.Height, 0));
        dev.update();
    }

    private void ScreenDolly(int x, int y)
    {
        using MemoryManagerScope _ = new();
        OdTvGsViewId? viewId = TvDeviceId?.openObject()?.getActiveView();
        OdTvGsView? pView = viewId?.openObject();
        if (pView == null)
            return;

        OdGeVector3d vec = new(x, y, 0);
        vec.transformBy((pView.screenMatrix() * pView.projectionMatrix()).inverse());
        pView.dolly(vec);
    }

    #region Appearance commands

    private void OnOffAnimation(bool bEnable)
    {
        if (TvDeviceId.IsNull())
            return;
        using MemoryManagerScope _ = new();
        OdTvExtendedView? exView = GetActiveTvExtendedView();
        exView?.setAnimationEnabled(bEnable);
    }

    private void OnOffFPS(bool bEnable)
    {
        if (TvDeviceId.IsNull())
            return;
        using MemoryManagerScope _ = new();
        OdTvGsDevice dev = TvDeviceId.openObject(OdTv_OpenMode.kForWrite);
        if (dev.getShowFPS() != bEnable)
        {
            dev.setShowFPS(bEnable);
            dev.update();
            Invalidate();
        }
    }

    private void OnOffViewCube(bool bEnable)
    {
        if (TvDeviceId.IsNull())
            return;
        using MemoryManagerScope _ = new();
        OdTvExtendedView? extView = GetActiveTvExtendedView();
        if (extView != null && extView.getEnabledViewCube() != bEnable)
        {
            extView.setEnabledViewCube(bEnable);
            Invalidate();
        }
    }

    private void OnOffWCS(bool bEnable)
    {
        if (TvDeviceId.IsNull())
            return;
        using MemoryManagerScope _ = new();
        OdTvExtendedView? extView = GetActiveTvExtendedView();
        if (extView != null && extView.getEnabledWCS() != bEnable)
        {
            extView.setEnabledWCS(bEnable);
            Invalidate();
        }
    }

    private void SetRenderMode(OdTvGsView_RenderMode renderMode)
    {
        if (TvDeviceId.IsNull())
            return;
        using MemoryManagerScope _ = new();

        OdTvExtendedView? exView = GetActiveTvExtendedView();

        if (exView == null)
            return;
        OdTvGsView_RenderMode oldMode = exView.getRenderMode();
        if (oldMode != renderMode)
        {
            exView.setRenderMode(renderMode);
            TvDeviceId.openObject().update();
        }
    }

    private void SetZoomStep(double dValue)
    {
        if (TvDeviceId.IsNull() || dValue < 1)
            return;
        OdTvExtendedView? exView = GetActiveTvExtendedView();
        exView?.setZoomScale(dValue);
    }

    #endregion Appearance commands

    private void StartDragger(OdTvDragger dragger, bool useCurrentAsPrevious = false)
    {
        DraggerResult res = DraggerResult.NothingToDo;

        if (_dragger == null)
            res = dragger.Start(null, Cursor);
        else
        {
            OdTvDragger? pPrevDragger = _dragger;
            if (_dragger.HasPrevious())
            {
                DraggerResult res_prev;
                if (useCurrentAsPrevious)
                    _dragger.Finish(out res_prev);
                else
                    pPrevDragger = _dragger.Finish(out res_prev);
                ActionAferDragger(res_prev);
            }
            res = dragger.Start(prevDragger: pPrevDragger, Cursor);
        }
        // need update active dragger before calling action
        _dragger = dragger;
        ActionAferDragger(res);
    }
}