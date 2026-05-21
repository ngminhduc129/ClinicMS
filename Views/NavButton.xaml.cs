using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ClinicMS.Views
{
    public partial class NavButton : UserControl
    {
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(NavButton), new PropertyMetadata(""));
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(string), typeof(NavButton), new PropertyMetadata(""));
        public static readonly DependencyProperty PageKeyProperty =
            DependencyProperty.Register(nameof(PageKey), typeof(string), typeof(NavButton), new PropertyMetadata("", OnActiveChanged));
        public static readonly DependencyProperty CurrentPageProperty =
            DependencyProperty.Register(nameof(CurrentPage), typeof(string), typeof(NavButton), new PropertyMetadata("", OnActiveChanged));
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(NavButton));
        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(NavButton));

        public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
        public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }
        public string PageKey { get => (string)GetValue(PageKeyProperty); set => SetValue(PageKeyProperty, value); }
        public string CurrentPage { get => (string)GetValue(CurrentPageProperty); set => SetValue(CurrentPageProperty, value); }
        public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
        public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

        public NavButton() { InitializeComponent(); }

        private static void OnActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NavButton nav)
                nav.Dispatcher.BeginInvoke(new Action(nav.UpdateActiveState),
                    System.Windows.Threading.DispatcherPriority.Render);
        }

        private void UpdateActiveState()
        {
            if (Btn == null || Btn.Template == null) return;
            Btn.ApplyTemplate();

            bool active = PageKey == CurrentPage;

            var bg        = (Border?)Btn.Template.FindName("Bg", Btn);
            var label     = (TextBlock?)Btn.Template.FindName("LabelText", Btn);
            var icon      = (TextBlock?)Btn.Template.FindName("IconText", Btn);
            var indicator = (Border?)Btn.Template.FindName("ActiveIndicator", Btn);

            var ease     = new CubicEase { EasingMode = EasingMode.EaseOut };
            var duration = TimeSpan.FromMilliseconds(200);

            // Nền: trắng mờ khi active, trong suốt khi không
            if (bg != null)
                bg.Background = active
                    ? new SolidColorBrush(Color.FromArgb(75, 255, 255, 255))
                    : Brushes.Transparent;

            // Chữ: sáng hoàn toàn khi active, mờ khi không
            if (label != null)
            {
                label.BeginAnimation(OpacityProperty, new DoubleAnimation
                {
                    To = active ? 1.0 : 0.55,
                    Duration = duration,
                    EasingFunction = ease
                });
                label.FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal;
            }

            // Icon: sáng hoàn toàn khi active, mờ khi không
            if (icon != null)
                icon.BeginAnimation(OpacityProperty, new DoubleAnimation
                {
                    To = active ? 1.0 : 0.55,
                    Duration = duration,
                    EasingFunction = ease
                });

            // Indicator bên trái: fade in/out + width
            if (indicator != null)
            {
                indicator.Visibility = Visibility.Visible;

                var fadeAnim = new DoubleAnimation
                {
                    To = active ? 1.0 : 0.0,
                    Duration = duration,
                    EasingFunction = ease
                };
                fadeAnim.Completed += (s, e) =>
                {
                    if (!active) indicator.Visibility = Visibility.Collapsed;
                };

                indicator.BeginAnimation(OpacityProperty, fadeAnim);
                indicator.BeginAnimation(WidthProperty, new DoubleAnimation
                {
                    To = active ? 4.0 : 0.0,
                    Duration = duration,
                    EasingFunction = ease
                });
            }
        }

        private void Btn_Click(object sender, RoutedEventArgs e)
        {
            if (Command?.CanExecute(CommandParameter) == true)
                Command.Execute(CommandParameter);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateActiveState();
        }
    }
}
