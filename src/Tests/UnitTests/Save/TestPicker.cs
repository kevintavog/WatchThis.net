using System;
using System.IO;
using System.Threading.Tasks;
using Kekiri;
using NUnit.Framework;
using WatchThis.Controllers;
using WatchThis.Models;

namespace UnitTests
{
	public class TestPicker : ISlideshowPickerViewer, IPlatformService
	{
		public string LastQuestion { get; private set; }
		public QuestionResponseType QuestionResponse { get; set; }

		static TestPicker()
		{
			Preferences.Load("not a real file");
		}

		public void InvokeOnUiThread(Action action)
		{
			Task.Factory.StartNew( () => action() );
		}

		public string ChooseFolder(string message)
		{
			throw new NotImplementedException();
		}

		public QuestionResponseType AskQuestion(string caption, string question)
		{
			LastQuestion = question;
			return QuestionResponse;
		}

		public void ShowMessage(string caption, string message)
		{
			throw new NotImplementedException();
		}

		public string GetValueFromUser(string caption, string message, string defaultValue)
		{
			throw new NotImplementedException();
		}

		public void RunSlideshow(SlideshowModel model)
		{
			throw new NotImplementedException();
		}

		public bool IsEditActive
		{
			get { throw new NotImplementedException(); }
		}

		public bool IsSavedActive
		{
			get { throw new NotImplementedException(); }
		}

		public SlideshowModel SelectedSavedModel
		{
			get { throw new NotImplementedException(); }
		}
	}
}
