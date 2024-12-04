using System;
using System.Globalization;
using System.Windows.Data;

namespace OfflineTimeTracker.Service
{
    public class ProjectDescriptionConverter : IValueConverter
    {
        // Метод преобразования (из источника в цель)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string projectDescription = value as string;
            if (string.IsNullOrWhiteSpace(projectDescription))
            {
                return "Проект: -";
            }
            return $"Проект: {projectDescription}";
        }

        // Метод обратного преобразования (не требуется, возвращаем null)
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Обычно не требуется для однонаправленной привязки
            return null;
        }
    }
}
