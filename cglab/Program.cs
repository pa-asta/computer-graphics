#region Директивы using (подключаемые библиотеки) и точка входа приложения
//#define UseOpenGL // Раскомментировать для использования OpenGL
#if (!UseOpenGL)
using Device     = CGLabPlatform.GDIDevice;
using DeviceArgs = CGLabPlatform.GDIDeviceUpdateArgs;
#else
using Device     = CGLabPlatform.OGLDevice;
using DeviceArgs = CGLabPlatform.OGLDeviceUpdateArgs;
using SharpGL;
#endif

// ==================================================================================

    using System;
    using System.Linq;
    using System.Drawing;
    using System.Collections.Generic;
    using CGLabPlatform;

// ==================================================================================
using CGApplication = AppMain;
public abstract class AppMain : CGApplicationTemplate<Application, Device, DeviceArgs>
{ [STAThread] static void Main() { RunApplication(); } }
// ==================================================================================
#endregion


public abstract class Application : CGApplication
{
    [DisplayNumericProperty(Default: new[] { 0d, 0d }, Increment: 1, Name: "Сдвиг")]
    public abstract DVector2 Shift { get; set; }

    [DisplayNumericProperty(Default: new[] { 0d, 0d }, Increment: 1, Name: "Центр вращения")]
    public abstract DVector2 RotationShift { get; set; }

    [DisplayNumericProperty(Default: 0d, Increment: 1, Decimals: 2, Name: "Угол поворота")]
    public virtual double Angle
    {
        get { return angle; }
        set
        {
            while (value < 0) value += 360;
            while (value >= 360) value -= 360;
            angle = value;
        }
    }

    [DisplayNumericProperty(Default: 0.01d, Increment: 0.01, Minimum: 1e-3, Maximum: 0.25, Decimals: 3, Name: "Шаг")]
    public abstract double Step { get; set; }

    [DisplayNumericProperty(Default: 10d, Increment: 0.1, Name: "Параметр а")]
    public abstract double Parameter { get; set; }

    private double angle = 0d;

    protected override void OnMainWindowLoad(object sender, EventArgs args)
    {
        base.RenderDevice.BufferBackCol = 0xF0;
        base.ValueStorage.RowHeight = 30;
        base.RenderDevice.Size = new Size(600, 600);
        base.MainWindow.Size = new Size(900, 600);

        base.RenderDevice.MouseMoveWithLeftBtnDown += (s, e)
            => Shift += new DVector2(e.MovDeltaX, -e.MovDeltaY);
    }


    protected override void OnDeviceUpdate(object s, DeviceArgs e)
    {
        DVector2 shift = Shift;
        double step = Step;
        double angle = Angle;
        double param = Parameter;
        Size screenSize = base.RenderDevice.Size;
        DVector2 screenCenter = new DVector2(screenSize.Width / 2d, screenSize.Height / 2d);
        DVector2 center = new DVector2(screenCenter.X + shift.X, screenCenter.Y - shift.Y);
        DVector2 rotationCenter = new DVector2(center.X + RotationShift.X, center.Y - RotationShift.Y);

        DrawAxis(center, e);
        DrawRotationPoint(rotationCenter, e);


        var sinf = Math.Sin(angle * Math.PI / 180);
        var cosf = Math.Cos(angle * Math.PI / 180);

        var rx = rotationCenter.X;
        var ry = rotationCenter.Y;

        var points = GetPoints(param, step);
        var scale = GetScale(points, e);

        points = 
            points
            .Select(p => new DVector2(scale * p.X, scale * p.Y))
            .Select(p => p + center)
            .Select(p => 
                new DVector2(
                    (p.X - rx) * cosf - (p.Y - ry) * sinf + rx,
                    (p.X - rx) * sinf + (p.Y - ry) * cosf + ry))
            .ToList();



        for (int i = 0; i < points.Count - 1; i++)
        {
            e.Surface.DrawLine(Color.Black.ToArgb(), points[i], points[i + 1]);
        }
    }

    private List<DVector2> GetPoints(double a, double precision)
    {
        List<DVector2> points = new List<DVector2>();

        for (double t = 0; t < 2 * Math.PI; t += precision) 
        {
            points.Add(new DVector2(
                a * Math.Pow(Math.Cos(t), 3), a * Math.Pow(Math.Sin(t), 3)));
        }

        return points;
    }

    private double GetScale(List<DVector2> points, DeviceArgs e)
    { 
        Size size = base.RenderDevice.Size;
        double ScreenMin = (size.Width < size.Height) ? size.Width : size.Height;
        double XMax = points.Max(p => p.X);
        double YMax = points.Max(p => p.Y);
        double max = (XMax > YMax) ? XMax : YMax;

        return ScreenMin < 100 * max ? ScreenMin / max * 0.5 : max;
    }

    private void DrawAxis(DVector2 center, DeviceArgs e)
    {
        e.Surface.DrawLine(Color.Black.ToArgb(),
            0, center.Y, base.RenderDevice.Width, center.Y);
        e.Surface.DrawLine(Color.Black.ToArgb(),
            center.X, 0, center.X, base.RenderDevice.Height);
    }

    private void DrawRotationPoint(DVector2 rotationCenter, DeviceArgs e) 
    {
        e.Graphics.FillEllipse(Brushes.RoyalBlue,
                new Rectangle((int)rotationCenter.X - 5, (int)rotationCenter.Y - 5, 10, 10));
    }
}

