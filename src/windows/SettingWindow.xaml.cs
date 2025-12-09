using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LiveCaptionsTranslator.models;
using LiveCaptionsTranslator.utils;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;
using TextBlock = Wpf.Ui.Controls.TextBlock;

namespace LiveCaptionsTranslator
{
    public partial class SettingWindow : FluentWindow
    {
        private System.Windows.Controls.Button currentSelected;
        private Dictionary<string, FrameworkElement> sectionReferences;

        public SettingWindow()
        {
            InitializeComponent();
            ApplicationThemeManager.ApplySystemTheme();
            DataContext = Translator.Setting;

            Loaded += (sender, args) =>
            {
                SystemThemeWatcher.Watch(this, WindowBackdropType.Mica, true);
                Initialize();
                SelectButton(PromptButton);
                UpdatePromptIndex();
                UpdatePromptReadOnly();
            };
        }

        private void Initialize()
        {
            sectionReferences = new Dictionary<string, FrameworkElement>
            {
                { "General", ContentPanel },
                { "Prompt", PromptSection }
            };
            
            foreach (var apiName in TranslateAPI.TRANSLATE_FUNCTIONS.Keys.Where(apiName =>
                         !TranslateAPI.NO_CONFIG_APIS.Contains(apiName)))
            {
                sectionReferences[apiName] = FindName($"{apiName}Section") as StackPanel;
                SwitchConfig(apiName, Translator.Setting.ConfigIndices[apiName]);
            }
        }
        
        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string apiName = button.Tag as string;
                var configs = Translator.Setting.Configs[apiName];
                var configIndex = Translator.Setting.ConfigIndices[apiName];
                
                var type = Type.GetType($"LiveCaptionsTranslator.models.{apiName}Config");
                var config = Activator.CreateInstance(type) as TranslateAPIConfig;
                configs.Insert(configIndex + 1, config);
                SwitchConfig(apiName, configIndex + 1);
                
