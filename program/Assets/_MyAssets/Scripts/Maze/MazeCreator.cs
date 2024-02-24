using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

using Random = UnityEngine.Random;

public static class MazeCreator {
    [Flags]
    public enum ActiveWall {
        None = 0,

        R = 1 << 0, //Right
        F = 1 << 1, //Forward
        L = 1 << 2, //Left
        B = 1 << 3, //Back

        All = int.MaxValue
    }

    public static MazeInfo[,] Maze { get; private set; }


    
    /// <summary>
    /// �ܺ��� �ִ� �̷θ� ����
    /// </summary>
    public static void CreateEmptyMaze(int width, int height) {
        if(width * height < 2) {
            Debug.LogError("Maze size not enough.");
            return;
        }

        ActiveWall[,] wallInfos = new ActiveWall[width, height];
        // �߾ӿ� �ִ� ��� �� ����
        for(int x = 1; x < width - 1; x++) {
            for(int y = 1; y < height - 1; y++) {
                wallInfos[x, y] = ActiveWall.None;
            }
        }
        // �ܰ� Edge ����
        for(int x = 1; x < width - 1; x++) {
            wallInfos[x, 0] = ActiveWall.B;
        }
        for(int x = 1; x < width - 1; x++) {
            wallInfos[x, height - 1] = ActiveWall.F;
        }
        for(int y = 1; y < height - 1; y++) {
            wallInfos[0, y] = ActiveWall.L;
        }
        for(int y = 1; y < height - 1; y++) {
            wallInfos[width - 1, y] = ActiveWall.R;
        }
        // �ܰ� �𼭸� ����
        wallInfos[0, 0] = ActiveWall.L | ActiveWall.B;
        wallInfos[width - 1, 0] = ActiveWall.R | ActiveWall.B;
        wallInfos[0, height - 1] = ActiveWall.L | ActiveWall.F;
        wallInfos[width - 1, height - 1] = ActiveWall.R | ActiveWall.F;

        SetMaze(wallInfos);
    }

