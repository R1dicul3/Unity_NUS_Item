using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RoomPillarPuzzleSceneTools
{
    [MenuItem("Tools/Whitebox/Rebuild Room 1 Pillar Puzzle")]
    public static void RebuildRoom1PillarPuzzle()
    {
        RoomPillarPuzzle2D puzzle = Object.FindFirstObjectByType<RoomPillarPuzzle2D>();
        bool createdPuzzle = puzzle == null;
        if (puzzle == null)
        {
            Transform parent = GameObject.Find("White_Box")?.transform;
            GameObject puzzleObject = new GameObject("Room_1_Pillar_Puzzle");
            if (parent != null)
            {
                puzzleObject.transform.SetParent(parent, false);
            }

            puzzle = puzzleObject.AddComponent<RoomPillarPuzzle2D>();
        }

        Transform whitebox = GameObject.Find("White_Box")?.transform;
        if (whitebox != null)
        {
            puzzle.transform.SetParent(whitebox, false);
        }

        if (createdPuzzle)
        {
            puzzle.transform.localPosition = Vector3.zero;
            puzzle.transform.localRotation = Quaternion.identity;
            puzzle.transform.localScale = Vector3.one;
        }
        puzzle.ConfigureRoom(GameObject.Find("Room_1")?.transform);
        puzzle.ConfigureSprite(FindReusableBlockSprite());
        puzzle.RebuildPuzzle();
        Selection.activeGameObject = puzzle.gameObject;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private static Sprite FindReusableBlockSprite()
    {
        foreach (SpriteRenderer renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (renderer.sprite == null)
            {
                continue;
            }

            if (renderer.gameObject.name.StartsWith("Segment_", System.StringComparison.Ordinal)
                || renderer.gameObject.name.StartsWith("Line_", System.StringComparison.Ordinal))
            {
                continue;
            }

            return renderer.sprite;
        }

        return null;
    }
}
