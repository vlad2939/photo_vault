using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace PhotoVault.App.Controls;

/// <summary>
/// Panou grid virtualizat pentru miniaturi (§6.1, §11): creează containere doar pentru
/// rândurile vizibile (+1 rând tampon), deci rămâne fluid și la 50.000 de poze.
/// Coloanele se adaptează la lățime; celulele se întind uniform pe tot rândul.
/// Suportă recycling (VirtualizingPanel.VirtualizationMode="Recycling").
/// </summary>
public class VirtualizingWrapPanel : VirtualizingPanel, IScrollInfo
{
    /// <summary>Spațiu liber la margini, ca umbra / efectul de hover al cardurilor să nu fie tăiate.</summary>
    private const double Edge = 6;

    public static readonly DependencyProperty ItemMinWidthProperty = DependencyProperty.Register(
        nameof(ItemMinWidth), typeof(double), typeof(VirtualizingWrapPanel),
        new FrameworkPropertyMetadata(200.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty ItemHeightProperty = DependencyProperty.Register(
        nameof(ItemHeight), typeof(double), typeof(VirtualizingWrapPanel),
        new FrameworkPropertyMetadata(214.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>
    /// Raport lățime/înălțime pentru zona imaginii; dacă e &gt; 0, înălțimea celulei se calculează
    /// din lățimea ei (imagine + <see cref="ItemFooterHeight"/>), altfel se folosește <see cref="ItemHeight"/>.
    /// </summary>
    public static readonly DependencyProperty ImageAspectRatioProperty = DependencyProperty.Register(
        nameof(ImageAspectRatio), typeof(double), typeof(VirtualizingWrapPanel),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty ItemFooterHeightProperty = DependencyProperty.Register(
        nameof(ItemFooterHeight), typeof(double), typeof(VirtualizingWrapPanel),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty SpacingProperty = DependencyProperty.Register(
        nameof(Spacing), typeof(double), typeof(VirtualizingWrapPanel),
        new FrameworkPropertyMetadata(12.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    private Size _extent;
    private Size _viewport;
    private double _offset;
    private int _columns = 1;
    private double _itemWidth;
    private double _itemHeight;

    public double ItemMinWidth
    {
        get => (double)GetValue(ItemMinWidthProperty);
        set => SetValue(ItemMinWidthProperty, value);
    }

    public double ItemHeight
    {
        get => (double)GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    public double ImageAspectRatio
    {
        get => (double)GetValue(ImageAspectRatioProperty);
        set => SetValue(ImageAspectRatioProperty, value);
    }

    public double ItemFooterHeight
    {
        get => (double)GetValue(ItemFooterHeightProperty);
        set => SetValue(ItemFooterHeightProperty, value);
    }

    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    private double RowStride => _itemHeight + Spacing;

    // ------------------------------------------------------------------ Layout

    protected override Size MeasureOverride(Size availableSize)
    {
        var owner = ItemsControl.GetItemsOwner(this);
        var count = owner?.Items.Count ?? 0;

        var width = double.IsInfinity(availableSize.Width) ? ItemMinWidth + 2 * Edge : availableSize.Width;
        var usable = Math.Max(0, width - 2 * Edge);
        _columns = Math.Max(1, (int)((usable + Spacing) / (ItemMinWidth + Spacing)));
        _itemWidth = Math.Max(0, (usable - Spacing * (_columns - 1)) / _columns);
        _itemHeight = ImageAspectRatio > 0 ? Math.Round(_itemWidth / ImageAspectRatio + ItemFooterHeight) : ItemHeight;

        var rows = (count + _columns - 1) / _columns;
        var extentHeight = rows == 0 ? 0 : rows * RowStride - Spacing + 2 * Edge;
        var viewportHeight = double.IsInfinity(availableSize.Height) ? extentHeight : availableSize.Height;
        UpdateScrollInfo(new Size(width, viewportHeight), new Size(width, extentHeight));

        var (first, last) = GetRealizationRange(count);
        RealizeItems(first, last);
        CleanUpItems(first, last);

        return new Size(width, viewportHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var generator = ItemContainerGenerator;
        var children = InternalChildren;
        for (var i = 0; i < children.Count; i++)
        {
            var index = generator.IndexFromGeneratorPosition(new GeneratorPosition(i, 0));
            if (index < 0) continue;
            children[i].Arrange(GetItemRect(index));
        }
        return finalSize;
    }

    private Rect GetItemRect(int index)
    {
        var row = index / _columns;
        var column = index % _columns;
        return new Rect(
            Edge + column * (_itemWidth + Spacing),
            Edge + row * RowStride - _offset,
            _itemWidth,
            _itemHeight);
    }

    private (int First, int Last) GetRealizationRange(int count)
    {
        if (count == 0) return (0, -1);
        var firstRow = Math.Max(0, (int)Math.Floor((_offset - Edge) / RowStride) - 1);
        var lastRow = (int)Math.Floor((_offset + _viewport.Height) / RowStride) + 1;
        var first = Math.Min(count - 1, firstRow * _columns);
        var last = Math.Min(count - 1, (lastRow + 1) * _columns - 1);
        return (first, last);
    }

    private void RealizeItems(int first, int last)
    {
        if (last < first) return;

        var children = InternalChildren;   // accesul inițializează generatorul
        var generator = ItemContainerGenerator;
        var start = generator.GeneratorPositionFromIndex(first);
        var childIndex = start.Offset == 0 ? start.Index : start.Index + 1;
        var itemSize = new Size(_itemWidth, _itemHeight);

        using (generator.StartAt(start, GeneratorDirection.Forward, allowStartAtRealizedItem: true))
        {
            for (var i = first; i <= last; i++, childIndex++)
            {
                if (generator.GenerateNext(out var isNewlyRealized) is not UIElement child) break;

                // Container nou sau reciclat (scos anterior din copii) → se inserează la poziția lui
                var attached = VisualTreeHelper.GetParent(child) == this;
                if (!attached)
                {
                    if (childIndex >= children.Count) AddInternalChild(child);
                    else InsertInternalChild(childIndex, child);
                }
                if (isNewlyRealized || !attached) generator.PrepareItemContainer(child);

                child.Measure(itemSize);
            }
        }
    }

    private void CleanUpItems(int first, int last)
    {
        var owner = ItemsControl.GetItemsOwner(this);
        var recycling = owner is not null && GetVirtualizationMode(owner) == VirtualizationMode.Recycling;
        var generator = ItemContainerGenerator;
        var children = InternalChildren;

        for (var i = children.Count - 1; i >= 0; i--)
        {
            var position = new GeneratorPosition(i, 0);
            var index = generator.IndexFromGeneratorPosition(position);
            if (index >= first && index <= last) continue;

            if (recycling && generator is IRecyclingItemContainerGenerator recycler)
                recycler.Recycle(position, 1);
            else
                generator.Remove(position, 1);
            RemoveInternalChildRange(i, 1);
        }
    }

    protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
    {
        switch (args.Action)
        {
            case NotifyCollectionChangedAction.Remove:
            case NotifyCollectionChangedAction.Replace:
            case NotifyCollectionChangedAction.Move:
                RemoveInternalChildRange(args.Position.Index, args.ItemUICount);
                break;
            case NotifyCollectionChangedAction.Reset:
                // Colecție nouă (ex. re-încărcare după indexare) → de la începutul listei
                SetVerticalOffset(0);
                break;
        }
        base.OnItemsChanged(sender, args);
    }

    protected override void BringIndexIntoView(int index)
    {
        if (_columns <= 0) return;
        var top = Edge + index / _columns * RowStride;
        if (top < _offset) SetVerticalOffset(top - Edge);
        else if (top + _itemHeight > _offset + _viewport.Height) SetVerticalOffset(top + _itemHeight + Edge - _viewport.Height);
        UpdateLayout();
    }

    // ------------------------------------------------------------------ IScrollInfo (doar vertical)

    public ScrollViewer? ScrollOwner { get; set; }
    public bool CanHorizontallyScroll { get; set; }
    public bool CanVerticallyScroll { get; set; }

    public double ExtentWidth => _extent.Width;
    public double ExtentHeight => _extent.Height;
    public double ViewportWidth => _viewport.Width;
    public double ViewportHeight => _viewport.Height;
    public double HorizontalOffset => 0;
    public double VerticalOffset => _offset;

    private void UpdateScrollInfo(Size viewport, Size extent)
    {
        if (viewport == _viewport && extent == _extent) return;
        _viewport = viewport;
        _extent = extent;
        _offset = Clamp(_offset);
        ScrollOwner?.InvalidateScrollInfo();
    }

    private double Clamp(double offset) => Math.Max(0, Math.Min(offset, _extent.Height - _viewport.Height));

    public void SetVerticalOffset(double offset)
    {
        offset = Clamp(offset);
        if (Math.Abs(offset - _offset) < 0.5) return;
        _offset = offset;
        ScrollOwner?.InvalidateScrollInfo();
        InvalidateMeasure();
    }

    public void SetHorizontalOffset(double offset) { }

    public void LineUp() => SetVerticalOffset(_offset - 48);
    public void LineDown() => SetVerticalOffset(_offset + 48);
    public void PageUp() => SetVerticalOffset(_offset - _viewport.Height * 0.9);
    public void PageDown() => SetVerticalOffset(_offset + _viewport.Height * 0.9);
    public void MouseWheelUp() => SetVerticalOffset(_offset - RowStride * 0.75);
    public void MouseWheelDown() => SetVerticalOffset(_offset + RowStride * 0.75);
    public void LineLeft() { }
    public void LineRight() { }
    public void PageLeft() { }
    public void PageRight() { }
    public void MouseWheelLeft() { }
    public void MouseWheelRight() { }

    public Rect MakeVisible(Visual visual, Rect rectangle)
    {
        var children = InternalChildren;
        for (var i = 0; i < children.Count; i++)
        {
            if (children[i] != visual && !visual.IsDescendantOf(children[i])) continue;

            var index = ItemContainerGenerator.IndexFromGeneratorPosition(new GeneratorPosition(i, 0));
            if (index >= 0) BringIndexIntoView(index);
            break;
        }
        return rectangle;
    }
}