    public static void CreateMaze(int width, int height) {
        if(width * height < 2) {
            Debug.LogError("Maze size not enough.");
            return;
        }

        ActiveWall[,] wallInfos = new ActiveWall[width, height];
        // maze �迭 �ʱ�ȭ
        for(int x = 0; x < width; x++) {
            for(int y = 0; y < height; y++) {
                wallInfos[x, y] = ActiveWall.All;
            }
        }

        // currentPoint�� ���� �̵� ������ point�� ��ȯ
        List<ActiveWall> GetMoveDirectionList(Vector2Int currentPoint, int mazeWidth, int mazeHeight) {
            List<ActiveWall> moveDirectionList = new List<ActiveWall>();

            Vector2Int r = new Vector2Int(currentPoint.x + 1, currentPoint.y);
            if(r.x < mazeWidth && wallInfos[r.x, r.y] == ActiveWall.All) {
                moveDirectionList.Add(ActiveWall.R);
            }

            Vector2Int f = new Vector2Int(currentPoint.x, currentPoint.y + 1);
            if(f.y < mazeHeight && wallInfos[f.x, f.y] == ActiveWall.All) {
                moveDirectionList.Add(ActiveWall.F);
            }

            Vector2Int l = new Vector2Int(currentPoint.x - 1, currentPoint.y);
            if(l.x >= 0 && wallInfos[l.x, l.y] == ActiveWall.All) {
                moveDirectionList.Add(ActiveWall.L);
            }

            Vector2Int b = new Vector2Int(currentPoint.x, currentPoint.y - 1);
            if(b.y >= 0 && wallInfos[b.x, b.y] == ActiveWall.All) {
                moveDirectionList.Add(ActiveWall.B);
            }

            return moveDirectionList;
        }
        Vector2Int currentPoint = new Vector2Int(Random.Range(0, width), Random.Range(0, height));
        Vector2Int nextPoint;

        List<Vector2Int> pathRecorder = new List<Vector2Int>();
        pathRecorder.Add(currentPoint);

        while(true) {
            List<ActiveWall> moveDirectionList = GetMoveDirectionList(currentPoint, width, height);
            if(moveDirectionList.Count > 0) {
                int randomIndex = Random.Range(0, moveDirectionList.Count);
                switch(moveDirectionList[randomIndex]) {
                    case ActiveWall.R: 
                        nextPoint = new Vector2Int(currentPoint.x + 1, currentPoint.y);
                        wallInfos[currentPoint.x, currentPoint.y] &= ~ActiveWall.R;
                        wallInfos[nextPoint.x, nextPoint.y] &= ~ActiveWall.L;
                        break;
                    case ActiveWall.F: 
                        nextPoint = new Vector2Int(currentPoint.x, currentPoint.y + 1);
                        wallInfos[currentPoint.x, currentPoint.y] &= ~ActiveWall.F;
                        wallInfos[nextPoint.x, nextPoint.y] &= ~ActiveWall.B;
                        break;
                    case ActiveWall.L: 
                        nextPoint = new Vector2Int(currentPoint.x - 1, currentPoint.y);
                        wallInfos[currentPoint.x, currentPoint.y] &= ~ActiveWall.L;
                        wallInfos[nextPoint.x, nextPoint.y] &= ~ActiveWall.R;
                        break;
                    case ActiveWall.B: 
                        nextPoint = new Vector2Int(currentPoint.x, currentPoint.y - 1);
                        wallInfos[currentPoint.x, currentPoint.y] &= ~ActiveWall.B;
                        wallInfos[nextPoint.x, nextPoint.y] &= ~ActiveWall.F;
                        break;
                    default:
                        nextPoint = Vector2Int.zero;
                        Debug.LogError("Direction not added correctly.");
                        break;
                }

                pathRecorder.Add(nextPoint);
                currentPoint = nextPoint;
            }
            else {
                bool isBreak = true;
                for(int i = pathRecorder.Count - 2; i >= 0; i--) {
                    if(GetMoveDirectionList(pathRecorder[i], width, height).Count > 0) {
                        pathRecorder.RemoveRange(i + 1, pathRecorder.Count - (i + 1));
                        currentPoint = pathRecorder[i];

                        isBreak = false;
                        break;
                    }
                }
                if(isBreak) {
                    break;
                }
            }
        }

        // �̹� ������ �̷� �ȿ��� �����ϰ� ���� ����
        // �̵� ������ ������ �ø��� ����
        List<ActiveWall> activatedWallList = new List<ActiveWall>();
        ActiveWall tempWallInfo;
        ActiveWall removeWall;
        for(int x = 1; x < width - 1; x++) {
            for(int y = 1; y < height - 1; y++) {
                activatedWallList.Clear();

                tempWallInfo = wallInfos[x, y];
                if(tempWallInfo.HasFlag(ActiveWall.R)) activatedWallList.Add(ActiveWall.R);
                if(tempWallInfo.HasFlag(ActiveWall.F)) activatedWallList.Add(ActiveWall.F);
                if(tempWallInfo.HasFlag(ActiveWall.L)) activatedWallList.Add(ActiveWall.L);
                if(tempWallInfo.HasFlag(ActiveWall.B)) activatedWallList.Add(ActiveWall.B);

                if(activatedWallList.Count > 2) {
                    removeWall = activatedWallList[Random.Range(0, activatedWallList.Count)];
                    wallInfos[x, y] &= ~removeWall;
                    switch(removeWall) {
                        case ActiveWall.R: wallInfos[x + 1, y] &= ~ActiveWall.L; break;
                        case ActiveWall.F: wallInfos[x, y + 1] &= ~ActiveWall.B; break;
                        case ActiveWall.L: wallInfos[x - 1, y] &= ~ActiveWall.R; break;
                        case ActiveWall.B: wallInfos[x, y - 1] &= ~ActiveWall.F; break;
                    }
                }
            }
        }

        SetMaze(wallInfos);
    }

