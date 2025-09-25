using ODA.Kernel.TD_RootIntegrated;
using ODA.Visualize.TV_Visualize;
using Simple3DViewer.Shared.Scopes;

namespace Simple3DViewer.Winform.Controls.OdaVisualize.Draggers;

internal class OdTvPanDragger : OdTvDragger
{
    // last clicked or moved point (WCS)
    private OdGePoint3d? _prevPt = null;

    // last camera position (WCS)
    private OdGePoint3d? _pos = null;

    private readonly Cursor _panCursor = Cursors.Default;

    public OdTvPanDragger(OdTvGsDeviceId tvDeviceId, OdTvModelId tvDraggersModelId, OdaVisualizeControl viewControl)
        : base(tvDeviceId, tvDraggersModelId, viewControl)
    {
        _panCursor = Cursors.Hand;
        NeedFreeDrag = true;
    }

    public override DraggerResult NextPoint(int x, int y)
    {
        if (State == DraggerState.Waiting)
            return DraggerResult.NothingToDo;
        using MemoryTransactionScope _ = new();
        OdTvGsViewId? viewId = _viewControl.GetActiveTvViewId();
        if (viewId == null || viewId.isNull())
            return DraggerResult.NothingToDo;

        OdTvGsView view = viewId.openObject();
        if (view == null)
            return DraggerResult.NothingToDo;

        _pos = view.position();
        _prevPt = ToEyeToWorld(x, y) - _pos.asVector();

        return DraggerResult.NothingToDo;
    }

    public override DraggerResult Drag(int x, int y)
    {
        if (State == DraggerState.Waiting)
            return DraggerResult.NothingToDo;

        // calculate click point in WCS
        OdGePoint3d pt = ToEyeToWorld(x, y);
        // obtain delta for dolly
        OdGeVector3d delta = (_prevPt - (pt - _pos)).asVector();

        using MemoryTransactionScope _ = new();
        OdTvGsViewId? viewId = _viewControl.GetActiveTvViewId();
        if (viewId == null || viewId.isNull())
            return DraggerResult.NothingToDo;

        OdTvGsView view = viewId.openObject();
        if (view == null)
            return DraggerResult.NothingToDo;
        // transform delta to eye
        delta.transformBy(view.viewingMatrix());

        // perform camera moving
        view.dolly(delta.x, delta.y, delta.z);

        // remember the difference between click point in WCS and camera previous position
        _prevPt = pt - _pos!.asVector();

        // remember camera current position
        _pos = view.position();

        return DraggerResult.NeedUpdateView;
    }

    public override DraggerResult NextPointUp(int x, int y)
    {
        return Reset();
    }

    public override bool UpdateCursor()
    {
        switch (State)
        {
            case DraggerState.Waiting:
            case DraggerState.Working:
                CurrentCursor = _panCursor;
                break;

            case DraggerState.Finishing:
                CurrentCursor = LasAppActiveCursor;
                break;
        }

        return true;
    }
}