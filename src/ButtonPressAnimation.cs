namespace StudentPerformance;

// Attached property enables the same feedback for every button through its style.
public static class ButtonPressAnimation
{
    private const string AnimationName = "ButtonPressFeedback";

    public static readonly BindableProperty IsEnabledProperty = BindableProperty.CreateAttached(
        "IsEnabled", typeof(bool), typeof(ButtonPressAnimation), false,
        propertyChanged: OnIsEnabledChanged);

    public static bool GetIsEnabled(BindableObject view) => (bool)view.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(BindableObject view, bool value) => view.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(BindableObject view, object oldValue, object newValue)
    {
        if (view is not Button button) return;
        if ((bool)oldValue)
        {
            button.Pressed -= OnPressed;
            button.Released -= OnReleased;
            button.Clicked -= OnClicked;
            button.Unfocused -= OnUnfocused;
            button.Unloaded -= OnUnloaded;
            Reset(button);
        }
        if ((bool)newValue)
        {
            button.Pressed += OnPressed;
            button.Released += OnReleased;
            button.Clicked += OnClicked;
            button.Unfocused += OnUnfocused;
            button.Unloaded += OnUnloaded;
        }
    }

    private static void Animate(Button button, double scale, uint duration)
    {
        button.AbortAnimation(AnimationName);
        new Animation(value => button.Scale = value, button.Scale, scale, Easing.CubicOut)
            .Commit(button, AnimationName, length: duration);
    }

    private static void OnPressed(object? sender, EventArgs e)
    {
        if (sender is Button button) Animate(button, 0.96, 75);
    }

    private static void OnReleased(object? sender, EventArgs e)
    {
        if (sender is Button button) Animate(button, 1, 140);
    }

    private static void OnClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button) return;
        // Also provides feedback when activated with Enter or Space.
        button.AbortAnimation(AnimationName);
        var animation = new Animation();
        animation.Add(0, 0.3, new Animation(value => button.Scale = value, button.Scale, 0.96, Easing.CubicOut));
        animation.Add(0.3, 1, new Animation(value => button.Scale = value, 0.96, 1, Easing.CubicOut));
        animation.Commit(button, AnimationName, length: 180);
    }

    private static void OnUnfocused(object? sender, FocusEventArgs e)
    {
        if (sender is Button button) Animate(button, 1, 100);
    }

    private static void OnUnloaded(object? sender, EventArgs e)
    {
        if (sender is Button button) Reset(button);
    }

    private static void Reset(Button button)
    {
        button.AbortAnimation(AnimationName);
        button.Scale = 1;
    }
}
