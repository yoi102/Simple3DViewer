using ODA.Kernel.TD_RootIntegrated;
using ODA.Visualize.TV_Visualize;
using Simple3DViewer.Shared.Extensions;
using Simple3DViewer.Shared.Scopes;

namespace Simple3DViewer.Winform.Controls.OdaVisualize.Draggers;

internal class OdTvSelectDragger : OdTvDragger
{
    public delegate void ObjectSelectedHandler(OdTvSelectionSet sSet, OdTvModelId modelId);

    public event ObjectSelectedHandler? ObjectSelected;

    public event ObjectSelectedHandler? ObjectsSelected;

    private enum SelectState
    {
        kPoint = 0,
        kWindow = 1,
        kCrossing = 2
    };

    public const string TvSelectorView = "$ODA_WPF_TVVIEWER_SELECTORVIEW";
    public const string TvSelectorLinetype = "$ODA_WPF_TVVIEWER_SELECTORLINETYPE";

    // model for selection
    private readonly OdTvModelId _modelId;

    // dragger state
    private SelectState _state = SelectState.kPoint;

    // first cliked point (Device CS)
    private readonly OdTvDCPoint _firstDevicePt = new();

    // temporary view for draggers geometry
    private readonly OdTvGsViewId _tempViewId;

    // specific linetype for the selector
    private readonly OdTvLinetypeId _frameLinetypeId;

    // temporary geometry
    private OdTvEntityId? _entityId;

    private OdTvGeometryDataId? _frameId;
    private OdTvGeometryDataId? _frameIdContourId;

    private readonly OdTvSelectionOptions _opt = new();
    private readonly OdTvDCPoint[] _pts = new OdTvDCPoint[2];

    private OdTvGsView? _view = null;

    public OdTvSelectDragger(OdaVisualizeControl viewControl, OdTvModelId modelId, OdTvGsDeviceId tvDeviceId, OdTvModelId tvDraggersModelId)
        : base(tvDeviceId, tvDraggersModelId, viewControl)
    {
        using MemoryManagerScope _ = new();

        _pts[0] = new OdTvDCPoint();
        _pts[1] = new OdTvDCPoint();

        _modelId = modelId;
        // In this dragger we will be used a separate special view for drawing temporary objects like selection rectangles
        // Such technique will allow doesn't depend on the render mode at the current active view. Also we will not have any problems
        // with linetype scale for our temporary objects
        OdTvGsDevice dev = tvDeviceId.openObject(OdTv_OpenMode.kForWrite);
        _tempViewId = dev.createView(TvSelectorView, false);

        // Get specific linetype for the selection rectangle boundary
        OdTvDatabaseId dbId = _modelId.openObject().getDatabase();
        _frameLinetypeId = dbId.openObject().findLinetype(TvSelectorLinetype);
        if (_frameLinetypeId.isNull())
        {
            // Create custom linetype for the selection rectangle boundary
            OdTvLinetypeElement dash = OdTvLinetypeDashElement.createObject(0.25);
            OdTvLinetypeElementPtr ltDash = new(dash, OdRxObjMod.kOdRxObjAttach);
            OdTvLinetypeElement space = OdTvLinetypeSpaceElement.createObject(0.25);
            OdTvLinetypeElementPtr ltSpace = new(space, OdRxObjMod.kOdRxObjAttach);
            OdTvLinetypeElementArray ltArr = new() { ltDash, ltSpace };

            _frameLinetypeId = dbId.openObject(OdTv_OpenMode.kForWrite).createLinetype(TvSelectorLinetype, ltArr);

            GC.SuppressFinalize(ltDash);
            GC.SuppressFinalize(ltSpace);
        }
    }

    ~OdTvSelectDragger()
    {
        _view?.Dispose();
    }

    public void SetSelectionLevel(OdTvSelectionOptions_Level level)
    {
        _opt.setLevel(level);
    }

    public override DraggerResult NextPoint(int x, int y)
    {
        DraggerResult rc = DraggerResult.NothingToDo;
        //first of all we need the active view to perform selection
        OdTvGsViewId? viewId = _viewControl.GetActiveTvViewId();
        if (viewId.IsNull())
            return rc;

        OdTvGsView pView = viewId.openObject();
        if (pView == null)
            return rc;
        _view ??= pView;

        //perform selection
        if (_state == SelectState.kPoint)
        {
            //remember first click
            _firstDevicePt.x = x;
            _firstDevicePt.y = y;
            _pts[0] = new OdTvDCPoint(_firstDevicePt.x, _firstDevicePt.y);
            _pts[1] = new OdTvDCPoint(_firstDevicePt.x, _firstDevicePt.y);
            //update base color
            UpdateBaseColor();
            _opt.setMode(OdTvSelectionOptions_Mode.kPoint);
        }
        else
        {
            //remember second click
            _pts[1] = new OdTvDCPoint(x, y);

            if (_state == SelectState.kWindow)
                _opt.setMode(OdTvSelectionOptions_Mode.kWindow);
            else
                _opt.setMode(OdTvSelectionOptions_Mode.kCrossing);

            // setup temporary view and model
            DisableTemporaryObjects();

            //mark that we need to update the view
            rc = DraggerResult.NeedUpdateView;
        }

        // prepare data for the selection call
        OdTvSelectionSet pSSet = _view.select(_pts, _opt, _modelId);

        //check selection results
        if (pSSet != null && pSSet.numItems() == 0 && _state == SelectState.kPoint) // start window or crossing mode
        {
            _state = SelectState.kWindow;

            // setup temporary view and model
            EnableTemporaryObjects();

            //mark that we want to receive drag without pressed mouse button
            NeedFreeDrag = true;
        }
        else if (_state != SelectState.kPoint)
            _state = SelectState.kPoint;

        if (pSSet != null && pSSet.numItems() != 0)
        {
            // merge new selection set with current
            Merge(pSSet);

            //call window update
            rc = DraggerResult.NeedUpdateView;
        }

        return rc;
    }

