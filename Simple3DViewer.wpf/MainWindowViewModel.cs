using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Resources.Strings;
using Simple3DViewer.Winform.Controls;
using Simple3DViewer.wpf.Enums;
using Simple3DViewer.wpf.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;

namespace Simple3DViewer.wpf;

internal class LevelOption : ObservableObject
{
    private readonly MainWindowViewModel _owner;
    public ViewerSelectionOptionsLevel Level { get; }

    public string Display
    {
        get
        {
            string? value_str = Level.ToString();
            if (string.IsNullOrEmpty(value_str))
                return string.Empty;
            FieldInfo? fi = Level.GetType().GetField(value_str);
            if (fi != null)
            {
                DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
                return ((attributes.Length > 0) && (!String.IsNullOrEmpty(attributes[0].Description))) ? attributes[0].Description : Level.ToString();
            }
            else
            {
                throw new InvalidEnumArgumentException();
            }
        }
    }

    public bool IsChecked
    {
        get => _owner.SelectedLevel == Level;
        set { if (value) _owner.SelectedLevel = Level; } // 只有勾选为 true 时写回
    }

    public LevelOption(MainWindowViewModel owner, ViewerSelectionOptionsLevel level)
    { _owner = owner; Level = level; }

    public void Refresh() => OnPropertyChanged(nameof(IsChecked));
}

internal partial class MainWindowViewModel : ObservableObject
{
    private readonly ICultureSettingService cultureSettingService;
    private readonly IDialogService dialogService;
    private readonly IThemeSettingService themeSettingService;

    [ObservableProperty]
    private bool _topmost;

    [ObservableProperty]
    private int currentCultureLCID;

    [ObservableProperty]
    private OdaVisualizeContext? odaVisualizeContext;

    private bool isDarkTheme;

    [ObservableProperty]
    private ViewerDraggerType leftButtonDragger = ViewerDraggerType.Select;

    [ObservableProperty]
    private ViewerDraggerType middleButtonDragger = ViewerDraggerType.Pan;

    [ObservableProperty]
    private ViewerRenderMode renderMode = ViewerRenderMode.kNone;

    private ViewerSelectionOptionsLevel _selectedLevel = ViewerSelectionOptionsLevel.Entity;

    public ViewerSelectionOptionsLevel SelectedLevel
    {
        get => _selectedLevel;
        set
        {
            if (_selectedLevel == value) return;
            _selectedLevel = value;
            OnPropertyChanged();
            // 让所有项更新勾选状态
            foreach (LevelOption o in Levels) o.Refresh();
        }
    }

    [ObservableProperty]
    private ViewerDraggerType rightButtonDragger = ViewerDraggerType.Orbit;

    [ObservableProperty]
    private bool showFPS = true;

    [ObservableProperty]
    private bool showViewCube = true;

    [ObservableProperty]
    private bool showWCS = true;

    public ObservableCollection<LevelOption> Levels { get; }

    public MainWindowViewModel(IThemeSettingService themeSettingService, ICultureSettingService cultureSettingService, IDialogService dialogService)
    {
        this.themeSettingService = themeSettingService;
        this.cultureSettingService = cultureSettingService;
        this.dialogService = dialogService;
        Levels = new ObservableCollection<LevelOption>(
           Enum.GetValues(typeof(ViewerSelectionOptionsLevel))
               .Cast<ViewerSelectionOptionsLevel>()
               .Select(l => new LevelOption(this, l))
       );

        CurrentCultureLCID = System.Globalization.CultureInfo.CurrentCulture.LCID;
        IsDarkTheme = themeSettingService.IsDarkTheme;
    }

    public bool IsDarkTheme
    {
        get { return isDarkTheme; }
        set
        {
            if (SetProperty(ref isDarkTheme, value))
            {
                themeSettingService.ApplyThemeLightDark(value);
            }
        }
    }

    [RelayCommand]
    private void ChangeCulture(string lcidString)
    {
        if (!int.TryParse(lcidString, out int lcid))
            return;
        CurrentCultureLCID = lcid;
        cultureSettingService.ChangeCulture(lcid);
    }

    [RelayCommand]
    private void ChangeTopmost()
    {
        Topmost = !Topmost;
    }

    [RelayCommand]
    private async Task OpenFile()
    {
        OpenFileDialog dlg = new()
        {
            Title = $"{Strings.OpenFile}",
            Filter = "STEP (*.step;*.stp)|*.step;*.stp|" +
            "DWG (*.dwg)|*.dwg|" +
            "DGN (*.dgn)|*.dgn|" +
            "OBJ (*.obj)|*.obj|" +
            "STL (*.stl)|*.stl|" +
            "Visualize Scene (*.vsf;*.vsfx)|*.vsf;*.vsfx|" +
            "PRC (*.prc)|*.prc|" +
            $"{Strings.AllSupportedFiles}|*.dwg;*.dgn;*.obj;*.stl;*.vsf;*.vsfx;*.prc;*.step;*.stp",
            FilterIndex = 1,
            CheckFileExists = true,
            CheckPathExists = true,
            Multiselect = false
        };
        if (dlg.ShowDialog() == false)
        {
            return;
        }

        using IDisposable _ = dialogService.ShowProgressDialog();

        OdaVisualizeContext? newOdaVisualizeContext = await Task.Run(() =>
        {
            OdaVisualizeContext odaVisualizeContext = new();
            if (odaVisualizeContext.LoadFile(dlg.FileName))
            {
                return odaVisualizeContext;
            }
            return null;
        });

        this.OdaVisualizeContext = newOdaVisualizeContext;
    }

