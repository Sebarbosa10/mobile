using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Anuncios y tienda de prueba para el editor: no contacta ningún servicio ni cobra nada.
public sealed class SimulatedMonetization : MonoBehaviour, IAdsProvider, IStoreProvider
{
    private const int CanvasOrder = 100;
    private const int InterstitialSeconds = 3;

    private RectTransform banner;
    private Image interstitial;
    private UIButton closeButton;
    private OverlayPanel purchaseDialog;

    private Action interstitialClosed;
    private Action<bool> purchaseFinished;

    public static SimulatedMonetization Create()
    {
        UIKit.EnsureEventSystem();

        Canvas canvas = UIKit.CreateCanvas("MonetizationCanvas", CanvasOrder);
        DontDestroyOnLoad(canvas.gameObject);

        SimulatedMonetization simulated = canvas.gameObject.AddComponent<SimulatedMonetization>();
        RectTransform root = (RectTransform)canvas.transform;

        simulated.BuildBanner(root);
        simulated.BuildInterstitial(root);
        simulated.BuildPurchaseDialog(root);

        return simulated;
    }

    private void BuildBanner(RectTransform root)
    {
        UIPalette palette = UIKit.Palette;

        // Respeta solo el área segura real: el banner va pegado abajo, sin la reserva de la UI
        RectTransform area = UIKit.CreateRect(root, "BannerArea");
        UIKit.Stretch(area);
        area.gameObject.AddComponent<SafeAreaFitter>();

        Image image = UIKit.CreateImage(area, "Banner", null, palette.night);
        banner = image.rectTransform;
        banner.anchorMin = Vector2.zero;
        banner.anchorMax = Vector2.right;
        banner.pivot = new Vector2(0.5f, 0f);
        banner.anchoredPosition = Vector2.zero;
        banner.sizeDelta = new Vector2(0f, Monetization.BannerHeight);

        TextMeshProUGUI label = UIKit.CreateText(
            banner, "Label", "ANUNCIO DE PRUEBA", UIFont.Label, 40f, palette.labelMuted, 6f);
        UIKit.Stretch(label.rectTransform);

        banner.gameObject.SetActive(false);
    }

    private void BuildInterstitial(RectTransform root)
    {
        UIPalette palette = UIKit.Palette;

        interstitial = UIKit.CreateImage(root, "Interstitial", null, palette.night, raycast: true);
        UIKit.Stretch(interstitial.rectTransform);

        TextMeshProUGUI title = UIKit.CreateText(
            interstitial.rectTransform, "Title", "ANUNCIO", UIFont.Display, 180f, palette.text, 6f);
        UIKit.PlaceCenter(title.rectTransform, new Vector2(0f, 180f), new Vector2(980f, 210f));

        TextMeshProUGUI subtitle = UIKit.CreateText(
            interstitial.rectTransform, "Subtitle", "Simulación de un anuncio de pantalla completa",
            UIFont.Body, 40f, palette.labelMuted);
        UIKit.PlaceCenter(subtitle.rectTransform, new Vector2(0f, 30f), new Vector2(980f, 60f));

        closeButton = UIButton.Create(interstitial.rectTransform, "AdCloseButton", "CERRAR", ButtonVariant.Neutral);
        UIKit.PlaceCenter(closeButton.RectTransform, new Vector2(0f, -220f), closeButton.RectTransform.sizeDelta);
        closeButton.OnClick.AddListener(CloseInterstitial);

        interstitial.gameObject.SetActive(false);
    }

    private void BuildPurchaseDialog(RectTransform root)
    {
        UIPalette palette = UIKit.Palette;

        purchaseDialog = OverlayPanel.Create(root, "PurchaseDialog", 64f);
        RectTransform content = purchaseDialog.Content;

        TextMeshProUGUI title = UIKit.CreateText(
            content, "Title", "QUITAR ANUNCIOS", UIFont.Display, 96f, palette.text, 4f);
        title.rectTransform.sizeDelta = new Vector2(760f, 120f);
        purchaseDialog.Add(title, 120f);

        TextMeshProUGUI price = UIKit.CreateText(
            content, "Price", $"Pago único de {MonetizationConfig.FallbackPrice}", UIFont.Body, 44f, palette.text);
        price.rectTransform.sizeDelta = new Vector2(760f, 60f);
        purchaseDialog.Add(price, 60f, 24f);

        TextMeshProUGUI test = UIKit.CreateText(
            content, "TestNotice", "Compra de prueba: no se cobra nada", UIFont.Body, 32f, palette.labelMuted);
        test.rectTransform.sizeDelta = new Vector2(760f, 44f);
        purchaseDialog.Add(test, 44f, 12f);

        UIButton buy = UIButton.Create(content, "BuyButton", "COMPRAR", ButtonVariant.Primary);
        purchaseDialog.Add(buy, buy.LayoutHeight, 48f);
        buy.OnClick.AddListener(() => FinishPurchase(true));
        buy.ClickSound = null; // la compra exitosa toca ui_unlock

        UIButton cancel = UIButton.Create(content, "BackButton", "CANCELAR", ButtonVariant.Neutral);
        purchaseDialog.Add(cancel, cancel.LayoutHeight, 36f);
        cancel.OnClick.AddListener(() => FinishPurchase(false));

        purchaseDialog.Finish(72f);
    }

    public bool InterstitialReady => true;

    public bool PrivacyOptionsRequired => false;

    public string RemoveAdsPrice => MonetizationConfig.FallbackPrice;

    public void ShowPrivacyOptions()
    {
    }

    public void Shutdown()
    {
        banner.gameObject.SetActive(false);
    }

    void IAdsProvider.ShowBanner()
    {
        banner.gameObject.SetActive(true);
    }

    void IAdsProvider.HideBanner()
    {
        // Al salir de Play Mode el canvas puede destruirse antes que el menú
        if (banner != null)
            banner.gameObject.SetActive(false);
    }

    void IAdsProvider.ShowInterstitial(Action closed)
    {
        interstitialClosed = closed;

        interstitial.gameObject.SetActive(true);
        interstitial.transform.SetAsLastSibling();

        StartCoroutine(InterstitialCountdown());
    }

    // Como un anuncio real, no se puede cerrar hasta que pasan unos segundos
    private IEnumerator InterstitialCountdown()
    {
        closeButton.SetInteractable(false);

        for (int seconds = InterstitialSeconds; seconds > 0; seconds--)
        {
            closeButton.SetText($"CERRAR EN {seconds}");
            yield return new WaitForSecondsRealtime(1f);
        }

        closeButton.SetText("CERRAR");
        closeButton.SetInteractable(true);
    }

    private void CloseInterstitial()
    {
        interstitial.gameObject.SetActive(false);

        Action closed = interstitialClosed;
        interstitialClosed = null;
        closed?.Invoke();
    }

    void IStoreProvider.BuyRemoveAds(Action<bool> finished)
    {
        purchaseFinished = finished;
        purchaseDialog.Show();
    }

    private void FinishPurchase(bool purchased)
    {
        purchaseDialog.Hide();

        if (purchased)
            Monetization.GrantSimulatedPurchase();

        Action<bool> finished = purchaseFinished;
        purchaseFinished = null;
        finished?.Invoke(purchased);
    }
}
