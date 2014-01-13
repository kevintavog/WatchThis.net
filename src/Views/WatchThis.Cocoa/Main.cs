using System;
using System.Drawing;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;

namespace WatchThis
{

	class MainClass
	{
		static void Main(string[] args)
		{
			System.Threading.Thread.CurrentThread.Name = "Main";
			NSApplication.Init();
			NSApplication.Main(args);
		}
	}
}	

