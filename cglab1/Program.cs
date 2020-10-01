#region Директивы using (подключаемые библиотеки) и точка входа приложения

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
        this.RenderDevice.HotkeyRegister(Keys.Up, (s, e) => this.RotationShift -= new DVector2(0.0, m));
        this.RenderDevice.HotkeyRegister(Keys.Down, (s, e) => this.RotationShift += new DVector2(0.0, m));
        this.RenderDevice.HotkeyRegister(Keys.Left, (s, e) => this.RotationShift += new DVector2(m, 0.0));
        this.RenderDevice.HotkeyRegister(Keys.Right, (s, e) => this.RotationShift -= new DVector2(m, 0.0));
    }

    protected override void OnDeviceUpdate(object s, DeviceArgs e)
    {
        Size screenSize = base.RenderDevice.Size;
        DVector2 screenCenter = new DVector2(screenSize.Width / 2d, screenSize.Height / 2d);
        
        DVector2 rotationCenter = new DVector2(screenCenter.X + Shift.X, screenCenter.Y - Shift.Y);
     
        var points = GetSolution(Parameter, Step);
        var autoscale = GetAutoScale(points, e);

        DrawAxis(rotationCenter, e); 
        DrawAxisPoints(rotationCenter, autoscale, e);
        DrawGraph(points, rotationCenter, autoscale, e);
        DrawRotationCenter(rotationCenter, e);
    }

    private DVector2 GetAxisCenter(DVector2 rc) => new DVector2(rc.X + RotationShift.X, rc.Y - RotationShift.Y); 

    private DVector2 Rotate(DVector2 p, DVector2 r, double angle) 
    {
        var sinf = Math.Sin(angle * Math.PI / 180);
        var cosf = Math.Cos(angle * Math.PI / 180);

        return new DVector2(
                    (p.X - r.X) * cosf - (p.Y - r.Y) * sinf + r.X,
                    (p.X - r.X) * sinf + (p.Y - r.Y) * cosf + r.Y);
    }

    private DVector2 SetScale(DVector2 v, double scale) => scale * new DVector2(Scale.X * v.X, Scale.Y * v.Y);

    private double GetAutoScale(List<DVector2> points, DeviceArgs e)
    { 
        Size size = base.RenderDevice.Size;
        double ScreenMin = (size.Width < size.Height) ? size.Width : size.Height;
        double xMax = points.Max(p => p.X);
        double yMax = points.Max(p => p.Y);
        double max = (xMax > yMax) ? xMax : yMax;

        return ScreenMin / 4 / max * Parameter;
    }

    private List<DVector2> GetSolution(double param, double step) 
    {
        var points = new List<DVector2>();
        
        for (double t = 0; t < 2 * Math.PI; t += step) 
        {
            double x = param * Math.Pow(Math.Cos(t), 3);
            double y = param * Math.Pow(Math.Sin(t), 3);
            points.Add(new DVector2(x, y));
        }

        return points;
    }

    private List<DVector2> GetAxisPoints(DVector2 direction) 
    {
        return Enumerable.Range(1, 5)
            .Select(n => new DVector2(direction.X * n, direction.Y * n))
            .ToList();
    }

    private void DrawGraph(List<DVector2> points, DVector2 rc, double scale, DeviceArgs e) 
    {
        points = BindWithAxis(points, rc, scale, e);

        for (int i = 0; i < points.Count - 1; i++)
            e.Surface.DrawLine(Color.DarkMagenta.ToArgb(), points[i], points[i + 1]);
    }

    private List<DVector2> BindWithAxis(List<DVector2> points, DVector2 rc, double scale, DeviceArgs e)
    {
        var ac = GetAxisCenter(rc);
        return points.Select(p => SetScale(p, scale))
                     .Select(p => p + ac)
                     .Select(p => Rotate(p, rc, Angle)).ToList();
    }

    private void DrawAxisPoints(DVector2 rc, double scale, DeviceArgs e) 
    {
        var ac = GetAxisCenter(rc);
        var directions = 
            new List<DVector2>() { new DVector2(1, 0), new DVector2(0, -1),
                                   new DVector2(-1, 0), new DVector2(0,  1) };

        foreach (var dir in directions) 
        {
            var axisPoints = GetAxisPoints(dir);
            var bindedPoints = BindWithAxis(axisPoints, rc, scale, e);

            for (int i = 0; i < axisPoints.Count(); i++) 
            {
                DrawLabel(axisPoints[i], bindedPoints[i], scale, e);
                DrawDot(bindedPoints[i], 4, Brushes.Black, e);
            }
        }
    }

    private void DrawLabel(DVector2 value, DVector2 point, double scale, DeviceArgs e) 
    {
        var label = value.X != 0 ? value.X + "(x)" : -value.Y + "(y)";

        e.Graphics.DrawString(label, new Font("Arial", 9), Brushes.Black, point.X, point.Y);
    }

    private void DrawDot(DVector2 p, int radius, Brush brush, DeviceArgs e) {
        e.Graphics.FillEllipse(brush,
                new Rectangle((int)(p.X - radius / 2), (int)(p.Y - radius / 2), radius, radius));
    }

    private void DrawAxis(DVector2 rc, DeviceArgs e)
    {
        double DISTANCE = 200;
        double width = 20 * base.RenderDevice.Width;
        double height = 20 * base.RenderDevice.Height;

        var ac = GetAxisCenter(rc);

        var xAxis = new [] { new DVector2(-width, ac.Y), new DVector2(width, ac.Y) };
        var yAxis = new [] { new DVector2(ac.X, -height), new DVector2(ac.X, height) };

        e.Surface.DrawLine(Color.Black.ToArgb(), Rotate(xAxis[0], rc, Angle), Rotate(xAxis[1], rc, Angle));
        e.Surface.DrawLine(Color.Black.ToArgb(), Rotate(yAxis[0], rc, Angle), Rotate(yAxis[1], rc, Angle));

        var xLabel = new DVector2(ac.X + DISTANCE, ac.Y + DISTANCE / 10);
        var yLabel = new DVector2(ac.X + DISTANCE / 10, ac.Y - DISTANCE);
        var rXLabel = Rotate(xLabel, rc, Angle);
        var rYLabel = Rotate(yLabel, rc, Angle);
    }

    private void DrawRotationCenter(DVector2 rc, DeviceArgs e) => 
        DrawDot(rc, 6, Brushes.DarkMagenta, e);
}
