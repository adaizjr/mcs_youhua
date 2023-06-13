using BepInEx;
using HarmonyLib;
using JSONClass;
using KBEngine;
using mcs_youhua;
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
            Debug.Log("mcs_youhua,yuanying_jiaoyihui!");
            Harmony.CreateAndPatchAll(typeof(yuanying_jiaoyihui));
            Debug.Log("mcs_youhua,zhongmen_paifa!");
            Harmony.CreateAndPatchAll(typeof(zhongmen_paifa));
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
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Avatar), "setMonstarDeath")]
        public static void Avatar_setMonstarDeath_Postfix(Avatar __instance)
        {
            List<int> tmp_list = new List<int>() { 4101, 4102, 4103, 4104, 4105 };
            List<int> list = new List<int>();
            for (int i = 0; i < jsonData.instance.AvatarRandomJsonData.Count; i++)
            {
                int tmp_ke = int.Parse(jsonData.instance.AvatarRandomJsonData.keys[i]);
                if (tmp_ke < 20000 && tmp_list.Contains(tmp_ke))
                {
                    if (DateTime.Parse(jsonData.instance.AvatarRandomJsonData[i]["BirthdayTime"].str).Year < __instance.worldTimeMag.getNowTime().Year)
                        list.Add(tmp_ke);
                }
            }
            for (int j = 0; j < list.Count; j++)
            {
                jsonData.instance.setMonstarDeath(list[j], false);
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
