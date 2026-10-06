using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ChallengeCardState
{
    Unlocked,
    Locked,
    Completed
}

/// <summary>
/// Tarjeta del selector de desafíos, 984×280 más el labio de 12:
/// número en un círculo, nombre (Anton 68), descripción (Barlow 34) y un
/// ícono a la derecha.
///  - Desbloqueada: azul, número en círculo Noche, flecha de jugar.
///  - Bloqueada: gris, candado, "Requiere N encestadas". Al tocarla vibra
///    6 px en horizontal durante 0,15 s y no hace nada más.
///  - Completada: azul con borde verde de 8 px y un check en el círculo.
/// </summary>
public sealed class ChallengeCard : MonoBehaviour
{
    public static readonly Vector2 Size = new(984f, 280f);

    private const float ShakeDistance = 6f;
    private const float ShakeDuration = 0.15f;

    private ChallengeDefinition definition;

    private Button button;
    private PressableFace pressable;
    private RectTransform content;

    private Image border;
    private Image numberDisc;
    private TextMeshProUGUI numberText;
    private Image checkIcon;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI descriptionText;
    private Image playIcon;
    private Image lockIcon;

    private ChallengeCardState state;
    private Coroutine shakeRoutine;

    /// <summary>Se tocó una tarjeta jugable (desbloqueada o completada).</summary>
    public event Action<ChallengeDefinition> Selected;

    public RectTransform RectTransform =>
        (RectTransform)transform;

    public static ChallengeCard Create(Transform parent, ChallengeDefinition definition)
    {
        UISkin skin = UIKit.Skin;
        UIPalette palette = UIKit.Palette;

        RectTransform root = UIKit.CreateRect(parent, $"Challenge{definition.Number}Card");
        root.sizeDelta = new Vector2(Size.x, Size.y + UIKit.ButtonLip);

        ChallengeCard card = root.gameObject.AddComponent<ChallengeCard>();
        card.definition = definition;

        card.button = root.gameObject.AddComponent<Button>();
        card.button.transition = Selectable.Transition.None;
        card.button.onClick.AddListener(card.HandleClick);

        Image face = UIKit.CreateImage(root, "Face", skin.buttonBase, Color.white, raycast: true);
        UIKit.Stretch(face.rectTransform);

        card.content = UIKit.CreateRect(face.rectTransform, "Content");
        UIKit.Stretch(card.content);
        card.content.offsetMin = new Vector2(0f, UIKit.ButtonLip);

        // Borde de "completada": marco de 8 px sobre la cara (sin el labio).
        card.border = UIKit.CreateImage(card.content, "Border", skin.panelBorder, palette.success);
        UIKit.Stretch(card.border.rectTransform);
        card.border.pixelsPerUnitMultiplier = 48f / 36f;

        // Círculo con el número (o el check).
        card.numberDisc = UIKit.CreateImage(card.content, "NumberDisc", skin.disc, palette.night);
        UIKit.Place(
            card.numberDisc.rectTransform,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(40f, 0f), new Vector2(150f, 150f));

        card.numberText = UIKit.CreateText(
            card.numberDisc.rectTransform, "Number", definition.Number.ToString(),
            UIFont.Display, 96f, palette.text);
        UIKit.Stretch(card.numberText.rectTransform);

        card.checkIcon = UIKit.CreateImage(card.numberDisc.rectTransform, "Check", skin.iconCheck, palette.onSuccess);
        UIKit.PlaceCenter(card.checkIcon.rectTransform, Vector2.zero, new Vector2(92f, 92f));

        // Nombre + descripción, centrados en vertical.
        RectTransform textBlock = UIKit.CreateRect(card.content, "TextBlock");
        textBlock.anchorMin = new Vector2(0f, 0.5f);
        textBlock.anchorMax = new Vector2(1f, 0.5f);
        textBlock.pivot = new Vector2(0.5f, 0.5f);
        textBlock.offsetMin = new Vector2(40f + 150f + 32f, 0f);
        textBlock.offsetMax = new Vector2(-(40f + 72f + 32f), 0f);

        VerticalLayoutGroup layout = textBlock.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = textBlock.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        card.titleText = UIKit.CreateText(
            textBlock, "Title", definition.Name.ToUpperInvariant(),
            UIFont.Display, 68f, palette.text, 2f, TextAlignmentOptions.Left);

        card.descriptionText = UIKit.CreateText(
            textBlock, "Description", definition.Description,
            UIFont.Body, 34f, palette.text, 0f, TextAlignmentOptions.Left);
        card.descriptionText.textWrappingMode = TextWrappingModes.Normal;

        // Ícono derecho: flecha o candado.
        card.playIcon = UIKit.CreateImage(card.content, "Play", skin.iconPlay, palette.text);
        UIKit.Place(
            card.playIcon.rectTransform,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-44f, 0f), new Vector2(64f, 64f));

        card.lockIcon = UIKit.CreateImage(card.content, "Lock", skin.iconLock, palette.lockedSubtext);
        UIKit.Place(
            card.lockIcon.rectTransform,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-40f, 0f), new Vector2(72f, 72f));

        card.pressable = root.gameObject.AddComponent<PressableFace>();
        card.pressable.Configure(face, card.content, card.button);

        card.SetState(ChallengeCardState.Unlocked);

        return card;
    }

    public void SetState(ChallengeCardState newState)
    {
        state = newState;

        UIPalette palette = UIKit.Palette;

        bool locked = state == ChallengeCardState.Locked;
        bool completed = state == ChallengeCardState.Completed;

        pressable.SetColors(
            locked ? palette.locked : palette.primary,
            locked ? palette.locked : palette.primaryPressed);
        pressable.SetPressEnabled(!locked);

        border.gameObject.SetActive(completed);

        numberDisc.color = locked
            ? palette.lockedLip
            : completed ? palette.success : palette.night;

        numberText.gameObject.SetActive(!completed);
        numberText.color = locked ? palette.lockedText : palette.text;
        checkIcon.gameObject.SetActive(completed);

        titleText.color = locked ? palette.lockedText : palette.text;

        descriptionText.color = locked ? palette.lockedSubtext : palette.text;
        descriptionText.text = state switch
        {
            ChallengeCardState.Locked => $"Requiere {definition.RequiredBaskets} encestadas",
            ChallengeCardState.Completed => "Completado. Tocá para volver a jugarlo.",
            _ => definition.Description
        };

        playIcon.gameObject.SetActive(!locked);
        lockIcon.gameObject.SetActive(locked);
    }

    private void HandleClick()
    {
        if (state == ChallengeCardState.Locked)
        {
            Shake();
            return;
        }

        Selected?.Invoke(definition);
    }

    private void Shake()
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        yield return UITween.Run(ShakeDuration, t =>
        {
            // Tres idas y vueltas que se amortiguan.
            float offset = Mathf.Sin(t * Mathf.PI * 6f) * ShakeDistance * (1f - t);
            content.anchoredPosition = new Vector2(offset, content.anchoredPosition.y);
        });

        content.anchoredPosition = new Vector2(0f, content.anchoredPosition.y);
        shakeRoutine = null;
    }
}
