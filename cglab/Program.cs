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
using System.Windows.Forms;

public abstract class AppMain : CGApplicationTemplate<Application, Device, DeviceArgs>
{ [STAThread] static void Main() { RunApplication(); } }
// ==================================================================================
#endregion


public abstract class Application : CGApplication
{
    [DisplayNumericProperty(Default: new [] { 0d, 0d }, Increment: 1, Name: "Сдвиг")]
    public abstract DVector2 Shift { get; set; }

    [DisplayNumericProperty(Default: new [] { 0d, 0d }, Increment: 1, Name: "Центр вращения")]
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

    private double angle = 0d;

    [DisplayNumericProperty(Default: 0.05d, Increment: 0.05, Minimum: 1e-5, Maximum: 0.5, Decimals: 3, Name: "Шаг")]
    public abstract double Step { get; set; }

    [DisplayNumericProperty(Default: 1d, Increment: 0.1, Name: "Параметр")]
    public abstract double Parameter { get; set; }

    [DisplayNumericProperty(Default: new [] { 1d, 1d }, Increment: 0.1, Minimum: 0, Name: "Масштаб по x/y")]
    public abstract DVector2 Scale { get; set; }


    protected override void OnMainWindowLoad(object sender, EventArgs args)
    {
        base.RenderDevice.BufferBackCol = 0xF0;
        base.ValueStorage.RowHeight = 30;
        base.RenderDevice.Size = new Size(600, 600);
        base.MainWindow.Size = new Size(900, 600);

        base.RenderDevice.MouseMoveWithLeftBtnDown += (s, e)
            => Shift += new DVector2(e.MovDeltaX, -e.MovDeltaY);

        base.RenderDevice.MouseMoveWithMiddleBtnDown += (s, e)
            => Angle += e.MovDeltaX;

        this.RenderDevice.MouseWheel += (s, e) => this.Scale += 0.0005 * (double) e.Delta;

        base.RenderDevice.MouseMoveWithRightBtnDown += (s, e)
            => Scale += 0.005 * new DVector2(e.MovDeltaX, -e.MovDeltaY);

        double m = 2;
        this.RenderDevice.HotkeyRegister(Keys.Up, (s, e) => this.RotationShift += new DVector2(0.0, m));
        this.RenderDevice.HotkeyRegister(Keys.Down, (s, e) => this.RotationShift -= new DVector2(0.0, m));
        this.RenderDevice.HotkeyRegister(Keys.Left, (s, e) => this.RotationShift -= new DVector2(m, 0.0));
        this.RenderDevice.HotkeyRegister(Keys.Right, (s, e) => this.RotationShift += new DVector2(m, 0.0));
    }


    protected override void OnDeviceUpdate(object s, DeviceArgs e)
    {
        DVector2 shift = Shift;
        
        Size screenSize = base.RenderDevice.Size;
        DVector2 screenCenter = new DVector2(screenSize.Width / 2d, screenSize.Height / 2d);
        
        DVector2 rotationCenter = new DVector2(screenCenter.X + shift.X, screenCenter.Y - shift.Y);
        DVector2 axisCenter = new DVector2(rotationCenter.X + RotationShift.X, rotationCenter.Y - RotationShift.Y); 

        DrawAxis(axisCenter, rotationCenter, e);
        DrawRotationCenter(rotationCenter, e);

        var points = GetPoints(Parameter, Step);
        var autoscale = GetScale(points, e);

        points = 
            points
            .Select(p => autoscale * new DVector2(Scale.X * p.X, Scale.Y * p.Y))
            .Select(p => p + axisCenter)
            .Select(p => RotatePoint(p, rotationCenter, Angle))
            .ToList();

        for (int i = 0; i < points.Count - 1; i++)
            e.Surface.DrawLine(Color.Black.ToArgb(), points[i], points[i + 1]);
    }

    private List<DVector2> GetPoints(double a, double step) 
    {
        var points = new List<DVector2>();
        
        for (double t = 0; t < 2 * Math.PI; t += step) 
        {
            double x = a * Math.Pow(Math.Cos(t), 3);
            double y = a * Math.Pow(Math.Sin(t), 3);
            points.Add(new DVector2(x, y));
        }

        return points;
    }

    private double GetScale(List<DVector2> points, DeviceArgs e)
    { 
        Size size = base.RenderDevice.Size;
        double ScreenMin = (size.Width < size.Height) ? size.Width : size.Height;
        double xMax = points.Max(p => p.X);
        double yMax = points.Max(p => p.Y);
        double max = (xMax > yMax) ? xMax : yMax;

        return ScreenMin / 2 / max * Parameter * 0.9;
    }

    private void DrawAxis(DVector2 axisCenter, DVector2 rotationCenter, DeviceArgs e)
    {
        double width = 20 * base.RenderDevice.Width;
        double height = 20 * base.RenderDevice.Height;
        DVector2 ac = axisCenter;
        DVector2 rc = rotationCenter;
        var xAxis = new [] { new DVector2(-width, ac.Y), new DVector2(width, ac.Y) };
        var yAxis = new [] { new DVector2(ac.X, -height), new DVector2(ac.X, height) };

        e.Surface.DrawLine(Color.Black.ToArgb(), RotatePoint(xAxis[0], rc, Angle), RotatePoint(xAxis[1], rc, Angle));
        e.Surface.DrawLine(Color.Black.ToArgb(), RotatePoint(yAxis[0], rc, Angle), RotatePoint(yAxis[1], rc, Angle));
    }

    private DVector2 RotatePoint(DVector2 p, DVector2 r, double angle) 
    {
        var sinf = Math.Sin(angle * Math.PI / 180);
        var cosf = Math.Cos(angle * Math.PI / 180);

        return new DVector2(
                    (p.X - r.X) * cosf - (p.Y - r.Y) * sinf + r.X,
                    (p.X - r.X) * sinf + (p.Y - r.Y) * cosf + r.Y);
    }

    private void DrawRotationCenter(DVector2 rotationCenter, DeviceArgs e) 
    {
        e.Graphics.FillEllipse(Brushes.RoyalBlue,
                new Rectangle((int)rotationCenter.X - 5, (int)rotationCenter.Y - 5, 10, 10));
    }
}

