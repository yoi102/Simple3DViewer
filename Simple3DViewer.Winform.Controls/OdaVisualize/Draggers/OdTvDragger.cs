using ODA.Kernel.TD_RootIntegrated;
using ODA.Visualize.TV_Visualize;
using Simple3DViewer.Shared.Scopes;
using Simple3DViewer.Winform.Controls.OdaVisualize.Misc;

namespace Simple3DViewer.Winform.Controls.OdaVisualize.Draggers;

[Flags]
public enum DraggerResult
{
    NothingToDo = 0,
    NeedUpdateCursor = 1,
    NeedUpdateView = 2,
    NeedUFinishDragger = 4,
};

/** \details
Dragging object states.
*/

[Flags]
public enum DraggerViewChangeType
{
    ViewChangePan = 0x00,    // Notify that the view was panned
    ViewChangeZoom = 0x01,    // Notify that the view was zoomed (in/out, window, extents)
    ViewChangeRotate = 0x02,    // Notify that the view was rotated
    ViewChangeFull = 0x04     // Notify that the view parameters was changed
};

internal abstract class OdTvDragger
{
    public enum DraggerState
    {
        Waiting,
        Working,
        Finishing
    }

    // state of current dragger
    protected DraggerState State { get; set; }

    //device, associated with this dragger
    protected OdTvGsDeviceId TvDeviceId { get; private set; }

    // previous dragger
    protected OdTvDragger? PrevDragger { get; set; }

    // true if and only if the dragger have temporary drawable geometry
    protected bool HaveDrawableTemporaryGeometry { get; set; }

    // true if and only if the dragger wants to receive drag without pressed buttons
    public bool NeedFreeDrag { get; protected set; }

    // color of dragger
    protected OdTvColorDef TvDraggerColor { get; private set; }

    // dragger model id
    protected OdTvModelId TvDraggerModelId { get; private set; }

    // cursors
    public Cursor? CurrentCursor { get; set; }

    public Cursor? LasAppActiveCursor { get; set; }

    public TvWpfViewWCS? WCS { get; set; }

    public OdaVisualizeControl _viewControl;

    // Constructor
    public OdTvDragger(OdTvGsDeviceId tvDeviceId, OdTvModelId tvDraggersModelId, OdaVisualizeControl viewControl)
    {
        TvDeviceId = tvDeviceId;
        TvDraggerModelId = tvDraggersModelId;
        TvDraggerColor = new OdTvColorDef();
        _viewControl = viewControl;

        State = DraggerState.Waiting;
        HaveDrawableTemporaryGeometry = false;
        NeedFreeDrag = false;
    }

    /// <summary>
    /// Update base color in dependence of the current background
    /// </summary>
    public void UpdateBaseColor()
    {
        using MemoryTransactionScope _ = new();
        uint bgColor = TvDeviceId.openObject().getBackgroundColor();
        byte[] bytes = new[] { byte.MaxValue, (byte)(bgColor >> 16), (byte)(bgColor >> 8), (byte)(bgColor >> 0) };
        int newColor = BitConverter.ToInt32(bytes, 0);
        Color col = Color.FromArgb(newColor);
        col = col.GetHue() < 128 ? Color.Black : Color.White;
        TvDraggerColor = new OdTvColorDef(col.R, col.G, col.B);
    }

    /// <summary>
    /// Should be called to prepare dragger for the work
    /// </summary>
    public virtual DraggerResult Start(OdTvDragger? prevDragger, Cursor cursor)
    {
        if (prevDragger != null)
            PrevDragger = prevDragger;

        CurrentCursor = cursor;
        LasAppActiveCursor = cursor;

        State = DraggerState.Waiting;

        if (UpdateCursor())
            return DraggerResult.NeedUpdateCursor;

        return DraggerResult.NothingToDo;
    }

    /// <summary>
    /// Should be called to prepare dragger for the work
    /// </summary>
    public virtual DraggerResult Activate()
    {
        if (State == DraggerState.Working || State == DraggerState.Finishing)
            return DraggerResult.NothingToDo;

        State = DraggerState.Working;

        if (UpdateCursor())
            return DraggerResult.NeedUpdateCursor;

        return DraggerResult.NothingToDo;
    }

    /// <summary>
    /// Predominantly should be called on the mouse click
    /// </summary>
    public virtual DraggerResult NextPoint(int x, int y)
    {
        return DraggerResult.NothingToDo;
    }

    /// <summary>
    /// Predominantly should be called on the mouse release
    /// </summary>
    public virtual DraggerResult NextPointUp(int x, int y)
    {
        return DraggerResult.NothingToDo;
    }

    /// <summary>
    /// Predominantly should be called on mouse move
    /// </summary>
    public virtual DraggerResult Drag(int x, int y)
    {
        return DraggerResult.NothingToDo;
    }

    /// <summary>
    /// Should be called before exit the dragger. Returns the previous dragger.
    /// </summary>
    public virtual OdTvDragger? Finish(out DraggerResult rc)
    {
        rc = DraggerResult.NothingToDo;

        if (HaveDrawableTemporaryGeometry)
        {
            // remove the draggers model from the view
            RemoveDraggersModelFromView();

            //remove entities from the model
            using MemoryTransactionScope _ = new();
            OdTvModel model = TvDraggerModelId.openObject(OdTv_OpenMode.kForWrite);
            if (model != null)
                model.clearEntities();

            // need redraw after
            rc = DraggerResult.NeedUpdateView;
        }

        // update the state of the dragger
        State = DraggerState.Finishing;

        //reset the dragger
        rc = rc | Reset();

        return PrevDragger;
    }

