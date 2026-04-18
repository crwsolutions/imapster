using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;


#if WINDOWS
using Microsoft.UI.Xaml;
using Microsoft.UI.Input;
using System.Reflection;
#endif

#if ANDROID
using Android.Views;
using Android.OS;
#endif

#if IOS || MACCATALYST
using UIKit;
#endif

namespace Imapster.Behaviors;

// =========================
// ENUM
// =========================
public enum CursorIcon
{
    // Basis cursors
    Arrow,
    Hand,
    IBeam,
    Cross,

    // Resize cursors
    SizeWestEast,              // ←→ Horizontale resize
    SizeNorthSouth,            // ↑↓ Verticale resize
    SizeAll,                   // ↕↔ Alle richtingen
    SizeNorthwestSoutheast,    // ↖↘ Diagonaal (linksboven-rechtsonder)
    SizeNortheastSouthwest,    // ↗↙ Diagonaal (rechtsboven-linksonder)

    // Algemene cursors
    Wait,                      // Draaiende cirkel / loading
    Help,                      // ?
    UniversalNo,               // Verboden / geen toegang

    // Grab cursors
    Grab,                      // Hand open (kan slepen)
    Grabbing,                  // Hand gesloten (sleept)

    // Windows-only cursors (fallback naar Arrow op andere platforms)
    Pin,                       // Hand met pin-symbool (Windows-only)
    Person,                    // Hand met persoon-symbool (Windows-only)
    AppStarting,               // App aan het starten (Windows-only, fallback naar Wait)
    UpArrow                    // Pijl naar boven (Windows-only)
}

// =========================
// ATTACHED PROPERTY
// =========================
public static class CursorBehavior
{
    public static readonly BindableProperty CursorProperty =
        BindableProperty.CreateAttached(
            "Cursor",
            typeof(CursorIcon),
            typeof(CursorBehavior),
            CursorIcon.Arrow,
            propertyChanged: OnCursorChanged);

    public static CursorIcon GetCursor(BindableObject view)
        => (CursorIcon)view.GetValue(CursorProperty);

    public static void SetCursor(BindableObject view, CursorIcon value)
        => view.SetValue(CursorProperty, value);

    private static void OnCursorChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is VisualElement element)
        {
            // 👇 altijd proberen
            if (element.Handler != null)
            {
                ApplyCursor(element, element.Handler);
            }

            // 👇 EN luisteren naar handler changes
            element.HandlerChanged -= OnHandlerChanged;
            element.HandlerChanged += OnHandlerChanged;
        }
    }

    private static void OnHandlerChanged(object? sender, EventArgs e)
    {
        if (sender is VisualElement element && element.Handler != null)
        {
            ApplyCursor(element, element.Handler);
        }
    }

    internal static void ApplyCursor(VisualElement element, IViewHandler handler)
    {
        var cursor = GetCursor(element);

#if WINDOWS
        if (handler.PlatformView is UIElement native)
        {
            var inputCursor = cursor switch
            {
                CursorIcon.Arrow => InputSystemCursor.Create(InputSystemCursorShape.Arrow),
                CursorIcon.Hand => InputSystemCursor.Create(InputSystemCursorShape.Hand),
                CursorIcon.IBeam => InputSystemCursor.Create(InputSystemCursorShape.IBeam),
                CursorIcon.Cross => InputSystemCursor.Create(InputSystemCursorShape.Cross),
                CursorIcon.SizeWestEast => InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast),
                CursorIcon.SizeNorthSouth => InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth),
                CursorIcon.SizeAll => InputSystemCursor.Create(InputSystemCursorShape.SizeAll),
                CursorIcon.SizeNorthwestSoutheast => InputSystemCursor.Create(InputSystemCursorShape.SizeNorthwestSoutheast),
                CursorIcon.SizeNortheastSouthwest => InputSystemCursor.Create(InputSystemCursorShape.SizeNortheastSouthwest),
                CursorIcon.Wait => InputSystemCursor.Create(InputSystemCursorShape.Wait),
                CursorIcon.Help => InputSystemCursor.Create(InputSystemCursorShape.Help),
                CursorIcon.UniversalNo => InputSystemCursor.Create(InputSystemCursorShape.UniversalNo),
                CursorIcon.Grab => InputSystemCursor.Create(InputSystemCursorShape.Arrow), // Fallback: geen directe Grab op Windows
                CursorIcon.Grabbing => InputSystemCursor.Create(InputSystemCursorShape.Arrow), // Fallback: geen directe Grabbing op Windows
                CursorIcon.Pin => InputSystemCursor.Create(InputSystemCursorShape.Pin),
                CursorIcon.Person => InputSystemCursor.Create(InputSystemCursorShape.Person),
                CursorIcon.AppStarting => InputSystemCursor.Create(InputSystemCursorShape.AppStarting),
                CursorIcon.UpArrow => InputSystemCursor.Create(InputSystemCursorShape.UpArrow),
                _ => InputSystemCursor.Create(InputSystemCursorShape.Arrow),
            };

            var prop = typeof(UIElement).GetProperty(
                "ProtectedCursor",
                BindingFlags.Instance | BindingFlags.NonPublic);

            prop?.SetValue(native, inputCursor);
        }
