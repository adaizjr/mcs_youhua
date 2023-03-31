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
                player.emailDateMag.cyNpcList.Add(mailPatch.mailid);
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
                int[] arr_tmp = new int[10];
                int tmp_zong = 0;
                foreach (var tmp in jsonData.instance.AvatarJsonData.list)
                {
                    int tmp_level = tmp["Level"].I;
                    int tmp_big = (tmp_level - 1) / 3;
                    arr_tmp[tmp_big]++;
                    tmp_zong++;
                }
                __result = "总计" + tmp_zong.ToString() + "，练气" + arr_tmp[0].ToString() + "，筑基" + arr_tmp[1].ToString() + "，金丹" + arr_tmp[2].ToString() + "，元婴" + arr_tmp[3].ToString() + "，化神" + arr_tmp[4].ToString();
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
}