    public override DraggerResult Drag(int x, int y)
    {
        if (_state == SelectState.kPoint)
            return DraggerResult.NothingToDo;

        //filter coordinates
        if ((x >= _viewControl.Width || x < 1) || (y >= _viewControl.Height || y < 1))
            return DraggerResult.NothingToDo;
        using MemoryManagerScope _ = new();
        // create temporary geometry if need
        OdTvEntity? entity = null;
        if (!_entityId.IsNull())
            entity = _entityId.openObject(OdTv_OpenMode.kForWrite);
        UpdateFrame(entity == null, x, y);

        return DraggerResult.NeedUpdateView;
    }

    public override DraggerResult ProcessEscape()
    {
        if (_state != SelectState.kPoint)
            return DraggerResult.NothingToDo;

        _viewControl.ClearSelectionSet();

        _pts[0] = new OdTvDCPoint();
        _pts[1] = new OdTvDCPoint();

        return DraggerResult.NeedUpdateView;
    }

    public override bool UpdateCursor()
    {
        CurrentCursor = State == DraggerState.Finishing ? LasAppActiveCursor : Cursors.Arrow;
        return true;
    }

    private void EnableTemporaryObjects()
    {
        using MemoryManagerScope _ = new();
        //get device
        OdTvGsDevice dev = TvDeviceId.openObject(OdTv_OpenMode.kForWrite);
        if (dev is null)
            return;

        //add temporary view
        dev.addView(_tempViewId);

        //get view ptr
        OdTvGsView? view = _tempViewId.openObject(OdTv_OpenMode.kForWrite);
        if (view == null)
            return;

        //setup view to make it contr directional with the WCS normal
        view.setView(new OdGePoint3d(0, 0, 1), new OdGePoint3d(0, 0, 0), new OdGeVector3d(0, 1, 0), 1, 1);

        //add draggers model to view
        view.addModel(TvDraggerModelId);
    }

    private void DisableTemporaryObjects()
    {
        using MemoryManagerScope _ = new();
        //remove view from the device
        OdTvGsDevice dev = TvDeviceId.openObject(OdTv_OpenMode.kForWrite);
        dev.removeView(_tempViewId);
        //erase draggers model fromview
        _tempViewId.openObject(OdTv_OpenMode.kForWrite).eraseModel(TvDraggerModelId);
        //remove entities from the temporary model
        TvDraggerModelId.openObject(OdTv_OpenMode.kForWrite).clearEntities();
        dev.update();
    }

