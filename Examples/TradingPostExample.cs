private static List<SO_TradingPost_Buyable> addedItems = new List<SO_TradingPost_Buyable>();

/*
 * Adds the specified item to the trading post. Allows you to specify how much
 * of an item is given per purchase, how many times it can be purchased, the
 * tier, the cost, and where it should be placed in the sort order.
 */
public static void AddItemToTradingPost(Item_Base item, int buyAmount, int startStock, int trashCubes, int tradeCoins, TradingPost.Tier tier, int sortingOrder)
{
    // Get the list and make sure it is properly loaded.
    var startingStockField = Traverse.Create<TradingPost>().Field("startingStock");
    List<SO_TradingPost_Buyable> startingStock = startingStockField.GetValue() as List<SO_TradingPost_Buyable>;
    if (startingStock.Count == 0)
    {
        startingStock = Resources.LoadAll<SO_TradingPost_Buyable>("SO_TradingPost").ToList<SO_TradingPost_Buyable>();
        startingStock = (from b in startingStock orderby b.sortingOrder select b).ToList<SO_TradingPost_Buyable>();
        startingStockField.SetValue(startingStock);
    }

    SO_TradingPost_Buyable newItem = ScriptableObject.CreateInstance<SO_TradingPost_Buyable>();
    newItem.tier = tier;
    newItem.reward = new Cost(item, buyAmount);
    newItem.sortingOrder = sortingOrder;
    newItem.startStock = startStock;
    List<Cost> cost = new List<Cost>();
    // Trash cube cost.
    if (trashCubes > 0 || (tradeCoins == 0))
    {
        cost.Add(new Cost(ItemManager.GetItemByIndex(364), trashCubes));
    }
    // Trade coin cost.
    if (tradeCoins > 0)
    {
        cost.Add(new Cost(ItemManager.GetItemByIndex(564), tradeCoins));
    }
    newItem.cost = cost.ToArray();

    bool inserted = false;
    // Insert the item into the list in the correct place.
    for (int i = 0; i < startingStock.Count; ++i)
    {
        if (newItem.sortingOrder < startingStock[i].sortingOrder)
        {
            startingStock.Insert(i, newItem);
            inserted = true;
            break;
        }
    }

    // Handle it being greater than all items.
    if (!inserted)
    {
        startingStock.Add(newItem);
    }

    // We are tracking our added items so at a later point they can be
    // purged, for example when we unload the mod.
    addedItems.Add(newItem);

    // Now, find any existing trading posts and add the item to it.
    foreach (TradingPost post in FindObjectsOfType<TradingPost>())
    {
        post.buyableItems.Add(newItem.CreateInstance());
    }
}
