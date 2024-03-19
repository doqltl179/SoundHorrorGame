using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class GameLevelStruct {
    public static readonly GameLevelSettings[] GameLevels = new GameLevelSettings[] {
        new GameLevelSettings() {
            LevelWidth = 16,
            LevelHeight = 16,

            IsEmpty = false,

            Monsters = new GameLevelSettings.MonsterStruct[] {
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Bunny,
                    generateCount = 2,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Honey,
                    generateCount = 2,
                },
            },

            Items = new GameLevelSettings.ItemStruct[] {
                new GameLevelSettings.ItemStruct() {
                    type = LevelLoader.ItemType.Crystal,
                    generateCount = 3,
                },
            },

            HandlingCubeCount = 8,
            TeleportCount = 0,
        },
        new GameLevelSettings() {
            LevelWidth = 16,
            LevelHeight = 16,

            IsEmpty = false,

            Monsters = new GameLevelSettings.MonsterStruct[] {
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Bunny,
                    generateCount = 2,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Honey,
                    generateCount = 2,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Froggy,
                    generateCount = 1,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Kitty,
                    generateCount = 1,
                },
            },

            Items = new GameLevelSettings.ItemStruct[] {
                new GameLevelSettings.ItemStruct() {
                    type = LevelLoader.ItemType.Crystal,
                    generateCount = 3,
                },
            },

            HandlingCubeCount = 8,
            TeleportCount = 2,
        },
        new GameLevelSettings() {
            LevelWidth = 16,
            LevelHeight = 16,

            IsEmpty = false,

            Monsters = new GameLevelSettings.MonsterStruct[] {
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Bunny,
                    generateCount = 2,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Honey,
                    generateCount = 2,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Froggy,
                    generateCount = 1,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Kitty,
                    generateCount = 1,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Cloudy,
                    generateCount = 1,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Starry,
                    generateCount = 1,
                },
            },

            Items = new GameLevelSettings.ItemStruct[] {
                new GameLevelSettings.ItemStruct() {
                    type = LevelLoader.ItemType.Crystal,
                    generateCount = 3,
                },
            },

            HandlingCubeCount = 8,
            TeleportCount = 4,
        },
        new GameLevelSettings() {
            LevelWidth = 16,
            LevelHeight = 16,

            IsEmpty = false,

            Monsters = new GameLevelSettings.MonsterStruct[] {
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Bunny,
                    generateCount = 2,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Honey,
                    generateCount = 2,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Froggy,
                    generateCount = 2,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Kitty,
                    generateCount = 1,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Cloudy,
                    generateCount = 2,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Starry,
                    generateCount = 1,
                },
            },

            Items = new GameLevelSettings.ItemStruct[] {
                new GameLevelSettings.ItemStruct() {
                    type = LevelLoader.ItemType.Crystal,
                    generateCount = 3,
                },
            },

            HandlingCubeCount = 8,
            TeleportCount = 4,
        },
        //new GameLevelSettings() {
        //    LevelWidth = 16,
        //    LevelHeight = 16,

        //    IsEmpty = false,

        //    Monsters = new GameLevelSettings.MonsterStruct[] {
        //        new GameLevelSettings.MonsterStruct() {
        //            type = LevelLoader.MonsterType.Kitty,
        //            generateCount = 1,
        //        },
        //    },

        //    Items = new GameLevelSettings.ItemStruct[] {
        //        new GameLevelSettings.ItemStruct() {
        //            type = LevelLoader.ItemType.Crystal,
        //            generateCount = 3,
        //        },
        //    },

        //    HandlingCubeCount = 8,
        //    TeleportCount = 4,
        //},
    };

    #region Utility
    public static int GetAllGenerateItemCount(int levelIndex) {
        if(levelIndex < 0 || levelIndex >= GameLevels.Length) return 0;

        return GameLevels[levelIndex].Items != null ? GameLevels[levelIndex].Items.Sum(t => t.generateCount) : 0;
    }
    #endregion
}

public class GameLevelSettings {
    public int LevelWidth;
    public int LevelHeight;

    public bool IsEmpty;

    public struct MonsterStruct {
        public LevelLoader.MonsterType type;
        public int generateCount;
    }
    public MonsterStruct[] Monsters;

    public struct ItemStruct {
        public LevelLoader.ItemType type;
        public int generateCount;
    }
    public ItemStruct[] Items;

    public int HandlingCubeCount;
    public int TeleportCount;
}