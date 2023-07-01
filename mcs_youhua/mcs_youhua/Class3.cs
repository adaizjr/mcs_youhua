using Bag;
using BepInEx;
using HarmonyLib;
using KBEngine;
using script.MenPaiTask;
using script.MenPaiTask.ZhangLao.UI;
using script.MenPaiTask.ZhangLao.UI.Base;
using script.MenPaiTask.ZhangLao.UI.Ctr;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace zjr_mcs
{
    internal class zhongmen_paifa
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CreateElderTaskCtr), "PublishTask")]
        public static bool CreateElderTaskCtr_PublishTask_Prefix(ref CreateElderTaskCtr __instance)
        {
            bool flag = Tools.instance.getPlayer().ElderTaskMag.PlayerAllotTask(__instance.SlotList);
            if (flag)
            {
                __instance.ClearItemList();
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ElderTaskMag), "PlayerAllotTask")]
        public static bool ElderTaskMag_PlayerAllotTask_Prefix(List<ElderTaskSlot> slotList, ref bool __result, ElderTaskMag __instance)
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
        static void adf(ElderTask i_et, int num, int num2, int num4, ElderTaskMag __instance)
        {
            Avatar player = Tools.instance.getPlayer();

            USelectNum.Show("发布<color=white>{num}</color>条相同的任务", 1, num4, delegate (int selectNum)
            {
                for (int i = 0; i < selectNum; i++)
                {
                    ElderTask elderTask = new ElderTask();
                    foreach (BaseItem baseItem2 in i_et.needItemList)
                    {
                        elderTask.AddNeedItem(baseItem2);
                    }
                    elderTask.Money = i_et.Money;
                    __instance.AddWaitAcceptTask(elderTask);
                }
                player.AddMoney(-num * selectNum);
                PlayerEx.AddShengWang((int)player.menPai, -num2 * selectNum, false);
                ElderTaskUIMag.Inst.ElderTaskUI.Ctr.CreateTaskList();
                ElderTaskUIMag.Inst.OpenElderTaskUI();
                UIPopTip.Inst.Pop("发布任务成功", 0);
            }, null);
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(ElderTaskCtr), "CreateTaskList")]
        public static bool ElderTaskCtr_CreateTaskList_Prefix(ElderTaskCtr __instance)
        {
            ElderTaskMag elderTaskMag = Tools.instance.getPlayer().ElderTaskMag;
            List<ElderTask> tmp_list = new List<ElderTask>();
            foreach (ElderTask data in elderTaskMag.GetCompleteTaskList())
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
    }
}
