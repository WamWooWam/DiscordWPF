using DiscordWPF.Data;
using Newtonsoft.Json;
using Ookii.Dialogs;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using WamWooWam.Core.Extensions;

namespace DiscordWPF
{
    /// <summary>
    /// Interaction logic for SettingsUI.xaml
    /// </summary>
    public partial class SettingsUI : UserControl
    {
        Theme editingTheme = null;
        bool themeSaved = true;
        DispatcherTimer updateTimer = new DispatcherTimer(DispatcherPriority.Render);

        public SettingsUI()
        {
            InitializeComponent();

            ChangeTheme();

            themeSelector.Items.Clear();
            foreach (Theme theme in App.Config.Personalisation.Themes)
            {
                themeSelector.Items.Add(new ComboBoxItem() { Content = theme.ThemeName });
            }

            themeSelector.SelectedIndex = App.Config.Personalisation.SelectedTheme;

            foreColourSlider.ColourChanged += ForeColourSlider_ColourChanged;
            backColourSlider.ColourChanged += BackColourSlider_ColourChanged;
            secondaryColourSlider.ColourChanged += SecondaryColourSlider_ColourChanged;
            foreSecondaryColourSlider.ColourChanged += ForeSecondaryColourSlider_ColourChanged;
            selectionBackgroundSlider.ColourChanged += SelectionBackgroundSlider_ColourChanged;

            successSlider.ColourChanged += SuccessSlider_ColourChanged;
            warningSlider.ColourChanged += WarningSlider_ColourChanged;
            errorSlider.ColourChanged += ErrorSlider_ColourChanged;

            userNicknames.IsChecked = App.Config.General.Nicknames;
            userColours.IsChecked = App.Config.General.UserColours;
            userColoursMentions.IsChecked = App.Config.General.UserColourMentions;
            userColoursMessages.IsChecked = App.Config.General.UserColourTitles;
            textFormatting.IsChecked = App.Config.General.FormatText;
            reduceAnimations.IsChecked = App.Config.General.ReduceAnimations;
            imageWindowIcon.IsChecked = App.Config.General.GuildImageWindowIcon;

            userNicknames.Checked += SettingsChanged; userNicknames.Unchecked += SettingsChanged;
            userColours.Checked += SettingsChanged; userColours.Unchecked += SettingsChanged;
            userColoursMentions.Checked += SettingsChanged; userColoursMentions.Unchecked += SettingsChanged;
            userColoursMessages.Checked += SettingsChanged; userColoursMessages.Unchecked += SettingsChanged;
            textFormatting.Checked += SettingsChanged; textFormatting.Unchecked += SettingsChanged;
            reduceAnimations.Checked += SettingsChanged; reduceAnimations.Unchecked += SettingsChanged;
            imageWindowIcon.Checked += SettingsChanged; imageWindowIcon.Unchecked += SettingsChanged;

            ((Storyboard)(App.Current.MainWindow).Resources["AnimateSettingsHide"]).Completed += SettingsUI_Completed;
            ((Storyboard)(App.Current.MainWindow).Resources["AnimateSettingsShow"]).Completed += SettingsUI_Completed1;
            updateTimer.Interval = TimeSpan.FromMilliseconds(500);
            updateTimer.Tick += UpdateTimer_Tick;

            foreach (FontFamily font in Fonts.SystemFontFamilies.OrderBy(f => f.FamilyNames.First().Value))
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = font.FamilyNames.First().Value;
                item.FontFamily = font;
                item.FontSize = 14;
                fontComboBox.Items.Add(item);
                if (font == App.Theme.Font)
                {
                    fontComboBox.SelectedItem = item;
                }
            }

            warningText.Visibility = Visibility.Collapsed;
        }

        private void SettingsUI_Completed1(object sender, EventArgs e)
        {
            updateTimer.Start();
        }

        private void SettingsUI_Completed(object sender, EventArgs e)
        {
            updateTimer.Stop();
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (themeSaved == false)
            {
                App.DiscordWindow.ChangeTheme();
                ChangeTheme();
            }
        }

        private void SelectionBackgroundSlider_ColourChanged(object sender, EventArgs e)
        {
            App.Theme.SelectedBackground = selectionBackgroundSlider.ResultColour;

            if (editingTheme != null)
                editingTheme.SelectedBackground = selectionBackgroundSlider.ResultColour;
            themeSaved = false;
        }

        private void ErrorSlider_ColourChanged(object sender, EventArgs e)
        {
            App.Theme.Error = errorSlider.ResultColour;

            if (editingTheme != null)
                editingTheme.Error = errorSlider.ResultColour;
            themeSaved = false;
        }

        private void WarningSlider_ColourChanged(object sender, EventArgs e)
        {
            App.Theme.Warning = warningSlider.ResultColour;

            if (editingTheme != null)
                editingTheme.Warning = warningSlider.ResultColour;
            themeSaved = false;
        }

