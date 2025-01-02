using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

//Original script by Caroline and Cecil Manjerovic 5-5-24
//Edited version by Caroline Manjerovic 12-8-24

//represents a tile on the map and all information about it.
//contains the constrain and collapse function integral for wave function collapse.

public class Tile
{
    private int NUM_TILES; //the number of different tiles available overall
    private List<int> possibleTiles = new List<int>(); //a list of which tiles could still possibly be placed. stored as an array of indeces, each index corresponding to a tile
    private List<(int x, int y)> directions = new List<(int x, int y)>(); //list of directions, pointing to the tile's neighbors
    int x, y; //positions on the grid, assuming from the bottom left corner
    private bool collapsed; //whether or not the tile has been placed
    private int chosenTile; //the index of the tile placed there
    private int level; //the height the tile is to be spawned at
    private bool hasBeenRaised; //whether it is part of a cliffline that has been scanned already (only relevant for cliff tiles)

    public Tile(int nt, int x, int y) //inititalize
    {
        NUM_TILES = nt;

        //at this state, every tile is possible
        for (int i = 0; i < NUM_TILES; i++)
        {
            possibleTiles.Add(i);
        }

        collapsed = false;
        level = 0;
        this.x = x;
        this.y = y;
        hasBeenRaised = false;
    }

    public bool isCollapsed() {return collapsed;}

    public int getEntropy() {return possibleTiles.Count;}

    public List<int> getPossibleTiles() {return possibleTiles;}

    public int getChosenTile() {return chosenTile; }

    public void addLevel() {level++;}

    public int getLevel() {return level;}

    public int getX() {return x;}

    public int getY() {return y;}

    public void raise() {hasBeenRaised = true;}

    public bool getHasBeenRaised() {return hasBeenRaised;}

    //creates and passes an exact copy of itself. used by changesMade to prevent stored versions of the map from being passed by reference
    public Tile clone() {
        Tile tileClone = new Tile(NUM_TILES, this.x, this.y);
        tileClone.possibleTiles = this.possibleTiles;
        tileClone.collapsed = this.collapsed;
        tileClone.chosenTile = this.chosenTile;
        tileClone.level = this.level;
        tileClone.hasBeenRaised = this.hasBeenRaised;
        return tileClone;
    }

    //returns which directions the adjacent tiles are in. For tiles that are on the edges or corners this will differ.
    public List<(int x, int y)> getDirections(int MAP_SIZE)
    {
        this.directions.Clear();
        if (x+1 < MAP_SIZE)
        {
            directions.Add((1, 0));
        }
        if (x-1 >= 0) {
            directions.Add((-1,0));
        }
        if (y+1 < MAP_SIZE)
        {
            directions.Add((0, 1));
        }
        if (y-1 >= 0) {
            directions.Add((0,-1));
        }

        return directions;
    }

    //decides which tile should be placed from possible tiles, and updates all information accordingly.
    //uses weights to increase the possibility of certain tiles being called.
    //also calls a changesMade object to check which tiles have already been tried.
    public int collapse(List<int> weights, changesMade cm)
    {
        //trim the list of possible tiles to remove tiles already tried on that step
        List<(int, int, int)> tilesAlreadyTried = cm.getTilesAlreadyTried();
        List<int> possibleTilesThisStep = new List<int>();

        for (int i = 0; i < possibleTiles.Count; i++)
        {
            //if tile is not contained in tiles already tried, add it to possible tiles this step
            if (!tilesAlreadyTried.Contains((possibleTiles[i], this.x, this.y)))
                possibleTilesThisStep.Add(possibleTiles[i]);
        }

        //get a list of the weights of only the possible tiles
        List<int> possibleWeights = new List<int>();
        for (int i=0; i<possibleTilesThisStep.Count; i++) {
            possibleWeights.Add(weights[possibleTilesThisStep[i]]);
        }

        //sum weights and choose random in range of sum
        int weightSum = possibleWeights.Sum();
        int randomSum = UnityEngine.Random.Range(0, weightSum);

        //get index by subtracting possibleWeights from random sum. larger weights are more likely to reach 0
        int chosenIndex = 0;
        for (int i=0; i<possibleWeights.Count; i++) 
        {
            randomSum -= possibleWeights[i];
            if (randomSum <= 0) 
            {
                chosenIndex = possibleTilesThisStep[i];
                break;
            }
        }

        //mark the tile as collapsed
        collapsed = true;
        //mark this tile index and position as having been tried already this step
        cm.tryTile(chosenIndex, this.x, this.y);
        //change the possible tiles to just the index of the tile being spawned, will be needed when neighbors reference it in constrain()
        possibleTiles = new List<int>() {chosenIndex};
        //mark the index chosen as the index belonging to this tile
        chosenTile = chosenIndex;
        //return the index of the tile
        return chosenIndex;
    }

