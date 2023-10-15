using System;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangelogEditor;

[HarmonyPatch(typeof(ChangeLog), nameof(ChangeLog.Start))]
static class ChangeLogStartPatch
{
    static bool Prefix(ChangeLog __instance)
    {
        // Store the Gameobject for later use
        ChangelogEditorPlugin.ChangelogGameObject = __instance.gameObject;
        
        ChangelogEditorPlugin.UpdateChangelogWidth(null, null);

        if (ChangelogEditorPlugin.overrideText.Value == ChangelogEditorPlugin.Toggle.On)
        {
            __instance.m_textField.text = ChangelogEditorPlugin.customFileText;
            return false;
        }

        if (ChangelogEditorPlugin.shouldChangeText.Value != ChangelogEditorPlugin.Toggle.On) return true;
        __instance.m_textField.text = ChangelogEditorPlugin.customFileText + __instance.m_changeLog.text;
        return false;
    }

    static void Postfix(ChangeLog __instance)
    {
        ChangeLogExtension.topicTmp = Utils.FindChild(__instance.transform, "Topic").GetComponent<TextMeshProUGUI>();
        ChangeLogExtension.topicTmp.rectTransform.anchoredPosition += new Vector2(0, 20);
        __instance.UpdateChangelog();
    }
}

[HarmonyPatch(typeof(ChangeLog), nameof(ChangeLog.LateUpdate))]
static class ChangeLogLateUpdatePatch
{
    static void Postfix(ChangeLog __instance)
    {
        ChangeLogExtension.UpdateTopicText(__instance); // Not sure why this is needed after Hildir's patch, but it is. Didn't really care to investigate. Didn't need it before.
    }
}

public static class ChangeLogExtension
{
    internal static TextMeshProUGUI topicTmp = null!;

    public static void UpdateChangelog(this ChangeLog __instance)
    {
        __instance.m_hasSetScroll = false;
        UpdateTopicText(__instance);
        if (ChangelogEditorPlugin.overrideText.Value == ChangelogEditorPlugin.Toggle.On)
        {
            __instance.m_textField.text = ChangelogEditorPlugin.customFileText.ToLiteral();
        }
        else if (ChangelogEditorPlugin.shouldChangeText.Value == ChangelogEditorPlugin.Toggle.On)
        {
            __instance.m_textField.text = ChangelogEditorPlugin.customFileText.ToLiteral() + __instance.m_changeLog.text;
        }
        else
        {
            __instance.m_textField.text = __instance.m_changeLog.text;
        }

        __instance.gameObject.SetActive(ChangelogEditorPlugin.shouldShowChangelog.Value == ChangelogEditorPlugin.Toggle.On);
    }

    public static void UpdateTopicText(ChangeLog clog)
    {
        topicTmp.text = ChangelogEditorPlugin.topicText.Value;
    }
}