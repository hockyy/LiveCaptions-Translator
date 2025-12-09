using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

using LiveCaptionsTranslator.utils;

namespace LiveCaptionsTranslator
{
    public partial class CaptionPage : Page
    {
        public const int CARD_HEIGHT = 110;

        private static CaptionPage instance;
        public static CaptionPage Instance => instance;

        public CaptionPage()
        {
            InitializeComponent();
            DataContext = Translator.Caption;
            instance = this;

            Loaded += (s, e) =>
            {
                AutoHeight();
                (App.Current.MainWindow as MainWindow).CaptionLogButton.Visibility = Visibility.Visible;
                Translator.Caption.PropertyChanged += TranslatedChanged;
                Translator.Setting.OverlayWindow.PropertyChanged += SettingChanged;
                ApplyFontFamily();
            };
            Unloaded += (s, e) =>
            {
                (App.Current.MainWindow as MainWindow).CaptionLogButton.Visibility = Visibility.Collapsed;
                Translator.Caption.PropertyChanged -= TranslatedChanged;
                Translator.Setting.OverlayWindow.PropertyChanged -= SettingChanged;
            };

            CollapseTranslatedCaption(Translator.Setting.MainWindow.CaptionLogEnabled);
        }

        private async void TextBlock_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock textBlock)
            {
                try
                {
                    Clipboard.SetText(textBlock.Text);
                    textBlock.ToolTip = "Copied!";
                }
                catch
                {
                    textBlock.ToolTip = "Error to Copy";
                }
                await Task.Delay(500);
                textBlock.ToolTip = "Click to Copy";
            }
        }

        private void TranslatedChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Translator.Caption.DisplayTranslatedCaption))
            {
                if (Encoding.UTF8.GetByteCount(Translator.Caption.DisplayTranslatedCaption) >= TextUtil.LONG_THRESHOLD)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.TranslatedCaption.FontSize = 15;
                    }), DispatcherPriority.Background);
                }
                else
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.TranslatedCaption.FontSize = 18;
                    }), DispatcherPriority.Background);
                }
            }
            else if (e.PropertyName == nameof(Translator.Caption.DisplayContexts))
            {
                // When contexts update, reapply font family to new items
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ApplyFontFamily();
                }), DispatcherPriority.Background);
            }
        }

        public void CollapseTranslatedCaption(bool isCollapsed)
        {
            var converter = new GridLengthConverter();

            if (isCollapsed)
            {
                TranslatedCaption_Row.Height = (GridLength)converter.ConvertFromString("Auto");
                CaptionLogCard.Visibility = Visibility.Visible;
            }
            else
            {
                TranslatedCaption_Row.Height = (GridLength)converter.ConvertFromString("*");
                CaptionLogCard.Visibility = Visibility.Collapsed;
            }
        }

        public void AutoHeight()
        {
            if (Translator.Setting.MainWindow.CaptionLogEnabled)
                (App.Current.MainWindow as MainWindow).AutoHeightAdjust(
                    minHeight: CARD_HEIGHT * (Translator.Setting.MainWindow.CaptionLogMax + 1),
                    maxHeight: CARD_HEIGHT * (Translator.Setting.MainWindow.CaptionLogMax + 1));
            else
                (App.Current.MainWindow as MainWindow).AutoHeightAdjust(
                    minHeight: (int)App.Current.MainWindow.MinHeight,
                    maxHeight: (int)App.Current.MainWindow.MinHeight);
        }

        private void SettingChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FontFamily")
            {
                ApplyFontFamily();
            }
        }

        private void ApplyFontFamily()
        {
            var fontFamily = new FontFamily(Translator.Setting.OverlayWindow.FontFamily);
            this.OriginalCaption.FontFamily = fontFamily;
            this.TranslatedCaption.FontFamily = fontFamily;
            
            // Apply to all TextBlocks in the ItemsControl (log cards)
            foreach (var item in CaptionLogItems.Items)
            {
                var container = CaptionLogItems.ItemContainerGenerator.ContainerFromItem(item);
                if (container != null)
                {
                    ApplyFontFamilyToVisualTree(container, fontFamily);
                }
            }
        }

        private void ApplyFontFamilyToVisualTree(DependencyObject parent, FontFamily fontFamily)
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is TextBlock textBlock)
                {
                    textBlock.FontFamily = fontFamily;
                }
                ApplyFontFamilyToVisualTree(child, fontFamily);
            }
        }
    }
}
