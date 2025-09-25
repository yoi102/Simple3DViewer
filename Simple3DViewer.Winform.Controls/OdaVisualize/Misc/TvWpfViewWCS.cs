using ODA.Kernel.TD_RootIntegrated;
using ODA.Visualize.TV_Visualize;
using Simple3DViewer.Shared.Scopes;

namespace Simple3DViewer.Winform.Controls.OdaVisualize.Misc;

internal class TvWpfViewWCS
{
    // Special model for WCS object
    private readonly OdTvModelId _tvWcsModelId;

    // View, in which this WCS is located
    private readonly OdTvGsViewId _viewId;

    // View, associated with this view
    private readonly OdTvGsViewId _wcsViewId;

    private static int wcsViewNumber = 1;

    public TvWpfViewWCS(OdTvDatabaseId tvDbId, OdTvGsViewId tvViewId)
    {
        _viewId = tvViewId;

        using MemoryTransactionScope _ = new();
        OdTvGsDevice dev = _viewId.openObject().device().openObject(OdTv_OpenMode.kForWrite);

        // add wcs view
        OdTvResult rc = OdTvResult.tvOk;
        _wcsViewId = dev.createView("WcsView_" + wcsViewNumber, false, ref rc);
        dev.addView(_wcsViewId);

        _tvWcsModelId = tvDbId.openObject(OdTv_OpenMode.kForWrite).createModel("$ODA_TVVIEWER_WCS_" + wcsViewNumber++);
        OdTvGsView wcsView = _wcsViewId.openObject(OdTv_OpenMode.kForWrite);
        wcsView.addModel(_tvWcsModelId);
    }

    public void UpdateWCS()
    {
        using MemoryTransactionScope _ = new();

        OdTvGsView view = _viewId.openObject();

        // remove old wcs entities
        OdTvModel wcsModel = _tvWcsModelId.openObject(OdTv_OpenMode.kForWrite);
        wcsModel.clearEntities();

        //1.1 Add wcs entity
        OdTvEntityId wcsObjId = wcsModel.appendEntity("WCS");
        OdTvEntity wcsObj = wcsObjId.openObject(OdTv_OpenMode.kForWrite);

        // define the start point for the WCS
        OdGePoint3d start = new(0d, 0d, 0d);

        // caculate axis lines length in wireframe and shaded modes
        double lineLength = 0.09;
        if ((int)view.mode() != (int)OdGsView_RenderMode.kWireframe && (int)view.mode() != (int)OdGsView_RenderMode.k2DOptimized)
            lineLength = 0.07;

        // create X axis and label
        OdTvGeometryDataId wcsX = wcsObj.appendSubEntity("wcs_x");
        OdTvEntity pWcsX = wcsX.openAsSubEntity(OdTv_OpenMode.kForWrite);
        OdGePoint3d endx = new(start);
        endx.x += lineLength;
        CreateWcsAxis(wcsX, new OdTvColorDef(189, 19, 19), start, endx, "X");

        // create Y axis and label
        OdTvGeometryDataId wcsY = wcsObj.appendSubEntity("wcs_y");
        OdTvEntity pWcsY = wcsY.openAsSubEntity(OdTv_OpenMode.kForWrite);
        OdGePoint3d endy = new(start);
        endy.y += lineLength;
        CreateWcsAxis(wcsY, new OdTvColorDef(12, 171, 20), start, endy, "Y");

        // create Z axis and label
        OdTvGeometryDataId wcsZ = wcsObj.appendSubEntity("wcs_z");
        OdTvEntity pWcsZ = wcsZ.openAsSubEntity(OdTv_OpenMode.kForWrite);
        OdGePoint3d endz = new(start);
        endz.z += lineLength;
        CreateWcsAxis(wcsZ, new OdTvColorDef(20, 57, 245), start, endz, "Z");

        _wcsViewId.openObject().device().openObject().invalidate();
    }

