using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

//Created by Caroline Manjerovic 12-8-24

//Contains the list of which tiles are able to be placed next to which other tiles, as well as the weights for how commonly each tile is placed.
//The rules are stored as a matrix containing four entries for each row (up, down, left, right)
//Only like edge types can be placed next to one another.
//The edge types are:
    //0 = ground
    //1 = path
    //2 = wall
    //3 = cliff edge up
    //4 = cliff edge down
    //5 = cliff edge left
    //6 = cliff edge right
     

public class TileRuleList
{
    int NUM_TILES;
    //labels which edges can go next to one another
    private int[,] ruleSet;
    //labels which edges CANNOT go next to one another. Will always override ruleSet when applicable.
    //tiles can only go next to each other if they share labels in ruleSet AND don't share labels in ruleSetBlackList
    private int[,] ruleSetBlackList;
    //labels how common each tile is
    private List<int> tileWeights = new List<int>();

    public TileRuleList(int nt)
    {
        NUM_TILES = nt;
        ruleSet = new int[NUM_TILES, 4];
        ruleSetBlackList = new int[NUM_TILES, 4];
        tileWeights = new List<int> {
            4, //cliffCornerUpLeft
            4, //cliffCornerUpRight
            4, //cliffCornerDownLeft
            4, //cliffCornerDownRight
            40, //cliffUp
            40, //cliffDown
            40, //cliffLeft
            40, //cliffRight
            25, //flowers1
            25, //flowers2
            100, //ground
            1, //pathUpLeft
            1, //pathUpRight
            1, //pathDownLeft
            1, //pathDownRight
            1, //pathTUp
            1, //pathTDown
            1, //pathTLeft
            1, //pathTRight
            40, //pathHorizontal
            40, //pathVertical
            0, //pathIntersection
            4, //tower
            2, //towerWallHorizontal
            2, //towerWallVertical
            10,
            10, //trees
            14,
            14,
            2, //wallCornerUpLeft
            2, //wallCornerUpRight
            2, //wallCornerDownLeft
            2, //wallCornerDownRight
            2, //wallCrumbleUp
            2, //wallCrumbleDown
            2, //wallCrumbleLeft
            2, //wallCrumbleRight
            20, //wallHorizontal
            20, //wallVertical
        };

        //0 = ground
        //1 = path
        //2 = wall
        //3 = cliff edge up
        //4 = cliff edge down
        //5 = cliff edge left
        //6 = cliff edge right
        ruleSet = new int[,] {
            {0, 5, 0, 3}, //cliffCornerUpLeft
            {0, 6, 3, 0}, //cliffCornerUpRight
            {5, 0, 0, 4}, //cliffCornerDownLeft
            {6, 0, 4, 0}, //cliffCornerDownRight
            {0, 0, 3, 3}, //cliffUp
            {0, 0, 4, 4}, //cliffDown
            {5, 5, 0, 0}, //cliffLeft
            {6, 6, 0, 0}, //cliffRight
            {0, 0, 0, 0}, //flowers1
            {0, 0, 0, 0}, //flowers2
            {0, 0, 0, 0}, //ground
            {1, 0, 1, 0}, //pathUpLeft
            {1, 0, 0, 1}, //pathUpRight
            {0, 1, 1, 0}, //pathDownLeft
            {0, 1, 0, 1}, //pathDownRight
            {1, 0, 1, 1}, //pathTUp
            {0, 1, 1, 1}, //pathTDown
            {1, 1, 1, 0}, //pathTLeft
            {1, 1, 0, 1}, //pathTRight
            {0, 0, 1, 1}, //pathHorizontal
            {1, 1, 0, 0}, //pathVertical
            {1, 1, 1, 1}, //pathIntersection
            {0, 0, 0, 0}, //tower
            {0, 0, 2, 2}, //towerWallHorizontal
            {2, 2, 0, 0}, //towerWallVertical
            {0, 0, 0, 0},
            {0, 0, 0, 0}, //trees
            {0, 0, 0, 0},
            {0, 0, 0, 0},
            {2, 0, 2, 0}, //wallCornerUpLeft
            {2, 0, 0, 2}, //wallCornerUpRight
            {0, 2, 2, 0}, //wallCornerDownLeft
            {0, 2, 0, 2}, //wallCornerDownRight
            {0, 2, 0, 0}, //wallCrumbleUp
            {2, 0, 0, 0}, //wallCrumbleDown
            {0, 0, 0, 2}, //wallCrumbleLeft
            {0, 0, 2, 0}, //wallCrumbleRight
            {0, 0, 2, 2}, //wallHorizontal
            {2, 2, 0, 0}, //wallVertical
        };

        //0: ignore
        //1: things that are too big to be adjacent (trees, towers)
        //2: path edges
        //3: wall and cliff edges
        ruleSetBlackList = new int[,] {
            {3, 0, 3, 0}, //cliffCornerUpLeft
            {3, 0, 0, 3}, //cliffCornerUpRight
            {0, 3, 3, 0}, //cliffCornerDownLeft
            {0, 3, 0, 3}, //cliffCornerDownRight
            {3, 0, 0, 0}, //cliffUp
            {0, 3, 0, 0}, //cliffDown
            {0, 0, 3, 0}, //cliffLeft
            {0, 0, 0, 3}, //cliffRight
            {0, 0, 0, 0}, //flowers1
            {0, 0, 0, 0}, //flowers2
            {0, 0, 0, 0}, //ground
            {0, 2, 0, 2}, //pathUpLeft
            {0, 2, 2, 0}, //pathUpRight
            {2, 0, 0, 2}, //pathDownLeft
            {2, 0, 2, 0}, //pathDownRight
            {0, 2, 0, 0}, //pathTUp
            {2, 0, 0, 0}, //pathTDown
            {0, 0, 0, 2}, //pathTLeft
            {0, 0, 2, 0}, //pathTRight
            {2, 2, 0, 0}, //pathHorizontal
            {0, 0, 2, 2}, //pathVertical
            {0, 0, 0, 0}, //pathIntersection
            {1, 1, 1, 1}, //tower
            {1, 1, 1, 1}, //towerWallHorizontal
            {1, 1, 1, 1}, //towerWallVertical
            {1, 1, 1, 1},
            {1, 1, 1, 1}, //trees
            {0, 0, 0, 0},
            {0, 0, 0, 0},
            {0, 3, 0, 3}, //wallCornerUpLeft
            {0, 3, 3, 0}, //wallCornerUpRight
            {3, 0, 0, 3}, //wallCornerDownLeft
            {3, 0, 3, 0}, //wallCornerDownRight
            {3, 0, 3, 3}, //wallCrumbleUp
            {0, 3, 3, 3}, //wallCrumbleDown
            {3, 3, 3, 0}, //wallCrumbleLeft
            {3, 3, 0, 3}, //wallCrumbleRight
            {3, 3, 0, 0}, //wallHorizontal
            {0, 0, 3, 3}, //wallVertical
        };
    }

    public int[,] getRuleSet()
    { 
        return ruleSet;
    }

    public int[,] getRuleSetBlackList()
    {
        return ruleSetBlackList;
    }

    public List<int> getWeights()
    {
        return tileWeights;
    }
}