    private void UpdateFrame(bool bCreate, int x, int y)
    {
        using MemoryManagerScope _ = new();

        OdGePoint3d[] pts = new OdGePoint3d[5];

        pts[0] = ToEyeToWorldLocal(_firstDevicePt.x, _firstDevicePt.y);
        pts[2] = ToEyeToWorldLocal(x, y);
        pts[4] = new OdGePoint3d(pts[0]);

        OdTvGsView pLocalView = _tempViewId.openObject();
        if (pLocalView == null)
        {
            return;
        }

        OdGeMatrix3d matr = pLocalView.viewingMatrix();
        OdGePoint3d p0 = matr * pts[0];
        OdGePoint3d p2 = matr * pts[2];

        pts[1] = new OdGePoint3d(p0.x, p2.y, p2.z);
        pts[3] = new OdGePoint3d(p2.x, p0.y, p2.z);

        bool bCrossing = p0.x > p2.x;

        _state = SelectState.kWindow;
        if (bCrossing)
            _state = SelectState.kCrossing;

        matr = pLocalView.eyeToWorldMatrix();
        pts[1].transformBy(matr);
        pts[3].transformBy(matr);

        //update or create entity
        if (bCreate)
        {
            OdTvModel model = TvDraggerModelId.openObject(OdTv_OpenMode.kForWrite);
            _entityId = model.appendEntity();
            {
                OdTvEntity entityNew = _entityId.openObject(OdTv_OpenMode.kForWrite);

                //create frame
                _frameId = entityNew.appendPolygon(new[] { pts[0], pts[1], pts[2], pts[3] });
                _frameId.openObject().setTransparency(new OdTvTransparencyDef(0.7));
                _frameId.openAsPolygon().setFilled(true);

                _frameIdContourId = entityNew.appendPolyline(pts);
                _frameIdContourId.openObject().setColor(TvDraggerColor);

                if (bCrossing)
                {
                    entityNew.setColor(new OdTvColorDef(0, 255, 0));
                    entityNew.setLinetypeScale(0.02);
                    _frameIdContourId.openObject().setLinetype(new OdTvLinetypeDef(_frameLinetypeId));
                }
                else
                    entityNew.setColor(new OdTvColorDef(0, 0, 255));
            }
        }
        else
        {
            if (_frameId is null || _frameIdContourId is null || _entityId is null)
                return;
            OdTvGeometryData? frame = _frameId?.openObject();
            if (frame is null || frame.getType() != OdTv_OdTvGeometryDataType.kPolygon)
            {
                return;
            }

            OdTvPolygonData polygon = frame.getAsPolygon();
            polygon.setPoints(new[] { pts[0], pts[1], pts[2], pts[3] });

            _frameIdContourId.openAsPolyline().setPoints(pts);

            if (bCrossing)
            {
                _entityId.openObject(OdTv_OpenMode.kForWrite).setColor(new OdTvColorDef(0, 255, 0));
                _entityId.openObject(OdTv_OpenMode.kForWrite).setLinetypeScale(0.03);
                _frameIdContourId.openObject().setLinetype(new OdTvLinetypeDef(_frameLinetypeId));
            }
            else
            {
                _entityId.openObject(OdTv_OpenMode.kForWrite).setColor(new OdTvColorDef(0, 0, 255));
                _frameIdContourId.openObject().setLinetype(new OdTvLinetypeDef());
            }
        }
    }

    private OdGePoint3d ToEyeToWorldLocal(int x, int y)
    {
        using MemoryManagerScope _ = new();
        OdGePoint3d wcsPt = new(x, y, 0);
        OdTvGsView view = _tempViewId.openObject();

        if (view.isPerspective())
            wcsPt.z = view.projectionMatrix().GetItem(2, 3);
        wcsPt.transformBy((view.screenMatrix() * view.projectionMatrix()).inverse());
        wcsPt.z = 0;
        //transform to world coordinate system
        wcsPt.transformBy(view.eyeToWorldMatrix());
        return wcsPt;
    }

    private void Merge(OdTvSelectionSet sSet)
    {
        if (sSet == null)
            return;

        // always clear previous selection set
        if (_viewControl.SelectionSet != null)
        {
            Highlight(_viewControl.SelectionSet, false);
            _viewControl.SelectionSet.Dispose();
            _viewControl.SelectionSet = null;
        }

        if (_opt.getLevel() != OdTvSelectionOptions_Level.kEntity)
        {
            _viewControl.SelectionSet = sSet;
            Highlight(_viewControl.SelectionSet, true);
            return;
        }

        if (ObjectSelected != null)
        {
            if (sSet.getOptions().getMode() == OdTvSelectionOptions_Mode.kPoint)
                ObjectSelected?.Invoke(sSet, _modelId);
            else
                ObjectsSelected?.Invoke(sSet, _modelId);
        }

        if (sSet.getOptions().getMode() == OdTvSelectionOptions_Mode.kWindow ||
            sSet.getOptions().getMode() == OdTvSelectionOptions_Mode.kCrossing)
        {
            if (_viewControl.SelectionSet == null)
            {
                _viewControl.SelectionSet = sSet;
                Highlight(_viewControl.SelectionSet, true);
                return;
            }
        }
    }

    private void Highlight(OdTvSelectionSet sSet, bool bDoIt)
    {
        if (sSet == null)
            return;
        OdTvSelectionSetIterator pIter = sSet.getIterator();
        for (; pIter != null && !pIter.done(); pIter.step())
            Highlight(pIter, bDoIt);

        if (ObjectSelected != null)
        {
            if (sSet.getOptions().getMode() == OdTvSelectionOptions_Mode.kPoint)
                ObjectSelected?.Invoke(sSet, _modelId);
            else
                ObjectsSelected?.Invoke(sSet, _modelId);
        }
    }

    private void Highlight(OdTvSelectionSetIterator pIter, bool bDoIt)
    {
        OdTvGsViewId? viewId = _viewControl.GetActiveTvViewId();
        if (viewId.IsNull())
            return;

        OdTvGsView view = viewId.openObject();
        if (view == null)
            return;

        //get entity
        OdTvEntityId id = pIter.getEntity();
        //get sub item
        OdTvSubItemPath path = new();
        pIter.getPath(path);
        //perform highlight
        view.highlight(id, path, bDoIt);
    }
}