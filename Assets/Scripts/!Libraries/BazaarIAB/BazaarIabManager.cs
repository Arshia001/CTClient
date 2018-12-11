using UnityEngine;
using NotSoSimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;

/* Apache License. Copyright (C) Bobardo Studio - All Rights Reserved.
 * Unauthorized publishing the plugin with different name is strictly prohibited.
 * This plugin is free and no one has right to sell it to others.
 * http://bobardo.com
 * http://opensource.org/licenses/Apache-2.0
 */

public enum ResultCode
{
    Success = 0,
    InvalidConsumption = 1,
    SubscriptionsNotAvailable = 2,
    UserCancelled = 3,
    ItemUnavailable = 4,
    ItemNotOwned = 5,
    ItemAlreadyOwned = 6,
    Error = 7,
    DeveloperError = 8,
    BillingUnavailable = 9,
    ExpectionInBillingSetup = 10
}

static class ErrorCodeExtensions
{
    public static bool IsSuccess(this ResultCode Code) => Code == ResultCode.Success;
}

public class Purchase
{
    public string OrderId { get; private set; }
    public string PurchaseToken { get; private set; }
    public string Payload { get; private set; }
    public string PackageName { get; private set; }
    public int PurchaseState { get; private set; }
    public string PurchaseTime { get; private set; }
    public string Sku { get; private set; }

    public Purchase(string Json) : this(JSON.Parse(Json)) { }

    public Purchase(JSONNode Node)
    {
        OrderId = Node["orderId"].AsString;
        PurchaseToken = Node["token"].AsString ?? Node["purchaseToken"].AsString;
        Payload = Node["developerPayload"].AsString;
        PackageName = Node["packageName"].AsString;
        PurchaseState = Node["purchaseState"].AsInt.Value;
        PurchaseTime = Node["purchaseTime"].AsString;
        Sku = Node["productId"].AsString;
    }
}

public class SkuDetails
{
    static int ConvertPriceString(string Price)
    {
        // There are two sets of "arabic" digits in unicode, one is Persian and the other is Arabic. I have no idea which one Bazaar returns, so we check for both.
        Price = string.Concat(Price.Where(ch => (0x0660 <= ch && ch <= 0x0669) || (0x06f0 <= ch && ch <= 0x06f9)));
        if (Price.Length == 0)
            return 0;
        Price = Price
            .Replace((char)0x0660, '0')
            .Replace((char)0x0661, '1')
            .Replace((char)0x0662, '2')
            .Replace((char)0x0663, '3')
            .Replace((char)0x0664, '4')
            .Replace((char)0x0665, '5')
            .Replace((char)0x0666, '6')
            .Replace((char)0x0667, '7')
            .Replace((char)0x0668, '8')
            .Replace((char)0x0669, '9')
            .Replace((char)0x06f0, '0')
            .Replace((char)0x06f1, '1')
            .Replace((char)0x06f2, '2')
            .Replace((char)0x06f3, '3')
            .Replace((char)0x06f4, '4')
            .Replace((char)0x06f5, '5')
            .Replace((char)0x06f6, '6')
            .Replace((char)0x06f7, '7')
            .Replace((char)0x06f8, '8')
            .Replace((char)0x06f9, '9');

        return int.Parse(Price);
    }

    public string Title { get; private set; }
    public string Description { get; private set; }
    public string Type { get; private set; }
    public int Price { get; private set; }
    public string Sku { get; private set; }

    public SkuDetails(string Json) : this(JSON.Parse(Json)) { }

    public SkuDetails(JSONNode Node)
    {
        Title = Node["title"].AsString;
        Description = Node["description"].AsString;
        Type = Node["type"].AsString;
        Price = ConvertPriceString(Node["price"].AsString);
        Sku = Node["productId"].AsString;
    }
}

public class BazaarIabManager : MonoBehaviour
{
    public delegate void OnPurchaseResultDelegate(ResultCode Code, Purchase Purchase);
    public delegate void OnHasPurchaseResultDelegate(ResultCode Code, string SKU, bool UserHasItem, Purchase Purchase);
    public delegate void OnPurchaseAndConsumeResultDelegate(ResultCode Code, Purchase Purchase);
    public delegate void OnSkuDetailsResultDelegate(ResultCode Code, Dictionary<string, SkuDetails> Details);
    public delegate void OnConsumeResultDelegate(ResultCode Code, Purchase Purchase);
    public delegate void OnGetInventoryResultDelegate(ResultCode Code, Dictionary<string, Purchase> OwnedItems);


    public static BazaarIabManager Instance { get; private set; }


    public event OnPurchaseResultDelegate OnPurchaseResultEvent;
    public event OnHasPurchaseResultDelegate OnHasPurchaseResultEvent;
    public event OnPurchaseAndConsumeResultDelegate OnPurchaseAndConsumeResultEvent;
    public event OnSkuDetailsResultDelegate OnSkuDetailsResultEvent;
    public event OnConsumeResultDelegate OnConsumeResultEvent;
    public event OnGetInventoryResultDelegate OnGetInventoryResultEvent;


    public string PublicKey;

    private AndroidJavaObject PluginObject = null;


    void Start()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            PluginObject = new AndroidJavaObject("com.bobardo.bazaar.iab.ServiceBillingBazaar", PublicKey, gameObject.name);
            Debug.Log("Finished initializing Bazaar IAB");
        }
        catch (Exception Ex)
        {
            Debug.LogException(Ex);
        }
