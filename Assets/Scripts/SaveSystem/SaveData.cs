using System;
using UnityEngine;

namespace SaveSystem
{
    /// <summary>
    /// 可序列化的 Vector3 包装类，因为 Unity 的 JsonUtility 不支持直接序列化 Vector3。
    /// </summary>
    [Serializable]
    public class SerializableVector3
    {
        public float x;
        public float y;
        public float z;

        public SerializableVector3() { }

        public SerializableVector3(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    /// <summary>
    /// 存档数据结构。
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int saveVersion;

        /// <summary>玩家位置</summary>
        public SerializableVector3 playerPosition;

        /// <summary>累计游玩时间（秒）</summary>
        public float playTimeSeconds;

        /// <summary>保存时间戳（ISO 8601 格式）</summary>
        public string saveTimestamp;

        /// <summary>保存时的场景名</summary>
        public string sceneName;

        public bool isPoweredMode;
        public int levelVariantIndex;
        public string currentEmotion;
        public string[] collectedObjectIds;
        public PillarPuzzleState[] pillarPuzzleStates;
    }

    [Serializable]
    public class PillarPuzzleState
    {
        public string puzzleId;
        public PillarState[] pillars;
    }

    [Serializable]
    public class PillarState
    {
        public string pillarId;
        public float currentSinkDistance;
        public float targetSinkDistance;
        public int targetVisibleSegmentCount;
        public bool hasBeenActivated;
    }
}
