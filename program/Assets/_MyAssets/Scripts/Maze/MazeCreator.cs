using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    public static ActiveWall[,] Maze { get; private set; }


    
    /// <summary>
    /// 외벽만 있는 미로를 생성
    /// </summary>
    public static void CreateEmptyMaze(int width, int height) {
        if(width * height < 2) {
            Debug.LogError("Maze size not enough.");
            return;
        }

        Maze = new ActiveWall[width, height];
        // 중앙에 있는 모든 벽 제거
        for(int x = 1; x < width - 1; x++) {
            for(int y = 1; y < height - 1; y++) {
                Maze[x, y] = ActiveWall.None;
            }
        }
        // 외곽 Edge 설정
        for(int x = 1; x < width - 1; x++) {
            Maze[x, 0] = ActiveWall.B;
        }
        for(int x = 1; x < width - 1; x++) {
            Maze[x, height - 1] = ActiveWall.F;
        }
        for(int y = 1; y < height - 1; y++) {
            Maze[0, y] = ActiveWall.L;
        }
        for(int y = 1; y < height - 1; y++) {
            Maze[width - 1, y] = ActiveWall.R;
        }
        // 외곽 모서리 설정
        Maze[0, 0] = ActiveWall.L | ActiveWall.B;
        Maze[width - 1, 0] = ActiveWall.R | ActiveWall.B;
        Maze[0, height - 1] = ActiveWall.L | ActiveWall.F;
        Maze[width - 1, height - 1] = ActiveWall.R | ActiveWall.F;
    }

    public static void CreateMaze(int width, int height) {
        if(width * height < 2) {
            Debug.LogError("Maze size not enough.");
            return;
        }

        Maze = new ActiveWall[width, height];
        // maze 배열 초기화
        for(int x = 0; x < width; x++) {
            for(int y = 0; y < height; y++) {
                Maze[x, y] = ActiveWall.All;
            }
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
                        Maze[currentPoint.x, currentPoint.y] &= ~ActiveWall.R;
                        Maze[nextPoint.x, nextPoint.y] &= ~ActiveWall.L;
                        break;
                    case ActiveWall.F: 
                        nextPoint = new Vector2Int(currentPoint.x, currentPoint.y + 1);
                        Maze[currentPoint.x, currentPoint.y] &= ~ActiveWall.F;
                        Maze[nextPoint.x, nextPoint.y] &= ~ActiveWall.B;
                        break;
                    case ActiveWall.L: 
                        nextPoint = new Vector2Int(currentPoint.x - 1, currentPoint.y);
                        Maze[currentPoint.x, currentPoint.y] &= ~ActiveWall.L;
                        Maze[nextPoint.x, nextPoint.y] &= ~ActiveWall.R;
                        break;
                    case ActiveWall.B: 
                        nextPoint = new Vector2Int(currentPoint.x, currentPoint.y - 1);
                        Maze[currentPoint.x, currentPoint.y] &= ~ActiveWall.B;
                        Maze[nextPoint.x, nextPoint.y] &= ~ActiveWall.F;
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

                tempWallInfo = Maze[x, y];
                if(tempWallInfo.HasFlag(ActiveWall.R)) activatedWallList.Add(ActiveWall.R);
                if(tempWallInfo.HasFlag(ActiveWall.F)) activatedWallList.Add(ActiveWall.F);
                if(tempWallInfo.HasFlag(ActiveWall.L)) activatedWallList.Add(ActiveWall.L);
                if(tempWallInfo.HasFlag(ActiveWall.B)) activatedWallList.Add(ActiveWall.B);

                if(activatedWallList.Count > 2) {
                    removeWall = activatedWallList[Random.Range(0, activatedWallList.Count)];
                    Maze[x, y] &= ~removeWall;
                    switch(removeWall) {
                        case ActiveWall.R: Maze[x + 1, y] &= ~ActiveWall.L; break;
                        case ActiveWall.F: Maze[x, y + 1] &= ~ActiveWall.B; break;
                        case ActiveWall.L: Maze[x - 1, y] &= ~ActiveWall.R; break;
                        case ActiveWall.B: Maze[x, y - 1] &= ~ActiveWall.F; break;
                    }
                }
            }
        }
    }

    // currentPoint로 부터 이동 가능한 point를 반환
    private static List<ActiveWall> GetMoveDirectionList(Vector2Int currentPoint, int mazeWidth, int mazeHeight) {
        List<ActiveWall> moveDirectionList = new List<ActiveWall>();

        Vector2Int r = new Vector2Int(currentPoint.x + 1, currentPoint.y);
        if(r.x < mazeWidth && Maze[r.x, r.y] == ActiveWall.All) {
            moveDirectionList.Add(ActiveWall.R);
        }

        Vector2Int f = new Vector2Int(currentPoint.x, currentPoint.y + 1);
        if(f.y < mazeHeight && Maze[f.x, f.y] == ActiveWall.All) {
            moveDirectionList.Add(ActiveWall.F);
        }

        Vector2Int l = new Vector2Int(currentPoint.x - 1, currentPoint.y);
        if(l.x >= 0 && Maze[l.x, l.y] == ActiveWall.All) {
            moveDirectionList.Add(ActiveWall.L);
        }

        Vector2Int b = new Vector2Int(currentPoint.x, currentPoint.y - 1);
        if(b.y >= 0 && Maze[b.x, b.y] == ActiveWall.All) {
            moveDirectionList.Add(ActiveWall.B);
        }

        return moveDirectionList;
    }
}