    //[RelayCommand]
    //private async Task OpenFile()
    //{
    //    #region Test

    //    //string[] paths = [
    //    //                   "C:\\Users\\yoiri\\Downloads\\45214-2X02.stp",
    //    //                           "C:\\Users\\yoiri\\Downloads\\TPS48111LQDGXRQ1.STEP",
    //    //                           "C:\\Users\\yoiri\\Downloads\\2N7002-7-F.STEP",
    //    //                           "C:\\Users\\yoiri\\Downloads\\THRMC1005X55N.step",
    //    //                       ];

    //    //await Parallel.ForEachAsync(paths, async (path, token) =>
    //    //{
    //    //    using OdaVisualizeContext context = new();
    //    //    bool res = context.LoadFile(path);
    //    //    if (res && context.IsInitialized)
    //    //    {
    //    //        Console.WriteLine($"Loaded {path} successfully.");
    //    //        if (context.DatabaseInfo is not null)
    //    //        {
    //    //            Console.WriteLine($"Import time: {context.DatabaseInfo.ImportTime} ms");
    //    //        }
    //    //    }
    //    //    else
    //    //    {
    //    //        Console.WriteLine($"Failed to load {path}.");
    //    //    }
    //    //});
    //    #endregion

    //    var dlg = new OpenFileDialog
    //    {
    //        Title = $"{Strings.OpenFile}",
    //        Filter =
    //            "STEP (*.step;*.stp)|*.step;*.stp|" +
    //            "DWG (*.dwg)|*.dwg|" +
    //            "DGN (*.dgn)|*.dgn|" +
    //            "OBJ (*.obj)|*.obj|" +
    //            "STL (*.stl)|*.stl|" +
    //            "Visualize Scene (*.vsf;*.vsfx)|*.vsf;*.vsfx|" +
    //            "PRC (*.prc)|*.prc|" +
    //            $"{Strings.AllSupportedFiles}|*.dwg;*.dgn;*.obj;*.stl;*.vsf;*.vsfx;*.prc;*.step;*.stp",
    //        FilterIndex = 1,
    //        CheckFileExists = true,
    //        CheckPathExists = true,
    //        Multiselect = false
    //    };

    //    if (dlg.ShowDialog() != true)
    //        return;

    //    // 取消信号：一旦 Set，命令立即结束，不再等待后台
    //    var cancelTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    //    var canceled = false;

    //    // 进度框的取消按钮：触发取消信号
    //    using var ___ = dialogService.ShowProgressDialogWithCancelButton(() =>
    //      {
    //          canceled = true;
    //          cancelTcs.TrySetResult(true);
    //      });

    //    // 后台线程执行：注意内部处理失败/异常时的释放
    //    var workerTask = Task.Run(() =>
    //            {
    //                OdaVisualizeContext? ctx = null;
    //                try
    //                {
    //                    ctx = new OdaVisualizeContext();
    //                    if (ctx.LoadFile(dlg.FileName))
    //                    {
    //                        // 成功，把 ctx 返回给外层
    //                        return ctx;
    //                    }

    //                    // 加载失败：立即释放
    //                    ctx.Dispose();
    //                    return null;
    //                }
    //                catch
    //                {
    //                    // 异常：释放后继续抛
    //                    ctx?.Dispose();
    //                    throw;
    //                }
    //            });

    //    try
    //    {
    //        // 谁先完成就跟随谁：取消 -> 立即返回；工作完成 -> 取结果
    //        var finished = await Task.WhenAny(workerTask, cancelTcs.Task);

    //        if (finished == cancelTcs.Task)
    //        {
    //            // 用户取消：立刻结束命令 & 关闭进度框

    //            // 后台稍后完成时：如果有结果，释放它（不等待）
    //            _ = workerTask.ContinueWith(t =>
    //            {
    //                if (t.Status == TaskStatus.RanToCompletion && t.Result is IDisposable d)
    //                {
    //                    try { d.Dispose(); } catch { /* ignore */ }
    //                }
    //                // 记录异常以避免未观察异常（需要的话写日志）
    //                _ = t.Exception;
    //            }, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.NotOnRanToCompletion);

    //            // 即使 RanToCompletion 也处理释放（上面 NotOnRanToCompletion 不覆盖的情况）
    //            _ = workerTask.ContinueWith(t =>
    //            {
    //                if (t.Status == TaskStatus.RanToCompletion && t.Result is IDisposable d)
    //                {
    //                    try { d.Dispose(); } catch { /* ignore */ }
    //                }
    //            }, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);

    //            return;
    //        }

    //        // 工作先完成：安全取结果
    //        var newCtx = await workerTask;

    //        if (newCtx != null)
    //        {
    //            if (!canceled)
    //            {
    //                // 未取消：正常使用
    //                this.OdaVisualizeContext = newCtx;
    //            }
    //            else
    //            {
    //                // 极端竞态：当你判断到完成时，用户恰好已点取消 -> 释放
    //                newCtx.Dispose();
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        // TODO: 记录日志/提示
    //        Console.WriteLine(ex);
    //    }
    //}
}