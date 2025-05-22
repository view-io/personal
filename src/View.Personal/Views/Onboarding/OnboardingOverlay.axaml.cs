using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Reactive;
using Avalonia.Styling;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using View.Personal.Classes;

namespace View.Personal.Views
{
    /// <summary>
    /// Represents the onboarding overlay that guides the user through application features.
    /// </summary>
    public partial class OnboardingOverlay : UserControl
    {
        #region Public-Members

        /// <summary>
        /// Identifies the IsVisible Avalonia property.
        /// </summary>
        public new static readonly StyledProperty<bool> IsVisibleProperty =
            AvaloniaProperty.Register<OnboardingOverlay, bool>(nameof(IsVisible), false);

        /// <summary>
        /// Gets or sets whether the overlay is visible.
        /// </summary>
        public new bool IsVisible
        {
            get => GetValue(IsVisibleProperty);
            set
            {
                SetValue(IsVisibleProperty, value);
                if (IsInitialized)
                {
                    ((Control)this).IsVisible = value;
                    var root = this.FindControl<Canvas>("OverlayRoot");
                    root?.SetValue(IsVisibleProperty, value);
                }
            }
        }

        #endregion

        #region Private-Members

        private readonly List<OnboardingStep> _steps = new()
        {
            new OnboardingStep { TargetName = "SettingsPanel", Title = "Settings", Description = "Configure your LLM and API keys here." },
            new OnboardingStep { TargetName = "Files", Title = "Files", Description = "Manage the files you want to chat with." },
            new OnboardingStep { TargetName = "Data Monitor", Title = "Data Monitor", Description = "Live updates for connected files and services." },
            new OnboardingStep { TargetName = "StartNewChatButton", Title = "Start New Chat", Description = "Start a fresh conversation with your assistant." },
            new OnboardingStep { TargetName = "Console", Title = "Console", Description = "Inspect logs and debug details." }
        };