        private void SuccessSlider_ColourChanged(object sender, EventArgs e)
        {
            App.Theme.Success = successSlider.ResultColour;

            if (editingTheme != null)
                editingTheme.Success = successSlider.ResultColour;
            themeSaved = false;
        }

        private void ForeSecondaryColourSlider_ColourChanged(object sender, EventArgs e)
        {
            App.Theme.SecondaryForeground = foreSecondaryColourSlider.ResultColour;

            if (editingTheme != null)
                editingTheme.SecondaryForeground = foreSecondaryColourSlider.ResultColour;
            themeSaved = false;
        }

        private void SecondaryColourSlider_ColourChanged(object sender, EventArgs e)
        {
            App.Theme.SecondaryBackground = secondaryColourSlider.ResultColour;

            if (editingTheme != null)
                editingTheme.SecondaryBackground = secondaryColourSlider.ResultColour;
            themeSaved = false;
        }

        private void BackColourSlider_ColourChanged(object sender, EventArgs e)
        {
            App.Theme.Background = backColourSlider.ResultColour;

            if (editingTheme != null)
                editingTheme.Background = backColourSlider.ResultColour;
            themeSaved = false;
        }

        private void ForeColourSlider_ColourChanged(object sender, EventArgs e)
        {
            App.Theme.Foreground = foreColourSlider.ResultColour;

            if (editingTheme != null)
                editingTheme.Foreground = foreColourSlider.ResultColour;
            themeSaved = false;
        }

        public void ChangeTheme()
        {
            App.UpdateResources();
            App.Current.MainWindow.InvalidateVisual();

            foreColourSlider.Base(App.ForegroundBrush.Color);
            backColourSlider.Base(App.BackgroundBrush.Color);
            secondaryColourSlider.Base(App.SecondaryBackgroundBrush.Color);
            foreSecondaryColourSlider.Base(App.SecondaryForegroundBrush.Color);

            successSlider.Base(App.SuccessColour);
            warningSlider.Base(App.WarningColour);
            errorSlider.Base(App.ErrorColour);

            warningText.Foreground = new SolidColorBrush(App.WarningColour);
        }

        private async void closeImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (editingTheme != null && !themeSaved)
            {
                if (!SaveTheme())
                    return;
            }

            SettingsChanged(null, null);

            App.Config.Personalisation.SelectedTheme = themeSelector.SelectedIndex;
            App.Config.Save();
            ((Storyboard)(App.Current.MainWindow).Resources["AnimateSettingsHide"]).Begin(this.Parent as FrameworkElement);

