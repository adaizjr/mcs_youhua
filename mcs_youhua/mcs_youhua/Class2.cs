using Bag;
using HarmonyLib;
using script.ExchangeMeeting.Logic;
using script.ExchangeMeeting.Logic.Interface;
using script.ExchangeMeeting.UI.Ctr;
using script.ExchangeMeeting.UI.Interface;
using script.ItemSource;
using script.ItemSource.Interface;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace zjr_mcs
{
    internal class yuanying_jiaoyihui
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ItemSourceUpdate), "Update", new Type[] { typeof(int) })]
        public static bool ItemSourceUpdate_Update_Prefix(ItemSourceUpdate __instance, ref int times)
        {
            Dictionary<int, ABItemSourceData> itemSourceDataDic = ABItemSource.Get().ItemSourceDataDic;
            foreach (int key in itemSourceDataDic.Keys)
            {
                if (itemSourceDataDic[key].Count > 0)
                {
                    itemSourceDataDic[key].HasCostTime = 0;
                    itemSourceDataDic[key].Count = 1;
                }
                itemSourceDataDic[key].HasCostTime += times;
                int tmp_uptime = Math.Max(1, itemSourceDataDic[key].UpdateTime);
                if (itemSourceDataDic[key].HasCostTime >= tmp_uptime)
                {
                    itemSourceDataDic[key].Count += itemSourceDataDic[key].HasCostTime / tmp_uptime;
                    itemSourceDataDic[key].HasCostTime %= tmp_uptime;
                }
            }
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ABItemSourceIO), "Get", new Type[] { typeof(int) })]
        public static bool ABItemSourceIO_Get_Prefix(ABItemSourceIO __instance, ref bool __result, ref int id)
        {
            __result = my_get(id);
            return false;
        }
        static bool my_get(int id)
        {
            if (!ABItemSource.Get().ItemSourceDataDic.ContainsKey(id))
            {
                Debug.LogError("物品Id：" + id + "不在自动生成表中");
                return false;
            }
            if (ABItemSource.Get().ItemSourceDataDic[id].Count > 0)
            {
                ABItemSource.Get().ItemSourceDataDic[id].Count--;
                return true;
            }
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PublishCtr), "Publish")]
        public static bool PublishCtr_Publish_Prefix(PublishCtr __instance)
        {
            bool value = Traverse.Create(__instance).Method("CheckCanPublish", Array.Empty<object>()).GetValue<bool>();
            if (value)
            {
                int num = 100;
                Dictionary<int, int> dictionary = new Dictionary<int, int>();
                foreach (BaseSlot baseSlot in __instance.UI.PublishDataUI.GiveItems)
                {
                    bool flag = !baseSlot.IsNull();
                    if (flag)
                    {
                        int id = baseSlot.Item.Id;
                        int count = baseSlot.Item.Count;
                        bool flag2 = dictionary.ContainsKey(id);
                        if (flag2)
                        {
                            Dictionary<int, int> dictionary2 = dictionary;
                            int key = id;
                            dictionary2[key] += count;
                        }
                        else
                        {
                            dictionary.Add(id, count);
                        }
                    }
                }
                foreach (KeyValuePair<int, int> keyValuePair in dictionary)
                {
                    num = Math.Min(Tools.instance.getPlayer().getItemNum(keyValuePair.Key) / keyValuePair.Value, num);
                    bool flag3 = keyValuePair.Key > 18000 && keyValuePair.Key < 19000;
                    if (flag3)
                    {
                        num = 1;
                    }
                }
                USelectNum.Show("发布<color=white>{num}</color>条相同的寄换请求", 1, num, delegate (int selectNum)
                {
                    bool flag4 = (int)Tools.instance.getPlayer().money < __instance.UI.PublishDataUI.DrawMoney * selectNum;
                    if (flag4)
                    {
                        UIPopTip.Inst.Pop("灵石不足！", 0);
                    }
                    else
                    {
                        List<BaseItem> list = new List<BaseItem>();
                        PlayerEx.Player.AddMoney(-__instance.UI.PublishDataUI.DrawMoney * selectNum);
                        foreach (BaseSlot baseSlot2 in __instance.UI.PublishDataUI.GiveItems)
                        {
                            bool flag5 = !baseSlot2.IsNull();
                            if (flag5)
                            {
                                list.Add(baseSlot2.Item.Clone());
                                PlayerEx.Player.removeItem(baseSlot2.Item.Uid, baseSlot2.Item.Count * selectNum);
                                IExchangeUIMag.Inst.PlayerBag.RemoveTempItem(baseSlot2.Item.Uid, baseSlot2.Item.Count * (selectNum - 1));
                            }
                        }
                        List<BaseItem> list2 = new List<BaseItem>();
                        list2.Add(__instance.UI.PublishDataUI.NeedItem.Item);
                        for (int i = 0; i < selectNum; i++)
                        {
                            IExchangeMag.Inst.ExchangeIO.CreatePlayerExchange(list2, list);
                        }
                        __instance.UpdatePlayerList();
                        __instance.UI.PublishDataUI.Clear();
                        IExchangeUIMag.Inst.SubmitBag.CreateTempList();
                    }
                }, null);
            }
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UpdateExchange), "SuccessExchange", new Type[] { typeof(IExchangeData) })]
        public static bool UpdateExchange_SuccessExchange_Prefix(UpdateExchange __instance, ref IExchangeData data)
        {
            Tools.instance.getPlayer().addItem(data.NeedItems[0].Id, 1, Tools.CreateItemSeid(data.NeedItems[0].Id), true);
            return false;
        }
    }
}
