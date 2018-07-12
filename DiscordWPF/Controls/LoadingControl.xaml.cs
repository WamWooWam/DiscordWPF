using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DiscordWPF.Controls
{
    /// <summary>
    /// Interaction logic for LoadingControl.xaml
    /// </summary>
    public partial class LoadingControl : UserControl
    {
        public string Text { get => connectingStatus.Text; set => connectingStatus.Text = value; }

        public double Value { get => connetingProgress.Value; set => connetingProgress.Value = value; }
        public double Maximum { get => connetingProgress.Maximum; set => connetingProgress.Maximum = value; }
        public bool IsIndeterminate { get => connetingProgress.IsIndeterminate; set => connetingProgress.IsIndeterminate = value; }

        public LoadingControl()
        {
            InitializeComponent();
        }

        public void ChangeStatus(string text, int? value = null, int? max = null)
        {
            connectingStatus.Text = text;
            connetingProgress.Maximum = max ?? connetingProgress.Maximum;
            if (value == null)
            {
                connetingProgress.IsIndeterminate = true;
            }
            else
            {
                connetingProgress.IsIndeterminate = false;
                connetingProgress.Value = value.Value;
            }
        }

        public async Task ChangeStatusAsync(string text, int? value = null, int? max = null)
            => await Dispatcher.InvokeAsync(() => ChangeStatus(text, value, max));
    }
}
