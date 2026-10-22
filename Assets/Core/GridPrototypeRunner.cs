#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEngine;

public class GridPrototypeRunner : MonoBehaviour
{
    private TestRoomSetup setup;
    private GridEncounter encounter;
    private Camera gameCamera;
    private Transform visualRoot;
    private readonly Dictionary<Entity, SpriteRenderer> characters = new Dictionary<Entity, SpriteRenderer>();
    private readonly Dictionary<Vector2Int, SpriteRenderer> cells = new Dictionary<Vector2Int, SpriteRenderer>();
    private readonly Dictionary<Vector2Int, SpriteRenderer> boundaries = new Dictionary<Vector2Int, SpriteRenderer>();
    private readonly Dictionary<WallProp, SpriteRenderer> fixtures = new Dictionary<WallProp, SpriteRenderer>();
    private readonly Dictionary<string, Sprite> art = new Dictionary<string, Sprite>();
    private bool largeEnemy;
    private bool rangedEnemy;
    private bool targeting;
    private string feedback = "";
    private string loadError;
    private GUIStyle labelStyle;
    private GUIStyle buttonStyle;
    private float cameraHalfHeight = 3.5f;
    private RoomAtmosphere atmosphere;
    private readonly Dictionary<Enemy, SkeletonSpriteAnimator> enemyAnimations = new Dictionary<Enemy, SkeletonSpriteAnimator>();
    private bool AnimationBusy
    {
        get { foreach (SkeletonSpriteAnimator animation in enemyAnimations.Values) if (animation.Busy) return true; return false; }
    }
    private readonly Dictionary<PropObject, SpriteRenderer> floorProps = new Dictionary<PropObject, SpriteRenderer>();
    private bool inventoryOpen;
    private Vector2 inventoryScroll;

    private float UiScale { get { return Mathf.Max(0.1f, Mathf.Min(Screen.width / 960f, Screen.height / 720f)); } }
    private float UiWidth { get { return Screen.width / UiScale; } }
    private float UiHeight { get { return Screen.height / UiScale; } }
    private Rect StatusRect { get { return new Rect(12, 12, 350, 210); } }
    private Rect ControlsRect { get { return new Rect(12, UiHeight - 212, 345, 200); } }
    private Rect OptionsRect { get { return new Rect(UiWidth - 230, UiHeight - 270, 218, 258); } }

    private void Start()
    {
        foreach (string name in new[] { "player-idle", "sentinel-idle", "floor-slate", "wall-slate",
            "wall-edge", "wall-corner", "door-closed", "door-open", "bookcase",
            "floor-quiet", "violet-lamp", "rune-decal", "chest-closed", "chest-open", "crate" })
        {
            Sprite sprite = Resources.Load<Sprite>("DungeonPrototype/Art/" + name);
            if (sprite == null)
            {
                loadError = "Room art is still importing or missing: " + name +
                    ". Stop Play, choose Dungeon > Reimport Prototype Art, then play again.";
                Debug.LogError(loadError);
                return;
            }
            art.Add(name, sprite);
        }
        gameCamera = Camera.main;
        if (gameCamera == null)
        {
            GameObject cameraObject = new GameObject("Player Camera");
            cameraObject.tag = "MainCamera";
            gameCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }
        gameCamera.orthographic = true;
        gameCamera.clearFlags = CameraClearFlags.SolidColor;
        gameCamera.backgroundColor = new Color(0.035f, 0.035f, 0.05f);
        BuildRoom();
    }

