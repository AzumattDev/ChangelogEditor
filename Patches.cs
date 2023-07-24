using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace ChangelogEditor;

[HarmonyPatch(typeof(ChangeLog),nameof(ChangeLog.Start))]
static class ChangeLogStartPatch
{
    static bool Prefix(ChangeLog __instance)
    {
        // Store the Gameobject for later use
        ChangelogEditorPlugin.ChangelogGameObject = __instance.gameObject;

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
        __instance.UpdateChangelog();
    }
}

public static class ChangeLogExtension
{
    public static void UpdateChangelog(this ChangeLog __instance)
    {
        __instance.m_hasSetScroll = false;
        UpdateTopicText(__instance);
        if (ChangelogEditorPlugin.overrideText.Value == ChangelogEditorPlugin.Toggle.On)
        {
            __instance.m_textField.text = ChangelogEditorPlugin.customFileText;
        }
        else if (ChangelogEditorPlugin.shouldChangeText.Value == ChangelogEditorPlugin.Toggle.On)
        {
            __instance.m_textField.text = ChangelogEditorPlugin.customFileText + __instance.m_changeLog.text;
        }
        else
        {
            __instance.m_textField.text = __instance.m_changeLog.text;
        }
        __instance.gameObject.SetActive(ChangelogEditorPlugin.shouldShowChangelog.Value == ChangelogEditorPlugin.Toggle.On);

    }

    public static void UpdateTopicText(ChangeLog clog)
    {
        Utils.FindChild(clog.transform, "Topic").GetComponent<Text>().text = ChangelogEditorPlugin.topicText.Value;
    }
}