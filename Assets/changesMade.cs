using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

//Created by Caroline Manjerovic 12-7-24

//stores the state of the map at each step so that it may be reverted to later. cntains get functions for said information.
//clones any objects requested to prevent pass by reference errors.

public class changesMade
{
    Tile[,] mapTileInfo; //the state of the map at this step
    GameObject tileJustPlacedPrefab; //the gameobject of the tile just placed
    int numTimesReverted; //the number of times this state has been reverted to
    int MAP_SIZE; //size of the map
    List<(int index, int x, int y)> tilesAlreadyTried = new List<(int, int , int)>(); //a list of tile that have already been tried on the NEXT step. contains the tile index and location in each entry.

    public changesMade(Tile[,] newmti, int ms, GameObject tjpp)
    {
        MAP_SIZE = ms;
        mapTileInfo = new Tile[MAP_SIZE, MAP_SIZE];

        //need to create clones of every tile in mapTileInfo
        for (int i = 0; i < MAP_SIZE; i++)
        {
            for (int j = 0; j < MAP_SIZE; j++)
            {
                mapTileInfo[i, j] = newmti[i, j].clone();
            }
        }

        tileJustPlacedPrefab = tjpp;
        numTimesReverted = 0;
    }

    public Tile[,] getMapTileInfo () {
        //return a clone
        Tile[,] mapTileInfoCopy = new Tile[MAP_SIZE, MAP_SIZE];
        for (int i = 0; i < MAP_SIZE; i++)
        {
            for (int j = 0; j < MAP_SIZE; j++)
            {
                mapTileInfoCopy[i, j] = mapTileInfo[i, j].clone();
            }
        }
        return mapTileInfoCopy;
    }

    public int getNumTimesReverted () { return numTimesReverted; }

    //just marks this state as having been reverted to
    public void revertToThisState () { numTimesReverted++; }

    //if a tile has been tried going into the next step, and we ended up back here, mark it so we don't try it again
    public void tryTile(int tileIndex, int x, int y) { tilesAlreadyTried.Add((tileIndex, x, y)); }

    public List<(int, int, int)> getTilesAlreadyTried () {  return tilesAlreadyTried; }

    public GameObject getGameObject() { return tileJustPlacedPrefab; }
}
