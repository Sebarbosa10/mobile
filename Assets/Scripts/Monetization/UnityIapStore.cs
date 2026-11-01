using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Purchasing;

// Compra real con Google Play Billing (Unity IAP 5). "Sin anuncios" es un producto no consumible:
// al abrir el juego se consultan las compras, así se recupera solo si el jugador reinstala.
public sealed class UnityIapStore : IStoreProvider
{
    private readonly Action granted;
    private readonly Action changed;

    private StoreController controller;
    private Product product;
    private Action<bool> purchaseFinished;

    public UnityIapStore(Action granted, Action changed)
    {
        this.granted = granted;
        this.changed = changed;

        Connect();
    }

    public string RemoveAdsPrice =>
        product?.metadata?.localizedPriceString ?? MonetizationConfig.FallbackPrice;

    private async void Connect()
    {
        try
        {
            await UnityServices.InitializeAsync();

            controller = UnityIAPServices.StoreController();

            controller.OnStoreConnected += HandleStoreConnected;
            controller.OnStoreDisconnected += HandleStoreDisconnected;
            controller.OnProductsFetched += HandleProductsFetched;
            controller.OnProductsFetchFailed += HandleProductsFetchFailed;
            controller.OnPurchasesFetched += HandlePurchasesFetched;
            controller.OnPurchasesFetchFailed += HandlePurchasesFetchFailed;
            controller.OnPurchasePending += HandlePurchasePending;
            controller.OnPurchaseConfirmed += HandlePurchaseConfirmed;
            controller.OnPurchaseFailed += HandlePurchaseFailed;
            controller.OnPurchaseDeferred += HandlePurchaseDeferred;

            await controller.Connect();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[IAP] No se pudo conectar la tienda: {exception.Message}");
        }
    }

    public void BuyRemoveAds(Action<bool> finished)
    {
        if (controller == null || product == null || !product.availableToPurchase)
        {
            Debug.LogWarning("[IAP] El producto todavía no está disponible.");
            finished?.Invoke(false);
            return;
        }

        purchaseFinished = finished;
        controller.PurchaseProduct(product);
    }

    private void HandleStoreConnected()
    {
        controller.FetchProducts(new List<ProductDefinition>
        {
            new(MonetizationConfig.RemoveAdsProductId, ProductType.NonConsumable)
        });
    }

    private void HandleStoreDisconnected(StoreConnectionFailureDescription failure)
    {
        Debug.LogWarning($"[IAP] Tienda desconectada: {failure.message}");
    }

    private void HandleProductsFetched(List<Product> products)
    {
        product = products.FirstOrDefault(p => p.definition.id == MonetizationConfig.RemoveAdsProductId);

        changed?.Invoke();
        controller.FetchPurchases();
    }

    private void HandleProductsFetchFailed(ProductFetchFailed failure)
    {
        Debug.LogWarning($"[IAP] No se pudo obtener el producto: {failure.FailureReason}");
    }

    private void HandlePurchasesFetched(Orders orders)
    {
        if (orders.ConfirmedOrders.Any(Contains))
            granted?.Invoke();

        foreach (PendingOrder order in orders.PendingOrders.Where(Contains))
            controller.ConfirmPurchase(order);
    }

    private void HandlePurchasesFetchFailed(PurchasesFetchFailureDescription failure)
    {
        Debug.LogWarning($"[IAP] No se pudieron consultar las compras: {failure.Message}");
    }

    private void HandlePurchasePending(PendingOrder order)
    {
        if (Contains(order))
            controller.ConfirmPurchase(order);
    }

    // Recién con la compra confirmada en Google Play se quitan los anuncios
    private void HandlePurchaseConfirmed(Order order)
    {
        if (order is ConfirmedOrder && Contains(order))
        {
            granted?.Invoke();
            Finish(true);
            return;
        }

        if (order is FailedOrder failed)
            Debug.LogWarning($"[IAP] Falló la confirmación: {failed.Details}");

        Finish(false);
    }

    private void HandlePurchaseFailed(FailedOrder order)
    {
        Debug.LogWarning($"[IAP] Compra no completada: {order.FailureReason}");
        Finish(false);
    }

    // Pago pendiente (por ejemplo, en efectivo): al acreditarse llega como compra pendiente normal
    private void HandlePurchaseDeferred(DeferredOrder order)
    {
        Finish(false);
    }

    private void Finish(bool purchased)
    {
        Action<bool> finished = purchaseFinished;
        purchaseFinished = null;
        finished?.Invoke(purchased);
    }

    private static bool Contains(Order order) =>
        order.CartOrdered.Items().Any(item => item.Product.definition.id == MonetizationConfig.RemoveAdsProductId);
}
