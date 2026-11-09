#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Contracts;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Services;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Persistence;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Validation;
using UnityEditor;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Temporary
{
    public static class BatchLevelGenerator
    {
        [MenuItem("Tools/Paw Slide Pop/Level Editor/Generate All Levels (1-36)")]
        public static void GenerateAll()
        {
            Match3TileDatabaseSO database = AssetDatabase.LoadAssetAtPath<Match3TileDatabaseSO>("Assets/_PawSlidePopGame/_Data/Tiles/Database.asset");
            if (database == null)
            {
                Debug.LogError("[LevelEditor] Tile database asset not found.");
                return;
            }

            int successCount = 0;
            for (int levelNum = 1; levelNum <= 36; levelNum++)
            {
                if (TryGenerateLevel(levelNum, database))
                {
                    successCount++;
                }
            }

            Debug.Log($"[LevelEditor] Generated {successCount}/36 levels successfully.");
            AssetDatabase.Refresh();
        }

        public static bool TryGenerateLevel(int levelNum, Match3TileDatabaseSO database)
        {
            string levelId = $"Level_{levelNum:D3}";
            
            // 1. Basic properties
            int width = 8;
            int height = 8;
            int moves = 30;
            
            if (levelNum == 1) { width = 6; height = 6; moves = 30; }
            else if (levelNum == 2) { width = 7; height = 7; moves = 35; }
            else if (levelNum == 12) { width = 7; height = 7; moves = 20; }
            
            // 2. Allowed Spawnable Tiles
            List<int> allowedSpawnables = new List<int> { 101, 102, 103, 104, 106 }; // Default 5 normal tiles (Alligator, Bear, Cat, Dog, Koala)
            if (levelNum <= 3) allowedSpawnables = new List<int> { 101, 102, 103, 104 }; // Simpler for early levels
            else if (levelNum >= 13 && levelNum <= 25) allowedSpawnables = new List<int> { 101, 102, 103, 104, 106, 105 }; // 6 tiles (add Panda)
            else if (levelNum >= 26) allowedSpawnables = new List<int> { 101, 102, 103, 104, 106, 105, 107 }; // 7 tiles (add Sheep)
            
            // 3. Goals / Targets
            List<LevelTargetRequirement> targets = new List<LevelTargetRequirement>();
            List<LevelOverlayRequirement> overlays = new List<LevelOverlayRequirement>();
            
            switch (levelNum)
            {
                case 1:
                    targets.Add(new LevelTargetRequirement(101, 10)); // Alligator
                    targets.Add(new LevelTargetRequirement(102, 10)); // Bear
                    break;
                case 2:
                    targets.Add(new LevelTargetRequirement(101, 15));
                    targets.Add(new LevelTargetRequirement(102, 15));
                    break;
                case 3:
                    targets.Add(new LevelTargetRequirement(101, 15));
                    targets.Add(new LevelTargetRequirement(103, 15)); // Cat
                    break;
                case 4:
                    targets.Add(new LevelTargetRequirement(101, 20));
                    targets.Add(new LevelTargetRequirement(104, 20)); // Dog
                    break;
                case 5:
                    targets.Add(new LevelTargetRequirement(101, 20));
                    targets.Add(new LevelTargetRequirement(106, 15)); // Koala
                    break;
                case 6:
                    targets.Add(new LevelTargetRequirement(102, 20));
                    targets.Add(new LevelTargetRequirement(103, 15));
                    break;
                case 7:
                    targets.Add(new LevelTargetRequirement(103, 20));
                    targets.Add(new LevelTargetRequirement(104, 15));
                    break;
                case 8:
                    targets.Add(new LevelTargetRequirement(104, 25)); // Collect target
                    targets.Add(new LevelTargetRequirement(101, 20));
                    break;
                case 9:
                    targets.Add(new LevelTargetRequirement(102, 25));
                    targets.Add(new LevelTargetRequirement(106, 20));
                    break;
                case 10:
                    targets.Add(new LevelTargetRequirement(101, 20));
                    targets.Add(new LevelTargetRequirement(102, 20));
                    break;
                case 11:
                    targets.Add(new LevelTargetRequirement(102, 20));
                    targets.Add(new LevelTargetRequirement(103, 20));
                    break;
                case 12:
                    targets.Add(new LevelTargetRequirement(103, 25));
                    targets.Add(new LevelTargetRequirement(104, 25));
                    break;
                case 13:
                    targets.Add(new LevelTargetRequirement(302, 6)); // Bubble overlay target
                    targets.Add(new LevelTargetRequirement(104, 20));
                    overlays.Add(new LevelOverlayRequirement(302, 6));
                    break;
                case 14:
                    targets.Add(new LevelTargetRequirement(302, 12));
                    targets.Add(new LevelTargetRequirement(106, 15));
                    overlays.Add(new LevelOverlayRequirement(302, 12));
                    break;
                case 15:
                    targets.Add(new LevelTargetRequirement(302, 18));
                    targets.Add(new LevelTargetRequirement(101, 15));
                    overlays.Add(new LevelOverlayRequirement(302, 18));
                    break;
                case 16:
                    targets.Add(new LevelTargetRequirement(102, 25));
                    targets.Add(new LevelTargetRequirement(103, 20));
                    break;
                case 17:
                    targets.Add(new LevelTargetRequirement(302, 10));
                    targets.Add(new LevelTargetRequirement(101, 20));
                    overlays.Add(new LevelOverlayRequirement(302, 10));
                    break;
                case 18:
                    targets.Add(new LevelTargetRequirement(301, 8)); // Ice overlay target
                    targets.Add(new LevelTargetRequirement(102, 20));
                    overlays.Add(new LevelOverlayRequirement(301, 8));
                    break;
                case 19:
                    targets.Add(new LevelTargetRequirement(301, 8));
                    targets.Add(new LevelTargetRequirement(302, 8));
                    overlays.Add(new LevelOverlayRequirement(301, 8));
                    overlays.Add(new LevelOverlayRequirement(302, 8));
                    break;
                case 20:
                    targets.Add(new LevelTargetRequirement(103, 30));
                    targets.Add(new LevelTargetRequirement(104, 30));
                    break;
                case 21:
                    targets.Add(new LevelTargetRequirement(301, 10));
                    targets.Add(new LevelTargetRequirement(302, 10));
                    overlays.Add(new LevelOverlayRequirement(301, 10));
                    overlays.Add(new LevelOverlayRequirement(302, 10));
                    break;
                case 22:
                    targets.Add(new LevelTargetRequirement(101, 30));
                    targets.Add(new LevelTargetRequirement(102, 20));
                    break;
                case 23:
                    targets.Add(new LevelTargetRequirement(301, 12));
                    targets.Add(new LevelTargetRequirement(103, 15));
                    overlays.Add(new LevelOverlayRequirement(301, 12));
                    break;
                case 24:
                    targets.Add(new LevelTargetRequirement(303, 4)); // Chocolate overlay target
                    targets.Add(new LevelTargetRequirement(104, 15));
                    overlays.Add(new LevelOverlayRequirement(303, 4));
                    break;
                case 25:
                    targets.Add(new LevelTargetRequirement(303, 8));
                    targets.Add(new LevelTargetRequirement(106, 15));
                    overlays.Add(new LevelOverlayRequirement(303, 8));
                    break;
                case 26:
                    targets.Add(new LevelTargetRequirement(303, 8));
                    targets.Add(new LevelTargetRequirement(301, 8));
                    overlays.Add(new LevelOverlayRequirement(303, 8));
                    overlays.Add(new LevelOverlayRequirement(301, 8));
                    break;
                case 27:
                    targets.Add(new LevelTargetRequirement(101, 30));
                    targets.Add(new LevelTargetRequirement(102, 30));
                    break;
                case 28:
                    targets.Add(new LevelTargetRequirement(103, 35));
                    targets.Add(new LevelTargetRequirement(104, 35));
                    break;
                case 29:
                    targets.Add(new LevelTargetRequirement(106, 25));
                    targets.Add(new LevelTargetRequirement(101, 25));
                    break;
                case 30:
                    targets.Add(new LevelTargetRequirement(301, 8));
                    targets.Add(new LevelTargetRequirement(102, 20));
                    overlays.Add(new LevelOverlayRequirement(301, 8));
                    break;
                case 31:
                    targets.Add(new LevelTargetRequirement(103, 25));
                    targets.Add(new LevelTargetRequirement(104, 25));
                    break;
                case 32:
                    targets.Add(new LevelTargetRequirement(302, 12));
                    targets.Add(new LevelTargetRequirement(101, 25));
                    overlays.Add(new LevelOverlayRequirement(302, 12));
                    break;
                case 33:
                    targets.Add(new LevelTargetRequirement(201, 2)); // Cake target
                    targets.Add(new LevelTargetRequirement(102, 25));
                    break;
                case 34:
                    targets.Add(new LevelTargetRequirement(201, 3));
                    targets.Add(new LevelTargetRequirement(103, 25));
                    break;
                case 35:
                    targets.Add(new LevelTargetRequirement(201, 3));
                    targets.Add(new LevelTargetRequirement(301, 8));
                    overlays.Add(new LevelOverlayRequirement(301, 8));
                    break;
                case 36:
                    targets.Add(new LevelTargetRequirement(201, 3));
                    targets.Add(new LevelTargetRequirement(303, 6));
                    overlays.Add(new LevelOverlayRequirement(303, 6));
                    break;
                default:
                    targets.Add(new LevelTargetRequirement(101, 20));
                    break;
            }

            // Create generation request
            LevelGenerationRequest request = new LevelGenerationRequest
            {
                levelId = levelId,
                displayLevelNumber = levelNum,
                width = width,
                height = height,
                movesLimit = moves,
                requireNoInitialMatches = true,
                requireAllAllowedSpawnableAppearAtLeastOnce = true,
                allowedSpawnableTileIds = allowedSpawnables,
                disallowedInitialTileIds = new List<int> { 151, 152, 153, 154, 155, 156, 201, 231, 301, 302, 303 },
                targets = targets,
                requiredOverlayPlacements = overlays
            };

            LevelGenerationService generator = new LevelGenerationService();
            LevelJsonExportService exporter = new LevelJsonExportService();

            var result = generator.Generate(request, database);
            if (!result.success)
            {
                Debug.LogError($"[LevelEditor] Base generation failed for {levelId}: {result.message}");
                return false;
            }

            var levelData = result.levelData;
            
            // Sync spawnableTileConfigs
            levelData.spawnableTileConfigs = new List<LevelSpawnableTileConfig>();
            foreach (var spawnableId in levelData.spawnableTileIds)
            {
                levelData.spawnableTileConfigs.Add(new LevelSpawnableTileConfig(spawnableId, 100, true));
            }
            
            // 4. Custom Level Modifications (Tut Setup / Blocker & Underlay Placement)
            CustomizeLevel(levelNum, levelData);

            // Re-validate post customization
            var validation = Match3LevelDataValidator.Validate(levelData, database);
            if (!validation.IsValid)
            {
                Debug.LogError($"[LevelEditor] Customized level {levelId} failed validation. Errors: {validation.Issues.Count}");
                foreach (var issue in validation.Issues)
                {
                    Debug.LogError($"[LevelEditor] {issue.Code}: {issue.Message}");
                }
                return false;
            }

            // Export to JSON
            var document = LevelEditorDocumentFactory.Create(levelData);
            if (!exporter.TryExport(document, levelId, true))
            {
                Debug.LogError($"[LevelEditor] Export failed for {levelId}");
                return false;
            }

            return true;
        }

        private static void CustomizeLevel(int levelNum, Match3LevelData levelData)
        {
            int w = levelData.width;
            int h = levelData.height;

            switch (levelNum)
            {
                case 1:
                    // Set up a clear first match on row 2 (y=2)
                    // We separate them with a 102 so it's not a match initially, but sliding 101 to the left completes a match
                    // Row 2 cells are: x=0..5. Index formula: y * width + x
                    // Make it: [101, 101, 102, 101, 103, 104]
                    levelData.tileLayout[2 * w + 0] = 101;
                    levelData.tileLayout[2 * w + 1] = 101;
                    levelData.tileLayout[2 * w + 2] = 102;
                    levelData.tileLayout[2 * w + 3] = 101;
                    levelData.tileLayout[2 * w + 4] = 103;
                    levelData.tileLayout[2 * w + 5] = 104;
                    break;
                case 4:
                    // Intro Cross Bomb. Setup a row (y=3) to easily match 4 normal tiles
                    // Make Row 3: [101, 101, 102, 101, 103, 104, 105, 106]
                    // And put 101 at Row 2, Col 2 (2,2) with neighbors that don't match it.
                    levelData.tileLayout[3 * w + 0] = 101;
                    levelData.tileLayout[3 * w + 1] = 101;
                    levelData.tileLayout[3 * w + 2] = 102;
                    levelData.tileLayout[3 * w + 3] = 101;
                    levelData.tileLayout[3 * w + 4] = 103;
                    levelData.tileLayout[3 * w + 5] = 104;
                    levelData.tileLayout[3 * w + 6] = 105;
                    levelData.tileLayout[3 * w + 7] = 106;

                    levelData.tileLayout[2 * w + 1] = 102;
                    levelData.tileLayout[2 * w + 2] = 101;
                    levelData.tileLayout[2 * w + 3] = 103;
                    break;
                case 5:
                    // Practice Cross Bomb. Start with a Cross Bomb (152) at (4,4)
                    levelData.tileLayout[4 * w + 4] = 152;
                    break;
                case 6:
                    // Intro Square Bomb. Setup a 2x2 prep at center-left
                    // (2,2)=102, (3,2)=102, (2,3)=102, (4,3)=102. Sliding x=4 left makes it a 2x2.
                    levelData.tileLayout[2 * w + 2] = 102;
                    levelData.tileLayout[2 * w + 3] = 102;
                    levelData.tileLayout[3 * w + 2] = 102;
                    levelData.tileLayout[3 * w + 4] = 102;
                    break;
                case 7:
                    // Practice combining Cross and Square Bomb
                    levelData.tileLayout[4 * w + 3] = 151; // Square Bomb
                    levelData.tileLayout[4 * w + 4] = 152; // Cross Bomb
                    break;
                case 9:
                    // Practice collection, start with two random boosters on board
                    levelData.tileLayout[3 * w + 3] = 151;
                    levelData.tileLayout[4 * w + 4] = 152;
                    break;
                case 10:
                    // Intro Area Bomb Med (T-shape match of 5)
                    // Setup: Row 3 has 101 at (0,3), (1,3), and 102 at (2,3)
                    // Col 2 has 101 at (2,2) and (2,1). sliding col 2 down creates it.
                    levelData.tileLayout[3 * w + 0] = 101;
                    levelData.tileLayout[3 * w + 1] = 101;
                    levelData.tileLayout[3 * w + 2] = 102;
                    
                    levelData.tileLayout[2 * w + 2] = 101;
                    levelData.tileLayout[1 * w + 2] = 101;
                    
                    // Prevent accidental vertical match in Col 2:
                    levelData.tileLayout[0 * w + 2] = 103;
                    
                    // Prevent accidental horizontal matches:
                    levelData.tileLayout[2 * w + 1] = 103;
                    levelData.tileLayout[2 * w + 3] = 104;
                    levelData.tileLayout[1 * w + 1] = 106;
                    levelData.tileLayout[1 * w + 3] = 107;
                    
                    // Prevent accidental vertical matches in Col 0 and Col 1:
                    levelData.tileLayout[2 * w + 0] = 103;
                    levelData.tileLayout[4 * w + 0] = 104;
                    levelData.tileLayout[4 * w + 1] = 106;
                    break;
                case 11:
                    // Practice Area Bomb Med. Place one at (4,4)
                    levelData.tileLayout[4 * w + 4] = 153;
                    break;
                case 16:
                    // Intro Hammer. Place one StoneTile (231) blocker at center (4,4)
                    levelData.tileLayout[4 * w + 4] = 231;
                    break;
                case 29:
                    // Intro Lock Cell. Underlay: Place 4 LockCells (401) at (2,2), (5,2), (2,5), (5,5)
                    levelData.underlayLayout[2 * w + 2] = 401;
                    levelData.underlayLayout[2 * w + 5] = 401;
                    levelData.underlayLayout[5 * w + 2] = 401;
                    levelData.underlayLayout[5 * w + 5] = 401;
                    break;
                case 30:
                    // Practice Lock Cell. Place 6 LockCells (401)
                    levelData.underlayLayout[1 * w + 3] = 401;
                    levelData.underlayLayout[2 * w + 4] = 401;
                    levelData.underlayLayout[3 * w + 1] = 401;
                    levelData.underlayLayout[3 * w + 6] = 401;
                    levelData.underlayLayout[4 * w + 3] = 401;
                    levelData.underlayLayout[5 * w + 4] = 401;
                    break;
                case 32:
                    // LockCell with overlays. Place 4 LockCells (401)
                    levelData.underlayLayout[2 * w + 2] = 401;
                    levelData.underlayLayout[2 * w + 5] = 401;
                    levelData.underlayLayout[5 * w + 2] = 401;
                    levelData.underlayLayout[5 * w + 5] = 401;
                    break;
                case 33:
                    // Intro Cake + Portal. ExitCells (402) at bottom y=7, x=2,3,4,5
                    // Place 2 Cakes (201) at top row y=0, x=3,4 (spaced out if possible, but 2 is fine and doesn't match-3)
                    levelData.underlayLayout[7 * w + 2] = 402;
                    levelData.underlayLayout[7 * w + 3] = 402;
                    levelData.underlayLayout[7 * w + 4] = 402;
                    levelData.underlayLayout[7 * w + 5] = 402;
                    levelData.tileLayout[0 * w + 3] = 201;
                    levelData.tileLayout[0 * w + 4] = 201;
                    break;
                case 34:
                    // Practice Cake. ExitCells at bottom y=7, x=1,2,3,4,5. 3 Cakes at top spaced out to avoid match-3
                    levelData.underlayLayout[7 * w + 1] = 402;
                    levelData.underlayLayout[7 * w + 2] = 402;
                    levelData.underlayLayout[7 * w + 3] = 402;
                    levelData.underlayLayout[7 * w + 4] = 402;
                    levelData.underlayLayout[7 * w + 5] = 402;
                    levelData.tileLayout[0 * w + 1] = 201;
                    levelData.tileLayout[0 * w + 3] = 201;
                    levelData.tileLayout[0 * w + 5] = 201;
                    break;
                case 35:
                    // Cake + Portal + Ice. ExitCells at bottom y=7, x=1,2,3,4,5. 3 Cakes at top spaced out.
                    levelData.underlayLayout[7 * w + 1] = 402;
                    levelData.underlayLayout[7 * w + 2] = 402;
                    levelData.underlayLayout[7 * w + 3] = 402;
                    levelData.underlayLayout[7 * w + 4] = 402;
                    levelData.underlayLayout[7 * w + 5] = 402;
                    levelData.tileLayout[0 * w + 1] = 201;
                    levelData.tileLayout[0 * w + 3] = 201;
                    levelData.tileLayout[0 * w + 5] = 201;
                    break;
                case 36:
                    // Advanced Level. ExitCells at bottom y=7, x=2,3,4,5. 3 Cakes at top spaced out.
                    // LockCells at (2,2), (2,5), (5,2), (5,5)
                    levelData.underlayLayout[7 * w + 2] = 402;
                    levelData.underlayLayout[7 * w + 3] = 402;
                    levelData.underlayLayout[7 * w + 4] = 402;
                    levelData.underlayLayout[7 * w + 5] = 402;
                    levelData.tileLayout[0 * w + 1] = 201;
                    levelData.tileLayout[0 * w + 3] = 201;
                    levelData.tileLayout[0 * w + 5] = 201;
                    levelData.underlayLayout[2 * w + 2] = 401;
                    levelData.underlayLayout[2 * w + 5] = 401;
                    levelData.underlayLayout[5 * w + 2] = 401;
                    levelData.underlayLayout[5 * w + 5] = 401;
                    break;
            }
        }
    }
}
#endif
