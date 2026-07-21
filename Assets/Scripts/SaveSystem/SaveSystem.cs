using System;
using System.IO;
using UnityEngine;

namespace SaveSystem
{
    /// <summary>
    /// 存档系统静态工具类。负责存档的读写、删除和查询。
    /// </summary>
    public static class SaveSystem
    {
        public const int MaxSlots = 3;
        public const string FileExtension = ".json";

        /// <summary>获取指定存档槽的文件路径。</summary>
        public static string GetSavePath(int slot)
        {
            return Path.Combine(Application.persistentDataPath, $"save_{slot}{FileExtension}");
        }

        /// <summary>存档槽是否存在。</summary>
        public static bool HasSave(int slot)
        {
            return File.Exists(GetSavePath(slot));
        }

        /// <summary>保存数据到指定槽位。</summary>
        public static void Save(int slot, SaveData data)
        {
            string path = GetSavePath(slot);
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
            Debug.Log($"[SaveSystem] Save written to {path}");
        }

        /// <summary>从指定槽位读取存档。</summary>
        public static SaveData Load(int slot)
        {
            string path = GetSavePath(slot);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[SaveSystem] Save slot {slot} does not exist.");
                return null;
            }

            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            Debug.Log($"[SaveSystem] Save loaded: {path}");
            return data;
        }

        /// <summary>删除指定槽位的存档。</summary>
        public static void Delete(int slot)
        {
            string path = GetSavePath(slot);
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[SaveSystem] Save slot {slot} deleted.");
            }
        }

        /// <summary>获取最大存档槽位数。</summary>
        public static int GetMaxSlots()
        {
            return MaxSlots;
        }

        /// <summary>存档是否已满。</summary>
        public static bool IsFull()
        {
            for (int i = 1; i <= MaxSlots; i++)
            {
                if (!HasSave(i))
                    return false;
            }
            return true;
        }

        /// <summary>删除最早的存档（按文件修改时间）。</summary>
        public static void DeleteOldest()
        {
            int oldestSlot = -1;
            DateTime oldestTime = DateTime.MaxValue;

            for (int i = 1; i <= MaxSlots; i++)
            {
                string path = GetSavePath(i);
                if (File.Exists(path))
                {
                    DateTime writeTime = File.GetLastWriteTime(path);
                    if (writeTime < oldestTime)
                    {
                        oldestTime = writeTime;
                        oldestSlot = i;
                    }
                }
            }

            if (oldestSlot > 0)
            {
                Delete(oldestSlot);
            }
        }

        /// <summary>获取存档的元数据信息（用于 UI 展示，无需完整反序列化）。</summary>
        public static SaveMetaInfo GetMetaInfo(int slot)
        {
            string path = GetSavePath(slot);
            if (!File.Exists(path))
                return null;

            try
            {
                string json = File.ReadAllText(path);
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                if (data == null)
                    return null;

                return new SaveMetaInfo
                {
                    slot = slot,
                    saveTimestamp = data.saveTimestamp,
                    playTimeSeconds = data.playTimeSeconds,
                    sceneName = data.sceneName
                };
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] 读取存档 {slot} 元数据失败：{e.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// 存档元数据（用于 UI 列表展示）。
    /// </summary>
    public class SaveMetaInfo
    {
        public int slot;
        public string saveTimestamp;
        public float playTimeSeconds;
        public string sceneName;

        public string GetFormattedPlayTime()
        {
            TimeSpan ts = TimeSpan.FromSeconds(playTimeSeconds);
            return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
    }
}
