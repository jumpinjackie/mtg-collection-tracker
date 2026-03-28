using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MtgCollectionTracker.Views.Behaviors;

public sealed class CardFaceFlipBehavior
{
    private sealed class FlipState
    {
        public bool HasSeenFirstFaceState { get; set; }

        public CancellationTokenSource? AnimationCts { get; set; }
    }

    private static readonly Dictionary<Image, FlipState> States = new();

    public static readonly AttachedProperty<bool> EnabledProperty =
        AvaloniaProperty.RegisterAttached<CardFaceFlipBehavior, Image, bool>("Enabled");

    public static readonly AttachedProperty<bool> FaceStateProperty =
        AvaloniaProperty.RegisterAttached<CardFaceFlipBehavior, Image, bool>("FaceState");

    static CardFaceFlipBehavior()
    {
        EnabledProperty.Changed.AddClassHandler<Image>(OnEnabledChanged);
        FaceStateProperty.Changed.AddClassHandler<Image>(OnFaceStateChanged);
    }

    private CardFaceFlipBehavior()
    {
    }

    public static void SetEnabled(Image element, bool value) => element.SetValue(EnabledProperty, value);

    public static bool GetEnabled(Image element) => element.GetValue(EnabledProperty);

    public static void SetFaceState(Image element, bool value) => element.SetValue(FaceStateProperty, value);

    public static bool GetFaceState(Image element) => element.GetValue(FaceStateProperty);

    private static void OnEnabledChanged(Image image, AvaloniaPropertyChangedEventArgs e)
    {
        var enabled = e.GetNewValue<bool>();
        var state = GetOrCreateState(image);

        if (!enabled)
        {
            state.AnimationCts?.Cancel();
            state.AnimationCts?.Dispose();
            state.AnimationCts = null;
        }

        state.HasSeenFirstFaceState = false;
    }

    private static void OnFaceStateChanged(Image image, AvaloniaPropertyChangedEventArgs e)
    {
        if (!GetEnabled(image))
        {
            return;
        }

        var state = GetOrCreateState(image);
        if (!state.HasSeenFirstFaceState)
        {
            state.HasSeenFirstFaceState = true;
            return;
        }

        _ = RunFlipAnimationAsync(image, state);
    }

    private static FlipState GetOrCreateState(Image image)
    {
        if (!States.TryGetValue(image, out var state))
        {
            state = new FlipState();
            States.Add(image, state);
            image.DetachedFromVisualTree += (_, _) => Cleanup(image);
        }

        return state;
    }

    private static void Cleanup(Image image)
    {
        if (!States.TryGetValue(image, out var state))
        {
            return;
        }

        state.AnimationCts?.Cancel();
        state.AnimationCts?.Dispose();
        States.Remove(image);
    }

    private static async Task RunFlipAnimationAsync(Image image, FlipState state)
    {
        var scale = EnsureScaleTransform(image);

        state.AnimationCts?.Cancel();
        state.AnimationCts?.Dispose();
        state.AnimationCts = new CancellationTokenSource();
        var token = state.AnimationCts.Token;
        const int halfFrames = 7;
        const int frameDelayMs = 16;

        try
        {
            var totalFrames = halfFrames * 2;
            for (var frame = 0; frame <= totalFrames; frame++)
            {
                token.ThrowIfCancellationRequested();
                var t = EaseInOutCubic(frame / (double)totalFrames);
                ApplyVerticalAxisTurn(scale, t);
                await Task.Delay(frameDelayMs, token);
            }

            ResetTransform(scale);
        }
        catch (TaskCanceledException)
        {
            // Ignore cancellation when users rapidly switch faces.
        }
    }

    private static ScaleTransform EnsureScaleTransform(Image image)
    {
        if (image.RenderTransform is ScaleTransform existingScale)
        {
            return existingScale;
        }

        ScaleTransform scale;
        if (image.RenderTransform is TransformGroup existingGroup &&
            existingGroup.Children.Count > 0 &&
            existingGroup.Children[0] is ScaleTransform initialScale)
        {
            scale = initialScale;
        }
        else
        {
            scale = new ScaleTransform(1d, 1d);
        }

        image.RenderTransform = scale;
        return scale;
    }

    private static void ApplyVerticalAxisTurn(ScaleTransform scale, double t)
    {
        // 0..pi models a full front->edge->back turn around the vertical axis.
        var angle = Math.PI * t;
        var width = Math.Abs(Math.Cos(angle));

        scale.ScaleX = Math.Max(0.03d, width);
    }

    private static void ResetTransform(ScaleTransform scale)
    {
        scale.ScaleX = 1d;
        scale.ScaleY = 1d;
    }

    private static double EaseInOutCubic(double t)
    {
        if (t < 0.5d)
        {
            return 4d * t * t * t;
        }

        var inverse = -2d * t + 2d;
        return 1d - ((inverse * inverse * inverse) / 2d);
    }
}