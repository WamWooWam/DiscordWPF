using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DiscordWPF.Dialogs
{
    /// <summary>
    /// Interaction logic for InputDialog.xaml
    /// </summary>
    public partial class TwoFactorAuthDialog : Window
    {
        public string Code { get => textBoxContainer.Visibility == Visibility.Visible ? string.Join("", textBoxContainer.Children.OfType<TextBox>().Select(t => t.Text)) : backupCode.Text; }

        public TwoFactorAuthDialog()
        {
            InitializeComponent();
            foreach(TextBox textBox in textBoxContainer.Children.OfType<TextBox>())
            {
                textBox.PreviewTextInput += TextBox_PreviewTextInput;
                textBox.TextChanged += TextBox_TextChanged;
            }
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox s = (sender as TextBox);
            int index = textBoxContainer.Children.IndexOf(s);
            if(s != null)
            {
                if(string.IsNullOrWhiteSpace(s.Text))
                {
                    if(index > 0 && index < textBoxContainer.Children.Count)
                    {
                        TextBox next = textBoxContainer.Children[index - 1] as TextBox;
                        next.Focus();
                    }
                }
                else
                {
                    if (index < (textBoxContainer.Children.Count - 1))
                    {
                        TextBox next = textBoxContainer.Children[index + 1] as TextBox;
                        next.Focus();
                    }
                    else
                    {
                        okButton.Focus();
                    }
                }
            }
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Owner != null)
                Icon = Owner.Icon;
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            if(textBoxContainer.Visibility == Visibility.Visible)
            {
                helpText.Text = "Use normal code...";
                textBoxContainer.Visibility = Visibility.Collapsed;
                backupCode.Visibility = Visibility.Visible;
            }
            else
            {
                helpText.Text = "Use backup code...";
                textBoxContainer.Visibility = Visibility.Visible;
                backupCode.Visibility = Visibility.Collapsed;
            }
        }
    }
}
