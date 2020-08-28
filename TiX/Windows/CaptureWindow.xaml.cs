using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TiX.Windows
{
    public partial class CaptureWindow : Window
    {
        private static readonly Point EmptyPoint = new Point(-1, -1);

        public CaptureWindow()
        {
            this.InitializeComponent();

            this.Left = SystemParameters.VirtualScreenLeft;
            this.Top = SystemParameters.VirtualScreenTop;
            this.Width = SystemParameters.VirtualScreenWidth;
            this.Height = SystemParameters.VirtualScreenHeight;
        }

        public System.Drawing.Image CropedImage { get; private set; }

        private bool m_drag = false;

        private Point m_dragStart = EmptyPoint;
        private Point m_dragCurrent = EmptyPoint;
        private Rect m_rect = Rect.Empty;

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.m_drag = true;
                this.m_dragStart = e.GetPosition(null);
                this.UpdateRefct(this.m_dragStart);

                this.RegionRect.Visibility = Visibility.Visible;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (e.LeftButton == MouseButtonState.Pressed && this.m_drag)
            {
                this.UpdateRefct(e.GetPosition(null));
            }
        }

        private void UpdateRefct(Point cur)
        {
            this.m_dragCurrent = cur;
            this.m_rect = new Rect(
                Math.Min(this.m_dragStart.X, this.m_dragCurrent.X),
                Math.Min(this.m_dragStart.Y, this.m_dragCurrent.Y),
                Math.Abs(this.m_dragStart.X - this.m_dragCurrent.X),
                Math.Abs(this.m_dragStart.Y - this.m_dragCurrent.Y)
            );

            this.RegionRect.RenderSize = this.m_rect.Size;

            Canvas.SetLeft(this.RegionRect, this.m_rect.Left);
            Canvas.SetTop(this.RegionRect, this.m_rect.Top);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            var p = e.GetPosition(null);

            if (e.ChangedButton == MouseButton.Left && this.m_drag)
            {
                this.UpdateRefct(p);
                this.m_drag = false;

                this.RegionRect.Visibility = Visibility.Hidden;

                if (this.m_rect.Height > 0 && this.m_rect.Width > 0)
                {
                    this.CropedImage = new System.Drawing.Bitmap(
                        (int)Math.Floor(this.m_rect.Width),
                        (int)Math.Floor(this.m_rect.Height),
                        System.Drawing.Imaging.PixelFormat.Format24bppRgb
                    );

                    using (var g = System.Drawing.Graphics.FromImage(this.CropedImage))
                    {
                        g.CopyFromScreen(
                            (int)Math.Floor(this.m_rect.Left), (int)Math.Floor(this.m_rect.Top),
                            0, 0,
                            this.CropedImage.Size,
                            System.Drawing.CopyPixelOperation.SourceCopy);

                        this.DialogResult = true;
                    }
                }

                this.Close();
            }


            if (!this.m_drag)
            {
                this.RegionText.Text = string.Format(
                    "{0:#,##0}x{1:#,##0}",
                    (int)Math.Floor(p.X),
                    (int)Math.Floor(p.Y)
                );
            }
            else
            {
                this.RegionText.Text = string.Format(
                        "{0:#,##0}x{1:#,##0} - {2:#,##0}x{3:#,##0}",
                        (int)Math.Floor(this.m_dragStart.X),
                        (int)Math.Floor(this.m_dragStart.Y),
                        (int)Math.Floor(p.X),
                        (int)Math.Floor(p.Y)
                    );
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (e.Key == Key.Escape)
            {
                if (this.m_drag)
                {
                    this.m_drag = false;
                    this.RegionRect.Visibility = Visibility.Hidden;
                }
                else
                {
                    this.Close();
                }
            }
        }
    }
}
