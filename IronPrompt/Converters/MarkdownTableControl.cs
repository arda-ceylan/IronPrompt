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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System.Collections.ObjectModel;
using IronPrompt.ViewModels;

namespace IronPrompt.Converters
{
    public class MarkdownTableControl : Grid
    {
        public static readonly StyledProperty<ObservableCollection<string>> HeadersProperty =
            AvaloniaProperty.Register<MarkdownTableControl, ObservableCollection<string>>(nameof(Headers));

        public static readonly StyledProperty<ObservableCollection<TableRowViewModel>> RowsProperty =
            AvaloniaProperty.Register<MarkdownTableControl, ObservableCollection<TableRowViewModel>>(nameof(Rows));

        public ObservableCollection<string> Headers
        {
            get => GetValue(HeadersProperty);
            set => SetValue(HeadersProperty, value);
        }

        public ObservableCollection<TableRowViewModel> Rows
        {
            get => GetValue(RowsProperty);
            set => SetValue(RowsProperty, value);
        }

        public MarkdownTableControl()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch;
            Margin = new Thickness(0, 5, 0, 15);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == HeadersProperty || change.Property == RowsProperty)
            {
                BuildTable();
            }
        }

        private void BuildTable()
        {
            Children.Clear();
            RowDefinitions.Clear();
            ColumnDefinitions.Clear();

            var headers = Headers;
            var rows = Rows;

            if (headers == null || headers.Count == 0) return;

            for (int colIndex = 0; colIndex < headers.Count; colIndex++)
            {
                ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
            }

            RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            for (int colIndex = 0; colIndex < headers.Count; colIndex++)
            {
                var border = new Border
                {
                    Background = new SolidColorBrush(Color.Parse("#1A1A2E")),
                    BorderBrush = new SolidColorBrush(Color.Parse("#3A3F58")),
                    BorderThickness = new Thickness(
                        colIndex == 0 ? 1 : 0, 
                        1, 
                        1, 
                        1),
                    Padding = new Thickness(12, 10),
                    Child = new TextBlock
                    {
                        Text = headers[colIndex],
                        Foreground = Brushes.White,
                        FontWeight = FontWeight.Bold,
                        FontSize = 14,
                        TextWrapping = TextWrapping.Wrap
                    }
                };

                Grid.SetRow(border, 0);
                Grid.SetColumn(border, colIndex);
                Children.Add(border);
            }

            if (rows != null)
            {
                int rowIndex = 1;
                foreach (var row in rows)
                {
                    RowDefinitions.Add(new RowDefinition(GridLength.Auto));

                    var bg = rowIndex % 2 == 0 ? Color.Parse("#131324") : Color.Parse("#1E1E30");
                    
                    for (int colIndex = 0; colIndex < headers.Count; colIndex++)
                    {
                        var cellText = colIndex < row.Cells.Count ? row.Cells[colIndex] : string.Empty;

                        var textBlock = new SelectableTextBlock
                        {
                            TextWrapping = TextWrapping.Wrap,
                            Foreground = new SolidColorBrush(Color.Parse("#E0E0FF")),
                            FontSize = 13,
                        };
                        MarkdownHelper.SetMarkdownText(textBlock, cellText);

                        var border = new Border
                        {
                            Background = new SolidColorBrush(bg),
                            BorderBrush = new SolidColorBrush(Color.Parse("#25283B")),
                            BorderThickness = new Thickness(
                                colIndex == 0 ? 1 : 0, 
                                0, 
                                1, 
                                1),
                            Padding = new Thickness(12, 10),
                            Child = textBlock
                        };

                        Grid.SetRow(border, rowIndex);
                        Grid.SetColumn(border, colIndex);
                        Children.Add(border);
                    }
                    rowIndex++;
                }
            }
        }
    }
}
