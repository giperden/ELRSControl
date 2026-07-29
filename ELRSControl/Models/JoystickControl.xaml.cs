using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ELRSControl.Models
{
    public partial class JoystickControl : UserControl, INotifyPropertyChanged
    {
        private bool _isMouseDown = false;
        private int _centerX = 100;
        private int _centerY = 100;
        private int _radius = 75;

        private int _xValue = 1500;
        private int _yValue = 1500;

        public int XValue
        {
            get => _xValue;
            set { _xValue = value; OnPropertyChanged(); UpdateKnobPosition(); }
        }

        public int YValue
        {
            get => _yValue;
            set { _yValue = value; OnPropertyChanged(); UpdateKnobPosition(); }
        }

        public JoystickControl()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private void JoystickCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isMouseDown = true;
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
        }

        private void JoystickCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            _isMouseDown = false;
        }

        private void UpdateJoystickPosition(Point mousePos)
        {
            double dx = mousePos.X - _centerX;
            double dy = mousePos.Y - _centerY;
            double distance = System.Math.Sqrt(dx * dx + dy * dy);
            if (distance > _radius)
            {
                double angle = System.Math.Atan2(dy, dx);
                dx = _radius * System.Math.Cos(angle);
                dy = _radius * System.Math.Sin(angle);
            }
            XValue = (int)(1500 + (dx / _radius) * 500);
            YValue = (int)(1500 - (dy / _radius) * 500);
            XValue = System.Math.Max(1000, System.Math.Min(2000, XValue));
            YValue = System.Math.Max(1000, System.Math.Min(2000, YValue));
        }

        private void UpdateKnobPosition()
        {
            double normalizedX = (XValue - 1500) / 500.0;
            double normalizedY = (1500 - YValue) / 500.0;

            double x = _centerX - 15 + (normalizedX * _radius);
            double y = _centerY - 15 + (normalizedY * _radius);
            double dx = x + 15 - _centerX;
            double dy = y + 15 - _centerY;
            double distance = System.Math.Sqrt(dx * dx + dy * dy);

            if (distance > _radius)
            {
                double angle = System.Math.Atan2(dy, dx);
                dx = _radius * System.Math.Cos(angle);
                dy = _radius * System.Math.Sin(angle);
                x = _centerX - 15 + dx;
                y = _centerY - 15 + dy;
            }

            Canvas.SetLeft(JoystickKnob, x);
            Canvas.SetTop(JoystickKnob, y);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
