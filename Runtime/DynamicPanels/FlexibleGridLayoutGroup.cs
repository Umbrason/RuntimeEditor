using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FlexibleGridLayoutGroup : LayoutGroup
{
    public enum FitType
    {
        None,
        MaxColumns,
        MaxRows
    }
    public FitType fitType;

    public int columns;

    public int rows;
    public bool IsRowCountDynamic { get { return !(fitType == FitType.MaxColumns); } }
    public bool IsColumnCountDynamic { get { return !(fitType == FitType.MaxRows); } }

    public Vector2 spacing;
    public bool freeAspect = true;

    public Vector2 aspectRatio = Vector2.one;

    public override float minWidth { get { return 0; } }
    public override float minHeight { get { return 0; } }


    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        var activeChildren = from i in System.Linq.Enumerable.Range(0, rectTransform.childCount) where rectTransform.GetChild(i).gameObject.activeSelf && rectTransform.GetChild(i).GetComponent<RectTransform>() != null select rectTransform.GetChild(i).GetComponent<RectTransform>();
        if (activeChildren.Count() == 0)
            return;

        var sqrt = Mathf.Sqrt(activeChildren.Count());
        var width = rectTransform.rect.width;
        var height = rectTransform.rect.height;
        bool isVertical = fitType == FitType.MaxRows;
        bool isHorizontal = fitType == FitType.MaxColumns;

        var flexibleSizes = activeChildren.Select((child) =>
        {
            var layoutElement = child.GetComponent<LayoutElement>();
            var w = layoutElement ? layoutElement.flexibleWidth : 1;
            var h = layoutElement ? layoutElement.flexibleHeight : 1;
            return new Vector2(w, h);
        }).ToArray();

        switch (fitType)
        {
            case FitType.None:
                columns = Mathf.CeilToInt(sqrt);
                rows = columns;
                break;
            case FitType.MaxRows:
                columns = Mathf.CeilToInt(activeChildren.Count() / (float)rows);
                break;
            case FitType.MaxColumns:
                rows = Mathf.CeilToInt(activeChildren.Count() / (float)columns);
                break;
        }
        int outer = (isVertical ? columns : rows);
        int inner = (isHorizontal ? columns : rows);
        for (int i = 0; i < outer; i++)
        {
            int start = i * inner;
            int innerItemCount = Mathf.Min(inner, activeChildren.Count() - start);
            var flexSum = flexibleSizes.ToList().GetRange(start, innerItemCount).Aggregate(Vector2.zero, (sum, next) => next + sum);
            var itemGroupSize = 0f;
            for (int j = 0; j < inner; j++)
            {
                var index = i * inner + j;
                if (index >= activeChildren.Count())
                    break;
                var currentColumn = isVertical ? i : j;
                var currentRow = isHorizontal ? i : j;
                var item = activeChildren.ElementAt(index);

                var xPos = spacing.x * currentColumn + padding.left;
                var yPos = spacing.y * currentRow + padding.top;

                var xOffset = isHorizontal ? itemGroupSize : i * (width - padding.top - padding.bottom - spacing.x * (outer - 1)) / outer;
                var yOffset = isVertical ? itemGroupSize : i * (height - padding.top - padding.bottom - spacing.y * (outer - 1)) / outer;

                var flexWidth = isHorizontal ? flexibleSizes[index].x / flexSum.x : 1f / outer;
                var flexHeight = isVertical ? flexibleSizes[index].y / flexSum.y : 1f / outer;
                var itemWidth = (width - padding.left - padding.right - spacing.x * (isHorizontal ? innerItemCount - 1 : outer - 1)) * flexWidth;
                var itemHeight = (height - padding.top - padding.bottom - spacing.y * (isVertical ? innerItemCount - 1 : outer - 1)) * flexHeight;

                if (!freeAspect)
                {
                    var widthPercent = aspectRatio.x / aspectRatio.y;
                    var heightPercent = aspectRatio.y / aspectRatio.x;
                    if (itemHeight / aspectRatio.y > itemWidth / aspectRatio.x)
                        itemHeight = itemWidth * heightPercent;
                    else
                        itemWidth = itemHeight * widthPercent;
                }
                itemGroupSize += isHorizontal ? itemWidth : itemHeight;
                SetChildAlongAxis(item, 0, xPos + xOffset, itemWidth);
                SetChildAlongAxis(item, 1, yPos + yOffset, itemHeight);
            }
        }
    }

    public override void CalculateLayoutInputVertical()
    {

    }

    public override void SetLayoutHorizontal()
    {

    }

    public override void SetLayoutVertical()
    {

    }
}