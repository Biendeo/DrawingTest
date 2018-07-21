using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace DrawingTest {
	static class Program {
		static PictureBox pictureBox;
		static System.Timers.Timer updateTimer;
		static Bitmap buffer1;
		static Bitmap buffer2;
		static int currentBuffer;
		static int frameCount;
		static Thread drawThread;
		static bool closing;

		static bool forward;
		static bool backward;
		static bool strafeLeft;
		static bool strafeRight;
		static bool turnLeft;
		static bool turnRight;

		static float playerX;
		static float playerY;
		static float playerRotation;

		static bool AppStillIdle {
			get {
				Message msg;
				return !PeekMessage(out msg, IntPtr.Zero, 0, 0, 0);
			}
		}
		
		//And the declarations for those two native methods members:        
		[StructLayout(LayoutKind.Sequential)]
		public struct Message {
			public IntPtr hWnd;
			public uint msg;
			public IntPtr wParam;
			public IntPtr lParam;
			public uint time;
			public System.Drawing.Point p;
		}

		[System.Security.SuppressUnmanagedCodeSecurity] // We won’t use this maliciously
		[DllImport("User32.dll", CharSet = CharSet.Auto)]
		public static extern bool PeekMessage(out Message msg, IntPtr hWnd, uint messageFilterMin, uint messageFilterMax, uint flags);

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() {
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			closing = false;
			frameCount = 0;
			currentBuffer = 0;
			Form1 form = new Form1();
			form.Text = "Hello!";
			pictureBox = new PictureBox();
			pictureBox.Dock = DockStyle.Fill;
			form.Controls.Add(pictureBox);
			pictureBox.Visible = true;
			pictureBox.Show();
			pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
			form.KeyDown += Form_KeyDown;
			form.KeyUp += Form_KeyUp;
			drawThread = new Thread(() => {
				DateTime lastTime = DateTime.Now;
				DateTime currentTime = DateTime.Now.AddSeconds(1.0);
				while (!closing) {
					double fps = 10000000.0 / (currentTime - lastTime).Ticks;
					Console.WriteLine(fps);
					Update();
					lastTime = currentTime;
					currentTime = DateTime.Now;
				}
			});
			playerX = 50.0f;
			playerY = 50.0f;
			playerRotation = 0.0f;
			forward = false;
			backward = false;
			strafeLeft = false;
			strafeRight = false;
			turnLeft = false;
			turnRight = false;
			buffer1 = new Bitmap(320, 200);
			buffer2 = new Bitmap(320, 200);
			Application.Idle += Application_Idle;
			Application.Run(form);
			closing = true;
			//drawThread.Join();
		}

		private static void Application_Idle(object sender, EventArgs e) {
			DateTime lastTime = DateTime.Now;
			DateTime currentTime = DateTime.Now.AddSeconds(1.0);
			while (AppStillIdle) {
				double fps = 10000000.0 / (currentTime.Ticks - lastTime.Ticks);
				Console.WriteLine(currentTime.Ticks);
				Update();
				Draw();
				lastTime = currentTime;
				currentTime = DateTime.Now;
			}
		}

		private static void Form_KeyDown(object sender, KeyEventArgs e) {
			if (e.KeyCode == Keys.W || e.KeyCode == Keys.Up) {
				forward = true;
			} else if (e.KeyCode == Keys.S || e.KeyCode == Keys.Down) {
				backward = true;
			} else if (e.KeyCode == Keys.A) {
				strafeLeft = true;
			} else if (e.KeyCode == Keys.D) {
				strafeRight = true;
			} else if (e.KeyCode == Keys.Left) {
				turnLeft = true;
			} else if (e.KeyCode == Keys.Right) {
				turnRight = true;
			}
		}

		private static void Form_KeyUp(object sender, KeyEventArgs e) {
			if (e.KeyCode == Keys.W || e.KeyCode == Keys.Up) {
				forward = false;
			} else if (e.KeyCode == Keys.S || e.KeyCode == Keys.Down) {
				backward = false;
			} else if (e.KeyCode == Keys.A) {
				strafeLeft = false;
			} else if (e.KeyCode == Keys.D) {
				strafeRight = false;
			} else if (e.KeyCode == Keys.Left) {
				turnLeft = false;
			} else if (e.KeyCode == Keys.Right) {
				turnRight = false;
			}
		}
		
		private static void Update() {
			const float turnSpeed = 0.5f;
			const float moveSpeed = 0.5f;

			if (turnLeft) {
				playerRotation -= turnSpeed;
			} else if (turnRight) {
				playerRotation += turnSpeed;
			}

			if (playerRotation < -180.0f) {
				playerRotation += 360.0f;
			} else if (playerRotation >= 180.0f) {
				playerRotation -= 360.0f;
			}

			double rotationRads = playerRotation * 2 * Math.PI / 180.0;

			if (forward) {
				playerX += (float)(Math.Cos(rotationRads) * moveSpeed);
				playerY += (float)(Math.Sin(rotationRads) * moveSpeed);
			} else if (backward) {
				playerX -= (float)(Math.Cos(rotationRads) * moveSpeed);
				playerY -= (float)(Math.Sin(rotationRads) * moveSpeed);
			}

			if (strafeLeft) {
				playerX += (float)(Math.Sin(rotationRads) * moveSpeed);
				playerY += (float)(Math.Cos(rotationRads) * moveSpeed);
			} else if (strafeRight) {
				playerX -= (float)(Math.Sin(rotationRads) * moveSpeed);
				playerY -= (float)(Math.Cos(rotationRads) * moveSpeed);
			}

			Console.WriteLine($"Forward: {forward}, Backward: {backward}, StrafeLeft: {strafeLeft}, StrafeRight: {strafeRight}, TurnLeft: {turnLeft}, TurnRight: {turnRight}");
		}

		private static void Draw() {

			Bitmap currentBitmap;

			if (currentBuffer == 0) {
				currentBitmap = buffer1;
			} else {
				currentBitmap = buffer2;
			}
			Color frameColor = Color.FromArgb(frameCount % 256, frameCount % 256, frameCount % 256);
			using (var g = Graphics.FromImage(currentBitmap)) {
				g.FillRectangle(new SolidBrush(frameColor), new Rectangle(0, 0, 320, 200));
				// Player line is four points:
				double rotationRads = playerRotation * 2 * Math.PI / 180.0;
				Point frontPoint = new Point((int)(playerX + 5 * Math.Cos(rotationRads)), (int)(playerY + 5 * Math.Sin(rotationRads)));
				Point backPoint = new Point((int)(playerX - 5 * Math.Cos(rotationRads)), (int)(playerY - 5 * Math.Sin(rotationRads)));
				Point leftPoint = new Point((int)(playerX + 5 * Math.Sin(rotationRads)), (int)(playerY - 5 * Math.Cos(rotationRads)));
				Point rightPoint = new Point((int)(playerX - 5 * Math.Sin(rotationRads)), (int)(playerY + 5 * Math.Cos(rotationRads)));
				g.DrawLine(new Pen(Color.Purple, 1.0f), backPoint, frontPoint);
				g.DrawLine(new Pen(Color.Purple, 1.0f), leftPoint, frontPoint);
				g.DrawLine(new Pen(Color.Purple, 1.0f), rightPoint, frontPoint);
			}

			pictureBox.Image = currentBitmap;

			currentBuffer = (currentBuffer + 1) % 2;
			++frameCount;
		}
	}
}
