using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CAS.Behaviors
{
    class ScrollSynchronizeBehavior : Behavior<ScrollViewer>
    {
        protected override void OnAttached()
        { base.OnAttached(); }

        protected override void OnDetaching()
        { base.OnDetaching(); }

        static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(double), typeof(ScrollSynchronizeBehavior),
            new FrameworkPropertyMetadata((d, e) =>
            {
                ScrollSynchronizeBehavior scrollSynchronizer = (ScrollSynchronizeBehavior)d;
                double value = (double)e.NewValue;
                if (scrollSynchronizer.Orientation == Orientation.Horizontal)
                {
                    scrollSynchronizer.AssociatedObject.ScrollToHorizontalOffset(value);
                }
                else if (scrollSynchronizer.Orientation == Orientation.Vertical)
                {
                    scrollSynchronizer.AssociatedObject.ScrollToVerticalOffset(value);
                }
            }
            )
        );

        static readonly DependencyProperty OrientationProperty = DependencyProperty.Register("Orientation", typeof(Orientation), typeof(ScrollSynchronizeBehavior),
            new FrameworkPropertyMetadata(Orientation.Horizontal));

        public double Source
        {
            get => (double)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }
    }
}
