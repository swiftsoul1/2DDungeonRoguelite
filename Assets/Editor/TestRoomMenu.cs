#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

public static class TestRoomMenu
{
    [MenuItem("Dungeon/Play Test Room")]
    private static void PlayTestRoom()
    {
        // Respect unsaved scene work; Unity supplies its normal save/discard/cancel dialog.
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        PrototypeArtImporter.PreparePrototypeArt();
        EditorSceneManager.OpenScene("Assets/Scenes/GridRoomPrototype.unity");
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Dungeon/Play Test Room", true)]
    private static bool CanPlayTestRoom()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }
}
#endif