    /// <summary>
    /// Return true if and only if the dragger can be finished
    /// </summary>
    public virtual bool CanFinish()
    {
        return State == DraggerState.Waiting ? true : false;
    }

    /// <summary>
    /// Process 'enter' pressing
    /// </summary>
    public virtual bool ProcessEnter()
    {
        return false;
    }

    /// <summary>
    /// Process 'escape' pressing
    /// </summary>
    public virtual DraggerResult ProcessEscape()
    {
        return DraggerResult.NothingToDo;
    }

    /// <summary>
    /// Process 'backspace' pressing
    /// </summary>
    public virtual DraggerResult ProcessBackspace()
    {
        return DraggerResult.NothingToDo;
    }

    /// <summary>
    /// Process any key pressing
    /// </summary>
    public virtual DraggerResult ProcessText(string text)
    {
        return DraggerResult.NothingToDo;
    }

    /// <summary>
    /// Formally creating and deleting of the temporary draggers geometry should be performed inside
    /// 'start' and 'finish' methods or in other methods.Modification of such objects should be normally performed
    /// inside 'nextpoint', 'nextpointup' and 'drag' methods.But there are some situations when the geometry should be modified at the wach
    /// view update. For such cases there is this method
    /// </summary>
    public virtual void DrawInvalidate()
    { }

    public virtual void NotifyAboutViewChange(DraggerViewChangeType type)
    { }

    /// <summary>
    /// This method updates the cursor (if it is need)
    /// </summary>
    public virtual bool UpdateCursor()
    {
        return false;
    }

    /// <summary>
    /// Returns true if and only of the current dragger has the previous one
    /// </summary>
    public bool HasPrevious()
    {
        return PrevDragger != null;
    }

    /// <summary>
    /// add draggers model to the active view
    /// </summary>
    protected void AddDraggersModelToView()
    {
        using MemoryTransactionScope _ = new();
        OdTvGsViewId? viewId = _viewControl.GetActiveTvViewId();
        if (viewId != null && !viewId.isNull())
            viewId.openObject(OdTv_OpenMode.kForWrite).addModel(TvDraggerModelId);
    }

    /// <summary>
    /// Remove the draggers model from active view
    /// </summary>
    protected void RemoveDraggersModelFromView()
    {
        using MemoryTransactionScope _ = new();
        OdTvGsViewId? viewId = _viewControl.GetActiveTvViewId();
        if (viewId != null && !viewId.isNull())
            viewId.openObject(OdTv_OpenMode.kForWrite).eraseModel(TvDraggerModelId);
    }

    /// <summary>
    /// Reset the dragger state
    /// </summary>
    protected DraggerResult Reset()
    {
        if (State == DraggerState.Working)
            State = DraggerState.Waiting;

        if (UpdateCursor())
            return DraggerResult.NeedUpdateCursor;

        return DraggerResult.NothingToDo;
    }

    /// <summary>
    /// Method for transform from eye to WCS
    /// </summary>
    protected OdGePoint3d ToEyeToWorld(int x, int y)
    {
        OdGePoint3d wcsPt = new(x, y, 0d);
        OdTvGsViewId? viewId = _viewControl.GetActiveTvViewId();
        if (viewId == null || viewId.isNull())
            return wcsPt;

        using MemoryTransactionScope _ = new();
        OdTvGsView view = viewId.openObject();

        if (view.isPerspective())
            wcsPt.z = view.projectionMatrix()[2, 3];

        wcsPt.transformBy((view.screenMatrix() * view.projectionMatrix()).inverse());
        wcsPt.z = 0d;

        // transform to WCS
        wcsPt.transformBy(view.eyeToWorldMatrix());

        return wcsPt;
    }

    protected bool ToUcsToWorld(OdGePoint3d wcsPt)
    {
        OdTvGsViewId? viewId = _viewControl.GetActiveTvViewId();
        if (viewId == null || viewId.isNull())
            return false;
        OdTvGsView pView = viewId.openObject();
        if (pView == null)
            return false;

        // define UCS plane
        OdGePlane plane = new();
        plane.set(OdGePoint3d.kOrigin, OdGeVector3d.kXAxis, OdGeVector3d.kYAxis);

        if (!pView.isPerspective())
        {
            // For orthogonal projection we simply check intersection between viewing direction and UCS plane.
            OdGeLine3d line = new(wcsPt, pView.position() - pView.target());
            pView.Dispose();
            return plane.intersectWith(line, wcsPt);
        }

        // For perspective projection we emit ray from viewer position through WCS point.
        double projVal = pView.projectionMatrix().GetItem(3, 2);
        double focalLength = -1.0 / projVal;
        OdGePoint3d pos = pView.target() + ((pView.position() - pView.target()).normal() * focalLength);
        OdGeRay3d ray = new(pos, wcsPt);
        pView.Dispose();
        return plane.intersectWith(ray, wcsPt);
    }

    /// <summary>
    /// Method for check correct work of the dragger
    /// </summary>
    protected bool CheckDragger()
    {
        return State != DraggerState.Waiting;
    }
}