#endif

#if ANDROID
        if (Build.VERSION.SdkInt >= BuildVersionCodes.N &&
            handler.PlatformView is Android.Views.View nativeView)
        {
            var type = cursor switch
            {
                CursorIcon.Arrow => PointerIconType.Arrow,
                CursorIcon.Hand => PointerIconType.Hand,
                CursorIcon.IBeam => PointerIconType.Text,
                CursorIcon.Cross => PointerIconType.Crosshair,
                CursorIcon.SizeWestEast => PointerIconType.ColumnResize,
                CursorIcon.SizeNorthSouth => PointerIconType.RowResize,
                CursorIcon.SizeAll => PointerIconType.AllResize,
                CursorIcon.SizeNorthwestSoutheast => PointerIconType.TopLeftResize,
                CursorIcon.SizeNortheastSouthwest => PointerIconType.TopRightResize,
                CursorIcon.Wait => PointerIconType.Wait,
                CursorIcon.Help => PointerIconType.Help,
                CursorIcon.UniversalNo => PointerIconType.Disable,
                CursorIcon.Grab => PointerIconType.Grab,
                CursorIcon.Grabbing => PointerIconType.Grabbing,
                // Fallbacks voor Windows-only cursors
                CursorIcon.Pin => PointerIconType.Arrow,
                CursorIcon.Person => PointerIconType.Arrow,
                CursorIcon.AppStarting => PointerIconType.Wait,
                CursorIcon.UpArrow => PointerIconType.NorthPanning,
                _ => PointerIconType.Arrow
            };

            nativeView.PointerIcon = PointerIcon.GetSystemIcon(nativeView.Context, type);
        }
#endif

