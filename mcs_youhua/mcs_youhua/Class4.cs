using HarmonyLib;
using JSONClass;
using KillSystem;
using System;
using System.Collections.Generic;

namespace zjr_mcs
{
    internal class chuanyin_zengqiang
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CyNpcList), "InitNpcList", new Type[] { typeof(int) })]
        public static bool CyNpcList_InitNpcList_Prefix(ref CyNpcList __instance, ref int type)
        {
            InitNpcList(__instance, type);
            return false;
        }
        static void InitNpcList(CyNpcList __instance, int type)
        {
            Tools.ClearObj(__instance.cyNpcCell.transform);
            __instance.friendCells = new List<CyFriendCell>();
            __instance.curSelectFriend = null;
            Dictionary<string, List<EmailData>>.KeyCollection keys = Tools.instance.getPlayer().emailDateMag.newEmailDictionary.Keys;
            List<int> list = new List<int>();
            foreach (string text in keys)
            {
                if ((type != -5 || int.Parse(text) == 3067) && (type != -1 || jsonData.instance.AvatarJsonData[text].TryGetField("IsTag").b) && (type != -4 || PlayerEx.IsDaoLv(int.Parse(text))) && (type != -3 || (CyTeShuNpc.DataDict.ContainsKey(int.Parse(text)) && CyTeShuNpc.DataDict[int.Parse(text)].Type == 1)) && (type < 0 || jsonData.instance.AvatarJsonData[text]["MenPai"].I == type))
                {
                    CyFriendCell component = Tools.InstantiateGameObject(__instance.cyNpcCell, __instance.npcCellParent.transform).GetComponent<CyFriendCell>();
                    component.Init(int.Parse(text));
                    component.redDian.SetActive(true);
                    list.Add(int.Parse(text));
                    __instance.friendCells.Add(component);
                }
            }
            for (int i = 0; i < __instance.friendList.Count; i++)
            {
                int num = __instance.friendList[i];
                if (!list.Contains(__instance.friendList[i]) && __instance.friendList[i] != 0 && (type != -5 || num == 3067) && (type != -4 || PlayerEx.IsDaoLv(num)) && (type != -3 || (CyTeShuNpc.DataDict.ContainsKey(num) && CyTeShuNpc.DataDict[num].Type == 1)))
                {
                    if (type == -1)
                    {
                        if (NpcJieSuanManager.inst.IsDeath(num))
                        {
                            goto IL_2FC;
                        }
                        jsonData.instance.AvatarJsonData[__instance.friendList[i].ToString()].TryGetField("IsTag");
                        if (!jsonData.instance.AvatarJsonData[__instance.friendList[i].ToString()].TryGetField("IsTag").b)
                        {
                            goto IL_2FC;
                        }
                    }
                    else if (type >= 0 && (NpcJieSuanManager.inst.IsDeath(num) || jsonData.instance.AvatarJsonData[__instance.friendList[i].ToString()].TryGetField("MenPai").I != type))
                    {
                        goto IL_2FC;
                    }
                    if (type == -10)
                    {
                        if (NpcJieSuanManager.inst.IsDeath(num))
                        {
                        }
                        else
                        {
                            goto IL_2FC;
                        }
                    }
                    CyFriendCell component2 = Tools.InstantiateGameObject(__instance.cyNpcCell, __instance.npcCellParent.transform).GetComponent<CyFriendCell>();
                    component2.Init(__instance.friendList[i]);
                    __instance.friendCells.Add(component2);
                }
                IL_2FC:;
            }
            __instance.isShowSelectTag = false;
            __instance.sanJiao.transform.localRotation.Set(0f, 0f, 180f, 0f);
            CyUIMag.inst.cyEmail.cySendBtn.Hide();
            CyUIMag.inst.cyEmail.Restart();
            __instance.curSelectFriend = null;

        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CyNpcList), "ShowMoreSelect")]
        public static bool CyNpcList_ShowMoreSelect_Prefix(ref CyNpcList __instance)
        {
            ShowMoreSelect(__instance);
            return false;
        }
        // Token: 0x06001861 RID: 6241 RVA: 0x000AA830 File Offset: 0x000A8A30
        static void ShowMoreSelect(CyNpcList __instance)
        {
            __instance.isShowSelectTag = !__instance.isShowSelectTag;
            if (__instance.isShowSelectTag)
            {
                if (__instance.selectPanel.transform.childCount < 2)
                {
                    Tools.InstantiateGameObject(__instance.cySelectCell, __instance.selectPanel.transform).GetComponent<CySelectCell>().Init("全部", delegate
                    {
                        __instance.selectPanel.SetActive(false);
                        __instance.curSelect.text = "全部";
                        InitNpcList(__instance, -2);
                    });
                    if (KillManager.Inst.KillerModels.ContainsKey(1))
                    {
                        Tools.InstantiateGameObject(__instance.cySelectCell, __instance.selectPanel.transform).GetComponent<CySelectCell>().Init("风雨楼", delegate
                        {
                            __instance.selectPanel.SetActive(false);
                            __instance.curSelect.text = "风雨楼";
                            InitNpcList(__instance, -5);
                        });
                    }
                    Tools.InstantiateGameObject(__instance.cySelectCell, __instance.selectPanel.transform).GetComponent<CySelectCell>().Init("拍卖会", delegate
                    {
                        __instance.selectPanel.SetActive(false);
                        __instance.curSelect.text = "拍卖会";
                        InitNpcList(__instance, -3);
                    });
                    Tools.InstantiateGameObject(__instance.cySelectCell, __instance.selectPanel.transform).GetComponent<CySelectCell>().Init("标记", delegate
                    {
                        __instance.selectPanel.SetActive(false);
                        __instance.curSelect.text = "标记";
                        InitNpcList(__instance, -1);
                    });
                    Tools.InstantiateGameObject(__instance.cySelectCell, __instance.selectPanel.transform).GetComponent<CySelectCell>().Init("道侣", delegate
                    {
                        __instance.selectPanel.SetActive(false);
                        __instance.curSelect.text = "道侣";
                        InitNpcList(__instance, -4);
                    });
                    using (List<JSONObject>.Enumerator enumerator = jsonData.instance.CyShiLiNameData.list.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            JSONObject data = enumerator.Current;
                            Tools.InstantiateGameObject(__instance.cySelectCell, __instance.selectPanel.transform).GetComponent<CySelectCell>().Init(data["name"].Str, delegate
                            {
                                __instance.selectPanel.SetActive(false);
                                __instance.curSelect.text = data["name"].Str;
                                InitNpcList(__instance, data["id"].I);
                            });
                        }
                    }
                    Tools.InstantiateGameObject(__instance.cySelectCell, __instance.selectPanel.transform).GetComponent<CySelectCell>().Init("失联", delegate
                    {
                        __instance.selectPanel.SetActive(false);
                        __instance.curSelect.text = "失联";
                        InitNpcList(__instance, -10);
                    });
                }
                __instance.sanJiao.transform.localRotation.Set(0f, 0f, 0f, 0f);
                __instance.selectPanel.SetActive(true);
                return;
            }
            __instance.isShowSelectTag = false;
            __instance.selectPanel.SetActive(false);
            __instance.sanJiao.transform.localRotation.Set(0f, 0f, 180f, 0f);
        }
    }
}