        private int _currentStep = 0;
        private Window? _mainWindow;
        private Action? _onComplete;
        private Control? _lastTarget = null;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="OnboardingOverlay"/> class.
        /// </summary>
        public OnboardingOverlay()
        {
            InitializeComponent();
            DataContext = this;
            IsVisible = false;
            var nextButton = this.FindControl<Button>("NextButton");
            if (nextButton != null)
                nextButton.Click += NextButton_Click;

            var skipButton = this.FindControl<Button>("SkipButton");
            if (skipButton != null)
                skipButton.Click += SkipButton_Click;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Starts the onboarding sequence.
        /// </summary>
        /// <param name="mainWindow">The main application window.</param>
        /// <param name="onComplete">The callback to execute upon completion.</param>
        public void Start(Window mainWindow, Action onComplete)
        {
            _mainWindow = mainWindow;
            _onComplete = onComplete;
            _currentStep = 0;
            IsVisible = true;

            var root = this.FindControl<Canvas>("OverlayRoot");
            if (root != null) root.IsVisible = true;

            _mainWindow.GetObservable(Window.BoundsProperty)
                .Subscribe(new AnonymousObserver<Rect>(_ => HighlightTarget(_steps[_currentStep].TargetName)));

            DispatcherTimer.RunOnce(() => ShowStep(), TimeSpan.FromMilliseconds(200));
        }

        #endregion

        #region Event-Handlers

        private void NextButton_Click(object? sender, RoutedEventArgs e)
        {
            ClearPreviousHighlight();
            if (_currentStep < _steps.Count - 1)
            {
                _currentStep++;
                ShowStep();
            }
            else
            {
                CompleteWalkthrough();
            }
        }

        private void SkipButton_Click(object? sender, RoutedEventArgs e)
        {
            ClearPreviousHighlight();
            CompleteWalkthrough();
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Clears the previous step's highlight class.
        /// </summary>
        private void ClearPreviousHighlight()
        {
            foreach (var step in _steps)
            {
                var control = _mainWindow?.FindControl<Control>(step.TargetName);
                control?.Classes.Remove("highlighted-onboarding");
            }
        }

        /// <summary>
        /// Completes the onboarding walkthrough and saves the state.
        /// </summary>
        private void CompleteWalkthrough()
        {
            IsVisible = false;

            try
            {
                string onboardingDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ViewPersonal", "data");
                string onboardingPath = Path.Combine(onboardingDir, "onboarding.json");

                if (!Directory.Exists(onboardingDir))
                {
                    Directory.CreateDirectory(onboardingDir);
                }

                var state = new OnboardingState { Completed = true };
                var json = System.Text.Json.JsonSerializer.Serialize(state);
                File.WriteAllText(onboardingPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to save onboarding state: {ex.Message}");
            }

            _onComplete?.Invoke();
        }

        /// <summary>
        /// Displays the current onboarding step.
        /// </summary>
        private void ShowStep()
        {
            var step = _steps[_currentStep];

            var titleBlock = this.FindControl<TextBlock>("TooltipTitle");
            if (titleBlock != null)
                titleBlock.Text = step.Title;

            var descBlock = this.FindControl<TextBlock>("TooltipDescription");
            if (descBlock != null)
                descBlock.Text = step.Description;

            var nextButton = this.FindControl<Button>("NextButton");
            if (nextButton != null)
                nextButton.Content = _currentStep == _steps.Count - 1 ? "Finish" : "Next";

            HighlightTarget(step.TargetName);
        }

        /// <summary>
        /// Highlights the UI element for the current onboarding step.
        /// </summary>
        /// <param name="targetName">The name of the control to highlight.</param>
        private void HighlightTarget(string targetName)
        {
            if (_mainWindow == null)
                return;

            var highlight = this.FindControl<Border>("HighlightBorder");
            var tooltip = this.FindControl<StackPanel>("TooltipPanel");
            var tooltipBorder = this.FindControl<Border>("TooltipPanelBorder");
            var navList = _mainWindow.FindControl<ListBox>("NavList");

            if (highlight == null || tooltip == null || tooltipBorder == null)
                return;

            Control? target = null;

            if (targetName is "Files" or "Data Monitor" or "Settings" or "Console")
            {
                if (navList != null)
                {
                    foreach (var item in navList.Items)
                    {
                        if (item is ListBoxItem lbi && lbi.Tag?.ToString() == targetName)
                        {
                            target = lbi;
                            break;
                        }

                        if (item is ListBoxItem lbi2 && lbi2.Content is StackPanel sp)
                        {
                            foreach (var child in sp.Children)
                            {
                                if (child is TextBlock tb && tb.Text == targetName)
                                {
                                    target = lbi2;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            target ??= _mainWindow.FindControl<Control>(targetName);

            if (target == null)
            {
                highlight.IsVisible = false;
                return;
            }

            _lastTarget = target;

            Dispatcher.UIThread.Post(() =>
            {
                if (target.Bounds.Width <= 0 || target.Bounds.Height <= 0)
                {
                    highlight.IsVisible = false;
                    return;
                }

                var point = target.TranslatePoint(new Point(0, 0), _mainWindow);
                if (!point.HasValue)
                {
                    highlight.IsVisible = false;
                    return;
                }

                target.Classes.Add("highlighted-onboarding");

                highlight.IsVisible = true;
                Canvas.SetLeft(highlight, point.Value.X - 6);
                Canvas.SetTop(highlight, point.Value.Y - 6);
                highlight.Width = target.Bounds.Width + 12;
                highlight.Height = target.Bounds.Height + 12;

                const double tooltipWidth = 340;
                const double margin = 16;

                double tooltipLeft = point.Value.X + target.Bounds.Width + margin;
                double tooltipBottomAlignedTop = point.Value.Y + target.Bounds.Height - tooltipBorder.Bounds.Height;
                double tooltipTop = Math.Max(tooltipBottomAlignedTop, 24);

                if (tooltipLeft + tooltipWidth > _mainWindow.Bounds.Width)
                {
                    tooltipLeft = _mainWindow.Bounds.Width - tooltipWidth - 24;
                }

                Canvas.SetLeft(tooltipBorder, tooltipLeft);
                Canvas.SetTop(tooltipBorder, tooltipTop);
                tooltip.MaxWidth = tooltipWidth;
                tooltip.Opacity = 1;

            }, DispatcherPriority.Render);
        }

        #endregion
    }

   
}