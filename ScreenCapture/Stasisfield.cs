using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace QIT.ScreenCapture
{
	public partial class Stasisfield : Form
	{
		public Stasisfield( )
		{
			InitializeComponent( );
			GenerateStasisfield( );
		}

		private bool isLoading = false;
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

				isLoading = true;
				return true;
			}
			catch
			{
				return false;
			}

		}

		private bool isDrag = false;

		#region 마우스 클릭 이벤트
		private void Stasisfield_MouseMove( object sender, MouseEventArgs e )
		{
			if ( isDrag )
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
			Post( );
		}
		#endregion

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

		private void Render( )
		{
			// 음... 아마도 내 생각엔 이부분에서 문제가 생길 것 같군요
			while ( true )
			{
				if ( isLoading )
				{
					graphics.Graphics.Clear( Color.Black );

					graphics.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
					graphics.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
					graphics.Graphics.TranslateTransform( this.AutoScrollPosition.X, this.AutoScrollPosition.Y );

					graphics.Graphics.DrawImage( stasisImage, Point.Empty );
					using ( Brush b = new SolidBrush( Color.FromArgb( 128, 255, 255, 255 ) ) )
					{
						graphics.Graphics.FillRectangle( b, new Rectangle( Point.Empty, this.Size ) );
					}

					if ( clickedLocation[0] != EmptyPoint && clickedLocation[1] != EmptyPoint )
					{
						using ( Pen p = new Pen( Color.Red, 1.2f ) )
						{
							graphics.Graphics.DrawRectangle( p, GetSizeFromLocation( clickedLocation[0], clickedLocation[1] ) );
							// 이 부분은 클릭 좌표 조정을 위해 남겨두었어요. 어딘가 쓸떼가 있지 않을까 해서
							//graphics.Graphics.DrawString( string.Format( "down at : {0}\tup at : {1}", clickedLocation[0].ToString( ), clickedLocation[1] ), new Font( "맑은 고딕", 13 ), Brushes.Black, Point.Empty );
						}
					}

					graphics.Render( );
				}

			}
		}

		private void Post( )
		{
			var rect = GetSizeFromLocation(clickedLocation[0], clickedLocation[1]);
			Image origin = (Image)stasisImage.Clone();
			Image cropedImage = new Bitmap(rect.Width, rect.Height);

			using ( Graphics g = Graphics.FromImage( cropedImage ) )
			{
				g.DrawImage(
					origin,
					new Rectangle( Point.Empty, origin.Size ),
					rect,
					GraphicsUnit.Pixel
				);
			}

			using ( frmUpload frm = new frmUpload( ) )
			{
				frm.AutoStart = false;
				frm.Text = "캡쳐 화면 전송중";
				frm.SetImage( cropedImage );
				frm.ShowDialog( );
			}

			this.Close( );
		}
	}
}
