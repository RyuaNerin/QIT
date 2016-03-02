using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;

namespace TiX.ScreenCapture
{
	public partial class Stasisfield : Form
	{
		/// <summary>
		/// 이 멋진 함수는 화면 캡쳐를 위한 정지장을 생성합니다.
		/// 숙주 폼이 생성되는 시점에서 호출되어야 합니다.
		/// </summary>
		/// <returns>작업 성공 여부</returns>
		public Stasisfield( )
		{
			InitializeComponent( );

			this.m_screenRect = SystemInformation.VirtualScreen;

			// 폼 초기화
			this.Size = this.m_screenRect.Size;

			// 이미지 캡쳐
			m_capture = new Bitmap( this.m_screenRect.Width, this.m_screenRect.Height, PixelFormat.Format24bppRgb );
			using ( Graphics g = Graphics.FromImage( m_capture ) )
			{
				g.CopyFromScreen( this.m_screenRect.Location, Point.Empty, this.m_screenRect.Size );
			}

			// 배경용 이미지 생성.
			m_captureBlur = new Bitmap( this.m_screenRect.Width, this.m_screenRect.Height, PixelFormat.Format32bppArgb );
			using ( Graphics g = Graphics.FromImage( m_captureBlur ) )
			{
				g.DrawImageUnscaledAndClipped( m_capture, new Rectangle( 0, 0, this.m_screenRect.Width, this.m_screenRect.Height ) );

				// 뿌연 효과 적용
				using ( Brush b = new SolidBrush( Color.FromArgb( 128, 255, 255, 255 ) ) )
				{
					g.FillRectangle( b, new Rectangle( Point.Empty, this.Size ) );
				}
			}

			// 폼에 적용
			this.BackgroundImage = m_captureBlur;

			// 클릭 좌표 초기화
			m_location[0] = m_location[1] = EmptyPoint;
		}

		public Stasisfield( string TargetUserID, string TargetTweetID ) : this( )
		{
			this.TargetTweetID = TargetTweetID;
			this.TargetUserID = TargetUserID;
		}

		private static readonly Point EmptyPoint = new Point(-1, -1); 

		private Rectangle   m_screenRect;
		private bool        m_done          = false;
		private bool        m_drag          = false;
		private Image       m_captureBlur   = null;                     // 정지장이 생성되는 시점의 화면 화상입니다.
		private Image       m_capture       = null;                     // 정지장이 생성되는 시점의 화면 화상입니다.
		private Point[]     m_location      = new Point[2];             // 마우스 다운 및 업 이벤트 발생 좌표

		public string TargetUserID  { get; private set; } 
		public string TargetTweetID { get; private set; } 
        public Image  CropedImage   { get; private set; }

		private void Stasisfield_FormClosed( object sender, FormClosedEventArgs e )
		{
			this.m_captureBlur.Dispose( );
			this.m_capture.Dispose( );
		}

		private void Stasisfield_Shown( object sender, EventArgs e )
		{
			this.Location = new Point( this.m_screenRect.Left, this.m_screenRect.Top );
		}

		private void Stasisfield_FormClosing( object sender, FormClosingEventArgs e )
		{
			if ( m_done )
			{
				this.Hide( );

				var src = GetSizeFromLocation(m_location[0], m_location[1]);
				if ( CropAvailable( m_location[0], m_location[1], src ) )
				{
                    this.CropedImage = CropImage(m_capture, src);
				}
			}
		}


		#region 마우스 클릭 이벤트
		private void Stasisfield_MouseMove( object sender, MouseEventArgs e )
		{
			if ( m_drag )
			{
				m_location[1] = e.Location;
				this.Invalidate( );
            }
		}
		private void Stasisfield_MouseDown( object sender, MouseEventArgs e )
		{
			m_location[0] = e.Location;
			m_location[1] = EmptyPoint;
			m_drag = true;
		}
		private void Stasisfield_MouseUp( object sender, MouseEventArgs e )
		{
            if (m_drag)
            {
			    m_drag = false;
			    m_done = true;
			    this.Close( );
            }
		}
		private void Stasisfield_KeyDown( object sender, KeyEventArgs e )
		{
			if ( e.KeyCode == Keys.Escape )
			{
				this.m_done = false;
				this.m_drag = false;
				this.Close( );
			}
		}
		#endregion

		protected override void OnPaint( PaintEventArgs e )
		{
            base.OnPaint(e);

			var src = GetSizeFromLocation(m_location[0], m_location[1]);

			if ( CropAvailable( m_location[0], m_location[1], src ) )
			{
				// 이미지 밝게 다시 그리기
				var cropedImage = CropImage(m_capture, src);
				e.Graphics.DrawImage( cropedImage, src );

				// 빨간펜 선생님
				e.Graphics.DrawRectangle(Pens.Red, GetSizeFromLocation( m_location[0], m_location[1] ) );

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
				e.Graphics.DrawString(string.Format("{0}x{1} - {2}x{3}", src.Left, src.Top, src.Right, src.Bottom), this.Font, Brushes.Black, Point.Empty);
			}
		}

		/// <summary>
		/// 두 개의 좌표로 Rectangle객체를 구합니다.
		/// 원래 인터넷에서 긁어와서 쓰려했는데 쉬발롬이 이상한걸 올려놔서 뭐가 문젠지 한참 고민했네
		/// 노가다 하지마세요 사긔님 ;ㅅ;
		/// </summary>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <returns></returns>
		private Rectangle GetSizeFromLocation( Point p1, Point p2 )
		{
			return new Rectangle(
				Math.Min( p1.X, p2.X ),
				Math.Min( p1.Y, p2.Y ),
				Math.Abs( p1.X - p2.X ),
				Math.Abs( p1.Y - p2.Y ) );
		}

		/// <summary>
		/// 유효한 CropImage를 생성할 수 있는 상태인지 검사합니다.
		/// </summary>
		/// <param name="p1">MouseDown 이벤트가 발생한 위치입니다.</param>
		/// <param name="p2">MouseUp 이벤트가 발생한 위치입니다.</param>
		/// <param name="src">이미지 크기 사각형</param>
		/// <returns></returns>
		private bool CropAvailable( Point p1, Point p2, Rectangle src )
		{
			if ( p1 != EmptyPoint && p2 != EmptyPoint && src.Width > 0 && src.Height > 0 )
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
		private Image CropImage( Image image, Rectangle src )
		{
			var dest = new Rectangle(0, 0, src.Width, src.Height);

			Image cropedImage = new Bitmap(src.Width, src.Height);
			using ( Graphics g = Graphics.FromImage( cropedImage ) )
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
	}
}