                Translator.Setting.OnPropertyChanged("Configs");
            }
        }
        
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string apiName = button.Tag as string;
                var configs = Translator.Setting.Configs[apiName];
                var configIndex = Translator.Setting.ConfigIndices[apiName];

                if (configs.Count <= 1)
                {
                    (FindName($"{apiName}DeleteFlyout") as Flyout)?.Show();
                    return;
                }
                configs.RemoveAt(configIndex);
                SwitchConfig(apiName, Math.Max(0, Math.Min(configs.Count - 1, configIndex)));
                
                Translator.Setting.OnPropertyChanged("Configs");
            }
        }

        private void PriorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string apiName = button.Tag as string;
                var configIndex = Translator.Setting.ConfigIndices[apiName];
                SwitchConfig(apiName, configIndex - 1);
            }
        }
        
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string apiName = button.Tag as string;
                var configIndex = Translator.Setting.ConfigIndices[apiName];
                SwitchConfig(apiName, configIndex + 1);
            }
        }

        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button)
            {
                SelectButton(button);
                string targetSection = button.Tag.ToString();
                if (sectionReferences.TryGetValue(targetSection, out FrameworkElement element))
                    element.BringIntoView();
            }
        }

        private void OpenAIAPIUrlInfo_MouseEnter(object sender, MouseEventArgs e)
        {
            OpenAIAPIUrlInfoFlyout.Show();
        }

        private void OpenAIAPIUrlInfo_MouseLeave(object sender, MouseEventArgs e)
        {
            OpenAIAPIUrlInfoFlyout.Hide();
        }
        
        private void OllamaAPIUrlInfo_MouseEnter(object sender, MouseEventArgs e)
        {
            OllamaAPIUrlInfoFlyout.Show();
        }

        private void OllamaAPIUrlInfo_MouseLeave(object sender, MouseEventArgs e)
        {
            OllamaAPIUrlInfoFlyout.Hide();
        }
        
        private void SwitchConfig(string apiName, int index)
        {
            if (index < 0 || index >= Translator.Setting.Configs[apiName].Count)
                return;
            
            if (Translator.Setting.ConfigIndices[apiName] != index)
                Translator.Setting.ConfigIndices[apiName] = index;
            
            if (FindName($"{apiName}Index") is TextBlock indexTextBlock)
            {
                int total = Translator.Setting.Configs[apiName].Count;
                indexTextBlock.Text = $"{index + 1}/{total}";
            }
            Translator.Setting.OnPropertyChanged(null);
        }
        
        private void SelectButton(System.Windows.Controls.Button button)
        {
            if (currentSelected != null)
                currentSelected.Background = new SolidColorBrush(Colors.Transparent);
            button.Background = (Brush)FindResource("ControlFillColorSecondaryBrush");
            currentSelected = button;
        }

        private void PromptPriorButton_Click(object sender, RoutedEventArgs e)
        {
            if (Translator.Setting.CurrentPromptIndex > 0)
            {
                Translator.Setting.CurrentPromptIndex--;
                Translator.Setting.Prompt = Translator.Setting.SavedPrompts[Translator.Setting.CurrentPromptIndex];
                UpdatePromptIndex();
                UpdatePromptReadOnly();
            }
        }

        private void PromptNextButton_Click(object sender, RoutedEventArgs e)
        {
            if (Translator.Setting.CurrentPromptIndex < Translator.Setting.SavedPrompts.Count - 1)
            {
                Translator.Setting.CurrentPromptIndex++;
                Translator.Setting.Prompt = Translator.Setting.SavedPrompts[Translator.Setting.CurrentPromptIndex];
                UpdatePromptIndex();
                UpdatePromptReadOnly();
            }
        }

        private void PromptSaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Save current prompt text to the current index
            string currentPrompt = PromptTextBox.Text?.Trim() ?? "";
            
            // If empty, use default prompt
            if (string.IsNullOrWhiteSpace(currentPrompt))
            {
                currentPrompt = Setting.DEFAULT_PROMPT;
            }

            // Don't allow modifying the default prompt (index 0)
            if (Translator.Setting.CurrentPromptIndex == 0)
            {
                return;
            }

            Translator.Setting.SavedPrompts[Translator.Setting.CurrentPromptIndex] = currentPrompt;
            Translator.Setting.Prompt = currentPrompt;
            Translator.Setting.OnPropertyChanged("SavedPrompts");
        }

        private void PromptNewButton_Click(object sender, RoutedEventArgs e)
        {
            // Create a new prompt after the current one
            string newPrompt = PromptTextBox.Text?.Trim() ?? Setting.DEFAULT_PROMPT;
            
            if (string.IsNullOrWhiteSpace(newPrompt))
            {
                newPrompt = Setting.DEFAULT_PROMPT;
            }

            Translator.Setting.SavedPrompts.Insert(Translator.Setting.CurrentPromptIndex + 1, newPrompt);
            Translator.Setting.CurrentPromptIndex++;
            Translator.Setting.Prompt = newPrompt;
            UpdatePromptIndex();
            UpdatePromptReadOnly();
            Translator.Setting.OnPropertyChanged("SavedPrompts");
        }

        private void PromptDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // Cannot delete if it's the default prompt (index 0) or only one prompt left
            if (Translator.Setting.CurrentPromptIndex == 0 || Translator.Setting.SavedPrompts.Count <= 1)
            {
                PromptDeleteFlyout.Show();
                return;
            }

            Translator.Setting.SavedPrompts.RemoveAt(Translator.Setting.CurrentPromptIndex);
            
            // Adjust index
            if (Translator.Setting.CurrentPromptIndex >= Translator.Setting.SavedPrompts.Count)
            {
                Translator.Setting.CurrentPromptIndex = Translator.Setting.SavedPrompts.Count - 1;
            }
            
            Translator.Setting.Prompt = Translator.Setting.SavedPrompts[Translator.Setting.CurrentPromptIndex];
            UpdatePromptIndex();
            UpdatePromptReadOnly();
            Translator.Setting.OnPropertyChanged("SavedPrompts");
        }

        private void UpdatePromptIndex()
        {
            int current = Translator.Setting.CurrentPromptIndex + 1;
            int total = Translator.Setting.SavedPrompts.Count;
            PromptIndex.Text = $"{current}/{total}";
        }

        private void UpdatePromptReadOnly()
        {
            // Make the default prompt (index 0) read-only
            bool isDefault = Translator.Setting.CurrentPromptIndex == 0;
            PromptTextBox.IsReadOnly = isDefault;
            
            // Visual indication for read-only state
            if (isDefault)
            {
                PromptTextBox.Opacity = 0.7;
            }
            else
            {
                PromptTextBox.Opacity = 1.0;
            }
        }
    }
}