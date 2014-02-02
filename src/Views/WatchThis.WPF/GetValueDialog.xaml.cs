using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WatchThis.Wpf
{
    /// <summary>
    /// Interaction logic for GetValueWindow.xaml
    /// </summary>
    public partial class GetValueDialog : Window
    {
        public GetValueDialog(string caption, string message, string defaultValue)
        {
            InitializeComponent();

            Message.Text = message;
            Title = caption;
            InputTextBox.Text = defaultValue;
            Loaded += (s,e) => InputTextBox.Focus();
        }

        public static string Show(Window parent, string caption, string message, string defaultValue)
        {
            var dialog = new GetValueDialog(caption, message, defaultValue);
            dialog.Owner = parent;
            dialog.ShowDialog();
            if (dialog.DialogResult == true)
            {
                return dialog.InputTextBox.Text;
            }

            return null;
        }

        private void OnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void OnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
