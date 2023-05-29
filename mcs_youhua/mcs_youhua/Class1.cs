using Bag;
using BepInEx;
using HarmonyLib;
using JSONClass;
using KBEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace zjr_mcs
{
    [BepInPlugin("plugins.zjr.mcs_youhua", "zjr优化插件", "1.0.0.0")]
    public class youhuaBepInExMod : BaseUnityPlugin
    {// 在插件启动时会直接调用Awake()方法

        void Awake()
        {
            // 使用Debug.Log()方法来将文本输出到控制台
            Debug.Log("Hello,mcs_youhua!");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            Harmony.CreateAndPatchAll(typeof(youhuaBepInExMod));
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UINPCSVItem), "RefreshUI")]
        public static void UINPCSVItem_RefreshUI_Postfix(UINPCSVItem __instance)
        {
            __instance.NPCTitle.text = __instance.NPCData.Title + "" + __instance.NPCData.BigLevel.ToString();
        }

        private void Update()
        {
            bool flag = Tools.instance == null;
            if (!flag)
            {
                Avatar player = Tools.instance.getPlayer();
                bool flag2 = player == null;
                if (!flag2)
                {
                    bool keyUp = Input.GetKeyUp(KeyCode.Delete);
                    if (keyUp && !USelectNum.IsShow)
                    {
                        USelectNum.Show("清空邮件，1垃圾，2所有", 1, 2, delegate (int selectNum)
                        {
                            ClearChuanYin(selectNum == 2);
                        }, null);
                    }
                }
            }
        }
        public static void ClearChuanYin(bool i_b_all)
        {
            if (i_b_all)
            {
                //Tools.instance.getPlayer().NewChuanYingList.Clear();
                Tools.instance.getPlayer().emailDateMag.hasReadEmailDictionary.Clear();
                UIPopTip.Inst.Pop("清空所有邮件！", 0);
            }
            else
            {
                foreach (var tmp_kvp in Tools.instance.getPlayer().emailDateMag.hasReadEmailDictionary)
                {
                    List<EmailData> tmp_ed_new = new List<EmailData>();
                    List<EmailData> tmp_ed = tmp_kvp.Value;
                    foreach (var tmp in tmp_ed)
                    {
                        if (tmp.actionId == 1)
                        {
                            if (tmp.item[1] > 0)
                                tmp_ed_new.Add(tmp);
                        }
                        else if (tmp.actionId == 2)
                        {
                            if (!tmp.CheckIsOut() && !tmp.isComplete)
                                tmp_ed_new.Add(tmp);
                        }
                        else if (tmp.isAnswer)
                        {
                        }
                        else if (tmp.isPlayer)
                        {
                        }
                        else
                        {
                            tmp_ed_new.Add(tmp);
                        }
                    }
                    tmp_ed.Clear();
                    foreach (var tmp in tmp_ed_new)
                        tmp_ed.Add(tmp);
                }
                UIPopTip.Inst.Pop("清空垃圾邮件！", 0);
                Tools.instance.getPlayer().emailDateMag.AddNewEmail(mailPatch.mailid.ToString(), new EmailData(mailPatch.mailid, 1, true, true, Tools.instance.getPlayer().worldTimeMag.nowTime));
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(script.ExchangeMeeting.UI.Ctr.PublishCtr), "Publish")]
        public static bool PublishCtr_Publish_Prefix(script.ExchangeMeeting.UI.Ctr.PublishCtr __instance)
        {
            bool value = Traverse.Create(__instance).Method("CheckCanPublish", Array.Empty<object>()).GetValue<bool>();
            if (value)
            {
                int num = 9999;
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
                                script.ExchangeMeeting.UI.Interface.IExchangeUIMag.Inst.PlayerBag.RemoveTempItem(baseSlot2.Item.Uid, baseSlot2.Item.Count * (selectNum - 1));
                            }
                        }
                        List<BaseItem> list2 = new List<BaseItem>();
                        list2.Add(__instance.UI.PublishDataUI.NeedItem.Item);
                        for (int i = 0; i < selectNum; i++)
                        {
                            script.ExchangeMeeting.Logic.Interface.IExchangeMag.Inst.ExchangeIO.CreatePlayerExchange(list2, list);
                        }
                        __instance.UpdatePlayerList();
                        __instance.UI.PublishDataUI.Clear();
                        script.ExchangeMeeting.UI.Interface.IExchangeUIMag.Inst.SubmitBag.CreateTempList();
                    }
                }, null);
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(script.MenPaiTask.ZhangLao.UI.Ctr.CreateElderTaskCtr), "PublishTask")]
        public static bool CreateElderTaskCtr_PublishTask_Prefix(ref script.MenPaiTask.ZhangLao.UI.Ctr.CreateElderTaskCtr __instance)
        {
            bool flag = Tools.instance.getPlayer().ElderTaskMag.PlayerAllotTask(__instance.SlotList);
            if (flag)
            {
                __instance.ClearItemList();
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(script.MenPaiTask.ElderTaskMag), "PlayerAllotTask")]
        public static bool ElderTaskMag_PlayerAllotTask_Prefix(List<script.MenPaiTask.ZhangLao.UI.Base.ElderTaskSlot> slotList, ref bool __result, script.MenPaiTask.ElderTaskMag __instance)
        {
            Avatar player = Tools.instance.getPlayer();
            List<BaseItem> list = new List<BaseItem>();
            foreach (script.MenPaiTask.ZhangLao.UI.Base.ElderTaskSlot elderTaskSlot in slotList)
            {
                bool flag = !elderTaskSlot.IsNull();
                if (flag)
                {
                    list.Add(elderTaskSlot.Item.Clone());
                }
            }
            bool flag2 = list.Count == 0;
            bool result;
            if (flag2)
            {
                UIPopTip.Inst.Pop("至少需要一个物品", 0);
                __result = false;
                result = false;
            }
            else
            {
                bool flag3 = player.menPai <= 0;
                if (flag3)
                {
                    UIPopTip.Inst.Pop("无权发布任务", 0);
                    __result = false;
                    result = false;
                }
                else
                {
                    script.MenPaiTask.ElderTask elderTask = new script.MenPaiTask.ElderTask();
                    int num = 0;
                    int num2 = 0;
                    foreach (BaseItem baseItem in list)
                    {
                        elderTask.AddNeedItem(baseItem);
                        num += __instance.GetNeedMoney(baseItem);
                        int num3 = num2;
                        num2 = num3 + 1;
                    }
                    elderTask.Money = num;
                    bool flag4 = __instance.CheckCanAllotTask(num, num2);
                    if (flag4)
                    {
                        int num4 = 100;
                        bool flag5 = num <= 0;
                        if (flag5)
                        {
                            num = 1;
                        }
                        bool flag6 = num2 <= 0;
                        if (flag6)
                        {
                            num2 = 1;
                        }
                        num4 = Math.Min((int)player.money / num, num4);
                        num4 = Math.Min(PlayerEx.GetShengWang((int)player.menPai) / num2, num4);
                        adf(elderTask, num, num2, num4, __instance);

                        __result = true;
                        result = false;
                    }
                    else
                    {
                        UIPopTip.Inst.Pop("灵石或声望不足", 0);
                        __result = false;
                        result = false;
                    }
                }
            }
            return result;
        }
        static void adf(script.MenPaiTask.ElderTask i_et, int num, int num2, int num4, script.MenPaiTask.ElderTaskMag __instance)
        {
            Avatar player = Tools.instance.getPlayer();

            USelectNum.Show("发布<color=white>{num}</color>条相同的任务", 1, num4, delegate (int selectNum)
            {
                for (int i = 0; i < selectNum; i++)
                {
                    script.MenPaiTask.ElderTask elderTask = new script.MenPaiTask.ElderTask();
                    foreach (BaseItem baseItem2 in i_et.needItemList)
                    {
                        elderTask.AddNeedItem(baseItem2);
                    }
                    elderTask.Money = i_et.Money;
                    __instance.AddWaitAcceptTask(elderTask);
                }
                player.AddMoney(-num * selectNum);
                PlayerEx.AddShengWang((int)player.menPai, -num2 * selectNum, false);
                script.MenPaiTask.ZhangLao.UI.ElderTaskUIMag.Inst.ElderTaskUI.Ctr.CreateTaskList();
                script.MenPaiTask.ZhangLao.UI.ElderTaskUIMag.Inst.OpenElderTaskUI();
                UIPopTip.Inst.Pop("发布任务成功", 0);
            }, null);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(script.ExchangeMeeting.Logic.UpdateExchange), "SuccessExchange", new Type[] { typeof(script.ExchangeMeeting.Logic.Interface.IExchangeData) })]
        public static bool UpdateExchange_SuccessExchange_Prefix(script.ExchangeMeeting.Logic.UpdateExchange __instance, ref script.ExchangeMeeting.Logic.Interface.IExchangeData data)
        {
            Tools.instance.getPlayer().addItem(data.NeedItems[0].Id, 1, Tools.CreateItemSeid(data.NeedItems[0].Id), true);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(script.MenPaiTask.ZhangLao.UI.Ctr.ElderTaskCtr), "CreateTaskList")]
        public static bool ElderTaskCtr_CreateTaskList_Prefix(script.MenPaiTask.ZhangLao.UI.Ctr.ElderTaskCtr __instance)
        {
            script.MenPaiTask.ElderTaskMag elderTaskMag = Tools.instance.getPlayer().ElderTaskMag;
            List<script.MenPaiTask.ElderTask> tmp_list = new List<script.MenPaiTask.ElderTask>();
            foreach (script.MenPaiTask.ElderTask data in elderTaskMag.GetCompleteTaskList())
            {
                foreach (BaseItem needItem in data.needItemList)
                {
                    Tools.instance.getPlayer().addItem(needItem.Id, needItem.Count, needItem.Seid, ShowText: true);
                }
                tmp_list.Add(data);
            }
            foreach (var tmp in tmp_list)
            {
                elderTaskMag.RemoveCompleteTask(tmp);
            }
            return true;
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EmailDataMag), "AddNewEmail", new Type[] { typeof(string), typeof(EmailData) })]
        public static void EmailDataMag_AddNewEmail_Postfix(EmailDataMag __instance, ref string npcId, ref EmailData data)
        {
            if (data.actionId == 2)
            {
                int itemId = data.item[0];
                if (itemId == 5119 || itemId == 5211 || itemId == 5308 || itemId == 5404 || itemId == 5517)
                {
                    data.isComplete = true;
                    NPCEx.AddFavor(data.npcId, data.addHaoGanDu, false, true);
                    //int addCount = jsonData.instance.ItemJsonData[emailData.item[0].ToString()]["price"].I * emailData.item[1];
                    //NPCEx.AddQingFen(emailData.npcId, addCount, false);
                    NpcJieSuanManager.inst.AddItemToNpcBackpack(data.npcId, data.item[0], data.item[1], null, false);
                    __instance.AuToSendToPlayer(data.npcId, 997, 997, data.sendTime, null);
                }
            }
        }
    }

    [HarmonyPatch(typeof(jsonData), "Preload")]
    class RandomPatch
    {
        public static void Postfix(jsonData __instance)
        {
            if (__instance.RandomList.Count < 9500)
                for (int i = 0; i < 9500; i++)
                {
                    __instance.RandomList.Add(jsonData.GetRandom());
                }
        }
    }

    [HarmonyPatch(typeof(NPCFactory), "AuToCreateNpcs")]
    class gengduonpcPatch
    {
        static Dictionary<int, List<string>> NpcAuToCreateDictionary = new Dictionary<int, List<string>>();
        public static void Postfix(NPCFactory __instance)
        {
            JSONObject npcCreateData = jsonData.instance.NpcCreateData;
            if (NpcAuToCreateDictionary.Count < 1)
            {
                JSONObject npcleiXingDate = jsonData.instance.NPCLeiXingDate;
                foreach (string text in npcleiXingDate.keys)
                {
                    if (npcleiXingDate[text]["Level"].I == 1 && npcleiXingDate[text]["LiuPai"].I != 34)
                    {
                        int i = npcleiXingDate[text]["Type"].I;
                        if (NpcAuToCreateDictionary.ContainsKey(i))
                        {
                            NpcAuToCreateDictionary[i].Add(text);
                        }
                        else
                        {
                            NpcAuToCreateDictionary.Add(i, new List<string>
                        {
                            text
                        });
                        }
                    }
                }
            }
            foreach (JSONObject jsonobject in npcCreateData.list)
            {
                int j = jsonobject["NumA"].I;
                if (jsonobject["EventValue"].Count > 0 && GlobalValue.Get(jsonobject["EventValue"][0].I, "NPCFactory.AuToCreateNpcs 每10年自动生成NPC") == jsonobject["EventValue"][1].I)
                {
                    j = jsonobject["NumB"].I;
                }
                int i2 = jsonobject["id"].I;
                while (j > 0)
                {
                    string index = NpcAuToCreateDictionary[i2][__instance.getRandom(0, NpcAuToCreateDictionary[i2].Count - 1)];
                    JSONObject npcDate = new JSONObject(jsonData.instance.NPCLeiXingDate[index].ToString(), -2, false, false);
                    __instance.AfterCreateNpc(npcDate, false, 0, false, null, 0);
                    j--;
                }
            }
            Avatar player = Tools.instance.getPlayer();

            if (!player.emailDateMag.cyNpcList.Contains(mailPatch.mailid))
            {
                player.emailDateMag.cyNpcList.Add(mailPatch.mailid);
            }
            if (player.emailDateMag.hasReadEmailDictionary.ContainsKey(mailPatch.mailid.ToString()))
            {
                player.emailDateMag.hasReadEmailDictionary[mailPatch.mailid.ToString()].Clear();
            }
            player.emailDateMag.AddNewEmail(mailPatch.mailid.ToString(), new EmailData(mailPatch.mailid, 1, true, true, player.worldTimeMag.nowTime));
        }
    }

    [HarmonyPatch(typeof(CyEmail), "GetContent", new Type[] { typeof(string), typeof(EmailData) })]
    class mailPatch
    {
        public static int mailid = 2;
        public static bool Prefix(CyEmail __instance, ref string __result, ref string msg, ref EmailData emailData)
        {
            if (emailData.npcId == mailid)
            {
                __instance.cySendBtn.gameObject.SetActive(false);
                int[] arr_dengji = new int[10];
                int tmp_zong = 0;
                foreach (var tmp in jsonData.instance.AvatarJsonData.list)
                {
                    int tmp_id = tmp["id"].I;
                    int tmp_level = tmp["Level"].I;
                    int tmp_big = (tmp_level - 1) / 3;
                    if (tmp_id >= 20000)
                    {
                        arr_dengji[tmp_big]++;
                        tmp_zong++;
                    }
                }
                __result = "总计" + tmp_zong.ToString() + "，练气" + arr_dengji[0].ToString() + "，筑基" + arr_dengji[1].ToString() + "，金丹" + arr_dengji[2].ToString() + "，元婴" + arr_dengji[3].ToString() + "，化神" + arr_dengji[4].ToString();
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(UILingTianPanel), "OnShouGeBtnClick")]
    class dongfushouhuoPatch
    {
        public static bool Prefix(UILingTianPanel __instance)
        {
            __instance.IsShouGe = !__instance.IsShouGe;
            if (__instance.IsShouGe)
            {
                __instance.ShouGeBtn1.SetActive(false);
                __instance.ShouGeBtn2.SetActive(true);

                int dongFuID = DongFuManager.NowDongFuID;
                DongFuData dongFuData = new DongFuData(dongFuID);
                dongFuData.Load();
                KBEngine.Avatar player = PlayerEx.Player;
                for (int slot = 0; slot < DongFuManager.LingTianCount; slot++)
                {
                    int id = dongFuData.LingTian[slot].ID;
                    if (id == 0)
                    {
                        //Debug.LogError("灵田收获异常，不能收获id为0的草药");
                    }
                    else
                    {
                        _ItemJsonData itemJsonData = _ItemJsonData.DataDict[id];
                        int num = dongFuData.LingTian[slot].LingLi / itemJsonData.price;
                        if (num > 0)
                        {
                            player.addItem(id, num, Tools.CreateItemSeid(id), false);
                            dongFuData.LingTian[slot].LingLi -= itemJsonData.price * num;
                        }
                    }
                }
                dongFuData.Save();
                UIDongFu.Inst.InitData();
                foreach (UILingTianCell uilingTianCell in __instance.LingTianList)
                {
                    uilingTianCell.RefreshUI();
                }
                __instance.PlayerInventory.RefreshUI();
                //DongFuManager.RefreshDongFuShow();
                return false;
            }
            __instance.ShouGeBtn1.SetActive(true);
            __instance.ShouGeBtn2.SetActive(false);

            return false;
        }
    }

    [HarmonyPatch(typeof(CyEmailCell), "Init", new Type[] { typeof(EmailData), typeof(bool) })]
    class shouemailPatch
    {
        public static void Postfix(CyEmailCell __instance, ref EmailData emailData, ref bool isDeath)
        {
            if (emailData.isOld)
            {
            }
            else
            {
                try
                {
                    if (emailData.actionId == 1)
                    {
                        if (emailData.item[1] > 0)
                        {
                            __instance.submitBtn.gameObject.SetActive(false);
                            Tools.instance.getPlayer().addItem(emailData.item[0], emailData.item[1], Tools.CreateItemSeid(emailData.item[0]), false);
                            __instance.item.ShowHasGet();
                            __instance.UpdateSize();
                            emailData.item[1] = -1;
                        }
                    }
                }
                catch (Exception message)
                {
                    Debug.LogError(message);
                    Debug.LogError(string.Format("物品ID:{0}不存在", emailData.item[0]));
                }
            }
        }
    }

    [HarmonyPatch(typeof(Tab.WuDaoSlot), "Study")]
    class WuDaoSlotPatch
    {
        public static bool Prefix(Tab.WuDaoSlot __instance)
        {
            if (__instance.State == 1 && !PlayerEx.Player.SelectTianFuID.HasItem(316))
            {
                myclickyiwang(__instance);
                return false;
            }
            return true;
        }
        static void myclickyiwang(Tab.WuDaoSlot __instance)
        {
            MethodInfo tmp_method_ClickYiWang = typeof(Tab.WuDaoSlot).GetMethod("ClickYiWang", BindingFlags.Instance | BindingFlags.NonPublic);
            tmp_method_ClickYiWang.Invoke(__instance, null);
        }
    }
}
