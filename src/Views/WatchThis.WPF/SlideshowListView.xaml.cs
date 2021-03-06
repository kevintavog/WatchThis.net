﻿using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WatchThis.Controllers;
using WatchThis.Models;
using WatchThis.Utilities;

namespace WatchThis.Wpf
{
    public partial class SlideshowListView : Window, ISlideshowPickerViewer, IPlatformService, INotifyPropertyChanged
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public event PropertyChangedEventHandler PropertyChanged;
        public SlideshowChooserController Controller { get; set; }

        string _lastAddedPath;

        public SlideshowListView()
        {
            Controller = new SlideshowChooserController(this, this);
            InitializeComponent();

            SlideshowList.Focus();

            Controller.FindSavedSlideshows();
            this.FirePropertyChanged(PropertyChanged, () => Controller);
        }

        private void OnMouseDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            InvokeOnUiThread(delegate { Controller.RunSlideshow(); });
        }

        private void RunSlideshow(object sender, ExecutedRoutedEventArgs args)
        {
            Controller.RunSlideshow();
        }

        private void AddFolder(object sender, ExecutedRoutedEventArgs args)
        {
            Controller.AddEditFolder();
        }

        private void OpenSlideshow(object sender, ExecutedRoutedEventArgs args)
        {
            Controller.OpenSlideshow();
        }

        private void SaveSlideshow(object sender, ExecutedRoutedEventArgs args)
        {
            Controller.SaveSlideshow();
        }

        private void EditSlideshow(object sender, ExecutedRoutedEventArgs args)
        {
            Controller.EditSlideshow();
        }

        private void DeleteSlideshow(object sender, ExecutedRoutedEventArgs args)
        {
            Controller.DeleteSlideshow();
        }

        private void ClearEdit(object sender, ExecutedRoutedEventArgs args)
        {
            Controller.ClearEdit();
        }

        private void RemoveSelectedFolders(object sender, ExecutedRoutedEventArgs args)
        {
            var folderList = new List<FolderModel>();
            foreach (var item in CreatedListView.SelectedItems)
            {
                folderList.Add((FolderModel)item);
            }

            Controller.RemoveEditFolders(folderList);
        }

        private void ActivateEditTab(object sender, ExecutedRoutedEventArgs args)
        {
            TabControl.SelectedItem = EditTabItem;
        }

        private void ActivateSavedTab(object sender, ExecutedRoutedEventArgs args)
        {
            TabControl.SelectedItem = SavedTabItem;
        }

        private void Exit(object sender, ExecutedRoutedEventArgs args)
        {
            Application.Current.Shutdown();
        }

        void FolderDragEnter(object sender, DragEventArgs e)
        {
            var folders = GetDropFolders(e);
            if (folders.Count < 1)
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        void FolderDrop(object sender, DragEventArgs e)
        {
            Controller.AddEditDroppedFolders(GetDropFolders(e));
        }

        IList<string> GetDropFolders(DragEventArgs e)
        {
            var folders = new List<string>();

            var droppedNames = e.Data.GetData(DataFormats.FileDrop, true) as string[];
            if (droppedNames != null)
            {
                foreach (var n in droppedNames)
                {
                    if (!Directory.Exists(n))
                    {
                        folders.Clear();
                        break;
                    }
                    folders.Add(n);
                }
            }
            return folders;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Enter)
            {
                Controller.RunSlideshow();
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = !Controller.CanClose();
        }

        public string ChooseFolder(string message)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = message;
            dialog.SelectedPath = _lastAddedPath;
            if (System.Windows.Forms.DialogResult.OK == dialog.ShowDialog())
            {
                _lastAddedPath = dialog.SelectedPath;
                return dialog.SelectedPath;
            }

            return null;
        }

        public void RunSlideshow(SlideshowModel model)
        {
            var show = new SlideshowWindow(model);
            show.Show();
        }

        public QuestionResponseType AskQuestion(string caption, string question)
        {
            var response = MessageBox.Show(
                question,
                caption,
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            switch (response)
            {
                case MessageBoxResult.Yes:
                    return QuestionResponseType.Yes;
                case MessageBoxResult.No:
                    return QuestionResponseType.No;
                default:
                    return QuestionResponseType.Cancel;
            }
        }

        public void ShowMessage(string caption, string message)
        {
            MessageBox.Show(
                message,
                caption,
                MessageBoxButton.OK);
        }

        public string GetValueFromUser(string caption, string message, string defaultValue)
        {
            return GetValueDialog.Show(this, caption, message, defaultValue);
        }


        public bool IsEditActive { get { return TabControl.SelectedItem == EditTabItem; } }
        public bool IsSavedActive { get { return TabControl.SelectedItem == SavedTabItem; } }
        public SlideshowModel SelectedSavedModel { get { return SlideshowList.SelectedItem as SlideshowModel; } }

        public void InvokeOnUiThread(Action action)
        {
            Dispatcher.BeginInvoke(action);
        }
    }
}
