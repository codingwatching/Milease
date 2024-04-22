using System.Collections;
using System.Collections.Generic;
using Milease.Core.Animator;
using Milease.Core.UI;
using Milease.Enums;
using Milease.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TestData
{
    public string Name;
}

public class MilListItemDemo : MilListViewItem
{
    public TMP_Text Content, Arrow;
    public Image Background;

    protected override IEnumerable<MilStateParameter> ConfigDefaultState()
        => new[]
        {
            Background.MilState("color", Color.clear),
            Content.rectTransform.MilState(nameof(Content.rectTransform.anchoredPosition), new Vector2(138f, 2.3f),
                EaseFunction.Back, EaseType.Out),
            Arrow.MilState("color", Color.clear),
            Arrow.rectTransform.MilState(nameof(Arrow.rectTransform.anchoredPosition), new Vector2(88f, 2.3f),
                EaseFunction.Back, EaseType.Out),
            Content.MilState("color", new Color(1f, 1f, 1f, 0.7f))
        };

    protected override IEnumerable<MilStateParameter> ConfigSelectedState()
        => new[]
        {
            Background.MilState("color", ColorUtils.RGB(132, 115, 186)),
            Content.rectTransform.MilState(nameof(Content.rectTransform.anchoredPosition), new Vector2(186f, 2.3f),
                EaseFunction.Back, EaseType.Out),
            Arrow.MilState("color", Color.white),
            Arrow.rectTransform.MilState(nameof(Arrow.rectTransform.anchoredPosition), new Vector2(138f, 2.3f),
                EaseFunction.Back, EaseType.Out),
            Content.MilState("color", Color.white)
        };

    protected override void OnSelect(PointerEventData eventData)
    {

    }

    protected override MilInstantAnimator ConfigClickAnimation()
        => null;

    public override void UpdateAppearance()
    {
        var data = Binding as TestData;
        Content.text = data.Name;
    }

    public override void AdjustAppearance(float pos)
    {

    }
}