#endif
    }

    public void OnInitError(string msg)
    {
        var Parsed = ParsePluginResults(msg);
        if (Parsed.Item1 == ResultCode.ExpectionInBillingSetup)
        {
            Debug.LogError("Failed to initialize IAB: " + Parsed.Item2);
            StopIABService();
        }
    }

    void StopIABService()
    {
        try
        {
            if (PluginObject != null)
                PluginObject.Call("stopIabHelper");
            PluginObject = null;
        }
        catch { }
    }

    public void DebugLog(string msg)
    {
        Debug.Log(msg);
    }

    void OnApplicationQuit()
    {
        StopIABService();
    }

    public bool Purchase(string ProductSku, string Payload = "") => InvokePluginFunction(o => o.Call<string>("launchPurchaseFlow", ProductSku, Payload));

    public bool PurchaseAndConsume(string ProductSku, string Payload = "") => InvokePluginFunction(o => o.Call<string>("purchaseAndConsume", ProductSku, Payload));

    public bool Consume(string ProductSku) => InvokePluginFunction(o => o.Call<string>("consume", ProductSku));

    public bool CheckHasItem(string ProductSku) => InvokePluginFunction(o => o.Call<string>("checkHasItem", ProductSku));

    public bool GetInventory() => InvokePluginFunction(o => o.Call<string>("getInventory"));

    public bool GetSkuDetails(IEnumerable<string> Skus) => InvokePluginFunction(o => o.Call<string>("getSkuDetails", string.Join(";", Skus)));

    bool InvokePluginFunction(Func<AndroidJavaObject, string> Func)
    {
        if (PluginObject == null)
        {
            Debug.LogError("IAB plugin is not initialized");
            return false;
        }

        var Json = JSON.Parse(Func(PluginObject));
        if (Json["result"].AsBool ?? false)
            return true;

        Debug.LogError("IAB plugin method failed with error: " + Json["data"].AsString);
        return false;
    }

    Tuple<ResultCode, string> ParsePluginResults(string Results)
    {
        var Json = JSON.Parse(Results);
        return new Tuple<ResultCode, string>((ResultCode)Json["errorCode"].AsInt, Json["data"].AsString);
    }

    public void OnPurchaseResult(string Results)
    {
        Debug.Log("OnPurchaseResult: " + Results);

        var Parsed = ParsePluginResults(Results);
        Purchase P = null;
        if (Parsed.Item1.IsSuccess())
            P = new Purchase(Parsed.Item2);
        else
            Debug.LogError(Parsed.Item2);
        OnPurchaseResultEvent?.Invoke(Parsed.Item1, P);
    }

    public void OnHasPurchaseResult(string Results)
    {
        Debug.Log("OnHasPurchaseResult: " + Results);

        var Parsed = ParsePluginResults(Results);
        if (Parsed.Item1 == ResultCode.Success)
        {
            var P = new Purchase(Parsed.Item2);
            OnHasPurchaseResultEvent?.Invoke(ResultCode.Success, P.Sku, true, P);
        }
        else if (Parsed.Item1 == ResultCode.ItemNotOwned)
        {
            OnHasPurchaseResultEvent?.Invoke(ResultCode.Success, Parsed.Item2, false, null);
        }
        else
        {
            Debug.LogError(Parsed.Item2);
            OnHasPurchaseResultEvent?.Invoke(Parsed.Item1, null, false, null);
        }
    }

    public void OnPurchaseAndConsumeResult(string Results)
    {
        Debug.Log("OnPurchaseAndConsumeResult: " + Results);

        var Parsed = ParsePluginResults(Results);
        Purchase P = null;
        if (Parsed.Item1.IsSuccess())
            P = new Purchase(Parsed.Item2);
        else
            Debug.LogError(Parsed.Item2);
        OnPurchaseAndConsumeResultEvent?.Invoke(Parsed.Item1, P);
    }

    public void OnSkuDetailsResult(string Results)
    {
        Debug.Log("OnSkuDetailsResult: " + Results);

        var Parsed = ParsePluginResults(Results);
        Dictionary<string, SkuDetails> Items = null;
        if (Parsed.Item1.IsSuccess())
        {
            Items = new Dictionary<string, SkuDetails>();
            var Json = JSON.Parse(Parsed.Item2);
            foreach (var KV in Json.AsDictionary)
                Items.Add(KV.Key, new SkuDetails(KV.Value.AsString));
        }
        else
            Debug.LogError(Parsed.Item2);
        OnSkuDetailsResultEvent?.Invoke(Parsed.Item1, Items);
    }

    public void OnConsumeResult(string Results)
    {
        Debug.Log("OnConsumeResult: " + Results);

        var Parsed = ParsePluginResults(Results);
        Purchase P = null;
        if (Parsed.Item1.IsSuccess())
            P = new Purchase(Parsed.Item2);
        else
            Debug.LogError(Parsed.Item2);
        OnConsumeResultEvent?.Invoke(Parsed.Item1, P);
    }

    public void OnGetInventoryResult(string Results)
    {
        Debug.Log("OnGetInventoryResult: " + Results);

        var Parsed = ParsePluginResults(Results);
        Dictionary<string, Purchase> Items = null;
        if (Parsed.Item1.IsSuccess())
        {
            Items = new Dictionary<string, Purchase>();
            var Json = JSON.Parse(Parsed.Item2);
            foreach (var KV in Json.AsDictionary)
                Items.Add(KV.Key, new Purchase(KV.Value.AsObject));
        }
        else
            Debug.LogError(Parsed.Item2);
        OnGetInventoryResultEvent?.Invoke(Parsed.Item1, Items);
    }
}
