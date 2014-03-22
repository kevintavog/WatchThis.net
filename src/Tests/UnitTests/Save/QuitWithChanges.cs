using System;
using Kekiri;
using WatchThis.Controllers;
using NUnit.Framework;
using WatchThis.Models;
using System.Threading.Tasks;
using System.IO;

namespace UnitTests
{
	public class QuitWithChanges : ScenarioTest
	{
		private SlideshowChooserController controller;
		private TestPicker picker = new TestPicker();


		[Given]
		public void GivenAPreviouslySavedSlideshow()
		{
			controller = new SlideshowChooserController(picker, picker);
			controller.EditedSlideshow.Filename = "a bogus filename";
		}

		[Given]
		public void TheUserMakesChanges()
		{
			controller.EditedSlideshow.FolderList.Add(new FolderModel());
		}

		[When]
		public void WhenTheUserQuits()
		{
			// The user response doesn't matter - we want to ensure the question is asked
			picker.QuestionResponse = QuestionResponseType.Cancel;
			controller.CanClose();
		}

		[Then]
		public void ThenTheUserShouldBePromptedToSaveChanges()
		{
			Assert.That(picker.LastQuestion, Is.Not.Null.And.StartsWith("Do you"));
		}
	}
}

