using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonoMac.AppKit;
using MonoMac.Foundation;

namespace WatchThis
{
	public partial class ShowList : MonoMac.AppKit.NSWindow
	{
#region Constructors

		// Called when created from unmanaged code
		public ShowList(IntPtr handle) : base(handle)
		{
			Initialize();
		}
		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public ShowList(NSCoder coder) : base(coder)
		{
			Initialize();
		}
		// Shared initialization code
		void Initialize()
		{
		}

#endregion

		public Action<IList<string>> DropAction { get; set; }

		[Export ("draggingEntered:")]
		public NSDragOperation DraggingEntered(NSDraggingInfo sender)
		{
			var list = FilePaths(sender);
			return (DropAction != null && list.Count > 0) ? NSDragOperation.All : NSDragOperation.None;
		}

		[Export ("performDragOperation:")]
		bool PerformDragOperation(NSDraggingInfo sender)
		{
			if (DropAction != null)
			{
				var list = FilePaths(sender);
				DropAction(list);
				return true;
			}
			return false;
		}

		IList<string> FilePaths(NSDraggingInfo dragInfo)
		{
			var list = new List<string>();
			if (dragInfo.DraggingPasteboard.Types.Contains(NSPasteboard.NSFilenamesType))
			{
				NSArray data = dragInfo.DraggingPasteboard.GetPropertyListForType(NSPasteboard.NSFilenamesType) as NSArray;
				if (data != null)
				{
					for (uint idx = 0; idx < data.Count; ++idx)
					{
						string path = NSString.FromHandle(data.ValueAt(idx));
						if (Directory.Exists(path))
						{
							list.Add(path);
						}
					}
				}
			}

			return list;
		}
	}
}

