using ODA.Visualize.TV_Visualize;
using Simple3DViewer.Shared.Scopes;
using Simple3DViewer.Winform.Controls.OdaVisualize.ObjectProperties;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Simple3DViewer.Winform.Controls;

public class OdaVisualizeContext:IDisposable
{
    [MemberNotNullWhen(true, nameof(TvDatabaseId))]
    [MemberNotNullWhen(true, nameof(DatabaseInfo))]
    [MemberNotNullWhen(true, nameof(FilePath))]
    public bool IsInitialized { get; private set; } = false;
    public OdTvDatabaseId? TvDatabaseId { get; private set; }

    public TvDatabaseInfo? DatabaseInfo { get; private set; }
    public string? FilePath { get; private set; }

    public void Dispose()
    {
        OdTvFactoryId factId = TV_Visualize_Globals.odTvGetFactory();
        if (TvDatabaseId is not null)
            factId.removeDatabase(TvDatabaseId);
    }

    public bool LoadFile(string filepath)
    {
        IsInitialized = false;
        using MemoryManagerScope _ = new();

        OdTvFactoryId factId = TV_Visualize_Globals.odTvGetFactory();
        if (TvDatabaseId is not null)
            factId.removeDatabase(TvDatabaseId);

        try
        {
            bool isVsf = false;
            OdTvBaseImportParams? importparam = GetImportParams(filepath, ref isVsf);
            DatabaseInfo = new TvDatabaseInfo
            {
                FilePath = filepath,
                Type = isVsf ? TvDatabaseInfo.ProfilingType.FromFile : TvDatabaseInfo.ProfilingType.Import
            };

            importparam?.setFilePath(filepath);
            importparam?.setProfiling(DatabaseInfo);

            Stopwatch timer = Stopwatch.StartNew();
            timer.Start();
            string ext = System.IO.Path.GetExtension(filepath);
            if (ext.Equals(".vsfx", StringComparison.CurrentCultureIgnoreCase))
                TvDatabaseId = factId.readVSFX(filepath);
            else if (importparam is not null)
                TvDatabaseId = isVsf ? factId.readFile(importparam.getFilePath()) : factId.importFile(importparam);
            timer.Stop();
            DatabaseInfo.ImportTime = timer.ElapsedMilliseconds;
        }
        catch
        {
            return false;
        }

        if (TvDatabaseId is null)
            return false;
        FilePath = filepath;
        IsInitialized = true;
        return true;
    }


    private OdTvBaseImportParams? GetImportParams(string filePath, ref bool isVsf)
    {
        OdTvBaseImportParams? importParams = null;
        string ext = System.IO.Path.GetExtension(filePath);
        if (ext != null)
        {
            ext = ext.ToLower();
            if (ext == ".dwg")
            {
                OdTvDwgImportParams dwgPmtrs = new();
                importParams = dwgPmtrs;
                //dwgPmtrs.setDCRect(new OdTvDCRect(0, this.Width, this.Height, 0));
                dwgPmtrs.setObjectNaming(true);
                dwgPmtrs.setStoreSourceObjects(false);
                dwgPmtrs.setFeedbackForChooseCallback(null);
            }
            else if (ext == ".dgn")
            {
                OdTvDgnImportParams dgnPmtrs = new();
                importParams = dgnPmtrs;
                //dgnPmtrs.setDCRect(new OdTvDCRect(0, this.Width, this.Height, 0));
                dgnPmtrs.setObjectNaming(true);
                dgnPmtrs.setStoreSourceObjects(false);
                dgnPmtrs.setFeedbackForChooseCallback(null);
            }
            else if (ext == ".obj")
                importParams = new OdTvObjImportParams();
            else if (ext == ".stl")
                importParams = new OdTvStlImportParams();
            else if (ext == ".vsf" || ext == ".vsfx")
            {
                isVsf = true;
                importParams = new OdTvBaseImportParams();
            }
            else if (ext == ".prc")
            {
                OdTvPrcImportParams prcPmtrs = new();
                importParams = prcPmtrs;
                //prcPmtrs.setDCRect(new OdTvDCRect(0, this.Width, this.Height, 0));
                prcPmtrs.setObjectNaming(true);
                prcPmtrs.setStoreSourceObjects(false);
                prcPmtrs.setFeedbackForChooseCallback(null);
                prcPmtrs.setImportWithHierarchy(true);
                prcPmtrs.setImportPrcParams(true);
            }
            else if (ext == ".step" || ext == ".stp")
            {
                importParams = new OdTvStepImportParams();
            }
        }
        return importParams;
    }
}
