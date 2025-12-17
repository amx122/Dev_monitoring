using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NexusHub.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private object _currentView;

        public MainViewModel()
        {
            ShowDashboard(); // Стартова сторінка
        }

        [RelayCommand]
        private void ShowDashboard()
        {
            CurrentView = CreatePagePlaceholder("Admin Dashboard", "🏠", Brushes.White);
        }

        [RelayCommand]
        private void ShowMap()
        {
            CurrentView = CreatePagePlaceholder("Live Map", "📍", Brushes.LightBlue);
        }

        [RelayCommand]
        private void ShowMusic()
        {
            CurrentView = CreatePagePlaceholder("Music Player", "🎵", Brushes.MediumPurple);
        }

        // Вкладка чату (залишили, як ти просив)
        [RelayCommand]
        private void ShowChat()
        {
            CurrentView = CreatePagePlaceholder("Chat Interface", "💬", Brushes.Cyan);
        }

        [RelayCommand]
        private void ShowSecurity()
        {
            CurrentView = CreatePagePlaceholder("Security Center", "🛡️", Brushes.Orange);
        }

        [RelayCommand]
        private void ShowSettings()
        {
            CurrentView = CreatePagePlaceholder("Settings", "⚙️", Brushes.Gray);
        }

        private UIElement CreatePagePlaceholder(string title, string icon, Brush color)
        {
            StackPanel panel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            panel.Children.Add(new TextBlock
            {
                Text = icon,
                FontSize = 60,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            });

            panel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 32,
                Foreground = color,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            return panel;
        }
    }
}