    public bool IsNeedUpdateWCS(OdTvGsView_RenderMode oldmode, OdTvGsView_RenderMode newmode)
    {
        using MemoryTransactionScope _ = new();
        OdTvGsView wcsView = _wcsViewId.openObject(OdTv_OpenMode.kForWrite);
        if (wcsView == null)
        {
            return false;
        }

        bool bOldModeWire = false;
        if (oldmode == OdTvGsView_RenderMode.k2DOptimized || oldmode == OdTvGsView_RenderMode.kWireframe)
            bOldModeWire = true;

        bool bNewModeWire = false;
        if (newmode == OdTvGsView_RenderMode.k2DOptimized || newmode == OdTvGsView_RenderMode.kWireframe)
            bNewModeWire = true;

        wcsView.setMode(bNewModeWire ? OdTvGsView_RenderMode.kWireframe : OdTvGsView_RenderMode.kGouraudShaded);

        if (bOldModeWire != bNewModeWire)
        {
            return true;
        }

        return false;
    }

    private void CreateWcsAxis(OdTvGeometryDataId wcsId, OdTvColorDef color, OdGePoint3d startPoint, OdGePoint3d endPoint, string axisName)
    {
        using MemoryTransactionScope _ = new();

        OdTvEntity pWcs = wcsId.openAsSubEntity(OdTv_OpenMode.kForWrite);
        pWcs.setColor(color);

        OdTvGsView view = _wcsViewId.openObject();

        OdGePoint3d labelRefPoint = new(endPoint);

        // draw lines in wireframe and draw cylinders in shaded modes
        if ((int)view.mode() == (int)OdGsView_RenderMode.k2DOptimized || (int)view.mode() == (int)OdGsView_RenderMode.kWireframe)
        {
            //append axis
            pWcs.appendPolyline(startPoint, endPoint);
        }
        else
        {
            // distance to the end of the arrow
            double lastPointDist = 0.022;
            OdGePoint3d lastPoint = new(endPoint);
            if (axisName == "X")
                lastPoint.x += lastPointDist;
            else if (axisName == "Y")
                lastPoint.y += lastPointDist;
            else if (axisName == "Z")
                lastPoint.z += lastPointDist;

            OdGePoint3dVector pnts = new();
            pnts.Add(new OdGePoint3d(startPoint));
            pnts.Add(new OdGePoint3d(endPoint));
            pnts.Add(new OdGePoint3d(endPoint));
            pnts.Add(new OdGePoint3d(lastPoint));

            OdDoubleArray radii = new(4);
            radii.Add(0.007);
            radii.Add(0.007);
            radii.Add(0.015);
            radii.Add(0d);

            pWcs.appendShellFromCylinder(pnts, radii, OdTvCylinderData_Capping.kBoth, 50);

            // update label reference point
            labelRefPoint = lastPoint;
        }

        // append labels
        if (axisName == "X")
            labelRefPoint.x += 0.015;
        else if (axisName == "Y")
            labelRefPoint.y += 0.015;
        else if (axisName == "Z")
            labelRefPoint.z += 0.015;

        OdTvEntityId wcsTextEntityId = _tvWcsModelId.openObject(OdTv_OpenMode.kForWrite).appendEntity("TextEntity");
        OdTvEntity textEntity = wcsTextEntityId.openObject(OdTv_OpenMode.kForWrite);
        textEntity.setColor(color);
        textEntity.setAutoRegen(true);

        OdTvGeometryDataId labelTextId = textEntity.appendText(labelRefPoint, axisName);
        OdTvTextData labelText = labelTextId.openAsText();
        labelText.setAlignmentMode(OdTvTextStyle_AlignmentType.kMiddleCenter);
        labelText.setTextSize(0.02);
        labelText.setNonRotatable(true);

        if ((int)view.mode() != (int)OdGsView_RenderMode.k2DOptimized && (int)view.mode() != (int)OdGsView_RenderMode.kWireframe)
            textEntity.setLineWeight(new OdTvLineWeightDef(4));
    }

    public void RemoveWCS()
    {
        using MemoryTransactionScope _ = new();
        // remove old wcs entities
        _tvWcsModelId.openObject(OdTv_OpenMode.kForWrite).clearEntities();
    }

    public OdTvGsViewId GetWcsViewId()
    {
        return _wcsViewId;
    }

    public OdTvGsViewId GetParentView()
    {
        return _viewId;
    }
}