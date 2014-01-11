using System;

namespace WatchThis.Controllers
{
	public interface IPlatformService
	{
		void InvokeOnUiThread(Action action);
	}
}