#if IOS
        if (handler.PlatformView is UIView nativeView)
        {
            nativeView.AddInteraction(new UIPointerInteraction(new PointerDelegate(cursor)));
        }

        class PointerDelegate : UIPointerInteractionDelegate
        {
            private readonly CursorIcon _cursor;

            public PointerDelegate(CursorIcon cursor)
            {
                _cursor = cursor;
            }

            public override UIPointerStyle GetStyleForRegion(UIPointerInteraction interaction, UIPointerRegion region)
            {
                var preview = _cursor switch
                {
                    CursorIcon.Arrow => UITargetedPreview.Create(region.Rect),
                    CursorIcon.Hand => UITargetedPreview.CreateWithCursor(region.Rect, UIStandardCursor.PointingHand),
                    CursorIcon.IBeam => UITargetedPreview.CreateWithCursor(region.Rect, UIStandardCursor.IBeam),
                    CursorIcon.Cross => UITargetedPreview.CreateWithCursor(region.Rect, UIStandardCursor.Cross),
                    CursorIcon.SizeWestEast => UITargetedPreview.CreateWithCursor(region.Rect, UIStandardCursor.ResizeLeftRight),
                    CursorIcon.SizeNorthSouth => UITargetedPreview.CreateWithCursor(region.Rect, UIStandardCursor.ResizeUpDown),
                    CursorIcon.SizeAll => UITargetedPreview.CreateWithCursor(region.Rect, UIStandardCursor.Resize),
                    CursorIcon.SizeNorthwestSoutheast => UITargetedPreview.CreateWithCursor(region.Rect, UIStandardCursor.ResizeUpLeft),
                    CursorIcon.SizeNortheastSouthwest => UITargetedPreview.CreateWithCursor(region.Rect, UIStandardCursor.ResizeUpRight),
                    CursorIcon.Wait => UITargetedPreview.CreateWithCursor(region.Rect, UIStandardCursor.Wait),
                    CursorIcon.Help => UITargetedPreview.CreateWithCursor(region.Rect, UIStandardCursor.Help),
                    CursorIcon.UniversalNo => UITargetedPreview.CreateWithCursor(region.Rect, UIStandardCursor.OperationNotAllowed),
                    CursorIcon.Grab => UITargetedPreview.CreateWithCursor(region.Rect, UIStandardCursor.Grab),
                    CursorIcon.Grabbing => UITargetedPreview.CreateWithCursor(region.Rect, UIStandardCursor.Grabbing),
                    _ => UITargetedPreview.Create(region.Rect)
                };
                
                return UIPointerStyle.Create(preview);
            }
        }
#endif

#if MACCATALYST
        if (handler.PlatformView is UIView nativeView)
        {
            nativeView.AddInteraction(new UIHoverGestureRecognizer(g =>
            {
                if (g.State == UIGestureRecognizerState.Changed)
                {
                    var nsCursor = cursor switch
                    {
                        CursorIcon.Arrow => AppKit.NSCursor.ArrowCursor,
                        CursorIcon.Hand => AppKit.NSCursor.PointingHandCursor,
                        CursorIcon.IBeam => AppKit.NSCursor.IBeamCursor,
                        CursorIcon.Cross => AppKit.NSCursor.CrosshairCursor,
                        CursorIcon.SizeWestEast => AppKit.NSCursor.ResizeLeftRightCursor,
                        CursorIcon.SizeNorthSouth => AppKit.NSCursor.ResizeUpDownCursor,
                        CursorIcon.SizeAll => AppKit.NSCursor.ResizeUpLeftDownRightCursor, // macOS heeft geen directe "SizeAll", dit is de dichtstbijzijnde
                        CursorIcon.SizeNorthwestSoutheast => AppKit.NSCursor.ResizeUpLeftDownRightCursor,
                        CursorIcon.SizeNortheastSouthwest => AppKit.NSCursor.ResizeUpRightDownLeftCursor,
                        CursorIcon.Wait => AppKit.NSCursor.ProgressCursor,
                        CursorIcon.Help => AppKit.NSCursor.ContextualMenuCursor,
                        CursorIcon.UniversalNo => AppKit.NSCursor.ForbiddenCursor,
                        CursorIcon.Grab => AppKit.NSCursor.OpenHandCursor,
                        CursorIcon.Grabbing => AppKit.NSCursor.ClosedHandCursor,
                        _ => AppKit.NSCursor.ArrowCursor
                    };
                    
                    nsCursor.Set();
                }
            }));
        }
#endif
    }
}

// =========================
// HANDLER REGISTRATION
// =========================
//public static class CursorHandlerRegistration
//{
//    public static void Init()
//    {
//        ViewHandler.ViewMapper.AppendToMapping("Cursor", (handler, view) =>
//        {
//            if (view is VisualElement element)
//            {
//                CursorBehavior.ApplyCursor(element, handler);
//            }
//        });
//    }
//}
