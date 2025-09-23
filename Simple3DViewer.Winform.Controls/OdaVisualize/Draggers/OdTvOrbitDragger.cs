using ODA.Kernel.TD_RootIntegrated;
using ODA.Visualize.TV_Visualize;
using ODA.Visualize.TV_VisualizeTools;
using Simple3DViewer.Shared.Extensions;
using Simple3DViewer.Shared.Scopes;

namespace Simple3DViewer.Winform.Controls.OdaVisualize.Draggers;

internal class OdTvOrbitDragger : OdTvDragger
{
    private readonly Cursor _orbitCursor = Cursors.Default;

    // last clicked or moved point (WCS)
    private OdTvDCPoint? _startPt = null;

    // center of scene
    private OdGePoint3d? _center = null;

    public OdTvOrbitDragger(OdTvGsDeviceId tvDeviceId, OdTvModelId tvDraggersModelId, OdaVisualizeControl viewControl)
        : base(tvDeviceId, tvDraggersModelId, viewControl)
    {
        _orbitCursor = Cursors.Arrow;
        NeedFreeDrag = true;
    }

    private struct OdTvViewportParams
    {
        public OdTvViewportParams(OdTvGsView pView)
        {
            position = pView.position();
            target = pView.target();
            up = pView.upVector();
            fieldWidth = pView.fieldWidth();
            fieldHeight = pView.fieldHeight();
            projectionType = pView.isPerspective() ? OdTvGsView_Projection.kPerspective : OdTvGsView_Projection.kParallel;
        }

        //restore saved param to view
        public void Setup(OdTvExtendedView pTvExtendedView)
        {
            pTvExtendedView.setView(position, target, up, fieldWidth, fieldHeight, projectionType);
        }

        public OdTvViewportParams Clone()
        {
            return new OdTvViewportParams
            {
                position = new OdGePoint3d(this.position),
                target = new OdGePoint3d(this.target),
                up = new OdGeVector3d(this.up),
                fieldWidth = this.fieldWidth,
                fieldHeight = this.fieldHeight,
                projectionType = this.projectionType
            };
        }

        public OdGePoint3d position;
        public OdGePoint3d target;
        public OdGeVector3d up;
        public double fieldWidth;
        public double fieldHeight;
        public OdTvGsView_Projection projectionType;
    };

    private OdTvViewportParams _vParams;

    public override DraggerResult NextPoint(int x, int y)
    {
        if (State == DraggerState.Waiting)
            return DraggerResult.NothingToDo;

        // calculate click point in WCS
        OdGePoint3d pt = ToEyeToWorld(x, y);
        using MemoryManagerScope _ = new();
        OdTvGsViewId? viewId = _viewControl.GetActiveTvViewId();
        if (viewId.IsNull())
            return DraggerResult.NothingToDo;

        OdTvGsView view = viewId.openObject();
        if (view == null)
            return DraggerResult.NothingToDo;

        _vParams = new OdTvViewportParams(view);

        // transfer point to the eye coordinate system
        _startPt = new OdTvDCPoint(x, y);

        // here we should to remember the extents since we want to rotate the scene about this point
        OdGeBoundBlock3d extents = new();
        OdTvExtendedView.getExtentsFromTvView(extents, viewId);
        if (extents != null)
            _center = extents.center();
        else
            _center = _viewControl.GetMainModelExtents().center();

        return DraggerResult.NothingToDo;
    }

    public override DraggerResult Drag(int x, int y)
    {
        if (State == DraggerState.Waiting)
            return DraggerResult.NothingToDo;

        using MemoryManagerScope _ = new();
        OdTvGsViewId? viewId = _viewControl.GetActiveTvViewId();
        if (viewId.IsNull())
            return DraggerResult.NothingToDo;

        OdTvGsView view = viewId.openObject();
        OdTvExtendedView? exView = _viewControl.GetActiveTvExtendedView();
        if (view == null || exView == null)
            return DraggerResult.NothingToDo;

        _vParams.Setup(exView);

        // calculate the angles for the rotation about appropriate axes
        OdTvDCRect viewportRect = new();
        view.getViewport(viewportRect);
        OdGePoint2d screenSize = new(Math.Abs(viewportRect.xmax - viewportRect.xmin), Math.Abs(viewportRect.ymax - viewportRect.ymin));
        double size = Math.Max(screenSize.x, screenSize.y);
        double distX = (_startPt!.x - x) * Math.PI / size;
        double distY = (_startPt.y - y) * Math.PI / size;

        double xOrbit = distY;
        double yOrbit = distX;
        if (xOrbit != 0.0 || yOrbit != 0.0)
        {
            DoOrbit(_vParams, xOrbit, yOrbit, _center!, exView);
        }

        return DraggerResult.NeedUpdateView;
    }

    private void DoOrbit(OdTvViewportParams vparams, double xOrbit, double yOrbit, OdGePoint3d viewCenter, OdTvExtendedView pTvExtendedView)
    {
        OdTvViewportParams rotatedParams = vparams.Clone();

        // compute camera side vector
        OdGeVector3d side = rotatedParams.up.crossProduct(rotatedParams.target - rotatedParams.position).normalize();
        double angle = Math.Sqrt(yOrbit * yOrbit + xOrbit * xOrbit);
        OdGeVector3d axis = (rotatedParams.up * yOrbit - side * xOrbit).normalize();
        rotatedParams.position.rotateBy(angle, axis, viewCenter);
        rotatedParams.target.rotateBy(angle, axis, viewCenter);
        if (yOrbit != 0.0)
        {
            side = side.rotateBy(angle, axis);
        }

        // update up vector
        rotatedParams.up = (rotatedParams.target - rotatedParams.position).crossProduct(side).normalize();
        rotatedParams.Setup(pTvExtendedView);
    }

    public override DraggerResult NextPointUp(int x, int y)
    {
        return Reset();
    }

    public override bool UpdateCursor()
    {
        CurrentCursor = State == DraggerState.Finishing ? LasAppActiveCursor : _orbitCursor;
        return true;
    }

    public override bool CanFinish()
    {
        return true;
    }
}