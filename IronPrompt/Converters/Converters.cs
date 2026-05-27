// Copyright 2026 Arda Ceylan
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Avalonia.Data.Converters;
using Avalonia.Media;
using IronPrompt.ViewModels;
using System;
using System.Globalization;

namespace IronPrompt.Converters
{
    public class UserColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isUser)
            {
                return isUser
                    ? new SolidColorBrush(Color.Parse("#00B4D8"))
                    : new SolidColorBrush(Color.Parse("#9D4EDD"));
            }
            return Brushes.Gray;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EnumToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is MessagePartType currentType && parameter is string targetTypeStr)
            {
                return currentType.ToString() == targetTypeStr;
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class HeaderLevelToFontSizeConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int level)
            {
                return level switch
                {
                    1 => 22.0,
                    2 => 19.0,
                    3 => 16.0,
                    4 => 14.0,
                    _ => 16.0
                };
            }
            return 16.0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NotNullToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool isNotNull = value != null;
            if (parameter is string paramStr && paramStr.Equals("invert", StringComparison.OrdinalIgnoreCase))
            {
                return !isNotNull;
            }
            return isNotNull;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LanguageBackgroundConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string currentLang && parameter is string targetLang)
            {
                return currentLang == targetLang
                    ? new SolidColorBrush(Color.Parse("#00B4D8"))
                    : Brushes.Transparent;
            }
            return Brushes.Transparent;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LanguageForegroundConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string currentLang && parameter is string targetLang)
            {
                return currentLang == targetLang
                    ? Brushes.White
                    : new SolidColorBrush(Color.Parse("#8E8E9F"));
            }
            return Brushes.White;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