    private void BuildRoom()
    {
        foreach (SkeletonSpriteAnimator animation in enemyAnimations.Values) animation.Dispose();
        enemyAnimations.Clear();
        if (atmosphere != null) { atmosphere.Dispose(); atmosphere = null; }
        if (visualRoot != null)
        {
            visualRoot.gameObject.SetActive(false);
            Destroy(visualRoot.gameObject);
        }
        encounter = null;
        characters.Clear();
        cells.Clear();
        boundaries.Clear();
        fixtures.Clear();
        floorProps.Clear();
        inventoryOpen = false;
        targeting = false;
        inventoryScroll = Vector2.zero;
        try { setup = TestRoomSetup.Create(largeEnemy, rangedEnemy, true); }
        catch (Exception exception)
        {
            loadError = "The test room could not be assembled. Check the Console for details.";
            Debug.LogException(exception);
            return;
        }
        encounter = setup.Encounter;
        visualRoot = new GameObject("Generated Test Room").transform;
        RoomGrid grid = setup.Room.Grid;
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                int tileHash = RoomAtmosphere.TileHash(x, y);
                string floorAsset = ((uint)tileHash % 5u) == 0 ? "floor-slate" : "floor-quiet";
                SpriteRenderer tile = MakeSprite(floorAsset, "Floor " + x + "," + y, 0);
                Fit(tile, x, y, 1, 1, 1f);
                tile.transform.rotation = Quaternion.Euler(0, 0, ((uint)tileHash % 4u) * 90);
                cells.Add(new Vector2Int(x, y), tile);
                bool seam = setup.Connected && y == 9;
                bool edge = seam || x == 0 || y == 0 || x == grid.Width - 1 || y == grid.Height - 1;
                if (!edge) continue;
                bool corner = (x == 0 || x == grid.Width - 1) && (y == 0 || y == grid.Height - 1);
                SpriteRenderer wall = MakeSprite(corner ? "wall-corner" : "wall-edge", "Wall " + x + "," + y, 1);
                Fit(wall, x, y, 1, 1, 1f);
                int angle = corner ? (x == 0 ? (y == 0 ? 90 : 0) : (y == 0 ? 180 : 270)) :
                    (seam || y == grid.Height - 1 ? 0 : y == 0 ? 180 : x == 0 ? 90 : 270);
                wall.transform.rotation = Quaternion.Euler(0, 0, angle);
                boundaries.Add(new Vector2Int(x, y), wall);
            }
        }
        characters.Add(setup.Player, MakeSprite("player-idle", "Hero", 10));
        characters.Add(setup.Enemy, MakeSprite("sentinel-idle", "Sentinel", 10));
        AttachEnemyAnimation(setup.Enemy);
        if (setup.SecondEnemy != null)
        {
            characters.Add(setup.SecondEnemy, MakeSprite("sentinel-idle", "Chamber Archer", 10));
            AttachEnemyAnimation(setup.SecondEnemy);
        }
        fixtures.Add(setup.Door, MakeSprite("door-closed", "North Door", 8));
        fixtures.Add(setup.Bookcase, MakeSprite("bookcase", "Bookcase", 8));
        foreach (WallProp lamp in setup.Lamps)
            fixtures.Add(lamp, MakeSprite("violet-lamp", "Violet Wall Lamp", 8));
        foreach (PropObject prop in setup.Room.Props)
            floorProps.Add(prop, MakeSprite(prop is Chest ? "chest-closed" : "crate", prop.Name, 7));
        SpriteRenderer emblem = MakeSprite("rune-decal", "Broken Ring Floor Inlay", 1);
        Fit(emblem, 5, 4, 2, 2, 0.95f);
        emblem.color = new Color(0.8f, 0.75f, 0.85f, 0.38f);
        if (setup.Connected)
        {
            SpriteRenderer secondEmblem = MakeSprite("rune-decal", "Chamber Inlay", 1);
            Fit(secondEmblem, 5, 14, 2, 2, 0.95f);
            secondEmblem.color = emblem.color;
        }
        atmosphere = new RoomAtmosphere(visualRoot, setup);
        feedback = "";
        RefreshViews();
        FollowPlayer();
        Debug.Log("Test room ready: grid, combat, wall fixtures, camera and controls connected.");
    }

    private void AttachEnemyAnimation(Enemy enemy)
    {
        string asset = enemy.IsRanged ? "skeleton-archer-atlas-v1" : "skeleton-boneclub-atlas-v1";
        Texture2D atlas = Resources.Load<Texture2D>("DungeonPrototype/Art/" + asset);
        if (atlas != null) enemyAnimations.Add(enemy, new SkeletonSpriteAnimator(enemy, characters[enemy], atlas, enemy.IsRanged, setup.Player));
        else Debug.LogWarning("Enemy animation atlas missing: " + asset + "; using existing placeholder art.");
    }

    private SpriteRenderer MakeSprite(string asset, string name, int order)
    {
        GameObject view = new GameObject(name);
        view.transform.SetParent(visualRoot);
        SpriteRenderer renderer = view.AddComponent<SpriteRenderer>();
        renderer.sprite = art[asset];
        renderer.sortingOrder = order;
        return renderer;
    }

    private static void Fit(SpriteRenderer view, int x, int y, int width, int height, float padding)
    {
        view.transform.position = new Vector3(x + (width - 1) * 0.5f, y + (height - 1) * 0.5f, 0);
        float scale = Mathf.Min(width / view.sprite.bounds.size.x, height / view.sprite.bounds.size.y) * padding;
        view.transform.localScale = new Vector3(scale, scale, 1);
    }

    private bool PointerOverUi()
    {
        if (inventoryOpen) return true;
        Vector2 point = new Vector2(Input.mousePosition.x / UiScale, (Screen.height - Input.mousePosition.y) / UiScale);
        return StatusRect.Contains(point) || ControlsRect.Contains(point) || OptionsRect.Contains(point);
    }

    private void Update()
    {
        if (encounter == null) return;
        if (Input.GetKeyDown(KeyCode.I)) { targeting = false; inventoryOpen = !inventoryOpen; return; }
        if (Input.GetKeyDown(KeyCode.R)) { BuildRoom(); return; }
        if (inventoryOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) inventoryOpen = false;
            return;
        }
        if (AnimationBusy) return;
        if (!PointerOverUi() && Input.mouseScrollDelta.y != 0)
            AdjustZoom(-Input.mouseScrollDelta.y * 0.5f);
        if (Input.GetKeyDown(KeyCode.F)) { targeting = !targeting; return; }
        if (targeting)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)) { targeting = false; return; }
            if (Input.GetMouseButtonDown(0) && !PointerOverUi())
            {
                Vector3 aim = gameCamera.ScreenToWorldPoint(Input.mousePosition);
                Enemy target = encounter.Grid.GetOccupant(Mathf.RoundToInt(aim.x), Mathf.RoundToInt(aim.y)) as Enemy;
                if (encounter.TryAttack(target)) targeting = false;
            }
            return;
        }
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) encounter.TryStep(0, 1);
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) encounter.TryStep(0, -1);
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) encounter.TryStep(-1, 0);
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) encounter.TryStep(1, 0);
        else if (Input.GetKeyDown(KeyCode.Space)) encounter.Wait();
        else if (Input.GetKeyDown(KeyCode.E)) encounter.TryUseNearbyWallObject();
        else if (Input.GetMouseButtonDown(0) && !PointerOverUi())
        {
            Vector3 world = gameCamera.ScreenToWorldPoint(Input.mousePosition);
            int x = Mathf.RoundToInt(world.x), y = Mathf.RoundToInt(world.y);
            Enemy clicked = encounter.Grid.GetOccupant(x, y) as Enemy;
            if (clicked == null || encounter.Grid.AreAdjacent(setup.Player, clicked)) encounter.TryInteractCell(x, y);
        }
    }

    private void LateUpdate()
    {
        if (encounter == null) return;
        RefreshViews();
        FollowPlayer();
    }

    private void FollowPlayer()
    {
        Player player = encounter.Player;
        gameCamera.transform.position = new Vector3(player.XPos + (player.FootprintWidth - 1) * 0.5f,
            player.YPos + (player.FootprintHeight - 1) * 0.5f, -10);
        gameCamera.transform.rotation = Quaternion.identity;
        gameCamera.orthographicSize = cameraHalfHeight;
    }

    private void AdjustZoom(float amount)
    {
        cameraHalfHeight = Mathf.Clamp(cameraHalfHeight + amount, 2.5f, 6f);
    }

    private void RefreshViews()
    {
        if (atmosphere != null) atmosphere.RefreshShadows();
        foreach (KeyValuePair<PropObject, SpriteRenderer> pair in floorProps)
        {
            Chest chest = pair.Key as Chest;
            if (chest != null) pair.Value.sprite = art[chest.IsUsed ? "chest-open" : "chest-closed"];
            Fit(pair.Value, pair.Key.XPos, pair.Key.YPos, pair.Key.FootprintWidth, pair.Key.FootprintHeight, 0.95f);
        }
        foreach (KeyValuePair<Vector2Int, SpriteRenderer> pair in boundaries)
            pair.Value.enabled = !(encounter.Grid.GetWallObject(pair.Key.x, pair.Key.y) is Door);
        foreach (KeyValuePair<WallProp, SpriteRenderer> pair in fixtures)
        {
            WallProp prop = pair.Key;
            Door door = prop as Door;
            if (door != null) pair.Value.sprite = art[door.IsOpen ? "door-open" : "door-closed"];
            Fit(pair.Value, prop.XPos, prop.YPos, prop.FootprintWidth, prop.FootprintHeight, 0.98f);
            if (door != null)
            {
                // Door art must fill the actual opening rather than shrink to a square.
                bool sideways = prop.Facing == WallFacing.East || prop.Facing == WallFacing.West;
                float width = sideways ? prop.FootprintHeight : prop.FootprintWidth;
                float height = sideways ? prop.FootprintWidth : prop.FootprintHeight;
                Vector3 bounds = pair.Value.sprite.bounds.size;
                pair.Value.transform.localScale = new Vector3(width * 0.98f / bounds.x, height * 0.98f / bounds.y, 1);
            }
            pair.Value.transform.rotation = Quaternion.Euler(0, 0, -(int)prop.Facing * 90);
            pair.Value.color = prop.IsUsed ? new Color(0.7f, 0.7f, 0.7f) : Color.white;
        }
        foreach (KeyValuePair<Entity, SpriteRenderer> pair in characters)
        {
            LivingEntity living = pair.Key as LivingEntity;
            Enemy animated = pair.Key as Enemy;
            if (animated != null && enemyAnimations.ContainsKey(animated)) continue;
            pair.Value.enabled = living == null || living.IsAlive;
            Fit(pair.Value, pair.Key.XPos, pair.Key.YPos, pair.Key.FootprintWidth, pair.Key.FootprintHeight, 0.94f);
        }
        foreach (SkeletonSpriteAnimator animation in enemyAnimations.Values) animation.Tick(Time.unscaledDeltaTime);
        foreach (KeyValuePair<Vector2Int, SpriteRenderer> pair in cells)
        {
            Entity occupant = encounter.Grid.GetOccupant(pair.Key.x, pair.Key.y);
            Color lighting = atmosphere != null ? atmosphere.FloorTint(pair.Key.x, pair.Key.y) : Color.white;
            Color highlight = occupant == encounter.Player ? new Color(1f, 0.88f, 0.88f) :
                occupant != null ? new Color(0.92f, 0.84f, 1f) : Color.white;
            if (targeting)
            {
                int range = setup.Player.EquippedWeapon == null ? 1 : setup.Player.EquippedWeapon.Range;
                int gapX = Mathf.Max(0, Mathf.Max(setup.Player.XPos - pair.Key.x, pair.Key.x - (setup.Player.XPos + setup.Player.FootprintWidth - 1)));
                int gapY = Mathf.Max(0, Mathf.Max(setup.Player.YPos - pair.Key.y, pair.Key.y - (setup.Player.YPos + setup.Player.FootprintHeight - 1)));
                if (gapX + gapY <= range) highlight = new Color(0.6f, 0.8f, 1f);
                Enemy target = occupant as Enemy;
                if (target != null) highlight = encounter.CanAttack(target) ? new Color(0.45f, 1f, 0.5f) : new Color(1f, 0.4f, 0.4f);
            }
            pair.Value.color = lighting * highlight;
        }
    }

    private void OnDestroy()
    {
        foreach (SkeletonSpriteAnimator animation in enemyAnimations.Values) animation.Dispose();
        enemyAnimations.Clear();
        if (atmosphere != null) atmosphere.Dispose();
    }

    private void OnGUI()
    {
        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(UiScale, UiScale, 1));
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 16 };
        }
        if (loadError != null)
        {
            GUI.Box(new Rect(20, 20, 650, 120), "Room setup");
            GUI.Label(new Rect(35, 50, 620, 85), loadError, labelStyle);
        }
        else if (encounter != null)
        {
            DrawStatus();
            if (inventoryOpen) DrawInventory();
            else { DrawControls(); DrawOptions(); }
        }
        GUI.matrix = previousMatrix;
    }

    private void DrawStatus()
    {
        string location = setup.Player.YPos >= 10 ? "CHAMBER 2" : setup.Player.YPos >= 9 ? "CONNECTING DOORWAY" : "CHAMBER 1";
        GUI.Box(StatusRect, location);
        GUI.Label(new Rect(24, 42, 325, 26), "Hero " + setup.Player.CurrentHealth + "/" + setup.Player.MaxHealth +
            " HP   Armor " + setup.Player.Armor + "   Turn " + encounter.TurnNumber, labelStyle);
        Enemy localEnemy = setup.Player.YPos >= 10 ? setup.SecondEnemy : setup.Enemy;
        GUI.Label(new Rect(24, 70, 325, 26), localEnemy.IsAlive ? localEnemy.Name + " " + localEnemy.CurrentHealth +
            "/" + localEnemy.MaxHealth + " HP" : "Chamber enemy defeated", labelStyle);
        GUI.Label(new Rect(24, 98, 325, 26), !setup.Player.IsAlive ? "DEFEATED — restart to try again" :
            targeting ? "AIM: green target valid; red blocked/out of range" : encounter.IsOver ? "ROOM CLEARED — keep exploring" : "YOUR TURN", labelStyle);
        GUI.Label(new Rect(24, 130, 325, 78), encounter.LastAction, labelStyle);
    }

    private void DrawControls()
    {
        Rect panel = ControlsRect;
        GUI.Box(panel, "Move, fight, and explore");
        float top = panel.y + 25;
        bool previousEnabled = GUI.enabled;
        GUI.enabled = previousEnabled && !targeting && !AnimationBusy;
        if (GUI.Button(new Rect(76, top, 48, 36), "Up", buttonStyle)) encounter.TryStep(0, 1);
        if (GUI.Button(new Rect(24, top + 40, 48, 36), "Left", buttonStyle)) encounter.TryStep(-1, 0);
        if (GUI.Button(new Rect(76, top + 40, 48, 36), "Wait", buttonStyle)) encounter.Wait();
        if (GUI.Button(new Rect(128, top + 40, 48, 36), "Right", buttonStyle)) encounter.TryStep(1, 0);
        if (GUI.Button(new Rect(76, top + 80, 48, 36), "Down", buttonStyle)) encounter.TryStep(0, -1);
        if (GUI.Button(new Rect(192, top, 150, 36), "Use nearby (E)", buttonStyle)) encounter.TryUseNearbyWallObject();
        GUI.enabled = previousEnabled;
        if (GUI.Button(new Rect(192, top + 40, 150, 36), "Restart (R)", buttonStyle)) BuildRoom();
        if (GUI.Button(new Rect(192, top + 80, 150, 36), "Inventory (I)", buttonStyle)) { targeting = false; inventoryOpen = true; }
        GUI.enabled = previousEnabled && !AnimationBusy;
        if (GUI.Button(new Rect(24, top + 120, 318, 36), targeting ? "Cancel aim (Esc)" : "Attack / Aim (F)", buttonStyle)) targeting = !targeting;
        GUI.enabled = previousEnabled;
    }

    private void DrawInventory()
    {
        Rect panel = new Rect(UiWidth * 0.5f - 310, UiHeight * 0.5f - 230, 620, 460);
        GUI.Box(panel, "INVENTORY  " + setup.Player.Inventory.Items.Count + "/" + setup.Player.Inventory.Capacity);
        if (GUI.Button(new Rect(panel.x + 510, panel.y + 8, 100, 30), "Close (I)", buttonStyle)) inventoryOpen = false;
        GUI.Label(new Rect(panel.x + 15, panel.y + 40, 585, 50),
            "Weapon: " + (setup.Player.EquippedWeapon == null ? "none (" + setup.Player.BaseAttackDamage + " damage)" : setup.Player.EquippedWeapon.Name) +
            "\nArmor: " + (setup.Player.EquippedArmor == null ? "none" : setup.Player.EquippedArmor.Name) + "  /  total defense " + setup.Player.Armor,
            labelStyle);
        List<InventoryItem> items = new List<InventoryItem>(setup.Player.Inventory.Items);
        inventoryScroll = GUI.BeginScrollView(new Rect(panel.x + 15, panel.y + 98, 590, 225), inventoryScroll,
            new Rect(0, 0, 565, Mathf.Max(220, items.Count * 66)));
        for (int i = 0; i < items.Count; i++)
        {
            InventoryItem item = items[i];
            float y = i * 66;
            bool equipped = setup.Player.EquippedWeapon == item || setup.Player.EquippedArmor == item;
            GUI.Label(new Rect(5, y + 4, 410, 60), item.Name + (equipped ? " (equipped)" : "") + "\n" + item.Description, labelStyle);
            bool previous = GUI.enabled;
            GUI.enabled = previous && setup.Player.IsAlive && !equipped && !AnimationBusy;
            if (GUI.Button(new Rect(430, y + 10, 125, 38), equipped ? "Equipped" : item is Consumable ? "Use" : "Equip", buttonStyle))
                encounter.TryUseItem(item);
            GUI.enabled = previous;
        }
        if (items.Count == 0) GUI.Label(new Rect(5, 12, 550, 70), "Your inventory is empty. Search the supply chest to find equipment.", labelStyle);
        GUI.EndScrollView();
        bool wasEnabled = GUI.enabled;
        GUI.enabled = wasEnabled && setup.Player.IsAlive && setup.Player.EquippedWeapon != null && !AnimationBusy;
        if (GUI.Button(new Rect(panel.x + 15, panel.y + 332, 285, 36), "Unequip weapon", buttonStyle)) encounter.TryUnequipWeapon();
        GUI.enabled = wasEnabled && setup.Player.IsAlive && setup.Player.EquippedArmor != null && !AnimationBusy;
        if (GUI.Button(new Rect(panel.x + 310, panel.y + 332, 285, 36), "Unequip armor", buttonStyle)) encounter.TryUnequipArmor();
        GUI.enabled = wasEnabled;
        GUI.Label(new Rect(panel.x + 15, panel.y + 380, 585, 68), encounter.LastAction +
            "\nEquipping and using an item take one turn. Opening inventory is free.", labelStyle);
    }

    private void DrawOptions()
    {
        Rect panel = OptionsRect;
        GUI.Box(panel, "View and size tests");
        if (GUI.Button(new Rect(panel.x + 10, panel.y + 28, 198, 36), setup.Player.FootprintWidth == 1 ?
            "Grow hero to 2x2" : "Restore hero size", buttonStyle))
        {
            bool changed = setup.Player.FootprintWidth == 1 ? setup.Player.TryResize(2, 2) : setup.Player.TryRestoreBaseSize();
            feedback = changed ? "Hero: " + setup.Player.FootprintWidth + "x" + setup.Player.FootprintHeight : "Not enough clear space";
        }
        if (GUI.Button(new Rect(panel.x + 10, panel.y + 70, 198, 36), largeEnemy ? "Restart: 1x1 sentinel" : "Restart: 2x2 sentinel", buttonStyle))
        {
            largeEnemy = !largeEnemy;
            BuildRoom();
        }
        if (GUI.Button(new Rect(panel.x + 10, panel.y + 112, 198, 48), rangedEnemy ? "Restart: melee enemy" : "Restart: ranged enemy", buttonStyle))
        {
            rangedEnemy = !rangedEnemy;
            largeEnemy = false;
            BuildRoom();
        }
        if (GUI.Button(new Rect(panel.x + 10, panel.y + 174, 95, 36), "Zoom +", buttonStyle)) AdjustZoom(-0.5f);
        if (GUI.Button(new Rect(panel.x + 113, panel.y + 174, 95, 36), "Zoom -", buttonStyle)) AdjustZoom(0.5f);
        GUI.Label(new Rect(panel.x + 10, panel.y + 218, 198, 30), "Zoom " + (3.5f / cameraHalfHeight).ToString("0.0") +
            "x  /  mouse wheel", labelStyle);
    }
}
#endif