            if (warningText.Visibility == Visibility.Visible && DiscordWindow.selectedTextChannel != null)
            {
                warningText.Visibility = Visibility.Collapsed;
                await App.DiscordWindow.Refresh(DiscordWindow.selectedTextChannel);
            }
            else
                warningText.Visibility = Visibility.Collapsed;
        }

        private void bgBrushLabel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (backColourSlider.Visibility == Visibility.Collapsed)
                backColourSlider.Visibility = Visibility.Visible;
            else
                backColourSlider.Visibility = Visibility.Collapsed;
        }

        private void fgBrushLabel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (foreColourSlider.Visibility == Visibility.Collapsed)
                foreColourSlider.Visibility = Visibility.Visible;
            else
                foreColourSlider.Visibility = Visibility.Collapsed;
        }

        private void secondaryBrushLabel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (secondaryColourSlider.Visibility == Visibility.Collapsed)
                secondaryColourSlider.Visibility = Visibility.Visible;
            else
                secondaryColourSlider.Visibility = Visibility.Collapsed;
        }

        private void fgSecondaryBrushLabel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (foreSecondaryColourSlider.Visibility == Visibility.Collapsed)
                foreSecondaryColourSlider.Visibility = Visibility.Visible;
            else
                foreSecondaryColourSlider.Visibility = Visibility.Collapsed;
        }

        private void defaultColours_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void fontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            App.Font = (fontComboBox.SelectedItem as ComboBoxItem).FontFamily;

            if (editingTheme != null)
                editingTheme.Font = (fontComboBox.SelectedItem as ComboBoxItem).FontFamily;

            themeSaved = false;

            App.DiscordWindow.ChangeTheme();
            ChangeTheme();
        }

        private void themeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int thing = themeSelector.SelectedIndex;

            if (editingTheme != null && !themeSaved)
            {
                if (!SaveTheme())
                    return;
            }

            if (App.Config.Personalisation.Themes.ElementAtOrDefault(thing) != null)
            {
                App.SelectedTheme = thing;
                App.ChangeTheme();

                Theme thingTheme = App.Config.Personalisation.Themes[thing];

                if (!thingTheme.ReadOnly)
                {
                    editingTheme = thingTheme;
                    themeEditor.Visibility = Visibility.Visible;
                }
                else
                {
                    editingTheme = null;
                    themeEditor.Visibility = Visibility.Collapsed;
                    themeSaved = true;
                }
            }

            App.DiscordWindow.ChangeTheme();
            ChangeTheme();
        }

        private bool SaveTheme()
        {
            TaskDialog dialog = new TaskDialog()
            {
                MainIcon = TaskDialogIcon.Warning,
                MainInstruction = "Save unsaved theme?",
                Content = $"Do you want to save changes the theme \"{editingTheme.ThemeName}\"?"
            };
            TaskDialogButton yes = new TaskDialogButton(ButtonType.Yes);
            TaskDialogButton no = new TaskDialogButton(ButtonType.No);
            TaskDialogButton cancel = new TaskDialogButton(ButtonType.Cancel);

            dialog.Buttons.Add(yes);
            dialog.Buttons.Add(no);
            dialog.Buttons.Add(cancel);

            TaskDialogButton button = dialog.ShowDialog();
            if (button == yes)
            {
                if (App.Config.Personalisation.Themes.ElementAtOrDefault(editingTheme.Position) != null)
                    App.Config.Personalisation.UserThemes.RemoveAt(editingTheme.Position - 2);
                App.Config.Personalisation.UserThemes.Insert(editingTheme.Position - 2, editingTheme);
                App.Config.Save();

                themeSaved = true;

                App.SelectedTheme = App.Config.Personalisation.Themes.ToList().IndexOf(editingTheme);
                App.ChangeTheme();
            }
            else if (button == no)
            {
                if (!App.Config.Personalisation.UserThemes.Any(t => t.ThemeName == editingTheme.ThemeName))
                {
                    themeSelector.Items.Remove(themeSelector.Items.Cast<ComboBoxItem>().First(c => (string)c.Content == editingTheme.ThemeName));
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        private void addThemeButton_Click(object sender, RoutedEventArgs e)
        {
            if (editingTheme != null && !themeSaved)
            {
                if (!SaveTheme())
                    return;
            }

            Dialogs.InputDialog dialog = new Dialogs.InputDialog();
            dialog.MainInstruction = "Enter a theme name.";
            dialog.Content = "Enter the name for your new theme here.";

            if (dialog.ShowDialog() == true)
            {
                if (!App.Config.Personalisation.UserThemes.Any(t => t.ThemeName == dialog.Input))
                {
                    editingTheme = JsonConvert.DeserializeObject<Theme>(App.Theme.ToJson());
                    editingTheme.ThemeName = dialog.Input;
                    editingTheme.ReadOnly = false;
                    editingTheme.Position = App.Config.Personalisation.Themes.Count;
                    App.Config.Personalisation.UserThemes.Add(editingTheme);
                    App.Config.Save();

                    themeEditor.Visibility = Visibility.Visible;
                    themeSaved = true;
                    themeSelector.Items.Add(new ComboBoxItem() { Content = dialog.Input });
                    themeSelector.SelectedIndex = themeSelector.Items.Count - 1;
                }
                else
                {
                    TaskDialog error = new TaskDialog();
                    error.MainIcon = TaskDialogIcon.Error;
                    error.MainInstruction = "Theme already exists!";
                    error.Content = $"A theme with that name already exists.";

                    TaskDialogButton yes = new TaskDialogButton(ButtonType.Ok);

                    error.Buttons.Add(yes);

                    error.ShowDialog();
                }
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void logoutGrid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            App.Config.Token = "";
            App.Config.Save();

            App.DiscordWindow.Client.LogoutAsync();
        }

        private void successLabel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (successSlider.Visibility == Visibility.Collapsed)
                successSlider.Visibility = Visibility.Visible;
            else
                successSlider.Visibility = Visibility.Collapsed;
        }

        private void warningLabel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (warningSlider.Visibility == Visibility.Collapsed)
                warningSlider.Visibility = Visibility.Visible;
            else
                warningSlider.Visibility = Visibility.Collapsed;
        }

        private void errorLabel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (errorSlider.Visibility == Visibility.Collapsed)
                errorSlider.Visibility = Visibility.Visible;
            else
                errorSlider.Visibility = Visibility.Collapsed;
        }

        private void SettingsChanged(object sender, RoutedEventArgs e)
        {
            warningText.Visibility = Visibility.Visible;

            App.Config.General.Nicknames = userNicknames.IsChecked.GetValueOrDefault();

            App.Config.General.UserColours = userColours.IsChecked.GetValueOrDefault();
            App.Config.General.UserColourMentions = userColoursMentions.IsChecked.GetValueOrDefault();
            App.Config.General.UserColourTitles = userColoursMessages.IsChecked.GetValueOrDefault();

            App.Config.General.FormatText = textFormatting.IsChecked.GetValueOrDefault();
            App.Config.General.ReduceAnimations = reduceAnimations.IsChecked.GetValueOrDefault();
            App.Config.General.GuildImageWindowIcon = imageWindowIcon.IsChecked.GetValueOrDefault();
        }

        private void selectionBackgroundLabel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (selectionBackgroundSlider.Visibility == Visibility.Collapsed)
                selectionBackgroundSlider.Visibility = Visibility.Visible;
            else
                selectionBackgroundSlider.Visibility = Visibility.Collapsed;
        }
    }
}
