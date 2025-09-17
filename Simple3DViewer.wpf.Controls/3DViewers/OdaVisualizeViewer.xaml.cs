using Simple3DViewer.Winform.Controls;
using Simple3DViewer.Winform.Controls.OdaVisualize;
using System.Windows;

namespace Simple3DViewer.wpf.Controls._3DViewers;

/// <summary>
/// OdaVisualizeViewer.xaml 的交互逻辑
/// </summary>
public partial class OdaVisualizeViewer : System.Windows.Controls.UserControl
{
    public OdaVisualizeViewer()
    {
        InitializeComponent();
        _visualizeControl.RenderModeChanged += (s, e) =>
        {
            RenderMode = _visualizeControl.RenderMode;
        };
    }

    public bool ShowFPS
    {
        get { return (bool)GetValue(ShowFPSProperty); }
        set { SetValue(ShowFPSProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ShowFPS.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ShowFPSProperty =
        DependencyProperty.Register(nameof(ShowFPS), typeof(bool), typeof(OdaVisualizeViewer), new PropertyMetadata(true, OnShowFPSVanleChanged));

    private static void OnShowFPSVanleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is OdaVisualizeViewer viewer)
        {
            viewer._visualizeControl.ShowFPS = (bool)e.NewValue;
        }
    }

    public bool ShowViewCube
    {
        get { return (bool)GetValue(ShowViewCubeProperty); }
        set { SetValue(ShowViewCubeProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ShowViewCube.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ShowViewCubeProperty =
        DependencyProperty.Register(nameof(ShowViewCube), typeof(bool), typeof(OdaVisualizeViewer), new PropertyMetadata(true, OnShowViewCubeValueChanged));

    private static void OnShowViewCubeValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is OdaVisualizeViewer viewer)
        {
            viewer._visualizeControl.ShowViewCube = (bool)e.NewValue;
        }
    }

    public bool ShowWCS
    {
        get { return (bool)GetValue(ShowWCSProperty); }
        set { SetValue(ShowWCSProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ShowWCS.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ShowWCSProperty =
        DependencyProperty.Register(nameof(ShowWCS), typeof(bool), typeof(OdaVisualizeViewer), new PropertyMetadata(true, OnShowWCSValueChanged));

    private static void OnShowWCSValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is OdaVisualizeViewer viewer)
        {
            viewer._visualizeControl.ShowWCS = (bool)e.NewValue;
        }
    }

    public Winform.Controls.OdaVisualize.RenderMode RenderMode
    {
        get { return (Winform.Controls.OdaVisualize.RenderMode)GetValue(RenderModeProperty); }
        set { SetValue(RenderModeProperty, value); }
    }

    // Using a DependencyProperty as the backing store for RenderMode.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty RenderModeProperty =
        DependencyProperty.Register(nameof(RenderMode), typeof(Winform.Controls.OdaVisualize.RenderMode), typeof(OdaVisualizeViewer), new FrameworkPropertyMetadata(
            Winform.Controls.OdaVisualize.RenderMode.kNone,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnRenderModeChanged));

    private static void OnRenderModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is OdaVisualizeViewer viewer)
        {
            viewer._visualizeControl.RenderMode = (Winform.Controls.OdaVisualize.RenderMode)e.NewValue;
        }
    }

    public DraggerType LeftButtonDragger
    {
        get { return (DraggerType)GetValue(LeftButtonDraggerProperty); }
        set { SetValue(LeftButtonDraggerProperty, value); }
    }

    // Using a DependencyProperty as the backing store for LeftButtonDragger.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty LeftButtonDraggerProperty =
        DependencyProperty.Register(nameof(LeftButtonDragger), typeof(DraggerType), typeof(OdaVisualizeViewer), new PropertyMetadata(DraggerType.Select, OnLeftButtonDraggerChanged));

    private static void OnLeftButtonDraggerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is OdaVisualizeViewer viewer)
        {
            viewer._visualizeControl.LeftButtonDragger = (DraggerType)e.NewValue;
        }
    }

    public DraggerType MiddleButtonDragger
    {
        get { return (DraggerType)GetValue(MiddleButtonDraggerProperty); }
        set { SetValue(MiddleButtonDraggerProperty, value); }
    }

    // Using a DependencyProperty as the backing store for MiddleButtonDragger.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty MiddleButtonDraggerProperty =
        DependencyProperty.Register(nameof(MiddleButtonDragger), typeof(DraggerType), typeof(OdaVisualizeViewer), new PropertyMetadata(DraggerType.Select, OnMiddleButtonDraggerChanged));

    private static void OnMiddleButtonDraggerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is OdaVisualizeViewer viewer)
        {
            viewer._visualizeControl.MiddleButtonDragger = (DraggerType)e.NewValue;
        }
    }

    public DraggerType RightButtonDragger
    {
        get { return (DraggerType)GetValue(RightButtonDraggerProperty); }
        set { SetValue(RightButtonDraggerProperty, value); }
    }

    // Using a DependencyProperty as the backing store for RightButtonDragger.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty RightButtonDraggerProperty =
        DependencyProperty.Register(nameof(RightButtonDragger), typeof(DraggerType), typeof(OdaVisualizeViewer), new PropertyMetadata(DraggerType.Select, OnRightButtonDraggerChanged));

    private static void OnRightButtonDraggerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is OdaVisualizeViewer viewer)
        {
            viewer._visualizeControl.RightButtonDragger = (DraggerType)e.NewValue;
        }
    }

    public OdaVisualizeContext? OdaVisualizeContext
    {
        get { return (OdaVisualizeContext?)GetValue(OdaVisualizeContextProperty); }
        set { SetValue(OdaVisualizeContextProperty, value); }
    }

    // Using a DependencyProperty as the backing store for OdaVisualizeContext.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty OdaVisualizeContextProperty =
        DependencyProperty.Register(nameof(OdaVisualizeContext), typeof(OdaVisualizeContext), typeof(OdaVisualizeViewer), new PropertyMetadata(null, OnOdaVisualizeContextChanged));

    private static void OnOdaVisualizeContextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not OdaVisualizeContext odaVisualizeContext)
        {
            return;
        }

        if (d is OdaVisualizeViewer viewer)
        {
            viewer._visualizeControl.OdaVisualizeContext = odaVisualizeContext;
        }
    }
}