using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kogel.Record.Extension
{
	/// <summary>
	/// 系统接口
	/// </summary>
	public class WindowApi
	{
		//寻找目标进程窗口       
		[DllImport("USER32.DLL")]
		public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
		//设置进程窗口到最前       
		[DllImport("USER32.DLL")]
		public static extern bool SetForegroundWindow(IntPtr hWnd);

		#region GetWindowCapture的dll引用
		[DllImport("user32.dll")]
		private static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rectangle rect);

		[DllImport("gdi32.dll")]
		private static extern IntPtr CreateCompatibleDC(
		 IntPtr hdc // handle to DC
		 );
		[DllImport("gdi32.dll")]
		private static extern IntPtr CreateCompatibleBitmap(
		 IntPtr hdc,         // handle to DC
		 int nWidth,      // width of bitmap, in pixels
		 int nHeight      // height of bitmap, in pixels
		 );
		[DllImport("gdi32.dll")]
		private static extern IntPtr SelectObject(
		 IntPtr hdc,           // handle to DC
		 IntPtr hgdiobj    // handle to object
		 );
		[DllImport("gdi32.dll")]
		private static extern int DeleteDC(
		 IntPtr hdc           // handle to DC
		 );
		[DllImport("user32.dll")]
		private static extern bool PrintWindow(
		 IntPtr hwnd,                // Window to copy,Handle to the window that will be copied.
		 IntPtr hdcBlt,              // HDC to print into,Handle to the device context.
		 UInt32 nFlags               // Optional flags,Specifies the drawing options. It can be one of the following values.
		 );
		[DllImport("user32.dll")]
		private static extern IntPtr GetWindowDC(
		 IntPtr hwnd
		 );
		#endregion
		/// <summary>
		/// 根据句柄获取截图
		/// </summary>
		/// <param name="hWnd"></param>
		/// <returns></returns>
		public static Bitmap GetWindowCapture(IntPtr hWnd)
		{
			IntPtr hscrdc = GetWindowDC(hWnd);
			Rectangle windowRect = new Rectangle();
			GetWindowRect(hWnd, ref windowRect);
			int width = Math.Abs(windowRect.X - windowRect.Width);
			int height = Math.Abs(windowRect.Y - windowRect.Height);
			IntPtr hbitmap = CreateCompatibleBitmap(hscrdc, width, height);
			IntPtr hmemdc = CreateCompatibleDC(hscrdc);
			SelectObject(hmemdc, hbitmap);
			PrintWindow(hWnd, hmemdc, 0);
			Bitmap bmp = Image.FromHbitmap(hbitmap);
			DeleteDC(hscrdc);//删除用过的对象
			DeleteDC(hmemdc);//删除用过的对象
			return bmp;
		}
		/// <summary>
		/// 根据句柄获取截图路径
		/// </summary>
		/// <param name="hWnd"></param>
		/// <returns></returns>
		public static string GetCapturePath(IntPtr hWnd)
		{
			string path = string.Empty;
			string dicPath = AppDomain.CurrentDomain.BaseDirectory + "Intercept";
			if (!Directory.Exists(dicPath))
			{
				Directory.CreateDirectory(dicPath);
			}
			using (Bitmap bitmap = GetWindowCapture(hWnd))
			{
				path = dicPath + "\\" + Guid.NewGuid().ToString().Replace("-", "") + ".png";
				bitmap.Save(path);
			}
			return path;
		}


		private delegate bool WNDENUMPROC(IntPtr hWnd, int lParam);

		//用来遍历所有窗口 
		[DllImport("user32.dll")]
		private static extern bool EnumWindows(WNDENUMPROC lpEnumFunc, int lParam);

		//获取窗口Text 
		[DllImport("user32.dll")]
		private static extern int GetWindowTextW(IntPtr hWnd, [MarshalAs(UnmanagedType.LPWStr)]StringBuilder lpString, int nMaxCount);

		//获取窗口类名 
		[DllImport("user32.dll")]
		private static extern int GetClassNameW(IntPtr hWnd, [MarshalAs(UnmanagedType.LPWStr)]StringBuilder lpString, int nMaxCount);

		//自定义一个类，用来保存句柄信息，在遍历的时候，随便也用空上句柄来获取些信息，呵呵 
		public struct WindowInfo
		{
			public IntPtr hWnd;
			public string szWindowName;
			public string szClassName;
		}

		public static WindowInfo[] GetAllDesktopWindows()
		{
			//用来保存窗口对象 列表
			List<WindowInfo> wndList = new List<WindowInfo>();

			//enum all desktop windows 
			EnumWindows(delegate (IntPtr hWnd, int lParam)
			{
				WindowInfo wnd = new WindowInfo();
				StringBuilder sb = new StringBuilder(256);

				//get hwnd 
				wnd.hWnd = hWnd;

				//get window name  
				GetWindowTextW(hWnd, sb, sb.Capacity);
				wnd.szWindowName = sb.ToString();

				//get window class 
				GetClassNameW(hWnd, sb, sb.Capacity);
				wnd.szClassName = sb.ToString();

				//add it into list 
				wndList.Add(wnd);
				return true;
			}, 0);

			return wndList.ToArray();
		}
	}
}
