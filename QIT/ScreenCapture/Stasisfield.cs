using System;
using System.Drawing;
using System.Drawing.Imaging;
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

            this.m_rect = SystemInformation.VirtualScreen;

            // 폼 초기화
            this.Size = this.m_rect.Size;

            // 이미지 캡쳐
            stasisImage = new Bitmap(this.m_rect.Width, this.m_rect.Height, PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(stasisImage))
            {
                g.CopyFromScreen(this.m_rect.Location, Point.Empty, this.m_rect.Size);
            }

            // 배경용 이미지 생성.
            stasisImageBlur = new Bitmap(this.m_rect.Width, this.m_rect.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(stasisImageBlur))
            {
                g.DrawImageUnscaledAndClipped(stasisImage, new Rectangle(0, 0, this.m_rect.Width, this.m_rect.Height));

                // 뿌연 효과 적용
                using (Brush b = new SolidBrush(Color.FromArgb(128, 255, 255, 255)))
                {
                    g.FillRectangle(b, new Rectangle(Point.Empty, this.Size));
                }
            }

            // 폼에 적용
            this.BackgroundImage = stasisImageBlur;


            // 클릭 좌표 초기화
            clickedLocation[0] = clickedLocation[1] = EmptyPoint;
		}

        private Rectangle m_rect;
		private bool isDone = false;
		private bool isDrag = false;
        private Image stasisImageBlur = null; // 정지장이 생성되는 시점의 화면 화상입니다.
        private Image stasisImage = null; // 정지장이 생성되는 시점의 화면 화상입니다.
		private Point[] clickedLocation = new Point[2]; // 마우스 다운 및 업 이벤트 발생 좌표
        private Point EmptyPoint { get { return new Point(-1, -1); } }
        private Pen p = new Pen(Color.Red, 1.0f);

        private void Stasisfield_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.stasisImageBlur.Dispose();
            this.stasisImage.Dispose();            
        }

		private void Stasisfield_Shown( object sender, EventArgs e )
		{
			this.Location = new Point(this.m_rect.Left, this.m_rect.Top);
		}

		void Stasisfield_FormClosing( object sender, FormClosingEventArgs e )
		{
			if ( isDone )
			{
				this.Hide( );

				var src = GetSizeFromLocation(clickedLocation[0], clickedLocation[1]);
				if ( CropAvailable( clickedLocation[0], clickedLocation[1], src ) )
				{
					var cropedImage = CropImage(stasisImage, src);
					Post( cropedImage );
				}
			}
		}

		#region 마우스 클릭 이벤트
		private void Stasisfield_MouseMove( object sender, MouseEventArgs e )
		{
			if ( isDrag )
			{
				clickedLocation[1] = e.Location;
                this.Invalidate();
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
			this.Close( );
		}
		private void Stasisfield_KeyDown( object sender, KeyEventArgs e )
		{
			if ( e.KeyCode == Keys.Escape )
			{
				this.isDone = false;
				this.isDrag = false;
				this.Close( );
			}
		}
		#endregion

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var src = GetSizeFromLocation(clickedLocation[0], clickedLocation[1]);
            if (CropAvailable(clickedLocation[0], clickedLocation[1], src))
            {
                // 이미지 밝게 다시 그리기
                var cropedImage = CropImage(stasisImage, src);
                e.Graphics.DrawImage(cropedImage, src);

                // 빨간펜 선생님
                e.Graphics.DrawRectangle(p, GetSizeFromLocation(clickedLocation[0], clickedLocation[1]));
                // 이 부분은 클릭 좌표 조정을 위해 남겨두었어요. 어딘가 쓸떼가 있지 않을까 해서
                e.Graphics.DrawString( string.Format( "down at : {0}\tup at : {1}", clickedLocation[0].ToString( ), clickedLocation[1] ), new Font( "맑은 고딕", 13 ), Brushes.Black, Point.Empty );
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
                Math.Min(p1.X, p2.X),
                Math.Min(p1.Y, p2.Y),
                Math.Abs(p1.X - p2.X),
                Math.Abs(p1.Y - p2.Y));
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

		/// <summary>
		/// 이미지를 트윗합니다.
		/// </summary>
		/// <param name="image">트윗할 이미지</param>
		private void Post( Image image )
		{
			TweetModerator.Tweet( image, "캡처 화면 전송중" );
		}
		
	}
}
