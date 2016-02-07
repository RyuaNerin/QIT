using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Quicx.ScreenCapture
{
	public partial class Stasisfield : Form
	{
		public Stasisfield( )
		{
			InitializeComponent( );
            if (!GenerateStasisfield()) this.Close();
		}

		private bool isLoading = false;
        private bool isDone = false;
        private bool isDrag = false;
		private Image stasisImage = null; // 정지장이 생성되는 시점의 화면 화상입니다.
		private Point[] clickedLocation = new Point[2]; // 마우스 다운 및 업 이벤트 발생 좌표
		private Point EmptyPoint { get { return new Point( -1, -1 ); } }

		BufferedGraphics graphics;
		BufferedGraphicsContext graphicsContext;
		Thread RenderThread;

		/// <summary>
		/// 이 멋진 함수는 화면 캡쳐를 위한 정지장을 생성합니다.
		/// 숙주 폼이 생성되는 시점에서 호출되어야 합니다.
		/// </summary>
		/// <returns>작업 성공 여부</returns>
		public bool GenerateStasisfield( )
		{
			try
			{
				// 폼 초기화
				this.FormBorderStyle = FormBorderStyle.None;
				this.Location = new Point( 0, 0 );
				// 다중 화면 캡쳐 지원을 위해 이 부분을 수정합니다. (지금 말고)
				this.Size = Screen.PrimaryScreen.Bounds.Size;

				// 그래픽스 객체 초기화
				graphicsContext = BufferedGraphicsManager.Current;
				graphicsContext.MaximumBuffer = this.Size;
				graphics = graphicsContext.Allocate( this.CreateGraphics( ), new Rectangle( Point.Empty, this.Size ) );

				// 이미지 캡쳐
				stasisImage = new Bitmap( this.Size.Width, this.Size.Height );
				using ( Graphics g = Graphics.FromImage( stasisImage ) )
				{
					g.CopyFromScreen( Point.Empty, Point.Empty, Screen.PrimaryScreen.Bounds.Size );
				}

				// 클릭 좌표 초기화
				clickedLocation[0] = clickedLocation[1] = EmptyPoint;

				// 클릭 이벤트 연결
				this.MouseDown += Stasisfield_MouseDown;
				this.MouseUp += Stasisfield_MouseUp;
				this.MouseMove += Stasisfield_MouseMove;

				// 렌더링 스레드 초기화
				RenderThread = new Thread( Render );
				RenderThread.Start( );

                // 폼 닫히기 전 이벤트 정의
                this.FormClosing += Stasisfield_FormClosing;

                // 커서 변경
                this.Cursor = Cursors.Cross;

				isLoading = true;
				return true;
			}
			catch
			{
				return false;
			}

		}

        void Stasisfield_FormClosing(object sender, FormClosingEventArgs e)
        {
            RenderThread.Abort();
            if (isDone)
            {
                this.ShowInTaskbar = false;
                this.Hide();

                var src = GetSizeFromLocation(clickedLocation[0], clickedLocation[1]);
                if (CropAvailable(clickedLocation[0], clickedLocation[1], src))
                {
                    var cropedImage = CropImage(stasisImage, src);
                    Post(cropedImage);
                }
            }
        }

		#region 마우스 클릭 이벤트
		private void Stasisfield_MouseMove( object sender, MouseEventArgs e )
        {
            if (isDrag)
            {
                clickedLocation[1] = e.Location;
            }
        }
		private void Stasisfield_MouseDown( object sender, MouseEventArgs e )
        {
            clickedLocation[0] = e.Location;
            clickedLocation[1] = EmptyPoint;
            isDrag = true;
        }
		private void Stasisfield_MouseUp( object sender, MouseEventArgs e )
		{
			isDrag = false;
            isDone = true;
            this.Close();
		}
		#endregion

		private void Render( )
		{
			while ( true )
			{
				if ( isLoading )
				{
					graphics.Graphics.Clear( Color.Black );

                    graphics.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
                    graphics.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    graphics.Graphics.TranslateTransform(this.AutoScrollPosition.X, this.AutoScrollPosition.Y);

                    using (Image copy = (Image)stasisImage.Clone())
                    {
                        // 이미지 그리기
                        graphics.Graphics.DrawImage(copy, Point.Empty);

                        // 뿌연 오버레이
                        using (Brush b = new SolidBrush(Color.FromArgb(128, 255, 255, 255)))
                        {
                            graphics.Graphics.FillRectangle(b, new Rectangle(Point.Empty, this.Size));
                        }

                        var src = GetSizeFromLocation(clickedLocation[0], clickedLocation[1]);
                        if (CropAvailable(clickedLocation[0], clickedLocation[1], src))
                        {
                            // 이미지 밝게 다시 그리기
                            var cropedImage = CropImage(stasisImage, src);
                            graphics.Graphics.DrawImage(cropedImage, src);

                            // 빨간펜 선생님
						    using ( Pen p = new Pen( Color.Red, 1.2f ) )
						    {
							    graphics.Graphics.DrawRectangle( p, GetSizeFromLocation( clickedLocation[0], clickedLocation[1] ) );
							    // 이 부분은 클릭 좌표 조정을 위해 남겨두었어요. 어딘가 쓸떼가 있지 않을까 해서
							    //graphics.Graphics.DrawString( string.Format( "down at : {0}\tup at : {1}", clickedLocation[0].ToString( ), clickedLocation[1] ), new Font( "맑은 고딕", 13 ), Brushes.Black, Point.Empty );
						    }
					    }
                    }

					graphics.Render( );
				}

			}
		}

		/// <summary>
		/// 두 개의 좌표로 Rectangle객체를 구합니다.
		/// 원래 인터넷에서 긁어와서 쓰려했는데 쉬발롬이 이상한걸 올려놔서 뭐가 문젠지 한참 고민했네
		/// </summary>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <returns></returns>
		private Rectangle GetSizeFromLocation( Point p1, Point p2 )
		{
			int smallX;
			int smallY;
			int bigX;
			int bigY;

			if ( p1.X < p2.X )
			{
				smallX = p1.X;
				bigX = p2.X;
			}
			else
			{
				smallX = p2.X;
				bigX = p1.X;
			}
			if ( p1.Y < p2.Y )
			{
				smallY = p1.Y;
				bigY = p2.Y;
			}
			else
			{
				smallY = p2.Y;
				bigY = p1.Y;
			}

			int width = bigX - smallX;
			int height = bigY - smallY;

			return new Rectangle( new Point( smallX, smallY ), new Size( width, height ) );
		}

        /// <summary>
        /// 유효한 CropImage를 생성할 수 있는 상태인지 검사합니다.
        /// </summary>
        /// <param name="p1">MouseDown 이벤트가 발생한 위치입니다.</param>
        /// <param name="p2">MouseUp 이벤트가 발생한 위치입니다.</param>
        /// <param name="src">이미지 크기 사각형</param>
        /// <returns></returns>
        private bool CropAvailable(Point p1, Point p2, Rectangle src)
        {
            if (p1 != EmptyPoint && p2 != EmptyPoint && src.Width > 0 && src.Height > 0)
                return true;
            else
                return false;
        }

        /// <summary>
        /// 이미지를 잘라요
        /// </summary>
        /// <param name="image">원본 이미지</param>
        /// <param name="src">자를 크기</param>
        /// <returns></returns>
        private Image CropImage(Image image, Rectangle src)
        {
            var dest = new Rectangle(0, 0, src.Width, src.Height);

            Image cropedImage = new Bitmap(src.Width, src.Height);
            using (Graphics g = Graphics.FromImage(cropedImage))
            {
                g.DrawImage(
                    image,
                    dest,
                    src,
                    GraphicsUnit.Pixel
                );
            }

            return cropedImage;
        }

        /// <summary>
        /// 이미지를 트윗합니다.
        /// </summary>
        /// <param name="image">트윗할 이미지</param>
		private void Post(Image image)
		{
			using ( frmUpload frm = new frmUpload( ) )
			{
				frm.AutoStart = false;
				frm.Text = "캡쳐 화면 전송중";
                //frm.SetText(string.Format("dest : {0}\nsrc : {1}", dest.ToString(), src.ToString()));
                frm.SetImage(image);
				frm.ShowDialog( );
            }
         }
	}
}
