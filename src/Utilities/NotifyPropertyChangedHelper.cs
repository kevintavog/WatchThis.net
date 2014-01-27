using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;

namespace WatchThis.Utilities
{
	// From http://stackoverflow.com/questions/7597761/reflecting-across-serialized-object-to-set-propertychanged-event
	public static class NotifyPropertyChangedHelper
	{
		public delegate void ChangeOccuredHandler(object sender, string propertyName);

		public static void SetupPropertyChanged(INotifyPropertyChanged component, ChangeOccuredHandler changedHandler)
		{
			SetupPropertyChanged(new List<object>(), component, changedHandler);
		}

		static void SetupPropertyChanged(IList<object> closed, INotifyPropertyChanged component, ChangeOccuredHandler changedHandler)
		{
			if (closed.Contains(component)) return; // event was already registered

			closed.Add(component); //adds the property that is to be processed

			//sets the property changed event if the property isn't a collection
			if (!(component is INotifyCollectionChanged))
				component.PropertyChanged += (sender, e) => changedHandler(sender, e.PropertyName);

			/*			
         * If the component is an enumerable there are two steps. First check to see if it supports the INotifyCollectionChanged event.
         * If it supports it add and handler on to this object to support notification.  Next iterate through the collection of objects
         * to add hook up their PropertyChangedEvent.
         * 
         * If the component isn't a collection then iterate through its properties and attach the changed handler to the properties.
         */
			if (component is IEnumerable<object>)
			{
				if (component is INotifyCollectionChanged)
				{
					//((INotifyCollectionChanged)component).CollectionChanged += collectionHandler;
					((INotifyCollectionChanged)component).CollectionChanged += (sender, e) => changedHandler(sender, "collection");
				}

				foreach (object obj in component as IEnumerable<object>)
				{
					if (obj is INotifyPropertyChanged)
						SetupPropertyChanged(closed, (INotifyPropertyChanged)obj, changedHandler);
				}
			}
			else
			{
				foreach (PropertyInfo info in component.GetType().GetProperties())
				{
					var propertyValue = info.GetValue(component, new object[] { });
					var inpc = propertyValue as INotifyPropertyChanged;
					if (inpc == null) continue;
					SetupPropertyChanged(closed, inpc, changedHandler);
				}
			}
		}
	}

}

