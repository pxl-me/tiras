using System.Windows;
using System.Windows.Controls;

namespace Oscilloscope.Client.Ui.Helpers;

public static class PasswordBoxAssistant
{
    public static readonly DependencyProperty BoundPasswordProperty =
        DependencyProperty.RegisterAttached(
            "BoundPassword",
            typeof(string),
            typeof(PasswordBoxAssistant),
            new FrameworkPropertyMetadata(string.Empty, OnBoundPasswordChanged));

    public static string GetBoundPassword(DependencyObject d)
        => (string)d.GetValue(BoundPasswordProperty);

    public static void SetBoundPassword(DependencyObject d, string value)
        => d.SetValue(BoundPasswordProperty, value);

    public static readonly DependencyProperty BindPasswordProperty =
        DependencyProperty.RegisterAttached(
            "BindPassword",
            typeof(bool),
            typeof(PasswordBoxAssistant),
            new PropertyMetadata(false, OnBindPasswordChanged));

    public static bool GetBindPassword(DependencyObject d)
        => (bool)d.GetValue(BindPasswordProperty);

    public static void SetBindPassword(DependencyObject d, bool value)
        => d.SetValue(BindPasswordProperty, value);

    private static readonly DependencyProperty IsUpdatingProperty =
        DependencyProperty.RegisterAttached(
            "IsUpdating",
            typeof(bool),
            typeof(PasswordBoxAssistant),
            new PropertyMetadata(false));

    private static bool GetIsUpdating(DependencyObject d)
        => (bool)d.GetValue(IsUpdatingProperty);

    private static void SetIsUpdating(DependencyObject d, bool value)
        => d.SetValue(IsUpdatingProperty, value);

    private static void OnBindPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox pb) return;

        if ((bool)e.OldValue)
            pb.PasswordChanged -= PasswordChanged;

        if ((bool)e.NewValue)
            pb.PasswordChanged += PasswordChanged;
    }

    private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox pb) return;
        if (!GetBindPassword(pb)) return;

        if (GetIsUpdating(pb)) return;

        pb.Password = e.NewValue?.ToString() ?? string.Empty;
    }

    private static void PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox pb) return;

        SetIsUpdating(pb, true);
        SetBoundPassword(pb, pb.Password);
        SetIsUpdating(pb, false);
    }
}