using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ChatAppFrontend.ViewsModel;
using ChatAppFrontend.ViewModel;

namespace ChatAppFrontend.Converters
{
    public class StringEqualsVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            string val1 = value.ToString() ?? string.Empty;
            string val2 = parameter.ToString() ?? string.Empty;

            return val1 == val2 ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Nouveau convertisseur qui fonctionne avec les deux valeurs
    public class CreatorVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values.Length < 2 || values[0] == null || values[1] == null)
                    return Visibility.Collapsed;

                string creatorUsername = values[0].ToString() ?? string.Empty;
                string currentUsername = values[1].ToString() ?? string.Empty;

                return creatorUsername == currentUsername ? Visibility.Visible : Visibility.Collapsed;
            }
            catch
            {
                return Visibility.Collapsed;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MessageDeleteVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null || parameter == null)
                    return Visibility.Collapsed;

                if (!(parameter is HomeViewModel viewModel))
                    return Visibility.Collapsed;

                var message = value as Message;
                if (message == null)
                    return Visibility.Collapsed;

                // Permettre la suppression si:
                // 1. Nous sommes dans un salon (pas en conversation privée ou public)
                // 2. Le message a été envoyé par l'utilisateur actuel
                bool isInChannel = viewModel.CurrentConversation?.Type == ConversationType.Channel;
                bool isMessageFromCurrentUser = message.Sender == viewModel.CurrentUsername;

                return (isInChannel && isMessageFromCurrentUser) ? Visibility.Visible : Visibility.Collapsed;
            }
            catch
            {
                // En cas d'erreur, ne pas afficher le bouton
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 