namespace View.Personal.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Layout;
    using Avalonia.Media;
    using Markdig;
    using Markdig.Syntax;
    using Markdig.Syntax.Inlines;
    using Markdig.Extensions.Tables;
    using Avalonia.Controls.Primitives;

    /// <summary>
    /// Provides functionality to render Markdown content as Avalonia UI controls.
    /// </summary>
    public static class MarkdownRenderer
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
          .UseAdvancedExtensions()
          .UsePipeTables()
          .Build();

        #endregion

        #region Constructors-and-Factories

        #endregion



        #region Public-Methods
        /// <summary>
        /// Renders a Markdown string to Avalonia UI controls.
        /// </summary>
        /// <param name="markdown">The Markdown string to render.</param>
        /// <returns>A control containing the rendered Markdown.</returns>
        public static Control Render(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return new SelectableTextBlock { Text = string.Empty };

            try
            {
                var document = Markdig.Markdown.Parse(markdown, Pipeline);
                return RenderDocument(document);

            }
            catch (Exception ex)
            {
                return new SelectableTextBlock
                {
                    Text = $"Markdown rendering error: {ex.Message}",
                    Foreground = Brushes.Red
                };
            }
        }
        #endregion

        #region Private-Methods

        /// <summary>
        /// Converts a MarkdownDocument into a vertical StackPanel containing rendered blocks.
        /// </summary>
        private static Control RenderDocument(MarkdownDocument document)
        {
            var panel = new StackPanel
            {
                Spacing = 8,
                Orientation = Orientation.Vertical
            };

            foreach (var block in document)
            {
                var control = RenderBlock(block);
                if (control != null)
                    panel.Children.Add(control);
            }

            return panel;
        }

        /// <summary>
        /// Renders a specific Markdown block element into a UI control.
        /// </summary>
        private static Control RenderBlock(Block block)
        {
            return block switch
            {
                HeadingBlock heading => RenderHeading(heading),
                ParagraphBlock paragraph => RenderParagraph(paragraph),
                ListBlock list => RenderList(list),
                CodeBlock code => RenderCodeBlock(code),
                QuoteBlock quote => RenderQuoteBlock(quote),
                ThematicBreakBlock => new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Color.Parse("#EAECF0")),
                    Margin = new Thickness(0, 4, 0, 4)
                },
                Table table => RenderTable(table),
                _ => new SelectableTextBlock { Text = block.ToString() }
            };
        }

        /// <summary>
        /// Renders a Markdown heading block with dynamic font size.
        /// </summary>
        private static Control RenderHeading(HeadingBlock heading)
        {
            var text = heading.Inline != null ? GetInlineText(heading.Inline) : string.Empty;
            var fontSize = heading.Level switch
            {
                1 => 24,
                2 => 20,
                3 => 18,
                4 => 16,
                5 => 14,
                _ => 14
            };

            return new SelectableTextBlock
            {
                Text = text,
                FontSize = fontSize,
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, heading.Level == 1 ? 10 : 5, 0, heading.Level == 1 ? 10 : 5)
            };
        }

        /// <summary>
        /// Renders a Markdown paragraph block with inline elements.
        /// </summary>
        private static Control RenderParagraph(ParagraphBlock paragraph)
        {
            if (paragraph.Inline == null)
                return new SelectableTextBlock { Text = string.Empty };

            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 2, 0, 2)
            };
            panel.Children.Add(RenderInlines(paragraph.Inline));
            return panel;
        }

        /// <summary>
        /// Renders a Markdown list block (ordered or unordered).
        /// </summary>
        private static Control RenderList(ListBlock list)
        {
            var panel = new StackPanel
            {
                Spacing = 4,
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 4, 0, 4)
            };

            int index = 1;
            foreach (var item in list)
            {
                if (item is ListItemBlock listItem)
                {
                    var itemPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8
                    };

                    // Add bullet or number
                    var bullet = new SelectableTextBlock
                    {
                        Text = list.IsOrdered ? $"{index++}." : "•",
                        VerticalAlignment = VerticalAlignment.Top,
                        Width = 20,
                        TextAlignment = TextAlignment.Right
                    };

                    itemPanel.Children.Add(bullet);

                    // Add content
                    var contentPanel = new StackPanel
                    {
                        Orientation = Orientation.Vertical,
                        Spacing = 4
                    };

                    foreach (var block in listItem)
                    {
                        var control = RenderBlock(block);
                        if (control != null)
                            contentPanel.Children.Add(control);
                    }

                    itemPanel.Children.Add(contentPanel);
                    panel.Children.Add(itemPanel);
                }
            }

            return panel;
        }


        /// <summary>
        /// Renders a Markdown code block, styled with monospaced font and border.
        /// </summary>
        private static Control RenderCodeBlock(CodeBlock codeBlock)
        {
            string code = string.Empty;

            if (codeBlock is FencedCodeBlock fencedCodeBlock)
            {
                var lines = fencedCodeBlock.Lines;
                if (lines.Count > 0)
                {
                    var codeLines = new List<string>();
                    foreach (var line in lines)
                    {
                        if (line is Markdig.Helpers.StringLine stringLine && stringLine.Slice.Text != null)
                        {
                            codeLines.Add(stringLine.Slice.Text.Substring(stringLine.Slice.Start, stringLine.Slice.Length));
                        }
                    }
                    code = string.Join(Environment.NewLine, codeLines);
                }
            }
            else if (codeBlock is CodeBlock indentedCodeBlock)
            {
                var lines = indentedCodeBlock.Lines;
                if (lines.Count > 0)
                {
                    var codeLines = new List<string>();
                    foreach (var line in lines)
                    {
                        if (line is Markdig.Helpers.StringLine stringLine && stringLine.Slice.Text != null)
                        {
                            codeLines.Add(stringLine.Slice.Text.Substring(stringLine.Slice.Start, stringLine.Slice.Length));
                        }
                    }
                    code = string.Join(Environment.NewLine, codeLines);
                }
            }

            var codeTextBlock = new SelectableTextBlock
            {
                Text = code,
                FontFamily = new FontFamily("Consolas, Menlo, Monaco, 'Courier New', monospace"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(8)
            };

            return new Border
            {
                Background = new SolidColorBrush(Color.Parse("#F5F7F9")),
                BorderBrush = new SolidColorBrush(Color.Parse("#E4E7EB")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 4, 0, 4),
                Child = codeTextBlock
            };
        }

        /// <summary>
        /// Renders a Markdown blockquote with vertical bar styling.
        /// </summary>
        private static Control RenderQuoteBlock(QuoteBlock quote)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 4
            };

            foreach (var block in quote)
            {
                var control = RenderBlock(block);
                if (control != null)
                    panel.Children.Add(control);
            }

            return new Border
            {
                BorderThickness = new Thickness(4, 0, 0, 0),
                BorderBrush = new SolidColorBrush(Color.Parse("#E4E7EB")),
                Padding = new Thickness(12, 4, 4, 4),
                Margin = new Thickness(0, 4, 0, 4),
                Child = panel
            };
        }

        /// <summary>
        /// Renders a Markdown table into a grid layout.
        /// </summary>
        private static Control RenderTable(Table table)
        {
            var grid = new Grid
            {
                Margin = new Thickness(0, 8, 0, 8),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            // Create a ScrollViewer to handle overflow
            var scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
                Content = grid
            };

            // Define rows and columns
            for (int i = 0; i < table.Count; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            // Make columns more responsive
            for (int i = 0; i < table.ColumnDefinitions.Count; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            // Add cells
            for (int rowIndex = 0; rowIndex < table.Count; rowIndex++)
            {
                var row = (TableRow)table[rowIndex];
                for (int colIndex = 0; colIndex < row.Count; colIndex++)
                {
                    if (colIndex < table.ColumnDefinitions.Count)
                    {
                        var cell = (TableCell)row[colIndex];
                        var cellContent = new StackPanel
                        {
                            Orientation = Orientation.Vertical,
                            Spacing = 2,
                            Margin = new Thickness(4),
                            HorizontalAlignment = HorizontalAlignment.Stretch
                        };

                        foreach (var block in cell)
                        {
                            var control = RenderBlock(block);
                            if (control != null)
                            {
                                if (control is TextBlock tb)
                                {
                                    tb.TextWrapping = TextWrapping.Wrap;
                                    tb.TextTrimming = TextTrimming.CharacterEllipsis;
                                    tb.Width = double.NaN;
                                    tb.Margin = new Thickness(2);
                                    tb.HorizontalAlignment = HorizontalAlignment.Stretch;
                                }
                                else if (control is SelectableTextBlock stb)
                                {
                                    stb.TextWrapping = TextWrapping.Wrap;
                                    stb.TextTrimming = TextTrimming.CharacterEllipsis;
                                    stb.Width = double.NaN;
                                    stb.Margin = new Thickness(2);
                                    stb.HorizontalAlignment = HorizontalAlignment.Stretch;
                                }

                                cellContent.Children.Add(control);
                            }
                        }

                        var border = new Border
                        {
                            BorderThickness = new Thickness(1),
                            BorderBrush = new SolidColorBrush(Color.Parse("#E4E7EB")),
                            Child = cellContent,
                            Padding = new Thickness(2),
                            HorizontalAlignment = HorizontalAlignment.Stretch
                        };

                        // Style header row
                        if (rowIndex == 0)
                        {
                            border.Background = new SolidColorBrush(Color.Parse("#F5F7F9"));
                            foreach (var child in cellContent.Children)
                            {
                                if (child is TextBlock textBlock)
                                {
                                    textBlock.FontWeight = FontWeight.Bold;
                                }
                                else if (child is SelectableTextBlock selectableTextBlock)
                                {
                                    selectableTextBlock.FontWeight = FontWeight.Bold;
                                }
                            }
                        }

                        Grid.SetRow(border, rowIndex);
                        Grid.SetColumn(border, colIndex);
                        grid.Children.Add(border);
                    }
                }
            }

            return scrollViewer;
        }

        /// <summary>
        /// Renders all inline elements within a container inline.
        /// </summary>
        private static Control RenderInlines(ContainerInline container)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 0
            };

            var currentInline = container.FirstChild;
            while (currentInline != null)
            {
                var control = RenderInline(currentInline);
                if (control != null)
                {
                    if (panel.Children.Count > 0)
                        control.Margin = new Thickness(2, 0, 0, 0);
                    panel.Children.Add(control);
                }
                currentInline = currentInline.NextSibling;
            }

            return panel;
        }

        /// <summary>
        /// Renders a single Markdown inline element into an Avalonia control.
        /// </summary>
        private static Control RenderInline(Inline inline)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    return new SelectableTextBlock { Text = literal.Content.ToString(), TextWrapping = TextWrapping.Wrap, MaxWidth = 610 };
                case EmphasisInline emphasis:
                    return RenderEmphasis(emphasis);
                case LineBreakInline _:
                    return new SelectableTextBlock { Text = Environment.NewLine };
                case CodeInline code:
                    return new Border
                    {
                        Background = new SolidColorBrush(Color.Parse("#F5F7F9")),
                        BorderBrush = new SolidColorBrush(Color.Parse("#E4E7EB")),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(2),
                        Padding = new Thickness(2, 0, 2, 0),
                        Child = new SelectableTextBlock
                        {
                            Text = code.Content,
                            FontFamily = new FontFamily("Consolas, Menlo, Monaco, 'Courier New', monospace"),
                        }
                    };
                case LinkInline link:
                    return RenderLink(link);
                case DelimiterInline delimiter:
                    if (delimiter.GetType().FullName?.Contains("PipeTableDelimiterInline") == true)
                    {
                        // This is a table delimiter, handle it properly
                        return new SelectableTextBlock { Text = string.Empty };
                    }
                    return new SelectableTextBlock { Text = delimiter.ToString() };
                default:
                    // For any other inline types, return an empty SelectableTextBlock to avoid errors.
                    return new SelectableTextBlock { Text = string.Empty };
            }
        }

        /// <summary>
        /// Renders bold or italic Markdown emphasis inline.
        /// </summary>
        private static Control RenderEmphasis(EmphasisInline emphasis)
        {
            var text = GetInlineText(emphasis);
            var textBlock = new SelectableTextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 610
            };

            if (emphasis.DelimiterCount == 2)
            {
                textBlock.FontWeight = FontWeight.Bold;
            }
            else if (emphasis.DelimiterCount == 1)
            {
                textBlock.FontStyle = FontStyle.Italic;
            }

            return textBlock;
        }

        /// <summary>
        /// Renders a Markdown hyperlink inline element.
        /// </summary>
        private static Control RenderLink(LinkInline link)
        {
            var text = GetInlineText(link);
            if (string.IsNullOrEmpty(text))
                text = link.Url;

            var button = new Button
            {
                Content = text,
                Padding = new Thickness(0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush(Color.Parse("#0472EF")),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };

            return button;
        }

        /// <summary>
        /// Recursively retrieves the plain text content from a container inline.
        /// </summary>
        private static string GetInlineText(ContainerInline container)
        {
            if (container == null)
                return string.Empty;

            var text = string.Empty;
            var inline = container.FirstChild;

            while (inline != null)
            {
                if (inline is LiteralInline literal)
                {
                    text += literal.Content.ToString();
                }
                else if (inline is ContainerInline containerInline)
                {
                    text += GetInlineText(containerInline);
                }

                inline = inline.NextSibling;
            }

            return text;
        }

        #endregion
    }
}