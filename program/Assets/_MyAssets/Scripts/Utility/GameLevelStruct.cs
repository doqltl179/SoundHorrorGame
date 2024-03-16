using System.Collections;
using System.Collections.Generic;
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
                    generateCount = 2,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Kitty,
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
                    generateCount = 3,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Froggy,
                    generateCount = 2,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Kitty,
                    generateCount = 3,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Cloudy,
                    generateCount = 3,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Starry,
                    generateCount = 3,
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