    private static void SetMaze(ActiveWall[,] wallInfos) {
        int width = wallInfos.GetLength(0);
        int height = wallInfos.GetLength(1);

        // �� ���⺰�� ���� �̵��� ������ ��ǥ Ȯ��
        Maze = new MazeInfo[width, height];
        ActiveWall currentInfo;
        ActiveWall tempInfo;
        Vector2Int nextCoord_R;
        Vector2Int nextCoord_F;
        Vector2Int nextCoord_L;
        Vector2Int nextCoord_B;
        for(int x = 0; x < width; x++) {
            for(int y = 0; y < height; y++) {
                currentInfo = wallInfos[x, y];

                nextCoord_R = new Vector2Int(x, y);
                nextCoord_F = new Vector2Int(x, y);
                nextCoord_L = new Vector2Int(x, y);
                nextCoord_B = new Vector2Int(x, y);

                if(!currentInfo.HasFlag(ActiveWall.R)) {
                    int tempX = x + 1;
                    while(true) {
                        tempInfo = wallInfos[tempX, y];
                        if(!tempInfo.HasFlag(ActiveWall.F) || !tempInfo.HasFlag(ActiveWall.B)) {
                            break;
                        }

                        tempX++;
                        if(tempX >= width) {
                            tempX--;

                            break;
                        }
                    }

                    nextCoord_R = new Vector2Int(tempX, y);
                }
                if(!currentInfo.HasFlag(ActiveWall.F)) {
                    int tempY = y + 1;
                    while(true) {
                        tempInfo = wallInfos[x, tempY];
                        if(!tempInfo.HasFlag(ActiveWall.R) || !tempInfo.HasFlag(ActiveWall.L)) {
                            break;
                        }

                        tempY++;
                        if(tempY >= height) {
                            tempY--;

                            break;
                        }
                    }

                    nextCoord_F = new Vector2Int(x, tempY);
                }
                if(!currentInfo.HasFlag(ActiveWall.L)) {
                    int tempX = x - 1;
                    while(true) {
                        tempInfo = wallInfos[tempX, y];
                        if(!tempInfo.HasFlag(ActiveWall.F) || !tempInfo.HasFlag(ActiveWall.B)) {
                            break;
                        }

                        tempX--;
                        if(tempX < 0) {
                            tempX++;

                            break;
                        }
                    }

                    nextCoord_L = new Vector2Int(tempX, y);
                }
                if(!currentInfo.HasFlag(ActiveWall.B)) {
                    int tempY = y - 1;
                    while(true) {
                        tempInfo = wallInfos[x, tempY];
                        if(!tempInfo.HasFlag(ActiveWall.R) || !tempInfo.HasFlag(ActiveWall.L)) {
                            break;
                        }

                        tempY--;
                        if(tempY < 0) {
                            tempY++;

                            break;
                        }
                    }

                    nextCoord_B = new Vector2Int(x, tempY);
                }

                Maze[x, y] = new MazeInfo(currentInfo, new Vector2Int(x, y), nextCoord_R, nextCoord_F, nextCoord_L, nextCoord_B);
            }
        }
    }
}

public class MazeInfo {
    public MazeCreator.ActiveWall WallInfo { get; private set; }
    public Vector2Int CurrentCoord { get; private set; }

    public Vector2Int NextCrossLoadCoord_R { get; private set; }
    public Vector2Int NextCrossLoadCoord_F { get; private set; }
    public Vector2Int NextCrossLoadCoord_L { get; private set; }
    public Vector2Int NextCrossLoadCoord_B { get; private set; }

    public MazeInfo(
        MazeCreator.ActiveWall wallInfo, 
        Vector2Int coord,
        Vector2Int nextCoord_R,
        Vector2Int nextCoord_F,
        Vector2Int nextCoord_L,
        Vector2Int nextCoord_B) {
        WallInfo = wallInfo;
        CurrentCoord = coord;
        NextCrossLoadCoord_R = nextCoord_R;
        NextCrossLoadCoord_F = nextCoord_F;
        NextCrossLoadCoord_L = nextCoord_L;
        NextCrossLoadCoord_B = nextCoord_B;
    }
}
