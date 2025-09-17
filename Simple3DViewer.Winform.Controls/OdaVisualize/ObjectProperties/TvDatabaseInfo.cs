using ODA.Visualize.TV_Visualize;

namespace Simple3DViewer.Winform.Controls.OdaVisualize.ObjectProperties;

public class TvDatabaseInfo : OdTvFilerTimeProfiling
{
    [Flags]
    public enum ProfilingType
    {
        New = 0,
        BuiltIn = 1,
        FromFile = 2,
        Import = 4
    }

    public long ImportTime { get; set; }
    public long VectorizingTime { get; set; }
    public long TvCreationTime { get; set; }
    public long FirstUpdateTime { get; set; }
    public long CDACreationTime { get; set; }
    public ProfilingType Type { get; set; }

    public long TotalTime { get { return ImportTime + VectorizingTime + FirstUpdateTime + CDACreationTime; } }

    public string? FilePath { get; set; }

    public TvDatabaseInfo()
    {
        Type = ProfilingType.New;
    }

    public override void setVectorizingTime(long time)
    {
        VectorizingTime = time;
    }

    public override long getVectorizingTime()
    {
        return VectorizingTime;
    }

    public override void setTvTime(long time)
    {
        TvCreationTime = time;
    }

    public override long getTvTime()
    {
        return TvCreationTime;
    }

    public override void setImportTime(long time)
    {
        ImportTime = time;
    }

    public override long getImportTime()
    {
        return ImportTime;
    }

    public override void setCDATreeCreationTime(long time)
    {
        CDACreationTime = time;
    }

    public override long getCDATreeCreationTime()
    {
        return CDACreationTime;
    }

}
