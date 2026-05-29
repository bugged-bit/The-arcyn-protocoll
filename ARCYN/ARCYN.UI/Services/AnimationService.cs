using System.Windows;
using System.Windows.Media.Animation;

namespace ARCYN.UI.Services;

public sealed class AnimationService
{
    public static void FadeIn(UIElement target, double durationMs = 120, double from = 0, double to = 1, Action? onComplete = null)
    {
        // Cancel any pending opacity animation to avoid overlap
        target.BeginAnimation(UIElement.OpacityProperty, null);
        var anim = new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(durationMs))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            FillBehavior = FillBehavior.HoldEnd
        };
        if (onComplete != null)
            anim.Completed += (_, _) => onComplete();
        target.BeginAnimation(UIElement.OpacityProperty, anim);
    }

    public static void FadeOut(UIElement target, double durationMs = 100, Action? onComplete = null)
    {
        // Cancel any pending opacity animation to avoid overlap
        target.BeginAnimation(UIElement.OpacityProperty, null);
        var anim = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(durationMs))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn },
            FillBehavior = FillBehavior.HoldEnd
        };
        if (onComplete != null)
            anim.Completed += (_, _) => onComplete();
        target.BeginAnimation(UIElement.OpacityProperty, anim);
    }

    public static void ResizeTo(FrameworkElement target, double? toWidth, double? toHeight, double durationMs, EasingMode mode = EasingMode.EaseOut)
    {
        if (toWidth.HasValue)
        {
            target.BeginAnimation(FrameworkElement.WidthProperty, null);
            var anim = new DoubleAnimation(toWidth.Value, TimeSpan.FromMilliseconds(durationMs))
            {
                EasingFunction = new CubicEase { EasingMode = mode },
                FillBehavior = FillBehavior.HoldEnd
            };
            target.BeginAnimation(FrameworkElement.WidthProperty, anim);
        }
        if (toHeight.HasValue)
        {
            target.BeginAnimation(FrameworkElement.HeightProperty, null);
            var anim = new DoubleAnimation(toHeight.Value, TimeSpan.FromMilliseconds(durationMs))
            {
                EasingFunction = new CubicEase { EasingMode = mode },
                FillBehavior = FillBehavior.HoldEnd
            };
            target.BeginAnimation(FrameworkElement.HeightProperty, anim);
        }
    }
}
