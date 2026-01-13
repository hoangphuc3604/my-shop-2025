using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;

namespace MyShop.Services
{
    /// <summary>
    /// Service to manage responsive layout behavior for UI pages
    /// Defines breakpoints and provides helper methods for responsive design
    /// </summary>
    public class ResponsiveService
    {
        /// <summary>
        /// Breakpoints for responsive design
        /// </summary>
        public class Breakpoints
        {
            public const double MOBILE = 600;      // Mobile devices
            public const double TABLET = 1024;     // Tablets
            public const double DESKTOP = 1440;    // Desktop
            public const double LARGE_DESKTOP = 1920; // Large desktop/4K
        }

        /// <summary>
        /// Get current viewport size category
        /// </summary>
        public static ViewportSize GetCurrentViewportSize(double width, double height)
        {
            if (width < Breakpoints.MOBILE)
                return ViewportSize.Mobile;
            else if (width < Breakpoints.TABLET)
                return ViewportSize.Tablet;
            else if (width < Breakpoints.LARGE_DESKTOP)
                return ViewportSize.Desktop;
            else
                return ViewportSize.LargeDesktop;
        }

        /// <summary>
        /// Determine if layout should be compact (stacked vertically)
        /// </summary>
        public static bool IsCompactLayout(double width)
        {
            return width < Breakpoints.TABLET;
        }

        /// <summary>
        /// Determine if layout should use multi-column layout
        /// </summary>
        public static bool IsWideLayout(double width)
        {
            return width >= Breakpoints.DESKTOP;
        }

        /// <summary>
        /// Calculate optimal column count for grid layouts
        /// </summary>
        public static int GetOptimalColumnCount(double width)
        {
            if (width < Breakpoints.MOBILE)
                return 1;
            else if (width < Breakpoints.TABLET)
                return 2;
            else if (width < Breakpoints.LARGE_DESKTOP)
                return 3;
            else
                return 4;
        }

        /// <summary>
        /// Calculate optimal padding/margin based on viewport
        /// </summary>
        public static double GetOptimalPadding(double width)
        {
            if (width < Breakpoints.MOBILE)
                return 8;
            else if (width < Breakpoints.TABLET)
                return 12;
            else if (width < Breakpoints.DESKTOP)
                return 16;
            else
                return 20;
        }
    }

    /// <summary>
    /// Viewport size categories
    /// </summary>
    public enum ViewportSize
    {
        Mobile,
        Tablet,
        Desktop,
        LargeDesktop
    }
}