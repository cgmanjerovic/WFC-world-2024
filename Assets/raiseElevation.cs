using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

//Created By Caroline Manjerovic 12-8-24

//contains methods used to elevate the tiles to match the locations of cliff edges, which are called in waveFunctionCollapse.
//seperated into its own class for readability.

public class raiseElevation
{
    Tile[,] mapTileInfo;
    TileRuleList ruleSet;
    int MAP_SIZE;

    //stores the directions of the backs of the cliffs
    List<(int x, int y)> cliffBacks;

    public raiseElevation(Tile[,] mpt, TileRuleList rs, int ms) {
        mapTileInfo = mpt;
        ruleSet = rs;
        MAP_SIZE = ms;

        cliffBacks = new List<(int x, int y)> {
            (0, 1), //cliffUp
            (0, -1), //cliffDown
            (-1, 0), //cliffLeft
            (1, 0) //cliffRight
        };
    }

    //returns a list of the cliffline containing the tile at (x, y)---------------------------------------------------------------------------------
    public List<(int, int)> findCliffLine(int x, int y)
    {
        List<(int x, int y)> cliffLine = new List<(int, int)>();
        cliffLine.Add((x, y));

        //continue until we reach the end of the list. We will keep adding cliff edges to the end list within this loop
        for (int i = 0; i < cliffLine.Count; i++)
        {
            //mark the tile as having been touched
            mapTileInfo[cliffLine[i].x, cliffLine[i].y].raise();

            //                                                   cliffLine[i] is a coordinate pair
            //                                                   get the object from mapTileInfo located at cliffLine[i]
            //  get the directions from that tile                and get the directions from that
            List<(int x, int y)> directions = mapTileInfo[cliffLine[i].x, cliffLine[i].y].getDirections(MAP_SIZE);

            //for each direction in directions
            for (int j = 0; j < directions.Count; j++)
            {
                //get the coordinates and tile object of the neighbor in current direction
                (int x, int y) neighbor = (cliffLine[i].x + directions[j].x, cliffLine[i].y + directions[j].y);
                Tile neighborTileInfo = mapTileInfo[neighbor.x, neighbor.y];

                //find the direction of the neighbor relative to the current tile (located at cliffLine[i]) in order to check the edge type
                int edgeDirection = 1;
                if (directions[j] == (0, 1)) edgeDirection = 0; //if its above, we want the bottom edge
                else if (directions[j] == (1, 0)) edgeDirection = 3; //etc.
                else if (directions[j] == (-1, 0)) edgeDirection = 2;

                if ((neighborTileInfo.getChosenTile() <= 7) && //if tile is a cliff
                    ruleSet.getRuleSet()[neighborTileInfo.getChosenTile(), edgeDirection] >= 3 && //and the edge facing the current tile is the side of a cliff
                    !neighborTileInfo.getHasBeenRaised()) //and it hasn't been touched already
                {
                    cliffLine.Add((neighbor.x, neighbor.y));
                }
            }

        }

        //return the list
        return cliffLine;
    }

    //Given a cliffline, will traverse inward and touch all tiles within that cliffline, raising their level-----------------------------------------
    public void raiseTiles(List<(int x, int y)> cliffLine)
    {
        List<(int x, int y)> currentList = new List<(int, int)>();
        List<(int x, int y)> currentListDirections = new List<(int, int)>();

        //Create a list of  cliffEdge tiles and the direction to traverse from them in
        for (int i=0; i<cliffLine.Count; i++)
        {
            //get index of tile we are currently looking at
            int currentTileIndex = mapTileInfo[cliffLine[i].x, cliffLine[i].y].getChosenTile();
            //if the tile is a cliff that is NOT a corner
            if ((currentTileIndex >= 4 && currentTileIndex <= 7))
            {
                //add that tile's coordinates to currentList
                currentList.Add((cliffLine[i].x, cliffLine[i].y));

                //get the diretion of the back of the cliffs so we know what direction to traverse in
                (int x, int y) directionToAdd = (new int(), new int());

                if (currentTileIndex >= 4 && currentTileIndex <= 7) //if tile is a cliff
                    directionToAdd = (cliffBacks[currentTileIndex - 4].x, cliffBacks[currentTileIndex - 4].y);

                currentListDirections.Add((directionToAdd.x, directionToAdd.y));
            }
        }

        //iterate through each tile in our current list
        for (int i = 0; i < currentList.Count; i++)
        {
            //get tile in the given direction from list
            (int x, int y) currentTileNeighbor = (currentList[i].x + currentListDirections[i].x, currentList[i].y + currentListDirections[i].y);

            //if said tile hasn't been touched
            if (!currentList.Contains((currentTileNeighbor.x, currentTileNeighbor.y)))
            {
                //add that tile's coordinates and that same direction to the list
                currentList.Add((currentTileNeighbor.x, currentTileNeighbor.y));
                currentListDirections.Add((currentListDirections[i].x, currentListDirections[i].y));
                //raise the tile a level
                mapTileInfo[currentTileNeighbor.x, currentTileNeighbor.y].addLevel();
            }
        }
    }
}
