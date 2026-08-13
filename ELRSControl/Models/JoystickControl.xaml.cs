using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ELRSControl.Models
{
    public partial class JoystickControl : UserControl
    {
        private bool _isMouseDown = false;
        private const double CenterX = 100;
        private const double CenterY = 100;
        private const double Radius = 75;

        #region Dependency Properties

        public static readonly DependencyProperty XValueProperty =
            DependencyProperty.Register(
                nameof(XValue),
                typeof(int),
                typeof(JoystickControl),
                new FrameworkPropertyMetadata(1500, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnChannelValueChanged));

        public static readonly DependencyProperty YValueProperty =
            DependencyProperty.Register(
                nameof(YValue),
                typeof(int),
                typeof(JoystickControl),
                new FrameworkPropertyMetadata(1500, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnChannelValueChanged));

        public int XValue
        {
            get => (int)GetValue(XValueProperty);
            set => SetValue(XValueProperty, value);
        }

        public int YValue
        {
            get => (int)GetValue(YValueProperty);
            set => SetValue(YValueProperty, value);
        }

        private static void OnChannelValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is JoystickControl joystick && !joystick._isMouseDown)
            {
                joystick.UpdateKnobPosition();
            }
        }

        #endregion

        public JoystickControl()
        {
            InitializeComponent();
        }

        private void JoystickCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isMouseDown = true;
            JoystickCanvas.CaptureMouse();
            UpdateJoystickPosition(e.GetPosition(JoystickCanvas));
        }

        private void JoystickCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isMouseDown)
            {
                UpdateJoystickPosition(e.GetPosition(JoystickCanvas));
            }
        }

        private void JoystickCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isMouseDown = false;
            JoystickCanvas.ReleaseMouseCapture();
        }

        private void JoystickCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!JoystickCanvas.IsMouseCaptured)
            {
                _isMouseDown = false;
            }
        }

        private void UpdateJoystickPosition(Point mousePos)
        {
            double dx = mousePos.X - CenterX;
            double dy = mousePos.Y - CenterY;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance > Radius)
            {
                double angle = Math.Atan2(dy, dx);
                dx = Radius * Math.Cos(angle);
                dy = Radius * Math.Sin(angle);
            }
            int newX = (int)(1500 + (dx / Radius) * 500);
            XValue = Math.Max(1000, Math.Min(2000, newX));
            YValue = Math.Max(1000, Math.Min(2000, newY));

            UpdateKnobPosition();
            int newY = (int)(1500 - (dy / Radius) * 500);
        }

        private void UpdateKnobPosition()
        {
            double normalizedX = (XValue - 1500) / 500.0;
            double normalizedY = (1500 - YValue) / 500.0;

            double x = CenterX - 15 + (normalizedX * Radius);
            double y = CenterY - 15 + (normalizedY * Radius);

            double dx = x + 15 - CenterX;
            double dy = y + 15 - CenterY;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance > Radius)
            {
                double angle = Math.Atan2(dy, dx);
                dx = Radius * Math.Cos(angle);
                dy = Radius * Math.Sin(angle);
                x = CenterX - 15 + dx;
                y = CenterY - 15 + dy;
            }

            Canvas.SetLeft(JoystickKnob, x);
            Canvas.SetTop(JoystickKnob, y);
        }
    }
}
