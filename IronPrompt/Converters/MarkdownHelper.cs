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
using Avalonia.Controls.Documents;
using Avalonia.Media;
using System.Text;
using System.Text.RegularExpressions;

namespace IronPrompt.Converters
{
    public static class MarkdownHelper
    {
        public static readonly AttachedProperty<string> MarkdownTextProperty =
            AvaloniaProperty.RegisterAttached<SelectableTextBlock, string>("MarkdownText", typeof(MarkdownHelper), string.Empty);

        static MarkdownHelper()
        {
            MarkdownTextProperty.Changed.AddClassHandler<SelectableTextBlock>((tb, e) => OnMarkdownTextChanged(tb, e));
        }

        public static string GetMarkdownText(SelectableTextBlock element) => element.GetValue(MarkdownTextProperty);
        public static void SetMarkdownText(SelectableTextBlock element, string value) => element.SetValue(MarkdownTextProperty, value);

        private static void OnMarkdownTextChanged(SelectableTextBlock textBlock, AvaloniaPropertyChangedEventArgs e)
        {
            var text = e.GetNewValue<string>();
            
            if (textBlock.Inlines == null) return;
            textBlock.Inlines.Clear();
            
            if (string.IsNullOrEmpty(text)) return;

            var lines = text.Split('\n');
            var processedText = new StringBuilder();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmedLine = line.TrimStart();
                
                if (trimmedLine.StartsWith("* ") || trimmedLine.StartsWith("- "))
                {
                    int leadingSpacesCount = line.Length - trimmedLine.Length;
                    var indent = new string(' ', leadingSpacesCount);
                    
                    string bullet = (leadingSpacesCount > 0) ? "  ◦ " : "• ";

                    line = indent + bullet + trimmedLine.Substring(2);
                }
                
                processedText.Append(line);
                if (i < lines.Length - 1)
                {
                    processedText.Append('\n');
                }
            }
            
            string textToParse = processedText.ToString();

            var regex = new Regex(@"(\*\*[^*]+\*\*|\*[^*]+\*|`[^`]+`)", RegexOptions.Compiled);
            var matches = regex.Split(textToParse);

            foreach (var segment in matches)
            {
                if (string.IsNullOrEmpty(segment)) continue;

                if (segment.StartsWith("**") && segment.EndsWith("**"))
                {
                    var boldText = segment.Substring(2, segment.Length - 4);
                    textBlock.Inlines.Add(new Run(boldText)
                    {
                        FontWeight = FontWeight.Bold
                    });
                }
                else if (segment.StartsWith("*") && segment.EndsWith("*"))
                {
                    var italicText = segment.Substring(1, segment.Length - 2);
                    textBlock.Inlines.Add(new Run(italicText)
                    {
                        FontStyle = FontStyle.Italic
                    });
                }
                else if (segment.StartsWith("`") && segment.EndsWith("`"))
                {
                    var codeText = segment.Substring(1, segment.Length - 2);
                    textBlock.Inlines.Add(new Run(codeText)
                    {
                        FontFamily = new FontFamily("Consolas, Monospace"),
                        Foreground = new SolidColorBrush(Color.Parse("#FF9E64")),
                        FontWeight = FontWeight.SemiBold
                    });
                }
                else
                {
                    textBlock.Inlines.Add(new Run(segment));
                }
            }
        }
    }
}
