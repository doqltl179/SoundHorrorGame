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
    /// 외벽만 있는 미로를 생성
    /// </summary>
    public static void CreateEmptyMaze(int width, int height) {
        if(width * height < 2) {
            Debug.LogError("Maze size not enough.");
            return;
        }

        ActiveWall[,] wallInfos = new ActiveWall[width, height];
        // 중앙에 있는 모든 벽 제거
        for(int x = 1; x < width - 1; x++) {
            for(int y = 1; y < height - 1; y++) {
                wallInfos[x, y] = ActiveWall.None;
            }
        }
        // 외곽 Edge 설정
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
        // 외곽 모서리 설정
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
        // maze 배열 초기화
        for(int x = 0; x < width; x++) {
            for(int y = 0; y < height; y++) {
                wallInfos[x, y] = ActiveWall.All;
            }
        }

        // currentPoint로 부터 이동 가능한 point를 반환
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

        // 이미 생성된 미로 안에서 랜덤하게 벽을 제거
        // 이동 가능한 방향을 늘리기 위함
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
                else if(activatedWallList.Count >= 2 && Random.Range(0, 4) == 0) {
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

        // 각 방향별로 직선 이동이 가능한 좌표 확인
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
                    int tempX = x;
                    while(true) {
                        tempX++;
                        if(tempX >= width) {
                            tempX--;

                            break;
                        }

                        tempInfo = wallInfos[tempX, y];
                        if(!tempInfo.HasFlag(ActiveWall.F) || !tempInfo.HasFlag(ActiveWall.B) || tempInfo.HasFlag(ActiveWall.R)) {
                            break;
                        }
                    }

                    nextCoord_R = new Vector2Int(tempX, y);
                }
                if(!currentInfo.HasFlag(ActiveWall.F)) {
                    int tempY = y;
                    while(true) {
                        tempY++;
                        if(tempY >= height) {
                            tempY--;

                            break;
                        }

                        tempInfo = wallInfos[x, tempY];
                        if(!tempInfo.HasFlag(ActiveWall.R) || !tempInfo.HasFlag(ActiveWall.L) || tempInfo.HasFlag(ActiveWall.F)) {
                            break;
                        }
                    }

                    nextCoord_F = new Vector2Int(x, tempY);
                }
                if(!currentInfo.HasFlag(ActiveWall.L)) {
                    int tempX = x;
                    while(true) {
                        tempX--;
                        if(tempX < 0) {
                            tempX++;

                            break;
                        }

                        tempInfo = wallInfos[tempX, y];
                        if(!tempInfo.HasFlag(ActiveWall.F) || !tempInfo.HasFlag(ActiveWall.B) || tempInfo.HasFlag(ActiveWall.L)) {
                            break;
                        }
                    }

                    nextCoord_L = new Vector2Int(tempX, y);
                }
                if(!currentInfo.HasFlag(ActiveWall.B)) {
                    int tempY = y;
                    while(true) {
                        tempY--;
                        if(tempY < 0) {
                            tempY++;

                            break;
                        }

                        tempInfo = wallInfos[x, tempY];
                        if(!tempInfo.HasFlag(ActiveWall.R) || !tempInfo.HasFlag(ActiveWall.L) || tempInfo.HasFlag(ActiveWall.B)) {
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