    //shortens the list of possible tiles and reduces the entropy.
    //for each call of this function, it does it in relation to one neighbor based on the ruleset and ruleset black list.
    public bool constrain(List<int> neighborPossibleTiles, 
                          (int x, int y) direction, 
                          int[,] ruleSet,
                          int[,] ruleSetBlackList)
    {
        //as tiles are found to have at least one match in the neighbor's possible tiles, they will be considered "safe" from elimination and added to this list.
        //this list will replace the current list of possible tiles at the end.
        List<int> safeTiles = new List<int>();

        //whether or not the tile was actually affected by this, true by default, needs to "prove" it's unaffected
        //"constrained" is needed to determine whether its neighbors should also be constrianed in the "wave" part of the algorithm.
        bool constrained = true;

        //get the current direction. the four indeces correspond to the four possible directions.
        //0 = up
        //1 = down
        //2 = left
        //3 = right
        //THE DIRECTION IS THE CURRENT TILE RELATIVE TO ITS NEIGHBOR. EG. UP: THE CURRENT TILE IS **ABOVE** THE NEIGHBOR.
        int thisDirection = 0;
        if (direction == (0, -1)) thisDirection = 1;
        else if (direction == (-1, 0)) thisDirection = 2;
        else if (direction == (1, 0)) thisDirection = 3;

        //OPPOSITE DIRECTION IS THE NEIGHBOR RELATIVE TO THE CURRENT TILE.
        int oppositeDirection = 0;
        if (thisDirection == 0) oppositeDirection = 1;
        else if (thisDirection == 2) oppositeDirection = 3;
        else if (thisDirection == 3) oppositeDirection = 2;

        //compare each neighbor possible tile to each this possible tile
        for (int i=0; i<neighborPossibleTiles.Count; i++)
        {
            for (int j=0; j<possibleTiles.Count; j++)
            {
                //compare the edge of this current possible tile in one direction with the edge of the neighbor's current possible tile in the other direction
                //get the edge types of both relevant edges, as well as the edge type from the black list
                int thisEdgeRule = ruleSet[possibleTiles[j], thisDirection];
                int neighborEdgeRule = ruleSet[neighborPossibleTiles[i], oppositeDirection];
                int thisEdgeRuleBlackList = ruleSetBlackList[possibleTiles[j], thisDirection];
                int neighborEdgeRuleBlackList = ruleSetBlackList[neighborPossibleTiles[i], oppositeDirection];

                if (thisEdgeRule == neighborEdgeRule && //if the edges are the same type
                    !(thisEdgeRuleBlackList == neighborEdgeRuleBlackList && thisEdgeRuleBlackList != 0) && //and they aren't the same type on the black list, disregarding 0's there
                    !safeTiles.Contains(possibleTiles[j])) //and this tile type hasn't already been added
                {
                    //if they match, this tile's current possible tile is considered "safe," and is still a possible tile.
                    safeTiles.Add(possibleTiles[j]);

                    //if the list of safe tiles comes to be the same size as the current possible tiles, then there was no effect.
                    if (safeTiles.Count == possibleTiles.Count)
                    {
                        //set constrained to false and stop everything
                        constrained = false;
                        break;
                    }
                }
                
            }
            if (!constrained) break;
        }

        //update the set of possible tiles to reflect the constrain
        possibleTiles = safeTiles;
        return constrained;
    }

}
