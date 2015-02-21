using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Altus.UI.Controls
{
    /// <summary>
    /// Interaction logic for SmallGripper.xaml
    /// </summary>
    public partial class SmallGripper : UserControl
    {
        public SmallGripper()
        {
            InitializeComponent();
        }

        public static readonly RoutedEvent MovingEvent =
           EventManager.RegisterRoutedEvent("MovingEvent", RoutingStrategy.Bubble, typeof(MovingEventHandler), typeof(SmallGripper));
        public event MovingEventHandler Moving
        {
            add { AddHandler(MovingEvent, value); }
            remove { RemoveHandler(MovingEvent, value); }
        }

        bool _isDragging = false;
        Point _Point;
        private void Grid_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging
                && e.ChangedButton == MouseButton.Left
                && e.LeftButton.HasFlag(MouseButtonState.Pressed))
            {
                this.GripperButton.CaptureMouse();
                _isDragging = true;
                _Point = e.GetPosition(this);
            }
        }

        private void Grid_MouseMove_1(object sender, MouseEventArgs e)
        {
            if (_isDragging
                && e.LeftButton.HasFlag(MouseButtonState.Pressed))
            {
                Point newPoint = e.GetPosition(this.GripperButton);
                Point offset = new Point();
                offset.X += newPoint.X - _Point.X;
                offset.Y += newPoint.Y - _Point.Y;
                RaiseMovingEvent(offset);
            }
        }

        private void Grid_MouseUp_1(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging
                && e.ChangedButton == MouseButton.Left)
            {
                this.GripperButton.ReleaseMouseCapture();
                _isDragging = false;
            }
        }

        protected void RaiseMovingEvent(Point offset)
        {
            MovingEventArgs e = new MovingEventArgs(MovingEvent, offset);
            RaiseEvent(e);
        }
    }

    public delegate void MovingEventHandler(object sender, MovingEventArgs e);
    public class MovingEventArgs : RoutedEventArgs
    {
        public MovingEventArgs(RoutedEvent theEvent, Point offset)
            : base(theEvent)
        {
            Offset = offset;
        }

        public Point Offset { get; private set; }
    }
}
